using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// It seems our XR rendering doesn't respect the MSAA override setting on base cameras
// (which have options to disable MSAA or use the setting on the pipeline asset)
//
// As XR only uses the MSAA set on the pipeline asset, this wokaround temporarily
// changes the asset to match the override set on the base camera
public class DisableMsaaXrWorkaround : MonoBehaviour
{
    private UniversalRenderPipelineAsset m_UrpAsset;
    private int m_PreviousMsaaSampleCount;

    void Awake()
    {
        m_UrpAsset = null;
        Camera camera = GameObject.FindGameObjectWithTag("MainCamera")?.GetComponent<Camera>();
        if (camera != null && !camera.allowMSAA)
        {
            m_UrpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (m_UrpAsset != null)
            {
                m_PreviousMsaaSampleCount = m_UrpAsset.msaaSampleCount;
                m_UrpAsset.msaaSampleCount = 1;
            }
        }
    }

    void OnDisable()
    {
        if (m_UrpAsset != null)
        {
            m_UrpAsset.msaaSampleCount = m_PreviousMsaaSampleCount;
            m_UrpAsset = null;
        }
    }
}
