using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [CustomEditor(typeof(Volume))]
    sealed class VolumeEditor : Editor
    {
        SerializedProperty m_IsGlobal;
        SerializedProperty m_BlendRadius;
        SerializedProperty m_Weight;
        SerializedProperty m_Priority;
        SerializedProperty m_Profile;

        VolumeComponentListEditor m_ComponentList;

        Volume actualTarget
        {
            get { return target as Volume; }
        }

        void OnEnable()
        {
            var o = new PropertyFetcher<Volume>(serializedObject);
            m_IsGlobal = o.Find(x => x.isGlobal);
            m_BlendRadius = o.Find(x => x.blendDistance);
            m_Weight = o.Find(x => x.weight);
            m_Priority = o.Find(x => x.priority);
            m_Profile = o.Find(x => x.sharedProfile);

            m_ComponentList = new VolumeComponentListEditor(this);
            RefreshEffectListEditor(actualTarget.sharedProfile);
        }

        void OnDisable()
        {
            if (m_ComponentList != null)
                m_ComponentList.Clear();
        }

        void RefreshEffectListEditor(VolumeProfile asset)
        {
            m_ComponentList.Clear();

            if (asset != null)
                m_ComponentList.Init(asset, new SerializedObject(asset));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_IsGlobal);

            if (!m_IsGlobal.boolValue) // Blend radius is not needed for global volumes
            {
                EditorGUILayout.PropertyField(m_BlendRadius);
                m_BlendRadius.floatValue = Mathf.Max(m_BlendRadius.floatValue, 0f);
            }

            EditorGUILayout.PropertyField(m_Weight);
            EditorGUILayout.PropertyField(m_Priority);

            bool assetHasChanged = false;
            bool showCopy = m_Profile.objectReferenceValue != null;
            bool multiEdit = m_Profile.hasMultipleDifferentValues;

            // The layout system breaks alignement when mixing inspector fields with custom layouted
            // fields, do the layout manually instead
            int buttonWidth = showCopy ? 45 : 60;
            float indentOffset = EditorGUI.indentLevel * 15f;
            var lineRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
            var fieldRect = new Rect(labelRect.xMax, lineRect.y, lineRect.width - labelRect.width - buttonWidth * (showCopy ? 2 : 1), lineRect.height);
            var buttonNewRect = new Rect(fieldRect.xMax, lineRect.y, buttonWidth, lineRect.height);
            var buttonCopyRect = new Rect(buttonNewRect.xMax, lineRect.y, buttonWidth, lineRect.height);

            EditorGUI.PrefixLabel(labelRect, CoreEditorUtils.GetContent("Profile|A reference to a profile asset."));

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.BeginProperty(fieldRect, GUIContent.none, m_Profile);

                var profile = (VolumeProfile)EditorGUI.ObjectField(fieldRect, m_Profile.objectReferenceValue, typeof(VolumeProfile), false);

                if (scope.changed)
                {
                    assetHasChanged = true;
                    m_Profile.objectReferenceValue = profile;
                }

                EditorGUI.EndProperty();
            }

            using (new EditorGUI.DisabledScope(multiEdit))
            {
                if (GUI.Button(buttonNewRect, CoreEditorUtils.GetContent("New|Create a new profile."), showCopy ? EditorStyles.miniButtonLeft : EditorStyles.miniButton))
                {
                    // By default, try to put assets in a folder next to the currently active
                    // scene file. If the user isn't a scene, put them in root instead.
                    var targetName = actualTarget.name;
                    var scene = actualTarget.gameObject.scene;
                    var asset = VolumeProfileFactory.CreateVolumeProfile(scene, targetName);
                    m_Profile.objectReferenceValue = asset;
                    assetHasChanged = true;
                }

                if (showCopy && GUI.Button(buttonCopyRect, CoreEditorUtils.GetContent("Clone|Create a new profile and copy the content of the currently assigned profile."), EditorStyles.miniButtonRight))
                {
                    // Duplicate the currently assigned profile and save it as a new profile
                    var origin = (VolumeProfile)m_Profile.objectReferenceValue;
                    var path = AssetDatabase.GetAssetPath(origin);
                    path = AssetDatabase.GenerateUniqueAssetPath(path);

                    var asset = Instantiate(origin);
                    asset.components.Clear();
                    AssetDatabase.CreateAsset(asset, path);

                    foreach (var item in origin.components)
                    {
                        var itemCopy = Instantiate(item);
                        itemCopy.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                        itemCopy.name = item.name;
                        asset.components.Add(itemCopy);
                        AssetDatabase.AddObjectToAsset(itemCopy, asset);
                    }

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    m_Profile.objectReferenceValue = asset;
                    assetHasChanged = true;
                }
            }

            EditorGUILayout.Space();

            if (m_Profile.objectReferenceValue == null)
            {
                if (assetHasChanged)
                    m_ComponentList.Clear(); // Asset wasn't null before, do some cleanup
            }
            else
            {
                if (assetHasChanged)
                    RefreshEffectListEditor((VolumeProfile)m_Profile.objectReferenceValue);

                if (!multiEdit)
                {
                    m_ComponentList.OnGUI();
                    EditorGUILayout.Space();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
