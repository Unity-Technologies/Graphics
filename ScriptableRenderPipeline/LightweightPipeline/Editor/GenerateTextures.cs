using System.IO;
using UnityEngine;
using UnityEditor;

public class GenerateTextures : MonoBehaviour
{
    private static readonly int kTextureSize = 1024;
    private static readonly float kQuadraticFac = 25.0f;
    private static readonly float kToZeroFadeStart = 0.8f * 0.8f;
    private static readonly string kSavePath = "Assets/ScriptableRenderPipeline/LightweightPipeline/Textures/LightweightLightAttenuation.png";

    [MenuItem("RenderPipeline/LightweightPipeline/GenerateLightFalloffTexture")]
    public static void GenerateLightFalloffTexture()
    {
        Texture2D tex = new Texture2D(kTextureSize, 4, TextureFormat.Alpha8, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;

        byte[] bytes = new byte[kTextureSize * 4];
        for (int x = 0; x < kTextureSize; ++x)
        {
            float sqrRange = (float) x/(float) kTextureSize;
            byte atten = LightAttenuationNormalized(sqrRange);
            bytes[x] = atten;
            bytes[x + kTextureSize] = atten;
            bytes[x + kTextureSize * 2] = atten;
            bytes[x + kTextureSize * 3] = atten;
        }

        tex.LoadRawTextureData(bytes);
        tex.Apply(false);
        SaveTexture(tex);
    }

    public static void SaveTexture(Texture2D tex)
    {
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(kSavePath, bytes);
    }

    public static byte LightAttenuationNormalized(float distSqr)
    {
        // 1 / 1.0 + quadAtten * distSqr attenuation function
        float atten = 1.0f / (1.0f + CalculateLightQuadFac(1.0f) * distSqr);

        // however the above does not falloff to zero at light range.
        // Start fading from ktoZeroFadeStart to light range
        float fadeMultiplier = Mathf.Clamp01((distSqr - 1.0f) / (kToZeroFadeStart - 1.0f));
        atten *= fadeMultiplier;

        //return atten;
        return (byte)Mathf.RoundToInt(atten * (float)byte.MaxValue);
    }

    public static float CalculateLightQuadFac(float range)
    {
        return kQuadraticFac / (range * range);
    }
}
