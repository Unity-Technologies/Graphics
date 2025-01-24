using System;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using static UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    internal static class SpriteSubTargetUtility
    {
        public static RenderStateCollection GetDefaultRenderState(UniversalTarget target)
        {
            var result = CoreRenderStates.Default;

            // Add Z write
            if (target.zWriteControl == ZWriteControl.ForceEnabled)
                result.Add(RenderState.ZWrite(ZWrite.On));
            else
                result.Add(RenderState.ZWrite(ZWrite.Off), new FieldCondition(UniversalFields.SurfaceTransparent, true));

            // Add Z test
            result.Add(RenderState.ZTest(target.zTestMode.ToString()));

            return result;
        }

        public static void AddDefaultFields(ref TargetFieldContext context, UniversalTarget target)
        {
            // Only support SpriteColor legacy block if BaseColor/Alpha are not active
            var descs = context.blocks.Select(x => x.descriptor);
            bool useLegacyBlocks = descs.Contains(BlockFields.SurfaceDescriptionLegacy.SpriteColor);
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
            bool useLegacyBlocks = context.currentBlocks.Contains(BlockFields.SurfaceDescriptionLegacy.SpriteColor);
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

            context.AddProperty("Depth Write", new EnumField(ZWriteControl.ForceDisabled) { value = target.zWriteControl }, (evt) =>
            {
                if (Equals(target.zWriteControl, evt.newValue))
                    return;

                registerUndo("Change Depth Write Control");
                target.zWriteControl = (ZWriteControl)evt.newValue;
                onChange();
            });

            if (target.zWriteControl == ZWriteControl.ForceEnabled)
            {
                context.AddProperty("Depth Test", new EnumField(ZTestModeForUI.LEqual) { value = (ZTestModeForUI)target.zTestMode }, (evt) =>
                {
                    if (Equals(target.zTestMode, evt.newValue))
                        return;

                    registerUndo("Change Depth Test");
                    target.zTestMode = (ZTestMode)evt.newValue;
                    onChange();
                });
            }

            context.AddProperty("Alpha Clipping", new Toggle() { value = target.alphaClip }, (evt) =>
            {
                if (Equals(target.alphaClip, evt.newValue))
                    return;

                registerUndo("Change Alpha Clip");
                target.alphaClip = evt.newValue;
                onChange();
            });

            context.AddProperty("Disable Color Tint", new Toggle() { value = target.disableTint }, (evt) =>
            {
                if (Equals(target.disableTint, evt.newValue))
                    return;

                registerUndo("Change Disable Tint");
                target.disableTint = evt.newValue;
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
