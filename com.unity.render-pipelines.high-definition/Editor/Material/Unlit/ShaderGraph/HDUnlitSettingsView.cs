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

namespace UnityEditor.Experimental.Rendering.HDPipeline.Drawing
{
    class HDUnlitSettingsView : VisualElement
    {
        HDUnlitMasterNode m_Node;

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

        public HDUnlitSettingsView(HDUnlitMasterNode node)
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
                ps.Add(new PropertyRow(CreateLabel("Blending Mode", indentLevel)), (row) =>
                {
                    row.Add(new EnumField(HDUnlitMasterNode.AlphaModeLit.Additive), (field) =>
                    {
                        field.value = GetAlphaModeLit(m_Node.alphaMode);
                        field.RegisterValueChangedCallback(ChangeBlendMode);
                    });
                });

                m_SortPiorityField = new IntegerField();
                ps.Add(new PropertyRow(CreateLabel("Sorting Priority", indentLevel)), (row) =>
                {
                    row.Add(m_SortPiorityField, (field) =>
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

                ps.Add(new PropertyRow(CreateLabel("Appear in Refraction", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.drawBeforeRefraction.isOn;
                        toggle.OnToggleChanged(ChangeDrawBeforeRefraction);
                    });
                });

                ps.Add(new PropertyRow(CreateLabel("Distortion", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.distortion.isOn;
                        toggle.OnToggleChanged(ChangeDistortion);
                    });
                });

                if (m_Node.distortion.isOn)
                {
                    ++indentLevel;
                    ps.Add(new PropertyRow(CreateLabel("Mode", indentLevel)), (row) =>
                    {
                        row.Add(new EnumField(DistortionMode.Add), (field) =>
                        {
                            field.value = m_Node.distortionMode;
                            field.RegisterValueChangedCallback(ChangeDistortionMode);
                        });
                    });
                    ps.Add(new PropertyRow(CreateLabel("Distortion Only", indentLevel)), (row) =>
                    {
                        row.Add(new Toggle(), (toggle) =>
                        {
                            toggle.value = m_Node.distortionOnly.isOn;
                            toggle.OnToggleChanged(ChangeDistortionOnly);
                        });
                    });
                    ps.Add(new PropertyRow(CreateLabel("Depth Test", indentLevel)), (row) =>
                    {
                        row.Add(new Toggle(), (toggle) =>
                        {
                            toggle.value = m_Node.distortionDepthTest.isOn;
                            toggle.OnToggleChanged(ChangeDistortionDepthTest);
                        });
                    });
                    --indentLevel;
                }

                --indentLevel;
            }

            ps.Add(new PropertyRow(new Label("Double-Sided")), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.doubleSided.isOn;
                    toggle.OnToggleChanged(ChangeDoubleSided);
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

            Add(ps);
        }

        void ChangeSurfaceType(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.surfaceType, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Surface Type Change");
            m_Node.surfaceType = (SurfaceType)evt.newValue;
        }

        void ChangeDoubleSided(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Double-Sided Change");
            ToggleData td = m_Node.doubleSided;
            td.isOn = evt.newValue;
            m_Node.doubleSided = td;
        }

        void ChangeBlendMode(ChangeEvent<Enum> evt)
        {
            // Make sure the mapping is correct by handling each case.

            AlphaMode alphaMode = GetAlphaMode((HDUnlitMasterNode.AlphaModeLit)evt.newValue);

            if (Equals(m_Node.alphaMode, alphaMode))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Mode Change");
            m_Node.alphaMode = alphaMode;
        }

        void ChangeTransparencyFog(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Transparency Fog Change");
            ToggleData td = m_Node.transparencyFog;
            td.isOn = evt.newValue;
            m_Node.transparencyFog = td;
        }

        void ChangeDrawBeforeRefraction(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Draw Before Refraction Change");
            ToggleData td = m_Node.drawBeforeRefraction;
            td.isOn = evt.newValue;
            m_Node.drawBeforeRefraction = td;
        }

        void ChangeDistortion(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Distortion Change");
            ToggleData td = m_Node.distortion;
            td.isOn = evt.newValue;
            m_Node.distortion = td;
        }

        void ChangeDistortionMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.distortionMode, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Distortion Mode Change");
            m_Node.distortionMode = (DistortionMode)evt.newValue;
        }

        void ChangeDistortionOnly(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Distortion Only Change");
            ToggleData td = m_Node.distortionOnly;
            td.isOn = evt.newValue;
            m_Node.distortionDepthTest = td;
        }

        void ChangeDistortionDepthTest(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Distortion Depth Test Change");
            ToggleData td = m_Node.distortionDepthTest;
            td.isOn = evt.newValue;
            m_Node.distortionDepthTest = td;
        }

        void ChangeSortPriority(ChangeEvent<int> evt)
        {
            m_Node.sortPriority = Math.Max(-HDRenderQueue.k_TransparentPriorityQueueRange, Math.Min(evt.newValue, HDRenderQueue.k_TransparentPriorityQueueRange));
            // Force the text to match.
            m_SortPiorityField.value = m_Node.sortPriority;
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

        public AlphaMode GetAlphaMode(HDUnlitMasterNode.AlphaModeLit alphaModeLit)
        {
            switch (alphaModeLit)
            {
                case HDUnlitMasterNode.AlphaModeLit.Alpha:
                    return AlphaMode.Alpha;
                case HDUnlitMasterNode.AlphaModeLit.Premultiply:
                    return AlphaMode.Premultiply;
                case HDUnlitMasterNode.AlphaModeLit.Additive:
                    return AlphaMode.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaModeLit);
                        return AlphaMode.Alpha;
                    }

            }
        }

        public HDUnlitMasterNode.AlphaModeLit GetAlphaModeLit(AlphaMode alphaMode)
        {
            switch (alphaMode)
            {
                case AlphaMode.Alpha:
                    return HDUnlitMasterNode.AlphaModeLit.Alpha;
                case AlphaMode.Premultiply:
                    return HDUnlitMasterNode.AlphaModeLit.Premultiply;
                case AlphaMode.Additive:
                    return HDUnlitMasterNode.AlphaModeLit.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaMode);
                        return HDUnlitMasterNode.AlphaModeLit.Alpha;
                    }
            }
        }
    }
}
