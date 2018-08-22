using System;
using UnityAudioMIDIKit.Models;
namespace UnityAudioMIDIKit.Core.Models
{
    public interface IAudioInputBuffer
    {
        IAudioInputDevice Device { get; }
        int SampleRate { get; }
        float[] RetrieveBufferedSamples();
    }
}
