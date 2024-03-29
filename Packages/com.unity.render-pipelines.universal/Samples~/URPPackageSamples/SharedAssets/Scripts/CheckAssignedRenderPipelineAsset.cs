using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class CheckAssignedRenderPipelineAsset : MonoBehaviour
{
    [SerializeField] private UniversalRenderPipelineAsset m_PipelineAsset;
    [SerializeField] private GameObject m_WarningGameObject;

    private bool m_LastCorrectPipelineResults = false;

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

    private void CheckIfCorrectAssetIsAssigned()
    {
        if (m_PipelineAsset == null)
            return;

        bool correctAssetAssigned = isCorrectAssetAssigned;
        if (!correctAssetAssigned && m_LastCorrectPipelineResults != correctAssetAssigned)
            Debug.LogError("Incorrect/missing Universal Renderpipeline Asset assigned in Quality or Graphics Settings.\nPlease assign \"" + m_PipelineAsset.name + "\" to it.");

        m_LastCorrectPipelineResults = correctAssetAssigned;
        if (m_WarningGameObject != null)
            m_WarningGameObject.SetActive(!correctAssetAssigned);
    }
}
