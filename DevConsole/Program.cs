using System;
using System.IO;
using System.Reflection;
using System.Threading;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using UnityAudioMIDIKit;
using MonoMac.CoreFoundation;
using System.Linq;
using UnityAudioMIDIKit.Core.Mac;
using MonoMac.AudioToolbox;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace DevConsole
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            OpenInputForDevice(); return;
            //TestRingBuffers();
            //writeConstsAsInts();
        }

        private static void writeConstsAsInts()
        {
            //AudioDevicePropertyIDs
            string[] array = new string[] {
                "BufferFrameSize = 'fsiz',",
                "JackIsConnected = 'jack',",
                "VolumeScalar = 'volm',",
                "VolumeDecibels = 'vold',",
                "VolumeRangeDecibels = 'vdb#',",
                "VolumeScalarToDecibels = 'v2db',",
                "VolumeDecibelsToScalar = 'db2v',",
                "StereoPan = 'span',",
                "StereoPanChannels = 'spn#',",
                "Mute = 'mute',",
                "Solo = 'solo',",
                "PhantomPower = 'phan',",
                "PhaseInvert = 'phsi',",
                "ClipLight = 'clip',",
                "Talkback = 'talb',",
                "Listenback = 'lsnb',",
                "DataSource = 'ssrc',",
                "DataSources = 'ssc#',",
                "DataSourceNameForIDCFString = 'lscn',",
                "DataSourceKindForID = 'ssck',",
                "ClockSource = 'csrc',",
                "ClockSources = 'csc#',",
                "ClockSourceNameForIDCFString = 'lcsn',",
                "ClockSourceKindForID = 'csck',",
                "PlayThru = 'thru',",
                "PlayThruSolo = 'thrs',",
                "PlayThruVolumeScalar = 'mvsc',",
                "PlayThruVolumeDecibels = 'mvdb',",
                "PlayThruVolumeRangeDecibels = 'mvd#',",
                "PlayThruVolumeScalarToDecibels = 'mv2d',",
                "PlayThruVolumeDecibelsToScalar = 'mv2s',",
                "PlayThruStereoPan = 'mspn',",
                "PlayThruStereoPanChannels = 'msp#',",
                "PlayThruDestination = 'mdds',",
                "PlayThruDestinations = 'mdd#',",
                "PlayThruDestinationNameForIDCFString = 'mddc',",
                "ChannelNominalLineLevel = 'nlvl',",
                "ChannelNominalLineLevels = 'nlv#',",
                "ChannelNominalLineLevelNameForIDCFString = 'lcnl',",
                "HighPassFilterSetting = 'hipf',",
                "HighPassFilterSettings = 'hip#',",
                "HighPassFilterSettingNameForIDCFString = 'hipl',",
                "SubVolumeScalar = 'svlm',",
                "SubVolumeDecibels = 'svld',",
                "SubVolumeRangeDecibels = 'svd#',",
                "SubVolumeScalarToDecibels = 'sv2d',",
                "SubVolumeDecibelsToScalar = 'sd2v',",
                "SubMute = 'smut'"
            };

            var enumValueSets = new List<EnumValueSet>();

            foreach (var definition in array)
            {

                var enumValueSet = new EnumValueSet
                {
                    VarName = definition.Substring(0, definition.IndexOf('=')).Trim(),
                    CharValue = definition.Substring(definition.IndexOf('\'') + 1, 4),
                };

                enumValueSet.EnumValue =
                    ((int)enumValueSet.CharValue[0] << 24) +
                    ((int)enumValueSet.CharValue[1] << 16) +
                    ((int)enumValueSet.CharValue[2] << 8) +
                    ((int)enumValueSet.CharValue[3]);

                enumValueSet.EnumValueString = String.Format("{0}", enumValueSet.EnumValue);

                enumValueSets.Add(enumValueSet);
            }

            int varNameLength = (from x in enumValueSets select x.VarName.Length).Max();
            int valueStringLength = (from x in enumValueSets select x.EnumValueString.Length).Max();

            foreach (var row in enumValueSets)
            {
                Console.WriteLine("{0} = {1}, // '{2}'", row.VarName.PadRight(varNameLength), row.EnumValueString.PadLeft(valueStringLength), row.CharValue);
            }

            Console.ReadKey();
        }

        private struct EnumValueSet
        {
            public string VarName;
            public string CharValue;
            public string EnumValueString;
            public int EnumValue;
        }

        private static void TestRingBuffers()
        {

            var rB = new RingBuffer();

            rB.Allocate(2, 4, 512);

            rB.Store(new AudioBuffers(2), 1, 0);
        }

        private static void OpenInputForDevice()
        {
            PrintInputDeviceList();
            var deviceID = GetDeviceIDFromUser();
            var device = AudioService.GetAudioInputDevice(deviceID);

            if (device == null)
            {
                Console.WriteLine("Invalid input device ID or error upon retrieving input device.");
                WaitForKey("Press any key to end...");
                return;
            }

            Console.WriteLine("===========================================");
            Console.WriteLine("OPENING INPUT BUFFER - PRESS ANY KEY TO END");
            Console.WriteLine("===========================================");

            var inputBuffer = AudioService.OpenInputBuffer(device);

            while (!Console.KeyAvailable)
            {
                Thread.Sleep(100);
                var retrievedSamples = inputBuffer.RetrieveBufferedSamples();
                Console.WriteLine("{0:T} - Retrieved {1} samples.  Peak = {2}", DateTime.Now, retrievedSamples.Length, retrievedSamples.Length > 0 ? retrievedSamples.Max() : 0);
            }

            AudioService.CloseInputBuffer(inputBuffer);

            Console.WriteLine();
            WaitForKey("Press any key to end...");

            return;

        }

        public static void PrintInputDeviceList()
        {
            var inputDevices = AudioService.GatherAudioInputDevices();

            foreach (var inputDevice in inputDevices)
                Console.WriteLine("{0}: {1}", inputDevice.DeviceID, inputDevice.Name);
        }

        public static int GetDeviceIDFromUser()
        {
            int deviceID = 0;
            while (deviceID == 0)
            {
                Console.Write("Please enter device ID: ");
                string deviceIDString = Console.ReadLine();
                if (!int.TryParse(deviceIDString, out deviceID))
                {
                    Console.WriteLine("Non-numeric value entered.  Please enter a number value.");
                    Console.WriteLine();
                }
            }

            return deviceID;
        }

        public static void WaitForKey(string message = "Press any key to continue...")
        {
            Console.WriteLine();
            Console.WriteLine(message);
            while (Console.ReadKey() == null)
            {
                Thread.Sleep(100);
            }
        }
    }
}
