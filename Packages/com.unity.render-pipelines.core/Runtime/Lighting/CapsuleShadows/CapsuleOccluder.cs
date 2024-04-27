using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>Light Layers.</summary>
    [Flags]
    public enum CapsuleOccluderLightLayer
    {
        /// <summary>The light will no affect any object.</summary>
        Nothing = 0,   // Custom name for "Nothing" option
        /// <summary>Light Layer 0.</summary>
        LightLayerDefault = 1 << 0,
        /// <summary>Light Layer 1.</summary>
        LightLayer1 = 1 << 1,
        /// <summary>Light Layer 2.</summary>
        LightLayer2 = 1 << 2,
        /// <summary>Light Layer 3.</summary>
        LightLayer3 = 1 << 3,
        /// <summary>Light Layer 4.</summary>
        LightLayer4 = 1 << 4,
        /// <summary>Light Layer 5.</summary>
        LightLayer5 = 1 << 5,
        /// <summary>Light Layer 6.</summary>
        LightLayer6 = 1 << 6,
        /// <summary>Light Layer 7.</summary>
        LightLayer7 = 1 << 7,
        /// <summary>Everything.</summary>
        Everything = 0xFF, // Custom name for "Everything" option
    }

    [ExecuteAlways]
    public class CapsuleOccluder : MonoBehaviour
    {
        public Vector3 m_Center = Vector3.zero;
        public Quaternion m_Rotation = Quaternion.identity;
        public float m_Radius = 1f;
        public float m_Height = 1f;

        public CapsuleOccluderLightLayer m_LightLayerMask = CapsuleOccluderLightLayer.LightLayerDefault;
        public bool IsResetable => isResetable;
        public CapsuleModel Model => model;

        private CapsuleParams generatedParams;
        private bool isResetable;
        private CapsuleModel model;

        public void SetOriginalParams(CapsuleParams param)
        {
            generatedParams = param;
            m_Center = param.center;
            m_Rotation = param.rotation;
            m_Height = param.height;
            m_Radius = 0.5f * param.diameter;
            isResetable = true;
        }

        public void SetModel(CapsuleModel capsuleModel)
        {
            model = capsuleModel;
        }

        public bool IsChanged()
        {
            if (!isResetable)
            {
                return false;
            }

            bool center = m_Center != generatedParams.center;
            bool rot = m_Rotation != generatedParams.rotation;
            bool radius = Math.Abs(m_Radius - 0.5f * generatedParams.diameter) > .001f;
            bool height = Mathf.Abs(m_Height - generatedParams.height) > .001f;

            return center || rot || radius || height;
        }
        public void ResetParams()
        {
            if (isResetable)
            {
                m_Center = generatedParams.center;
                m_Rotation = generatedParams.rotation;
                m_Height = generatedParams.height;
                m_Radius = 0.5f * generatedParams.diameter;
            }
        }

        public Matrix4x4 CapsuleToWorld
        {
            get
            {
                Transform tr = transform;
                Vector3 scale = tr.lossyScale;
                float xyScale = Mathf.Max(scale.x, scale.y);
                return Matrix4x4.TRS(
                    tr.TransformPoint(m_Center),
                    tr.rotation * m_Rotation,
                    new Vector3(xyScale, xyScale, scale.z));
            }
        }

        private void OnValidate()
        {
            m_Radius = Mathf.Max(m_Radius, 0.0f);
            m_Height = Mathf.Max(m_Height , 0.0f);
        }

        private void OnEnable()
        {
            CapsuleOccluderManager.instance.RegisterCapsule(this);
        }

        private void OnDisable()
        {
            CapsuleOccluderManager.instance.DeregisterCapsule(this);
        }
    }

    public struct CapsuleParams
    {
        public Vector3 center;
        public Quaternion rotation;
        public float height;
        public float diameter;

        public static CapsuleParams FromOrientedBounds(Quaternion rotation, Bounds bounds)
        {
            Vector3 center = rotation * bounds.center;
            Vector3 size = bounds.size;

            float height = size.z;
            float diameter = Mathf.Max(size.x, size.y);
            Quaternion capsuleRotation = rotation;
            if (size.y > height)
            {
                height = size.y;
                diameter = Mathf.Max(size.z, size.x);
                capsuleRotation = rotation * Quaternion.FromToRotation(Vector3.up, Vector3.forward);
            }
            if (size.x > height)
            {
                height = size.x;
                diameter = Mathf.Max(size.y, size.z);
                capsuleRotation = rotation * Quaternion.FromToRotation(Vector3.right, Vector3.forward);
            }

            return new CapsuleParams {
                center = center,
                rotation = capsuleRotation,
                height = height,
                diameter = diameter,
            };
        }
    };

    public class CapsuleModel
    {
        public CapsuleOccluder m_Occluder;
        public Transform m_BoneTransform;
        public SkinnedMeshRenderer m_SkinnedMeshRenderer;
        public List<CapsuleModel> m_SubItems = new();
        public int m_Sequence;

        public override string ToString()
        {
            return string.Format(@"{0}", m_BoneTransform.gameObject.name);
        }

        internal void SetSequence(Func<int> getSequence)
        {
            if (m_Sequence == 0)
            {
                m_Sequence = getSequence.Invoke();
            }
        }
    }
}
