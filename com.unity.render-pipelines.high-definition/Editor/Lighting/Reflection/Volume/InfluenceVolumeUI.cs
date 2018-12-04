using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class InfluenceVolumeUI : IUpdateable<SerializedInfluenceVolume>
    {
        [Flags]
        internal enum Flag
        {
            None = 0,
            SectionExpandedShape = 1 << 0,
            SectionExpandedShapeSphere = 1 << 1,
            SectionExpandedShapeBox = 1 << 2,
            ShowInfluenceHandle = 1 << 3
        }

        EditorPrefBoolFlags<Flag> m_FlagStorage = new EditorPrefBoolFlags<Flag>("InfluenceVolumeUI");

        public HierarchicalBox boxBaseHandle;
        public HierarchicalBox boxInfluenceHandle;
        public HierarchicalBox boxInfluenceNormalHandle;

        public HierarchicalSphere sphereBaseHandle;
        public HierarchicalSphere sphereInfluenceHandle;
        public HierarchicalSphere sphereInfluenceNormalHandle;

        public bool HasFlag(Flag v) => m_FlagStorage.HasFlag(v);
        public bool SetFlag(Flag f, bool v) => m_FlagStorage.SetFlag(f, v);

        public InfluenceVolumeUI()
        {
            boxBaseHandle = new HierarchicalBox(
                k_GizmoThemeColorBase, k_HandlesColor
            );
            boxInfluenceHandle = new HierarchicalBox(
                k_GizmoThemeColorInfluence,
                k_HandlesColor, parent: boxBaseHandle
            );
            boxInfluenceNormalHandle = new HierarchicalBox(
                k_GizmoThemeColorInfluenceNormal,
                k_HandlesColor, parent: boxBaseHandle
            );

            sphereBaseHandle = new HierarchicalSphere(k_GizmoThemeColorBase);
            sphereInfluenceHandle = new HierarchicalSphere(
                k_GizmoThemeColorInfluence, parent: sphereBaseHandle
            );
            sphereInfluenceNormalHandle = new HierarchicalSphere(
                k_GizmoThemeColorInfluenceNormal, parent: sphereBaseHandle
            );
        }

        public void Update(SerializedInfluenceVolume v)
        {
            m_FlagStorage.SetFlag(Flag.SectionExpandedShapeBox | Flag.SectionExpandedShapeSphere, false);
            switch ((InfluenceShape)v.shape.intValue)
            {
                case InfluenceShape.Box: m_FlagStorage.SetFlag(Flag.SectionExpandedShapeBox, true); break;
                case InfluenceShape.Sphere: m_FlagStorage.SetFlag(Flag.SectionExpandedShapeSphere, true); break;
            }
        }
    }
}
