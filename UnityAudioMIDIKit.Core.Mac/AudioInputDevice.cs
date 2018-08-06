﻿using System;
using UnityAudioMIDIKit.Models;

namespace UnityAudioMIDIKit.Core.Mac
{
	internal class AudioInputDevice : IAudioInputDevice
    {
		public string Name { get; internal set; }
		public int DeviceID { get; internal set; }
    }
}
