using System;
using System.Runtime.InteropServices;
using MonoMac.AudioUnit;

namespace UnityAudioMIDIKit.Platform.Mac.AudioObjects
{
   [StructLayout(LayoutKind.Sequential)]
    internal struct AudioObjectPropertyAddress
    {
        public uint /* UInt32 */ Selector;
        public uint /* UInt32 */ Scope;
        public uint /* UInt32 */ Element;

        public AudioObjectPropertyAddress(uint selector, uint scope, uint element)
        {
            Selector = selector;
            Scope = scope;
            Element = element;
        }

        public AudioObjectPropertyAddress(AudioObjectPropertySelectorExtended selector, AudioObjectPropertyScope scope, AudioObjectPropertyElement element)
        {
            Selector = (uint)selector;
            Scope = (uint)scope;
            Element = (uint)element;
        }
    }
}