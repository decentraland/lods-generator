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
        public int transparencyMode;
        public int alphaTest;
        protected virtual Color GetColor()
        {
            Color colorAlpha = new Color();
            colorAlpha.r = 1;
            colorAlpha.g = 1;
            colorAlpha.b = 1;
            return colorAlpha;
        }

        protected virtual double GetAlpha()
        {
            return 1;
        }

        public uint GetMaterial(PiXYZAPI pxz, string entityID, Dictionary<string, string> contentTable)
        {
            uint material = pxz.Material.CreateMaterial($"{PXZConstants.CUSTOM_MATERIAL_CONVERTED}_{entityID}" , "PBR");
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
                    
                    //NOTE: We used the transparency mode to determine if this object should be transparent or not
                    if (transparencyMode.Equals(2))
                        pxz.Core.SetProperty(material, "Name", $"{pxz.Core.GetProperty(material, "Name")}_FORCED_TRANSPARENT");
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
                //TODO (Juani): Should this be SetMaterialMainColor?
                pxz.Material.SetPBRMaterialInfos(material, materialInfos);
            }
            return material;
        }
        
        public bool IsFullyTransparent()
        {
            return texture?.tex?.src == null && GetAlpha().Equals(0);
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

        protected override double GetAlpha()
        {
            if (albedoColor.a == null)
                return 1;
            return albedoColor.a.Value;
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
        public double r = 1;
        public double g = 1;
        public double b = 1;
        public double? a = 1;
    }
    
}
