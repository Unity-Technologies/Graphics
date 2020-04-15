using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDUnlitSettingsView
    {
        HDSystemData systemData;
        HDBuiltinData builtinData;
        HDUnlitData unlitData;

        public HDUnlitSettingsView(HDUnlitSubTarget subTarget)
        {
            systemData = subTarget.systemData;
            builtinData = subTarget.builtinData;
            unlitData = subTarget.unlitData;
        }

        public void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange)
        {
            // TODO: Register Undo actions...

            // Render State
            DoRenderStateArea(ref context, systemData, 0, onChange);
            
            // Transparent
            context.AddProperty("Receive Fog", 1, new Toggle() { value = builtinData.transparencyFog }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(builtinData.transparencyFog, evt.newValue))
                    return;

                builtinData.transparencyFog = evt.newValue;
                onChange();
            });

            // Distortion
            if(systemData.surfaceType == SurfaceType.Transparent)
            {
                DoDistortionArea(ref context, builtinData, 1, onChange);
            }

            // Alpha Test
            // TODO: AlphaTest is in SystemData but Alpha to Mask is in BuiltinData?
            context.AddProperty("Alpha Clipping", 0, new Toggle() { value = systemData.alphaTest }, (evt) =>
            {
                if (Equals(systemData.alphaTest, evt.newValue))
                    return;

                systemData.alphaTest = evt.newValue;
                onChange();
            });
            context.AddProperty("Alpha to Mask", 0, new Toggle() { value = builtinData.alphaToMask }, (evt) =>
            {
                if (Equals(builtinData.alphaToMask, evt.newValue))
                    return;

                builtinData.alphaToMask = evt.newValue;
                onChange();
            });

            // Misc
            context.AddProperty("Double-Sided Mode", 0, new EnumField(DoubleSidedMode.Disabled) { value = systemData.doubleSidedMode }, (evt) =>
            {
                if (Equals(systemData.doubleSidedMode, evt.newValue))
                    return;

                systemData.doubleSidedMode = (DoubleSidedMode)evt.newValue;
                onChange();
            });
            context.AddProperty("Add Precomputed Velocity", 0, new Toggle() { value = builtinData.addPrecomputedVelocity }, (evt) =>
            {
                if (Equals(builtinData.addPrecomputedVelocity, evt.newValue))
                    return;

                builtinData.addPrecomputedVelocity = evt.newValue;
                onChange();
            });
            context.AddProperty("Shadow Matte", 0, new Toggle() { value = unlitData.enableShadowMatte }, (evt) =>
            {
                if (Equals(unlitData.enableShadowMatte, evt.newValue))
                    return;

                unlitData.enableShadowMatte = evt.newValue;
                onChange();
            });
        }

        // TODO: Can we make this static and use it for all SubTargets?
        void DoRenderStateArea(ref TargetPropertyGUIContext context, HDSystemData systemData, int indentLevel, Action onChange)
        {
            context.AddProperty("Surface Type", indentLevel, new EnumField(SurfaceType.Opaque) { value = systemData.surfaceType }, (evt) =>
            {
                if (Equals(systemData.surfaceType, evt.newValue))
                    return;

                systemData.surfaceType = (SurfaceType)evt.newValue;
                systemData.TryChangeRenderingPass(systemData.renderingPass);
                onChange();
            });

            var renderingPassList = HDSubShaderUtilities.GetRenderingPassList(systemData.surfaceType == SurfaceType.Opaque, true);
            var renderingPassValue = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.GetOpaqueEquivalent(systemData.renderingPass) : HDRenderQueue.GetTransparentEquivalent(systemData.renderingPass);
            var renderQueueType = systemData.surfaceType == SurfaceType.Opaque ? HDRenderQueue.RenderQueueType.Opaque : HDRenderQueue.RenderQueueType.Transparent;
            context.AddProperty("Rendering Pass", indentLevel + 1, new PopupField<HDRenderQueue.RenderQueueType>(renderingPassList, renderQueueType, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName) { value = renderingPassValue }, (evt) =>
            {
                if(systemData.TryChangeRenderingPass(evt.newValue))
                {
                    onChange();
                }
            });

            context.AddProperty("Blending Mode", indentLevel + 1, new EnumField(BlendMode.Alpha) { value = systemData.blendMode }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(systemData.blendMode, evt.newValue))
                    return;

                systemData.blendMode = (BlendMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Test", indentLevel + 1, new EnumField(systemData.zTest) { value = systemData.zTest }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(systemData.zTest, evt.newValue))
                    return;

                systemData.zTest = (CompareFunction)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Write", indentLevel + 1, new Toggle() { value = systemData.zWrite }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(systemData.zWrite, evt.newValue))
                    return;

                systemData.zWrite = evt.newValue;
                onChange();
            });

            context.AddProperty("Cull Mode", indentLevel + 1, new EnumField(systemData.transparentCullMode) { value = systemData.transparentCullMode }, systemData.surfaceType == SurfaceType.Transparent && systemData.doubleSidedMode != DoubleSidedMode.Disabled, (evt) =>
            {
                if (Equals(systemData.transparentCullMode, evt.newValue))
                    return;

                systemData.transparentCullMode = (TransparentCullMode)evt.newValue;
                onChange();
            });

            context.AddProperty("Sorting Priority", indentLevel + 1, new IntegerField() { value = systemData.sortPriority }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(systemData.sortPriority, evt.newValue))
                    return;

                systemData.sortPriority = evt.newValue;
                onChange();
            });
        }

        // TODO: Can we make this static and use it for all SubTargets?
        void DoDistortionArea(ref TargetPropertyGUIContext context, HDBuiltinData builtinData, int indentLevel, Action onChange)
        {
            context.AddProperty("Distortion", 1, new Toggle() { value = builtinData.distortion }, (evt) =>
            {
                if (Equals(builtinData.distortion, evt.newValue))
                    return;

                builtinData.distortion = evt.newValue;
                onChange();
            });

            context.AddProperty("Distortion Blend Mode", 2, new EnumField(DistortionMode.Add) { value = builtinData.distortionMode }, builtinData.distortion, (evt) =>
            {
                if (Equals(builtinData.distortionMode, evt.newValue))
                    return;

                builtinData.distortionMode = (DistortionMode)evt.newValue;
                onChange();
            });

            // TODO: This was on HDUnlitMaster but not used anywhere
            // TODO: Can this be removed (See HDBuiltinData)?
            // context.AddProperty("Distortion Only", 2, new Toggle() { value = builtinData.distortionOnly }, builtinData.distortion, (evt) =>
            // {
            //     if (Equals(builtinData.distortionOnly, evt.newValue))
            //         return;

            //     builtinData.distortionOnly = evt.newValue;
            //     onChange();
            // });

            context.AddProperty("Distortion Depth Test", 2, new Toggle() { value = builtinData.distortionDepthTest }, builtinData.distortion, (evt) =>
            {
                if (Equals(builtinData.distortionDepthTest, evt.newValue))
                    return;

                builtinData.distortionDepthTest = evt.newValue;
                onChange();
            });
        }
    }
}
