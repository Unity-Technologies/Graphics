using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;

using Object = UnityEngine.Object;
using UnityEditorInternal;
using System.Reflection;

[CustomEditor(typeof(VFXModel), true)]
[CanEditMultipleObjects]
public class VFXSlotContainerEditor : Editor
{
    protected void OnEnable()
    {
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    protected void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
    }

    public virtual void DoInspectorGUI()
    {
        var slotContainer = targets[0] as VFXModel;
        IEnumerable<FieldInfo> settingFields = slotContainer.GetSettings(false, VFXSettingAttribute.VisibleFlags.InInspector);

        for (int i = 1; i < targets.Length; ++i)
        {
            IEnumerable<FieldInfo> otherSettingFields = (targets[i] as VFXModel).GetSettings(false, VFXSettingAttribute.VisibleFlags.InInspector);

            settingFields = settingFields.Intersect(otherSettingFields);
        }

        foreach (var prop in settingFields.Select(t => new KeyValuePair<FieldInfo, SerializedProperty>(t, serializedObject.FindProperty(t.Name))).Where(t => t.Value != null))
        {
            var attrs = prop.Key.GetCustomAttributes(typeof(StringProviderAttribute), true);
            if (attrs.Length > 0)
            {
                var strings = StringPropertyRM.FindStringProvider(attrs)();

                int selected = prop.Value.hasMultipleDifferentValues ? -1 : System.Array.IndexOf(strings, prop.Value.stringValue);
                int result = EditorGUILayout.Popup(ObjectNames.NicifyVariableName(prop.Value.name), selected, strings);
                if (result != selected)
                {
                    prop.Value.stringValue = strings[result];
                }
            }
            else
            {
                bool visibleChildren = EditorGUILayout.PropertyField(prop.Value);
                if (visibleChildren)
                {
                    SerializedProperty childProp = prop.Value.Copy();
                    while (childProp != null && childProp.NextVisible(visibleChildren) && childProp.propertyPath.StartsWith(prop.Value.propertyPath + "."))
                    {
                        visibleChildren = EditorGUILayout.PropertyField(childProp);
                    }
                }
            }
        }
    }

    void OnSceneGUI(SceneView sv)
    {
        try // make sure we don't break the whole scene
        {
            var slotContainer = targets[0] as VFXModel;
            if (VFXViewWindow.currentWindow != null)
            {
                VFXView view = VFXViewWindow.currentWindow.graphView;
                if (view.controller != null && view.controller.graph == slotContainer.GetGraph())
                {
                    if (slotContainer is VFXParameter)
                    {
                        var controller = view.controller.GetParameterController(slotContainer as VFXParameter);

                        controller.DrawGizmos(view.attachedComponent);
                    }
                    else
                    {
                        var controller = view.controller.GetNodeController(slotContainer, 0);
                        if (controller != null)
                            controller.DrawGizmos(view.attachedComponent);
                    }
                }
            }
        }
        catch(System.Exception e )
        {
            Debug.LogException(e);
        }
        finally
        {
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DoInspectorGUI();

        if (serializedObject.ApplyModifiedProperties())
        {
            foreach (VFXModel context in targets.OfType<VFXModel>())
            {
                // notify that something changed.
                context.Invalidate(VFXModel.InvalidationCause.kSettingChanged);
            }
        }
    }
}
