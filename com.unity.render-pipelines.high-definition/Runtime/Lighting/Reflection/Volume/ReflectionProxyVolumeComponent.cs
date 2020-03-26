namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Use this components to define a proxy volume for the reflection probes.
    /// </summary>
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Reflection-Proxy-Volume" + Documentation.endURL)]
    [AddComponentMenu("Rendering/Reflection Proxy Volume")]
    public class ReflectionProxyVolumeComponent : MonoBehaviour
    {
        [SerializeField]
        ProxyVolume m_ProxyVolume = new ProxyVolume();

        /// <summary>Access to proxy volume parameters</summary>
        public ProxyVolume proxyVolume => m_ProxyVolume;
    }
}
