using System;
using System.Security.Cryptography.X509Certificates;
using UnityAudioMIDIKit.Models;
using System.Collections;
using System.Collections.Generic;
using UnityAudioMIDIKit.Core;

namespace UnityAudioMIDIKit.Core
{
    public interface IInputBufferRegistry
    {
        IEnumerable<IAudioInputDevice> RegisteredDevices { get; }
        void RegisterDevice(IAudioInputDevice device);
        void UnregisterDevice(IAudioInputDevice device);
    }
}