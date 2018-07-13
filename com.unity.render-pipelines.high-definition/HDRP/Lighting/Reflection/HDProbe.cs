using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract class HDProbe : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("proxyVolumeComponent"), FormerlySerializedAs("m_ProxyVolumeReference")]
        ReflectionProxyVolumeComponent m_ProxyVolume = null;

        [SerializeField, FormerlySerializedAsAttribute("dimmer"), FormerlySerializedAsAttribute("m_Dimmer"), FormerlySerializedAsAttribute("multiplier")]
        float m_Multiplier = 1.0f;

        [SerializeField, FormerlySerializedAsAttribute("weight")]
        [Range(0.0f, 1.0f)]
        float m_Weight = 1.0f;

        public ReflectionProxyVolumeComponent proxyVolume { get { return m_ProxyVolume; } }
        public float multiplier { get { return m_Multiplier; } }
        public float weight { get { return m_Weight; } }
    }
}
