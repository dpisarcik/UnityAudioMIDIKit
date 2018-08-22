using System;
using System.Security.Cryptography.X509Certificates;
using UnityAudioMIDIKit.Models;
using System.Collections;
using System.Collections.Generic;
using UnityAudioMIDIKit.Core;
using UnityAudioMIDIKit.Core.Models;

namespace UnityAudioMIDIKit.Core
{
    public interface IAudioInputBufferRegistry
    {
        IEnumerable<IAudioInputBuffer> RegisteredBuffers { get; }
        void AddBuffer(IAudioInputBuffer buffer);
        void RemoveBuffer(IAudioInputBuffer buffer);
        IAudioInputBuffer this[int deviceID] { get; }
    }
}