using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class FabricSettingsView
    {
        HDSystemData systemData;
        HDBuiltinData builtinData;
        HDLightingData lightingData;
        FabricData fabricData;

        IntegerField m_SortPriorityField;

        public FabricSettingsView(HDFabricSubTarget subTarget)
        {
            systemData = subTarget.systemData;
            builtinData = subTarget.builtinData;
            lightingData = subTarget.lightingData;
            fabricData = subTarget.fabricData;
        }

        public void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange)
        {
            // Render State
            DoRenderStateArea(ref context, systemData, 0, onChange);

            // Alpha Test
            // TODO: AlphaTest is in SystemData but Alpha to Mask is in BuiltinData?
            context.AddProperty("Alpha Clipping", 0, new Toggle() { value = systemData.alphaTest }, (evt) =>
            {
                if (Equals(systemData.alphaTest, evt.newValue))
                    return;

                systemData.alphaTest = evt.newValue;
                onChange();
            });
            context.AddProperty("Alpha to Mask", 1, new Toggle() { value = builtinData.alphaToMask }, systemData.alphaTest, (evt) =>
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
            context.AddProperty("Energy Conserving Specular", 0, new Toggle() { value = lightingData.energyConservingSpecular }, (evt) =>
            {
                if (Equals(lightingData.energyConservingSpecular, evt.newValue))
                    return;

                lightingData.energyConservingSpecular = evt.newValue;
                onChange();
            });
            context.AddProperty("Material Type", 0, new EnumField(FabricData.MaterialType.CottonWool) { value = fabricData.materialType }, (evt) =>
            {
                if (Equals(fabricData.materialType, evt.newValue))
                    return;

                fabricData.materialType = (FabricData.MaterialType)evt.newValue;
                onChange();
            });
            context.AddProperty("Subsurface Scattering", 0, new Toggle() { value = lightingData.subsurfaceScattering }, systemData.surfaceType != SurfaceType.Transparent, (evt) =>
            {
                if (Equals(lightingData.subsurfaceScattering, evt.newValue))
                    return;

                lightingData.subsurfaceScattering = evt.newValue;
                onChange();
            });
            context.AddProperty("Transmission", 0, new Toggle() { value = lightingData.transmission }, (evt) =>
            {
                if (Equals(lightingData.transmission, evt.newValue))
                    return;

                lightingData.transmission = evt.newValue;
                onChange();
            });
            context.AddProperty("Receive Decals", 0, new Toggle() { value = lightingData.receiveDecals }, (evt) =>
            {
                if (Equals(lightingData.receiveDecals, evt.newValue))
                    return;

                lightingData.receiveDecals = evt.newValue;
                onChange();
            });
            context.AddProperty("Receive SSR", 0, new Toggle() { value = lightingData.receiveSSR }, (evt) =>
            {
                if (Equals(lightingData.receiveSSR, evt.newValue))
                    return;

                lightingData.receiveSSR = evt.newValue;
                onChange();
            });
            context.AddProperty("Add Precomputed Velocity", 0, new Toggle() { value = builtinData.addPrecomputedVelocity }, (evt) =>
            {
                if (Equals(builtinData.addPrecomputedVelocity, evt.newValue))
                    return;

                builtinData.addPrecomputedVelocity = evt.newValue;
                onChange();
            });
            context.AddProperty("Specular Occlusion Mode", 0, new EnumField(SpecularOcclusionMode.Off) { value = lightingData.specularOcclusionMode }, (evt) =>
            {
                if (Equals(lightingData.specularOcclusionMode, evt.newValue))
                    return;

                lightingData.specularOcclusionMode = (SpecularOcclusionMode)evt.newValue;
                onChange();
            });
            context.AddProperty("Override Baked GI", 0, new Toggle() { value = lightingData.overrideBakedGI }, (evt) =>
            {
                if (Equals(lightingData.overrideBakedGI, evt.newValue))
                    return;

                lightingData.overrideBakedGI = evt.newValue;
                onChange();
            });
            context.AddProperty("Depth Offset", 0, new Toggle() { value = builtinData.depthOffset }, (evt) =>
            {
                if (Equals(builtinData.depthOffset, evt.newValue))
                    return;

                builtinData.depthOffset = evt.newValue;
                onChange();
            });
            context.AddProperty("Support LOD CrossFade", 0, new Toggle() { value = systemData.supportLodCrossFade }, (evt) =>
            {
                if (Equals(systemData.supportLodCrossFade, evt.newValue))
                    return;

                systemData.supportLodCrossFade = evt.newValue;
                onChange();
            });
        }

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

            context.AddProperty("Blend Preserves Specular", indentLevel + 1, new Toggle() { value = lightingData.blendPreserveSpecular }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(lightingData.blendPreserveSpecular, evt.newValue))
                    return;

                lightingData.blendPreserveSpecular = evt.newValue;
                onChange();
            });

            context.AddProperty("Fog", indentLevel + 1, new Toggle() { value = builtinData.transparencyFog }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(builtinData.transparencyFog, evt.newValue))
                    return;

                builtinData.transparencyFog = evt.newValue;
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

            m_SortPriorityField = new IntegerField() { value = systemData.sortPriority };
            context.AddProperty("Sorting Priority", indentLevel + 1, m_SortPriorityField, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                var newValue = HDRenderQueue.ClampsTransparentRangePriority(evt.newValue);
                if (Equals(systemData.sortPriority, newValue))
                    return;
                
                m_SortPriorityField.value = newValue;
                systemData.sortPriority = evt.newValue;
                onChange();
            });
        }
    }
}
