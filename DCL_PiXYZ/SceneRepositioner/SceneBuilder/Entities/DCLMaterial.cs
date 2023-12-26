// unset:none

using System;
using System.Collections.Generic;
using DCL_PiXYZ.SceneRepositioner.JsonParsing.Parsers;
using Newtonsoft.Json;
using UnityEngine.Pixyz.API;
using UnityEngine.Pixyz.Core;
using UnityEngine.Pixyz.Material;

namespace DCL_PiXYZ.SceneRepositioner.SceneBuilder.Entities
{
    
    [JsonConverter(typeof(MaterialDataConverter))]
    [Serializable]
    public abstract class DCLMaterial
    {
        public TextureData texture;
        protected virtual ColorAlpha GetColor() => new ColorAlpha();

        public uint GetMaterial(PiXYZAPI pxz, string entityID, Dictionary<string, string> contentTable)
        {
            pxz.Material.CreateCustomMaterialPattern(entityID);
            uint material = pxz.Material.CreateMaterial("Material", entityID);
            pxz.Material.SetMaterialMainColor(material, GetColor());
            
            if (texture?.tex?.src != null)
            {
                if (contentTable.TryGetValue(texture.tex.src, out string texturePath))
                {
                    uint image = pxz.Material.ImportImage(texturePath);
                    
                    UnityEngine.Pixyz.Material.Texture albedoTexture = new UnityEngine.Pixyz.Material.Texture();
                    albedoTexture.image = image;
                    
                    ColorOrTexture colorOrTexture = new ColorOrTexture();
                    colorOrTexture.texture = albedoTexture;
                    
                    PBRMaterialInfos materialInfos = new PBRMaterialInfos();
                    materialInfos.albedo = colorOrTexture;
                    
                    pxz.Material.SetPBRMaterialInfos(material, materialInfos);

                }
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

        protected override ColorAlpha GetColor()
        {
            ColorAlpha colorAlpha = new ColorAlpha();
            colorAlpha.r = albedoColor.r;
            colorAlpha.g = albedoColor.g;
            colorAlpha.b = albedoColor.b;
            colorAlpha.a = colorAlpha.a;
            return colorAlpha;
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
        public int a = 1;
    }
    
}
