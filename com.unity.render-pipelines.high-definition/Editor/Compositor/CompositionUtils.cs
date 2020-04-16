using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Compositor;

using UnityEditor;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class CompositionUtils
    {
        public static readonly string k_DefaultCameraName = "MainCompositorCamera";

        static public void LoadDefaultCompositionGraph(CompositionManager compositor)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Compositor"))
            {
                AssetDatabase.CreateFolder("Assets", "Compositor");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            string path = "Assets/Compositor/DefaultCompositionGraph.shadergraph";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            bool ret1 = AssetDatabase.CopyAsset(HDUtils.GetHDRenderPipelinePath() + "Runtime/Compositor/ShaderGraphs/DefaultCompositionGraph.shadergraph", path);
            if (ret1 == false)
            {
                Debug.LogError("Error creating default shader graph");
                return;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            compositor.shader = AssetDatabase.LoadAssetAtPath<Shader>(path);

            string profilePath;
            {
                var fullpath = AssetDatabase.GetAssetPath(compositor.shader);
                profilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fullpath), System.IO.Path.GetFileNameWithoutExtension(compositor.shader.name)) + ".asset";
                profilePath = AssetDatabase.GenerateUniqueAssetPath(profilePath);
            }
            
            bool ret2 = AssetDatabase.CopyAsset(HDUtils.GetHDRenderPipelinePath() + "Runtime/Compositor/ShaderGraphs/DefaultCompositionGraph.asset", profilePath);
            if (ret2 == false)
            {
                Debug.LogError("Error creating default profile");
                return;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static public void RemoveAudioListeners(Camera camera)
        {
            var listener = camera.GetComponent<AudioListener>();
            if (listener)
            {
                CoreUtils.Destroy(listener);
            }
        }

        static public void SetDefaultCamera(CompositionManager compositor)
        {
            var camera = CompositionManager.GetSceceCamera();
            if (camera != null)
            {
                var outputCamera = Object.Instantiate(camera);
                RemoveAudioListeners(outputCamera);
                outputCamera.name = k_DefaultCameraName;
                outputCamera.tag = "Untagged";
                outputCamera.cullingMask = 0; // we don't want to render any 3D objects on the compositor camera
                compositor.outputCamera = outputCamera;
            }
        }

        static public void SetDefaultLayers(CompositionManager compositor)
        {
            for (int i = compositor.numLayers - 1; i>=0; --i)
            {
                if (compositor.layers[i].outputTarget == CompositorLayer.OutputTarget.CompositorLayer)
                {
                    if ((i+i < compositor.numLayers - 1) && (compositor.layers[i+1].outputTarget == CompositorLayer.OutputTarget.CameraStack))
                    {
                        continue;
                    }
                    compositor.AddNewLayer(i + 1);
                }
            }
        }

        static public void LoadOrCreateCompositionProfileAsset(CompositionManager compositor)
        {
            var shader = compositor.shader;
            var fullpath = AssetDatabase.GetAssetPath(shader);
            var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fullpath), System.IO.Path.GetFileNameWithoutExtension(shader.name)) + ".asset";

            CompositionProfile newProfile = AssetDatabase.LoadAssetAtPath<CompositionProfile>(path);

            if (newProfile == null)
            {
                Debug.Log($"Creating new composition profile asset at path: {path}");

                newProfile = ScriptableObject.CreateInstance<CompositionProfile>();

                //Note: no need to GenerateUniqueAssetPath(path), since we know that LoadAssetAtPath failed at this path
                AssetDatabase.CreateAsset(newProfile, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log($"Loading composition profile from {path}");
            }
            compositor.profile = newProfile;
        }
    }
}
