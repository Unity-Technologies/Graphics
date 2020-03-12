using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    class DefaultVFXTarget : ITargetImplementation
    {
#region Serialized Fields
        [SerializeField]
        bool m_Lit = false;

        [SerializeField]
        bool m_AlphaTest = false;
#endregion

#region Properties
        public Type targetType => typeof(VFXTarget);
        public string displayName => "Default";
        public string passTemplatePath => null;
        public string sharedTemplateDirectory => null;
#endregion

        public void SetupTarget(ref TargetSetupContext context)
        {
        }

        public void SetActiveBlocks(ref List<BlockFieldDescriptor> activeBlocks)
        {
            // Always supported Blocks
            activeBlocks.Add(BlockFields.SurfaceDescription.BaseColor);
            activeBlocks.Add(BlockFields.SurfaceDescription.Alpha);

            // Lit Blocks
            if(m_Lit)
            {
                activeBlocks.Add(BlockFields.SurfaceDescription.Metallic);
                activeBlocks.Add(BlockFields.SurfaceDescription.Smoothness);
                activeBlocks.Add(BlockFields.SurfaceDescription.Normal);
                activeBlocks.Add(BlockFields.SurfaceDescription.Emission);
            }

            // Alpha Blocks
            if(m_AlphaTest)
            {
                activeBlocks.Add(BlockFields.SurfaceDescription.ClipThreshold);
            }
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
                        toggle.value = m_Lit;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(m_Lit, evt.newValue))
                                return;
                            
                            m_Lit = evt.newValue;
                            onChange();
                        });
                    });
                });

            propertySheet.Add(new PropertyRow(new Label("Alpha Test")), (row) =>
                {
                    row.Add(new Toggle(), (toggle) =>
                    {
                        toggle.value = m_AlphaTest;
                        toggle.OnToggleChanged(evt => {
                            if (Equals(m_AlphaTest, evt.newValue))
                                return;
                            
                            m_AlphaTest = evt.newValue;
                            onChange();
                        });
                    });
                });
        }
    }
}
