using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class InfluenceVolumeUI
    {
        static HierarchicalBox s_BoxBaseHandle;
        static HierarchicalBox s_BoxInfluenceHandle;
        static HierarchicalBox s_BoxInfluenceNormalHandle;

        static HierarchicalSphere s_SphereBaseHandle;
        static HierarchicalSphere s_SphereInfluenceHandle;
        static HierarchicalSphere s_SphereInfluenceNormalHandle;

        static InfluenceVolumeUI()
        {
            Color[] shapeHandlesColor = new[]
            {
                k_GizmoThemeColorBase,
                k_GizmoThemeColorBase,
                k_GizmoThemeColorBase,
                k_GizmoThemeColorBase,
                k_GizmoThemeColorBase,
                k_GizmoThemeColorBase
            };

            //important: hierarchical box must be created here or the skin colors
            //may not be fully charged and handles could be drawn in black
            s_BoxBaseHandle = new HierarchicalBox(
                k_GizmoThemeColorBase,
                shapeHandlesColor);
            s_BoxInfluenceHandle = new HierarchicalBox(
                k_GizmoThemeColorInfluence,
                k_HandlesColor,
                parent: s_BoxBaseHandle);
            s_BoxInfluenceNormalHandle = new HierarchicalBox(
                k_GizmoThemeColorInfluenceNormal,
                k_HandlesColor,
                parent: s_BoxBaseHandle);

            s_SphereBaseHandle = new HierarchicalSphere(k_GizmoThemeColorBase);
            s_SphereInfluenceHandle = new HierarchicalSphere(
                k_GizmoThemeColorInfluence,
                parent: s_SphereBaseHandle);
            s_SphereInfluenceNormalHandle = new HierarchicalSphere(
                k_GizmoThemeColorInfluenceNormal,
                parent: s_SphereBaseHandle);
        }
    }
}
