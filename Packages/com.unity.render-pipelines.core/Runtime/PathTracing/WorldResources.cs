using System;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.PathTracing.Core
{
    [Serializable]
    [SupportedOnRenderPipeline()]
    [Categorization.CategoryInfo(Name = "R: Path Tracing Core World", Order = 1000), HideInInspector]
    internal class WorldRenderPipelineResources : IRenderPipelineResources
    {
        [SerializeField, HideInInspector] int _version = 3;

        public int version
        {
            get => _version;
        }

        [SerializeField, ResourcePath("Runtime/PathTracing/Shaders/BlitCubemap.compute")]
        ComputeShader _blitCubemap;

        [SerializeField, ResourcePath("Runtime/PathTracing/Shaders/BlitCookie.compute")]
        ComputeShader _blitGrayScaleCookie;

        [SerializeField, ResourcePath("Runtime/PathTracing/Shaders/SetAlphaChannel.compute")]
        ComputeShader _setAlphaChannelShader;

        [SerializeField, ResourcePath("Runtime/PathTracing/Shaders/PathTracingSkySamplingData.compute")]
        ComputeShader _pathTracingSkySamplingDataShader;

        [SerializeField, ResourcePath("Runtime/PathTracing/Meshes/SkyBoxMesh.mesh")]
        Mesh _skyBoxMesh;

        [SerializeField, ResourcePath("Runtime/PathTracing/Meshes/6FaceSkyboxMesh.mesh")]
        Mesh _sixFaceSkyBoxMesh;

        [SerializeField, ResourcePath("Runtime/PathTracing/Shaders/BuildLightGrid.compute")]
        ComputeShader _buildLightGridShader;


        public ComputeShader BlitCubemap
        {
            get => _blitCubemap;
            set => this.SetValueAndNotify(ref _blitCubemap, value, nameof(_blitCubemap));
        }

        public ComputeShader BlitGrayScaleCookie
        {
            get => _blitGrayScaleCookie;
            set => this.SetValueAndNotify(ref _blitGrayScaleCookie, value, nameof(_blitGrayScaleCookie));
        }

        public ComputeShader SetAlphaChannelShader
        {
            get => _setAlphaChannelShader;
            set => this.SetValueAndNotify(ref _setAlphaChannelShader, value, nameof(_setAlphaChannelShader));
        }

        public ComputeShader PathTracingSkySamplingDataShader
        {
            get => _pathTracingSkySamplingDataShader;
            set => this.SetValueAndNotify(ref _pathTracingSkySamplingDataShader, value, nameof(_pathTracingSkySamplingDataShader));
        }

        public Mesh SkyBoxMesh
        {
            get => _skyBoxMesh;
            set => this.SetValueAndNotify(ref _skyBoxMesh, value, nameof(_skyBoxMesh));
        }

        public Mesh SixFaceSkyBoxMesh
        {
            get => _sixFaceSkyBoxMesh;
            set => this.SetValueAndNotify(ref _sixFaceSkyBoxMesh, value, nameof(_sixFaceSkyBoxMesh));
        }

        public ComputeShader BuildLightGridShader
        {
            get => _buildLightGridShader;
            set => this.SetValueAndNotify(ref _buildLightGridShader, value, nameof(_buildLightGridShader));
        }
    }

    internal class WorldResourceSet
    {
        public ComputeShader BlitCubemap;
        public ComputeShader BlitGrayScaleCookie;
        public ComputeShader SetAlphaChannelShader;
        public ComputeShader PathTracingSkySamplingDataShader;
        public Mesh SkyBoxMesh;
        public Mesh SixFaceSkyBoxMesh;
        public ComputeShader BuildLightGridShader;

#if UNITY_EDITOR
        public void LoadFromAssetDatabase()
        {
            const string packageFolder = "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/";

            BlitCubemap = AssetDatabase.LoadAssetAtPath<ComputeShader>(packageFolder + "Shaders/BlitCubemap.compute");
            BlitGrayScaleCookie = AssetDatabase.LoadAssetAtPath<ComputeShader>(packageFolder + "Shaders/BlitCookie.compute");
            SetAlphaChannelShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(packageFolder + "Shaders/SetAlphaChannel.compute");
            PathTracingSkySamplingDataShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(packageFolder + "Shaders/PathTracingSkySamplingData.compute");
            SkyBoxMesh = AssetDatabase.LoadAssetAtPath<Mesh>(packageFolder + "Meshes/SkyboxMesh.mesh");
            SixFaceSkyBoxMesh = AssetDatabase.LoadAssetAtPath<Mesh>(packageFolder + "Meshes/6FaceSkyboxMesh.mesh");
            BuildLightGridShader = AssetDatabase.LoadAssetAtPath<ComputeShader>(packageFolder + "Shaders/BuildLightGrid.compute");
        }
#endif

        public bool LoadFromRenderPipelineResources()
        {
            if (GraphicsSettings.TryGetRenderPipelineSettings<WorldRenderPipelineResources>(out var rpResources))
            {
                Debug.Assert(rpResources.BlitCubemap != null);
                Debug.Assert(rpResources.BlitGrayScaleCookie != null);
                Debug.Assert(rpResources.SetAlphaChannelShader != null);
                Debug.Assert(rpResources.PathTracingSkySamplingDataShader != null);
                Debug.Assert(rpResources.SkyBoxMesh != null);
                Debug.Assert(rpResources.SixFaceSkyBoxMesh != null);
                Debug.Assert(rpResources.BuildLightGridShader != null);

                BlitCubemap = rpResources.BlitCubemap;
                BlitGrayScaleCookie = rpResources.BlitGrayScaleCookie;
                SetAlphaChannelShader = rpResources.SetAlphaChannelShader;
                PathTracingSkySamplingDataShader = rpResources.PathTracingSkySamplingDataShader;
                SkyBoxMesh = rpResources.SkyBoxMesh;
                SixFaceSkyBoxMesh = rpResources.SixFaceSkyBoxMesh;
                BuildLightGridShader = rpResources.BuildLightGridShader;

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
