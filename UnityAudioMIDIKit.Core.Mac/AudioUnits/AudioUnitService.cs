using System;
using System.Runtime.InteropServices;
using MonoMac.AudioUnit;
using System.Collections.Generic;
using System.ComponentModel;
using System.CodeDom;
using System.IO;
using System.Drawing;
using System.Runtime.CompilerServices;
using MonoMac.Foundation;
using MonoMac.CoreFoundation;
using UnityAudioMIDIKit.Core.Mac.AudioDevices;

namespace UnityAudioMIDIKit.Core.Mac.AudioUnits
{
    internal static class AudioUnitService
    {
        #region Public methods

        [DllImport(InteropHelper.AudioUnitLibrary, EntryPoint = "AudioUnitGetProperty")]
        private static extern AudioUnitStatus GetProperty(
            IntPtr inUnit, 
            AudioUnitPropertyIDType inID,
            AudioUnitScopeType inScope, 
            uint inBusElement, 
            out uint outData, 
            ref int outDataSize
        );

        [DllImport(InteropHelper.AudioUnitLibrary, EntryPoint = "AudioUnitGetProperty")]
        public static extern AudioUnitStatus GetProperty(
            IntPtr inUnit, 
            AudioDevicePropertyID inID,
            AudioUnitScopeType inScope, 
            uint inBusElement, 
            out uint outData, 
            ref int outDataSize
        );

		#endregion Public methods
    }
}