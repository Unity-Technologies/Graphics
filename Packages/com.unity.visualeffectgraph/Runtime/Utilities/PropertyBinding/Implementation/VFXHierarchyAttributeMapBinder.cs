using System.Collections.Generic;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Hierarchy to Attribute Map Binder")]
    [VFXBinder("Point Cache/Hierarchy to Attribute Map")]
    class VFXHierarchyAttributeMapBinder : VFXBinderBase
    {
        [VFXPropertyBinding("System.UInt32"), SerializeField]
        protected ExposedProperty m_BoneCount = "BoneCount";

        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        protected ExposedProperty m_PositionMap = "PositionMap";

        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        protected ExposedProperty m_TargetPositionMap = "TargetPositionMap";

        [VFXPropertyBinding("UnityEngine.Texture2D"), SerializeField]
        protected ExposedProperty m_RadiusPositionMap = "RadiusPositionMap";

        public enum RadiusMode
        {
            Fixed,
            Interpolate
        }

        public Transform HierarchyRoot;
        public float DefaultRadius = 0.1f;
        public uint MaximumDepth = 3;
        public RadiusMode Radius = RadiusMode.Fixed;

        private Texture2D position;
        private Texture2D targetPosition;
        private Texture2D radius;
        private List<Bone> bones;

        private struct Bone
        {
            public Transform source;
            public float sourceRadius;
            public Transform target;
            public float targetRadius;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateHierarchy();
        }

        void OnValidate()
        {
            UpdateHierarchy();
        }

        void UpdateHierarchy()
        {
            bones = ChildrenOf(HierarchyRoot, MaximumDepth);
            int count = bones.Count;

            position = new Texture2D(count, 1, TextureFormat.RGBAHalf, false, true);
            targetPosition = new Texture2D(count, 1, TextureFormat.RGBAHalf, false, true);
            radius = new Texture2D(count, 1, TextureFormat.RHalf, false, true);

            UpdateData();
        }

        List<Bone> ChildrenOf(Transform source, uint depth)
        {
            List<Bone> output = new List<Bone>();
            if (source == null)
                return output;

            foreach (Transform child in source)
            {
                output.Add(new Bone()
                {
                    source = source.transform,
                    target = child.transform,
                    sourceRadius = DefaultRadius,
                    targetRadius = DefaultRadius,
                });
                if (depth > 0)
                    output.AddRange(ChildrenOf(child, depth - 1));
            }
            return output;
        }

        void UpdateData()
        {
            int count = bones.Count;
            if (position.width != count) return;

            List<Color> positionList = new List<Color>();
            List<Color> targetList = new List<Color>();
            List<Color> radiusList = new List<Color>();

            for (int i = 0; i < count; i++)
            {
                Bone b = bones[i];
                positionList.Add(new Color(b.source.position.x, b.source.position.y, b.source.position.z, 1));
                targetList.Add(new Color(b.target.position.x, b.target.position.y, b.target.position.z, 1));
                radiusList.Add(new Color(b.sourceRadius, 0, 0, 1));
            }
            position.SetPixels(positionList.ToArray());
            targetPosition.SetPixels(targetList.ToArray());
            radius.SetPixels(radiusList.ToArray());

            position.Apply();
            targetPosition.Apply();
            radius.Apply();
        }

        public override bool IsValid(VisualEffect component)
        {
            return HierarchyRoot != null
                && component.HasTexture(m_PositionMap)
                && component.HasTexture(m_TargetPositionMap)
                && component.HasTexture(m_RadiusPositionMap)
                && component.HasUInt(m_BoneCount);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            UpdateData();

            component.SetTexture(m_PositionMap, position);
            component.SetTexture(m_TargetPositionMap, targetPosition);
            component.SetTexture(m_RadiusPositionMap, radius);
            component.SetUInt(m_BoneCount, (uint)bones.Count);
        }

        public override string ToString()
        {
            return string.Format("Hierarchy: {0} -> {1}", HierarchyRoot == null ? "(null)" : HierarchyRoot.name, m_PositionMap);
        }
    }
}
