using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Experimental.Rendering;

internal class WorleyfBmGenerator : MonoBehaviour
{
    internal enum NoiseType
    {
        PerlinWorley,
        Worley,
        Perlin
    }

    static string NoiseTypeToKernelName(NoiseType noiseType)
    {
        switch (noiseType)
        {
            case NoiseType.PerlinWorley:
                return "PerlinWorleyNoiseEvaluator";
            case NoiseType.Worley:
                return "WorleyNoiseEvaluator";
            case NoiseType.Perlin:
                return "PerlinNoiseEvaluator";
        }
        return "";
    }

    static Texture2D GenerateWorleyfBm(int width, int height, int depth, NoiseType noiseType)
    {
        // Load our compute shader
        ComputeShader worleyCS = (ComputeShader)AssetDatabase.LoadAssetAtPath("Packages/com.unity.render-pipelines.high-definition/Editor/Lighting/VolumetricClouds/WorleyEvaluator.compute", typeof(ComputeShader));

        // Create our render texture
        RenderTexture rTexture0 = new RenderTexture(width, height, 1, GraphicsFormat.R8G8B8A8_UNorm);
        rTexture0.enableRandomWrite = true;
        rTexture0.useMipMap = false;
        rTexture0.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
        rTexture0.depth = 0;
        rTexture0.width = width;
        rTexture0.height = height;
        rTexture0.Create();

        // Fetch the target kernel
        int kernel = worleyCS.FindKernel(NoiseTypeToKernelName(noiseType));

        // Create the intermediate texture
        Texture2D tex2d = new Texture2D(width, height * depth, TextureFormat.R8, false);

        RenderTexture prevActive = RenderTexture.active;
        // Copy the layers one by one
        for (int i = 0; i < depth; ++i)
        {
            // Generate the current layer
            worleyCS.SetTexture(kernel, "_WorleyEvaluationOutput", rTexture0);
            worleyCS.SetInt("_Layer", i);
            worleyCS.SetInt("_NumLayers", depth);
            worleyCS.Dispatch(kernel, width / 8, height / 8, 1);

            // Copy the result into a tex2d then a tex3d
            RenderTexture.active = rTexture0;
            tex2d.ReadPixels(new Rect(0, 0, width, height), 0, height * i, false);
            tex2d.Apply();
        }

        // Restore the previous render texture
        RenderTexture.active = prevActive;

        // Release the RT
        rTexture0.Release();

        // Return the result
        return tex2d;
    }

    /*
    // This functions generates the set of textures required for the real-time volumetric cloud simulation
    [MenuItem("Generation/Generate Worley Textures")]
    static public void GenerateTextures()
    {
        Texture2D result = GenerateWorleyfBm(128, 128, 128, NoiseType.PerlinWorley);
        SaveTextureAsPNG(result, "Assets/WorleyNoise128RGBA.png");

        result = GenerateWorleyfBm(32, 32, 32, NoiseType.Worley);
        SaveTextureAsPNG(result, "Assets/WorleyNoise32RGB.png");

        result = GenerateWorleyfBm(32, 32, 32, NoiseType.Perlin);
        SaveTextureAsPNG(result, "Assets/PerlinNoise32RGB.png");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    */
    public static void SaveTextureAsPNG(Texture2D texture, string fullPath)
    {
        byte[] _bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(fullPath, _bytes);
        Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + fullPath);
    }
}
