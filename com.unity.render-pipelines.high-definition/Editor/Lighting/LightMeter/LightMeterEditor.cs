using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Reflection;
using System.Linq.Expressions;
using System;

[CustomEditor(typeof(LightMeter))]
public class LightMeterEditor : Editor
{
    LightMeter luxMeter;

    void OnEnable()
    {
        luxMeter = target as LightMeter;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        GUILayout.Label("Measured: " + luxMeter.sampledValue + " lux");
    }
}
