using System;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A volume to modify how the probe subdivision is distributed in the scene.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Light/Experimental/Probe Hint Volume")]
    public class ProbeHintVolume : MonoBehaviour
    {
        [SerializeField]
        Vector3 m_Extent = Vector3.one;

        public int maxSubdivision = 1;

        public Vector3 extent
        {
            get => Vector3.Scale(m_Extent, transform.localScale);
            set
            {
                var scale = transform.localScale;
                var inverseScale = new Vector3(1.0f / scale.x, 1.0f / scale.y, 1.0f / scale.z);
                m_Extent = Vector3.Scale(value, inverseScale);
            }
        }

        public Matrix4x4 GetTransform()
        {
            return Matrix4x4.TRS(transform.position, ProbeReferenceVolume.instance.GetTransform().rot, extent);
        }
    }
}
