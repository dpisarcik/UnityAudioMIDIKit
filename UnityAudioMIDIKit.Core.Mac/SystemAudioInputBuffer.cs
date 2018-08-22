using System;
using UnityAudioMIDIKit.Core.Models;
using UnityAudioMIDIKit.Models;
using MonoMac.AudioUnit;
using MonoMac.AudioToolbox;
using System.Runtime.InteropServices;
using MonoMac.SystemConfiguration;
using System.Net;
using MonoMac.CoreText;
using System.Diagnostics;
using System.Linq;

namespace UnityAudioMIDIKit.Core.Mac
{
    internal class SystemAudioInputBuffer : SystemAudioInputBufferBase
    {
        public AudioUnit AudioUnit 
        { 
            get => _audioUnit;
            set
            {
                if (_audioUnit == null)
                    _audioUnit = value;
                else
                    throw new InvalidOperationException("AudioUnit cannot be changed in Core.Mac.SystemAudioInputBuffer.");
            }
         }

        private AudioUnit _audioUnit;

        public AudioStreamBasicDescription? StreamFormat { get; set; }
        public AudioBuffers AudioBuffers { get; private set; }
        public RingBuffer CoreAudioRingBuffer { get; private set; }
        public double FirstInputSampleTime { get; private set; }
        public double FirstOutputSampleTime { get; private set; }
        public double InToOutSampleTimeOffset { get; private set; }

        public override int SampleRate => this.StreamFormat.HasValue ? (int)this.StreamFormat.Value.SampleRate : -1;

        public SystemAudioInputBuffer(IAudioInputDevice device) : base(device)
        {
        }

        ~SystemAudioInputBuffer()
        {
            if (this.CoreAudioRingBuffer != null)
                this.CoreAudioRingBuffer.Deallocate();
            this.DeallocateCoreAudioBuffers();
        }

        public AudioUnitStatus InputRenderProc(AudioUnitRenderActionFlags actionFlags, AudioTimeStamp timeStamp, uint busNumber, uint numberFrames, AudioUnit audioUnit)
        {
            //8.15 p176 - Skip, because callback belongs to instance

            //8.16 p177 - Log timestamps & calculate time stamp offset
            // Have we ever logged input timing? (for offset calculation)
            if (this.FirstInputSampleTime < 0.0)
            {
                this.FirstInputSampleTime = timeStamp.SampleTime;
                if (this.FirstOutputSampleTime > -1.0 && this.InToOutSampleTimeOffset < 0.0) // From book errata: http://ptgmedia.pearsoncmg.com/images/9780321636843/errata/9780321636843learning-core-audio-errata-jan-23-2013.txt
                    this.InToOutSampleTimeOffset = this.FirstInputSampleTime - this.FirstOutputSampleTime;
            }

            //8.17 p177 - Retrieve captured samples from Input AUHAL
            int status = (int)this.AudioUnit.Render(ref actionFlags, timeStamp, busNumber, numberFrames, this.AudioBuffers);

            //8.18 p178 - Store captured samples into RingBuffer
            if (status == (int)AudioUnitStatus.OK)  // && timeStamp.SampleTime < this.BufferLength && timeStamp.SampleTime >= 0)
            {
                status = (int)this.CoreAudioRingBuffer.Store(this.AudioBuffers, numberFrames, (long)timeStamp.SampleTime);

                if (status == (int)AudioUnitStatus.OK)
                {
                    float[] newSamples;
                    status = (int)this.CoreAudioRingBuffer.Fetch(out newSamples, numberFrames, (long)timeStamp.SampleTime);
                    this.AddSamplesToRingBufferX(newSamples);
                }

                //if (this.FirstOutputSampleTime < 0.0)
                //{
                //    this.FirstOutputSampleTime = timeStamp.SampleTime;
                //    if (this.FirstInputSampleTime > 0.0 && this.InToOutSampleTimeOffset < 0.0)
                //    {
                //        this.InToOutSampleTimeOffset = this.FirstInputSampleTime - this.FirstOutputSampleTime;
                //    }
                //}

                // GET floats
                //this.AddSamplesToRingBufferX(this.AudioBuffers[0].Data, (long)timeStamp.SampleTime, (int)numberFrames);
                //throw new NotImplementedException("Need to fetch floats for single channel... Fetch pulls entire input channel");

            }
            return ErrorHandler.CheckError((AudioUnitStatus)status);
        }

        public AudioUnitStatus HandleRenderDelegate(AudioUnitRenderActionFlags actionFlags, AudioTimeStamp timeStamp, uint busNumber, uint numberFrames, AudioBuffers data)
        {
            int sampleCount = data[0].DataByteSize / Marshal.SizeOf(typeof(float));
            float[] samples = new float[sampleCount];
            Marshal.Copy(data[0].Data, samples, 0, sampleCount);

            Console.WriteLine("{0} samples in buffer --- Min: {1},       Max: {2}", numberFrames, samples.Min(), samples.Max());

            //if (this.FirstOutputSampleTime < 0.0)
            //{
            //    this.FirstOutputSampleTime = timeStamp.SampleTime;
            //    if (this.FirstInputSampleTime > 0.0 && this.InToOutSampleTimeOffset < 0.0)
            //    {
            //        this.InToOutSampleTimeOffset = this.FirstInputSampleTime - this.FirstOutputSampleTime;
            //    }
            //}

            //this.AddSamplesToRingBufferX(data[0].Data, (int)numberFrames);
            return AudioUnitStatus.OK;
        }

        public void AllocateCoreAudioBuffers(int numChannels, int bufferSizeFrames)
        {
            if (this.StreamFormat == null)
                throw new InvalidOperationException("StreamFormat must be set before invoking AllocateCoreAudioBuffers()");

            // 8.11 - p173-174 - Allocate AudioBuffers (formerly AudioBufferLists)
            int bufferSizeBytes = bufferSizeFrames * Marshal.SizeOf(typeof(float));
            var audioBuffers = new AudioBuffers(numChannels);

            for (int i = 0; i < audioBuffers.Count; i++)
            {
                IntPtr newPtr = Marshal.AllocHGlobal(bufferSizeBytes);
                audioBuffers.SetData(i, newPtr, bufferSizeBytes);
            }

            this.AudioBuffers = audioBuffers;

            // 8.12 - p174-175 - Allocate RingBuffers
            double millisecondsPerFrame = ((double)bufferSizeFrames / this.StreamFormat.Value.SampleRate) * 1000d;
            uint numFramesRequired = (uint)Math.Ceiling(MIN_MILLISECONDS_PER_RING_BUFFER / millisecondsPerFrame);

            this.CoreAudioRingBuffer = new RingBuffer();
            this.CoreAudioRingBuffer.Allocate(numChannels, (uint)this.StreamFormat.Value.BytesPerFrame, (uint)bufferSizeFrames * numFramesRequired);
            this.BufferLength = (int)numFramesRequired * bufferSizeFrames;
            this.RingBufferX = new float[this.BufferLength];
        }

        private void DeallocateCoreAudioBuffers()
        {
            if (this.AudioBuffers != null)
            {
                for (int i = 0; i < this.AudioBuffers.Count; i++)
                    Marshal.FreeHGlobal(this.AudioBuffers[i].Data);
            }
        }
    }
}
