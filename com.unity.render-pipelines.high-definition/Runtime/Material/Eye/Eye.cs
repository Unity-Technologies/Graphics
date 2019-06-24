using System;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;
using UnityEngine.Rendering;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class Eye : RenderPipelineMaterial
    {

        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            EyeGames = 1 << 0,
            EyeCinematics = 1 << 1
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, false, true, 1300)]
        public struct SurfaceData
        {
            // Standard
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;

            [SurfaceDataAttributes("Specular Occlusion")]
            public float specularOcclusion;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            [SurfaceDataAttributes(new string[] { "Normal", "Normal View Space" }, true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true)]
            public Vector3 geomNormalWS;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Smoothness)]
            [SurfaceDataAttributes("Iris Smoothness")]
            public float irisSmoothness;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Smoothness)]
            [SurfaceDataAttributes("Sclera Smoothness")]
            public float scleraSmoothness;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.AmbientOcclusion)]
            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            // Specular Tint
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Specular)]
            [SurfaceDataAttributes("Specular Tint", false, true)]
            public Vector3 specularColor;

            // Transmission
            [SurfaceDataAttributes("Cornea Height")]
            public float corneaHeight;
        }

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1350)]
        public struct BSDFData
        {
            public uint materialFeatures;

            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 fresnel0;

            public float ambientOcclusion;
            public float specularOcclusion;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true)]
            public Vector3 geomNormalWS;

            public float irisRoughness;
            public float scleraRoughness;

            public float corneaHeight;
        }

        //-----------------------------------------------------------------------------
        // Init precomputed textures
        //-----------------------------------------------------------------------------

        public Eye() {}

        public override void Build(HDRenderPipelineAsset hdAsset)
        {
            //PreIntegratedFGD.instance.Build(PreIntegratedFGD.FGDIndex.FGD_CharlieAndEyeLambert);
            //LTCAreaLight.instance.Build();
        }

        public override void Cleanup()
        {
            //PreIntegratedFGD.instance.Cleanup(PreIntegratedFGD.FGDIndex.FGD_CharlieAndEyeLambert);
            //LTCAreaLight.instance.Cleanup();
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            //PreIntegratedFGD.instance.RenderInit(PreIntegratedFGD.FGDIndex.FGD_CharlieAndEyeLambert, cmd);
        }

        public override void Bind(CommandBuffer cmd)
        {
            //PreIntegratedFGD.instance.Bind(cmd, PreIntegratedFGD.FGDIndex.FGD_CharlieAndEyeLambert);
            //LTCAreaLight.instance.Bind(cmd);
        }
    }
}
