using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    class VFXResources
    {
        public static VFXResources defaultResources
        {
            get
            {
                if (s_Instance == null)
                {
                    Initialize();
                }
                return s_Instance;
            }
        }
        private static VFXResources s_Instance;

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
        private void Initialize()
        {

            VFXResources newAsset = new VFXResources();

            newAsset.shader = Shader.Find("Hidden/Default StaticMeshOutput");

            newAsset.animationCurve = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0.0f, 0.0f, 0.0f, 0.0f),
                new Keyframe(0.25f, 0.25f, 0.0f, 0.0f),
                new Keyframe(1.0f, 0.0f, 0.0f, 0.0f),
            });

            newAsset.gradient = new Gradient();
            newAsset.gradient.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.gray, 1.0f),
            };
            newAsset.gradient.alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.1f),
                new GradientAlphaKey(0.8f, 0.8f),
                new GradientAlphaKey(0.0f, 1.0f),
            };

            newAsset.gradientMapRamp = new Gradient();
            newAsset.gradientMapRamp.colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.0f,    0.0f,   0.0f),  0.0f),
                new GradientColorKey(new Color(0.75f,   0.15f,  0.0f),  0.3f),
                new GradientColorKey(new Color(1.25f,   0.56f,  0.12f), 0.5f),
                new GradientColorKey(new Color(3.5f,    2.0f,   0.5f),  0.7f),
                new GradientColorKey(new Color(4.0f,    3.5f,   1.2f),  0.9f),
                new GradientColorKey(new Color(12.0f,   10.0f,  2.5f),  1.0f),
            };
            newAsset.gradientMapRamp.alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f),
            };

            s_Instance = newAsset;
        }
        Texture2D m_ParticleTexture;
        public Texture2D particleTexture {
            get
            {
                if (m_ParticleTexture == null)
                    m_ParticleTexture = SafeLoadAssetAtPath<Texture2D>(defaultPath + "Textures/DefaultParticle.tga");
                return m_ParticleTexture;
            }
        }

        Texture2D m_NoiseTexture;
        public Texture2D noiseTexture {
            get
            {
                if (m_NoiseTexture == null)
                    m_NoiseTexture = SafeLoadAssetAtPath<Texture2D>(defaultPath + "Textures/Noise.tga");
                return m_NoiseTexture;
            }
        }

        Texture3D m_VectorField;
        public Texture3D vectorField {
            get
            {
                if( m_VectorField == null)
                    m_VectorField = SafeLoadAssetAtPath<Texture3D>(defaultPath + "Textures/vectorfield.asset");
                return m_VectorField;
            }
        }
        Texture3D m_SignedDistanceField;

        public Texture3D signedDistanceField {
            get
            {
                if (m_SignedDistanceField == null)
                    m_SignedDistanceField = SafeLoadAssetAtPath<Texture3D>(defaultPath + "Textures/SignedDistanceField.asset");
                return m_SignedDistanceField;
            }
        }

        Mesh m_Mesh;
        public Mesh mesh {
            get
            {
                if(m_Mesh == null)
                    m_Mesh = Resources.GetBuiltinResource<Mesh>("New-Capsule.fbx");
                return m_Mesh;
            }
        }
        public AnimationCurve animationCurve { get; private set; }
        public Gradient gradient { get; private set; }
        public Gradient gradientMapRamp { get; private set; }
        public Shader shader { get; private set; }
    }
}
