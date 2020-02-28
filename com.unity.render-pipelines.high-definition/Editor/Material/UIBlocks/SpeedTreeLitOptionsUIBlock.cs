using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    class SpeedTreeLitOptionsUIBlock : MaterialUIBlock
    {
        public class Styles
        {
            public const string SpeedTreeHeader = "SpeedTree Options";

            public static GUIContent versionText = new GUIContent(HDSpeedTreeTarget.SpeedTreeVersion.displayName, "Major version of SpeedTree Creator for this asset.");
            public static GUIContent typeText = new GUIContent(HDSpeedTreeTarget.SpeedTree7GeomType.displayName, "Which type of tree geometry component is the part being shaded");
            public static GUIContent enableWindText = new GUIContent(HDSpeedTreeTarget.EnableWind.displayName, "Whether you want the wind effect to be on or not");
            public static GUIContent windQualityText = new GUIContent(HDSpeedTreeTarget.SpeedTree8WindQuality.displayName, "Detail level of the wind effect");
            public static GUIContent billboardText = new GUIContent("Billboard", "This surface is a billboard");
            public static GUIContent billboardFacingText = new GUIContent("Billboard Camera Facing", "Factor which affects billboard's impact on shadows");
        }

        Expandable m_ExpandableBit;

        public enum SpeedTreeVersionEnum
        {
            SpeedTreeVer7 = 0,
            SpeedTreeVer8,
        }

        public enum SpeedTree7Geom
        {
            Branch,
            BranchDetail,
            Frond,
            Leaf,
            Mesh,
        }

        public enum WindQualityEnum
        {
            None,
            Fastest,
            Fast,
            Better,
            Best,
            Palm,
        }

        MaterialProperty assetVersion = null;
        public const string kAssetVersion = "_SpeedTreeVersion";

        MaterialProperty geomType = null;
        public const string kGeomType = "_SpeedTreeGeom";

        MaterialProperty windEnable = null;
        public const string kWindEnable = "_WindEnabled";
        MaterialProperty windQuality = null;
        public const string kWindQuality = "_WindQuality";

        MaterialProperty isBillboard = null;
        public const string kIsBillboard = "_Billboard";
        MaterialProperty billboardFacesCam = null;
        public const string kBillboardFacing = "_BillboardFacing";

        public SpeedTreeLitOptionsUIBlock(Expandable expandableBit)
        {
            m_ExpandableBit = expandableBit;
        }

        public override void LoadMaterialProperties()
        {
            assetVersion = FindProperty(kAssetVersion);
            geomType = FindProperty(kGeomType);

            windEnable = FindProperty(kWindEnable);
            windQuality = FindProperty(kWindQuality);

            isBillboard = FindProperty(kIsBillboard);
            billboardFacesCam = FindProperty(kBillboardFacing);
        }

        void DrawSpeedTreeInputsGUI()
        {
            if (assetVersion != null)
                materialEditor.ShaderProperty(assetVersion, Styles.versionText);

            if (geomType != null)
                materialEditor.ShaderProperty(geomType, Styles.typeText);

            if (windEnable != null && windQuality != null)
            {
                materialEditor.ShaderProperty(windEnable, Styles.enableWindText);
                materialEditor.ShaderProperty(windQuality, Styles.windQualityText);
            }

            if (isBillboard != null)
            {
                materialEditor.ShaderProperty(isBillboard, Styles.billboardText);
                if (billboardFacesCam != null)
                {
                    materialEditor.ShaderProperty(billboardFacesCam, Styles.billboardFacingText);
                }
            }
        }

        public override void OnGUI()
        {
            using (var header = new MaterialHeaderScope(Styles.SpeedTreeHeader, (uint)m_ExpandableBit, materialEditor))
            {
                if (header.expanded)
                {
                    DrawSpeedTreeInputsGUI();
                }
            }
        }
    }
}
