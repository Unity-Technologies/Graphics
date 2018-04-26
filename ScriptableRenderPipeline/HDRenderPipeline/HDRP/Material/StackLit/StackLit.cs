using System;
using UnityEngine.Rendering;
//using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class StackLit : RenderPipelineMaterial
    {
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            StackLitStandard             = 1 << 0,
            StackLitAnisotropy           = 1 << 4,
            StackLitCoat                 = 1 << 6,
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1300)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("Material Features")]
            public uint materialFeatures;

            // Bottom interface (2 lobes BSDF) 
            // Standard parametrization
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;
            
            [SurfaceDataAttributes(new string[]{"Normal", "Normal View Space"}, true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes("Smoothness A")]
            public float perceptualSmoothnessA;
            [SurfaceDataAttributes("Smoothness B")]
            public float perceptualSmoothnessB;

            [SurfaceDataAttributes("Lobe Mixing")]
            public float lobeMix;

            [SurfaceDataAttributes("Metallic")]
            public float metallic;

            // Anisotropic
            [SurfaceDataAttributes("Tangent", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("Anisotropy")]
            public float anisotropy; // anisotropic ratio(0->no isotropic; 1->full anisotropy in tangent direction, -1->full anisotropy in bitangent direction)

            // Top interface and media (clearcoat)
            [SurfaceDataAttributes("Coat Roughness")]
            public float coatPerceptualSmoothness;
            [SurfaceDataAttributes("Coat IOR")]
            public float coatIor;
            [SurfaceDataAttributes("Coat Thickness")]
            public float coatThickness;
            [SurfaceDataAttributes("Coat Extinction Coefficient")]
            public Vector3 coatExtinction;


        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 1400)]
        public struct BSDFData
        {
            public uint materialFeatures;

            // Bottom interface (2 lobes BSDF) 
            // Standard parametrization
            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 fresnel0;

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3 normalWS;
            public float perceptualRoughnessA;
            public float perceptualRoughnessB;
            public float lobeMix;

            // Anisotropic
            [SurfaceDataAttributes("", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("", true)]
            public Vector3 bitangentWS;
            public float roughnessAT;
            public float roughnessAB;
            public float roughnessBT;
            public float roughnessBB;
            public float coatRoughness;
            public float anisotropy;

            // Top interface and media (clearcoat)
            public float coatPerceptualRoughness;
            public float coatIor;
            public float coatThickness;
            public Vector3 coatExtinction;

            //public fixed float test[2];
            //could use something like that:
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst=5)]
            //public float[] test;


        };
        //-----------------------------------------------------------------------------
        // Init precomputed textures
        //-----------------------------------------------------------------------------

        bool m_isInit;

        public StackLit() {}

        public override void Build(HDRenderPipelineAsset hdAsset)
        {
            PreIntegratedFGD.instance.Build();
            //LTCAreaLight.instance.Build();

            m_isInit = false;
        }

        public override void Cleanup()
        {
            PreIntegratedFGD.instance.Cleanup();
            //LTCAreaLight.instance.Cleanup();

            m_isInit = false;
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            if (m_isInit)
                return;

            PreIntegratedFGD.instance.RenderInit(cmd);

            m_isInit = true;
        }

        public override void Bind()
        {
            PreIntegratedFGD.instance.Bind();
            //LTCAreaLight.instance.Bind();
        }

    }
}
