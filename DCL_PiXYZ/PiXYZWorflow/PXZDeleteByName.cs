using System;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZDeleteByName : IPXZModifier
    {
        private string regex;

        public PXZDeleteByName(string regex)
        {
            this.regex = regex;
        }


        public OccurrenceList ApplyModification(PiXYZAPI pxz, OccurrenceList occurrenceList)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN PXZ DELETE BY NAME FOR REGEX " + regex);
            OccurrenceList occurenceToDelete = pxz.Scene.FindOccurrencesByProperty("Name", regex);
            pxz.Scene.DeleteOccurrences(occurenceToDelete);
            Console.WriteLine("END PXZ DELETE BY NAME FOR REGEX " + regex);
            return occurrenceList;
        }
    }
}