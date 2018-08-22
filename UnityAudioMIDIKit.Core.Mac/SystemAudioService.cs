using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.AudioUnit;
using UnityAudioMIDIKit.Core.Mac.AudioDevices;
using UnityAudioMIDIKit.Core.Mac.AudioObjects;
using UnityAudioMIDIKit.Core.Mac.AudioUnits;
using UnityAudioMIDIKit.Core.Models;
using UnityAudioMIDIKit.Models;
using MonoMac.AudioToolbox;
using System.Runtime.InteropServices;
using MonoMac.Foundation;

namespace UnityAudioMIDIKit.Core.Mac
{
	public class SystemAudioService : SystemAudioServiceBase
    {
        public const int ARBITRARY_BUFFER_LENGTH = 96000;

        private AudioObjectService _audioObjectService = new AudioObjectService();

        public SystemAudioService()
        {
        }

        public override IEnumerable<IAudioInputDevice> GatherAudioInputDevices()
        {
            var result = new List<IAudioInputDevice>();
            foreach (var device in GatherAudioUnitDevices())
            {
                result.Add(new AudioInputDevice() { Name = device.Name, DeviceID = device.DeviceID });
            }

            return result.OrderBy(x => x.DeviceID);
        }

        private IEnumerable<AudioUnitDevice> GatherAudioUnitDevices()
        {
            var deviceIDs = _audioObjectService.GetAudioObjectPropertyDataDynamic<int[]>(AudioObjectConstants.K_AUDIO_OBJECT_SYSTEM_OBJECT, AudioObjectPropertySelector.PropertyDevices, AudioObjectPropertyScope.Input);

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

        protected override IAudioInputBuffer CreateSystemInputBuffer(IAudioInputDevice device)
        {
            return new SystemAudioInputBuffer(device);
        }

        protected override void OpenSystemInputBuffer(IAudioInputBuffer buffer)
        {
            if (!(buffer is SystemAudioInputBuffer))
                throw new ArgumentException("Expected " + nameof(SystemAudioInputBuffer) + " for parameter 'buffer'", nameof(buffer));
            var actualBuffer = buffer as SystemAudioInputBuffer;

            // Main bit - 8.2 - from Learning Core Audio p164
            var inputUnit = CreateInputUnit(actualBuffer);
            ErrorHandler.CheckError(inputUnit.Start);
        }

        private AudioUnit CreateInputUnit(SystemAudioInputBuffer buffer)
        {
            // Create input unit - 8.4 - 8.13 - Learning Core Audio p169 - 176

            //8.4 p169 - Create AudioUnit
            var component = GetHALAudioComponent();
            buffer.AudioUnit = component.CreateAudioUnit();
            buffer.AudioUnit.Initialize();

            //8.5 p169-170 - Enable IO on Input AUHAL
            ErrorHandler.CheckError(buffer.AudioUnit.SetEnableIO(true, AudioUnitScopeType.Input, AudioUnitConstant.Element.INPUT_BUS));
            ErrorHandler.CheckError(buffer.AudioUnit.SetEnableIO(false, AudioUnitScopeType.Output, AudioUnitConstant.Element.OUTPUT_BUS));

            //8.7 p171 - Set current device on AUHAL
            ErrorHandler.CheckError(buffer.AudioUnit.SetCurrentDevice((uint)buffer.Device.DeviceID, AudioUnitScopeType.Global, AudioUnitConstant.Element.OUTPUT_BUS));

            //8.8 p171-172 - Get format from AUHAL input
            var auHalFormat = buffer.AudioUnit.GetAudioFormat(AudioUnitScopeType.Output, AudioUnitConstant.Element.INPUT_BUS);

            //8.9 p172 - Use AUHAL input sample rate
            var auHalInputFormat = buffer.AudioUnit.GetAudioFormat(AudioUnitScopeType.Input, AudioUnitConstant.Element.INPUT_BUS);

            AudioStreamBasicDescription requestedFormat = auHalFormat;
            requestedFormat.SampleRate = auHalInputFormat.SampleRate;

            buffer.StreamFormat = requestedFormat;
            ErrorHandler.CheckError(buffer.AudioUnit.SetFormat(requestedFormat, AudioUnitScopeType.Output, AudioUnitConstant.Element.INPUT_BUS));

            //8.10 - 8.11 - Calculate buffer size & allocate AudioBuffers
            AllocateCoreAudioBuffers(buffer);

            //8.13 p174 - Create Input Callback -- sets callback for the SystemAudioInputBuffer instance
            ErrorHandler.CheckError(buffer.AudioUnit.SetInputCallback(buffer.InputRenderProc, AudioUnitScopeType.Global, AudioUnitConstant.Element.INPUT_BUS)); // contradicts book-- need INPUT_BUS to get trigger callback

            return buffer.AudioUnit;
        }



        private void AllocateCoreAudioBuffers(SystemAudioInputBuffer buffer)
        {
            //8.10 p173 - Calculate capture buffer size
            uint bufferSizeFrames = 0;
            int uintSize = Marshal.SizeOf(typeof(uint));
            ErrorHandler.CheckError(AudioUnitService.GetProperty(
                buffer.AudioUnit.Handle,
                AudioDevicePropertyID.BufferFrameSize,
                AudioUnitScopeType.Global,
                AudioUnitConstant.Element.OUTPUT_BUS,
                out bufferSizeFrames,
                ref uintSize
            ));

            //8.11-8.12 p173-175 - Allocate AudioBuffers & RingBuffer
            buffer.AllocateCoreAudioBuffers(buffer.StreamFormat.Value.ChannelsPerFrame, (int)bufferSizeFrames);
        }

        private AudioComponent GetHALAudioComponent()
        {
            var componentDescription = new AudioComponentDescription()
            {
                ComponentType = AudioComponentType.Output,
                ComponentSubType = (int)AudioUnitSubType.HALOutput,
                ComponentManufacturer = AudioComponentManufacturerType.Apple,
            };

            return AudioComponent.FindNextComponent(null, ref componentDescription);
        }

        protected override void CloseSystemInputBuffer(IAudioInputBuffer buffer)
        {
            var systemBuffer = buffer as SystemAudioInputBuffer;
            systemBuffer.AudioUnit.Stop();
        }

    }
}