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
