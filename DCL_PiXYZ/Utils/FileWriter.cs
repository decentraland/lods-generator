using System;
using System.IO;

namespace DCL_PiXYZ.Utils
{
    public class FileWriter
    {
        public static string currentScene;
        
        public static void WriteToFile(string message, string fileName)
        {
            using (StreamWriter file = new StreamWriter(fileName, true))
                file.WriteLine(message);
        }
        
        public static void WriteToConsole(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"[{timestamp}] {currentScene} {message}");
        }
        
        
        public static void PrintDriveSize(string message)
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                if(d.IsReady)
                    WriteToConsole($"{message} Drive {d.Name} - Total: {ConvertToGB(d.TotalSize)} GB, Available: {ConvertToGB(d.AvailableFreeSpace)} GB, Free: {ConvertToGB(d.TotalFreeSpace)} GB");
            }
        }

        private static double ConvertToGB(long bytes)
        {
            return Math.Round(bytes / (double)(1024 * 1024 * 1024), 2);
        }

        
        
    }
}