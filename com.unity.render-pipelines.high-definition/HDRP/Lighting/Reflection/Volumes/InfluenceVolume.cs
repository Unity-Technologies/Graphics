using System;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class InfluenceVolume
    {
        [SerializeField, FormerlySerializedAs("m_ShapeType")]
        Shape m_Shape = Shape.Box;
        [SerializeField, FormerlySerializedAs("m_BoxBaseOffset")]
        Vector3 m_Offset;
        [SerializeField, Obsolete("Kept only for compatibility. Use m_Offset instead")]
        Vector3 m_SphereBaseOffset;

        // Box
        [SerializeField, FormerlySerializedAs("m_BoxBaseSize")]
        Vector3 m_BoxSize = Vector3.one;
        [SerializeField, FormerlySerializedAs("m_BoxInfluencePositiveFade")]
        Vector3 m_BoxBlendDistancePositive;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNegativeFade")]
        Vector3 m_BoxBlendDistanceNegative;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNormalPositiveFade")]
        Vector3 m_BoxBlendNormalDistancePositive;
        [SerializeField, FormerlySerializedAs("m_BoxInfluenceNormalNegativeFade")]
        Vector3 m_BoxBlendNormalDistanceNegative;
        [SerializeField, FormerlySerializedAs("m_BoxPositiveFaceFade")]
        Vector3 m_BoxSideFadePositive = Vector3.one;
        [SerializeField, FormerlySerializedAs("m_BoxNegativeFaceFade")]
        Vector3 m_BoxSideFadeNegative = Vector3.one;

        //editor value that need to be saved for easy passing from simplified to advanced and vice et versa
        // /!\ must not be used outside editor code
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendDistancePositive")]
        Vector3 m_EditorAdvancedModeBlendDistancePositive;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendDistanceNegative")]
        Vector3 m_EditorAdvancedModeBlendDistanceNegative;
        [SerializeField, FormerlySerializedAs("editorSimplifiedModeBlendDistance")]
        float m_EditorSimplifiedModeBlendDistance;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendNormalDistancePositive")]
        Vector3 m_EditorAdvancedModeBlendNormalDistancePositive;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeBlendNormalDistanceNegative")]
        Vector3 m_EditorAdvancedModeBlendNormalDistanceNegative;
        [SerializeField, FormerlySerializedAs("editorSimplifiedModeBlendNormalDistance")]
        float m_EditorSimplifiedModeBlendNormalDistance;
        [SerializeField, FormerlySerializedAs("editorAdvancedModeEnabled")]
        bool m_EditorAdvancedModeEnabled;

        // Sphere
        [SerializeField, FormerlySerializedAs("m_SphereBaseRadius")]
        float m_SphereRadius = 1;
        [SerializeField, FormerlySerializedAs("m_SphereInfluenceFade")]
        float m_SphereBlendDistance;
        [SerializeField, FormerlySerializedAs("m_SphereInfluenceNormalFade")]
        float m_SphereBlendNormalDistance;

        /// <summary>Shape of this InfluenceVolume.</summary>
        public Shape shape { get { return m_Shape; } set { m_Shape = value; } }

        /// <summary>Offset of this influence volume to the component handling him.</summary>
        public Vector3 offset { get { return m_Offset; } set { m_Offset = value; } }

        public Vector3 boxSize { get { return m_BoxSize; } set { m_BoxSize = value; } }

        public Vector3 boxBlendOffset { get { return (boxBlendDistanceNegative - boxBlendDistancePositive) * 0.5f; } }
        public Vector3 boxBlendSize { get { return -(boxBlendDistancePositive + boxBlendDistanceNegative); } }
        public Vector3 boxBlendDistancePositive { get { return m_BoxBlendDistancePositive; } set { m_BoxBlendDistancePositive = value; } }
        public Vector3 boxBlendDistanceNegative { get { return m_BoxBlendDistanceNegative; } set { m_BoxBlendDistanceNegative = value; } }
        public Vector3 boxBlendNormalOffset { get { return (boxBlendNormalDistanceNegative - boxBlendNormalDistancePositive) * 0.5f; } }
        public Vector3 boxBlendNormalSize { get { return -(boxBlendNormalDistancePositive + boxBlendNormalDistanceNegative); } }
        public Vector3 boxBlendNormalDistancePositive { get { return m_BoxBlendNormalDistancePositive; } set { m_BoxBlendNormalDistancePositive = value; } }
        public Vector3 boxBlendNormalDistanceNegative { get { return m_BoxBlendNormalDistanceNegative; } set { m_BoxBlendNormalDistanceNegative = value; } }

        public Vector3 boxSideFadePositive { get { return m_BoxSideFadePositive; } set { m_BoxSideFadePositive = value; } }
        public Vector3 boxSideFadeNegative { get { return m_BoxSideFadeNegative; } set { m_BoxSideFadeNegative = value; } }


        public float sphereRadius { get { return m_SphereRadius; } set { m_SphereRadius = value; } }
        public float sphereBlendDistance { get { return m_SphereBlendDistance; } set { m_SphereBlendDistance = value; } }
        public float sphereBlendNormalDistance { get { return m_SphereBlendNormalDistance; } set { m_SphereBlendNormalDistance = value; } }

        internal BoundingSphere GetBoundingSphereAt(Transform probeTransform)
        {
            switch (shape)
            {
                default:
                case Shape.Sphere:
                    return new BoundingSphere(probeTransform.TransformPoint(offset), sphereRadius);
                case Shape.Box:
                {
                    var position = probeTransform.TransformPoint(offset);
                    var radius = Mathf.Max(boxSize.x, Mathf.Max(boxSize.y, boxSize.z));
                    return new BoundingSphere(position, radius);
                }
            }
        }

        internal Bounds GetBoundsAt(Transform probeTransform)
        {
            switch (shape)
            {
                default:
                case Shape.Sphere:
                    return new Bounds(probeTransform.position, Vector3.one * sphereRadius);
                case Shape.Box:
                {
                    var position = probeTransform.TransformPoint(offset);
                    return new Bounds(position, boxSize);
                }
            }
        }

        internal Vector3 GetWorldPosition(Transform probeTransform)
        {
            return probeTransform.TransformPoint(offset);
        }

        internal void MigrateOffsetSphere()
        {
            if (shape == Shape.Sphere)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                m_Offset = m_SphereBaseOffset;
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
    }
}
