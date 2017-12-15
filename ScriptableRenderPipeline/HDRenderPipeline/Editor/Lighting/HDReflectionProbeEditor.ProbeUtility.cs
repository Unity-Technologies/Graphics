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


        [InitializeOnLoadMethod]
        static void Initialize()
        {
            s_PreviewMaterial = new Material(Shader.Find("Debug/ReflectionProbePreview"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            s_SphereMesh = Resources.GetBuiltinResource(typeof(Mesh), "New-Sphere.fbx") as Mesh;

            HDAdditionalReflectionData.OnNewItem += OnNewProbe;
            foreach (var data in HDAdditionalReflectionData.AllDatas)
                OnNewProbe(data);
        }

        static void OnNewProbe(HDAdditionalReflectionData value)
        {
            InitializeProbe(value.GetComponent<ReflectionProbe>(), value);
        }

        static void InitializeProbe(ReflectionProbe p, HDAdditionalReflectionData data)
        {
            var meshFilter = p.GetComponent<MeshFilter>() ?? p.gameObject.AddComponent<MeshFilter>();
            var meshRenderer = p.GetComponent<MeshRenderer>() ?? p.gameObject.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = s_SphereMesh;

            var material = Object.Instantiate(s_PreviewMaterial);
            material.SetTexture(_Cubemap, p.texture);
            material.hideFlags = HideFlags.HideAndDontSave;
            meshRenderer.material = material;

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
