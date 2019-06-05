using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline.Drawing
{
    class HairSettingsView : VisualElement
    {
        HairMasterNode m_Node;

        IntegerField m_SortPriorityField;

        Label CreateLabel(string text, int indentLevel)
        {
            string label = "";
            for (var i = 0; i < indentLevel; i++)
            {
                label += "    ";
            }
            return new Label(label + text);
        }

        public HairSettingsView(HairMasterNode node)
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

                ps.Add(new PropertyRow(CreateLabel("Preserve Specular Lighting", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.blendPreserveSpecular.isOn;
                        toggle.OnToggleChanged(ChangeBlendPreserveSpecular);
                    });
                });

                m_SortPriorityField = new IntegerField();
                ps.Add(new PropertyRow(CreateLabel("Sorting Priority", indentLevel)), (row) =>
                {
                    row.Add(m_SortPriorityField, (field) =>
                    {
                        field.value = m_Node.sortPriority;
                        field.RegisterValueChangedCallback(ChangeSortPriority);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Receive Fog", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.transparencyFog.isOn;
                        toggle.OnToggleChanged(ChangeTransparencyFog);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Back Then Front Rendering", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.backThenFrontRendering.isOn;
                        toggle.OnToggleChanged(ChangeBackThenFrontRendering);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Transparent Depth Prepass", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.alphaTestDepthPrepass.isOn;
                        toggle.OnToggleChanged(ChangeAlphaTestPrepass);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Transparent Depth Postpass", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.alphaTestDepthPostpass.isOn;
                        toggle.OnToggleChanged(ChangeAlphaTestPostpass);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Transparent Writes Motion Vector", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.transparentWritesMotionVec.isOn;
                        toggle.OnToggleChanged(ChangeTransparentWritesMotionVec);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("ZWrite", indentLevel)), (row) =>
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

                ps.Add(new PropertyRow(CreateLabel("Z Test", indentLevel)), (row) =>
                {
                    row.Add(new EnumField(m_Node.zTest), (e) =>
                    {
                        e.value = m_Node.zTest;
                        e.RegisterValueChangedCallback(ChangeZTest);
                    });
                });

                --indentLevel;
            }

            ps.Add(new PropertyRow(CreateLabel("Double-Sided", indentLevel)), (row) =>
            {
                row.Add(new EnumField(DoubleSidedMode.Disabled), (field) =>
                {
                    field.value = m_Node.doubleSidedMode;
                    field.RegisterValueChangedCallback(ChangeDoubleSidedMode);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Alpha Clipping", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.alphaTest.isOn;
                    toggle.OnToggleChanged(ChangeAlphaTest);
                });
            });

            if (m_Node.alphaTest.isOn)
            {
                ++indentLevel;
                ps.Add(new PropertyRow(CreateLabel("Use Shadow Threshold", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.alphaTestShadow.isOn;
                        toggle.OnToggleChanged(ChangeAlphaTestShadow);
                    });
                });
                --indentLevel;
            }

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

            ps.Add(new PropertyRow(CreateLabel("Geometric Specular AA", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.specularAA.isOn;
                    toggle.OnToggleChanged(ChangeSpecularAA);
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

            ps.Add(new PropertyRow(CreateLabel("Use Light Facing Normal", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.useLightFacingNormal.isOn;
                    toggle.OnToggleChanged(ChangeUseLightFacingNormal);
                });
            });

            Add(ps);
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

        void ChangeBlendMode(ChangeEvent<Enum> evt)
        {
            // Make sure the mapping is correct by handling each case.
            AlphaMode alphaMode = GetAlphaMode((HairMasterNode.AlphaModeLit)evt.newValue);

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

        void ChangeBackThenFrontRendering(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Back Then Front Rendering Change");
            ToggleData td = m_Node.backThenFrontRendering;
            td.isOn = evt.newValue;
            m_Node.backThenFrontRendering = td;
        }

        void ChangeSortPriority(ChangeEvent<int> evt)
        {
            m_Node.sortPriority = HDRenderQueue.ClampsTransparentRangePriority(evt.newValue);
            // Force the text to match.
            m_SortPriorityField.value = m_Node.sortPriority;
            if (Equals(m_Node.sortPriority, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Sort Priority Change");
        }

        void ChangeAlphaTest(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Test Change");
            ToggleData td = m_Node.alphaTest;
            td.isOn = evt.newValue;
            m_Node.alphaTest = td;
        }

        void ChangeAlphaTestPrepass(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Test Depth Prepass Change");
            ToggleData td = m_Node.alphaTestDepthPrepass;
            td.isOn = evt.newValue;
            m_Node.alphaTestDepthPrepass = td;
        }

        void ChangeAlphaTestPostpass(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Test Depth Postpass Change");
            ToggleData td = m_Node.alphaTestDepthPostpass;
            td.isOn = evt.newValue;
            m_Node.alphaTestDepthPostpass = td;
        }

        void ChangeTransparentWritesMotionVec(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Transparent Writes Motion Vector Change");
            ToggleData td = m_Node.transparentWritesMotionVec;
            td.isOn = evt.newValue;
            m_Node.transparentWritesMotionVec = td;
        }

        void ChangeAlphaTestShadow(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Test Shadow Change");
            ToggleData td = m_Node.alphaTestShadow;
            td.isOn = evt.newValue;
            m_Node.alphaTestShadow = td;
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

        void ChangeUseLightFacingNormal(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Use Light Facing Normal Change");
            ToggleData td = m_Node.useLightFacingNormal;
            td.isOn = evt.newValue;
            m_Node.useLightFacingNormal = td;
        }

        void ChangeSpecularAA(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Specular AA Change");
            ToggleData td = m_Node.specularAA;
            td.isOn = evt.newValue;
            m_Node.specularAA = td;
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

        void ChangeZWrite(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("ZWrite Change");
            ToggleData td = m_Node.zWrite;
            td.isOn = evt.newValue;
            m_Node.zWrite = td;
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

        public AlphaMode GetAlphaMode(HairMasterNode.AlphaModeLit alphaModeLit)
        {
            switch (alphaModeLit)
            {
                case HairMasterNode.AlphaModeLit.Alpha:
                    return AlphaMode.Alpha;
                case HairMasterNode.AlphaModeLit.Premultiply:
                    return AlphaMode.Premultiply;
                case HairMasterNode.AlphaModeLit.Additive:
                    return AlphaMode.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaModeLit);
                        return AlphaMode.Alpha;
                    }

            }
        }

        public HairMasterNode.AlphaModeLit GetAlphaModeLit(AlphaMode alphaMode)
        {
            switch (alphaMode)
            {
                case AlphaMode.Alpha:
                    return HairMasterNode.AlphaModeLit.Alpha;
                case AlphaMode.Premultiply:
                    return HairMasterNode.AlphaModeLit.Premultiply;
                case AlphaMode.Additive:
                    return HairMasterNode.AlphaModeLit.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaMode);
                        return HairMasterNode.AlphaModeLit.Alpha;
                    }
            }
        }
    }
}
