// unset:none

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DCL_PiXYZ.SceneRepositioner.JsonParsing.Parsers;
using Newtonsoft.Json;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Core;
using UnityEngine.Pixyz.Geom;
using UnityEngine.Pixyz.Material;

namespace DCL_PiXYZ.SceneRepositioner.SceneBuilder.Entities
{
    
    [JsonConverter(typeof(MaterialDataConverter))]
    [Serializable]
    public abstract class DCLMaterial
    {
        public TextureData texture;
        protected virtual Color GetColor()
        {
            Color colorAlpha = new Color();
            colorAlpha.r = 1;
            colorAlpha.g = 1;
            colorAlpha.b = 1;
            return colorAlpha;
        }

        public uint GetMaterial(PiXYZAPI pxz, string entityID, Dictionary<string, string> contentTable)
        {
            uint material = pxz.Material.CreateMaterial($"{PXYZConstants.CUSTOM_MATERIAL_CONVERTED}_{entityID}" , "PBR");
            ColorOrTexture albedoColorOrTexture = new ColorOrTexture();
            if (texture?.tex?.src != null)
            {
                if (contentTable.TryGetValue(texture.tex.src, out string texturePath))
                {
                    uint image = pxz.Material.ImportImage(texturePath);
            
                    UnityEngine.Pixyz.Material.Texture albedoTexture = new UnityEngine.Pixyz.Material.Texture();
                    Point2 point = new Point2();
                    point.x = 1;
                    point.y = 1;
                    albedoTexture.tilling = point;
                    albedoTexture.image = image;
            
                    albedoColorOrTexture._type = ColorOrTexture.Type.TEXTURE;
                    albedoColorOrTexture.texture = albedoTexture;
                    PBRMaterialInfos materialInfos = pxz.Material.GetPBRMaterialInfos(material);
                    materialInfos.albedo = albedoColorOrTexture;
                    pxz.Material.SetPBRMaterialInfos(material, materialInfos);
                }
                else
                {
                    //TODO: Download dynamic textures
                }
            }
            else
            {
                albedoColorOrTexture._type = ColorOrTexture.Type.COLOR;
                albedoColorOrTexture.color = GetColor();
                PBRMaterialInfos materialInfos = pxz.Material.GetPBRMaterialInfos(material);
                materialInfos.albedo = albedoColorOrTexture;
                pxz.Material.SetPBRMaterialInfos(material, materialInfos);
            }
            return material;
        }

    }
    
    [Serializable]
    public class EmptyMaterial : DCLMaterial
    {
        
    }

    [Serializable]
    public class UnlitMaterial : DCLMaterial
    {
    }

    [Serializable]
    public class PBRMaterial : DCLMaterial
    {
        public AlbedoColor albedoColor = new AlbedoColor ();

        protected override Color GetColor()
        {
            Color color = new Color();
            color.r = albedoColor.r;
            color.g = albedoColor.g;
            color.b = albedoColor.b;
            return color;
        }
    }

    [Serializable]
    public class TextureData
    {
        public Texture tex;
    }

    [JsonConverter(typeof(TextureDataConverter))]
    [Serializable]
    public class Texture
    {
        public string src;
    }

    [Serializable]
    public class AlbedoColor
    {
        public int r = 1;
        public int g = 1;
        public int b = 1;
        public int? a = 1;
    }
    
}
