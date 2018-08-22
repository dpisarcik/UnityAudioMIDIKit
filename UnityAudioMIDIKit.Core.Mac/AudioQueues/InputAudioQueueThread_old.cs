using System;
using System.Threading;
using MonoMac.AudioToolbox;

namespace UnityAudioMIDIKit.Core.Mac.AudioQueues
{
    internal class InputAudioQueueThread
    {
        public AudioInputBuffer_old Buffer { get; private set; }
		public InputAudioQueue AudioQueue { get; set; }
		public Thread Thread { get; private set; }
        public bool IsRunning { get; set; }
		public bool StopRequested { get; private set; } = false;

		public InputAudioQueueThread(AudioInputBuffer_old buffer, Thread thread)
		{
            this.Buffer = buffer;
			this.Thread = thread;
		}

        public void RequestStop()
        {
            this.StopRequested = true;
        }
    }
}
