using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(ProceduralTexture2D)), CanEditMultipleObjects]
public class ProceduralTexture2DEditor : Editor
{
    ProceduralTexture2D[] targetAssets;

    public void OnEnable()
    {
        Object[] monoObjects = targets;
        targetAssets = new ProceduralTexture2D[monoObjects.Length];
        for (int i = 0; i < monoObjects.Length; i++)
        {
            targetAssets[i] = monoObjects[i] as ProceduralTexture2D;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Input Texture
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("input"), new GUIContent("Texture"));
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            CopyInputTextureImportType(targetAssets[0]);
        }
        GUILayout.Space(10);

        // Texture Type
        EditorGUILayout.PropertyField(serializedObject.FindProperty("type"), new GUIContent("Texture Type"));

        // Include alpha for color textures
        if ((ProceduralTexture2D.TextureType)serializedObject.FindProperty("type").enumValueIndex == ProceduralTexture2D.TextureType.Color)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("includeAlpha"), new GUIContent("Include Alpha"));
        GUILayout.Space(10);

        // Filtering
        EditorGUILayout.PropertyField(serializedObject.FindProperty("generateMipMaps"), new GUIContent("Generate Mip Maps"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("filterMode"), new GUIContent("Filter mode"));
        Rect sliderRect = GUILayoutUtility.GetLastRect();
        if (serializedObject.FindProperty("generateMipMaps").boolValue == true)
        {
            sliderRect = new Rect(sliderRect.x, sliderRect.y + sliderRect.height, sliderRect.width, sliderRect.height);
            PropertyIntSlider(sliderRect, serializedObject.FindProperty("anisoLevel"), 0, 16, new GUIContent("Aniso Level"));
            GUILayoutUtility.GetRect(new GUIContent("Aniso Level"), EditorStyles.label);
        }
        GUILayout.Space(10);

        // Compression
        EditorGUILayout.PropertyField(serializedObject.FindProperty("compressionQuality"), new GUIContent("Compression"));

        // Memory size display
        string size = targetAssets.Length == 1 && targetAssets[0].memoryUsageBytes > 0 ?
            SizeSuffix(targetAssets[0].memoryUsageBytes) : "--";
        EditorGUILayout.LabelField("Size in memory: " + size, EditorStyles.centeredGreyMiniLabel);
        GUILayout.Space(10);

        // Apply changes button
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Apply"))
            for (int i = 0; i < targetAssets.Length; i++)
                PreprocessData(targetAssets[i]);
        GUILayout.EndHorizontal();

        // Normal compression warning
        if (targetAssets[0].type == ProceduralTexture2D.TextureType.Normal && targetAssets[0].compressionQuality != ProceduralTexture2D.CompressionLevel.HighQuality)
            EditorGUILayout.HelpBox("High quality compression recommended for normal maps", MessageType.Info);

        // Unapplied changes warning
        if (UnappliedSettingChanges(targetAssets[0]) == true)
            EditorGUILayout.HelpBox("Unapplied settings", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }

    // A slider function that takes a SerializedProperty
    void PropertyIntSlider(Rect position, SerializedProperty property, int leftValue, int rightValue, GUIContent label)
    {
        label = EditorGUI.BeginProperty(position, label, property);

        EditorGUI.BeginChangeCheck();
        var newValue = EditorGUI.IntSlider(position, label, property.intValue, leftValue, rightValue);
        if (EditorGUI.EndChangeCheck())
            property.intValue = newValue;
        EditorGUI.EndProperty();
    }

    private readonly string[] SizeSuffixes =
        { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    private string SizeSuffix(long value, int decimalPlaces = 1)
    {
        if (value < 0) { return "-" + SizeSuffix(-value); }
        if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

        // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
        int mag = (int)Mathf.Log(value, 1024);

        // 1L << (mag * 10) == 2 ^ (10 * mag) 
        // [i.e. the number of bytes in the unit corresponding to mag]
        decimal adjustedSize = (decimal)value / (1L << (mag * 10));

        // make adjustment when the value is large enough that
        // it would round up to 1000 or more
        if (System.Math.Round(adjustedSize, decimalPlaces) >= 1000)
        {
            mag += 1;
            adjustedSize /= 1024;
        }

        return string.Format("{0:n" + decimalPlaces + "} {1}",
            adjustedSize,
            SizeSuffixes[mag]);
    }

    private void CopyInputTextureImportType(ProceduralTexture2D target)
    {
        string path = AssetDatabase.GetAssetPath(target.input);
        TextureImporter inputImporter = (TextureImporter)TextureImporter.GetAtPath(path);
        switch (inputImporter.textureType)
        {
            case TextureImporterType.NormalMap:
                target.type = ProceduralTexture2D.TextureType.Normal;
                break;
            default:
                target.type = ProceduralTexture2D.TextureType.Color;
                break;
        }
    }

    private bool UnappliedSettingChanges(ProceduralTexture2D target)
    {
        if(target.currentInput != target.input
            || target.currentIncludeAlpha != target.includeAlpha
            || target.currentGenerateMipMaps != target.generateMipMaps
            || target.currentFilterMode != target.filterMode
            || target.currentAnisoLevel != target.anisoLevel
            || target.currentCompressionQuality != target.compressionQuality)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    /*********************************************************************/
    /*********************************************************************/
    /*************Procedural Stochastic Texturing Pre-process*************/
    /*********************************************************************/
    /*********************************************************************/
    const float GAUSSIAN_AVERAGE = 0.5f;    // Expectation of the Gaussian distribution
    const float GAUSSIAN_STD = 0.1666f;     // Std of the Gaussian distribution
    const int LUT_WIDTH = 128;              // Size of the look-up table
    private static int stepCounter = 0;
    private static int totalSteps = 0;

    struct TextureData
    {
        public Color[] data;
        public int width;
        public int height;

        public TextureData(int w, int h)
        {
            width = w;
            height = h;
            data = new Color[w * h];
        }
        public TextureData(TextureData td)
        {
            width = td.width;
            height = td.height;
            data = new Color[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    data[y * width + x] = td.data[y * width + x];
        }

        public Color GetColor(int w, int h)
        {
            return data[h * width + w];
        }
        public ref Color GetColorRef(int w, int h)
        {
            return ref data[h * width + w];
        }
        public void SetColorAt(int w, int h, Color value)
        {
            data[h * width + w] = value;
        }
    };

    private static void PreprocessData(ProceduralTexture2D target)
    {
        if (target.input == null)
            return;

        // Init progress bar
        stepCounter = 0;
        totalSteps = (target.type != ProceduralTexture2D.TextureType.Other ? 4 : 0) + (target.type != ProceduralTexture2D.TextureType.Other ? 9 : 12) + 1;
        EditorUtility.DisplayProgressBar("Pre-processing Procedural Texture Data", target.name, (float)stepCounter / (totalSteps - 1));

        // Section 1.4 Improvement: using a decorrelated color space for Color RGB and Normal XYZ textures
        TextureFormat inputFormat = TextureFormat.RGB24;
        TextureData albedoData = TextureToTextureData(target.input, ref inputFormat);
        TextureData decorrelated = new TextureData(albedoData);
        if (target.type != ProceduralTexture2D.TextureType.Other)
            DecorrelateColorSpace(ref albedoData, ref decorrelated, ref target.colorSpaceVector1, ref target.colorSpaceVector2, ref target.colorSpaceVector3, ref target.colorSpaceOrigin, target.name);
        ComputeCompressionScalers(target);

        // Perform precomputations
        TextureData Tinput = new TextureData(decorrelated.width, decorrelated.height);
        TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels

        List<int> channelsToProcess = new List<int> { 0, 1, 2 };
        if ((target.type == ProceduralTexture2D.TextureType.Color && target.includeAlpha == true) || target.type == ProceduralTexture2D.TextureType.Other)
            channelsToProcess.Add(3);
        Precomputations(ref decorrelated, channelsToProcess, ref Tinput, ref invT, target.name);

        RescaleForCompression(target, ref Tinput);
        EditorUtility.DisplayProgressBar("Pre-processing Procedural Texture Data", target.name, (float)stepCounter++ / (totalSteps - 1));

        // Serialize precomputed data and setup material
        FinalizePrecomputedTextures(ref inputFormat, target, ref Tinput, ref invT);

        target.memoryUsageBytes = target.Tinput.GetRawTextureData().Length + target.invT.GetRawTextureData().Length;

        EditorUtility.ClearProgressBar();

        // Update current applied settings
        target.currentInput = target.input;
        target.currentIncludeAlpha = target.includeAlpha;
        target.currentGenerateMipMaps = target.generateMipMaps;
        target.currentFilterMode = target.filterMode;
        target.currentAnisoLevel = target.anisoLevel;
        target.currentCompressionQuality = target.compressionQuality;
    }

    static TextureData TextureToTextureData(Texture2D input, ref TextureFormat inputFormat)
    {
        // Modify input texture import settings temporarily
        string texpath = AssetDatabase.GetAssetPath(input);
        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(texpath);
        TextureImporterCompression prev = importer.textureCompression;
        TextureImporterType prevType = importer.textureType;
        bool linearInput = importer.sRGBTexture == false || importer.textureType == TextureImporterType.NormalMap;
        bool prevReadable = importer.isReadable;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.ImportAsset(texpath, ImportAssetOptions.ForceUpdate);
            inputFormat = input.format;
        }

        // Copy input texture pixel data
        Color[] colors = input.GetPixels();
        TextureData res = new TextureData(input.width, input.height);
        for (int x = 0; x < res.width; x++)
        {
            for (int y = 0; y < res.height; y++)
            {
                res.SetColorAt(x, y, linearInput || PlayerSettings.colorSpace == ColorSpace.Gamma ?
                    colors[y * res.width + x] : colors[y * res.width + x].linear);
            }
        }

        // Revert input texture settings
        if (importer != null)
        {
            importer.textureType = prevType;
            importer.isReadable = prevReadable;
            importer.textureCompression = prev;
            AssetDatabase.ImportAsset(texpath, ImportAssetOptions.ForceUpdate);
        }
        return res;
    }

    static void FinalizePrecomputedTextures(ref TextureFormat inputFormat, ProceduralTexture2D target, ref TextureData Tinput, ref TextureData invT)
    {
        // Serialize precomputed data as new subasset texture. Reuse existing texture if possible to avoid breaking texture references in shadergraph.
        if(target.Tinput == null)
        {
            target.Tinput = new Texture2D(Tinput.width, Tinput.height, inputFormat, target.generateMipMaps, true);
            AssetDatabase.AddObjectToAsset(target.Tinput, target);
        }
        target.Tinput.Reinitialize(Tinput.width, Tinput.height, inputFormat, target.generateMipMaps);
        target.Tinput.name = target.input.name + "_T";
        target.Tinput.SetPixels(Tinput.data);
        target.Tinput.wrapMode = TextureWrapMode.Repeat;
        target.Tinput.filterMode = target.filterMode;
        target.Tinput.anisoLevel = target.anisoLevel;
        target.Tinput.Apply();
        if (target.compressionQuality != ProceduralTexture2D.CompressionLevel.None)
        {
            if(target.compressionQuality == ProceduralTexture2D.CompressionLevel.HighQuality)
                EditorUtility.CompressTexture(target.Tinput, TextureFormat.BC7, (int)target.compressionQuality);
            else if (inputFormat == TextureFormat.RGBA32)
                EditorUtility.CompressTexture(target.Tinput, TextureFormat.DXT5, (int)target.compressionQuality);
            else
                EditorUtility.CompressTexture(target.Tinput, TextureFormat.DXT1, (int)target.compressionQuality);
        }
        target.Tinput.Apply();

        if (target.invT == null)
        {
            target.invT = new Texture2D(invT.width, invT.height, inputFormat, false, true);
            AssetDatabase.AddObjectToAsset(target.invT, target);
        }
        target.invT.Reinitialize(invT.width, invT.height, inputFormat, false);
        target.invT.name = target.input.name + "_invT";
        target.invT.wrapMode = TextureWrapMode.Clamp;
        target.invT.filterMode = FilterMode.Bilinear;
        target.invT.SetPixels(invT.data);
        target.invT.Apply();

        // Update asset database
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void Precomputations(
            ref TextureData input,          // input:  example image
                List<int> channels,         // input:  channels to process
            ref TextureData Tinput,         // output: T(input) image
            ref TextureData invT,           // output: T^{-1} look-up table
            string assetName)
    {
        // Section 1.3.2 Applying the histogram transformation T on the input
        foreach (int channel in channels)
        {
            ComputeTinput(ref input, ref Tinput, channel);
            EditorUtility.DisplayProgressBar("Pre-processing Procedural Texture Data", assetName, (float)stepCounter++ / (totalSteps - 1));
        }

        // Section 1.3.3 Precomputing the inverse histogram transformation T^{-1}
        foreach (int channel in channels)
        {
            ComputeinvT(ref input, ref invT, channel);
            EditorUtility.DisplayProgressBar("Pre-processing Procedural Texture Data", assetName, (float)stepCounter++ / (totalSteps - 1));
        }

        // Section 1.5 Improvement: prefiltering the look-up table
        foreach (int channel in channels)
        {
            PrefilterLUT(ref Tinput, ref invT, channel);
            EditorUtility.DisplayProgressBar("Pre-processing Procedural Texture Data", assetName, (float)stepCounter++ / (totalSteps - 1));
        }
    }

    private static void ComputeCompressionScalers(ProceduralTexture2D target)
    {
        target.compressionScalers = Vector4.one;
        if (target.compressionQuality != ProceduralTexture2D.CompressionLevel.None && target.type != ProceduralTexture2D.TextureType.Other)
        {
            target.compressionScalers.x = 1.0f / target.colorSpaceVector1.magnitude;
            target.compressionScalers.y = 1.0f / target.colorSpaceVector2.magnitude;
            target.compressionScalers.z = 1.0f / target.colorSpaceVector3.magnitude;
        }
    }

    private static void RescaleForCompression(ProceduralTexture2D target, ref TextureData Tinput)
    {
        int channelCount = (target.type == ProceduralTexture2D.TextureType.Color && target.includeAlpha == true) || target.type == ProceduralTexture2D.TextureType.Other ?
            4 : 3;

        // If we use DXT compression
        // we need to rescale the Gaussian channels (see Section 1.6)
        if (target.compressionQuality != ProceduralTexture2D.CompressionLevel.None && target.type != ProceduralTexture2D.TextureType.Other)
        {
            for (int y = 0; y < Tinput.height; y++)
                for (int x = 0; x < Tinput.width; x++)
                    for (int i = 0; i < channelCount; i++)
                    {
                        float v = Tinput.GetColor(x, y)[i];
                        v = (v - 0.5f) / target.compressionScalers[i] + 0.5f;
                        Tinput.GetColorRef(x, y)[i] = v;
                    }
        }
    }

    /*****************************************************************************/
    /**************** Section 1.3.1 Target Gaussian distribution *****************/
    /*****************************************************************************/

    private static float Erf(float x)
    {
        // Save the sign of x
        int sign = 1;
        if (x < 0)
            sign = -1;
        x = Mathf.Abs(x);

        // A&S formula 7.1.26
        float t = 1.0f / (1.0f + 0.3275911f * x);
        float y = 1.0f - (((((1.061405429f * t + -1.453152027f) * t) + 1.421413741f)
            * t + -0.284496736f) * t + 0.254829592f) * t * Mathf.Exp(-x * x);

        return sign * y;
    }

    private static float ErfInv(float x)
    {
        float w, p;
        w = -Mathf.Log((1.0f - x) * (1.0f + x));
        if (w < 5.000000f)
        {
            w = w - 2.500000f;
            p = 2.81022636e-08f;
            p = 3.43273939e-07f + p * w;
            p = -3.5233877e-06f + p * w;
            p = -4.39150654e-06f + p * w;
            p = 0.00021858087f + p * w;
            p = -0.00125372503f + p * w;
            p = -0.00417768164f + p * w;
            p = 0.246640727f + p * w;
            p = 1.50140941f + p * w;
        }
        else
        {
            w = Mathf.Sqrt(w) - 3.000000f;
            p = -0.000200214257f;
            p = 0.000100950558f + p * w;
            p = 0.00134934322f + p * w;
            p = -0.00367342844f + p * w;
            p = 0.00573950773f + p * w;
            p = -0.0076224613f + p * w;
            p = 0.00943887047f + p * w;
            p = 1.00167406f + p * w;
            p = 2.83297682f + p * w;
        }
        return p * x;
    }

    private static float CDF(float x, float mu, float sigma)
    {
        float U = 0.5f * (1 + Erf((x - mu) / (sigma * Mathf.Sqrt(2.0f))));
        return U;
    }

    private static float invCDF(float U, float mu, float sigma)
    {
        float x = sigma * Mathf.Sqrt(2.0f) * ErfInv(2.0f * U - 1.0f) + mu;
        return x;
    }

    /*****************************************************************************/
    /**** Section 1.3.2 Applying the histogram transformation T on the input *****/
    /*****************************************************************************/
    private struct PixelSortStruct
    {
        public int x;
        public int y;
        public float value;
    };

    private static void ComputeTinput(ref TextureData input, ref TextureData T_input, int channel)
    {
        // Sort pixels of example image
        PixelSortStruct[] sortedInputValues = new PixelSortStruct[input.width * input.height];
        for (int y = 0; y < input.height; y++)
            for (int x = 0; x < input.width; x++)
            {
                sortedInputValues[y * input.width + x].x = x;
                sortedInputValues[y * input.width + x].y = y;
                sortedInputValues[y * input.width + x].value = input.GetColor(x, y)[channel];
            }
        System.Array.Sort(sortedInputValues, (x, y) => x.value.CompareTo(y.value));

        // Assign Gaussian value to each pixel
        for (uint i = 0; i < sortedInputValues.Length; i++)
        {
            // Pixel coordinates
            int x = sortedInputValues[i].x;
            int y = sortedInputValues[i].y;
            // Input quantile (given by its order in the sorting)
            float U = (i + 0.5f) / (sortedInputValues.Length);
            // Gaussian quantile
            float G = invCDF(U, GAUSSIAN_AVERAGE, GAUSSIAN_STD);
            // Store
            T_input.GetColorRef(x, y)[channel] = G;
        }
    }

    /*****************************************************************************/
    /*  Section 1.3.3 Precomputing the inverse histogram transformation T^{-1}   */
    /*****************************************************************************/

    private static void ComputeinvT(ref TextureData input, ref TextureData Tinv, int channel)
    {
        // Sort pixels of example image
        float[] sortedInputValues = new float[input.width * input.height];
        for (int y = 0; y < input.height; y++)
            for (int x = 0; x < input.width; x++)
            {
                sortedInputValues[y * input.width + x] = input.GetColor(x, y)[channel];
            }
        System.Array.Sort(sortedInputValues);

        // Generate Tinv look-up table 
        for (int i = 0; i < Tinv.width; i++)
        {
            // Gaussian value in [0, 1]
            float G = (i + 0.5f) / (Tinv.width);
            // Quantile value 
            float U = CDF(G, GAUSSIAN_AVERAGE, GAUSSIAN_STD);
            // Find quantile in sorted pixel values
            int index = (int)Mathf.Floor(U * sortedInputValues.Length);
            // Get input value 
            float I = sortedInputValues[index];
            // Store in LUT
            Tinv.GetColorRef(i, 0)[channel] = I;
        }
    }

    /*****************************************************************************/
    /******** Section 1.4 Improvement: using a decorrelated color space **********/
    /*****************************************************************************/

    // Compute the eigen vectors of the histogram of the input
    private static void ComputeEigenVectors(ref TextureData input, Vector3[] eigenVectors)
    {
        // First and second order moments
        float R = 0, G = 0, B = 0, RR = 0, GG = 0, BB = 0, RG = 0, RB = 0, GB = 0;
        for (int y = 0; y < input.height; y++)
        {
            for (int x = 0; x < input.width; x++)
            {
                Color col = input.GetColor(x, y);
                R += col.r;
                G += col.g;
                B += col.b;
                RR += col.r * col.r;
                GG += col.g * col.g;
                BB += col.b * col.b;
                RG += col.r * col.g;
                RB += col.r * col.b;
                GB += col.g * col.b;
            }
        }

        R /= (float)(input.width * input.height);
        G /= (float)(input.width * input.height);
        B /= (float)(input.width * input.height);
        RR /= (float)(input.width * input.height);
        GG /= (float)(input.width * input.height);
        BB /= (float)(input.width * input.height);
        RG /= (float)(input.width * input.height);
        RB /= (float)(input.width * input.height);
        GB /= (float)(input.width * input.height);

        // Covariance matrix
        double[][] covarMat = new double[3][];
        for (int i = 0; i < 3; i++)
            covarMat[i] = new double[3];
        covarMat[0][0] = RR - R * R;
        covarMat[0][1] = RG - R * G;
        covarMat[0][2] = RB - R * B;
        covarMat[1][0] = RG - R * G;
        covarMat[1][1] = GG - G * G;
        covarMat[1][2] = GB - G * B;
        covarMat[2][0] = RB - R * B;
        covarMat[2][1] = GB - G * B;
        covarMat[2][2] = BB - B * B;

        // Find eigen values and vectors using Jacobi algorithm
        double[][] eigenVectorsTemp = new double[3][];
        for (int i = 0; i < 3; i++)
            eigenVectorsTemp[i] = new double[3];
        double[] eigenValuesTemp = new double[3];
        ComputeEigenValuesAndVectors(covarMat, eigenVectorsTemp, eigenValuesTemp);

        // Set return values
        eigenVectors[0] = new Vector3((float)eigenVectorsTemp[0][0], (float)eigenVectorsTemp[1][0], (float)eigenVectorsTemp[2][0]);
        eigenVectors[1] = new Vector3((float)eigenVectorsTemp[0][1], (float)eigenVectorsTemp[1][1], (float)eigenVectorsTemp[2][1]);
        eigenVectors[2] = new Vector3((float)eigenVectorsTemp[0][2], (float)eigenVectorsTemp[1][2], (float)eigenVectorsTemp[2][2]);
    }

    // ----------------------------------------------------------------------------
    // Numerical diagonalization of 3x3 matrcies
    // Copyright (C) 2006  Joachim Kopp
    // ----------------------------------------------------------------------------
    // This library is free software; you can redistribute it and/or
    // modify it under the terms of the GNU Lesser General Public
    // License as published by the Free Software Foundation; either
    // version 2.1 of the License, or (at your option) any later version.
    //
    // This library is distributed in the hope that it will be useful,
    // but WITHOUT ANY WARRANTY; without even the implied warranty of
    // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    // Lesser General Public License for more details.
    //
    // You should have received a copy of the GNU Lesser General Public
    // License along with this library; if not, write to the Free Software
    // Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
    // ----------------------------------------------------------------------------
    // Calculates the eigenvalues and normalized eigenvectors of a symmetric 3x3
    // matrix A using the Jacobi algorithm.
    // The upper triangular part of A is destroyed during the calculation,
    // the diagonal elements are read but not destroyed, and the lower
    // triangular elements are not referenced at all.
    // ----------------------------------------------------------------------------
    // Parameters:
    //		A: The symmetric input matrix
    //		Q: Storage buffer for eigenvectors
    //		w: Storage buffer for eigenvalues
    // ----------------------------------------------------------------------------
    // Return value:
    //		0: Success
    //		-1: Error (no convergence)
    private static int ComputeEigenValuesAndVectors(double[][] A, double[][] Q, double[] w)
    {
        const int n = 3;
        double sd, so;                  // Sums of diagonal resp. off-diagonal elements
        double s, c, t;                 // sin(phi), cos(phi), tan(phi) and temporary storage
        double g, h, z, theta;          // More temporary storage
        double thresh;

        // Initialize Q to the identitity matrix
        for (int i = 0; i < n; i++)
        {
            Q[i][i] = 1.0;
            for (int j = 0; j < i; j++)
                Q[i][j] = Q[j][i] = 0.0;
        }

        // Initialize w to diag(A)
        for (int i = 0; i < n; i++)
            w[i] = A[i][i];

        // Calculate SQR(tr(A))  
        sd = 0.0;
        for (int i = 0; i < n; i++)
            sd += System.Math.Abs(w[i]);
        sd = sd * sd;

        // Main iteration loop
        for (int nIter = 0; nIter < 50; nIter++)
        {
            // Test for convergence 
            so = 0.0;
            for (int p = 0; p < n; p++)
                for (int q = p + 1; q < n; q++)
                    so += System.Math.Abs(A[p][q]);
            if (so == 0.0)
                return 0;

            if (nIter < 4)
                thresh = 0.2 * so / (n * n);
            else
                thresh = 0.0;

            // Do sweep
            for (int p = 0; p < n; p++)
            {
                for (int q = p + 1; q < n; q++)
                {
                    g = 100.0 * System.Math.Abs(A[p][q]);
                    if (nIter > 4 && System.Math.Abs(w[p]) + g == System.Math.Abs(w[p])
                        && System.Math.Abs(w[q]) + g == System.Math.Abs(w[q]))
                    {
                        A[p][q] = 0.0;
                    }
                    else if (System.Math.Abs(A[p][q]) > thresh)
                    {
                        // Calculate Jacobi transformation
                        h = w[q] - w[p];
                        if (System.Math.Abs(h) + g == System.Math.Abs(h))
                        {
                            t = A[p][q] / h;
                        }
                        else
                        {
                            theta = 0.5 * h / A[p][q];
                            if (theta < 0.0)
                                t = -1.0 / (System.Math.Sqrt(1.0 + (theta * theta)) - theta);
                            else
                                t = 1.0 / (System.Math.Sqrt(1.0 + (theta * theta)) + theta);
                        }
                        c = 1.0 / System.Math.Sqrt(1.0 + (t * t));
                        s = t * c;
                        z = t * A[p][q];

                        // Apply Jacobi transformation
                        A[p][q] = 0.0;
                        w[p] -= z;
                        w[q] += z;
                        for (int r = 0; r < p; r++)
                        {
                            t = A[r][p];
                            A[r][p] = c * t - s * A[r][q];
                            A[r][q] = s * t + c * A[r][q];
                        }
                        for (int r = p + 1; r < q; r++)
                        {
                            t = A[p][r];
                            A[p][r] = c * t - s * A[r][q];
                            A[r][q] = s * t + c * A[r][q];
                        }
                        for (int r = q + 1; r < n; r++)
                        {
                            t = A[p][r];
                            A[p][r] = c * t - s * A[q][r];
                            A[q][r] = s * t + c * A[q][r];
                        }

                        // Update eigenvectors
                        for (int r = 0; r < n; r++)
                        {
                            t = Q[r][p];
                            Q[r][p] = c * t - s * Q[r][q];
                            Q[r][q] = s * t + c * Q[r][q];
                        }
                    }
                }
            }
        }

        return -1;
    }

    // Main function of Section 1.4
    private static void DecorrelateColorSpace(
        ref TextureData input,              // input:  example image
        ref TextureData input_decorrelated, // output: decorrelated input 
        ref Vector3 colorSpaceVector1,      // output: color space vector1 
        ref Vector3 colorSpaceVector2,      // output: color space vector2
        ref Vector3 colorSpaceVector3,      // output: color space vector3
        ref Vector3 colorSpaceOrigin,       // output: color space origin
        string assetName)
    {
        // Compute the eigenvectors of the histogram
        Vector3[] eigenvectors = new Vector3[3];
        ComputeEigenVectors(ref input, eigenvectors);
        EditorUtility.DisplayProgressBar("Pre-processing Procedural Texture Data", assetName, (float)stepCounter++ / (totalSteps - 1));

        // Rotate to eigenvector space
        for (int y = 0; y < input.height; y++)
            for (int x = 0; x < input.width; x++)
                for (int channel = 0; channel < 3; ++channel)
                {
                    // Get current color
                    Color color = input.GetColor(x, y);
                    Vector3 vec = new Vector3(color.r, color.g, color.b);
                    // Project on eigenvector 
                    float new_channel_value = Vector3.Dot(vec, eigenvectors[channel]);
                    // Store
                    input_decorrelated.GetColorRef(x, y)[channel] = new_channel_value;
                }
        EditorUtility.DisplayProgressBar("Pre-processing Procedural Texture Data", assetName, (float)stepCounter++ / (totalSteps - 1));

        // Compute ranges of the new color space
        Vector2[] colorSpaceRanges = new Vector2[3]{
                new Vector2(float.MaxValue, float.MinValue),
                new Vector2(float.MaxValue, float.MinValue),
                new Vector2(float.MaxValue, float.MinValue) };
        for (int y = 0; y < input.height; y++)
            for (int x = 0; x < input.width; x++)
                for (int channel = 0; channel < 3; ++channel)
                {
                    colorSpaceRanges[channel].x = Mathf.Min(colorSpaceRanges[channel].x, input_decorrelated.GetColor(x, y)[channel]);
                    colorSpaceRanges[channel].y = Mathf.Max(colorSpaceRanges[channel].y, input_decorrelated.GetColor(x, y)[channel]);
                }
        EditorUtility.DisplayProgressBar("Pre-processing Procedural Texture Data", assetName, (float)stepCounter++ / (totalSteps - 1));

        // Remap range to [0, 1]
        for (int y = 0; y < input.height; y++)
            for (int x = 0; x < input.width; x++)
                for (int channel = 0; channel < 3; ++channel)
                {
                    // Get current value
                    float value = input_decorrelated.GetColor(x, y)[channel];
                    // Remap in [0, 1]
                    float remapped_value = (value - colorSpaceRanges[channel].x) / (colorSpaceRanges[channel].y - colorSpaceRanges[channel].x);
                    // Store
                    input_decorrelated.GetColorRef(x, y)[channel] = remapped_value;
                }
        EditorUtility.DisplayProgressBar("Pre-processing Procedural Texture Data", assetName, (float)stepCounter++ / (totalSteps - 1));

        // Compute color space origin and vectors scaled for the normalized range
        colorSpaceOrigin.x = colorSpaceRanges[0].x * eigenvectors[0].x + colorSpaceRanges[1].x * eigenvectors[1].x + colorSpaceRanges[2].x * eigenvectors[2].x;
        colorSpaceOrigin.y = colorSpaceRanges[0].x * eigenvectors[0].y + colorSpaceRanges[1].x * eigenvectors[1].y + colorSpaceRanges[2].x * eigenvectors[2].y;
        colorSpaceOrigin.z = colorSpaceRanges[0].x * eigenvectors[0].z + colorSpaceRanges[1].x * eigenvectors[1].z + colorSpaceRanges[2].x * eigenvectors[2].z;
        colorSpaceVector1.x = eigenvectors[0].x * (colorSpaceRanges[0].y - colorSpaceRanges[0].x);
        colorSpaceVector1.y = eigenvectors[0].y * (colorSpaceRanges[0].y - colorSpaceRanges[0].x);
        colorSpaceVector1.z = eigenvectors[0].z * (colorSpaceRanges[0].y - colorSpaceRanges[0].x);
        colorSpaceVector2.x = eigenvectors[1].x * (colorSpaceRanges[1].y - colorSpaceRanges[1].x);
        colorSpaceVector2.y = eigenvectors[1].y * (colorSpaceRanges[1].y - colorSpaceRanges[1].x);
        colorSpaceVector2.z = eigenvectors[1].z * (colorSpaceRanges[1].y - colorSpaceRanges[1].x);
        colorSpaceVector3.x = eigenvectors[2].x * (colorSpaceRanges[2].y - colorSpaceRanges[2].x);
        colorSpaceVector3.y = eigenvectors[2].y * (colorSpaceRanges[2].y - colorSpaceRanges[2].x);
        colorSpaceVector3.z = eigenvectors[2].z * (colorSpaceRanges[2].y - colorSpaceRanges[2].x);
    }

    /*****************************************************************************/
    /* ===== Section 1.5 Improvement: prefiltering the look-up table =========== */
    /*****************************************************************************/
    // Compute average subpixel variance at a given LOD
    private static float ComputeLODAverageSubpixelVariance(ref TextureData image, int LOD, int channel)
    {
        // Window width associated with
        int windowWidth = 1 << LOD;

        // Compute average variance in all the windows
        float average_window_variance = 0.0f;

        // Loop over al the windows
        for (int window_y = 0; window_y < image.height; window_y += windowWidth)
            for (int window_x = 0; window_x < image.width; window_x += windowWidth)
            {
                // Estimate variance of current window
                float v = 0.0f;
                float v2 = 0.0f;
                for (int y = 0; y < windowWidth; y++)
                    for (int x = 0; x < windowWidth; x++)
                    {
                        float value = image.GetColor(window_x + x, window_y + y)[channel];
                        v += value;
                        v2 += value * value;
                    }
                v /= (float)(windowWidth * windowWidth);
                v2 /= (float)(windowWidth * windowWidth);
                float window_variance = Mathf.Max(0.0f, v2 - v * v);

                // Update average
                average_window_variance += window_variance / (image.width * image.height / windowWidth / windowWidth);
            }

        return average_window_variance;
    }

    // Filter LUT by sampling a Gaussian N(mu, std²)
    private static float FilterLUTValueAtx(ref TextureData LUT, float x, float std, int channel)
    {
        // Number of samples for filtering (heuristic: twice the LUT resolution)
        const int numberOfSamples = 2 * LUT_WIDTH;

        // Filter
        float filtered_value = 0.0f;
        for (int sample = 0; sample < numberOfSamples; sample++)
        {
            // Quantile used to sample the Gaussian
            float U = (sample + 0.5f) / numberOfSamples;
            // Sample the Gaussian 
            float sample_x = invCDF(U, x, std);
            // Find sample texel in LUT (the LUT covers the domain [0, 1])
            int sample_texel = Mathf.Max(0, Mathf.Min(LUT_WIDTH - 1, (int)Mathf.Floor(sample_x * LUT_WIDTH)));
            // Fetch LUT at level 0
            float sample_value = LUT.GetColor(sample_texel, 0)[channel];
            // Accumulate
            filtered_value += sample_value;
        }
        // Normalize and return
        filtered_value /= (float)numberOfSamples;
        return filtered_value;
    }

    // Main function of section 1.5
    private static void PrefilterLUT(ref TextureData image_T_Input, ref TextureData LUT_Tinv, int channel)
    {
        // Prefilter 
        for (int LOD = 1; LOD < LUT_Tinv.height; LOD++)
        {
            // Compute subpixel variance at LOD 
            float window_variance = ComputeLODAverageSubpixelVariance(ref image_T_Input, LOD, channel);
            float window_std = Mathf.Sqrt(window_variance);

            // Prefilter LUT with Gaussian kernel of this variance
            for (int i = 0; i < LUT_Tinv.width; i++)
            {
                // Texel position in [0, 1]
                float x_texel = (i + 0.5f) / LUT_Tinv.width;
                // Filter look-up table around this position with Gaussian kernel
                float filteredValue = FilterLUTValueAtx(ref LUT_Tinv, x_texel, window_std, channel);
                // Store filtered value
                LUT_Tinv.GetColorRef(i, LOD)[channel] = filteredValue;
            }
        }
    }
}
