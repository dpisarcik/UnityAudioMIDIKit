using System;
using MonoMac.Foundation;
using MonoMac.CoreFoundation;
namespace UnityAudioMIDIKit.Platform.Mac.AudioUnits
{
    internal class AudioUnitDevice
    {
		public string Name { get; set; }
		public string Manufacturer { get; set; }
        public int DeviceID { get; internal set; }

        public override string ToString() => this.DeviceID.ToString() + ": " + this.Manufacturer + " - " + this.Name;
    }
}
