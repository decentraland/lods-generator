﻿using System.Collections.Generic;
using DCL_PiXYZ.SceneRepositioner.JsonParsing;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public interface IPXZModifier
    {
        OccurrenceList ApplyModification(PiXYZAPI pxz, OccurrenceList origin);
    }
}