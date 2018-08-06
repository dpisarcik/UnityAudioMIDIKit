using System;
using UnityAudioMIDIKit.Models;
using UnityAudioMIDIKit.Platform;

namespace UnityAudioMIDIKit.Platform.Mac
{
	internal class AudioInputDevice : IAudioInputDevice
    {
		public string Name { get; internal set; }
		public int DeviceID { get; internal set; }
    }
}
