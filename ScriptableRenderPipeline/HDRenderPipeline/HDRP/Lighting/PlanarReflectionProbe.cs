using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEngine.Experimental.Rendering
{
    public class PlanarReflectionProbe : MonoBehaviour
    {
        [SerializeField]
        ProjectionVolumeComponent m_ProjectionVolumeReference;
        [SerializeField]
        InfluenceVolume m_InfluenceVolume;

        public ProjectionVolumeComponent projectionVolumeReference { get { return m_ProjectionVolumeReference; } }
        public InfluenceVolume influenceVolume { get { return m_InfluenceVolume; } }
    }
}
