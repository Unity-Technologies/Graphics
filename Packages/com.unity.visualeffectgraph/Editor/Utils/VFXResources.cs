using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    class VFXResources : ScriptableObject
    {
        public static Values defaultResources
        {
            get
            {
                if (s_Values == null)
                {
                    Initialize();
                }
                return s_Values;
            }
        }
        private static bool m_Searched; // the instance has been searched and it is null
        private static VFXResources s_Instance;
        private static Values s_Values;

        static void LoadUserResourcesIfNeeded()
        {
            if (s_Instance == null && (!m_Searched || !object.ReferenceEquals(s_Instance, null)))
            // if instance is null and either it has never been searched or it was found but it has been destroyed since last time
            {
                foreach (var guid in AssetDatabase.FindAssets("t:VFXResources"))
                {
                    s_Instance = AssetDatabase.LoadAssetAtPath<VFXResources>(AssetDatabase.GUIDToAssetPath(guid));
                    if (s_Instance != null)
                    {
                        return;
                    }
                }
                s_Instance = null;
                m_Searched = true;
            }
        }

        void OnEnable()
        {
            if (AssetDatabase.FindAssets("t:VFXResources").Length > 1)
                Debug.LogError("Having more than one VFXResources in your project is unsupported");
            s_Instance = this;
            m_Searched = false;
        }

        public class Values
        {
            public AnimationCurve animationCurve
            {
                get
                {
                    LoadUserResourcesIfNeeded();
                    if (s_Instance != null)
                        return s_Instance.animationCurve;

                    return defaultAnimationCurve;
                }
            }
            public Gradient gradient
            {
                get
                {
                    LoadUserResourcesIfNeeded();
                    if (s_Instance != null)
                        return s_Instance.gradient;
                    return defaultGradient;
                }
            }
            public Gradient gradientMapRamp
            {
                get
                {
                    LoadUserResourcesIfNeeded();
                    if (s_Instance != null)
                        return s_Instance.gradientMapRamp;
                    return defaultGradientMapRamp;
                }
            }

            public Shader shader
            {
                get
                {
                    LoadUserResourcesIfNeeded();
                    if (s_Instance != null && s_Instance.shader != null)
                        return s_Instance.shader;

                    return defaultShader;
                }
            }


            public Texture2D particleTexture
            {
                get
                {
                    LoadUserResourcesIfNeeded();
                    if (s_Instance != null && s_Instance.particleTexture != null)
                        return s_Instance.particleTexture;
                    return defaultParticleTexture;
                }
            }

            public Texture2D noiseTexture
            {
                get
                {
                    LoadUserResourcesIfNeeded();
                    if (s_Instance != null && s_Instance.noiseTexture != null)
                        return s_Instance.noiseTexture;
                    return defaultNoiseTexture;
                }
            }
            public Texture3D vectorField
            {
                get
                {
                    LoadUserResourcesIfNeeded();
                    if (s_Instance != null && s_Instance.vectorField != null)
                        return s_Instance.vectorField;
                    return defaultVectorField;
                }
            }
            public Texture3D signedDistanceField
            {
                get
                {
                    LoadUserResourcesIfNeeded();
                    if (s_Instance != null && s_Instance.signedDistanceField != null)
                        return s_Instance.signedDistanceField;
                    return defaultSignedDistanceField;
                }
            }

            public Mesh mesh
            {
                get
                {
                    LoadUserResourcesIfNeeded();
                    if (s_Instance != null && s_Instance.mesh != null)
                        return s_Instance.mesh;

                    return defaultMesh;
                }
            }
        }

        private static string defaultPath { get { return VisualEffectGraphPackageInfo.assetPackagePath + "/"; } }

        private static T SafeLoadAssetAtPath<T>(string assetPath) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            }
            return asset;
        }

        private static void Initialize()
        {
            s_Values = new Values();

            defaultShader = Shader.Find("Shader Graphs/VFXDefault");

            defaultAnimationCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

            defaultGradient = new Gradient();
            defaultGradient.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.gray, 1.0f),
            };
            defaultGradient.alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.1f),
                new GradientAlphaKey(0.8f, 0.8f),
                new GradientAlphaKey(0.0f, 1.0f),
            };

            defaultGradientMapRamp = new Gradient();
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
        }

        static Texture2D m_DefaultParticleTexture;
        public static Texture2D defaultParticleTexture
        {
            get
            {
                if (m_DefaultParticleTexture == null)
                    m_DefaultParticleTexture = SafeLoadAssetAtPath<Texture2D>(defaultPath + "Textures/DefaultDot.tga");
                return m_DefaultParticleTexture;
            }
        }

        static Texture2D m_DefaultNoiseTexture;
        public static Texture2D defaultNoiseTexture
        {
            get
            {
                if (m_DefaultNoiseTexture == null)
                    m_DefaultNoiseTexture = SafeLoadAssetAtPath<Texture2D>(defaultPath + "Textures/Noise.tga");
                return m_DefaultNoiseTexture;
            }
        }

        static Texture3D m_DefaultVectorField;
        public static Texture3D defaultVectorField
        {
            get
            {
                if (m_DefaultVectorField == null)
                    m_DefaultVectorField = SafeLoadAssetAtPath<Texture3D>(defaultPath + "Textures/vectorfield.asset");
                return m_DefaultVectorField;
            }
        }

        static Texture3D m_DefaultSignedDistanceField;
        public static Texture3D defaultSignedDistanceField
        {
            get
            {
                if (m_DefaultSignedDistanceField == null)
                    m_DefaultSignedDistanceField = SafeLoadAssetAtPath<Texture3D>(defaultPath + "Textures/SignedDistanceField.asset");
                return m_DefaultSignedDistanceField;
            }
        }

        static Texture3D m_TileableGradientNoise;
        public static Texture3D tileableGradientNoise
        {
            get
            {
                if (m_TileableGradientNoise == null)
                    m_TileableGradientNoise = SafeLoadAssetAtPath<Texture3D>(defaultPath + "Textures/TileableGradientNoise.asset");
                return m_TileableGradientNoise;
            }
        }

        static Mesh m_DefaultMesh;
        static public Mesh defaultMesh
        {
            get
            {
                if (m_DefaultMesh == null)
                    m_DefaultMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                return m_DefaultMesh;
            }
        }

        private static ComputeShader m_SdfNormalsComputeShader;
        public static ComputeShader sdfNormalsComputeShader
        {
            get
            {
                if (m_SdfNormalsComputeShader == null)
                    m_SdfNormalsComputeShader = SafeLoadAssetAtPath<ComputeShader>(defaultPath + "Shaders/SDFBaker/GenSdfNormals.compute");
                return m_SdfNormalsComputeShader;
            }
        }

        private static ComputeShader m_SdfRayMapComputeShader;
        public static ComputeShader sdfRayMapComputeShader
        {
            get
            {
                if (m_SdfRayMapComputeShader == null)
                    m_SdfRayMapComputeShader = SafeLoadAssetAtPath<ComputeShader>(defaultPath + "Shaders/SDFBaker/GenSdfRayMap.compute");
                return m_SdfRayMapComputeShader;
            }
        }

        private static Shader m_RayMapVoxelizeShader;
        public static Shader rayMapVoxelizeShader
        {
            get
            {
                if (m_RayMapVoxelizeShader == null)
                    m_RayMapVoxelizeShader = SafeLoadAssetAtPath<Shader>(defaultPath + "Shaders/SDFBaker/RayMapVoxelize.shader");
                return m_RayMapVoxelizeShader;
            }
        }

        private static ShaderGraphVfxAsset m_ErrorFallbackShaderGraph;
        public static ShaderGraphVfxAsset errorFallbackShaderGraph
        {
            get
            {
                if (m_ErrorFallbackShaderGraph == null)
                    m_ErrorFallbackShaderGraph = SafeLoadAssetAtPath<ShaderGraphVfxAsset>(defaultPath + "ShaderGraph/VFXErrorFallback.shadergraph");
                return m_ErrorFallbackShaderGraph;
            }
        }

        [SerializeField]
        AnimationCurve animationCurve = null;

        [SerializeField]
        Gradient gradient = null;

        [SerializeField]
        Gradient gradientMapRamp = null;

        [SerializeField]
        Shader shader = null;

        [SerializeField]
        Texture2D particleTexture = null;

        [SerializeField]
        Texture2D noiseTexture = null;

        [SerializeField]
        Texture3D vectorField = null;

        [SerializeField]
        Texture3D signedDistanceField = null;

        [SerializeField]
        Mesh mesh = null;

        static AnimationCurve defaultAnimationCurve;
        static Gradient defaultGradient;
        static Gradient defaultGradientMapRamp;
        static Shader defaultShader;
        static ShaderGraphVfxAsset errorShaderFallback;

        public void SetDefaults()
        {
            if (s_Values == null)
                Initialize();
            animationCurve = defaultAnimationCurve;
            gradient = defaultGradient;
            gradientMapRamp = defaultGradientMapRamp;
        }
    }
}
