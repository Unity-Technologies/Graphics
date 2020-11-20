using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

[CustomEditor(typeof(PostProcessingRendererFeature))]
public class PostProcessingRendererFeatureEditor : Editor
{
    private int selectedVolume;
    private string[] volumeComponentOptions;
    private PostProcessingRendererFeature _rendererFeature;
    
    private void OnEnable()
    {
        volumeComponentOptions = getVolumeComponentOptions();
        _rendererFeature = (PostProcessingRendererFeature) target;
    }

    public override void OnInspectorGUI()
    {
        selectedVolume = _rendererFeature.settings.volumeComponentIndex;
        selectedVolume = EditorGUILayout.Popup("Volume Component" ,selectedVolume, volumeComponentOptions);
        _rendererFeature.settings.volumeComponentName = volumeComponentOptions[_rendererFeature.settings.volumeComponentIndex];
        _rendererFeature.settings.volumeComponentIndex = selectedVolume;
    }

    private string[] getVolumeComponentOptions()
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            .Where(x => typeof(VolumeComponent).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract && x.GetCustomAttribute<ControlsShaderAttribute>() != null)
            .Select(x => x.Name).ToArray();
    }
}
