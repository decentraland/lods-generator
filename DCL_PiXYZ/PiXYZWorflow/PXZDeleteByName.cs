using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
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

        public async Task ApplyModification(PiXYZAPI pxz)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN PXZ DELETE BY NAME FOR REGEX " + regex);
            OccurrenceList occurenceToDelete = pxz.Scene.FindOccurrencesByProperty("Name", regex);
            pxz.Scene.DeleteOccurrences(occurenceToDelete);
            Console.WriteLine("END PXZ DELETE BY NAME FOR REGEX " + regex);
            Console.WriteLine("-------------------------");
        }
    }
}