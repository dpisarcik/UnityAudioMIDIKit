using System;
using System.Runtime.InteropServices;
using MonoMac.AudioToolbox;
using System.Linq;

namespace UnityAudioMIDIKit.Core.Mac
{
    public class RingBuffer
    {
        public enum RingBufferStatus
		{
            OK = 0,
            TooMuch = 3,
            CPUOverload = 4,
		}

        public void Allocate(int numChannels, uint bytesPerFrame, uint capacityFrames)
        {
            this.Deallocate();

            capacityFrames = NextPowerOfTwo(capacityFrames);

            _numberChannels = numChannels;
            _bytesPerFrame = bytesPerFrame;
            _capacityFrames = capacityFrames;
            _capacityFramesMask = capacityFrames - 1;
            _capacityBytes = bytesPerFrame * capacityFrames;

            // Allocate everything in one memory allocation-- pointers, then deinterleaved channels
            int allocSize = ((int)_capacityBytes + Marshal.SizeOf(typeof(IntPtr))) * _numberChannels;
            _allocPtr = Marshal.AllocHGlobal(allocSize);
            InteropHelper.FillWithZeroes(_allocPtr, allocSize);

            _bufferPtrs = new IntPtr[_numberChannels];

            var curPtr = _allocPtr;
            curPtr += _numberChannels * Marshal.SizeOf(typeof(IntPtr));
            for (int i = 0; i < _numberChannels; ++i)
            {
                _bufferPtrs[i] = curPtr;
                curPtr += (int)_capacityBytes;
            }

            for (uint i = 0; i < GENERAL_RING_TIME_BOUNDS_QUEUE_SIZE; ++i)
            {
                _timeBoundsQueue[i].StartTime = 0;
                _timeBoundsQueue[i].EndTime = 0;
                _timeBoundsQueue[i].UpdateCounter = 0;
            }

            _timeBoundsQueuePtr = 0;
        }

        public void Deallocate() 
        {
            if (_allocPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_allocPtr);
                _allocPtr = IntPtr.Zero;
            }

            _numberChannels = 0;
            _capacityBytes = 0;
            _capacityFrames = 0;
        }

        private int GetBufferAllocSize()
        {
            return ((int)_capacityBytes + Marshal.SizeOf(typeof(IntPtr))) * _numberChannels;
        }

        private void ZeroRange(IntPtr[] buffers, int numChannels, int offset, int numBytes)
        {
            for (int i = 0; i < numChannels; i++)
                InteropHelper.FillWithZeroes(buffers[i] + offset, numBytes);
        }

        private void StoreABL(IntPtr[] buffers, int destOffset, AudioBuffers audioBuffers, int srcOffset, int numBytes)
        {
            int numChannels = audioBuffers.Count;
            AudioBuffer src;

            for (int i = 0; i < numChannels; i++)
            {
                src = audioBuffers[i];
                if (srcOffset > src.DataByteSize) 
                    continue;
                InteropHelper.MemCpy(buffers[i] + destOffset, src.Data + srcOffset, src.DataByteSize - srcOffset);

                int numNewSamples = (src.DataByteSize - srcOffset) / Marshal.SizeOf(typeof(float));
                float[] newSamples = new float[numNewSamples];
                Marshal.Copy(src.Data + srcOffset, newSamples, 0, numNewSamples);

                //Console.WriteLine("{0} samples written to CARingBuffer.  Max: {1}", numNewSamples, newSamples.Max());
            }
        }

        private void FetchABL(AudioBuffers audioBuffers, int destOffset, IntPtr[] buffers, int srcOffset, int numBytes)
        {
            int numChannels = audioBuffers.Count;
            AudioBuffer dest;

            for (int i = 0; i < numChannels; i++)
            {
                dest = audioBuffers[i];
                if (destOffset > dest.DataByteSize) 
                    continue;
                InteropHelper.MemCpy(dest.Data + destOffset, buffers[i] + srcOffset, Math.Min(numBytes, dest.DataByteSize - destOffset));
            }
        }

        private void ZeroABL(AudioBuffers audioBuffers, int destOffset, int numBytes)
        {
            int numBuffers = audioBuffers.Count;
            AudioBuffer dest;
            for (int i = 0; i < numBuffers; i++)
            {
                dest = audioBuffers[i];
                if (destOffset > dest.DataByteSize) continue;
                InteropHelper.FillWithZeroes(dest.Data + destOffset, Math.Min(numBytes, dest.DataByteSize - destOffset));
            }
        }

