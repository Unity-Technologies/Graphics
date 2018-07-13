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

        public ReflectionProxyVolumeComponent proxyVolume { get { return m_ProxyVolume; } }
        public float multiplier { get { return m_Multiplier; } }
    }
}
