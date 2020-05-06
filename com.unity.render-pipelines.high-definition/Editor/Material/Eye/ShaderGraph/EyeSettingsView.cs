using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class EyeSettingsView
    {
        SystemData systemData;
        BuiltinData builtinData;
        LightingData lightingData;
        EyeData eyeData;

        IntegerField m_SortPriorityField;

        public EyeSettingsView(EyeSubTarget subTarget)
        {
            systemData = subTarget.systemData;
            builtinData = subTarget.builtinData;
            lightingData = subTarget.lightingData;
            eyeData = subTarget.eyeData;
        }

        public void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            // Render State
            DoRenderStateArea(ref context, systemData, 0, onChange, registerUndo);

            // Alpha Test
            // TODO: AlphaTest is in SystemData but Alpha to Mask is in BuiltinData?
            context.AddProperty("Alpha Clipping", 0, new Toggle() { value = systemData.alphaTest }, (evt) =>
            {
                if (Equals(systemData.alphaTest, evt.newValue))
                    return;

                registerUndo("Alpha Clipping");
                systemData.alphaTest = evt.newValue;
                onChange();
            });
            context.AddProperty("Alpha to Mask", 1, new Toggle() { value = builtinData.alphaToMask }, systemData.alphaTest, (evt) =>
            {
                if (Equals(builtinData.alphaToMask, evt.newValue))
                    return;

                registerUndo("Alpha to Mask");
                builtinData.alphaToMask = evt.newValue;
                onChange();
            });
            context.AddProperty("Alpha Cutoff Depth Prepass", 1, new Toggle() { value = systemData.alphaTestDepthPrepass }, systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest, (evt) =>
            {
                if (Equals(systemData.alphaTestDepthPrepass, evt.newValue))
                    return;

                registerUndo("Alpha Cutoff Depth Prepass");
                systemData.alphaTestDepthPrepass = evt.newValue;
                onChange();
            });
            context.AddProperty("Alpha Cutoff Depth Postpass", 1, new Toggle() { value = systemData.alphaTestDepthPostpass }, systemData.surfaceType == SurfaceType.Transparent && systemData.alphaTest, (evt) =>
            {
                if (Equals(systemData.alphaTestDepthPostpass, evt.newValue))
                    return;

                registerUndo("Alpha Cutoff Depth Postpass");
                systemData.alphaTestDepthPostpass = evt.newValue;
                onChange();
            });

            // Misc
            context.AddProperty("Double-Sided Mode", 0, new EnumField(DoubleSidedMode.Disabled) { value = systemData.doubleSidedMode }, (evt) =>
            {
                if (Equals(systemData.doubleSidedMode, evt.newValue))
                    return;

                registerUndo("Double-Sided Mode");
                systemData.doubleSidedMode = (DoubleSidedMode)evt.newValue;
                onChange();
            });
            context.AddProperty("Material Type", 0, new EnumField(EyeData.MaterialType.Eye) { value = eyeData.materialType }, (evt) =>
            {
                if (Equals(eyeData.materialType, evt.newValue))
                    return;

                registerUndo("Material Type");
                eyeData.materialType = (EyeData.MaterialType)evt.newValue;
                onChange();
            });
            context.AddProperty("Subsurface Scattering", 0, new Toggle() { value = lightingData.subsurfaceScattering }, systemData.surfaceType != SurfaceType.Transparent, (evt) =>
            {
                if (Equals(lightingData.subsurfaceScattering, evt.newValue))
                    return;

                registerUndo("Subsurface Scattering");
                lightingData.subsurfaceScattering = evt.newValue;
                onChange();
            });
            context.AddProperty("Receive Decals", 0, new Toggle() { value = lightingData.receiveDecals }, (evt) =>
            {
                if (Equals(lightingData.receiveDecals, evt.newValue))
                    return;

                registerUndo("Receive Decals");
                lightingData.receiveDecals = evt.newValue;
                onChange();
            });
            context.AddProperty("Receive SSR", 0, new Toggle() { value = lightingData.receiveSSR }, (evt) =>
            {
                if (Equals(lightingData.receiveSSR, evt.newValue))
                    return;

                registerUndo("Receive SSR");
                lightingData.receiveSSR = evt.newValue;
                onChange();
            });
            context.AddProperty("Add Precomputed Velocity", 0, new Toggle() { value = builtinData.addPrecomputedVelocity }, (evt) =>
            {
                if (Equals(builtinData.addPrecomputedVelocity, evt.newValue))
                    return;

                registerUndo("Add Precomputed Velocity");
                builtinData.addPrecomputedVelocity = evt.newValue;
                onChange();
            });
            context.AddProperty("Specular Occlusion Mode", 0, new EnumField(SpecularOcclusionMode.Off) { value = lightingData.specularOcclusionMode }, (evt) =>
            {
                if (Equals(lightingData.specularOcclusionMode, evt.newValue))
                    return;

                registerUndo("Specular Occlusion Mode");
                lightingData.specularOcclusionMode = (SpecularOcclusionMode)evt.newValue;
                onChange();
            });
            context.AddProperty("Override Baked GI", 0, new Toggle() { value = lightingData.overrideBakedGI }, (evt) =>
            {
                if (Equals(lightingData.overrideBakedGI, evt.newValue))
                    return;

                registerUndo("Override Baked GI");
                lightingData.overrideBakedGI = evt.newValue;
                onChange();
            });
            context.AddProperty("Depth Offset", 0, new Toggle() { value = builtinData.depthOffset }, (evt) =>
            {
                if (Equals(builtinData.depthOffset, evt.newValue))
                    return;

                registerUndo("Depth Offset");
                builtinData.depthOffset = evt.newValue;
                onChange();
            });
            context.AddProperty("Support LOD CrossFade", 0, new Toggle() { value = systemData.supportLodCrossFade }, (evt) =>
            {
                if (Equals(systemData.supportLodCrossFade, evt.newValue))
                    return;

                registerUndo("Support LOD CrossFade");
                systemData.supportLodCrossFade = evt.newValue;
                onChange();
            });
        }

        void DoRenderStateArea(ref TargetPropertyGUIContext context, SystemData systemData, int indentLevel, Action onChange, Action<string> registerUndo)
        {
            context.AddProperty("Surface Type", indentLevel, new EnumField(SurfaceType.Opaque) { value = systemData.surfaceType }, (evt) =>
            {
                if (Equals(systemData.surfaceType, evt.newValue))
                    return;

                registerUndo("Surface Type");
                systemData.surfaceType = (SurfaceType)evt.newValue;
                systemData.TryChangeRenderingPass(systemData.renderingPass);
                onChange();
            });

            context.AddProperty("Blend Preserves Specular", indentLevel + 1, new Toggle() { value = lightingData.blendPreserveSpecular }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(lightingData.blendPreserveSpecular, evt.newValue))
                    return;

                registerUndo("Blend Preserves Specular");
                lightingData.blendPreserveSpecular = evt.newValue;
                onChange();
            });

            context.AddProperty("Fog", indentLevel + 1, new Toggle() { value = builtinData.transparencyFog }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(builtinData.transparencyFog, evt.newValue))
                    return;

                registerUndo("Fog");
                builtinData.transparencyFog = evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Test", indentLevel + 1, new EnumField(systemData.zTest) { value = systemData.zTest }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(systemData.zTest, evt.newValue))
                    return;

                registerUndo("Depth Test");
                systemData.zTest = (CompareFunction)evt.newValue;
                onChange();
            });

            context.AddProperty("Depth Write", indentLevel + 1, new Toggle() { value = systemData.zWrite }, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                if (Equals(systemData.zWrite, evt.newValue))
                    return;

                registerUndo("Depth Write");
                systemData.zWrite = evt.newValue;
                onChange();
            });

            context.AddProperty("Cull Mode", indentLevel + 1, new EnumField(systemData.transparentCullMode) { value = systemData.transparentCullMode }, systemData.surfaceType == SurfaceType.Transparent && systemData.doubleSidedMode == DoubleSidedMode.Disabled, (evt) =>
            {
                if (Equals(systemData.transparentCullMode, evt.newValue))
                    return;

                registerUndo("Cull Mode"); 
                systemData.transparentCullMode = (TransparentCullMode)evt.newValue;
                onChange();
            });

            m_SortPriorityField = new IntegerField() { value = systemData.sortPriority };
            context.AddProperty("Sorting Priority", indentLevel + 1, m_SortPriorityField, systemData.surfaceType == SurfaceType.Transparent, (evt) =>
            {
                var newValue = HDRenderQueue.ClampsTransparentRangePriority(evt.newValue);
                if (Equals(systemData.sortPriority, newValue))
                    return;
                
                registerUndo("Sorting Priority");
                m_SortPriorityField.value = newValue;
                systemData.sortPriority = evt.newValue;
                onChange();
            });
        }
    }
}