        public RingBufferStatus Store(AudioBuffers audioBuffers, uint numFramesToWrite, long startWrite)
        {
            if (numFramesToWrite == 0)
                return RingBufferStatus.OK;

            if (numFramesToWrite > _capacityFrames)
                return RingBufferStatus.TooMuch;

            long endWrite = startWrite + numFramesToWrite;

            if (startWrite < EndTime)
            {
                // Going backwards, throw everything out.
                SetTimeBounds(startWrite, startWrite);
            }
            else if (endWrite - StartTime <= _capacityFrames)
            { // buffer has not wrapped & will not need to 
            }
            else
            {
                // Advance start time past the region we are about to overwrite
                long newStart = endWrite - _capacityFrames;
                long newEnd = Math.Max(newStart, EndTime);
                SetTimeBounds(newStart, newEnd);
            }

            // Write the new frames.
            var buffers = _bufferPtrs;
            int nChannels = _numberChannels;
            int offset0, offset1, numBytes;
            long curEnd = EndTime;

            if (startWrite > curEnd)
            {
                // we are skipping some samples, so zero the range we are skipping.
                offset0 = FrameOffset(curEnd);
                offset1 = FrameOffset(startWrite);
                if (offset0 < offset1)
                    ZeroRange(buffers, nChannels, offset0, offset1 - offset0);
                else
                {
                    ZeroRange(buffers, nChannels, offset0, (int)_capacityBytes - offset0);
                    ZeroRange(buffers, nChannels, 0, offset1);
                }
                offset0 = offset1;
            }
            else
            {
                offset0 = FrameOffset(startWrite);
            }

            offset1 = FrameOffset(startWrite);
            if (offset0 < offset1)
            {
                StoreABL(buffers, offset0, audioBuffers, 0, offset1 - offset0);
            }
            else
            {
                numBytes = (int)_capacityBytes - offset0;
                StoreABL(buffers, offset0, audioBuffers, 0, numBytes);
                StoreABL(buffers, 0, audioBuffers, numBytes, offset1);
            }

            // now update the end time
            SetTimeBounds(StartTime, endWrite);

            return RingBufferStatus.OK;
        }

        public RingBufferStatus Fetch(out float[] newSamples, uint numFrames, long startRead, int channel = 0)
        {
            newSamples = new float[0];
            if (numFrames == 0)
                return RingBufferStatus.OK;

            newSamples = new float[numFrames];

            startRead = Math.Max(0, startRead);

            long endRead = startRead + numFrames;

            long startRead0 = startRead;

            RingBufferStatus status = ClipTimeBounds(ref startRead, ref endRead);
            if (status != RingBufferStatus.OK)
                return status;
            if (startRead == endRead)
            {
                return RingBufferStatus.OK;
            }


            int destStartArrayIndexOffset = Math.Max((int)0, (int)((startRead - startRead0)));

            var buffers = _bufferPtrs;
            int srcByteOffset0 = FrameOffset(startRead);
            int srcByteOffset1 = FrameOffset(endRead);
            int numBytes;

            if (srcByteOffset0 < srcByteOffset1)
            {
                numBytes = srcByteOffset1 - srcByteOffset0;
                FetchFloat(ref newSamples, destStartArrayIndexOffset, buffers, srcByteOffset0, numBytes, channel);
            }
            else
            {
                numBytes = (int)_capacityBytes - srcByteOffset0;
                int numSamples = numBytes / (int)_bytesPerFrame;
                FetchFloat(ref newSamples, destStartArrayIndexOffset, buffers, srcByteOffset0, numBytes, channel);
                FetchFloat(ref newSamples, destStartArrayIndexOffset + numSamples, buffers, 0, srcByteOffset1, channel);
                numBytes += srcByteOffset1; //Is this right??? Should this be one line up?
            }

            return RingBufferStatus.OK;
        }

        //AudioBuffers audioBuffers, int destOffset, IntPtr[] buffers, int srcOffset, int numBytes
        private void FetchFloat(ref float[] newSamples, int destArrayIndexOffset, IntPtr[] buffers, int srcByteOffset, int numBytes, int channel)
        {
            if (destArrayIndexOffset > newSamples.Length) return;

            int copyLength = Math.Min(numBytes / Marshal.SizeOf(typeof(float)), newSamples.Length - destArrayIndexOffset);
            float[] fetchSet = new float[copyLength];

            Marshal.Copy(buffers[channel] + srcByteOffset, fetchSet, 0, copyLength);
            Array.Copy(fetchSet, 0, newSamples, destArrayIndexOffset, copyLength);
        }

