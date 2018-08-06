using System;
using UnityAudioMIDIKit.Core;
using System.Collections;
using UnityAudioMIDIKit.Models;
using System.Collections.Generic;

namespace UnityAudioMIDIKit
{
    public static class AudioService
    {
        #if MACOSX
        private static ISystemAudioService _audioService = new UnityAudioMIDIKit.Core.Mac.SystemAudioService();
        #endif

        public static IEnumerable<IAudioInputDevice> GatherAudioInputDevices()
        {
            return _audioService.GatherAudioInputDevices();
        }


    }
}
