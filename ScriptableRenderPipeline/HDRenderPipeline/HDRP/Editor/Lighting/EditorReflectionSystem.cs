using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.SceneManagement;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public static class EditorReflectionSystem
    {
        static int _Cubemap = Shader.PropertyToID("_Cubemap");

        public static void BakeCustomReflectionProbe(PlanarReflectionProbe probe, bool usePreviousAssetPath, bool custom)
        {
            throw new NotImplementedException();
        }

        public static void BakeCustomReflectionProbe(ReflectionProbe probe, bool usePreviousAssetPath, bool custom)
        {
            if (!custom && probe.bakedTexture != null)
                probe.customBakedTexture = probe.bakedTexture;

            string path = "";
            if (usePreviousAssetPath)
                path = AssetDatabase.GetAssetPath(probe.customBakedTexture);

            string targetExtension = probe.hdr ? "exr" : "png";
            if (string.IsNullOrEmpty(path) || Path.GetExtension(path) != "." + targetExtension)
            {
                // We use the path of the active scene as the target path
                var targetPath = SceneManager.GetActiveScene().path;
                targetPath = Path.Combine(Path.GetDirectoryName(targetPath), Path.GetFileNameWithoutExtension(targetPath));
                if (string.IsNullOrEmpty(targetPath))
                    targetPath = "Assets";
                else if (Directory.Exists(targetPath) == false)
                    Directory.CreateDirectory(targetPath);

                string fileName = probe.name + (probe.hdr ? "-reflectionHDR" : "-reflection") + "." + targetExtension;
                fileName = Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetPath, fileName)));

                path = EditorUtility.SaveFilePanelInProject("Save reflection probe's cubemap.", fileName, targetExtension, "", targetPath);
                if (string.IsNullOrEmpty(path))
                    return;

                ReflectionProbe collidingProbe;
                if (HDReflectionProbeEditorUtility.IsCollidingWithOtherProbes(path, probe, out collidingProbe))
                {
                    if (!EditorUtility.DisplayDialog("Cubemap is used by other reflection probe",
                        string.Format("'{0}' path is used by the game object '{1}', do you really want to overwrite it?",
                            path, collidingProbe.name), "Yes", "No"))
                    {
                        return;
                    }
                }
            }

            EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
            if (!UnityEditor.Lightmapping.BakeReflectionProbe(probe, path))
                Debug.LogError("Failed to bake reflection probe to " + path);
            EditorUtility.ClearProgressBar();
        }

        public static void ResetProbeSceneTextureInMaterial(ReflectionProbe p)
        {
            var renderer = p.GetComponent<Renderer>();
            renderer.sharedMaterial.SetTexture(_Cubemap, p.texture);
        }

        public static void ResetProbeSceneTextureInMaterial(PlanarReflectionProbe p)
        {
            throw new NotImplementedException();
        }

        static MethodInfo k_Lightmapping_BakeReflectionProbeSnapshot = typeof(UnityEditor.Lightmapping).GetMethod("BakeReflectionProbeSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
        public static bool BakeReflectionProbeSnapshot(ReflectionProbe probe)
        {
            return (bool)k_Lightmapping_BakeReflectionProbeSnapshot.Invoke(null, new object[] { probe });
        }

        public static bool BakeReflectionProbeSnapshot(PlanarReflectionProbe probe)
        {
            throw new NotImplementedException();
        }

        static MethodInfo k_Lightmapping_BakeAllReflectionProbesSnapshots = typeof(UnityEditor.Lightmapping).GetMethod("BakeAllReflectionProbesSnapshots", BindingFlags.Static | BindingFlags.NonPublic);
        public static bool BakeAllReflectionProbesSnapshots()
        {
            return (bool)k_Lightmapping_BakeAllReflectionProbesSnapshots.Invoke(null, new object[0]);
        }
    }
}
