using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class TessellationOptionsUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string header = "Tessellation Options";

            public static string tessellationModeStr = "Tessellation Mode";
            public static readonly string[] tessellationModeNames = Enum.GetNames(typeof(TessellationMode));

            public static GUIContent tessellationText = new GUIContent("Tessellation Options", "Tessellation options");
            public static GUIContent tessellationFactorText = new GUIContent("Tessellation Factor", "Controls the strength of the tessellation effect. Higher values result in more tessellation. Maximum tessellation factor is 15 on the Xbox One and PS4");
            public static GUIContent tessellationFactorMinDistanceText = new GUIContent("Start Fade Distance", "Sets the distance from the camera at which tessellation begins to fade out.");
            public static GUIContent tessellationFactorMaxDistanceText = new GUIContent("End Fade Distance", "Sets the maximum distance from the Camera where HDRP tessellates triangle.");
            public static GUIContent tessellationFactorTriangleSizeText = new GUIContent("Triangle Size", "Sets the desired screen space size of triangles (in pixels). Smaller values result in smaller triangle.");
            public static GUIContent tessellationShapeFactorText = new GUIContent("Shape Factor", "Controls the strength of Phong tessellation shape (lerp factor).");
            public static GUIContent tessellationBackFaceCullEpsilonText = new GUIContent("Triangle Culling Epsilon", "Controls triangle culling. A value of -1.0 disables back face culling for tessellation, higher values produce more aggressive culling and better performance.");
        }

        // tessellation params
        MaterialProperty tessellationMode = null;
        const string kTessellationMode = "_TessellationMode";
        MaterialProperty tessellationFactor = null;
        const string kTessellationFactor = "_TessellationFactor";
        MaterialProperty tessellationFactorMinDistance = null;
        const string kTessellationFactorMinDistance = "_TessellationFactorMinDistance";
        MaterialProperty tessellationFactorMaxDistance = null;
        const string kTessellationFactorMaxDistance = "_TessellationFactorMaxDistance";
        MaterialProperty tessellationFactorTriangleSize = null;
        const string kTessellationFactorTriangleSize = "_TessellationFactorTriangleSize";
        MaterialProperty tessellationShapeFactor = null;
        const string kTessellationShapeFactor = "_TessellationShapeFactor";
        MaterialProperty tessellationBackFaceCullEpsilon = null;
        const string kTessellationBackFaceCullEpsilon = "_TessellationBackFaceCullEpsilon";
        MaterialProperty doubleSidedEnable = null;

        Expandable m_ExpandableBit;

        public TessellationOptionsUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        public override void LoadMaterialProperties()
        {
            doubleSidedEnable = FindProperty(kDoubleSidedEnable, false);

            // tessellation specific, silent if not found
            tessellationMode = FindProperty(kTessellationMode);
            tessellationFactor = FindProperty(kTessellationFactor);
            tessellationFactorMinDistance = FindProperty(kTessellationFactorMinDistance);
            tessellationFactorMaxDistance = FindProperty(kTessellationFactorMaxDistance);
            tessellationFactorTriangleSize = FindProperty(kTessellationFactorTriangleSize);
            tessellationShapeFactor = FindProperty(kTessellationShapeFactor);
            tessellationBackFaceCullEpsilon = FindProperty(kTessellationBackFaceCullEpsilon);
        }

        public override void OnGUI()
        {
            // If we don't have tesselation
            if (tessellationMode == null)
                return ;

            using (var header = new MaterialHeaderScope(Styles.header, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                {
                    DrawTesselationGUI();
                }
            }
        }

        void DrawTesselationGUI()
        {
            TessellationModePopup();
            materialEditor.ShaderProperty(tessellationFactor, Styles.tessellationFactorText);
            DrawDelayedFloatProperty(tessellationFactorMinDistance, Styles.tessellationFactorMinDistanceText);
            DrawDelayedFloatProperty(tessellationFactorMaxDistance, Styles.tessellationFactorMaxDistanceText);
            // clamp min distance to be below max distance
            tessellationFactorMinDistance.floatValue = Math.Min(tessellationFactorMaxDistance.floatValue, tessellationFactorMinDistance.floatValue);
            materialEditor.ShaderProperty(tessellationFactorTriangleSize, Styles.tessellationFactorTriangleSizeText);
            if ((TessellationMode)tessellationMode.floatValue == TessellationMode.Phong)
            {
                materialEditor.ShaderProperty(tessellationShapeFactor, Styles.tessellationShapeFactorText);
            }
            if (doubleSidedEnable.floatValue == 0.0)
            {
                materialEditor.ShaderProperty(tessellationBackFaceCullEpsilon, Styles.tessellationBackFaceCullEpsilonText);
            }
        }

        void TessellationModePopup()
        {
            EditorGUI.showMixedValue = tessellationMode.hasMixedValue;
            var mode = (TessellationMode)tessellationMode.floatValue;

            EditorGUI.BeginChangeCheck();
            mode = (TessellationMode)EditorGUILayout.Popup(Styles.tessellationModeStr, (int)mode, Styles.tessellationModeNames);
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo("Tessellation Mode");
                tessellationMode.floatValue = (float)mode;
            }

            EditorGUI.showMixedValue = false;
        }

        private void DrawDelayedFloatProperty(MaterialProperty prop, GUIContent content)
        {
            Rect position = EditorGUILayout.GetControlRect();
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            float newValue = EditorGUI.DelayedFloatField(position, content, prop.floatValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.floatValue = newValue;
        }
    }
}
