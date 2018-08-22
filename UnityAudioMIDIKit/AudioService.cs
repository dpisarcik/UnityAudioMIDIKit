using System;
using UnityAudioMIDIKit.Core;
using System.Collections;
using UnityAudioMIDIKit.Models;
using System.Collections.Generic;
using UnityAudioMIDIKit.Core.Models;
using System.Linq;

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

        public static IAudioInputDevice GetAudioInputDevice(int deviceID)
        {
            return _audioService.GetAudioInputDevice(deviceID);
        }

        public static IAudioInputBuffer OpenInputBuffer(IAudioInputDevice device)
        {
            return _audioService.OpenInputBuffer(device);
        }

        public static void CloseInputBuffer(IAudioInputBuffer buffer)
        {
            _audioService.CloseInputBuffer(buffer);
        }
    }
}
