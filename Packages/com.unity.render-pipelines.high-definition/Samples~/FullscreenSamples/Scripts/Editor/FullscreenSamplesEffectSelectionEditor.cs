using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FullscreenSamplesEffectSelection))]
public class FullscreenSamplesSelectionEditor : Editor
{
    SerializedProperty selectionEnum;
    SerializedProperty effectPrefabs;
    SerializedProperty dayTime;
    SerializedProperty addTimeOfDay;
    SerializedProperty directionalLight;
    SerializedProperty sceneVolume;
    SerializedProperty dayVolumeProfile;
    SerializedProperty nightVolumeProfile;
    SerializedProperty infoText;

    private void OnEnable()
    {
        selectionEnum = serializedObject.FindProperty("fullscreenEffect");
        effectPrefabs= serializedObject.FindProperty("effectPrefabs");
        dayTime = serializedObject.FindProperty("timeOfDay");
        addTimeOfDay = serializedObject.FindProperty("useAttachedDayNightProfile");
        directionalLight = serializedObject.FindProperty("directionalLight");
        sceneVolume = serializedObject.FindProperty("sceneVolume");
        dayVolumeProfile = serializedObject.FindProperty("dayVolumeProfile");
        nightVolumeProfile = serializedObject.FindProperty("nightVolumeProfile");
        infoText = serializedObject.FindProperty("infoText");
    }

    public override void OnInspectorGUI()
    {
        FullscreenSamplesEffectSelection selection = (FullscreenSamplesEffectSelection)target;

        serializedObject.Update();
        EditorGUILayout.PropertyField(selectionEnum);
        EditorGUILayout.HelpBox(selection.infos, MessageType.Info);
        EditorGUILayout.PropertyField(addTimeOfDay);
        EditorGUILayout.PropertyField(infoText);
        if (addTimeOfDay.boolValue)
        {
            EditorGUILayout.PropertyField(dayTime);
            EditorGUILayout.PropertyField(directionalLight);
            EditorGUILayout.PropertyField(sceneVolume);
            EditorGUILayout.PropertyField(dayVolumeProfile);
            EditorGUILayout.PropertyField(nightVolumeProfile);
        }
        EditorGUILayout.PropertyField(effectPrefabs);

        serializedObject.ApplyModifiedProperties();

    }

}
