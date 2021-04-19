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
        if (serializedObject.isEditingMultipleObjects)
        {
            GUI.enabled = false; // no sense to change the name in multiple selection because the name must be unique
            EditorGUI.showMixedValue = true;
            EditorGUILayout.TextField("Exposed Name", "-");
            EditorGUI.showMixedValue = false;
            GUI.enabled = true;
        }
        else
        {
            VFXParameter parameter = (VFXParameter)target;

            GUI.enabled = controller != null;
            string newName = EditorGUILayout.DelayedTextField("Exposed Name", parameter.exposedName);
            GUI.enabled = true;
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
