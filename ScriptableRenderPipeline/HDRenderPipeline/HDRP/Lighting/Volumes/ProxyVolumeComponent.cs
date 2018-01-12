namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ProxyVolumeComponent : MonoBehaviour
    {
        [SerializeField]
        ProxyVolume m_ProxyVolume = new ProxyVolume();

        public ProxyVolume proxyVolume { get { return m_ProxyVolume; } }
    }
}
