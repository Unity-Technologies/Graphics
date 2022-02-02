using System;
using UnityEngine;
using UnityEngine.Rendering;

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
        public Vector3 center = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public float radius = 0.1f;
        public float height = 1.0f;
        public CapsuleOccluderLightLayer lightLayersMask = CapsuleOccluderLightLayer.LightLayerDefault;

        public Matrix4x4 capsuleToWorld
        {
            get
            {
                Transform tr = transform;
                Vector3 scale = tr.lossyScale;
                float xyScale = Mathf.Max(scale.x, scale.y);
                return Matrix4x4.TRS(
                    tr.TransformPoint(center),
                    tr.rotation * rotation,
                    new Vector3(xyScale, xyScale, scale.z));
            }
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
}
