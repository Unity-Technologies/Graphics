using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// This script enables certain renderer features based on their name
// In order to enable certain renderer feature, it's name must be perfect match
public class RendererFeatureEnabler : MonoBehaviour
{
    public string rendererFeatureName;
    public UniversalRendererData rendererData;
    private ScriptableRendererFeature usedFeature;

    // Start is called before the first frame update
    void Awake()
    {
        SceneManager.sceneUnloaded += OnSceneChanges;
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature.name == rendererFeatureName)
            {
                usedFeature = feature;
                usedFeature.SetActive(true);
            }
        }
    }

    void OnSceneChanges(Scene scene)
    {
        Debug.Log("Disabling renderer feature");
        usedFeature.SetActive(false);
    }
}
