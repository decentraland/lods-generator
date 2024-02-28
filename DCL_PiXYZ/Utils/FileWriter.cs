using System.IO;

namespace DCL_PiXYZ.Utils
{
    public class FileWriter
    {
        public static void WriteToFile(string message, string fileName, bool append = true)
        {
            using (var file = new StreamWriter(fileName, append))
                file.WriteLine(message);
        }
    }
}