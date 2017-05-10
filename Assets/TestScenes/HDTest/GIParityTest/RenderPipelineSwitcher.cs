using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode]
public class RenderPipelineSwitcher : MonoBehaviour {
    [Tooltip("The RenderPipelineAsset to active while behaviour is enabled. Null is a valid choice as well.")]
    public RenderPipelineAsset  enabledPipelineAsset;

    [Tooltip("GameObjects to activate while behaviour is enabled.")]
    public GameObject[]         activeGameObjects;

    [Tooltip("GameObjects to deactivate while behaviour is enabled.")]
    public GameObject[]         inactiveGameObjects;

    RenderPipelineAsset m_PrevPipelineAsset;

    void OnEnable() {
        m_PrevPipelineAsset = GraphicsSettings.renderPipelineAsset;
        GraphicsSettings.renderPipelineAsset = enabledPipelineAsset;

        if(activeGameObjects != null)
            foreach(var go in activeGameObjects)
                go.SetActive(true);

        if(inactiveGameObjects != null)
            foreach(var go in inactiveGameObjects)
                go.SetActive(false);
	}

    void OnDisable() {
        GraphicsSettings.renderPipelineAsset = m_PrevPipelineAsset;

        if(inactiveGameObjects != null)
            foreach(var go in inactiveGameObjects)
                go.SetActive(true);

        if(activeGameObjects != null)
            foreach(var go in activeGameObjects)
                go.SetActive(false);
    }
}
