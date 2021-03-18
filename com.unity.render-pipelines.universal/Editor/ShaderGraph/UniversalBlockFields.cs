using UnityEngine;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalBlockFields
    {
        static BlockFieldProviderInfo m_ProviderInfo = new BlockFieldProviderInfo("URP");
        // Note: the provider below is specific for the UniversalBlockFields.SurfaceDescription group below
        // and isn't meant to be constructed except by SG to enumerate the blockfields available and their signature.
        class Provider : BlockFieldProvider
        {
            Provider()
                : base(m_ProviderInfo, () =>
                   {
                       return BlockFields.GetPartialSignatureMapFromGenerateBlockGroup(m_ProviderInfo.uniqueNamespace, typeof(UniversalBlockFields.SurfaceDescription), UniversalBlockFields.SurfaceDescription.tagName, s_OldValidSurfaceTagName, s_OldValidSurfaceBlockFieldNames);
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
        static string s_OldValidSurfaceTagName = "SurfaceDescription";
        static HashSet<string> s_OldValidSurfaceBlockFieldNames = new HashSet<string>()
        {
            "SpriteMask",
        };

        [GenerateBlocks("Universal Render Pipeline")]
        public struct SurfaceDescription
        {
            public static string tagName = "SurfaceDescription";
            public static BlockFieldDescriptor SpriteMask = new BlockFieldDescriptor(m_ProviderInfo, SurfaceDescription.tagName, "SpriteMask", "Sprite Mask", "SURFACEDESCRIPTION_SPRITEMASK",
                new ColorRGBAControl(new Color(1, 1, 1, 1)), ShaderStage.Fragment);
        }
    }
}
