using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class HDMeshTargetSettingsView : VisualElement
    {
        HDMeshTarget target;
        Action onChange;
        int indentLevel;

        public HDMeshTargetSettingsView(HDMeshTarget target, Action onChange)
        {
            // Set data
            name = "hdMeshSettings";
            this.target = target;
            this.onChange = onChange;
            indentLevel = 0;

            // Main
            DoSurfaceType(0);
            RenderingPass(1);

            if(target.surfaceType == SurfaceType.Transparent)
            {
                // Render State
                BlendingMode(1);
                DepthTest(1);
                DepthWrite(1);
                if(target.doubleSidedMode != DoubleSidedMode.Disabled)
                {
                    CullMode(1);
                }
                SortingPriority(1);

                // Misc
                ReceiveFog(1);

                // Distortion
                DoDistortion(1);
                if(target.distortion)
                {
                    DistortionBlendMode(2);
                    DistortionOnly(2);
                    DistortionDepthTest(2);
                }
            }

            // Misc
            DoubleSided(0);
            AlphaClipping(0);
            AddPrecomputedVelocity(0);
            ShadowMatte(0);
        }

#region Properties
        void DoSurfaceType(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Surface Type", indentLevel)), (row) =>
            {
                row.Add(new EnumField(SurfaceType.Opaque), (field) =>
                {
                    field.value = target.surfaceType;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.surfaceType, evt.newValue))
                            return;

                        target.surfaceType = (SurfaceType)evt.newValue;
                        UpdateRenderingPassValue(target.renderingPass);
                        onChange();
                    });
                });
            });
        }

        void RenderingPass(int indentLevel)
        {
            switch (target.surfaceType)
            {
                case SurfaceType.Opaque:
                    this.Add(new PropertyRow(CreateLabel("Rendering Pass", indentLevel)), (row) =>
                    {
                        var valueList = HDSubShaderUtilities.GetRenderingPassList(true, true);
                        row.Add(new PopupField<HDRenderQueue.RenderQueueType>(valueList, HDRenderQueue.RenderQueueType.Opaque, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName), (field) =>
                        {
                            var value = HDRenderQueue.GetOpaqueEquivalent(target.renderingPass);
                            field.value = value;
                            field.RegisterValueChangedCallback(evt => {
                                if(ChangeRenderingPass(value))
                                {
                                    onChange();
                                }
                            });
                        });
                    });
                    break;
                case SurfaceType.Transparent:
                    this.Add(new PropertyRow(CreateLabel("Rendering Pass", indentLevel)), (row) =>
                    {
                        Enum defaultValue;
                        switch (target.renderingPass) // Migration
                        {
                            default: //when deserializing without issue, we still need to init the default to something even if not used.
                            case HDRenderQueue.RenderQueueType.Transparent:
                                defaultValue = HDRenderQueue.TransparentRenderQueue.Default;
                                break;
                            case HDRenderQueue.RenderQueueType.PreRefraction:
                                defaultValue = HDRenderQueue.TransparentRenderQueue.BeforeRefraction;
                                break;
                        }

                        var valueList = HDSubShaderUtilities.GetRenderingPassList(false, true);
                        row.Add(new PopupField<HDRenderQueue.RenderQueueType>(valueList, HDRenderQueue.RenderQueueType.Transparent, HDSubShaderUtilities.RenderQueueName, HDSubShaderUtilities.RenderQueueName), (field) =>
                        {
                            var value = HDRenderQueue.GetTransparentEquivalent(target.renderingPass);
                            field.value = value;
                            field.RegisterValueChangedCallback(evt => {
                                if(ChangeRenderingPass(value))
                                {
                                    onChange();
                                }
                            });
                        });
                    });
                    break;
                default:
                    throw new ArgumentException("Unknown SurfaceType");
            }
        }

        void BlendingMode(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Blending Mode", indentLevel)), (row) =>
            {
                row.Add(new EnumField(AlphaMode.Alpha), (field) =>
                {
                    field.value = target.alphaMode;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.alphaMode, evt.newValue))
                            return;

                        target.alphaMode = (AlphaMode)evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void SortingPriority(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Sorting Priority", indentLevel)), (row) =>
            {
                row.Add(new IntegerField(), (field) =>
                {
                    field.value = target.sortPriority;
                    field.RegisterValueChangedCallback(evt => {
                        var value = HDRenderQueue.ClampsTransparentRangePriority(evt.newValue);
                        if (Equals(target.sortPriority, value))
                            return;

                        target.sortPriority = value;
                        onChange();
                    });
                });
            });
        }

        void ReceiveFog(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Receive Fog", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (field) =>
                {
                    field.value = target.transparencyFog;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.transparencyFog, evt.newValue))
                            return;

                        target.transparencyFog = evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void DoDistortion(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Distortion", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (field) =>
                {
                    field.value = target.distortion;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.distortion, evt.newValue))
                            return;

                        target.distortion = evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void DistortionBlendMode(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Distortion Blend Mode", indentLevel)), (row) =>
            {
                row.Add(new EnumField(DistortionMode.Add), (field) =>
                {
                    field.value = target.distortionMode;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.distortionMode, evt.newValue))
                            return;

                        target.distortionMode = (DistortionMode)evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void DistortionOnly(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Distortion Only", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (field) =>
                {
                    field.value = target.distortionOnly;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.distortionOnly, evt.newValue))
                            return;

                        target.distortionOnly = evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void DistortionDepthTest(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Distortion Depth Test", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (field) =>
                {
                    field.value = target.distortionDepthTest;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.distortionDepthTest, evt.newValue))
                            return;

                        target.distortionDepthTest = evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void DepthTest(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Depth Test", indentLevel)), (row) =>
            {
                row.Add(new EnumField(target.zTest), (field) =>
                {
                    field.value = target.zTest;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.zTest, evt.newValue))
                            return;

                        target.zTest = (CompareFunction)evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void DepthWrite(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Depth Write", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (field) =>
                {
                    field.value = target.zWrite;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.zWrite, evt.newValue))
                            return;

                        target.zWrite = evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void CullMode(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Cull Mode", indentLevel)), (row) =>
            {
                row.Add(new EnumField(target.transparentCullMode), (field) =>
                {
                    field.value = target.transparentCullMode;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.transparentCullMode, evt.newValue))
                            return;

                        target.transparentCullMode = (TransparentCullMode)evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void DoubleSided(int indentLevel)
        {
            this.Add(new PropertyRow(new Label("Double-Sided Mode")), (row) =>
            {
                row.Add(new EnumField(target.doubleSidedMode), (field) =>
                {
                    field.value = target.doubleSidedMode;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.doubleSidedMode, evt.newValue))
                            return;

                        target.doubleSidedMode = (DoubleSidedMode)evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void AlphaClipping(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Alpha Clipping", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (field) =>
                {
                    field.value = target.alphaTest;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.alphaTest, evt.newValue))
                            return;

                        target.alphaTest = evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void AddPrecomputedVelocity(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Add Precomputed Velocity", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (field) =>
                {
                    field.value = target.addPrecomputedVelocity;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.addPrecomputedVelocity, evt.newValue))
                            return;

                        target.addPrecomputedVelocity = evt.newValue;
                        onChange();
                    });
                });
            });
        }

        void ShadowMatte(int indentLevel)
        {
            this.Add(new PropertyRow(CreateLabel("Shadow Matte", indentLevel)), (row) =>
            {
                row.Add(new Toggle(), (field) =>
                {
                    field.value = target.enableShadowMatte;
                    field.RegisterValueChangedCallback(evt => {
                        if (Equals(target.enableShadowMatte, evt.newValue))
                            return;

                        target.enableShadowMatte = evt.newValue;
                        onChange();
                    });
                });
            });
        }
#endregion

#region Helpers
        Label CreateLabel(string text, int indentLevel)
        {
            string label = "";
            for (var i = 0; i < indentLevel; i++)
            {
                label += "    ";
            }
            return new Label(label + text);
        }

        bool ChangeRenderingPass(HDRenderQueue.RenderQueueType value)
        {
            switch (value)
            {
                case HDRenderQueue.RenderQueueType.Overlay:
                case HDRenderQueue.RenderQueueType.Unknown:
                case HDRenderQueue.RenderQueueType.Background:
                    throw new ArgumentException("Unexpected kind of RenderQueue, was " + value);
                default:
                    break;
            };
            return UpdateRenderingPassValue(value);
        }

        bool UpdateRenderingPassValue(HDRenderQueue.RenderQueueType value)
        {
            switch (target.surfaceType)
            {
                case SurfaceType.Opaque:
                    value = HDRenderQueue.GetOpaqueEquivalent(value);
                    break;
                case SurfaceType.Transparent:
                    value = HDRenderQueue.GetTransparentEquivalent(value);
                    break;
                default:
                    throw new ArgumentException("Unknown SurfaceType");
            }

            if (Equals(target.renderingPass, value))
                return false;

            target.renderingPass = value;
            return true;
        }
#endregion
    }
}