using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.VFX
{
    public class VFXResources : ScriptableObject
    {
        public static VFXResources defaultResources { get { return s_Instance; } }
        private static VFXResources s_Instance;

        private const string defaultFileName = "Editor/VFXDefaultResources.asset";
        private const string defaultPath = "Assets/VFXEditor/"; // Change this to a getter once we handle package mode paths

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VFXResources>(defaultPath + defaultFileName);

            if (asset == null)
            {
                Debug.LogWarning("Could not find " + defaultFileName + ", creating...");
                VFXResources newAsset = CreateInstance<VFXResources>();

                newAsset.particleTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(defaultPath + "Textures/DefaultParticle.tga");
                newAsset.noiseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(defaultPath + "Textures/Noise.tga");
                newAsset.vectorField = AssetDatabase.LoadAssetAtPath<Texture3D>(defaultPath + "Textures/vectorfield.asset");
                newAsset.particleMesh = Resources.GetBuiltinResource<Mesh>("New-Capsule.fbx");
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

                AssetDatabase.CreateAsset(newAsset, defaultPath + defaultFileName);
                asset = AssetDatabase.LoadAssetAtPath<VFXResources>(defaultPath + defaultFileName);
            }
            s_Instance = asset;
        }

        [Header("Default Resources")]
        public Texture2D particleTexture;
        public Texture2D noiseTexture;
        public Texture3D vectorField;
        public Mesh particleMesh;
        public AnimationCurve animationCurve;
        public Gradient gradient;
    }
}
