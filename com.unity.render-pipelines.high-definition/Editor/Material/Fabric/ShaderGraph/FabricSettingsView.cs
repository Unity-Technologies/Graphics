using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.Drawing
{
    class FabricSettingsView : MasterNodeSettingsView
    {
        FabricMasterNode m_Node;

        IntegerField m_SortPiorityField;

        Label CreateLabel(string text, int indentLevel)
        {
            string label = "";
            for (var i = 0; i < indentLevel; i++)
            {
                label += "    ";
            }
            return new Label(label + text);
        }

        public FabricSettingsView(FabricMasterNode node) : base(node)
        {
            m_Node = node;
            PropertySheet ps = new PropertySheet();

            int indentLevel = 0;
            ps.Add(new PropertyRow(CreateLabel("Surface Type", indentLevel)), (row) =>
            {
                row.Add(new EnumField(SurfaceType.Opaque), (field) =>
                {
                    field.value = m_Node.surfaceType;
                    field.RegisterValueChangedCallback(ChangeSurfaceType);
                });
            });

            if (m_Node.surfaceType == SurfaceType.Transparent)
            {
                ++indentLevel;

                ps.Add(new PropertyRow(CreateLabel("Blend Preserves Specular", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.blendPreserveSpecular.isOn;
                        toggle.OnToggleChanged(ChangeBlendPreserveSpecular);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Fog", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.transparencyFog.isOn;
                        toggle.OnToggleChanged(ChangeTransparencyFog);
                    });
                });

                m_SortPiorityField = new IntegerField();
                ps.Add(new PropertyRow(CreateLabel("Sort Priority", indentLevel)), (row) =>
                {
                    row.Add(m_SortPiorityField, (field) =>
                    {
                        field.value = m_Node.sortPriority;
                        field.RegisterValueChangedCallback(ChangeSortPriority);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Depth Write", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.zWrite.isOn;
                        toggle.OnToggleChanged(ChangeZWrite);
                    });
                });

                if (m_Node.doubleSidedMode == DoubleSidedMode.Disabled)
                {
                    ps.Add(new PropertyRow(CreateLabel("Cull Mode", indentLevel)), (row) =>
                    {
                        row.Add(new EnumField(m_Node.transparentCullMode), (e) =>
                        {
                            e.value = m_Node.transparentCullMode;
                            e.RegisterValueChangedCallback(ChangeTransparentCullMode);
                        });
                    });
                }

                ps.Add(new PropertyRow(CreateLabel("Depth Test", indentLevel)), (row) =>
                {
                    row.Add(new EnumField(m_Node.zTest), (e) =>
                    {
                        e.value = m_Node.zTest;
                        e.RegisterValueChangedCallback(ChangeZTest);
                    });
                });

                --indentLevel;
            }

            ps.Add(new PropertyRow(CreateLabel("Alpha Clipping", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.alphaTest.isOn;
                    toggle.OnToggleChanged(ChangeAlphaTest);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Double-Sided", indentLevel)), (row) =>
            {
                row.Add(new EnumField(DoubleSidedMode.Disabled), (field) =>
                {
                    field.value = m_Node.doubleSidedMode;
                    field.RegisterValueChangedCallback(ChangeDoubleSidedMode);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Energy Conserving Specular", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.energyConservingSpecular.isOn;
                    toggle.OnToggleChanged(ChangeEnergyConservingSpecular);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Material Type", indentLevel)), (row) =>
            {
                row.Add(new EnumField(FabricMasterNode.MaterialType.CottonWool), (field) =>
                {
                    field.value = m_Node.materialType;
                    field.RegisterValueChangedCallback(ChangeMaterialType);
                });
            });

            if (m_Node.surfaceType != SurfaceType.Transparent)
            {
                ps.Add(new PropertyRow(CreateLabel("Subsurface Scattering", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.subsurfaceScattering.isOn;
                        toggle.OnToggleChanged(ChangeSubsurfaceScattering);
                    });
                });
            }

            ps.Add(new PropertyRow(CreateLabel("Transmission", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.transmission.isOn;
                    toggle.OnToggleChanged(ChangeTransmission);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Receive Decals", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.receiveDecals.isOn;
                    toggle.OnToggleChanged(ChangeDecal);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Receive SSR", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.receiveSSR.isOn;
                    toggle.OnToggleChanged(ChangeSSR);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Add Precomputed Velocity", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.addPrecomputedVelocity.isOn;
                    toggle.OnToggleChanged(ChangeAddPrecomputedVelocity);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Specular Occlusion Mode", indentLevel)), (row) =>
            {
                row.Add(new EnumField(SpecularOcclusionMode.Off), (field) =>
                {
                    field.value = m_Node.specularOcclusionMode;
                    field.RegisterValueChangedCallback(ChangeSpecularOcclusionMode);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Override Baked GI", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.overrideBakedGI.isOn;
                    toggle.OnToggleChanged(ChangeoverrideBakedGI);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Depth Offset", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.depthOffset.isOn;
                    toggle.OnToggleChanged(ChangeDepthOffset);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("DOTS instancing", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.dotsInstancing.isOn;
                    toggle.OnToggleChanged(ChangeDotsInstancing);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Support LOD CrossFade", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.supportLodCrossFade.isOn;
                    toggle.OnToggleChanged(ChangeSupportLODCrossFade);
                });
            });

            Add(ps);
            Add(GetShaderGUIOverridePropertySheet());
        }

        void ChangeSurfaceType(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.surfaceType, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Surface Type Change");
            m_Node.surfaceType = (SurfaceType)evt.newValue;
        }

        void ChangeDoubleSidedMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.doubleSidedMode, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Double-Sided Mode Change");
            m_Node.doubleSidedMode = (DoubleSidedMode)evt.newValue;
        }

        void ChangeMaterialType(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.materialType, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Material Type Change");
            m_Node.materialType = (FabricMasterNode.MaterialType)evt.newValue;
        }

        void ChangeTransmission(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Transmission Change");
            ToggleData td = m_Node.transmission;
            td.isOn = evt.newValue;
            m_Node.transmission = td;
        }

        void ChangeSubsurfaceScattering(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("SSS Change");
            ToggleData td = m_Node.subsurfaceScattering;
            td.isOn = evt.newValue;
            m_Node.subsurfaceScattering = td;
        }

        void ChangeBlendMode(ChangeEvent<Enum> evt)
        {
            // Make sure the mapping is correct by handling each case.
            AlphaMode alphaMode = GetAlphaMode((FabricMasterNode.AlphaModeFabric)evt.newValue);

            if (Equals(m_Node.alphaMode, alphaMode))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Mode Change");
            m_Node.alphaMode = alphaMode;
        }

        void ChangeBlendPreserveSpecular(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Blend Preserve Specular Change");
            ToggleData td = m_Node.blendPreserveSpecular;
            td.isOn = evt.newValue;
            m_Node.blendPreserveSpecular = td;
        }

        void ChangeTransparencyFog(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Transparency Fog Change");
            ToggleData td = m_Node.transparencyFog;
            td.isOn = evt.newValue;
            m_Node.transparencyFog = td;
        }

        void ChangeSortPriority(ChangeEvent<int> evt)
        {
            m_Node.sortPriority = HDRenderQueue.ClampsTransparentRangePriority(evt.newValue);
            // Force the text to match.
            m_SortPiorityField.value = m_Node.sortPriority;
            if (Equals(m_Node.sortPriority, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Sort Priority Change");
        }

        void ChangeZWrite(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("ZWrite Change");
            ToggleData td = m_Node.zWrite;
            td.isOn = evt.newValue;
            m_Node.zWrite = td;
        }

        void ChangeAlphaTest(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Test Change");
            ToggleData td = m_Node.alphaTest;
            td.isOn = evt.newValue;
            m_Node.alphaTest = td;
        }

        void ChangeDecal(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Decal Change");
            ToggleData td = m_Node.receiveDecals;
            td.isOn = evt.newValue;
            m_Node.receiveDecals = td;
        }

        void ChangeSSR(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("SSR Change");
            ToggleData td = m_Node.receiveSSR;
            td.isOn = evt.newValue;
            m_Node.receiveSSR = td;
        }

        void ChangeAddPrecomputedVelocity(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Add Precomputed Velocity");
            ToggleData td = m_Node.addPrecomputedVelocity;
            td.isOn = evt.newValue;
            m_Node.addPrecomputedVelocity = td;
        }

        void ChangeEnergyConservingSpecular(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Energy Conserving Specular Change");
            ToggleData td = m_Node.energyConservingSpecular;
            td.isOn = evt.newValue;
            m_Node.energyConservingSpecular = td;
        }

        void ChangeSpecularOcclusionMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.specularOcclusionMode, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Specular Occlusion Mode Change");
            m_Node.specularOcclusionMode = (SpecularOcclusionMode)evt.newValue;
        }

        void ChangeoverrideBakedGI(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("overrideBakedGI Change");
            ToggleData td = m_Node.overrideBakedGI;
            td.isOn = evt.newValue;
            m_Node.overrideBakedGI = td;
        }

        void ChangeDepthOffset(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("DepthOffset Change");
            ToggleData td = m_Node.depthOffset;
            td.isOn = evt.newValue;
            m_Node.depthOffset = td;
        }

        void ChangeTransparentCullMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.transparentCullMode, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Transparent Cull Mode Change");
            m_Node.transparentCullMode = (TransparentCullMode)evt.newValue;
        }

        void ChangeZTest(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.zTest, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("ZTest Change");
            m_Node.zTest = (CompareFunction)evt.newValue;
        }

        void ChangeDotsInstancing(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("DotsInstancing Change");
            ToggleData td = m_Node.dotsInstancing;
            td.isOn = evt.newValue;
            m_Node.dotsInstancing = td;
        }

        void ChangeSupportLODCrossFade(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Support LOD CrossFade Change");
            ToggleData td = m_Node.supportLodCrossFade;
            td.isOn = evt.newValue;
            m_Node.supportLodCrossFade = td;
        }

        public AlphaMode GetAlphaMode(FabricMasterNode.AlphaModeFabric alphaModeLit)
        {
            switch (alphaModeLit)
            {
                case FabricMasterNode.AlphaModeFabric.Alpha:
                    return AlphaMode.Alpha;
                case FabricMasterNode.AlphaModeFabric.Premultiply:
                    return AlphaMode.Premultiply;
                case FabricMasterNode.AlphaModeFabric.Additive:
                    return AlphaMode.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaModeLit);
                        return AlphaMode.Alpha;
                    }

            }
        }

        public FabricMasterNode.AlphaModeFabric GetAlphaModeLit(AlphaMode alphaMode)
        {
            switch (alphaMode)
            {
                case AlphaMode.Alpha:
                    return FabricMasterNode.AlphaModeFabric.Alpha;
                case AlphaMode.Premultiply:
                    return FabricMasterNode.AlphaModeFabric.Premultiply;
                case AlphaMode.Additive:
                    return FabricMasterNode.AlphaModeFabric.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaMode);
                        return FabricMasterNode.AlphaModeFabric.Alpha;
                    }
            }
        }
    }
}
