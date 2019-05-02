using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Windows;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class HDRSkyEditor : EditorWindow
    {
        [MenuItem("Window/Texture/HDR Sky White Balance Editor")]
        public static void ShowWindow()
        {
            HDRSkyEditor window = EditorWindow.GetWindow<HDRSkyEditor>(true, GUI_NAME_WINDOW, true);
            window.Show();
        }

        private enum HDRSkyEditorError
        {
            None = 0,
            NoSavePathSpecified,
            NoTextureAssigned,
            TextureUnreadable,
            Count
        };

        private static readonly string[] hdrSkyEditorErrorDescriptions =
        {
            "No Error",
            "Error: No save path specified.",
            "Error: One or more texture slices has not texture assigned.",
            "Error: Texture source not readable.\nPlease set Read/Write Enabled checkbox in Texture2D import settings to allow this tool to use Texture2D as source."
        };

        public struct HDRSkySettings
        {
            public float colorTemperatureSource;
            public float colorTemperatureDestination;
        };

        private static readonly string GUI_NAME_WINDOW = "HDR Sky White Balance Editor";
        private static readonly string GUI_NAME_IS_SETTINGS_DISPLAYED = "Output Settings";
        private static readonly string GUI_NAME_COLOR_TEMPERATURE_SOURCE = "Source Color Temperature";
        private static readonly string GUI_NAME_COLOR_TEMPERATURE_DESTINATION = "Destination Color Temperature";
        private static readonly string GUI_TEXTURE_IN_DEFAULT_NAME = "Source Texture";
        private static readonly string SAVE_BUTTON_NAME = "Save HDR Sky";
        private static readonly string SAVE_DIALOGUE = "Save HDR Sky";
        private static readonly string DEFAULT_SAVE_DIRECTORY = "Assets";
        private static readonly string DEFAULT_FILENAME = "untitled_hdr_sky.exr";
        private static readonly string FILE_EXTENSION = "exr";

        [System.NonSerialized] private HDRSkyEditorError error;
        [System.NonSerialized] public Texture2D textureSource;
        [System.NonSerialized] public Texture2D textureDestination;
        [System.NonSerialized] public Color[] textureSourceData;
        [System.NonSerialized] public Color[] textureDestinationData;

        [System.NonSerialized] private bool isSettingsDisplayed = true;
        [System.NonSerialized] public HDRSkySettings settings;

        private void OnGUI()
        {
            // Display any errors at the top of our GUI.
            // Error will have one frame of latency.
            ErrorDisplay();

            if (ErrorCapture(ComputeAndDisplaySettings(ref settings)))
            {
                return;
            }

            if (ErrorCapture(ComputeAndDisplayTextureSource(ref textureSource, ref textureSourceData)))
            {
                return;
            }

            if (GUILayout.Button(SAVE_BUTTON_NAME))
            {
                Texture3D texture3D = null;
                if (ErrorCapture(HDRSkyEditor.ComputeTextureDestinationFromSource(ref textureDestination, ref textureDestinationData, ref textureSource, ref textureSourceData, settings)))
                {
                    return;
                }

                if (ErrorCapture(HDRSkyEditor.SaveEXRFromTextureDestination(textureDestination, settings)))
                {
                    return;
                }
            }
        }

        private void ErrorDisplay()
        {
            if (error != HDRSkyEditorError.None)
            {
                EditorGUILayout.HelpBox(hdrSkyEditorErrorDescriptions[(uint)error], MessageType.Info);
            }

            // Error has been displayed to user.
            // Clear error to allow user an opportunity to make changes.
            error = HDRSkyEditorError.None;
        }

        private bool ErrorCapture(HDRSkyEditorError errorNext)
        {
            error = (error == HDRSkyEditorError.None) ? errorNext : error;
            return !(error == HDRSkyEditorError.None);
        }

        private HDRSkyEditorError ComputeAndDisplaySettings(ref HDRSkySettings res)
        {
            isSettingsDisplayed = EditorGUILayout.Foldout(isSettingsDisplayed, GUI_NAME_IS_SETTINGS_DISPLAYED);
            if (isSettingsDisplayed)
            {
                EditorGUILayout.BeginVertical();
                ++EditorGUI.indentLevel;

                res.colorTemperatureSource = (res.colorTemperatureSource == 0) ? 6500 : res.colorTemperatureSource;
                res.colorTemperatureDestination = (res.colorTemperatureDestination == 0) ? 6500 : res.colorTemperatureDestination;

                res.colorTemperatureSource = Mathf.Clamp(EditorGUILayout.FloatField(GUI_NAME_COLOR_TEMPERATURE_SOURCE, res.colorTemperatureSource), 1000.0f, 10000.0f);
                res.colorTemperatureDestination = Mathf.Clamp(EditorGUILayout.FloatField(GUI_NAME_COLOR_TEMPERATURE_DESTINATION, res.colorTemperatureDestination), 1000.0f, 10000.0f);

                --EditorGUI.indentLevel;
                EditorGUILayout.EndVertical();
            }

            return HDRSkyEditorError.None;
        }

        private static HDRSkyEditorError ComputeAndDisplayTextureSource(ref Texture2D textureSource, ref Color[] textureSourceData)
        {
            HDRSkyEditorError error = HDRSkyEditorError.None;

            EditorGUILayout.BeginVertical();
            ++EditorGUI.indentLevel;

            Texture2D textureSourcePrevious = textureSource;
            textureSource = (Texture2D)EditorGUILayout.ObjectField((textureSource != null) ? textureSource.name : GUI_TEXTURE_IN_DEFAULT_NAME, textureSource, typeof(Texture2D), true);
            if (textureSource != null && textureSource != textureSourcePrevious)
            {
                // Different texture was assigned. Grab the pixel data for use in resampling.
                textureSourceData = textureSource.GetPixels(0, 0, textureSource.width, textureSource.height);
            }

            --EditorGUI.indentLevel;
            EditorGUILayout.EndVertical();

            return error;
        }

        private static HDRSkyEditorError ComputeTextureDestinationFromSource(ref Texture2D textureDestination, ref Color[] textureDestinationData, ref Texture2D textureSource, ref Color[] textureSourceData, HDRSkySettings settings)
        {
            if (textureSource == null) { return HDRSkyEditorError.NoTextureAssigned; }

            if (textureDestinationData == null || textureDestinationData.Length != textureSourceData.Length)
            {
                textureDestinationData = new Color[textureSourceData.Length];
            }

            const bool isGenerateMipmapsEnabled = false;
            textureDestination = new Texture2D(
                textureSource.width,
                textureSource.height,
                textureSource.format,
                isGenerateMipmapsEnabled
            );

            HDRSkyEditorError error = HDRSkyEditorError.None;

            Matrix4x4 chromaticAdaptationMatrix = ComputeVonKriesChromaticAdaptationMatrixFromTemperature(settings.colorTemperatureDestination, settings.colorTemperatureSource);
            Vector3 tmpVector;
            Color tmpColor;

            for (int y = 0, ylen = textureSource.height; y < ylen; ++y)
            {
                for (int x = 0, xlen = textureSource.width; x < xlen; ++x)
                {
                    int i = y * xlen + x;
                    tmpVector.x = textureSourceData[i].r;
                    tmpVector.y = textureSourceData[i].g;
                    tmpVector.z = textureSourceData[i].b;

                    tmpVector = chromaticAdaptationMatrix.MultiplyVector(tmpVector);

                    tmpColor.r = tmpVector.x;
                    tmpColor.g = tmpVector.y;
                    tmpColor.b = tmpVector.z;
                    tmpColor.a = 1.0f;

                    textureDestinationData[i] = tmpColor;
                }
            }

            if (error != HDRSkyEditorError.None) { return error; }
            textureDestination.SetPixels(textureDestinationData, 0);
            textureDestination.Apply(isGenerateMipmapsEnabled);
            return HDRSkyEditorError.None;
        }

        private static HDRSkyEditorError SaveEXRFromTextureDestination(Texture2D textureDestination, HDRSkySettings settings)
        {
            // Returns path relative to project base directory.
            // Required for AssetDatabase.CreateAsset() which Expects paths relative to project base.
            string path = EditorUtility.SaveFilePanelInProject(SAVE_DIALOGUE, DEFAULT_FILENAME, FILE_EXTENSION, "");
            if (path.Length == 0) { return HDRSkyEditorError.NoSavePathSpecified; }

            byte[] bytes = textureDestination.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
            File.WriteAllBytes(path, bytes);

            return HDRSkyEditorError.None;
        }

        // Simple Analytic Approximations to the CIE XYZ Color Matching Functions
        // jcgt.org/published/0002/02/01/paper.pdf
        private static float Cie1931FromWavelengthApproximateX(float wavelength)
        {
            float t1 = (wavelength - 442.0f) * ((wavelength < 442.0f) ? 0.0624f : 0.0374f);
            float t2 = (wavelength - 599.8f) * ((wavelength < 599.8f) ? 0.0264f : 0.0323f);
            float t3 = (wavelength - 501.1f) * ((wavelength < 501.1f) ? 0.0490f : 0.0382f);
            return 0.362f * Mathf.Exp(-0.5f * t1 * t1) + 1.056f * Mathf.Exp(-0.5f * t2 * t2) - 0.065f * Mathf.Exp(-0.5f * t3 * t3);
        }

        private static float Cie1931FromWavelengthApproximateY(float wavelength)
        {
            float t1 = (wavelength - 568.8f) * ((wavelength < 568.8f) ? 0.0213f : 0.0247f);
            float t2 = (wavelength - 530.9f) * ((wavelength < 530.9f) ? 0.0613f : 0.0322f);
            return 0.821f * Mathf.Exp(-0.5f * t1 * t1) + 0.286f * Mathf.Exp(-0.5f * t2 * t2);

        }

        private static float Cie1931FromWavelengthApproximateZ(float wavelength)
        {
            float t1 = (wavelength - 437.0f) * ((wavelength<437.0f) ? 0.0845f : 0.0278f);
            float t2 = (wavelength - 459.0f) * ((wavelength<459.0f) ? 0.0385f : 0.0725f);
            return 1.217f * Mathf.Exp(-0.5f * t1 * t1) + 0.681f * Mathf.Exp(-0.5f * t2 * t2);
        }

        private static void Cie1931FromWavelengthApproximate(ref Vector3 res, float wavelength)
        {
            res.x = Cie1931FromWavelengthApproximateX(wavelength);
            res.y = Cie1931FromWavelengthApproximateY(wavelength);
            res.z = Cie1931FromWavelengthApproximateZ(wavelength);
        }

        // https://en.wikipedia.org/wiki/Planck%27s_law
        private static float PlancksLaw(float temperature, float t)
        {
            float o = 662606896e-42f;
            float n = 299792458.0f;
            return 2.0f * Mathf.PI * o * n * n * Mathf.Pow(t, -5.0f) / (Mathf.Exp(0.014387768f / (t * temperature)) - 1.0f);
        }

        private static void Cie1931FromTemperatureApproximate(ref Vector3 res, float temperature, uint sampleCount)
        {
            float sampleStride = 400.0f / (float)sampleCount;
            res = Vector3.zero;
            Vector3 tmp = Vector3.zero;
            for (float wavelength = 380.0f; wavelength <= 780.0f; wavelength += sampleStride) {
                Cie1931FromWavelengthApproximate(ref tmp, wavelength);
                float weight = PlancksLaw(temperature, 1e-9f * wavelength);
                res.x += tmp.x * weight;
                res.y += tmp.y * weight;
                res.z += tmp.z * weight;
            }
            float normalizationInverse = (res.x + res.y + res.z);
            res.x /= normalizationInverse;
            res.y /= normalizationInverse;
            res.z /= normalizationInverse;
        }

        private static void Cie1931ChromacityFromCie1931(ref Vector2 res, Vector3 cie1931)
        {
            float normalization = 1.0f / (1.0f / (cie1931.x + cie1931.y + cie1931.z));
            res.x = cie1931.x * normalization;
            res.y = cie1931.y * normalization;
        }

        private static Matrix4x4 ComputeVonKriesChromaticAdaptationMatrixFromTemperature(float temperatureDestination, float temperatureSource)
        {
            Vector3 whitepointSourceCie1931 = Vector3.zero;
            Vector3 whitepointDestinationCie1931 = Vector3.zero;
            HDRSkyEditor.Cie1931FromTemperatureApproximate(ref whitepointSourceCie1931, temperatureSource, 64);
            HDRSkyEditor.Cie1931FromTemperatureApproximate(ref whitepointDestinationCie1931, temperatureDestination, 64);

            Vector2 whitepointChromacitySourceCie1931 = Vector2.zero;
            Vector2 whitepointChromatityDestinationCie1931 = Vector2.zero;
            HDRSkyEditor.Cie1931ChromacityFromCie1931(ref whitepointChromacitySourceCie1931, whitepointSourceCie1931);
            HDRSkyEditor.Cie1931ChromacityFromCie1931(ref whitepointChromatityDestinationCie1931, whitepointDestinationCie1931);

            return HDRSkyEditor.ComputeVonKriesChromaticAdaptationMatrix(whitepointChromacitySourceCie1931, whitepointChromatityDestinationCie1931);
        }

        // https://en.wikipedia.org/wiki/Chromatic_adaptation
        // http://www.brucelindbloom.com/index.html?Eqn_ChromAdapt.html
        // Note: All the matrices in this function are 3x3 matrices, zero padded out to 4x4 matrices since Unity does not support Matrix3x3.
        private static Matrix4x4 ComputeVonKriesChromaticAdaptationMatrix(Vector2 chromacityWhiteSource, Vector2 chromacityWhiteDestination) {
            // Transform from CIE1931 space to cone response domain.
            // https://en.wikipedia.org/wiki/LMS_color_space

            Matrix4x4 coneResponseCiecam02FromCie1931 = new Matrix4x4();
            coneResponseCiecam02FromCie1931.SetColumn(0, new Vector4(0.7328f, -0.7036f, 0.0030f, 0.0f));
            coneResponseCiecam02FromCie1931.SetColumn(1, new Vector4(0.4296f, 1.6975f, 0.0136f, 0.0f));
            coneResponseCiecam02FromCie1931.SetColumn(2, new Vector4(-0.1624f, 0.0061f, 0.9834f, 0.0f));
            coneResponseCiecam02FromCie1931.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            Matrix4x4 cie1931FromConeResponseCiecam02 = new Matrix4x4();
            cie1931FromConeResponseCiecam02.SetColumn(0, new Vector4(1.096124f, 0.4543691f, -0.009627610f, 0.0f));
            cie1931FromConeResponseCiecam02.SetColumn(1, new Vector4(-0.278869f, 0.4735332f, -0.005698032f, 0.0f));
            cie1931FromConeResponseCiecam02.SetColumn(2, new Vector4(0.1827452f, 0.07209781f, 1.015326f, 0.0f));
            cie1931FromConeResponseCiecam02.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            Vector3 xyzSource = new Vector3(
                chromacityWhiteSource.x,
                chromacityWhiteSource.y,
                1.0f - (chromacityWhiteSource.x + chromacityWhiteSource.y)
            );

            Vector3 xyzDestination = new Vector3(
                chromacityWhiteDestination.x,
                chromacityWhiteDestination.y,
                1.0f - (chromacityWhiteDestination.x + chromacityWhiteDestination.y)
            );

            Vector3 coneResponseSource = coneResponseCiecam02FromCie1931.MultiplyVector(xyzSource);
            Vector3 coneResponseDestination = coneResponseCiecam02FromCie1931.MultiplyVector(xyzDestination);
            Vector3 gain;
            gain.x = coneResponseDestination.x / coneResponseSource.x;
            gain.y = coneResponseDestination.y / coneResponseSource.y;
            gain.z = coneResponseDestination.z / coneResponseSource.z;


            Matrix4x4 gainMatrix = Matrix4x4.Scale(gain);

            Matrix4x4 cie1931FromRgb = new Matrix4x4();
            cie1931FromRgb.SetColumn(0, new Vector4(0.4124f, 0.2126f, 0.0193f, 0.0f));
            cie1931FromRgb.SetColumn(1, new Vector4(0.3576f, 0.7152f, 0.1192f, 0.0f));
            cie1931FromRgb.SetColumn(2, new Vector4(0.1805f, 0.0722f, 0.9505f, 0.0f));
            cie1931FromRgb.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            Matrix4x4 rgbFromCie1931 = new Matrix4x4();
            rgbFromCie1931.SetColumn(0, new Vector4(3.2405f, -0.9693f, 0.0556f, 0.0f));
            rgbFromCie1931.SetColumn(1, new Vector4(-1.5371f, 1.8760f, -0.2040f, 0.0f));
            rgbFromCie1931.SetColumn(2, new Vector4(-0.4985f, 0.0416f, 1.0572f, 0.0f));
            rgbFromCie1931.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            Matrix4x4 a = coneResponseCiecam02FromCie1931 * cie1931FromRgb;
            Matrix4x4 b = rgbFromCie1931 * cie1931FromConeResponseCiecam02;
            Matrix4x4 chromaticAdaptation = b * gainMatrix * a;

            return chromaticAdaptation;
        }
    }
}