using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract class HDProbe : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("proxyVolumeComponent"), FormerlySerializedAs("m_ProxyVolumeReference")]
        ReflectionProxyVolumeComponent m_ProxyVolume = null;

        public ReflectionProxyVolumeComponent proxyVolume { get { return m_ProxyVolume; } }
    }
}
