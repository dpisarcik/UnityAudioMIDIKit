using System;
using UnityAudioMIDIKit.Models;

namespace UnityAudioMIDIKit.Core.Models
{
    public abstract class AudioInputBufferBase : IAudioInputBuffer
    {
        public IAudioInputDevice Device { get; private set; }

        public int LastSampleRecorded { get; set; } = SAMPLES_NOT_LOADED;
        public float[] RingBuffer { get; set; }
        public int BufferLength => _bufferLength;

        private int LastSampleRetrieved { get; set; } = SAMPLES_NOT_LOADED;
        private const int SAMPLES_NOT_LOADED = -1;

        private readonly int _bufferLength;

        public AudioInputBufferBase(IAudioInputDevice device)
        {
            this.Device = device;
        }

        public float[] RetrieveBufferedSamples()
        {
            int lastSampleRecorded = this.LastSampleRecorded; // Using local variable to avoid race condition. Events may continue to write this.LastSampleRecorded as we read this data.
            int sourceIdx = this.LastSampleRetrieved + 1;
            if (sourceIdx >= _bufferLength)
                sourceIdx = 0;

            float[] outputSamples;
            int destIdx = 0;

            int copyLength;
            if (lastSampleRecorded < this.LastSampleRetrieved)
            {
                copyLength = _bufferLength - sourceIdx;

                // Set array size & copy tail end of the buffer.
                outputSamples = new float[lastSampleRecorded + (_bufferLength - sourceIdx) + 1];
                Array.Copy(this.RingBuffer, sourceIdx, outputSamples, 0, copyLength);

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
            Array.Copy(this.RingBuffer, sourceIdx, outputSamples, destIdx, copyLength);

            this.LastSampleRetrieved = lastSampleRecorded;
            return outputSamples;
        }
    }
}
