using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    class SpeedTreeLitOptionsUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string SpeedTreeHeader = "SpeedTree Options";

            //public static GUIContent versionText = new GUIContent("SpeedTree Asset Version", "Version of SpeedTree used to create the asset targeted");
            public static GUIContent typeText = new GUIContent("Geometry Type", "Which type of tree geometry component is the part being shaded");
            public static GUIContent windQualityText = new GUIContent("Wind Quality", "Detail level of the wind effect");
            public static GUIContent twoSidedText = new GUIContent("Two Sided", "Whether geometry should be two-sided or not");
            public static GUIContent cullingModeText = new GUIContent("Cull", "No culling, Front facing, or Back facing");
        }

        Expandable m_ExpandableBit;

        //MaterialProperty assetVersion = null;
        //const string kAssetVersion = "_SpeedTreeVersion";
        MaterialProperty geomType = null;
        const string kGeomType = "_SpeedTreeGeom";

        MaterialProperty windQuality = null;
        const string kWindQuality = "_WindQuality";
        MaterialProperty twoSidedEnable = null;
        const string kTwoSidedEnable = "_TwoSided";
        MaterialProperty cullMode = null;
        const string kCullMode = "_Cull";

        public SpeedTreeLitOptionsUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        public override void LoadMaterialProperties()
        {
            //assetVersion = FindProperty(kAssetVersion);
            geomType = FindProperty(kGeomType);
            windQuality = FindProperty(kWindQuality);
            twoSidedEnable = FindProperty(kTwoSidedEnable);
            cullMode = FindProperty(kCullMode);
        }

        void DrawSpeedTreeInputsGUI()
        {
            //EditorGUI.BeginChangeCheck();
            if (geomType != null)
                materialEditor.ShaderProperty(geomType, Styles.typeText);

            if (windQuality != null)
                materialEditor.ShaderProperty(windQuality, Styles.windQualityText);

            if (twoSidedEnable != null)
                materialEditor.ShaderProperty(twoSidedEnable, Styles.twoSidedText);

            if (cullMode != null)
                materialEditor.ShaderProperty(cullMode, Styles.cullingModeText);

            //if (EditorGUI.EndChangeCheck())
            //{

            //}
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.SpeedTreeHeader, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                    DrawSpeedTreeInputsGUI();
            }
        }
    }
}
