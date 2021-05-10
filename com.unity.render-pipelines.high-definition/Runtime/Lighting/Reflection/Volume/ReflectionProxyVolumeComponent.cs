namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Use this components to define a proxy volume for the reflection probes.
    /// </summary>
    [HDRPHelpURLAttribute("Reflection-Proxy-Volume")]
    [AddComponentMenu("Rendering/Reflection Proxy Volume")]
    public class ReflectionProxyVolumeComponent : MonoBehaviour
    {
        [SerializeField]
        ProxyVolume m_ProxyVolume = new ProxyVolume();

        /// <summary>Access to proxy volume parameters</summary>
        public ProxyVolume proxyVolume => m_ProxyVolume;
    }
}
