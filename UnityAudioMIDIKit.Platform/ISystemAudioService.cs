using System;
using System.Collections.Generic;
using UnityAudioMIDIKit.Models;

namespace UnityAudioMIDIKit.Platform
{
    public interface ISystemAudioService
    {
        IEnumerable<IAudioInputDevice> GatherAudioInputDevices();
    }
}
