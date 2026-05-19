using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Serialization;

namespace UnityEditor.VFX
{
    class VFXResources : ScriptableObject
    {
        public static class defaultResources
        {
            static VFXResources s_Instance => UnityEngine.VFX.VFXManager.editorResources as VFXResources;

            public static AnimationCurve animationCurve
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.animationCurve;
                    return null;
                }
            }
            public static Gradient gradient
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.gradient;
                    return null;
                }
            }
            public static Gradient gradientMapRamp
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.gradientMapRamp;
                    return null;
                }
            }

            public static Shader StaticMeshShader
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.staticMeshShader;
                    return null;
                }
            }
            public static ShaderGraphVfxAsset shaderGraphVfx
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.shaderGraphVfx;
                    return null;
                }
            }

            public static Texture2D particleTexture
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.particleTexture;
                    return null;
                }
            }

            public static Texture2D normalTexture
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.normalTexture;
                    return null;
                }
            }

            public static Texture2D maskTexture
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.maskTexture;
                    return null;
                }
            }

            public static Texture2D noiseTexture
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.noiseTexture;
                    return null;
                }
            }

            public static Texture2D sixWayPositiveTexture
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.sixWayPositiveTexture;
                    return null;
                }
            }

            public static Texture2D sixWayNegativeTexture
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.sixWayNegativeTexture;
                    return null;
                }
            }
            public static Texture3D vectorField
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.vectorField;
                    return null;
                }
            }
            public static Texture3D signedDistanceField
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.signedDistanceField;
                    return null;
                }
            }

            public static Mesh mesh
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.mesh;
                    return null;
                }
            }

            public static ShaderGraphVfxAsset errorFallbackShaderGraph
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.errorFallbackShaderGraph;
                    return null;
                }
            }

            public static Texture3D tileableGradientNoise
            {
                get
                {
                    if (s_Instance != null)
                        return s_Instance.tileableGradientNoise;
                    return null;
                }
            }
        }


        [SerializeField]
        AnimationCurve animationCurve = null;

        [SerializeField]
        Gradient gradient = null;

        [SerializeField]
        Gradient gradientMapRamp = null;

        [SerializeField]
        Texture2D particleTexture = null;

        [SerializeField]
        Texture2D maskTexture = null;

        [SerializeField]
        Texture2D normalTexture = null;

        [SerializeField]
        Texture2D noiseTexture = null;

        [SerializeField]
        Texture2D sixWayPositiveTexture = null;

        [SerializeField]
        Texture2D sixWayNegativeTexture = null;

        [SerializeField]
        Texture3D vectorField = null;

        [SerializeField]
        Texture3D signedDistanceField = null;

        [SerializeField]
        Texture3D tileableGradientNoise = null;

        [SerializeField]
        Mesh mesh = null;

        [SerializeField]
        ShaderGraphVfxAsset shaderGraphVfx = null;

        [SerializeField]
        ShaderGraphVfxAsset errorFallbackShaderGraph = null;

        [FormerlySerializedAs("shader"), SerializeField]
        Shader staticMeshShader = null;

        public static VFXResources CreateDefault()
        {
            var defaultShader = Shader.Find("Shader Graphs/VFXDefaultSingleMesh");
            var defaultAnimationCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
            var defaultGradient = new Gradient();
            defaultGradient.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.white, 1.0f),
            };
            defaultGradient.alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.1f),
                new GradientAlphaKey(0.8f, 0.8f),
                new GradientAlphaKey(0.0f, 1.0f),
            };

            var defaultGradientMapRamp = new Gradient();
            defaultGradientMapRamp.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.0f,    0.0f,   0.0f),  0.0f),
                new GradientColorKey(new Color(0.75f,   0.15f,  0.0f),  0.3f),
                new GradientColorKey(new Color(1.25f,   0.56f,  0.12f), 0.5f),
                new GradientColorKey(new Color(3.5f,    2.0f,   0.5f),  0.7f),
                new GradientColorKey(new Color(4.0f,    3.5f,   1.2f),  0.9f),
                new GradientColorKey(new Color(12.0f,   10.0f,  2.5f),  1.0f),
            };
            defaultGradientMapRamp.alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f),
            };

            var defaultPath = VisualEffectGraphPackageInfo.assetPackagePath + "/";
            var defaultShaderGraphVfx = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(defaultPath + "ShaderGraph/VFXDefault.shadergraph");
            var defaultParticleTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(defaultPath + "Textures/DefaultDot.tga");
            var defaultNormalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(defaultPath + "Textures/DefaultNormal.tga");
            var defaultMaskTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(defaultPath + "Textures/DefaultMasks.tga");
            var defaultNoiseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(defaultPath + "Textures/Noise.tga");
            var defaultSixWayPositiveTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(defaultPath + "Textures/Default6Way_P.tga");
            var defaultSixWayNegativeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(defaultPath + "Textures/Default6Way_N.tga");
            var defaultVectorField = AssetDatabase.LoadAssetAtPath<Texture3D>(defaultPath + "Textures/vectorfield.asset");
            var defaultSignedDistanceField = AssetDatabase.LoadAssetAtPath<Texture3D>(defaultPath + "Textures/SignedDistanceField.asset");
            var defaultErrorFallbackShaderGraph = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(defaultPath + "ShaderGraph/VFXErrorFallback.shadergraph");
            var defaultTileableGradientNoise = AssetDatabase.LoadAssetAtPath<Texture3D>(defaultPath + "Textures/TileableGradientNoise.asset");
            var defaultMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");

            var vfxResources = ScriptableObject.CreateInstance<VFXResources>();
            vfxResources.animationCurve = defaultAnimationCurve;
            vfxResources.gradient = defaultGradient;
            vfxResources.gradientMapRamp = defaultGradientMapRamp;
            vfxResources.staticMeshShader = defaultShader;
            vfxResources.shaderGraphVfx = defaultShaderGraphVfx;
            vfxResources.particleTexture = defaultParticleTexture;
            vfxResources.normalTexture = defaultNormalTexture;
            vfxResources.maskTexture = defaultMaskTexture;
            vfxResources.noiseTexture = defaultNoiseTexture;
            vfxResources.sixWayPositiveTexture = defaultSixWayPositiveTexture;
            vfxResources.sixWayNegativeTexture = defaultSixWayNegativeTexture;
            vfxResources.vectorField = defaultVectorField;
            vfxResources.signedDistanceField = defaultSignedDistanceField;
            vfxResources.errorFallbackShaderGraph = defaultErrorFallbackShaderGraph;
            vfxResources.tileableGradientNoise = defaultTileableGradientNoise;
            vfxResources.mesh = defaultMesh;

            return vfxResources;
        }
    }
}
