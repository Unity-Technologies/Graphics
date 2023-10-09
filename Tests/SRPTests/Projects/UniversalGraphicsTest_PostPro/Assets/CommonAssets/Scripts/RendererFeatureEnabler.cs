using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// This script enables certain renderer features based on their name
// In order to enable certain renderer feature, it's name must be perfect match
[ExecuteInEditMode]
public class RendererFeatureEnabler : MonoBehaviour
{
    public string[] rendererFeatureNames;
    public UniversalRendererData rendererData;

    private List<ScriptableRendererFeature> m_UsedFeatures;

    // Start is called before the first frame update
    void Awake()
    {
        m_UsedFeatures = new List<ScriptableRendererFeature>();
        foreach (var feature in rendererData.rendererFeatures)
        {
            foreach (var name in rendererFeatureNames)
            {
                if (name != null && feature.name == name)
                {
                    feature.SetActive(true);
                    m_UsedFeatures.Add(feature);
                }
            }
        }
    }

    void OnDestroy()
    {
        if (m_UsedFeatures != null)
        {
            foreach (var feature in m_UsedFeatures)
            {
                if(feature != null)
                    feature.SetActive(false);
            }
        }
    }
}
