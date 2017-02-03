using System;
using System.IO;
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using GradingType = PostProcessing.ColorGradingSettings.GradingType;
    using EyeAdaptationType = PostProcessing.EyeAdaptationSettings.EyeAdaptationType;

    [CustomEditor(typeof(PostProcessing))]
    public class PostProcessingEditor : Editor
    {
        #region Serialized settings
        public class ColorGradingSettings
        {
            public SerializedProperty type;
            public SerializedProperty exposure;

            public SerializedProperty logLut;

            public SerializedProperty neutralBlackIn;
            public SerializedProperty neutralWhiteIn;
            public SerializedProperty neutralBlackOut;
            public SerializedProperty neutralWhiteOut;
            public SerializedProperty neutralWhiteLevel;
            public SerializedProperty neutralWhiteClip;
        }

        public class EyeAdaptationSettings
        {
            public SerializedProperty enabled;
            public SerializedProperty showDebugHistogramInGameView;

            public SerializedProperty lowPercent;
            public SerializedProperty highPercent;

            public SerializedProperty minLuminance;
            public SerializedProperty maxLuminance;
            public SerializedProperty exposureCompensation;

            public SerializedProperty adaptationType;

            public SerializedProperty speedUp;
            public SerializedProperty speedDown;

            public SerializedProperty logMin;
            public SerializedProperty logMax;
        }

        public class ChromaticAberrationSettings
        {
            public SerializedProperty enabled;
            public SerializedProperty spectralTexture;
            public SerializedProperty intensity;
        }

        public class VignetteSettings
        {
            public SerializedProperty enabled;
            public SerializedProperty color;
            public SerializedProperty center;
            public SerializedProperty intensity;
            public SerializedProperty smoothness;
        }

        public class BloomSettings
        {
            public SerializedProperty enabled;
            public SerializedProperty intensity;
            public SerializedProperty threshold;
            public SerializedProperty softKnee;
            public SerializedProperty radius;
            public SerializedProperty lensTexture;
            public SerializedProperty lensIntensity;
        }
        #endregion

        public ColorGradingSettings colorGrading;
        public EyeAdaptationSettings eyeAdaptation;
        public ChromaticAberrationSettings chromaSettings;
        public VignetteSettings vignetteSettings;
        public BloomSettings bloomSettings;
        public SerializedProperty globalDithering;

        SerializedProperty FindProperty<TValue>(Expression<Func<PostProcessing, TValue>> expr)
        {
            var path = Utilities.GetFieldPath(expr);
            return serializedObject.FindProperty(path);
        }

        void OnEnable()
        {
            colorGrading = new ColorGradingSettings
            {
                type = FindProperty(x => x.colorGrading.type),
                exposure = FindProperty(x => x.colorGrading.exposure),

                logLut = FindProperty(x => x.colorGrading.logLut),

                neutralBlackIn = FindProperty(x => x.colorGrading.neutralBlackIn),
                neutralWhiteIn = FindProperty(x => x.colorGrading.neutralWhiteIn),
                neutralBlackOut = FindProperty(x => x.colorGrading.neutralBlackOut),
                neutralWhiteOut = FindProperty(x => x.colorGrading.neutralWhiteOut),
                neutralWhiteLevel = FindProperty(x => x.colorGrading.neutralWhiteLevel),
                neutralWhiteClip = FindProperty(x => x.colorGrading.neutralWhiteClip)
            };

            eyeAdaptation = new EyeAdaptationSettings
            {
                enabled = FindProperty(x => x.eyeAdaptation.enabled),
                showDebugHistogramInGameView = FindProperty(x => x.eyeAdaptation.showDebugHistogramInGameView),

                lowPercent = FindProperty(x => x.eyeAdaptation.lowPercent),
                highPercent = FindProperty(x => x.eyeAdaptation.highPercent),

                minLuminance = FindProperty(x => x.eyeAdaptation.minLuminance),
                maxLuminance = FindProperty(x => x.eyeAdaptation.maxLuminance),
                exposureCompensation = FindProperty(x => x.eyeAdaptation.exposureCompensation),

                adaptationType = FindProperty(x => x.eyeAdaptation.adaptationType),

                speedUp = FindProperty(x => x.eyeAdaptation.speedUp),
                speedDown = FindProperty(x => x.eyeAdaptation.speedDown),

                logMin = FindProperty(x => x.eyeAdaptation.logMin),
                logMax = FindProperty(x => x.eyeAdaptation.logMax)
            };

            chromaSettings = new ChromaticAberrationSettings
            {
                enabled = FindProperty(x => x.chromaSettings.enabled),
                spectralTexture = FindProperty(x => x.chromaSettings.spectralTexture),
                intensity = FindProperty(x => x.chromaSettings.intensity)
            };

            vignetteSettings = new VignetteSettings
            {
                enabled = FindProperty(x => x.vignetteSettings.enabled),
                color = FindProperty(x => x.vignetteSettings.color),
                center = FindProperty(x => x.vignetteSettings.center),
                intensity = FindProperty(x => x.vignetteSettings.intensity),
                smoothness = FindProperty(x => x.vignetteSettings.smoothness)
            };

            bloomSettings = new BloomSettings
            {
                enabled = FindProperty(x => x.bloomSettings.enabled),

                intensity = FindProperty(x => x.bloomSettings.intensity),
                threshold = FindProperty(x => x.bloomSettings.threshold),

                softKnee = FindProperty(x => x.bloomSettings.softKnee),
                radius = FindProperty(x => x.bloomSettings.radius),

                lensTexture = FindProperty(x => x.bloomSettings.lensTexture),
                lensIntensity = FindProperty(x => x.bloomSettings.lensIntensity)
            };

            globalDithering = FindProperty(x => x.globalDithering);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Do("Color Grading", ColorGradingUI);
            Do("Eye Adaptation", EyeAdaptationUI);
            Do("Bloom", BloomUI);
            Do("Chromatic Aberration", ChromaticAberrationUI);
            Do("Vignette", VignetteUI);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(globalDithering);

            serializedObject.ApplyModifiedProperties();
        }

        void Do(string header, Action func)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            if (func != null)
                func();

            EditorGUI.indentLevel--;
        }

        void ColorGradingUI()
        {
            var camera = (target as PostProcessing).GetComponent<Camera>();
            if (camera != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(EditorGUI.indentLevel * 15f);
                    if (GUILayout.Button("Export frame to EXR", EditorStyles.miniButton))
                    {
                        string path = EditorUtility.SaveFilePanelInProject("Export frame as EXR...", "Frame.exr", "exr", "");

                        if (!string.IsNullOrEmpty(path))
                            SaveFrameToEXR(camera, path);
                    }
                }
            }

            EditorGUILayout.PropertyField(colorGrading.exposure);
            EditorGUILayout.PropertyField(colorGrading.type);

            EditorGUI.indentLevel++;

            var gradingType = (GradingType)colorGrading.type.intValue;

            if (gradingType == GradingType.Custom)
            {
                EditorGUILayout.PropertyField(colorGrading.logLut);

                if (!ValidateLutImportSettings())
                {
                    EditorGUILayout.HelpBox("Invalid LUT import settings.", MessageType.Warning);

                    GUILayout.Space(-32);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Fix", GUILayout.Width(60)))
                        {
                            SetLUTImportSettings();
                            AssetDatabase.Refresh();
                        }
                        GUILayout.Space(8);
                    }
                    GUILayout.Space(11);
                }
            }
            else if (gradingType == GradingType.Neutral)
            {
                EditorGUILayout.PropertyField(colorGrading.neutralBlackIn);
                EditorGUILayout.PropertyField(colorGrading.neutralWhiteIn);
                EditorGUILayout.PropertyField(colorGrading.neutralBlackOut);
                EditorGUILayout.PropertyField(colorGrading.neutralWhiteOut);
                EditorGUILayout.PropertyField(colorGrading.neutralWhiteLevel);
                EditorGUILayout.PropertyField(colorGrading.neutralWhiteClip);
            }

            EditorGUI.indentLevel--;
        }

        void EyeAdaptationUI()
        {
            EditorGUILayout.PropertyField(eyeAdaptation.enabled);

            if (eyeAdaptation.enabled.boolValue)
            {
                EditorGUILayout.PropertyField(eyeAdaptation.showDebugHistogramInGameView);

                EditorGUILayout.PropertyField(eyeAdaptation.logMin, new GUIContent("Histogram Log Min"));
                EditorGUILayout.PropertyField(eyeAdaptation.logMax, new GUIContent("Histogram Log Max"));
                EditorGUILayout.Space();

                float low = eyeAdaptation.lowPercent.floatValue;
                float high = eyeAdaptation.highPercent.floatValue;

                EditorGUILayout.MinMaxSlider(new GUIContent("Filter"), ref low, ref high, 1f, 99f);

                eyeAdaptation.lowPercent.floatValue = low;
                eyeAdaptation.highPercent.floatValue = high;

                EditorGUILayout.PropertyField(eyeAdaptation.minLuminance);
                EditorGUILayout.PropertyField(eyeAdaptation.maxLuminance);
                EditorGUILayout.PropertyField(eyeAdaptation.exposureCompensation);

                EditorGUILayout.PropertyField(eyeAdaptation.adaptationType);

                if (eyeAdaptation.adaptationType.intValue == (int)EyeAdaptationType.Progressive)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(eyeAdaptation.speedUp);
                    EditorGUILayout.PropertyField(eyeAdaptation.speedDown);
                    EditorGUI.indentLevel--;
                }
            }
        }

        void BloomUI()
        {
            EditorGUILayout.PropertyField(bloomSettings.enabled);

            if (bloomSettings.enabled.boolValue)
            {
                EditorGUILayout.PropertyField(bloomSettings.intensity);
                EditorGUILayout.PropertyField(bloomSettings.threshold);

                EditorGUILayout.PropertyField(bloomSettings.softKnee);
                EditorGUILayout.PropertyField(bloomSettings.radius);

                EditorGUILayout.PropertyField(bloomSettings.lensTexture);
                EditorGUILayout.PropertyField(bloomSettings.lensIntensity);

                bloomSettings.intensity.floatValue = Mathf.Max(0f, bloomSettings.intensity.floatValue);
                bloomSettings.threshold.floatValue = Mathf.Max(0f, bloomSettings.threshold.floatValue);
                bloomSettings.lensIntensity.floatValue = Mathf.Max(0f, bloomSettings.lensIntensity.floatValue);
            }
        }

        void ChromaticAberrationUI()
        {
            EditorGUILayout.PropertyField(chromaSettings.enabled);

            if (chromaSettings.enabled.boolValue)
            {
                EditorGUILayout.PropertyField(chromaSettings.spectralTexture);
                EditorGUILayout.PropertyField(chromaSettings.intensity);
            }
        }

        void VignetteUI()
        {
            EditorGUILayout.PropertyField(vignetteSettings.enabled);

            if (vignetteSettings.enabled.boolValue)
            {
                EditorGUILayout.PropertyField(vignetteSettings.color);
                EditorGUILayout.PropertyField(vignetteSettings.center);
                EditorGUILayout.PropertyField(vignetteSettings.intensity);
                EditorGUILayout.PropertyField(vignetteSettings.smoothness);
            }
        }

        #region Color grading stuff
        void SetLUTImportSettings()
        {
            var lut = (target as PostProcessing).colorGrading.logLut;
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(lut));
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.anisoLevel = 0;
            importer.sRGBTexture = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            AssetDatabase.Refresh();
        }

        bool ValidateLutImportSettings()
        {
            var lut = (target as PostProcessing).colorGrading.logLut;

            if (lut == null)
                return true;

            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(lut));
            return importer.anisoLevel == 0
                && importer.mipmapEnabled == false
                && importer.sRGBTexture == false
                && (importer.textureCompression == TextureImporterCompression.Uncompressed)
                && importer.filterMode == FilterMode.Bilinear;
        }

        void SaveFrameToEXR(Camera camera, string path)
        {
            // We want a 1024x32 sized render at a minimum so that we have enough space to stamp the lut
            var aspect = (float)camera.pixelHeight / (float)camera.pixelWidth;
            var width = camera.pixelWidth >= 1024
                ? camera.pixelWidth
                : 1024;
            var height = Mathf.RoundToInt(width * aspect);

            var texture = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true);
            var targetRt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);

            // Render the current frame without post processing
            var oldPPState = (target as PostProcessing).enabled;
            (target as PostProcessing).enabled = false;
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            camera.targetTexture = targetRt;
            camera.Render();

            // Stamp the log lut in the top left corner
            const int k_InternalLogLutSize = 32;
            var stampMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/LutGen");
            stampMaterial.SetVector("_LutParams", new Vector4(
                    k_InternalLogLutSize,
                    0.5f / (k_InternalLogLutSize * k_InternalLogLutSize),
                    0.5f / k_InternalLogLutSize,
                    k_InternalLogLutSize / (k_InternalLogLutSize - 1f))
                );

            var stampRt = RenderTexture.GetTemporary(1024, 32, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            Graphics.Blit(null, stampRt, stampMaterial, 0);

            RenderTexture.active = targetRt;
            GL.PushMatrix();
            {
                GL.LoadPixelMatrix(0, width, height, 0);
                Graphics.DrawTexture(new Rect(0, 0, stampRt.width, stampRt.height), stampRt);
            }
            GL.PopMatrix();

            // Read back
            texture.ReadPixels(new Rect(0, 0, targetRt.width, targetRt.height), 0, 0);
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            (target as PostProcessing).enabled = oldPPState;

            // Cleanup
            RenderTexture.ReleaseTemporary(stampRt);
            RenderTexture.ReleaseTemporary(targetRt);
            Utilities.Destroy(stampMaterial);

            // Save
            File.WriteAllBytes(path, texture.EncodeToEXR(Texture2D.EXRFlags.CompressPIZ));
            AssetDatabase.Refresh();
        }
        #endregion
    }
}
