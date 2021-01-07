using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditorInternal;

namespace UnityEditor.Rendering
{
    class AdditionalPropertiesPreferences
    {
        class Styles
        {
            public static readonly GUIContent additionalPropertiesLabel = new GUIContent("Additional Properties", "Toggle all additional properties to either visible or hidden.");
            public static readonly GUIContent[] additionalPropertiesNames = { new GUIContent("All Visible"), new GUIContent("All Hidden") };
            public static readonly int[] additionalPropertiesValues = { 1, 0 };
        }

        static List<Type>           s_VolumeComponentEditorTypes;
        static List<(Type, Type)>   s_IAdditionalPropertiesBoolFlagsHandlerTypes; // (EditorType, ObjectType)
        static bool                 s_ShowAllAdditionalProperties = false;
        static Scene                s_DummyScene;

        static AdditionalPropertiesPreferences()
        {
            s_ShowAllAdditionalProperties = EditorPrefs.GetBool(Keys.showAllAdditionalProperties);
            s_DummyScene = EditorSceneManager.NewPreviewScene();
        }

        static void InitializeIfNeeded()
        {
            if (s_VolumeComponentEditorTypes == null)
            {
                s_VolumeComponentEditorTypes = TypeCache.GetTypesDerivedFrom<VolumeComponentEditor>()
                    .Where(
                        t => !t.IsAbstract
                    ).ToList();
            }

            if (s_IAdditionalPropertiesBoolFlagsHandlerTypes == null)
            {
                s_IAdditionalPropertiesBoolFlagsHandlerTypes = new List<(Type, Type)>();

                var typeList = TypeCache.GetTypesDerivedFrom<IAdditionalPropertiesBoolFlagsHandler>()
                    .Where(
                        t => !t.IsAbstract && typeof(Editor).IsAssignableFrom(t)
                    );

                foreach (var editorType in typeList)
                {
                    var customEditorAttributes = editorType.GetCustomAttributes(typeof(CustomEditorForRenderPipelineAttribute), true);
                    if (customEditorAttributes.Length != 0)
                    {
                        Type objectType = (Type)typeof(CustomEditor).GetField("m_InspectedType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(customEditorAttributes[0]);
                        s_IAdditionalPropertiesBoolFlagsHandlerTypes.Add((editorType, objectType));
                    }
                }
            }
        }

        static bool showAllAdditionalProperties
        {
            get => s_ShowAllAdditionalProperties;
            set
            {
                s_ShowAllAdditionalProperties = value;
                EditorPrefs.SetBool(Keys.showAllAdditionalProperties, s_ShowAllAdditionalProperties);

                ShowAllAdditionalProperties(showAllAdditionalProperties);
            }
        }

        static class Keys
        {
            internal const string showAllAdditionalProperties = "General.ShowAllAdditionalProperties";
        }

        internal static void PreferenceGUI()
        {
            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUILayout.IntPopup(Styles.additionalPropertiesLabel, showAllAdditionalProperties ? 1 : 0, Styles.additionalPropertiesNames, Styles.additionalPropertiesValues);
            if (EditorGUI.EndChangeCheck())
            {
                showAllAdditionalProperties = newValue == 1 ? true : false;
            }
        }

        static void ShowAllAdditionalProperties(bool value)
        {
            // The way we do this here is to gather all types of either VolumeComponentEditor or IAdditionalPropertiesBoolFlagsHandler (for regular components)
            // then we instantiate those classes in order to be able to call the relevant function to update the "ShowAdditionalProperties" flags.
            // The instance on which we call is not important because in the end it will only change a global editor preference.
            InitializeIfNeeded();

            // Volume components
            foreach (var editorType in s_VolumeComponentEditorTypes)
            {
                var editor = Activator.CreateInstance(editorType) as VolumeComponentEditor;
                editor.InitAdditionalPropertiesPreference();
                editor.SetAdditionalPropertiesPreference(value);
            }


            // Regular components
            // The code here is a bit messy because to instantiate an editor we need to have a valid reference to an object.
            // So we need to create a dummy game object and add the relevant component to it in order to instantiate the corresponding editor.
            // We also need to move it to a dummy preview scene in order not to dirty the main scene.
            var dummyGameObject = new GameObject("DummyAdditionalProperties");
            dummyGameObject.hideFlags = HideFlags.HideAndDontSave;
            SceneManager.MoveGameObjectToScene(dummyGameObject, s_DummyScene);

            foreach (var editorTypes in s_IAdditionalPropertiesBoolFlagsHandlerTypes)
            {
                var instance = dummyGameObject.AddComponent(editorTypes.Item2);
                var editor = Editor.CreateEditor(instance, editorTypes.Item1) as IAdditionalPropertiesBoolFlagsHandler;
                editor.ShowAdditionalProperties(value);

                UnityEngine.Object.DestroyImmediate((Editor)editor);
            }

            UnityEngine.Object.DestroyImmediate(dummyGameObject);

            // Force repaint in case some editors are already open.
            InternalEditorUtility.RepaintAllViews();
        }
    }
}
