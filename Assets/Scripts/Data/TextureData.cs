using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    public Layer[] layers;

    const int textureSize = 512;
    const TextureFormat format = TextureFormat.RGB565;

    float savedMinHeight;
    float savedMaxHeight;
    public void ApplyToMaterial (Material material)
    {
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColors", layers.Select(x=>x.tint).ToArray());
        material.SetFloatArray("baseHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColorStrengh", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);

        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }
    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(textureSize,textureSize,textures.Length,format,true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }
    [System.Serializable] public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0, 1)] public float tintStrength;
        [Range(0, 1)] public float startHeight;
        [Range(0, 1)] public float blendStrength;
        public float textureScale;
    }
}
