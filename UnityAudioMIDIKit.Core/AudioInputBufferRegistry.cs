using System.Collections.Generic;
using System.Linq;
using UnityAudioMIDIKit.Models;
using System;

namespace UnityAudioMIDIKit.Core
{
    internal class ActiveInputRegistry : IActiveInputRegistry
    {
        public IEnumerable<IAudioInputDevice> RegisteredDevices => _registeredDevices;

        private readonly List<IAudioInputDevice> _registeredDevices = new List<IAudioInputDevice>();

        public void AddDevice(IAudioInputDevice device)
        {
            if (_registeredDevices.Any(x => x.DeviceID == device.DeviceID))
                throw new InvalidOperationException(String.Format("Device '{0}: {1}' is already registered.", device.DeviceID, device.Name));

            _registeredDevices.Add(device);
        }

        public void RemoveDevice (IAudioInputDevice device)
        {
            var devicesToUnregister = (from x in _registeredDevices
                                       where x.DeviceID == device.DeviceID
                                       select x);

            foreach (var deviceToUnregister in devicesToUnregister)
                _registeredDevices.Remove(deviceToUnregister);
        }
    }
}