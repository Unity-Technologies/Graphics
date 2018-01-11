using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering
{
    public static class HDReflectionProbeEditorUtility
    {
        static int _Cubemap = Shader.PropertyToID("_Cubemap");
        static Material s_PreviewMaterial;
        static Mesh s_SphereMesh;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            s_PreviewMaterial = new Material(Shader.Find("Debug/ReflectionProbePreview"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            s_SphereMesh = Resources.GetBuiltinResource(typeof(Mesh), "New-Sphere.fbx") as Mesh;
        }

        public static Matrix4x4 GetLocalSpace(ReflectionProbe probe)
        {
            var t = probe.transform.position;
            return Matrix4x4.TRS(t, GetLocalSpaceRotation(probe), Vector3.one);
        }

        public static Quaternion GetLocalSpaceRotation(ReflectionProbe probe)
        {
            var supportsRotation = (SupportedRenderingFeatures.active.reflectionProbeSupportFlags & SupportedRenderingFeatures.ReflectionProbeSupportFlags.Rotation) != 0;
            return supportsRotation
                ? probe.transform.rotation
                : Quaternion.identity;
        }

        // Ensures that probe's AABB encapsulates probe's position
        // Returns true, if center or size was modified
        public static bool ValidateAABB(ReflectionProbe p, ref Vector3 center, ref Vector3 size)
        {
            var localSpace = GetLocalSpace(p);
            var localTransformPosition = localSpace.inverse.MultiplyPoint3x4(p.transform.position);

            var b = new Bounds(center, size);

            if (b.Contains(localTransformPosition))
                return false;

            b.Encapsulate(localTransformPosition);

            center = b.center;
            size = b.size;
            return true;
        }

        public static bool IsCollidingWithOtherProbes(string targetPath, ReflectionProbe targetProbe, out ReflectionProbe collidingProbe)
        {
            ReflectionProbe[] probes = Object.FindObjectsOfType<ReflectionProbe>().ToArray();
            collidingProbe = null;
            foreach (var probe in probes)
            {
                if (probe == targetProbe || probe.customBakedTexture == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(probe.customBakedTexture);
                if (path == targetPath)
                {
                    collidingProbe = probe;
                    return true;
                }
            }
            return false;
        }


        static MethodInfo k_Lightmapping_BakeReflectionProbeSnapshot = typeof(UnityEditor.Lightmapping).GetMethod("BakeReflectionProbeSnapshot", BindingFlags.Static | BindingFlags.NonPublic);
        public static bool BakeReflectionProbeSnapshot(ReflectionProbe probe)
        {
            return (bool)k_Lightmapping_BakeReflectionProbeSnapshot.Invoke(null, new object[] { probe });
        }

        static MethodInfo k_Lightmapping_BakeAllReflectionProbesSnapshots = typeof(UnityEditor.Lightmapping).GetMethod("BakeAllReflectionProbesSnapshots", BindingFlags.Static | BindingFlags.NonPublic);
        public static bool BakeAllReflectionProbesSnapshots()
        {
            return (bool)k_Lightmapping_BakeAllReflectionProbesSnapshots.Invoke(null, new object[0]);
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

        public static float CalculateSphereMaxBlendDistance(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor o)
        {
            return p.influenceSphereRadius.floatValue;
        }

        public static Vector3 CalculateBoxMaxBlendDistance(HDReflectionProbeUI s, SerializedHDReflectionProbe p, Editor o)
        {
            return p.boxSize.vector3Value * 0.5f;
        }

        internal static void InitializeProbe(ReflectionProbe p, HDAdditionalReflectionData data)
        {
            var meshFilter = p.GetComponent<MeshFilter>() ?? p.gameObject.AddComponent<MeshFilter>();
            var meshRenderer = p.GetComponent<MeshRenderer>() ?? p.gameObject.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = s_SphereMesh;

            var material = meshRenderer.sharedMaterial;
            if (material == null
                || material == s_PreviewMaterial
                || material.shader != s_PreviewMaterial.shader)
            {
                material = Object.Instantiate(s_PreviewMaterial);
                material.SetTexture(_Cubemap, p.texture);
                material.hideFlags = HideFlags.HideAndDontSave;
                meshRenderer.material = material;
            }

            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        internal static void ChangeVisibility(ReflectionProbe p, bool visible)
        {
            var meshRenderer = p.GetComponent<MeshRenderer>() ?? p.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.enabled = visible;
        }
    }
}
