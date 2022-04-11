using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    internal enum Texture2DMipSamplingMode
    {
        Standard,
        LOD,
        Gradient,
        Bias
    }

    internal enum Texture3DMipSamplingMode
    {
        Standard,
        LOD
    }

    internal struct Mip2DSamplingInputs
    {
        public int biasInput;
        public int lodInput;
        public int ddxInput;
        public int ddyInput;
        public MaterialSlot bias;
        public MaterialSlot lod;
        public MaterialSlot ddx;
        public MaterialSlot ddy;

        public static Mip2DSamplingInputs NewDefault()
        {
            return new Mip2DSamplingInputs()
            {
                bias = null,
                lod = null,
                ddx = null,
                ddy = null
            };
        }
    }

    internal struct Mip3DSamplingInputs
    {
        public int lodInput;
        public MaterialSlot lod;

        public static Mip3DSamplingInputs NewDefault()
        {
            return new Mip3DSamplingInputs()
            {
                lod = null
            };
        }
    }

    internal static class MipSamplingModesUtils
    {
        private static string kLodName = "LOD";
        private static string kBiasName = "Bias";
        private static string kDdxName = "DDX";
        private static string kDdyName = "DDY";

        public static string Get2DTextureSamplingMacro(Texture2DMipSamplingMode mode, bool usePlatformMacros, bool isArray)
        {
            if (isArray)
            {
                if (usePlatformMacros)
                {
                    switch (mode)
                    {
                        case Texture2DMipSamplingMode.Standard:
                            return "PLATFORM_SAMPLE_TEXTURE2D_ARRAY";
                        case Texture2DMipSamplingMode.LOD:
                            return "PLATFORM_SAMPLE_TEXTURE2D_ARRAY_LOD";
                        case Texture2DMipSamplingMode.Bias:
                            return "PLATFORM_SAMPLE_TEXTURE2D_ARRAY_BIAS";
                        case Texture2DMipSamplingMode.Gradient:
                            return "PLATFORM_SAMPLE_TEXTURE2D_ARRAY_GRAD";
                    }
                }
                else
                {
                    switch (mode)
                    {
                        case Texture2DMipSamplingMode.Standard:
                            return "SAMPLE_TEXTURE2D_ARRAY";
                        case Texture2DMipSamplingMode.LOD:
                            return "SAMPLE_TEXTURE2D_ARRAY_LOD";
                        case Texture2DMipSamplingMode.Bias:
                            return "SAMPLE_TEXTURE2D_ARRAY_BIAS";
                        case Texture2DMipSamplingMode.Gradient:
                            return "SAMPLE_TEXTURE2D_ARRAY_GRAD";
                    }
                }
            }
            else
            {
                if (usePlatformMacros)
                {
                    switch (mode)
                    {
                        case Texture2DMipSamplingMode.Standard:
                            return "PLATFORM_SAMPLE_TEXTURE2D";
                        case Texture2DMipSamplingMode.LOD:
                            return "PLATFORM_SAMPLE_TEXTURE2D_LOD";
                        case Texture2DMipSamplingMode.Bias:
                            return "PLATFORM_SAMPLE_TEXTURE2D_BIAS";
                        case Texture2DMipSamplingMode.Gradient:
                            return "PLATFORM_SAMPLE_TEXTURE2D_GRAD";
                    }
                }
                else
                {
                    switch (mode)
                    {
                        case Texture2DMipSamplingMode.Standard:
                            return "SAMPLE_TEXTURE2D";
                        case Texture2DMipSamplingMode.LOD:
                            return "SAMPLE_TEXTURE2D_LOD";
                        case Texture2DMipSamplingMode.Bias:
                            return "SAMPLE_TEXTURE2D_BIAS";
                        case Texture2DMipSamplingMode.Gradient:
                            return "SAMPLE_TEXTURE2D_GRAD";
                    }
                }
            }

            return "";
        }

        public static string Get3DTextureSamplingMacro(Texture3DMipSamplingMode mode, bool usePlatformMacros)
        {
            if (usePlatformMacros)
            {
                switch (mode)
                {
                    case Texture3DMipSamplingMode.Standard:
                        return "PLATFORM_SAMPLE_TEXTURE3D";
                    case Texture3DMipSamplingMode.LOD:
                        return "PLATFORM_SAMPLE_TEXTURE2D_LOD";
                }
            }
            else
            {
                switch (mode)
                {
                    case Texture3DMipSamplingMode.Standard:
                        return "SAMPLE_TEXTURE3D";
                    case Texture3DMipSamplingMode.LOD:
                        return "SAMPLE_TEXTURE3D_LOD";
                }
            }

            return "";
        }

        public static Mip2DSamplingInputs CreateMip2DSamplingInputs(
            AbstractMaterialNode node, Texture2DMipSamplingMode mode, Mip2DSamplingInputs previousInputs,
            int biasInputId, int lodInputId, int ddxInputId, int ddyInputId)
        {
            if (previousInputs.bias != null)
                node.RemoveSlot(previousInputs.bias.id);
            if (previousInputs.lod != null)
                node.RemoveSlot(previousInputs.lod.id);
            if (previousInputs.ddx != null)
                node.RemoveSlot(previousInputs.ddx.id);
            if (previousInputs.ddy != null)
                node.RemoveSlot(previousInputs.ddy.id);

            Mip2DSamplingInputs inputs = Mip2DSamplingInputs.NewDefault();
            inputs.biasInput = biasInputId;
            inputs.lodInput = lodInputId;
            inputs.ddxInput = ddxInputId;
            inputs.ddyInput = ddyInputId;
            switch (mode)
            {
                case Texture2DMipSamplingMode.LOD:
                    inputs.lod = node.AddSlot(new Vector1MaterialSlot(lodInputId, kLodName, kLodName, SlotType.Input, 0.0f));
                    break;
                case Texture2DMipSamplingMode.Bias:
                    inputs.bias = node.AddSlot(new Vector1MaterialSlot(biasInputId, kBiasName, kBiasName, SlotType.Input, 0.0f));
                    break;
                case Texture2DMipSamplingMode.Gradient:
                    inputs.ddx = node.AddSlot(new Vector2MaterialSlot(ddxInputId, kDdxName, kDdxName, SlotType.Input, new Vector2(0.0f, 0.0f)));
                    inputs.ddy = node.AddSlot(new Vector2MaterialSlot(ddyInputId, kDdyName, kDdyName, SlotType.Input, new Vector2(0.0f, 0.0f)));
                    break;
            }

            return inputs;
        }

        public static Mip3DSamplingInputs CreateMip3DSamplingInputs(
            AbstractMaterialNode node, Texture3DMipSamplingMode mode, Mip3DSamplingInputs previousInputs,
            int lodInputId)
        {
            if (previousInputs.lod != null)
                node.RemoveSlot(previousInputs.lod.id);

            Mip3DSamplingInputs inputs = Mip3DSamplingInputs.NewDefault();
            inputs.lodInput = lodInputId;
            switch (mode)
            {
                case Texture3DMipSamplingMode.LOD:
                    inputs.lod = node.AddSlot(new Vector1MaterialSlot(lodInputId, kLodName, kLodName, SlotType.Input, 0.0f));
                    break;
            }

            return inputs;
        }

        public static string GetSamplerMipArgs(
            AbstractMaterialNode node, Texture2DMipSamplingMode mode, Mip2DSamplingInputs inputs, GenerationMode generationMode)
        {
            switch (mode)
            {
                case Texture2DMipSamplingMode.LOD:
                    return string.Format(", {0}", node.GetSlotValue(inputs.lodInput, generationMode));
                case Texture2DMipSamplingMode.Bias:
                    return string.Format(", {0}", node.GetSlotValue(inputs.biasInput, generationMode));
                case Texture2DMipSamplingMode.Gradient:
                    return string.Format(", {0}, {1}", node.GetSlotValue(inputs.ddxInput, generationMode), node.GetSlotValue(inputs.ddyInput, generationMode));
            }

            return "";
        }

        public static string GetSamplerMipArgs(
            AbstractMaterialNode node, Texture3DMipSamplingMode mode, Mip3DSamplingInputs inputs, GenerationMode generationMode)
        {
            switch (mode)
            {
                case Texture3DMipSamplingMode.LOD:
                    return string.Format(", {0}", node.GetSlotValue(inputs.lodInput, generationMode));
            }

            return "";
        }
    }
}
