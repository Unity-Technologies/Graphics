using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    internal static class BlockFields
    {
        internal static BlockFieldProviderInfo m_ProviderInfo = new BlockFieldProviderInfo("SG");
        // Note: the provider below is specific for the BlockFields.VertexDescription and BlockFields.SurfaceDescription groups below,
        // and isn't meant to be constructed except by SG to enumerate the blockfields available and their signature.
        class Provider : BlockFieldProvider
        {
            Provider()
                : base(m_ProviderInfo, () =>
                   {
                       return GetPartialSignatureMapFromGenerateBlockGroup(m_ProviderInfo.uniqueNamespace, typeof(BlockFields.VertexDescription), BlockFields.VertexDescription.tagName, s_OldValidVertexTagName, s_OldValidVertexBlockFieldNames)
                           .Concat(GetPartialSignatureMapFromGenerateBlockGroup(m_ProviderInfo.uniqueNamespace, typeof(BlockFields.SurfaceDescription), BlockFields.SurfaceDescription.tagName, s_OldValidSurfaceTagName, s_OldValidSurfaceBlockFieldNames))
                           .Concat(GetPartialSignatureMapFromGenerateBlockGroup(m_ProviderInfo.uniqueNamespace, typeof(BlockFields.SurfaceDescriptionLegacy), BlockFields.SurfaceDescriptionLegacy.tagName, s_OldValidSurfaceTagName, s_OldValidSurfaceBlockFieldNames));
                   })
            {}
        }

        // These are valid blockfield names that we know dont collide with UnityEditor.ShaderGraph.BlockFields.
        // Both of these were used with tagnames prefix as serialized identifiers for blockfields, and didnt collide
        // with each other (post fixing the URP/HDRP collision of coatsmoothness and coatmask that is).
        // We save these to recognize previous shadergraphs with this weak serialization.
        // The new serialized blockfield descriptor string uses a unique namespace provided by the IBlockFieldProvider.
        //
        // Nothing should be added to this list, as no other blockfields were known prior to the "ProviderNamespace.Tag.Name"
        // format.
        static string s_OldValidVertexTagName = "VertexDescription";
        static HashSet<string> s_OldValidVertexBlockFieldNames = new HashSet<string>()
        {
            "Position",
            "Normal",
            "Tangent",
        };

        static string s_OldValidSurfaceTagName = "SurfaceDescription";
        static HashSet<string> s_OldValidSurfaceBlockFieldNames = new HashSet<string>()
        {
            "BaseColor",
            "NormalTS",
            "NormalOS",
            "NormalWS",
            "Metallic",
            "Specular",
            "Smoothness",
            "Occlusion",
            "Emission",
            "Alpha",
            "AlphaClipThreshold",
            "CoatMask",
            "CoatSmoothness",
            "SpriteColor",
        };

        public static IEnumerable<(BlockFieldSignature, BlockFieldDescriptor)>
        GetPartialSignatureMapFromGenerateBlockGroup(string providerNameSpace, Type typeObject, string tagName,
            string oldTagName = null, HashSet<string> oldBlockFieldNames = null)
        {
            // note: we do GetValue(typeObj) from a Type object instance as we know fields are static
            var entries = typeObject.GetFields().Where(fi => fi.GetValue(typeObject) is BlockFieldDescriptor)
                .Select(fi =>
                {
                    var ret = new List<(BlockFieldSignature, BlockFieldDescriptor)>();
                    var blockFieldDescriptor = fi.GetValue(typeObject) as BlockFieldDescriptor;
                    var attribs = typeObject.GetCustomAttributes(typeof(GenerateBlocksAttribute), false);
                    if (attribs != null && attribs.Length > 0)
                    {
                        var attribute = attribs[0] as GenerateBlocksAttribute;
                        blockFieldDescriptor.path = attribute?.path ?? "";
                    }
                    BlockFieldSignature blockFieldSignature = new BlockFieldSignature(providerNameSpace, blockFieldDescriptor.tag, blockFieldDescriptor.name);

                    // Add our first tuple linking fully qualified signature with the blockfielddescriptor
                    ret.Add((blockFieldSignature, blockFieldDescriptor));

                    // Check if the blockfield we're adding was used in the old non-qualified style blockfield shadergraphs,
                    // and output an (old - with empty namespace) signature for it so we intercept and map it to the same
                    // blockfield if needed:
                    if (oldTagName != null && oldBlockFieldNames != null)
                    {
                        if (oldBlockFieldNames.Contains(blockFieldDescriptor.name))
                        {
                            BlockFieldSignature oldBlockFieldSignature = new BlockFieldSignature("", oldTagName, blockFieldSignature.referenceName);
                            ret.Add((oldBlockFieldSignature, blockFieldDescriptor));
                        }
                    }
                    return ret;
                })
                .SelectMany(t => t);//.ToList();
            return entries;
        }

        [GenerateBlocks]
        public struct VertexDescription
        {
            public static string tagName = "VertexDescription";
            public static BlockFieldDescriptor Position      = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Position", "VERTEXDESCRIPTION_POSITION",
                new PositionControl(CoordinateSpace.Object), ShaderStage.Vertex);
            public static BlockFieldDescriptor Normal        = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Normal", "VERTEXDESCRIPTION_NORMAL",
                new NormalControl(CoordinateSpace.Object), ShaderStage.Vertex);
            public static BlockFieldDescriptor Tangent       = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Tangent", "VERTEXDESCRIPTION_TANGENT",
                new TangentControl(CoordinateSpace.Object), ShaderStage.Vertex);
        }

        [GenerateBlocks]
        public struct SurfaceDescription
        {
            public static string tagName = "SurfaceDescription";
            public static BlockFieldDescriptor BaseColor     = new BlockFieldDescriptor(m_ProviderInfo, tagName, "BaseColor", "Base Color", "SURFACEDESCRIPTION_BASECOLOR",
                new ColorControl(UnityEngine.Color.grey, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor NormalTS      = new BlockFieldDescriptor(m_ProviderInfo, tagName, "NormalTS", "Normal (Tangent Space)", "SURFACEDESCRIPTION_NORMALTS",
                new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor NormalOS      = new BlockFieldDescriptor(m_ProviderInfo, tagName, "NormalOS", "Normal (Object Space)", "SURFACEDESCRIPTION_NORMALOS",
                new NormalControl(CoordinateSpace.Object), ShaderStage.Fragment);
            public static BlockFieldDescriptor NormalWS      = new BlockFieldDescriptor(m_ProviderInfo, tagName, "NormalWS", "Normal (World Space)", "SURFACEDESCRIPTION_NORMALWS",
                new NormalControl(CoordinateSpace.World), ShaderStage.Fragment);
            public static BlockFieldDescriptor Metallic      = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Metallic", "SURFACEDESCRIPTION_METALLIC",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Specular      = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Specular", "Specular Color", "SURFACEDESCRIPTION_SPECULAR",
                new ColorControl(UnityEngine.Color.grey, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor Smoothness    = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Smoothness", "SURFACEDESCRIPTION_SMOOTHNESS",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Occlusion     = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Occlusion", "Ambient Occlusion", "SURFACEDESCRIPTION_OCCLUSION",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Emission      = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Emission", "SURFACEDESCRIPTION_EMISSION",
                new ColorControl(UnityEngine.Color.black, true), ShaderStage.Fragment);
            public static BlockFieldDescriptor Alpha         = new BlockFieldDescriptor(m_ProviderInfo, tagName, "Alpha", "SURFACEDESCRIPTION_ALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AlphaClipThreshold = new BlockFieldDescriptor(m_ProviderInfo, tagName, "AlphaClipThreshold", "Alpha Clip Threshold", "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLD",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatMask       = new BlockFieldDescriptor(m_ProviderInfo, tagName, "CoatMask", "Coat Mask", "SURFACEDESCRIPTION_COATMASK",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatSmoothness = new BlockFieldDescriptor(m_ProviderInfo, tagName, "CoatSmoothness", "Coat Smoothness", "SURFACEDESCRIPTION_COATSMOOTHNESS",
                new FloatControl(1.0f), ShaderStage.Fragment);
        }

        [GenerateBlocks]
        public struct SurfaceDescriptionLegacy
        {
            public static string tagName = "SurfaceDescription";
            public static BlockFieldDescriptor SpriteColor  = new BlockFieldDescriptor(m_ProviderInfo, tagName, "SpriteColor", "SURFACEDESCRIPTION_SPRITECOLOR",
                new ColorRGBAControl(UnityEngine.Color.white), ShaderStage.Fragment, isHidden: true);
        }
    }
}
