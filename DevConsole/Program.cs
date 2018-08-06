﻿using System;
using UnityAudioMIDIKit;

namespace DevConsole
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var inputDevices = AudioService.GatherAudioInputDevices();

            foreach (var device in inputDevices)
                Console.WriteLine("{0}: {1}", device.DeviceID, device.Name);

            //(new Sandbox()).Run(); //Use as hack to exercise internal functionality that will not be exposed by the Platform layer

            Console.WriteLine();
            Console.WriteLine("Press any key to end...");
            while (Console.ReadKey() == null)
            { }
        }
    }
}
