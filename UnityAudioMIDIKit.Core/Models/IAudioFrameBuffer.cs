using System;
using UnityAudioMIDIKit.Models;
namespace UnityAudioMIDIKit.Core.Models
{
    public interface IAudioFrameBuffer
    {
        IAudioInputDevice Device { get; }
    }
}
