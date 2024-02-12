using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Pixyz.Algo;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Geom;
using UnityEngine.Pixyz.Material;
using UnityEngine.Pixyz.Scene;

namespace DCL_PiXYZ
{
    public class PXZDecimateAndBake : IPXZModifier
    {
        public async Task ApplyModification(PiXYZAPI pxz)
        {
            Console.WriteLine("-------------------------");
            Console.WriteLine("BEGIN PXZ DECIMATE AND BAKE");
            bool OVERRIDE_UVS = true;
            int TEXTURE_RESOLUTION = 1024;
            uint TEXTURE_PADDING = 1;

            BakeMap bakeMap = new BakeMap();
            bakeMap.type = MapType.Diffuse;
            BakeMapList bakeMaps = new BakeMapList(new BakeMap[]{bakeMap});
            
            OccurrenceList rootOccurenceList =  new OccurrenceList();
            rootOccurenceList.AddOccurrence(pxz.Scene.GetRoot());
            pxz.Algo.CreateNormals(rootOccurenceList, -1, false, true);

            OccurrenceList lowOccs = new OccurrenceList();
            OccurrenceList highOccs = new OccurrenceList();

            //DUPLICATE MESHES
            foreach (var lowOcc in pxz.Scene.GetPartOccurrences(rootOccurenceList[0]).list)
            {
               OccurrenceList clonedEntity = new OccurrenceList();
               clonedEntity.AddOccurrence(pxz.Core.CloneEntity(lowOcc));
               pxz.Scene.MakeInstanceUnique(clonedEntity);
               pxz.Scene.SetParent(clonedEntity[0], pxz.Scene.GetParent(lowOcc));
               lowOccs.AddOccurrence(lowOcc);
               highOccs.AddOccurrence(clonedEntity[0]);
            }
            //pxz.Algo.CreateTangents(highOccs,0,true);

            //REPAIR AND DECIMATE WITHOUT UVS
            //pxz.Algo.RemoveUV(lowOccs);
            //pxz.Algo.RepairMesh(lowOccs, 0.1, true, false);
            //DecimateOptionsSelector decimateOptionsSelector = new DecimateOptionsSelector();
            //decimateOptionsSelector._type = DecimateOptionsSelector.Type.RATIO;
            //decimateOptionsSelector.ratio = 100;
            //pxz.Algo.DecimateTarget(lowOccs, decimateOptionsSelector,0,true);
            
            //READD UV INFO
            pxz.Algo.MapUvOnAABB(lowOccs, false, 0.1, 0);
            pxz.Algo.RepackUV(lowOccs, 0, true);
            pxz.Algo.NormalizeUV(lowOccs, 0, -1, true, true);
            
            //for (var i = 0; i < lowOccs.list.Length; i++)
            //    pxz.Algo.BakeUV(highOccs[i], lowOccs[i]);
            
            //BAKE MAP
            ImageList images = pxz.Algo.BakeMaps(lowOccs, highOccs, bakeMaps, oneToOne:true);

            //MERGE MESHES
            lowOccs = pxz.Scene.MergePartOccurrences(lowOccs, 0);
            
            uint material = pxz.Material.CreateMaterial("Baked" , "PBR");
            PBRMaterialInfos materialInfos = pxz.Material.GetPBRMaterialInfos(material);
            Texture albedoTexture = new Texture();
            Point2 point = new Point2();
            point.x = 1;
            point.y = 1;
            albedoTexture.tilling = point;
            albedoTexture.image = images[0];
            ColorOrTexture albedoColorOrTexture = new ColorOrTexture();
            albedoColorOrTexture._type = ColorOrTexture.Type.TEXTURE;
            albedoColorOrTexture.texture = albedoTexture;
            materialInfos.albedo = albedoColorOrTexture;
            pxz.Material.SetPBRMaterialInfos(material, materialInfos);
            pxz.Scene.SetOccurrenceMaterial(lowOccs[0],material);

            //DELETE MESHES
            pxz.Scene.DeleteOccurrences(highOccs);
            
            //???
            pxz.Algo.DeleteLines(lowOccs);
            pxz.Algo.DeleteFreeVertices(lowOccs);
            //pxz.Algo.DeletePatches(lowOccs, false);
            /*foreach (var occurence in lowOccs.list)
            {
                pxz.Core.SetProperty(occurence, "Name", "DecimateTargetBake " + pxz.Core.GetProperty(occurence, "Id"));
            }*/
            Console.WriteLine("END PXZ DECIMATE AND BAKE");
        }
    }
}