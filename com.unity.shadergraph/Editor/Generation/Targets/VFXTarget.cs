using System;
using System.Collections.Generic;
using Data.Interfaces;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.Graphing.Util;

namespace UnityEditor.ShaderGraph
{
    sealed class VFXTarget : Target
    {
        [SerializeField]
        bool m_Lit;

        [SerializeField]
        bool m_AlphaTest = false;

        public VFXTarget()
        {
            displayName = "Visual Effect";
        }

        [Inspectable("Lit", false)]
        public bool lit
        {
            get => m_Lit;
            set => m_Lit = value;
        }


        [Inspectable("Alpha Test", false)]
        public bool alphaTest
        {
            get => m_AlphaTest;
            set => m_AlphaTest = value;
        }

        public override List<string> subTargetNames { get; }
        public override int activeSubTargetIndex { get; set; }

        public override SubTarget activeSubTarget { get; set; }

        public override bool IsActive() => true;

        public override void Setup(ref TargetSetupContext context)
        {
        }

        public override void GetFields(ref TargetFieldContext context)
        {
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            context.AddBlock(BlockFields.SurfaceDescription.BaseColor);
            context.AddBlock(BlockFields.SurfaceDescription.Alpha);
            context.AddBlock(BlockFields.SurfaceDescription.Metallic,           lit);
            context.AddBlock(BlockFields.SurfaceDescription.Smoothness,         lit);
            context.AddBlock(BlockFields.SurfaceDescription.NormalTS,           lit);
            context.AddBlock(BlockFields.SurfaceDescription.Emission,           lit);
            context.AddBlock(BlockFields.SurfaceDescription.AlphaClipThreshold, alphaTest);
        }

        public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange)
        {
            context.AddProperty("Lit", new Toggle() { value = m_Lit }, (evt) =>
            {
                if (Equals(m_Lit, evt.newValue))
                    return;

                m_Lit = evt.newValue;
                onChange();
            });

            context.AddProperty("Alpha Test", new Toggle() { value = m_AlphaTest }, (evt) =>
            {
                if (Equals(m_AlphaTest, evt.newValue))
                    return;

                m_AlphaTest = evt.newValue;
                onChange();
            });
        }
    }
}
