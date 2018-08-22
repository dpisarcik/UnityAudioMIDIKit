using System;
using System.Collections.Generic;
using System.Threading;
using MonoMac.AudioToolbox;
using MonoMac.CoreFoundation;
using MonoMac.Foundation;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using MonoMac.AudioUnit;
using System.Runtime.InteropServices;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;
using MonoMac.OpenGL;

namespace UnityAudioMIDIKit.Core.Mac.AudioQueues
{
    internal class TestInputAudioQueue : InputAudioQueue
    {
        protected override void OnInputCompleted(IntPtr audioQueueBuffer, AudioTimeStamp timeStamp, AudioStreamPacketDescription[] packetDescriptions)
        {
            throw new NotImplementedException();
        }

        public TestInputAudioQueue(AudioStreamBasicDescription desc) : base(desc)
        {
        }

        public TestInputAudioQueue(AudioStreamBasicDescription desc, CFRunLoop runLoop, string runMode) : base(desc, runLoop, runMode)
        {
        }
    }

    internal static class AudioQueueProcessingService
    {
        private static Dictionary<AudioInputBuffer_old, InputAudioQueueThread_old> BufferThreads { get; set; } = new Dictionary<AudioInputBuffer_old, InputAudioQueueThread_old>();

        public static void ListenForInput(AudioInputBuffer_old buffer)
        {
            Thread thread = null;
            var audioQueueThread = new InputAudioQueueThread_old(buffer, thread);

            thread = new Thread(new ParameterizedThreadStart(HandleParameterizedThreadStart));
            BufferThreads.Add(buffer, audioQueueThread);

            thread.Start(audioQueueThread);
        }

        private static int DELETEME = 0;
        static void Queue_InputCompleted(object sender, InputCompletedEventArgs e)
        {
            DELETEME++;
            if (DELETEME <= 1000)
                Console.WriteLine("Input Completed!");
        }


        public static void StopListening(AudioInputBuffer_old buffer)
        {
            InputAudioQueueThread_old audioQueueThread = null;
            if (BufferThreads.ContainsKey(buffer))
                audioQueueThread = BufferThreads[buffer];

            if (audioQueueThread != null && audioQueueThread.IsRunning)
            {
                audioQueueThread.RequestStop();
                audioQueueThread.IsRunning = false;
                BufferThreads.Remove(buffer);
            }
        }



        private static void HandleParameterizedThreadStart(object audioQueueThread)
        {
            //RunAudioQueueLoop(audioQueueThread as InputAudioQueueThread);
        }

        //private static void HandleParameterizedThreadStart(object getAudioQueueThreadDelegate)
        //{
        //    var getDelegate = getAudioQueueThreadDelegate as GetAudioQueueThreadDelegate;
        //    RunAudioQueueLoop(getDelegate());
        //}


        private static void RunAudioQueueLoop(InputAudioQueueThread_old audioQueueThread)
        {
            var buffer = audioQueueThread.Buffer;
            //var queue = new TestInputAudioQueue(buffer.AudioUnit.GetAudioFormat(MonoMac.AudioUnit.AudioUnitScopeType.Input));
            //audioQueueThread.AudioQueue = queue;
            //queue.InputCompleted += Queue_InputCompleted;
#if DEBUG
            //PrintAudioQueueDataReadout(queue);
#endif //DEBUG

            //var packetDescription = new AudioStreamPacketDescription
            //{
            //    DataByteSize = queue.AudioStreamDescription.BytesPerPacket
            //};
            //var packetDescriptions = new AudioStreamPacketDescription[] { packetDescription };
            //int bufferSizeInBytes = buffer.BufferLength * queue.DeviceChannels * packetDescription.DataByteSize;
            int bufferSizeInBytes = buffer.AudioUnit.GetAudioFormat(AudioUnitScopeType.Input).FramesPerPacket * sizeof(float);

            //var bufferstuff = new AudioBufferList(buffer.AudioUnit.GetAudioFormat(AudioUnitScopeType.Input).ChannelsPerFrame);
            var bufferstuff = new AudioBuffers(buffer.AudioUnit.GetAudioFormat(AudioUnitScopeType.Input).ChannelsPerFrame);

            int something = buffer.AudioUnit.Initialize();
            audioQueueThread.Buffer.AudioUnit.Start();

            // Allocate, enqueue, and start
            //ErrorHandler.CheckError(queue.AllocateBuffer(bufferSizeInBytes, out buffer.SystemBufferPointer));
            //ErrorHandler.CheckError(queue.EnqueueBuffer(buffer.SystemBufferPointer, packetDescriptions));
            //ErrorHandler.CheckError(queue.Start());

            audioQueueThread.IsRunning = true;
            //CFRunLoop.Current.Run();
            while (!audioQueueThread.StopRequested)
            {
                //Console.WriteLine("Thread is running.");
                Thread.Sleep(50);
                CFRunLoop.Current.Run();                
                CFRunLoop.Main.Run();
                //Marshal.Copy(buffer.SystemBufferPointer, dest, 0, audioQueueThread.Buffer.BufferLength);
                //ErrorHandler.CheckError(queue.EnqueueBuffer(buffer.SystemBufferPointer, packetDescriptions));
            }

            Console.WriteLine("Thread is stopping...");
            // May need to clear CoreAudio buffer here.
        }

        private static void PrintAudioQueueDataReadout(InputAudioQueue queue)
        {
            var props = typeof(InputAudioQueue).GetProperties(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Instance).ToList();
            foreach (var prop in props.OrderBy(x => x.Name))
                PrintPropertyInfo(queue, prop);
        }

        private static void PrintPropertyInfo(object obj, PropertyInfo propInfo)
        {
            try
            {
                var value = propInfo.GetGetMethod(false).Invoke(obj, null);
                Console.WriteLine("{0}: {1}", propInfo.Name, value.ToString());
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    Console.WriteLine("{0}: {1}", propInfo.Name, ex.InnerException.Message);
                else
                    throw ex;

            }
        }
    }
}
