using UnityEngine;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.VFX.HDRP
{
    class SixWaySmokeLit : RenderPipelineMaterial
    {

        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            LitSixWaySmoke = 1 << 0,
        };
        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, false, true, 1700)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("Material Features")]
            public uint materialFeatures;

            public float absorptionRange;

            // Standard
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("Base Color", false, true, FieldPrecision.Real)]
            public Vector4 baseColor;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            [SurfaceDataAttributes(new string[] { "Normal", "Normal World Space" }, true, checkIsNormalized = true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes(new string[] { "Tangent", "Tangent World Space" })]
            public Vector4 tangentWS;

            [MaterialSharedPropertyMapping(MaterialSharedProperty.AmbientOcclusion)]
            [SurfaceDataAttributes("Ambient Occlusion", precision = FieldPrecision.Real)]
            public float ambientOcclusion;

            // MaterialFeature dependent attribute

            // Forward property only
            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true, precision = FieldPrecision.Real, checkIsNormalized = true)]
            public Vector3 geomNormalWS;

            //Smoke Lighting
            [SurfaceDataAttributes("Rig Right Top Back", precision = FieldPrecision.Real)]
            public Vector3 rigRTBk;
            [SurfaceDataAttributes("Rig Left Bottom Front", precision = FieldPrecision.Real)]
            public Vector3 rigLBtF;

            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 bakeDiffuseLighting0;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 bakeDiffuseLighting1;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 bakeDiffuseLighting2;

            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 backBakeDiffuseLighting0;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 backBakeDiffuseLighting1;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 backBakeDiffuseLighting2;


        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 1750)]
        public struct BSDFData
        {
            public uint materialFeatures;

            public float absorptionRange;

            [SurfaceDataAttributes("", false, true, FieldPrecision.Real)]
            public Vector4 diffuseColor;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 fresnel0;

            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public float ambientOcclusion; // Caution: This is accessible only if light layer is enabled, otherwise it is 1

            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true, checkIsNormalized: true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes(new string[] { "Tangent", "Tangent World Space" })]
            public Vector4 tangentWS;
            // Forward property only
            [SurfaceDataAttributes(new string[] { "Geometric Normal", "Geometric Normal View Space" }, true, precision = FieldPrecision.Real, checkIsNormalized = true)]
            public Vector3 geomNormalWS;


            //Smoke Lighting
            [SurfaceDataAttributes("Rig Right Top Back", precision = FieldPrecision.Real)]
            public Vector3 rigRTBk;
            [SurfaceDataAttributes("Rig Left Bottom Front", precision = FieldPrecision.Real)]
            public Vector3 rigLBtF;

            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 bakeDiffuseLighting0;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 bakeDiffuseLighting1;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 bakeDiffuseLighting2;

            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 backBakeDiffuseLighting0;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 backBakeDiffuseLighting1;
            [SurfaceDataAttributes(precision = FieldPrecision.Real)]
            public Vector3 backBakeDiffuseLighting2;
        };

        public SixWaySmokeLit() { }
    }
}
