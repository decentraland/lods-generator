using System;
using System.IO;

namespace DCL_PiXYZ.Utils
{
    public class FileWriter
    {
        public static void WriteToFile(string message, string fileName)
        {
            using (StreamWriter file = new StreamWriter(fileName, true))
                file.WriteLine(message);
        }
        
        public static void WriteToConsole(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"[{timestamp}] {message}");
        }
    }
}