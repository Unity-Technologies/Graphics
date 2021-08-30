using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using Object = UnityEngine.Object;
using UnityEditorInternal;
using System.Reflection;

[CustomEditor(typeof(VFXParameter), true)]
[CanEditMultipleObjects]
class VFXParameterEditor : VFXSlotContainerEditor
{
    VFXViewController controller;
    protected new void OnEnable()
    {
        base.OnEnable();

        VFXViewWindow current = VFXViewWindow.currentWindow;
        if (current != null)
        {
            controller = current.graphView.controller;
            if (controller != null)
                controller.useCount++;
        }
    }

    protected new void OnDisable()
    {
        if (controller != null)
        {
            controller.useCount--;
            controller = null;
        }
        base.OnDisable();
    }

    public override SerializedProperty DoInspectorGUI()
    {
        var saveEnabled = GUI.enabled;

        var referenceModel = serializedObject.targetObject as VFXModel;
        var resource = referenceModel.GetResource();
        if (resource != null && !resource.IsAssetEditable())
        {
            GUI.enabled = false;
            saveEnabled = false;
        }

        if (serializedObject.isEditingMultipleObjects)
        {
            GUI.enabled = false; // no sense to change the name in multiple selection because the name must be unique
            EditorGUI.showMixedValue = true;
            EditorGUILayout.TextField("Exposed Name", "-");
            EditorGUI.showMixedValue = false;
            GUI.enabled = saveEnabled;
        }
        else
        {
            VFXParameter parameter = (VFXParameter)target;

            GUI.enabled = controller != null && saveEnabled;
            string newName = EditorGUILayout.DelayedTextField("Exposed Name", parameter.exposedName);
            GUI.enabled = saveEnabled;
            if (GUI.changed)
            {
                VFXParameterController parameterController = controller.GetParameterController(parameter);
                if (parameterController != null)
                {
                    parameterController.exposedName = newName;
                }
            }
        }
        return base.DoInspectorGUI();
    }
}
