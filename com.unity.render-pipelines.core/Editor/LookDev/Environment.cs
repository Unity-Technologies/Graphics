using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

namespace UnityEditor.Rendering.LookDev
{
    //[CreateAssetMenu(fileName = "Environment", menuName = "LookDev/Environment", order = 1)]
    public class Environment : ScriptableObject
    {
        [Serializable]
        public class Shadow
        {
            public Cubemap cubemap; //[TODO: check]
            // Setup default position to be on the sun in the default HDRI.
            // This is important as the defaultHDRI don't call the set brightest spot function on first call.
            public float angleOffset = 0.0f; //[TODO: sync with sky]
            //public SphericalHarmonicsL2 shadowAmbientProbe; //[TODO: check interest for shadow]
            [SerializeField]
            private float m_Latitude = 60.0f; // [-90..90]
            [SerializeField]
            private float m_Longitude = 299.0f; // [0..360]
            [field: SerializeField]
            private float intensity { get; set; } = 1.0f;
            [field: SerializeField]
            public Color color { get; set; } = Color.white;

            public float latitude
            {
                get => m_Latitude;
                set { m_Latitude = value; ConformLatLong(); }
            }

            public float longitude
            {
                get => m_Longitude;
                set { m_Longitude = value; ConformLatLong(); }
            }

            private void ConformLatLong()
            {
                // Clamp latitude to [-90..90]
                if (m_Latitude < -90.0f)
                    m_Latitude = -90.0f;
                if (m_Latitude > 89.0f)
                    m_Latitude = 89.0f;

                // wrap longitude around
                m_Longitude = m_Longitude % 360.0f;
                if (m_Longitude < 0.0)
                    m_Longitude = 360.0f + m_Longitude;
            }
        }

        [Serializable]
        public class Sky
        {
            public Cubemap cubemap;
            public float angleOffset = 0.0f;
            public SphericalHarmonicsL2 ambientProbe;
        }

        public Sky sky = new Sky();
        public Shadow shadow = new Shadow();
    }

    //[CreateAssetMenu(fileName = "EnvironmentLibrary", menuName = "LookDev/EnvironmentLibrary", order = 1)]
    public class EnvironmentLibrary : ScriptableObject
    {
        [field: SerializeField]
        List<Environment> environments { get; set; } = new List<Environment>();

        public int Count => environments.Count;
        public Environment this[int index] => environments[index];

        public void Add()
        {
            Environment environment = ScriptableObject.CreateInstance<Environment>();
            Undo.RegisterCreatedObjectUndo(environment, "Add Environment");

            // Store this new environment as a subasset so we can reference it safely afterwards.
            AssetDatabase.AddObjectToAsset(environment, this);

            environments.Add(environment);

            // Force save / refresh. Important to do this last because SaveAssets can cause effect to become null!
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        public void Remove(int index)
        {
            Environment environment = environments[index];
            Undo.RecordObject(this, "Remove Environment");
            environments.RemoveAt(index);
            Undo.DestroyObjectImmediate(environment);

            // Force save / refresh
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }

    [CustomEditor(typeof(Environment))]
    class EnvironmentEditor : Editor
    {
        override public Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height)
        {
            height = height >> 1; //quick *.5
            Environment environment = (target as Environment);

            RenderTexture oldActive = RenderTexture.active;
            //[TODO: optimize RenderTexture creation]
            RenderTexture temporaryRT = new RenderTexture(width, height, 0);
            RenderTexture.active = temporaryRT;
            EnvironmentUtil.cubeToLatlongMaterial.SetTexture("_MainTex", environment.sky.cubemap);
            EnvironmentUtil.cubeToLatlongMaterial.SetVector("_WindowParams", new Vector4(height, -1000.0f, 2, 1.0f)); // Doesn't matter but let's match DrawLatLongThumbnail settings,-1000.0f to be sure to not have clipping issue (we should not clip normally but don't want to create a new shader)
            EnvironmentUtil.cubeToLatlongMaterial.SetVector("_CubeToLatLongParams", new Vector4(Mathf.Deg2Rad * environment.sky.angleOffset, 0.5f, 1.0f, 0.0f));
            EnvironmentUtil.cubeToLatlongMaterial.SetPass(0);
            GL.LoadPixelMatrix(0, width, height, 0);
            GL.Clear(true, true, default);
            Renderer.DrawFullScreenQuad(new Rect(0, 0, width, height));
            Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            result.Apply(false);
            RenderTexture.active = oldActive;
            DestroyImmediate(temporaryRT);
            return result;
        }
    }

    [CustomEditor(typeof(EnvironmentLibrary))]
    class EnvironmentLibraryEditor : Editor
    {

        // We don't want users to edit these in the inspector
        public override void OnInspectorGUI()
        {
            //DrawDefaultInspector();
            //if (GUILayout.Button("Add"))
            //{
            //    (target as EnvironmentLibrary).Add();
            //}
        }
    }

    class EnvironmentUtil
    {
        static public Material cubeToLatlongMaterial = new Material(Shader.Find("Hidden/LookDev/CubeToLatlong"));
        static public Material cubemapMaterial = new Material(Shader.Find("Skybox/Cubemap"));
    }
}
