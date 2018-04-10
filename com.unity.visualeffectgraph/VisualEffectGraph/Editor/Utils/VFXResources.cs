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

        private const string defaultFileName = "VFXDefaultResources.asset";
        private const string defaultPath = "Assets/VFXEditor/Editor/"; // Change this to a getter once we handle package mode paths

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VFXResources>(defaultPath + defaultFileName);

            if (asset == null)
            {
                Debug.LogWarning("Could not find " + defaultFileName + ", creating...");
                VFXResources newAsset = CreateInstance<VFXResources>();
                AssetDatabase.CreateAsset(newAsset, defaultPath + defaultFileName);
            }
            else
            {
                s_Instance = asset;
            }
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
