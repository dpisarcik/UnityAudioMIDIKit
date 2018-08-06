using System;
using System.Collections.Generic;
using UnityAudioMIDIKit.Models;

namespace UnityAudioMIDIKit
{
    public interface ISystemAudioService
    {
        IEnumerable<IAudioInputDevice> GatherAudioInputDevices();
    }
}
