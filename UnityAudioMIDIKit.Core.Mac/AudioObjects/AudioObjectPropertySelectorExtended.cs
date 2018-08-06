using MonoMac.AudioUnit;
using System;

namespace UnityAudioMIDIKit.Core.Mac.AudioObjects
{
    internal enum AudioObjectPropertySelectorAdditionalOptions
    {
        AudioDevicePropertyDeviceManufacturer = 1835101042,
        AudioDevicePropertyDeviceManufacturerCFString = 1819107691,
        AudioDevicePropertyDeviceName = 1851878757,
        AudioDevicePropertyDeviceNameCFString = 1819173229,
	};

    internal class AudioObjectPropertySelectorExtended
    {
        private uint Value { get; set; }

        public AudioObjectPropertySelectorExtended(uint value)
        {
            this.Value = value;
        }

        public static implicit operator AudioObjectPropertySelectorExtended(AudioObjectPropertySelector selector)
        {
            return new AudioObjectPropertySelectorExtended((uint)selector);
        }

        public static implicit operator AudioObjectPropertySelectorExtended(AudioObjectPropertySelectorAdditionalOptions selector)
        {
            return new AudioObjectPropertySelectorExtended((uint)selector);
        }

        public static implicit operator uint(AudioObjectPropertySelectorExtended selector)
        {
            return (uint)selector.Value;
        }
    }
}
