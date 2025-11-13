using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class CheckAssignedRenderPipelineAsset : MonoBehaviour
{
    [SerializeField] private UniversalRenderPipelineAsset m_PipelineAsset;
    [SerializeField] private GameObject m_WarningGameObject;

    private bool? m_LastCorrectPipelineResults;

    private bool isCorrectAssetAssigned => QualitySettings.renderPipeline == m_PipelineAsset
                                           || QualitySettings.renderPipeline == null && GraphicsSettings.defaultRenderPipeline == m_PipelineAsset;

    private void Awake()
    {
        CheckIfCorrectAssetIsAssigned();
    }

    private void Update()
    {
        CheckIfCorrectAssetIsAssigned();
    }

    private void SetAllCamerasEnabled(bool enable)
    {
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Camera c in allCameras)
            c.enabled = enable;
    }
    
    private void CheckIfCorrectAssetIsAssigned()
    {
        if (m_PipelineAsset == null)
            return;

        bool correctAssetAssigned = isCorrectAssetAssigned;
        if (!m_LastCorrectPipelineResults.HasValue || m_LastCorrectPipelineResults != correctAssetAssigned)
        {
            if (!correctAssetAssigned)
            {
                Debug.LogError("Incorrect/missing Universal Render Pipeline Asset assigned in Quality or Graphics Settings. Please assign \"" + m_PipelineAsset.name + "\" to view the sample.");
                SetAllCamerasEnabled(false); // Disable cameras to prevent error spam when the RP asset is not expected
            }
            else
            {
                SetAllCamerasEnabled(true);
            }
        }

        m_LastCorrectPipelineResults = correctAssetAssigned;
        if (m_WarningGameObject != null)
            m_WarningGameObject.SetActive(!correctAssetAssigned);
    }
}
