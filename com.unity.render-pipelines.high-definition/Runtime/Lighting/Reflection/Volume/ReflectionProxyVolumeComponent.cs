namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ReflectionProxyVolumeComponent : MonoBehaviour
    {
        [SerializeField]
        ProxyVolume m_ProxyVolume = new ProxyVolume();

        /// <summary>Access to proxy volume parameters</summary>
        public ProxyVolume proxyVolume => m_ProxyVolume;
    }
}
