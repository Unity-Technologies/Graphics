using UnityEngine;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    [MovedFrom("UnityEditor.Rendering.LWRP.ShaderGUI")] public static class ParticleGUI
    {
        public enum ColorMode
        {
            Multiply,
            Additive,
            Subtractive,
            Overlay,
            Color,
            Difference
        }

        public static class Styles
        {
            public static GUIContent colorMode = new GUIContent("Color Mode",
                "Controls how the Particle color and the Material color blend together.");

            public static GUIContent flipbookMode = new GUIContent("Flip-Book Blending",
                "Blends the frames in a flip-book together in a smooth animation.");

            public static GUIContent softParticlesEnabled = new GUIContent("Soft Particles",
                "Makes particles fade out when they get close to intersecting with the surface of other geometry in the depth buffer.");

            public static GUIContent softParticlesNearFadeDistanceText =
                new GUIContent("Near",
                    "The distance from the other surface where the particle is completely transparent.");

            public static GUIContent softParticlesFarFadeDistanceText =
                new GUIContent("Far",
                    "The distance from the other surface where the particle is completely opaque.");

            public static GUIContent cameraFadingEnabled = new GUIContent("Camera Fading",
                "Makes particles fade out when they get close to the camera.");

            public static GUIContent cameraNearFadeDistanceText =
                new GUIContent("Near",
                    "The distance from the camera where the particle is completely transparent.");

            public static GUIContent cameraFarFadeDistanceText =
                new GUIContent("Far", "The distance from the camera where the particle is completely opaque.");

            public static GUIContent distortionEnabled = new GUIContent("Distortion",
                "Creates a distortion effect by making particles perform refraction with the objects drawn before them.");

            public static GUIContent distortionStrength = new GUIContent("Strength",
                "Controls how much the Particle distorts the background. ");

            public static GUIContent distortionBlend = new GUIContent("Blend",
                "Controls how visible the distortion effect is. At 0, there’s no visible distortion. At 1, only the distortion effect is visible, not the background.");

            public static GUIContent VertexStreams = new GUIContent("Vertex Streams",
                "The vertex streams needed for this Material to function properly.");

            public static string streamPositionText = "Position (POSITION.xyz)";
            public static string streamNormalText = "Normal (NORMAL.xyz)";
            public static string streamColorText = "Color (COLOR.xyzw)";
            public static string streamUVText = "UV (TEXCOORD0.xy)";
            public static string streamUV2Text = "UV2 (TEXCOORD0.zw)";
            public static string streamAnimBlendText = "AnimBlend (TEXCOORD1.x)";
            public static string streamTangentText = "Tangent (TANGENT.xyzw)";

            public static GUIContent streamApplyToAllSystemsText = new GUIContent("Fix Now",
                "Apply the vertex stream layout to all Particle Systems using this material");

            public static string undoApplyCustomVertexStreams = L10n.Tr("Apply custom vertex streams from material");

            public static GUIStyle vertexStreamIcon = new GUIStyle();
        }

        private static ReorderableList vertexStreamList;

        public struct ParticleProperties
        {
            // Surface Option Props
            public MaterialProperty colorMode;

            // Advanced Props
            public MaterialProperty flipbookMode;
            public MaterialProperty softParticlesEnabled;
            public MaterialProperty cameraFadingEnabled;
            public MaterialProperty distortionEnabled;
            public MaterialProperty softParticlesNearFadeDistance;
            public MaterialProperty softParticlesFarFadeDistance;
            public MaterialProperty cameraNearFadeDistance;
            public MaterialProperty cameraFarFadeDistance;
            public MaterialProperty distortionBlend;
            public MaterialProperty distortionStrength;

            public ParticleProperties(MaterialProperty[] properties)
            {
                // Surface Option Props
                colorMode = BaseShaderGUI.FindProperty("_ColorMode", properties, false);
                // Advanced Props
                flipbookMode = BaseShaderGUI.FindProperty("_FlipbookBlending", properties);
                softParticlesEnabled = BaseShaderGUI.FindProperty("_SoftParticlesEnabled", properties);
                cameraFadingEnabled = BaseShaderGUI.FindProperty("_CameraFadingEnabled", properties);
                distortionEnabled = BaseShaderGUI.FindProperty("_DistortionEnabled", properties, false);
                softParticlesNearFadeDistance = BaseShaderGUI.FindProperty("_SoftParticlesNearFadeDistance", properties);
                softParticlesFarFadeDistance = BaseShaderGUI.FindProperty("_SoftParticlesFarFadeDistance", properties);
                cameraNearFadeDistance = BaseShaderGUI.FindProperty("_CameraNearFadeDistance", properties);
                cameraFarFadeDistance = BaseShaderGUI.FindProperty("_CameraFarFadeDistance", properties);
                distortionBlend = BaseShaderGUI.FindProperty("_DistortionBlend", properties, false);
                distortionStrength = BaseShaderGUI.FindProperty("_DistortionStrength", properties, false);
            }
        }

        public static void SetupMaterialWithColorMode(Material material)
        {
            var colorMode = (ColorMode) material.GetFloat("_ColorMode");

            switch (colorMode)
            {
                case ColorMode.Multiply:
                    material.DisableKeyword("_COLOROVERLAY_ON");
                    material.DisableKeyword("_COLORCOLOR_ON");
                    material.DisableKeyword("_COLORADDSUBDIFF_ON");
                    break;
                case ColorMode.Overlay:
                    material.DisableKeyword("_COLORCOLOR_ON");
                    material.DisableKeyword("_COLORADDSUBDIFF_ON");
                    material.EnableKeyword("_COLOROVERLAY_ON");
                    break;
                case ColorMode.Color:
                    material.DisableKeyword("_COLOROVERLAY_ON");
                    material.DisableKeyword("_COLORADDSUBDIFF_ON");
                    material.EnableKeyword("_COLORCOLOR_ON");
                    break;
                case ColorMode.Difference:
                    material.DisableKeyword("_COLOROVERLAY_ON");
                    material.DisableKeyword("_COLORCOLOR_ON");
                    material.EnableKeyword("_COLORADDSUBDIFF_ON");
                    material.SetVector("_BaseColorAddSubDiff", new Vector4(-1.0f, 1.0f, 0.0f, 0.0f));
                    break;
                case ColorMode.Additive:
                    material.DisableKeyword("_COLOROVERLAY_ON");
                    material.DisableKeyword("_COLORCOLOR_ON");
                    material.EnableKeyword("_COLORADDSUBDIFF_ON");
                    material.SetVector("_BaseColorAddSubDiff", new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                    break;
                case ColorMode.Subtractive:
                    material.DisableKeyword("_COLOROVERLAY_ON");
                    material.DisableKeyword("_COLORCOLOR_ON");
                    material.EnableKeyword("_COLORADDSUBDIFF_ON");
                    material.SetVector("_BaseColorAddSubDiff", new Vector4(-1.0f, 0.0f, 0.0f, 0.0f));
                    break;
            }
        }

        public static void FadingOptions(Material material, MaterialEditor materialEditor, ParticleProperties properties)
        {
            // Z write doesn't work with fading
            bool hasZWrite = (material.GetInt("_ZWrite") != 0);
            if(!hasZWrite)
            {
                // Soft Particles
                {
                    EditorGUI.showMixedValue = properties.softParticlesEnabled.hasMixedValue;
                    var enabled = properties.softParticlesEnabled.floatValue;

                    EditorGUI.BeginChangeCheck();
                    enabled = EditorGUILayout.Toggle(Styles.softParticlesEnabled, enabled != 0.0f) ? 1.0f : 0.0f;
                    if (EditorGUI.EndChangeCheck())
                    {
                        materialEditor.RegisterPropertyChangeUndo("Soft Particles Enabled");
                        properties.softParticlesEnabled.floatValue = enabled;
                    }

                    if (enabled >= 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        BaseShaderGUI.TwoFloatSingleLine(new GUIContent("Surface Fade"),
                            properties.softParticlesNearFadeDistance,
                            Styles.softParticlesNearFadeDistanceText,
                            properties.softParticlesFarFadeDistance,
                            Styles.softParticlesFarFadeDistanceText,
                            materialEditor);
                        EditorGUI.indentLevel--;
                    }
                }

                // Camera Fading
                {
                    EditorGUI.showMixedValue = properties.cameraFadingEnabled.hasMixedValue;
                    var enabled = properties.cameraFadingEnabled.floatValue;

                    EditorGUI.BeginChangeCheck();
                    enabled = EditorGUILayout.Toggle(Styles.cameraFadingEnabled, enabled != 0.0f) ? 1.0f : 0.0f;
                    if (EditorGUI.EndChangeCheck())
                    {
                        materialEditor.RegisterPropertyChangeUndo("Camera Fading Enabled");
                        properties.cameraFadingEnabled.floatValue = enabled;
                    }

                    if (enabled >= 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        BaseShaderGUI.TwoFloatSingleLine(new GUIContent("Distance"),
                            properties.cameraNearFadeDistance,
                            Styles.cameraNearFadeDistanceText,
                            properties.cameraFarFadeDistance,
                            Styles.cameraFarFadeDistanceText,
                            materialEditor);
                        EditorGUI.indentLevel--;
                    }
                }

                // Distortion
                if (properties.distortionEnabled != null)
                {
                    EditorGUI.showMixedValue = properties.distortionEnabled.hasMixedValue;
                    var enabled = properties.distortionEnabled.floatValue;

                    EditorGUI.BeginChangeCheck();
                    enabled = EditorGUILayout.Toggle(Styles.distortionEnabled, enabled != 0.0f) ? 1.0f : 0.0f;
                    if (EditorGUI.EndChangeCheck())
                    {
                        materialEditor.RegisterPropertyChangeUndo("Distortion Enabled");
                        properties.distortionEnabled.floatValue = enabled;
                    }

                    if (enabled >= 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        materialEditor.ShaderProperty(properties.distortionStrength, Styles.distortionStrength);
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.showMixedValue = properties.distortionStrength.hasMixedValue;
                        var blend = EditorGUILayout.Slider(Styles.distortionBlend, properties.distortionBlend.floatValue, 0f, 1f);
                        if(EditorGUI.EndChangeCheck())
                            properties.distortionBlend.floatValue = blend;
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUI.showMixedValue = false;
            }
        }

        public static void DoVertexStreamsArea(Material material, List<ParticleSystemRenderer> renderers, bool useLighting = false)
        {
            EditorGUILayout.Space();
            // Display list of streams required to make this shader work
            bool useNormalMap = false;
            bool useFlipbookBlending = (material.GetFloat("_FlipbookBlending") > 0.0f);
            if(material.HasProperty("_BumpMap"))
                useNormalMap = material.GetTexture("_BumpMap");

            // Build the list of expected vertex streams
            List<ParticleSystemVertexStream> streams = new List<ParticleSystemVertexStream>();
            List<string> streamList = new List<string>();

            streams.Add(ParticleSystemVertexStream.Position);
            streamList.Add(Styles.streamPositionText);

            if (useLighting || useNormalMap)
            {
                streams.Add(ParticleSystemVertexStream.Normal);
                streamList.Add(Styles.streamNormalText);
                if (useNormalMap)
                {
                    streams.Add(ParticleSystemVertexStream.Tangent);
                    streamList.Add(Styles.streamTangentText);
                }
            }

            streams.Add(ParticleSystemVertexStream.Color);
            streamList.Add(Styles.streamColorText);
            streams.Add(ParticleSystemVertexStream.UV);
            streamList.Add(Styles.streamUVText);

            if (useFlipbookBlending)
            {
                streams.Add(ParticleSystemVertexStream.UV2);
                streamList.Add(Styles.streamUV2Text);
                streams.Add(ParticleSystemVertexStream.AnimBlend);
                streamList.Add(Styles.streamAnimBlendText);
            }

            vertexStreamList = new ReorderableList(streamList, typeof(string), false, true, false, false);

            vertexStreamList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Vertex Streams");
            };

            vertexStreamList.DoLayoutList();

            // Display a warning if any renderers have incorrect vertex streams
            string Warnings = "";
            List<ParticleSystemVertexStream> rendererStreams = new List<ParticleSystemVertexStream>();
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                renderer.GetActiveVertexStreams(rendererStreams);
                if (!rendererStreams.SequenceEqual(streams))
                    Warnings += "-" + renderer.name + "\n";
            }

            if (!string.IsNullOrEmpty(Warnings))
            {
                EditorGUILayout.HelpBox(
                    "The following Particle System Renderers are using this material with incorrect Vertex Streams:\n" +
                    Warnings, MessageType.Error, true);
                // Set the streams on all systems using this material
                if (GUILayout.Button(Styles.streamApplyToAllSystemsText, EditorStyles.miniButton, GUILayout.ExpandWidth(true)))
                {
                    Undo.RecordObjects(renderers.Where(r => r != null).ToArray(), Styles.undoApplyCustomVertexStreams);

                    foreach (ParticleSystemRenderer renderer in renderers)
                    {
                        renderer.SetActiveVertexStreams(streams);
                    }
                }
            }
        }

        public static void SetMaterialKeywords(Material material)
        {
            // Setup particle + material color blending
            SetupMaterialWithColorMode(material);
            // Is the material transparent, this is set in BaseShaderGUI
            bool isTransparent = material.GetTag("RenderType", false) == "Transparent";
            // Z write doesn't work with distortion/fading
            bool hasZWrite = (material.GetInt("_ZWrite") != 0);

            material.EnableKeyword("_RECEIVE_SHADOWS_OFF");

            // Flipbook blending
            if (material.HasProperty("_FlipbookBlending"))
            {
                var useFlipbookBlending = (material.GetFloat("_FlipbookBlending") > 0.0f);
                CoreUtils.SetKeyword(material, "_FLIPBOOKBLENDING_ON", useFlipbookBlending);
            }
            // Soft particles
            var useSoftParticles = false;
            if (material.HasProperty("_SoftParticlesEnabled"))
            {
                useSoftParticles = (material.GetFloat("_SoftParticlesEnabled") > 0.0f && isTransparent);
                if (useSoftParticles)
                {
                    var softParticlesNearFadeDistance = material.GetFloat("_SoftParticlesNearFadeDistance");
                    var softParticlesFarFadeDistance = material.GetFloat("_SoftParticlesFarFadeDistance");
                    // clamp values
                    if (softParticlesNearFadeDistance < 0.0f)
                    {
                        softParticlesNearFadeDistance = 0.0f;
                        material.SetFloat("_SoftParticlesNearFadeDistance", 0.0f);
                    }

                    if (softParticlesFarFadeDistance < 0.0f)
                    {
                        softParticlesFarFadeDistance = 0.0f;
                        material.SetFloat("_SoftParticlesFarFadeDistance", 0.0f);
                    }
                    // set keywords
                    material.SetVector("_SoftParticleFadeParams",
                        new Vector4(softParticlesNearFadeDistance,
                            1.0f / (softParticlesFarFadeDistance - softParticlesNearFadeDistance), 0.0f, 0.0f));
                }
                else
                {
                    material.SetVector("_SoftParticleFadeParams", new Vector4(0.0f, 0.0f, 0.0f, 0.0f));
                }
                CoreUtils.SetKeyword(material, "_SOFTPARTICLES_ON", useSoftParticles);
            }
            // Camera fading
            var useCameraFading = false;
            if (material.HasProperty("_CameraFadingEnabled") && isTransparent)
            {
                useCameraFading = (material.GetFloat("_CameraFadingEnabled") > 0.0f);
                if (useCameraFading)
                {
                    var cameraNearFadeDistance = material.GetFloat("_CameraNearFadeDistance");
                    var cameraFarFadeDistance = material.GetFloat("_CameraFarFadeDistance");
                    // clamp values
                    if (cameraNearFadeDistance < 0.0f)
                    {
                        cameraNearFadeDistance = 0.0f;
                        material.SetFloat("_CameraNearFadeDistance", 0.0f);
                    }

                    if (cameraFarFadeDistance < 0.0f)
                    {
                        cameraFarFadeDistance = 0.0f;
                        material.SetFloat("_CameraFarFadeDistance", 0.0f);
                    }
                    // set keywords
                    material.SetVector("_CameraFadeParams",
                        new Vector4(cameraNearFadeDistance, 1.0f / (cameraFarFadeDistance - cameraNearFadeDistance),
                            0.0f, 0.0f));
                }
                else
                {
                    material.SetVector("_CameraFadeParams", new Vector4(0.0f, Mathf.Infinity, 0.0f, 0.0f));
                }
            }
            // Distortion
            if (material.HasProperty("_DistortionEnabled"))
            {
                var useDistortion = (material.GetFloat("_DistortionEnabled") > 0.0f) && isTransparent;
                CoreUtils.SetKeyword(material, "_DISTORTION_ON", useDistortion);
                if (useDistortion)
                    material.SetFloat("_DistortionStrengthScaled", material.GetFloat("_DistortionStrength") * 0.1f);
            }

            var useFading = (useSoftParticles || useCameraFading) && !hasZWrite;
            CoreUtils.SetKeyword(material, "_FADING_ON", useFading);
        }
    }
} // namespace UnityEditor
