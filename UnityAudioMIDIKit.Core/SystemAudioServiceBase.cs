using System;
using System.Collections.Generic;
using UnityAudioMIDIKit.Models;
using UnityAudioMIDIKit.Core.Models;
using System.Linq;

namespace UnityAudioMIDIKit.Core
{
    public abstract class SystemAudioServiceBase : ISystemAudioService
    {
        protected readonly IAudioInputBufferRegistry _inputBufferRegistry = new AudioInputBufferRegistry();

        public abstract IEnumerable<IAudioInputDevice> GatherAudioInputDevices();

        public IAudioInputDevice GetAudioInputDevice(int deviceID)
        {
            return GatherAudioInputDevices().FirstOrDefault(x => x.DeviceID == deviceID);
        }

        public IAudioInputBuffer OpenInputBuffer(IAudioInputDevice device)
        {
            var buffer = CreateSystemInputBuffer(device);
            _inputBufferRegistry.AddBuffer(buffer);
            OpenSystemInputBuffer(buffer);
            return buffer;
        }

        public void CloseInputBuffer(IAudioInputBuffer buffer)
        {
            CloseSystemInputBuffer(buffer);
            _inputBufferRegistry.RemoveBuffer(buffer);
        }

        /// <summary>
        /// Creates an uninitialized AudioInputBuffer.
        /// </summary>
        /// <returns>An uninitialized system input buffer for the given platform.</returns>
        /// <param name="device">Device.</param>
        protected abstract IAudioInputBuffer CreateSystemInputBuffer(IAudioInputDevice device);

        /// <summary>
        /// Opens the hardware device for the given platform. Audio will begin recording immediately, enabling retrieval of samples upon request.
        /// </summary>
        /// <param name="buffer">Uninitialized IAudioInputBuffer.</param>
        protected abstract void OpenSystemInputBuffer(IAudioInputBuffer buffer);

        /// <summary>
        /// Closes the hardware device for the given platform.  Frees up allocated buffer from memory.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        protected abstract void CloseSystemInputBuffer(IAudioInputBuffer buffer);
       
        ~SystemAudioServiceBase()
        {
            foreach (var inputDevice in _inputBufferRegistry.RegisteredBuffers)
                CloseInputBuffer(inputDevice);
        }
    }
}
