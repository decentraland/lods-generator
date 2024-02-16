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
    }
}