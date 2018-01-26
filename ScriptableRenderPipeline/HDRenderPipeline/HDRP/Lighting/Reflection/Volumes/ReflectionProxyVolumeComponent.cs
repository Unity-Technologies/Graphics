namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ReflectionProxyVolumeComponent : MonoBehaviour
    {
        [SerializeField]
        ProxyVolume m_ProxyVolume = new ProxyVolume();

        public ProxyVolume proxyVolume { get { return m_ProxyVolume; } }
    }
}
