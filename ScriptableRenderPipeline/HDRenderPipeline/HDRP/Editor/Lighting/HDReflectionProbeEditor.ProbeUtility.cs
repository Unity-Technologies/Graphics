using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering
{
    partial class HDReflectionProbeEditor
    {
        static Material s_PreviewMaterial;
        static Mesh s_SphereMesh;
        static int _Cubemap = Shader.PropertyToID("_Cubemap");

        void ChangeVisibilityOfAllTargets(bool visibility)
        {
            for (var i = 0; i < targets.Length; ++i)
            {
                var p = (ReflectionProbe)targets[i];
                ChangeVisibility(p, visibility);
            }
        }

        void InitializeAllTargetProbes()
        {
            for (var i = 0; i < targets.Length; ++i)
            {
                var p = (ReflectionProbe)targets[i];
                var a = (HDAdditionalReflectionData)m_AdditionalDataSerializedObject.targetObjects[i];
                InitializeProbe(p, a);
            }

            // For an unknown reason, newly created probes sometype have the type "Quad" (value = 1)
            // This type of probe is not supported by Unity since 5.4
            // But we need to force it here so it does not bake into a 2D texture but a Cubemap
            serializedObject.Update();
            serializedObject.FindProperty("m_Type").intValue = 0;
            serializedObject.ApplyModifiedProperties();
        }


        [InitializeOnLoadMethod]
        static void Initialize()
        {
            s_PreviewMaterial = new Material(Shader.Find("Debug/ReflectionProbePreview"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            s_SphereMesh = Resources.GetBuiltinResource(typeof(Mesh), "New-Sphere.fbx") as Mesh;
        }

        static void InitializeProbe(ReflectionProbe p, HDAdditionalReflectionData data)
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

        static void ChangeVisibility(ReflectionProbe p, bool visible)
        {
            var meshRenderer = p.GetComponent<MeshRenderer>() ?? p.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.enabled = visible;
        }
    }
}
