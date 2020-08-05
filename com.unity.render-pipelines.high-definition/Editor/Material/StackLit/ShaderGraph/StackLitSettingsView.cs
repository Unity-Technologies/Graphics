using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.Drawing
{
    class StackLitSettingsView : MasterNodeSettingsView
    {
        StackLitMasterNode m_Node;

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

        public StackLitSettingsView(StackLitMasterNode node) : base(node)
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

                // No refraction in StackLit, always show this:
                ps.Add(new PropertyRow(CreateLabel("Blending Mode", indentLevel)), (row) =>
                {
                    row.Add(new EnumField(StackLitMasterNode.AlphaModeLit.Additive), (field) =>
                    {
                        field.value = GetAlphaModeLit(m_Node.alphaMode);
                        field.RegisterValueChangedCallback(ChangeBlendMode);
                    });
                });

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
                    ps.Add(new PropertyRow(CreateLabel("Distortion Blend Mode", indentLevel)), (row) =>
                    {
                        row.Add(new EnumField(DistortionMode.Add), (field) =>
                        {
                            field.value = m_Node.distortionMode;
                            field.RegisterValueChangedCallback(ChangeDistortionMode);
                        });
                    });
                    ps.Add(new PropertyRow(CreateLabel("Distortion Depth Test", indentLevel)), (row) =>
                    {
                        row.Add(new Toggle(), (toggle) =>
                        {
                            toggle.value = m_Node.distortionDepthTest.isOn;
                            toggle.OnToggleChanged(ChangeDistortionDepthTest);
                        });
                    });
                    --indentLevel;
                }

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

            ps.Add(new PropertyRow(CreateLabel("Fragment Normal Space", indentLevel)), (row) =>
            {
                row.Add(new EnumField(NormalDropOffSpace.Tangent), (field) =>
                {
                    field.value = m_Node.normalDropOffSpace;
                    field.RegisterValueChangedCallback(ChangeSpaceOfNormalDropOffMode);
                });
            });

            // Rest of UI looks like this:
            //
            //  baseParametrization
            //    energyConservingSpecular
            //
            //  anisotropy
            //  coat
            //  coatNormal
            //  dualSpecularLobe
            //    dualSpecularLobeParametrization
            //    capHazinessWrtMetallic
            //  iridescence
            //  subsurfaceScattering
            //  transmission
            //
            //  receiveDecals
            //  receiveSSR
            //  addPrecomputedVelocity
            //  geometricSpecularAA
            //  specularOcclusion
            //
            //  anisotropyForAreaLights
            //  recomputeStackPerLight
            //  shadeBaseUsingRefractedAngles

            // Base parametrization:

            ps.Add(new PropertyRow(CreateLabel("Base Color Parametrization", indentLevel)), (row) =>
            {
                row.Add(new EnumField(StackLit.BaseParametrization.BaseMetallic), (field) =>
                {
                    field.value = m_Node.baseParametrization;
                    field.RegisterValueChangedCallback(ChangeBaseParametrization);
                });
            });

            if (m_Node.baseParametrization == StackLit.BaseParametrization.SpecularColor)
            {
                ++indentLevel;
                ps.Add(new PropertyRow(CreateLabel("Energy Conserving Specular", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.energyConservingSpecular.isOn;
                        toggle.OnToggleChanged(ChangeEnergyConservingSpecular);
                    });
                });
                --indentLevel;
            }

            // Material type enables:
            ps.Add(new PropertyRow(CreateLabel("Material Core Features", indentLevel)), (row) => {} );
            ++indentLevel;

            ps.Add(new PropertyRow(CreateLabel("Anisotropy", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.anisotropy.isOn;
                    toggle.OnToggleChanged(ChangeAnisotropy);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Coat", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.coat.isOn;
                    toggle.OnToggleChanged(ChangeCoat);
                });
            });

            if (m_Node.coat.isOn)
            {
                ++indentLevel;
                ps.Add(new PropertyRow(CreateLabel("Coat Normal", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.coatNormal.isOn;
                        toggle.OnToggleChanged(ChangeCoatNormal);
                    });
                });
                --indentLevel;
            }

            ps.Add(new PropertyRow(CreateLabel("Dual Specular Lobe", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.dualSpecularLobe.isOn;
                    toggle.OnToggleChanged(ChangeDualSpecularLobe);
                });
            });

            if (m_Node.dualSpecularLobe.isOn)
            {
                ++indentLevel;
                ps.Add(new PropertyRow(CreateLabel("Dual SpecularLobe Parametrization", indentLevel)), (row) =>
                {
                    row.Add(new EnumField(StackLit.DualSpecularLobeParametrization.HazyGloss), (field) =>
                    {
                        field.value = m_Node.dualSpecularLobeParametrization;
                        field.RegisterValueChangedCallback(ChangeDualSpecularLobeParametrization);
                    });
                });
                if ((m_Node.baseParametrization == StackLit.BaseParametrization.BaseMetallic)
                    && (m_Node.dualSpecularLobeParametrization == StackLit.DualSpecularLobeParametrization.HazyGloss))
                {
                    ps.Add(new PropertyRow(CreateLabel("Cap Haziness For Non Metallic", indentLevel)), (row) =>
                    {
                        row.Add(new Toggle(), (toggle) =>
                        {
                            toggle.value = m_Node.capHazinessWrtMetallic.isOn;
                            toggle.OnToggleChanged(ChangeCapHazinessWrtMetallic);
                        });
                    });
                }
                --indentLevel;
            }

            ps.Add(new PropertyRow(CreateLabel("Iridescence", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.iridescence.isOn;
                    toggle.OnToggleChanged(ChangeIridescence);
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
            --indentLevel; // ...Material type enables.

            ps.Add(new PropertyRow(CreateLabel("Receive Decals", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.receiveDecals.isOn;
                    toggle.OnToggleChanged(ChangeReceiveDecals);
                });
            });

            ps.Add(new PropertyRow(CreateLabel("Receive SSR", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.receiveSSR.isOn;
                    toggle.OnToggleChanged(ChangeReceiveSSR);
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

            ps.Add(new PropertyRow(CreateLabel("Geometric Specular AA", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.geometricSpecularAA.isOn;
                    toggle.OnToggleChanged(ChangeGeometricSpecularAA);
                });
            });

            //ps.Add(new PropertyRow(CreateLabel("Specular Occlusion (main enable)", indentLevel)), (row) =>
            //{
            //    row.Add(new Toggle(), (toggle) =>
            //    {
            //        toggle.value = m_Node.specularOcclusion.isOn;
            //        toggle.OnToggleChanged(ChangeSpecularOcclusion);
            //    });
            //});

            // SpecularOcclusion from SSAO
            if (m_Node.devMode.isOn)
            {
                // Only in dev mode do we show controls for SO fed from SSAO: otherwise, we keep the default which is DirectFromAO
                ps.Add(new PropertyRow(CreateLabel("Specular Occlusion (from SSAO)", indentLevel)), (row) =>
                {
                    row.Add(new EnumField(StackLitMasterNode.SpecularOcclusionBaseMode.DirectFromAO), (field) =>
                    {
                        field.value = m_Node.screenSpaceSpecularOcclusionBaseMode;
                        field.RegisterValueChangedCallback(ChangeScreenSpaceSpecularOcclusionBaseMode);
                    });

                });
                if (StackLitMasterNode.SpecularOcclusionModeUsesVisibilityCone(m_Node.screenSpaceSpecularOcclusionBaseMode))
                {
                    ++indentLevel;
                    ps.Add(new PropertyRow(CreateLabel("Specular Occlusion (SS) AO Cone Weight", indentLevel)), (row) =>
                    {
                        row.Add(new EnumField(StackLitMasterNode.SpecularOcclusionAOConeSize.CosWeightedAO), (field) =>
                        {
                            field.value = m_Node.screenSpaceSpecularOcclusionAOConeSize;
                            field.RegisterValueChangedCallback(ChangeScreenSpaceSpecularOcclusionAOConeSize);
                        });
                    });
                    ps.Add(new PropertyRow(CreateLabel("Specular Occlusion (SS) AO Cone Dir", indentLevel)), (row) =>
                    {
                        row.Add(new EnumField(StackLitMasterNode.SpecularOcclusionAOConeDir.ShadingNormal), (field) =>
                        {
                            field.value = m_Node.screenSpaceSpecularOcclusionAOConeDir;
                            field.RegisterValueChangedCallback(ChangeScreenSpaceSpecularOcclusionAOConeDir);
                        });
                    });
                    --indentLevel;
                }
            }

            // SpecularOcclusion from input AO (baked or data-based SO)
            {
                ps.Add(new PropertyRow(CreateLabel("Specular Occlusion (from input AO)", indentLevel)), (row) =>
                {
                    if (m_Node.devMode.isOn)
                    {
                        row.Add(new EnumField(StackLitMasterNode.SpecularOcclusionBaseMode.DirectFromAO), (field) =>
                        {
                            field.value = m_Node.dataBasedSpecularOcclusionBaseMode;
                            field.RegisterValueChangedCallback(ChangeDataBasedSpecularOcclusionBaseMode);
                        });
                    }
                    else
                    {
                        row.Add(new EnumField(StackLitMasterNode.SpecularOcclusionBaseModeSimple.DirectFromAO), (field) =>
                        {
                            // In non-dev mode, parse any enum value set to a method not shown in the simple UI as SPTD (highest quality) method:
                            StackLitMasterNode.SpecularOcclusionBaseModeSimple simpleUIEnumValue =
                                Enum.TryParse(m_Node.dataBasedSpecularOcclusionBaseMode.ToString(), out StackLitMasterNode.SpecularOcclusionBaseModeSimple parsedValue) ?
                                    parsedValue : StackLitMasterNode.SpecularOcclusionBaseModeSimple.SPTDIntegrationOfBentAO;
                            field.value = simpleUIEnumValue;
                            field.RegisterValueChangedCallback(ChangeDataBasedSpecularOcclusionBaseModeSimpleUI);
                        });
                    }
                });
                if (StackLitMasterNode.SpecularOcclusionModeUsesVisibilityCone(m_Node.dataBasedSpecularOcclusionBaseMode))
                {
                    ++indentLevel;
                    ps.Add(new PropertyRow(CreateLabel("Specular Occlusion AO Cone Weight", indentLevel)), (row) =>
                    {
                        row.Add(new EnumField(StackLitMasterNode.SpecularOcclusionAOConeSize.CosWeightedBentCorrectAO), (field) =>
                        {
                            field.value = m_Node.dataBasedSpecularOcclusionAOConeSize;
                            field.RegisterValueChangedCallback(ChangeDataBasedSpecularOcclusionAOConeSize);
                        });
                    });
                    --indentLevel;
                }
            }

            if (m_Node.SpecularOcclusionUsesBentNormal())
            {
                if (m_Node.devMode.isOn)
                {
                    ps.Add(new PropertyRow(CreateLabel("Specular Occlusion Bent Cone Fixup", indentLevel)), (row) =>
                    {
                        row.Add(new EnumField(StackLitMasterNode.SpecularOcclusionConeFixupMethod.Off), (field) =>
                        {
                            field.value = m_Node.specularOcclusionConeFixupMethod;
                            field.RegisterValueChangedCallback(ChangeSpecularOcclusionConeFixupMethod);
                        });
                    });
                }
                else
                {
                    // Just show a simple toggle when not in dev mode
                    ps.Add(new PropertyRow(CreateLabel("Specular Occlusion Bent Cone Fixup", indentLevel)), (row) =>
                    {
                        row.Add(new Toggle(), (toggle) =>
                        {
                            toggle.value = m_Node.specularOcclusionConeFixupMethod != StackLitMasterNode.SpecularOcclusionConeFixupMethod.Off;
                            toggle.OnToggleChanged(ChangeSpecularOcclusionConeFixupMethodSimpleUI);
                        });
                    });
                }
            }

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

            ps.Add(new PropertyRow(CreateLabel("Advanced Options", indentLevel)), (row) => {} );
            ++indentLevel;

            ps.Add(new PropertyRow(CreateLabel("Anisotropy For Area Lights", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (toggle) =>
                {
                    toggle.value = m_Node.anisotropyForAreaLights.isOn;
                    toggle.OnToggleChanged(ChangeAnisotropyForAreaLights);
                });
            });

            // Per Punctual/Directional Lights
            {
                ps.Add(new PropertyRow(CreateLabel("Per Punctual/Directional Lights:", indentLevel)), (row) => { });
                ++indentLevel;

                if (m_Node.coat.isOn)
                {
                    ps.Add(new PropertyRow(CreateLabel("Base Layer Uses Refracted Angles", indentLevel)), (row) =>
                    {
                        row.Add(new Toggle(), (toggle) =>
                        {
                            toggle.value = m_Node.shadeBaseUsingRefractedAngles.isOn;
                            toggle.OnToggleChanged(ChangeShadeBaseUsingRefractedAngles);
                        });
                    });
                }
                if (m_Node.coat.isOn || m_Node.iridescence.isOn)
                {
                    ps.Add(new PropertyRow(CreateLabel("Recompute Stack & Iridescence", indentLevel)), (row) =>
                    {
                        row.Add(new Toggle(), (toggle) =>
                        {
                            toggle.value = m_Node.recomputeStackPerLight.isOn;
                            toggle.OnToggleChanged(ChangeRecomputeStackPerLight);
                        });
                    });
                }
                ps.Add(new PropertyRow(CreateLabel("Honor Per Light Max Smoothness", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.honorPerLightMinRoughness.isOn;
                        toggle.OnToggleChanged(ChangeHonorPerLightMinRoughness);
                    });
                });

                --indentLevel;
            } // Per Punctual/Directional Lights

            // Uncomment to show the dev mode UI:
            //
            //ps.Add(new PropertyRow(CreateLabel("Enable Dev Mode", indentLevel)), (row) =>
            //{
            //    row.Add(new Toggle(), (toggle) =>
            //    {
            //        toggle.value = m_Node.devMode.isOn;
            //        toggle.OnToggleChanged(ChangeDevMode);
            //    });
            //});

            if (m_Node.devMode.isOn)
            {
                ps.Add(new PropertyRow(CreateLabel("Show And Enable StackLit Debugs", indentLevel)), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_Node.debug.isOn;
                        toggle.OnToggleChanged(ChangeDebug);
                    });
                });
            }

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

            --indentLevel; //...Advanced options

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

        void ChangeSpaceOfNormalDropOffMode(ChangeEvent<Enum> evt)
        {
              if (Equals(m_Node.normalDropOffSpace, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Normal Space Drop-Off Mode Change");
            m_Node.normalDropOffSpace = (NormalDropOffSpace)evt.newValue;
        }

        void ChangeBaseParametrization(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.baseParametrization, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Base Parametrization Change");
            m_Node.baseParametrization = (StackLit.BaseParametrization)evt.newValue;
        }

        void ChangeDualSpecularLobeParametrization(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.dualSpecularLobeParametrization, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("Dual Specular Lobe Parametrization Change");
            m_Node.dualSpecularLobeParametrization = (StackLit.DualSpecularLobeParametrization)evt.newValue;
        }

        void ChangeBlendMode(ChangeEvent<Enum> evt)
        {
            // Make sure the mapping is correct by handling each case.
            AlphaMode alphaMode = GetAlphaMode((StackLitMasterNode.AlphaModeLit)evt.newValue);

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

        void ChangeDistortionDepthTest(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Distortion Depth Test Change");
            ToggleData td = m_Node.distortionDepthTest;
            td.isOn = evt.newValue;
            m_Node.distortionDepthTest = td;
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

        void ChangeAlphaTest(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Alpha Test Change");
            ToggleData td = m_Node.alphaTest;
            td.isOn = evt.newValue;
            m_Node.alphaTest = td;
        }

        void ChangeReceiveDecals(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Receive Decals Change");
            ToggleData td = m_Node.receiveDecals;
            td.isOn = evt.newValue;
            m_Node.receiveDecals = td;
        }

        void ChangeReceiveSSR(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Receive SSR Change");
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

        void ChangeGeometricSpecularAA(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Specular AA Change");
            ToggleData td = m_Node.geometricSpecularAA;
            td.isOn = evt.newValue;
            m_Node.geometricSpecularAA = td;
        }

        void ChangeEnergyConservingSpecular(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Energy Conserving Specular Change");
            ToggleData td = m_Node.energyConservingSpecular;
            td.isOn = evt.newValue;
            m_Node.energyConservingSpecular = td;
        }

        void ChangeAnisotropy(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Anisotropy Change");
            ToggleData td = m_Node.anisotropy;
            td.isOn = evt.newValue;
            m_Node.anisotropy = td;
        }

        void ChangeCoat(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Coat Change");
            ToggleData td = m_Node.coat;
            td.isOn = evt.newValue;
            m_Node.coat = td;
        }

        void ChangeCoatNormal(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Coat Normal Change");
            ToggleData td = m_Node.coatNormal;
            td.isOn = evt.newValue;
            m_Node.coatNormal = td;
        }

        void ChangeDualSpecularLobe(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("DualSpecularLobe Change");
            ToggleData td = m_Node.dualSpecularLobe;
            td.isOn = evt.newValue;
            m_Node.dualSpecularLobe = td;
        }

        void ChangeCapHazinessWrtMetallic(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("CapHazinessWrtMetallic Change");
            ToggleData td = m_Node.capHazinessWrtMetallic;
            td.isOn = evt.newValue;
            m_Node.capHazinessWrtMetallic = td;
        }

        void ChangeIridescence(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Iridescence Change");
            ToggleData td = m_Node.iridescence;
            td.isOn = evt.newValue;
            m_Node.iridescence = td;
        }

        void ChangeSubsurfaceScattering(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("SubsurfaceScattering Change");
            ToggleData td = m_Node.subsurfaceScattering;
            td.isOn = evt.newValue;
            m_Node.subsurfaceScattering = td;
        }

        void ChangeTransmission(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Transmission Change");
            ToggleData td = m_Node.transmission;
            td.isOn = evt.newValue;
            m_Node.transmission = td;
        }

        //void ChangeSpecularOcclusion(ChangeEvent<bool> evt)
        //{
        //    m_Node.owner.owner.RegisterCompleteObjectUndo("SpecularOcclusion Change");
        //    ToggleData td = m_Node.specularOcclusion;
        //    td.isOn = evt.newValue;
        //    m_Node.specularOcclusion = td;
        //}

        void ChangeScreenSpaceSpecularOcclusionBaseMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.screenSpaceSpecularOcclusionBaseMode, evt.newValue))
                return;

            if (Equals(evt.newValue, StackLitMasterNode.SpecularOcclusionBaseMode.Custom))
            {
                Debug.LogWarning("Custom input not supported for SSAO based specular occlusion.");
                // Make sure the UI field doesn't switch and stays in synch with the master node property:
                if (evt.currentTarget is EnumField enumField)
                {
                    enumField.value = m_Node.screenSpaceSpecularOcclusionBaseMode;
                }
                return;
            }

            m_Node.owner.owner.RegisterCompleteObjectUndo("ScreenSpaceSpecularOcclusionBaseMode Change");
            m_Node.screenSpaceSpecularOcclusionBaseMode = (StackLitMasterNode.SpecularOcclusionBaseMode)evt.newValue;
        }

        void ChangeScreenSpaceSpecularOcclusionAOConeSize(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.screenSpaceSpecularOcclusionAOConeSize, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("ScreenSpaceSpecularOcclusionAOConeSize Change");
            m_Node.screenSpaceSpecularOcclusionAOConeSize = (StackLitMasterNode.SpecularOcclusionAOConeSize)evt.newValue;
        }

        void ChangeScreenSpaceSpecularOcclusionAOConeDir(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.screenSpaceSpecularOcclusionAOConeDir, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("ScreenSpaceSpecularOcclusionAOConeDir Change");
            m_Node.screenSpaceSpecularOcclusionAOConeDir = (StackLitMasterNode.SpecularOcclusionAOConeDir)evt.newValue;
        }

        void ChangeDataBasedSpecularOcclusionBaseMode(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.dataBasedSpecularOcclusionBaseMode, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("DataBasedSpecularOcclusionBaseMode Change");
            m_Node.dataBasedSpecularOcclusionBaseMode = (StackLitMasterNode.SpecularOcclusionBaseMode)evt.newValue;
        }

        void ChangeDataBasedSpecularOcclusionBaseModeSimpleUI(ChangeEvent<Enum> evt)
        {
            // StackLitMasterNode.SpecularOcclusionBaseModeSimple should always be a subset of StackLitMasterNode.SpecularOcclusionBaseMode:
            if (Equals(m_Node.dataBasedSpecularOcclusionBaseMode, (StackLitMasterNode.SpecularOcclusionBaseMode) evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("DataBasedSpecularOcclusionBaseMode (simple UI) Change");
            m_Node.dataBasedSpecularOcclusionBaseMode = (StackLitMasterNode.SpecularOcclusionBaseMode)evt.newValue;
        }

        void ChangeDataBasedSpecularOcclusionAOConeSize(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.dataBasedSpecularOcclusionAOConeSize, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("DataBasedSpecularOcclusionAOConeSize Change");
            m_Node.dataBasedSpecularOcclusionAOConeSize = (StackLitMasterNode.SpecularOcclusionAOConeSize)evt.newValue;
        }

        void ChangeSpecularOcclusionConeFixupMethod(ChangeEvent<Enum> evt)
        {
            if (Equals(m_Node.specularOcclusionConeFixupMethod, evt.newValue))
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("SpecularOcclusionConeFixupMethod Change");
            m_Node.specularOcclusionConeFixupMethod = (StackLitMasterNode.SpecularOcclusionConeFixupMethod)evt.newValue;
        }

        void ChangeSpecularOcclusionConeFixupMethodSimpleUI(ChangeEvent<bool> evt)
        {
            if ( (evt.newValue == false && Equals(m_Node.specularOcclusionConeFixupMethod, StackLitMasterNode.SpecularOcclusionConeFixupMethod.Off))
                || (evt.newValue == true && Equals(m_Node.specularOcclusionConeFixupMethod, StackLitMasterNode.SpecularOcclusionConeFixupMethod.BoostAndTilt)) )
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo("SpecularOcclusionConeFixupMethod Change");

            m_Node.specularOcclusionConeFixupMethod = evt.newValue ? StackLitMasterNode.SpecularOcclusionConeFixupMethod.BoostAndTilt
                                                                     : StackLitMasterNode.SpecularOcclusionConeFixupMethod.Off;
        }

        void ChangeAnisotropyForAreaLights(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("AnisotropyForAreaLights Change");
            ToggleData td = m_Node.anisotropyForAreaLights;
            td.isOn = evt.newValue;
            m_Node.anisotropyForAreaLights = td;
        }

        void ChangeoverrideBakedGI(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("overrideBakedGI Change");
            ToggleData td = m_Node.overrideBakedGI;
            td.isOn = evt.newValue;
            m_Node.overrideBakedGI = td;
        }

        void ChangeRecomputeStackPerLight(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("RecomputeStackPerLight Change");
            ToggleData td = m_Node.recomputeStackPerLight;
            td.isOn = evt.newValue;
            m_Node.recomputeStackPerLight = td;
        }

        void ChangeHonorPerLightMinRoughness(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("HonorPerLightMinRoughness Change");
            ToggleData td = m_Node.honorPerLightMinRoughness;
            td.isOn = evt.newValue;
            m_Node.honorPerLightMinRoughness = td;
        }

        void ChangeShadeBaseUsingRefractedAngles(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("ShadeBaseUsingRefractedAngles Change");
            ToggleData td = m_Node.shadeBaseUsingRefractedAngles;
            td.isOn = evt.newValue;
            m_Node.shadeBaseUsingRefractedAngles = td;
        }

        void ChangeDevMode(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("StackLit DevMode Change");
            ToggleData td = m_Node.devMode;
            td.isOn = evt.newValue;
            m_Node.devMode = td;
        }

        void ChangeDebug(ChangeEvent<bool> evt)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("StackLit Debug Change");
            ToggleData td = m_Node.debug;
            td.isOn = evt.newValue;
            m_Node.debug = td;
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

        public AlphaMode GetAlphaMode(StackLitMasterNode.AlphaModeLit alphaModeLit)
        {
            switch (alphaModeLit)
            {
                case StackLitMasterNode.AlphaModeLit.Alpha:
                    return AlphaMode.Alpha;
                case StackLitMasterNode.AlphaModeLit.Premultiply:
                    return AlphaMode.Premultiply;
                case StackLitMasterNode.AlphaModeLit.Additive:
                    return AlphaMode.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaModeLit);
                        return AlphaMode.Alpha;
                    }

            }
        }

        public StackLitMasterNode.AlphaModeLit GetAlphaModeLit(AlphaMode alphaMode)
        {
            switch (alphaMode)
            {
                case AlphaMode.Alpha:
                    return StackLitMasterNode.AlphaModeLit.Alpha;
                case AlphaMode.Premultiply:
                    return StackLitMasterNode.AlphaModeLit.Premultiply;
                case AlphaMode.Additive:
                    return StackLitMasterNode.AlphaModeLit.Additive;
                default:
                    {
                        Debug.LogWarning("Not supported: " + alphaMode);
                        return StackLitMasterNode.AlphaModeLit.Alpha;
                    }
            }
        }
    }
}
