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

namespace UnityAudioMIDIKit.Core.Mac.AudioObjects
{
    internal class AudioObjectService
    {
        #region Public methods

        public TOut GetAudioObjectPropertyDataFixed<TOut>(uint deviceObjectID, AudioObjectPropertySelectorExtended selector, AudioObjectPropertyScope scope, int size)
        {
            var propertyAddress = new AudioObjectPropertyAddress(selector, scope, AudioObjectPropertyElement.Master);
            var propInfo = GetUnmanagedPropertyData(deviceObjectID, propertyAddress, size);

            if (typeof(TOut) == typeof(int[]))
                return (TOut)MarshalToIntArray(propInfo);
            else if (typeof(TOut) == typeof(string))
                return (TOut)MarshalToString(propInfo);
            else
                throw new NotSupportedException("Type not yet handled.  Sorry this was built by a lazy engineer.");
        }


        public TOut GetAudioObjectPropertyDataDynamic<TOut>(uint deviceObjectID, AudioObjectPropertySelectorExtended selector, AudioObjectPropertyScope scope)
        {
            var propertyAddress = new AudioObjectPropertyAddress(selector, scope, AudioObjectPropertyElement.Master);
            uint inQualifierDataSize = 0;
            IntPtr inQualifierData = IntPtr.Zero; // aka NULL in obj-c

            uint dataPropertySize;

            var err = AudioObjectGetPropertyDataSize(deviceObjectID, ref propertyAddress, ref inQualifierDataSize, inQualifierData, out dataPropertySize);
            if (err != 0)
                throw new Exception(err.ToString());

            return GetAudioObjectPropertyDataFixed<TOut>(deviceObjectID, selector, scope, (int)dataPropertySize);
        }

        #endregion Public methods

        #region Private helpers

        private PropertyDataPointerInfo GetUnmanagedPropertyData(uint deviceObjectID, AudioObjectPropertyAddress propertyAddress, int size)
        {
            IntPtr resultPtr = Marshal.AllocHGlobal(size);
            uint inQualifierDataSize = 0;
            IntPtr inQualifierData = IntPtr.Zero; // aka NULL in obj-c
            uint dataPropertySize = (uint)size;

            var err = AudioObjectGetPropertyData(deviceObjectID, ref propertyAddress, ref inQualifierDataSize, inQualifierData, ref dataPropertySize, resultPtr);

            if (err != 0)
            {
                Marshal.FreeHGlobal(resultPtr);
                throw new Exception(String.Format("AudioUnit error '{0}'", err));
            }

            return new PropertyDataPointerInfo { Ptr = resultPtr, SizeInBytes = (int)dataPropertySize };
        }

        #endregion Private helpers

        #region Native library externs

        [DllImport(InteropHelper.AudioUnitLibrary)]
        private static extern int AudioObjectGetPropertyData(
            uint inObjectID,
            ref AudioObjectPropertyAddress inAddress,
            ref uint inQualifierDataSize,
            IntPtr inQualifierData,
            ref uint ioDataSize,
            IntPtr outDataPtr
        );

        [DllImport(InteropHelper.AudioUnitLibrary)]
        private static extern int AudioObjectGetPropertyDataSize(
            uint inObjectID,
            ref AudioObjectPropertyAddress inAddress,
            ref uint inQualifierDataSize,
            IntPtr inQualifierData,
            out uint outDataSize
        );

        #endregion Native library externs

        #region Marshalling
        private static object MarshalToString(PropertyDataPointerInfo propInfo)
        {
            string text = Marshal.PtrToStringAuto(propInfo.Ptr).ToString();
            Marshal.FreeHGlobal(propInfo.Ptr);
            return text.ToString();
        }

        private static IEnumerable<int> MarshalToIntArray(PropertyDataPointerInfo propInfo)
        {
            int numResults = propInfo.SizeInBytes / Marshal.SizeOf(typeof(int));
            int[] results = new int[numResults];
            Marshal.Copy(propInfo.Ptr, results, 0, numResults);
            Marshal.FreeHGlobal(propInfo.Ptr);

            return results;
        }
        #endregion Marshalling

        #region Private classes
        private class PropertyDataPointerInfo
        {
            public IntPtr Ptr { get; set; }
            public int SizeInBytes { get; set; }
        }
        #endregion Private classes

    }
}