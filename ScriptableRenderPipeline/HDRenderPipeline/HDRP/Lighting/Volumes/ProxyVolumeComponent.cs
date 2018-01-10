namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ProxyVolumeComponent : MonoBehaviour
    {
        [SerializeField]
        ProjectionVolume m_ProjectionVolume = new ProjectionVolume();

        public ProjectionVolume projectionVolume { get { return m_ProjectionVolume; } }
    }
}
