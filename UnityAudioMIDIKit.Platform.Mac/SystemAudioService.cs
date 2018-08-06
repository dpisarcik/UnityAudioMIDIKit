using System;
using System.Collections.Generic;
using MonoMac.AudioToolbox;
using MonoMac.AudioUnit;
using UnityAudioMIDIKit.Models;
using UnityAudioMIDIKit.Platform;
using UnityAudioMIDIKit.Platform.Mac.AudioObjects;
using UnityAudioMIDIKit.Platform.Mac.AudioUnits;
using System.Linq;
using System.Xml.Linq;

namespace UnityAudioMIDIKit.Platform.Mac
{
    public class SystemAudioService : ISystemAudioService
    {
        private AudioObjectService _audioObjectService = new AudioObjectService();

        public IEnumerable<IAudioInputDevice> GatherAudioInputDevices()
        {
            var result = new List<IAudioInputDevice>();
            foreach (var device in GatherAudioUnitDevices())
                result.Add(new AudioInputDevice() { Name = device.Name, DeviceID = device.DeviceID });

            return result.OrderBy(x => x.DeviceID);
        }

        private IEnumerable<AudioUnitDevice> GatherAudioUnitDevices()
        {
            var deviceIDs = _audioObjectService.GetAudioObjectPropertyDataDynamic<int[]>(AudioObjectConstants.K_AUDIO_OBJECT_SYSTEM_OBJECT, AudioObjectPropertySelector.PropertyDevices, AudioObjectPropertyScope.Global);

            var result = new List<AudioUnitDevice>();
            foreach (var deviceID in deviceIDs)
            {
                var device = new AudioUnitDevice();
                device.DeviceID = deviceID;
                device.Name = _audioObjectService.GetAudioObjectPropertyDataFixed<string>((uint)deviceID, AudioObjectPropertySelectorAdditionalOptions.AudioDevicePropertyDeviceName, AudioObjectPropertyScope.Global, 64);
                device.Manufacturer = _audioObjectService.GetAudioObjectPropertyDataFixed<string>((uint)deviceID, AudioObjectPropertySelectorAdditionalOptions.AudioDevicePropertyDeviceManufacturer, AudioObjectPropertyScope.Global, 64);

                result.Add(device);
            }

            return result;
        }
    }
}
