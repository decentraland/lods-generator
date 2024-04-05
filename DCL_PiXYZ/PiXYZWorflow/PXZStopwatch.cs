using System;
using System.Diagnostics;
using DCL_PiXYZ.Utils;

namespace DCL_PiXYZ
{
    public class PXZStopwatch
    {
        private Stopwatch stopwatch;
        
        public PXZStopwatch()
        {
            stopwatch = new Stopwatch();
        }

        public void Start() =>
            stopwatch.Restart();

        public void StopAndPrint(string pxzModifierName)
        {
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;

            // Format and display the TimeSpan value
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}",
                ts.Hours, ts.Minutes, ts.Seconds);

            FileWriter.WriteToConsole($"RunTime for {pxzModifierName} is " + elapsedTime);
        }
    }
}