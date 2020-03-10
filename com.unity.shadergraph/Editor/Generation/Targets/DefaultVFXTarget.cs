using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    class DefaultVFXTarget : ITargetImplementation
    {
        public Type targetType => typeof(VFXTarget);
        public string displayName => "Default";
        public string passTemplatePath => null;
        public string sharedTemplateDirectory => null;

        public Type dataType => typeof(DefaultVFXTargetData);
        public TargetImplementationData data { get; set; }
        public DefaultVFXTargetData vfxData => (DefaultVFXTargetData)data;

        public bool IsPipelineCompatible(RenderPipelineAsset currentPipeline)
        {
            return (currentPipeline != null);
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
        }

        public List<BlockFieldDescriptor> GetSupportedBlocks()
        {
            var supportedBlocks = new List<BlockFieldDescriptor>();

            // Always supported Blocks
            supportedBlocks.Add(BlockFields.SurfaceDescription.BaseColor);
            supportedBlocks.Add(BlockFields.SurfaceDescription.Alpha);

            // Alpha Blocks
            if(vfxData.alphaTest)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.ClipThreshold);
            }

            // Lit Blocks
            if(vfxData.lit)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.Metallic);
                supportedBlocks.Add(BlockFields.SurfaceDescription.Smoothness);
                supportedBlocks.Add(BlockFields.SurfaceDescription.Normal);
                supportedBlocks.Add(BlockFields.SurfaceDescription.Emission);
            }

            return supportedBlocks;
        }

        public ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks)
        {
            return null;
        }

        public void GetInspectorContent(PropertySheet propertySheet, Action onChange)
        {
            propertySheet.Add(new PropertyRow(new Label("Lit")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = vfxData.lit;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(vfxData.lit, evt.newValue))
                                return;
                            
                            vfxData.lit = evt.newValue;
                            onChange();
                        });
                    });
                });

            propertySheet.Add(new PropertyRow(new Label("Alpha Test")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = vfxData.alphaTest;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(vfxData.alphaTest, evt.newValue))
                                return;
                            
                            vfxData.alphaTest = evt.newValue;
                            onChange();
                        });
                    });
                });
        }
    }
}
