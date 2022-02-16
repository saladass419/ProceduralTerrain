Shader "Custom/Terrain"
{
    Properties
    {
        testTexture("Texture", 2D)="white"{}
        testScale("Scale",Float)=1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows

        #pragma target 3.0

        const static int maxLayerCount = 12;
        const static float epsilon = 1E-4;

        int layerCount;
        float3 baseColors[maxLayerCount];
        float baseHeights[maxLayerCount];
        float baseBlends[maxLayerCount];
        float baseColorStrengh[maxLayerCount];
        float baseTextureScales[maxLayerCount]; 

        float minHeight;
        float maxHeight;

        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        float InverseLerp(float min, float max, float value)
        {
            return saturate((value-min)/(max-min));
        }

        float3 triplanar (float3 worldPos, float scale, float3 blendAxis, int textureIdx){
            float3 scaledWorldPos = worldPos/scale;
            float3 xProjection=UNITY_SAMPLE_TEX2DARRAY(baseTextures,float3(scaledWorldPos.y,scaledWorldPos.z, textureIdx))*blendAxis.x;
            float3 yProjection=UNITY_SAMPLE_TEX2DARRAY(baseTextures,float3(scaledWorldPos.x,scaledWorldPos.z, textureIdx))*blendAxis.y;
            float3 zProjection=UNITY_SAMPLE_TEX2DARRAY(baseTextures,float3(scaledWorldPos.x,scaledWorldPos.y, textureIdx))*blendAxis.z;
            return xProjection+yProjection+zProjection;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 blendAxis = abs(IN.worldNormal);
            blendAxis/(blendAxis.x+blendAxis.y+blendAxis.z);
            float heightPercent=InverseLerp(minHeight,maxHeight,IN.worldPos.y);
            for(int i = 0; i < layerCount; i++)
            {
                float drawStrength = InverseLerp(-baseBlends[i]/2-epsilon, baseBlends[i]/2, heightPercent-baseHeights[i]);

                float3 baseColor = baseColors[i]*baseColorStrengh[i];
                float3 textureColor = triplanar(IN.worldPos,baseTextureScales[i],blendAxis,i)*(1-baseColorStrengh[i]);
                
                o.Albedo=o.Albedo * (1-drawStrength)+(baseColor+textureColor)*drawStrength;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}
