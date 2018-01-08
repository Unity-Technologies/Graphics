namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ProjectionVolumeComponent : MonoBehaviour
    {
        [SerializeField]
        ProjectionVolume m_ProjectionVolume = new ProjectionVolume();

        public ProjectionVolume projectionVolume { get { return m_ProjectionVolume; } }
    }
}
