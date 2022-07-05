using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    /// <summary>
    /// Editor script for the BakedLit material inspector.
    /// </summary>
    public static class BakedLitGUI
    {
        /// <summary>
        /// Container for the properties used in the <c>BakedLitGUI</c> editor script.
        /// </summary>
        public struct BakedLitProperties
        {
            // Surface Input Props

            /// <summary>
            /// The MaterialProperty for normal map.
            /// </summary>
            public MaterialProperty bumpMapProp;

            /// <summary>
            /// Constructor for the <c>BakedLitProperties</c> container struct.
            /// </summary>
            /// <param name="properties"></param>
            public BakedLitProperties(MaterialProperty[] properties)
            {
                // Surface Input Props
                bumpMapProp = BaseShaderGUI.FindProperty("_BumpMap", properties, false);
            }
        }

        /// <summary>
        /// Draws the surface inputs GUI.
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="materialEditor"></param>
        public static void Inputs(BakedLitProperties properties, MaterialEditor materialEditor)
        {
            BaseShaderGUI.DrawNormalArea(materialEditor, properties.bumpMapProp);
        }
    }
}
