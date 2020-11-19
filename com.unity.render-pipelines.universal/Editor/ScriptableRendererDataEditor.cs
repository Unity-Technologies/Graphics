using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScriptableRendererData), true)]
    [MovedFrom("UnityEditor.Rendering.LWRP")] public class ScriptableRendererDataEditor : Editor
    {
        class Styles
        {
            public static readonly GUIContent RenderFeatures =
                new GUIContent("Renderer Features",
                    "Features to include in this renderer.\nTo add or remove features, use the plus and minus at the bottom of this box.");

            public static readonly GUIContent PassNameField =
                new GUIContent("Name", "Render pass name. This name is the name displayed in Frame Debugger.");

            public static readonly GUIContent MissingFeature = new GUIContent("Missing RendererFeature",
                "Missing reference, due to compilation issues or missing files. you can attempt auto fix or choose to remove the feature.");

            public static GUIStyle BoldLabelSimple;

            public static GUIContent renderPipelineAssetsText = EditorGUIUtility.TrTextContent("Assigned to these Render Pipeline Assets", "This Renderer Data has been assigned to these Renderer Pipeline Assets.");
            static Styles()
            {
                BoldLabelSimple = new GUIStyle(EditorStyles.label);
                BoldLabelSimple.fontStyle = FontStyle.Bold;
            }
        }

        // Temporary saved bools for foldout header
        SavedBool m_RenderPipelineAssetsFoldout;

        List<ValueTuple<string, string>> m_RenderPipeLineAssets;
        List<string> m_RenderPipelineAssetNames;
        private SerializedProperty m_RendererFeatures;
        private SerializedProperty m_RendererFeaturesMap;
        private SerializedProperty m_FalseBool;
        [SerializeField] private bool falseBool = false;
        List<Editor> m_Editors = new List<Editor>();

        List<UniversalRenderPipelineAsset> rpAssets;

        //static int mField = 0;
        string[] options = {};
        private void OnEnable()
        {
            m_RendererFeatures = serializedObject.FindProperty(nameof(ScriptableRendererData.m_RendererFeatures));
            m_RendererFeaturesMap = serializedObject.FindProperty(nameof(ScriptableRendererData.m_RendererFeatureMap));
            var editorObj = new SerializedObject(this);
            m_FalseBool = editorObj.FindProperty(nameof(falseBool));
            UpdateEditorList();
            m_RenderPipelineAssetsFoldout = new SavedBool($"{target.GetType()}.RenderPipelineAssetsFoldout", true);
            m_RenderPipeLineAssets = new List<ValueTuple<string,string>>();
            m_RenderPipelineAssetNames = new List<string>();
            rpAssets = GetAllUniversalRenderPipelineAssets();
            FindAssignedRenderPipelineAssets();

            //options = new string[]{};
            //FindAssignedRenderPipelineAssets();
        }

        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
            // New button in header to assign asset
            Rect fullRect = EditorGUILayout.GetControlRect();
            float titleHeight = EditorGUIUtility.singleLineHeight + 5;
            Rect titleRect = new Rect(fullRect.x, fullRect.y, fullRect.width, titleHeight);
            if (GUI.Button(titleRect, $"Assign to Renderer List of {GraphicsSettings.defaultRenderPipeline.name}"))
            {
                UniversalRenderPipelineAsset rp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
                rp?.AddRendererToRendererDataList(target as ScriptableRendererData);
                FindAssignedRenderPipelineAssets();
            }
        }

        List<UniversalRenderPipelineAsset> GetAllUniversalRenderPipelineAssets()
        {
            List<UniversalRenderPipelineAsset> rpaList = new List<UniversalRenderPipelineAsset>();
            var rpAssets = AssetDatabase.FindAssets("t:RenderPipelineAsset");
            foreach (string asset in rpAssets)
            {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path) as UniversalRenderPipelineAsset;
                if (urpAsset != null)
                {
                    rpaList.Add(urpAsset);
                }
            }

            return rpaList;
        }

        public void FindAssignedRenderPipelineAssets()
        {
            // m_RenderPipeLineAssets.Clear();
            // var rpAssets = AssetDatabase.FindAssets("t:RenderPipelineAsset");
            // foreach (string asset in rpAssets)
            // {
            //     var path = AssetDatabase.GUIDToAssetPath(asset);
            //     UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path) as UniversalRenderPipelineAsset;
            //     if (urpAsset != null)
            //     {
            //         var renderers = urpAsset.RendererDataList;
            //         foreach(var renderer in renderers)
            //         {
            //             if (target == renderer)
            //             {
            //                 m_RenderPipeLineAssets.Add((urpAsset.name, path));
            //             }
            //         }
            //     }
            // }

            m_RenderPipeLineAssets.Clear();
            //var rpAssets = GetAllUniversalRenderPipelineAssets();
            foreach (var asset in rpAssets)
            {
                var path = AssetDatabase.GetAssetPath(asset);
                var renderers = asset.RendererDataList;
                foreach (var renderer in renderers)
                {
                    if (target == renderer)
                    {
                        m_RenderPipeLineAssets.Add((asset.name, path));
                        m_RenderPipelineAssetNames.Add(asset.name);
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (m_RendererFeatures == null)
                OnEnable();
            else if (m_RendererFeatures.arraySize != m_Editors.Count)
                UpdateEditorList();

            serializedObject.Update();
            DrawRendererFeatureList();
            DrawRenderPipelineAssetList();
        }

        // Create the bit mask from the rp assets rendererdatalist
        // show the drop down.
        // Update the correct rendererdatalist

        void DrawRenderPipelineAssetList()
        {
            // Foldout header
            m_RenderPipelineAssetsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_RenderPipelineAssetsFoldout.value, Styles.renderPipelineAssetsText);
            if (m_RenderPipelineAssetsFoldout.value)
            {
                foreach ((string, string) renderPipeLineAsset in m_RenderPipeLineAssets)
                {
                    if(GUILayout.Button(renderPipeLineAsset.Item1, "Label"))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<Object>(renderPipeLineAsset.Item2);
                        EditorGUIUtility.PingObject(asset);
                    }
                }
            }

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {

                List<string> names = new List<string>();
                //List<UniversalRenderPipelineAsset> rpAssets = GetAllUniversalRenderPipelineAssets();
                foreach (var asset in rpAssets)
                {
                    names.Add(asset.name);
                }


                // // LayerMask needs to be converted to be used in a MaskField...
                // int field = 0;
                // for (int c = 0; c < names.Count; c++)
                //     if ((mask & (1 << LayerMask.NameToLayer(names[c]))) != 0)
                //         field |= 1 << c;

                // This needs to be on initialization
                int field = 0;
                for (int c = 0; c < names.Count; c++)
                    if (m_RenderPipelineAssetNames.Contains(names[c]))
                        field |= 1 << c;

                options = names.ToArray();
                var mask = new BitVector32(field);
                //mField = EditorGUILayout.MaskField("Render Pipeline Assets", mField, options);


                var newMask = new BitVector32(EditorGUILayout.MaskField("Render Pipeline Assets", mask.Data, options));


                if (changeCheckScope.changed)
                {
                    //AssignRendererToAsset()
                    Debug.Log(newMask);
                    UpdateRendererDataLists(newMask);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void UpdateRendererDataLists(BitVector32 mask)
        {
            for (int c = 0; c < options.Length; c++)
            {
                if ((mask.Data & (1 << c)) != 0)
                {
                    Debug.Log(options[c]);
                    // Assign the renderer to the data list
                    AssignRendererToRPAsset(options[c]);

                }
            }

            FindAssignedRenderPipelineAssets();
        }

        void AssignRendererToRPAsset(string rpName)
        {

            foreach (var asset in rpAssets)
            {
                if (asset.name == rpName)
                {
                    Debug.Log("Adding");
                    asset.AddRendererToRendererDataList(target as ScriptableRendererData);
                    return;
                }
            }
        }

        private void DrawRendererFeatureList()
        {
            EditorGUILayout.LabelField(Styles.RenderFeatures, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (m_RendererFeatures.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No Renderer Features added", MessageType.Info);
            }
            else
            {
                //Draw List
                CoreEditorUtils.DrawSplitter();
                for (int i = 0; i < m_RendererFeatures.arraySize; i++)
                {
                    SerializedProperty renderFeaturesProperty = m_RendererFeatures.GetArrayElementAtIndex(i);
                    DrawRendererFeature(i, ref renderFeaturesProperty);
                    CoreEditorUtils.DrawSplitter();
                }
            }
            EditorGUILayout.Space();

            //Add renderer
            if (GUILayout.Button("Add Renderer Feature", EditorStyles.miniButton))
            {
                AddPassMenu();
            }
        }

        private void DrawRendererFeature(int index, ref SerializedProperty renderFeatureProperty)
        {
            Object rendererFeatureObjRef = renderFeatureProperty.objectReferenceValue;
            if (rendererFeatureObjRef != null)
            {
                bool hasChangedProperties = false;
                string title = ObjectNames.GetInspectorTitle(rendererFeatureObjRef);

                // Get the serialized object for the editor script & update it
                Editor rendererFeatureEditor = m_Editors[index];
                SerializedObject serializedRendererFeaturesEditor = rendererFeatureEditor.serializedObject;
                serializedRendererFeaturesEditor.Update();

                // Foldout header
                EditorGUI.BeginChangeCheck();
                SerializedProperty activeProperty = serializedRendererFeaturesEditor.FindProperty("m_Active");
                bool displayContent = CoreEditorUtils.DrawHeaderToggle(title, renderFeatureProperty, activeProperty, pos => OnContextClick(pos, index));
                hasChangedProperties |= EditorGUI.EndChangeCheck();

                // ObjectEditor
                if (displayContent)
                {
                    EditorGUI.BeginChangeCheck();
                    SerializedProperty nameProperty = serializedRendererFeaturesEditor.FindProperty("m_Name");
                    nameProperty.stringValue = ValidateName(EditorGUILayout.DelayedTextField(Styles.PassNameField, nameProperty.stringValue));
                    if (EditorGUI.EndChangeCheck())
                    {
                        hasChangedProperties = true;

                        // We need to update sub-asset name
                        rendererFeatureObjRef.name = nameProperty.stringValue;
                        AssetDatabase.SaveAssets();

                        // Triggers update for sub-asset name change
                        ProjectWindowUtil.ShowCreatedAsset(target);
                    }

                    EditorGUI.BeginChangeCheck();
                    rendererFeatureEditor.OnInspectorGUI();
                    hasChangedProperties |= EditorGUI.EndChangeCheck();

                    EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
                }

                // Apply changes and save if the user has modified any settings
                if (hasChangedProperties)
                {
                    serializedRendererFeaturesEditor.ApplyModifiedProperties();
                    serializedObject.ApplyModifiedProperties();
                    ForceSave();
                }
            }
            else
            {
                CoreEditorUtils.DrawHeaderToggle(Styles.MissingFeature,renderFeatureProperty, m_FalseBool,pos => OnContextClick(pos, index));
                m_FalseBool.boolValue = false; // always make sure false bool is false
                EditorGUILayout.HelpBox(Styles.MissingFeature.tooltip, MessageType.Error);
                if (GUILayout.Button("Attempt Fix", EditorStyles.miniButton))
                {
                    ScriptableRendererData data = target as ScriptableRendererData;
                    data.ValidateRendererFeatures();
                }
            }
        }

        private void OnContextClick(Vector2 position, int id)
        {
            var menu = new GenericMenu();

            if (id == 0)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Up"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Up"), false, () => MoveComponent(id, -1));

            if (id == m_RendererFeatures.arraySize - 1)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("Move Down"));
            else
                menu.AddItem(EditorGUIUtility.TrTextContent("Move Down"), false, () => MoveComponent(id, 1));

            menu.AddSeparator(string.Empty);
            menu.AddItem(EditorGUIUtility.TrTextContent("Remove"), false, () => RemoveComponent(id));

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        private void AddPassMenu()
        {
            GenericMenu menu = new GenericMenu();
            TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<ScriptableRendererFeature>();
            foreach (Type type in types)
            {
                var data = target as ScriptableRendererData;
                if (data.DuplicateFeatureCheck(type))
                {
                    continue;
                }

                string path = GetMenuNameFromType(type);
                menu.AddItem(new GUIContent(path), false, AddComponent, type.Name);
            }
            menu.ShowAsContext();
        }

        private void AddComponent(object type)
        {
            serializedObject.Update();

            ScriptableObject component = CreateInstance((string)type);
            component.name = $"New{(string)type}";
            Undo.RegisterCreatedObjectUndo(component, "Add Renderer Feature");

            // Store this new effect as a sub-asset so we can reference it safely afterwards
            // Only when we're not dealing with an instantiated asset
            if (EditorUtility.IsPersistent(target))
            {
                AssetDatabase.AddObjectToAsset(component, target);
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out var guid, out long localId);

            // Grow the list first, then add - that's how serialized lists work in Unity
            m_RendererFeatures.arraySize++;
            SerializedProperty componentProp = m_RendererFeatures.GetArrayElementAtIndex(m_RendererFeatures.arraySize - 1);
            componentProp.objectReferenceValue = component;

            // Update GUID Map
            m_RendererFeaturesMap.arraySize++;
            SerializedProperty guidProp = m_RendererFeaturesMap.GetArrayElementAtIndex(m_RendererFeaturesMap.arraySize - 1);
            guidProp.longValue = localId;
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Force save / refresh
            if (EditorUtility.IsPersistent(target))
            {
                ForceSave();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveComponent(int id)
        {
            SerializedProperty property = m_RendererFeatures.GetArrayElementAtIndex(id);
            Object component = property.objectReferenceValue;
            property.objectReferenceValue = null;

            Undo.SetCurrentGroupName(component == null ? "Remove Renderer Feature" : $"Remove {component.name}");

            // remove the array index itself from the list
            m_RendererFeatures.DeleteArrayElementAtIndex(id);
            m_RendererFeaturesMap.DeleteArrayElementAtIndex(id);
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Destroy the setting object after ApplyModifiedProperties(). If we do it before, redo
            // actions will be in the wrong order and the reference to the setting object in the
            // list will be lost.
            if (component != null)
            {
                Undo.DestroyObjectImmediate(component);
            }

            // Force save / refresh
            ForceSave();
        }

        private void MoveComponent(int id, int offset)
        {
            Undo.SetCurrentGroupName("Move Render Feature");
            serializedObject.Update();
            m_RendererFeatures.MoveArrayElement(id, id + offset);
            m_RendererFeaturesMap.MoveArrayElement(id, id + offset);
            UpdateEditorList();
            serializedObject.ApplyModifiedProperties();

            // Force save / refresh
            ForceSave();
        }

        private string GetMenuNameFromType(Type type)
        {
            var path = type.Name;
            if (type.Namespace != null)
            {
                if (type.Namespace.Contains("Experimental"))
                    path += " (Experimental)";
            }

            // Inserts blank space in between camel case strings
            return Regex.Replace(Regex.Replace(path, "([a-z])([A-Z])", "$1 $2", RegexOptions.Compiled),
                "([A-Z])([A-Z][a-z])", "$1 $2", RegexOptions.Compiled);
        }

        private string ValidateName(string name)
        {
            name = Regex.Replace(name, @"[^a-zA-Z0-9 ]", "");
            return name;
        }

        private void UpdateEditorList()
        {
            m_Editors.Clear();
            for (int i = 0; i < m_RendererFeatures.arraySize; i++)
            {
                m_Editors.Add(CreateEditor(m_RendererFeatures.GetArrayElementAtIndex(i).objectReferenceValue));
            }
        }

        private void ForceSave()
        {
            EditorUtility.SetDirty(target);
        }
    }
}
