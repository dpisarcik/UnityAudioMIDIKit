using System;
using System.Runtime.InteropServices;
using UnityAudioMIDIKit.Models;

namespace UnityAudioMIDIKit.Core.Models
{
    public abstract class SystemAudioInputBufferBase : IAudioInputBuffer
    {
        protected const int MIN_MILLISECONDS_PER_RING_BUFFER = 200;

        public IAudioInputDevice Device { get; private set; }

        protected int LastSampleRecorded { get; set; } = SAMPLES_NOT_LOADED;
        protected float[] RingBufferX { get; set; }
        public int BufferLength { get; protected set; } 

        private int LastSampleRetrieved { get; set; } = SAMPLES_NOT_LOADED;

        public abstract int SampleRate { get; }

        private const int SAMPLES_NOT_LOADED = -1;

        public SystemAudioInputBufferBase(IAudioInputDevice device)
        {
            this.Device = device;
        }

        public float[] RetrieveBufferedSamples()
        {
            int lastSampleRecorded = this.LastSampleRecorded; // Using local variable to avoid race condition. Events may continue to write this.LastSampleRecorded as we read this data.
            int sourceIdx = this.LastSampleRetrieved + 1;
            if (sourceIdx >= this.BufferLength)
                sourceIdx = 0;

            float[] outputSamples;
            int destIdx = 0;

            int copyLength;
            if (lastSampleRecorded < this.LastSampleRetrieved)
            {
                copyLength = this.BufferLength - sourceIdx;

                // Set array size & copy tail end of the buffer.
                outputSamples = new float[lastSampleRecorded + (this.BufferLength - sourceIdx) + 1];
                Array.Copy(this.RingBufferX, sourceIdx, outputSamples, 0, copyLength);

                // Set to wrap around.
                sourceIdx = 0;
                destIdx = copyLength;
            }
            else
            {
                // Set array size.
                outputSamples = new float[lastSampleRecorded - (sourceIdx - 1)];
            }

            if (outputSamples.Length <= 0)
                return outputSamples;

            // Copy to last index.
            copyLength = lastSampleRecorded - sourceIdx + 1;
            Array.Copy(this.RingBufferX, sourceIdx, outputSamples, destIdx, copyLength);

            this.LastSampleRetrieved = lastSampleRecorded;
            return outputSamples;
        }

        protected void AddSamplesToRingBufferX(float[] newSamples)
        {
            int numberSamples = newSamples.Length;

            //int numberSamples = (audioBuffer.DataByteSize / Marshal.SizeOf(typeof(float))) / audioBuffer.NumberChannels;
            int destIndex = this.LastSampleRecorded + 1;

            int sourceIndex = 0;

            if (destIndex >= this.BufferLength)
                destIndex = 0;

            // If length exceeds tail end of buffer, write to end and save rest for later.
            int copyLength = 0;
            if (destIndex + numberSamples >= this.BufferLength)
            {
                copyLength = this.BufferLength - destIndex;

                // Set array size & copy to tail end of the ring buffer.
                Array.Copy(newSamples, 0, RingBufferX, destIndex, copyLength);

                // Set to wrap around.
                sourceIndex = copyLength;
                destIndex = 0;
            }

            // Write remaining samples to ring buffer.
            copyLength = numberSamples - copyLength;
            Array.Copy(newSamples, sourceIndex, RingBufferX, destIndex, copyLength);

            this.LastSampleRecorded = (this.LastSampleRecorded + numberSamples) % RingBufferX.Length;
        }

        protected void AddSamplesToRingBufferX(IntPtr ptrToFloatSamples, long startIndex, int numberSamples)
        {

            // Copy to managed array.
            float[] newSamples = new float[numberSamples];
            Marshal.Copy(ptrToFloatSamples, newSamples, (int)startIndex, numberSamples);

            this.AddSamplesToRingBufferX(newSamples);
        }
    }
}
