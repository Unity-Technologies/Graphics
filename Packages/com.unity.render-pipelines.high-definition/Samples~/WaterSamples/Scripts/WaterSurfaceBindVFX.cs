using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
[CustomEditor(typeof(WaterSurfaceBindVFX))]
public class WaterSurfaceBindVFXEditor : Editor
{
    SerializedProperty waterSurface;

    private StringBuilder stringBuilder = new();

    void OnEnable()
    {
        waterSurface = serializedObject.FindProperty(nameof(WaterSurfaceBindVFX.waterSurface));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(waterSurface);
        serializedObject.ApplyModifiedProperties();

        var waterSurfaceBind = serializedObject.targetObject as WaterSurfaceBindVFX;
        if (waterSurfaceBind != null && waterSurfaceBind.enabled)
        {
            stringBuilder.Clear();
            var messageType = MessageType.Info;
            stringBuilder.AppendFormat("VisualEffects Count: {0}", waterSurfaceBind.visualEffects.Length);

            if (!waterSurfaceBind.waterSurface)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendFormat("WaterSurface is null.");
                messageType = MessageType.Warning;
            }
            else if (!waterSurfaceBind.active)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendFormat("WaterSurfaceBindVFX is inactive (check HDRenderSettings if Water is enabled).");
                messageType = MessageType.Warning;
            }

            if (WaterSurfaceBindVFX.registeredWaterSurfaces.Count > 1)
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendFormat("There are more than one WaterSurfaceBindVFX, this bindings relies on global and can conflict each other.");
                messageType = MessageType.Error;
            }

            EditorGUILayout.HelpBox(stringBuilder.ToString(), messageType);
        }
    }
}
#endif

[ExecuteInEditMode]
public class WaterSurfaceBindVFX : MonoBehaviour
{
    public WaterSurface waterSurface;
    
    public bool active { get; private set; }
    public VisualEffect[] visualEffects { get; private set; }

#if UNITY_EDITOR
    public static List<WaterSurfaceBindVFX> registeredWaterSurfaces = new();
#endif

    void OnEnable()
    {
#if UNITY_EDITOR
        registeredWaterSurfaces.Add(this);
#endif
        visualEffects = gameObject.GetComponentsInChildren<VisualEffect>(true);
        UpdateActive(false);
    }

    private void UpdateActive(bool newActive)
    {
        active = newActive;
        foreach (var vfx in visualEffects)
            vfx.enabled = newActive;
    }

    void Update()
    {
        var newActive = waterSurface != null && waterSurface.SetGlobalTextures();
        if (newActive != active)
        {
            UpdateActive(newActive);
        }
    }

    void OnDisable()
    {
        UpdateActive(false);
        visualEffects = Array.Empty<VisualEffect>();
#if UNITY_EDITOR
        registeredWaterSurfaces.Remove(this);
#endif
    }
}
