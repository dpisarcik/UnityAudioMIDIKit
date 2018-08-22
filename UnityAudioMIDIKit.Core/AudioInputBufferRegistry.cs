using System.Collections.Generic;
using System.Linq;
using System;
using UnityAudioMIDIKit.Core.Models;

namespace UnityAudioMIDIKit.Core
{
    internal class AudioInputBufferRegistry : IAudioInputBufferRegistry
    {
        public IEnumerable<IAudioInputBuffer> RegisteredBuffers => _registeredBuffers;

        public IAudioInputBuffer this[int deviceID] => RegisteredBuffers.FirstOrDefault(x => x.Device.DeviceID == deviceID);

        private readonly List<IAudioInputBuffer> _registeredBuffers = new List<IAudioInputBuffer>();

        public void AddBuffer(IAudioInputBuffer buffer)
        {
            if (_registeredBuffers.Any(x => x.Device.DeviceID == buffer.Device.DeviceID))
                throw new InvalidOperationException(String.Format("Device '{0}: {1}' is already registered.", buffer.Device.DeviceID, buffer.Device.Name));

            _registeredBuffers.Add(buffer);
        }

        public void RemoveBuffer (IAudioInputBuffer buffer)
        {
            var buffersToUnregister = (from x in _registeredBuffers
                                       where x.Device.DeviceID == buffer.Device.DeviceID
                                       select x).ToList();

            foreach(var bufferToUnregister in buffersToUnregister)
            //for (int i = 0; i < buffersToUnregister.Count(); i++)
                _registeredBuffers.Remove(bufferToUnregister);
        }
    }
}