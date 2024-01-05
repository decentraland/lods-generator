using System;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public static class PXZExtensions
    {
        public static void AddOccurrence(this OccurrenceList occurrenceList, uint newValue)
        {
            uint[] currentList = occurrenceList.list ?? Array.Empty<uint>();
            uint[] newList = new uint[currentList.Length + 1];
            currentList.CopyTo(newList, 0);
            newList[^1] = newValue;
            occurrenceList.list = newList;
        }
    }
}