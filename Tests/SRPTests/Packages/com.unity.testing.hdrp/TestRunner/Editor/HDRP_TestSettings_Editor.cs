using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(HDRP_TestSettings)), DisallowMultipleComponent]
public class HDRP_TestSettings_Editor : Editor
{
    HDRP_TestSettings typedTarget;
    bool liveViewSize = false;
    int prevWidth = 0;
    int prevHeight = 0;
    Vector2 targetResolution = Vector2.zero;

    SerializedProperty captureFramerate;
    SerializedProperty waitFrames;
    SerializedProperty ImageComparisonSettings;
    SerializedProperty xrCompatible;
    SerializedProperty xrThresholdMultiplier;
    SerializedProperty gpuDrivenCompatible;
    SerializedProperty waitForFrameCountMultiple;
    SerializedProperty frameCountMultiple;
    SerializedProperty checkMemoryAllocation;
    SerializedProperty renderPipelineAsset;
    SerializedProperty forceCameraRenderDuringSetup;
    SerializedProperty containsVFX;
    SerializedProperty doBeforeTest;

    void OnEnable()
    {
        typedTarget = target as HDRP_TestSettings;
        captureFramerate = serializedObject.FindProperty("captureFramerate");
        waitFrames = serializedObject.FindProperty("waitFrames");
        ImageComparisonSettings = serializedObject.FindProperty("ImageComparisonSettings");
        xrCompatible = serializedObject.FindProperty("xrCompatible");
        xrThresholdMultiplier = serializedObject.FindProperty("xrThresholdMultiplier");
        gpuDrivenCompatible = serializedObject.FindProperty("gpuDrivenCompatible");
        waitForFrameCountMultiple = serializedObject.FindProperty("waitForFrameCountMultiple");
        frameCountMultiple = serializedObject.FindProperty("frameCountMultiple");
        checkMemoryAllocation = serializedObject.FindProperty("checkMemoryAllocation");
        renderPipelineAsset = serializedObject.FindProperty("renderPipelineAsset");
        forceCameraRenderDuringSetup = serializedObject.FindProperty("forceCameraRenderDuringSetup");
        containsVFX = serializedObject.FindProperty("containsVFX");
        doBeforeTest = serializedObject.FindProperty("doBeforeTest");
    }

    override public void OnInspectorGUI()
    {

        EditorGUILayout.PropertyField(captureFramerate);
        EditorGUILayout.PropertyField(waitFrames);
        EditorGUILayout.PropertyField(ImageComparisonSettings);



        EditorGUILayout.PropertyField(xrCompatible);
        if(xrCompatible.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(xrThresholdMultiplier);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.PropertyField(gpuDrivenCompatible);

        EditorGUILayout.PropertyField(waitForFrameCountMultiple);
        if(waitForFrameCountMultiple.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(frameCountMultiple);
            EditorGUI.indentLevel--;
        }


        EditorGUILayout.PropertyField(checkMemoryAllocation);
        EditorGUILayout.PropertyField(renderPipelineAsset);
        EditorGUILayout.PropertyField(forceCameraRenderDuringSetup);

        EditorGUILayout.PropertyField(containsVFX);
        EditorGUILayout.PropertyField(doBeforeTest);

        if(typedTarget.ImageComparisonSettings.UseBackBuffer)
        {
            targetResolution = GetResolutionFromBackBufferResolutionEnum(typedTarget.ImageComparisonSettings.ImageResolution.ToString());
        }
        else
        {
            targetResolution = new Vector2(typedTarget.ImageComparisonSettings.TargetWidth, typedTarget.ImageComparisonSettings.TargetHeight);
        }

        liveViewSize = GUILayout.Toggle(liveViewSize, "When enabled, Game View resolution is updated live.");

        if (GUILayout.Button("Set Game View Size") || liveViewSize && (prevWidth != targetResolution.x || prevHeight != targetResolution.y))
        {
            prevWidth = (int)targetResolution.x;
            prevHeight = (int)targetResolution.y;

            SetGameViewSize();
        }

        if (GUILayout.Button("Fix Texts"))
        {
            TextMeshPixelSize[] texts = FindObjectsByType<TextMeshPixelSize>(FindObjectsSortMode.InstanceID);
            foreach (TextMeshPixelSize text in texts)
            {
                text.CorrectPosition();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }

        serializedObject.ApplyModifiedProperties();
    }

    public Vector2 GetResolutionFromBackBufferResolutionEnum(string resolution)
    {
        string[] resolutionArray = resolution.ToString().Split('w')[1].Split('h');
        return new Vector2(int.Parse(resolutionArray[0]), int.Parse(resolutionArray[1]));
    }

    void SetGameViewSize()
    {
        if (typedTarget == null) return;

        GameViewUtils.SetGameViewSize((int)targetResolution.x, (int)targetResolution.y);
    }
}
