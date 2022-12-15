using System;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    internal static class SpriteSubTargetUtility
    {
        public static void AddDefaultFields(ref TargetFieldContext context, UniversalTarget target)
        {
            // Only support SpriteColor legacy block if BaseColor/Alpha are not active
            var descs = context.blocks.Select(x => x.descriptor);
            bool useLegacyBlocks = !descs.Contains(BlockFields.SurfaceDescription.BaseColor) && !descs.Contains(BlockFields.SurfaceDescription.Alpha);
            context.AddField(CoreFields.UseLegacySpriteBlocks, useLegacyBlocks);

            // Surface Type
            context.AddField(UniversalFields.SurfaceTransparent);
            context.AddField(Fields.DoubleSided);

            // Blend Mode
            switch (target.alphaMode)
            {
                case AlphaMode.Premultiply:
                    context.AddField(UniversalFields.BlendPremultiply);
                    break;
                case AlphaMode.Additive:
                    context.AddField(UniversalFields.BlendAdd);
                    break;
                case AlphaMode.Multiply:
                    context.AddField(UniversalFields.BlendMultiply);
                    break;
                default:
                    context.AddField(Fields.BlendAlpha);
                    break;
            }
        }

        public static void GetDefaultActiveBlocks(ref TargetActiveBlockContext context, UniversalTarget target)
        {
            // Only support SpriteColor legacy block if BaseColor/Alpha are not active
            bool useLegacyBlocks = !context.currentBlocks.Contains(BlockFields.SurfaceDescription.BaseColor) && !context.currentBlocks.Contains(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescriptionLegacy.SpriteColor, useLegacyBlocks);

            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, target.alphaClip);
        }

        public static void AddDefaultPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo, UniversalTarget target)
        {
            context.AddProperty("Blending Mode", new UnityEngine.UIElements.EnumField(AlphaMode.Alpha) { value = target.alphaMode }, (evt) =>
            {
                if (Equals(target.alphaMode, evt.newValue))
                    return;

                registerUndo("Change Blend");
                target.alphaMode = (AlphaMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Alpha Clipping", new Toggle() { value = target.alphaClip }, (evt) =>
            {
                if (Equals(target.alphaClip, evt.newValue))
                    return;

                registerUndo("Change Alpha Clip");
                target.alphaClip = evt.newValue;
                onChange();
            });
        }

        public static void AddAlphaClipControlToPass(ref PassDescriptor pass, UniversalTarget target)
        {
            if (target.alphaClip)
                pass.defines.Add(CoreKeywordDescriptors.AlphaClipThreshold, 1);
        }
    }
}