        public RingBufferStatus Fetch(AudioBuffers audioBuffers, uint numFrames, long startRead, int[] channels = null)
        {
            if (numFrames == 0)
                return RingBufferStatus.OK;

            startRead = Math.Max(0, startRead);

            long endRead = startRead + (int)numFrames;

            long startRead0 = startRead;
            long endRead0 = endRead;

            RingBufferStatus status = ClipTimeBounds(ref startRead, ref endRead);
            if (status != RingBufferStatus.OK)
                return status;
            if (startRead == endRead)
            {
                ZeroABL(audioBuffers, 0, (int)(numFrames * _bytesPerFrame));
                return RingBufferStatus.OK;
            }

            int byteSize = (int)((endRead - startRead) * _bytesPerFrame);

            int destStartByteOffset = Math.Max((int)0, (int)((startRead - startRead0) * _bytesPerFrame));

            if (destStartByteOffset > 0)
                ZeroABL(audioBuffers, 0, Math.Min((int)(numFrames * _bytesPerFrame), destStartByteOffset));

            int destEndSize = Math.Max((int)0, (int)(endRead0 - endRead));
            if (destEndSize > 0)
                ZeroABL(audioBuffers, destStartByteOffset + byteSize, destEndSize * (int)_bytesPerFrame);

            var buffers = _bufferPtrs;
            int offset0 = FrameOffset(startRead);
            int offset1 = FrameOffset(endRead);
            int numBytes;

            if (offset0 < offset1)
            {
                numBytes = offset1 - offset0;
                FetchABL(audioBuffers, destStartByteOffset, buffers, offset0, numBytes);
            }
            else
            {
                numBytes = (int)_capacityBytes - offset0;
                FetchABL(audioBuffers, destStartByteOffset, buffers, offset0, numBytes);
                FetchABL(audioBuffers, destStartByteOffset + numBytes, buffers, 0, offset1);
                numBytes += offset1;
            }

            int nchannels = audioBuffers.Count;
            AudioBuffer dest;
            for (int i = 0; i < nchannels; i++)
            {
                dest = audioBuffers[i];
                dest.DataByteSize = numBytes;
            }

            return RingBufferStatus.OK;
        }

        public RingBufferStatus GetTimeBounds(ref long startTime, ref long endTime) 
        {
            for (int i = 0; i < 8; ++i) // fail after a few tries
            {
                uint curPtr = TimeBoundsQueuePtr;
                uint index = curPtr & GENERAL_RING_TIME_BOUNDS_QUEUE_MASK;
                TimeBounds bounds = _timeBoundsQueue[index];

                startTime = bounds.StartTime;
                endTime = bounds.EndTime;
                uint newPtr = bounds.UpdateCounter;

                if (newPtr == curPtr)
                    return RingBufferStatus.OK;
            }

            return RingBufferStatus.CPUOverload;
        }

        ~RingBuffer()
        {
            this.Deallocate();
        }

        protected struct TimeBounds {
            public long StartTime;
            public long EndTime;
            public uint UpdateCounter;
        }

		private const int GENERAL_RING_TIME_BOUNDS_QUEUE_SIZE = 32;
		private const int GENERAL_RING_TIME_BOUNDS_QUEUE_MASK = GENERAL_RING_TIME_BOUNDS_QUEUE_SIZE - 1;

        #region Protected methods
		protected int FrameOffset(long frameNumber) 
		{ 
            return (int)((frameNumber & GENERAL_RING_TIME_BOUNDS_QUEUE_MASK) * _bytesPerFrame); 
		}

		protected RingBufferStatus ClipTimeBounds(ref long startRead, ref long endRead)
		{
            long startTime = 0;
            long endTime = 0;

            RingBufferStatus status = GetTimeBounds(ref startTime, ref endTime);
            if (status != RingBufferStatus.OK)
                return status;

            if (startRead > endTime || endRead < startTime)
            {
                endRead = startRead;
                return RingBufferStatus.OK;
            }

            startRead = Math.Max(startRead, startTime);
            endRead = Math.Min(endRead, endTime);
            endRead = Math.Max(endRead, startRead);

            return RingBufferStatus.OK; // success
		}

		protected long StartTime { get { return _timeBoundsQueue[TimeBoundsQueuePtr & GENERAL_RING_TIME_BOUNDS_QUEUE_MASK].StartTime; } }
        protected long EndTime { get { return _timeBoundsQueue[TimeBoundsQueuePtr & GENERAL_RING_TIME_BOUNDS_QUEUE_MASK].EndTime; } }
		protected void SetTimeBounds(long startTime, long endTime)
		{
            uint nextPtr = (uint)_timeBoundsQueuePtr + 1;
            uint index = nextPtr & GENERAL_RING_TIME_BOUNDS_QUEUE_MASK;

            _timeBoundsQueue[index].StartTime = startTime;
            _timeBoundsQueue[index].EndTime = endTime;
            _timeBoundsQueue[index].UpdateCounter = nextPtr;

            InteropHelper.OSAtomicCompareAndSwap32Barrier((int)_timeBoundsQueuePtr, _timeBoundsQueuePtr + 1, ref _timeBoundsQueuePtr);
        }

        #endregion Protected methods

        #region Protected members

        protected IntPtr _allocPtr = IntPtr.Zero;
        protected IntPtr[] _bufferPtrs;
		protected int _numberChannels = 0;
		protected uint _bytesPerFrame;
		protected uint _capacityFrames = 0;
		protected uint _capacityFramesMask;
		protected uint _capacityBytes = 0;

		protected TimeBounds[] _timeBoundsQueue = new TimeBounds[GENERAL_RING_TIME_BOUNDS_QUEUE_SIZE];
    	int _timeBoundsQueuePtr;
        uint TimeBoundsQueuePtr => (uint)_timeBoundsQueuePtr;

        #endregion Protected members


        private uint NextPowerOfTwo(uint val)
        {
            uint result = val;
            result |= result >> 1;
            result |= result >> 2;
            result |= result >> 4;
            result |= result >> 8;
            result |= result >> 16;
            result++;

            return result;
        }
    }
}
