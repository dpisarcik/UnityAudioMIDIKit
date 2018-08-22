using System;
using System.Collections.Generic;
using UnityAudioMIDIKit.Models;
using UnityAudioMIDIKit.Core.Models;

namespace UnityAudioMIDIKit
{
    public interface ISystemAudioService
    {
        IEnumerable<IAudioInputDevice> GatherAudioInputDevices();
        IAudioInputBuffer OpenInputBuffer(IAudioInputDevice device);
        void CloseInputBuffer(IAudioInputBuffer buffer);
        IAudioInputDevice GetAudioInputDevice(int deviceID);
    }
}
