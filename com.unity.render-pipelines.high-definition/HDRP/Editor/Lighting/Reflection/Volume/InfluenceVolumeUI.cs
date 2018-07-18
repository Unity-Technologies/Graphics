using System;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI : BaseUI<SerializedInfluenceVolume>
    {
        const int k_AnimBoolFields = 2;
        internal static readonly Color k_GizmoThemeColorBase = new Color(255f / 255f, 229f / 255f, 148f / 255f, 80f / 255f);
        internal static readonly Color k_GizmoThemeColorBaseFace = new Color(255f / 255f, 229f / 255f, 148f / 255f, 45f / 255f);
        internal static readonly Color k_GizmoThemeColorInfluence = new Color(83f / 255f, 255f / 255f, 95f / 255f, 75f / 255f);
        internal static readonly Color k_GizmoThemeColorInfluenceFace = new Color(83f / 255f, 255f / 255f, 95f / 255f, 17f / 255f);
        internal static readonly Color k_GizmoThemeColorInfluenceNormal = new Color(0f / 255f, 229f / 255f, 255f / 255f, 80f / 255f);
        internal static readonly Color k_GizmoThemeColorInfluenceNormalFace = new Color(0f / 255f, 229f / 255f, 255f / 255f, 36f / 255f);
        internal static readonly Color k_GizmoThemeColorProjection = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        internal static readonly Color k_GizmoThemeColorProjectionFace = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        internal static readonly Color k_GizmoThemeColorDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);
        internal static readonly Color k_GizmoThemeColorDisabledFace = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);

        static readonly int k_ShapeCount = Enum.GetValues(typeof(ShapeType)).Length;

        public Gizmo6FacesBox boxBaseHandle;
        public Gizmo6FacesBoxContained boxInfluenceHandle;
        public Gizmo6FacesBoxContained boxInfluenceNormalHandle;

        public SphereBoundsHandle sphereBaseHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceHandle = new SphereBoundsHandle();
        public SphereBoundsHandle sphereInfluenceNormalHandle = new SphereBoundsHandle();

        public AnimBool isSectionExpandedShape { get { return m_AnimBools[k_ShapeCount]; } }
        public bool showInfluenceHandles { get; set; }

        public InfluenceVolumeUI()
            : base(k_ShapeCount + k_AnimBoolFields)
        {
            isSectionExpandedShape.value = true;

            boxBaseHandle = new Gizmo6FacesBox(monochromeFace:true, monochromeSelectedFace:true);
            boxInfluenceHandle = new Gizmo6FacesBoxContained(boxBaseHandle, monochromeFace:true, monochromeSelectedFace:true);
            boxInfluenceNormalHandle = new Gizmo6FacesBoxContained(boxBaseHandle, monochromeFace:true, monochromeSelectedFace:true);

            Color[] handleColors = new Color[]
            {
                HDReflectionProbeEditor.k_handlesColor[0][0],
                HDReflectionProbeEditor.k_handlesColor[0][1],
                HDReflectionProbeEditor.k_handlesColor[0][2],
                HDReflectionProbeEditor.k_handlesColor[1][0],
                HDReflectionProbeEditor.k_handlesColor[1][1],
                HDReflectionProbeEditor.k_handlesColor[1][2]
            };
            boxBaseHandle.handleColors = handleColors;
            boxInfluenceHandle.handleColors = handleColors;
            boxInfluenceNormalHandle.handleColors = handleColors;

            boxBaseHandle.faceColors = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorExtent };
            boxBaseHandle.faceColorsSelected = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorExtentFace };
            boxInfluenceHandle.faceColors = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlend };
            boxInfluenceHandle.faceColorsSelected = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceBlendFace };
            boxInfluenceNormalHandle.faceColors = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlend };
            boxInfluenceNormalHandle.faceColorsSelected = new Color[] { HDReflectionProbeEditor.k_GizmoThemeColorInfluenceNormalBlendFace };
        }

        public override void Update()
        {
            base.Update();
            SetIsSectionExpanded_Shape((ShapeType)data.shapeType.intValue);
        }

        void SetIsSectionExpanded_Shape(ShapeType shape)
        {
            for (var i = 0; i < k_ShapeCount; i++)
                m_AnimBools[i].target = (int)shape == i;
        }

        public AnimBool IsSectionExpanded_Shape(ShapeType shapeType)
        {
            return m_AnimBools[(int)shapeType];
        }
    }
}
