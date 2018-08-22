using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace UnityAudioMIDIKit.Core.Mac
{
    internal static class InteropHelper
    {
        public const string AudioUnitLibrary = "/System/Library/Frameworks/AudioUnit.framework/AudioUnit";
        private const int ZERO_BYTE_ARRAY_SIZE = 1024 * 1024;

		private static readonly byte[] _zeroBytes = new byte[ZERO_BYTE_ARRAY_SIZE];

        public static void FillWithZeroes(IntPtr ptr, int numBytesToZero)
		{
			while(numBytesToZero > 0)
			{
                int numBytesToCopy = numBytesToZero > ZERO_BYTE_ARRAY_SIZE ? ZERO_BYTE_ARRAY_SIZE : numBytesToZero;
				Marshal.Copy(_zeroBytes, 0, ptr, numBytesToCopy);

				ptr += numBytesToCopy;
				numBytesToZero -= numBytesToCopy;
			}
		}

        [DllImport("libSystem.dylib", EntryPoint = "memcpy")]
        public static extern IntPtr MemCpy(IntPtr dest, IntPtr src, int countBytes);

        [DllImport("libSystem.dylib", EntryPoint = "OSAtomicCompareAndSwap32Barrier")]
        public static extern bool OSAtomicCompareAndSwap32Barrier(int oldValue, int newValue, ref int theValue);
    }
}
