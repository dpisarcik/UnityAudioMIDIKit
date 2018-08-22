using System;
using MonoMac.AudioToolbox;
using System.Diagnostics;
using MonoMac.AudioUnit;

namespace UnityAudioMIDIKit.Core.Mac
{
    public static class ErrorHandler
    {
        public enum ErrorReportMode 
		{
            ThrowException,
            Console,
            DebugLog,
            Silent
        }

		public static ErrorReportMode ReportMode { get; set; }

		public static AudioQueueStatus CheckError(AudioQueueStatus status)
		{
			if(status != AudioQueueStatus.Ok)
				ReportError(status);
            return status;
		}

        public static AudioUnitStatus CheckError(AudioUnitStatus status)
        {
            if (status != AudioUnitStatus.OK)
                ReportError(status);
            return status;
        }

        public static int CheckError(int result)
        {
            if (result != 0)
                ReportError(result);
            return result;
        }

		private static void ReportError(object status)
		{
            if (ReportMode == ErrorReportMode.Silent)
                return;

            string errorText = status.GetType().Name + " did not return OK.  Error: " + status.ToString();

            switch (ReportMode)
            {
                case ErrorReportMode.Console:
                    Console.WriteLine(errorText);
                    break;

                case ErrorReportMode.DebugLog:
                    Debug.WriteLine(errorText);
                    break;

                case ErrorReportMode.ThrowException:
                    throw new Exception(errorText);

                default:
                    throw new Exception("ErrorReportMode not handled.");
            }

            return;
		}

        internal static void CheckError(Action action, bool rethrowOnException = true)
        {
            try
            {
                action();
            }
            catch(Exception ex)
            {
                ReportError(ex.GetType().Name + ": " + ex.Message);
                if (rethrowOnException)
                    throw ex;
            }
        }
    }
}
