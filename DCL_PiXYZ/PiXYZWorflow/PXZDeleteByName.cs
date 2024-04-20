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

        public void ApplyModification(PiXYZAPI pxz)
        {
            var occurenceToDelete = pxz.Scene.FindOccurrencesByProperty("Name", regex, caseInsensitive: true);
            foreach (uint u in occurenceToDelete.list)
                pxz.Scene.DeleteComponentByType(ComponentType.Part, u);
        }
    }
}