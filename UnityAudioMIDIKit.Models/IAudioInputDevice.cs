using System;
namespace UnityAudioMIDIKit.Models
{
    public interface IAudioInputDevice
    {
        string Name { get; }
        int DeviceID { get; }
    }
}
