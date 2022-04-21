using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
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

        Volume actualTarget => target as Volume;

        VolumeProfile profileRef => actualTarget.HasInstantiatedProfile() ? actualTarget.profile : actualTarget.sharedProfile;

        readonly GUIContent[] m_Modes = { new GUIContent("Global"), new GUIContent("Local") };

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
            m_ComponentList?.Clear();
        }

        void RefreshEffectListEditor(VolumeProfile asset)
        {
            m_ComponentList.Clear();

            asset?.Sanitize();

            if (asset != null)
                m_ComponentList.Init(asset, new SerializedObject(asset));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUIContent label = EditorGUIUtility.TrTextContent("Mode", "A global volume is applied to the whole scene.");
            Rect lineRect = EditorGUILayout.GetControlRect();
            int isGlobal = m_IsGlobal.boolValue ? 0 : 1;
            EditorGUI.BeginProperty(lineRect, label, m_IsGlobal);
            {
                EditorGUI.BeginChangeCheck();
                isGlobal = EditorGUI.Popup(lineRect, label, isGlobal, m_Modes);
                if (EditorGUI.EndChangeCheck())
                    m_IsGlobal.boolValue = isGlobal == 0;
            }
            EditorGUI.EndProperty();

            if (isGlobal != 0) // Blend radius is not needed for global volumes
            {
                if (!actualTarget.TryGetComponent<Collider>(out _))
                {
                    EditorGUILayout.HelpBox("Add a Collider to this GameObject to set boundaries for the local Volume.", MessageType.Info);

                    if (GUILayout.Button(EditorGUIUtility.TrTextContent("Add Collider"), EditorStyles.miniButton))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(EditorGUIUtility.TrTextContent("Box"), false, () => Undo.AddComponent<BoxCollider>(actualTarget.gameObject));
                        menu.AddItem(EditorGUIUtility.TrTextContent("Sphere"), false, () => Undo.AddComponent<SphereCollider>(actualTarget.gameObject));
                        menu.AddItem(EditorGUIUtility.TrTextContent("Capsule"), false, () => Undo.AddComponent<CapsuleCollider>(actualTarget.gameObject));
                        menu.AddItem(EditorGUIUtility.TrTextContent("Mesh"), false, () => Undo.AddComponent<MeshCollider>(actualTarget.gameObject));
                        menu.ShowAsContext();
                    }
                }

                EditorGUILayout.PropertyField(m_BlendRadius);
                m_BlendRadius.floatValue = Mathf.Max(m_BlendRadius.floatValue, 0f);
            }

            EditorGUILayout.PropertyField(m_Weight);
            EditorGUILayout.PropertyField(m_Priority);

            bool assetHasChanged = false;
            bool showCopy = m_Profile.objectReferenceValue != null;
            bool multiEdit = m_Profile.hasMultipleDifferentValues;

            // The layout system breaks alignment when mixing inspector fields with custom layout'd
            // fields, do the layout manually instead
            int buttonWidth = showCopy ? 45 : 60;
            float indentOffset = EditorGUI.indentLevel * 15f;
            lineRect = EditorGUILayout.GetControlRect();
            var labelRect = new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth - indentOffset, lineRect.height);
            var fieldRect = new Rect(labelRect.xMax, lineRect.y, lineRect.width - labelRect.width - buttonWidth * (showCopy ? 2 : 1), lineRect.height);
            var buttonNewRect = new Rect(fieldRect.xMax, lineRect.y, buttonWidth, lineRect.height);
            var buttonCopyRect = new Rect(buttonNewRect.xMax, lineRect.y, buttonWidth, lineRect.height);

            GUIContent guiContent;
            if (actualTarget.HasInstantiatedProfile())
                guiContent = EditorGUIUtility.TrTextContent("Profile (Instance)", "A copy of a profile asset.");
            else
                guiContent = EditorGUIUtility.TrTextContent("Profile", "A reference to a profile asset.");
            EditorGUI.PrefixLabel(labelRect, guiContent);

            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.BeginProperty(fieldRect, GUIContent.none, m_Profile);

                VolumeProfile profile;

                if (actualTarget.HasInstantiatedProfile())
                    profile = (VolumeProfile)EditorGUI.ObjectField(fieldRect, actualTarget.profile, typeof(VolumeProfile), false);
                else
                    profile = (VolumeProfile)EditorGUI.ObjectField(fieldRect, m_Profile.objectReferenceValue, typeof(VolumeProfile), false);

                if (scope.changed)
                {
                    assetHasChanged = true;
                    m_Profile.objectReferenceValue = profile;

                    if (actualTarget.HasInstantiatedProfile()) // Clear the instantiated profile, from now on we're using shared again
                        actualTarget.profile = null;
                }

                EditorGUI.EndProperty();
            }

            using (new EditorGUI.DisabledScope(multiEdit))
            {
                if (GUI.Button(buttonNewRect, EditorGUIUtility.TrTextContent("New", "Create a new profile."), showCopy ? EditorStyles.miniButtonLeft : EditorStyles.miniButton))
                {
                    // By default, try to put assets in a folder next to the currently active
                    // scene file. If the user isn't a scene, put them in root instead.
                    var targetName = actualTarget.name;
                    var scene = actualTarget.gameObject.scene;
                    var asset = VolumeProfileFactory.CreateVolumeProfile(scene, targetName);
                    m_Profile.objectReferenceValue = asset;
                    actualTarget.profile = null; // Make sure we're not using an instantiated profile anymore
                    assetHasChanged = true;
                }

                if (actualTarget.HasInstantiatedProfile())
                    guiContent = EditorGUIUtility.TrTextContent("Save", "Save the instantiated profile");
                else
                    guiContent = EditorGUIUtility.TrTextContent("Clone", "Create a new profile and copy the content of the currently assigned profile.");
                if (showCopy && GUI.Button(buttonCopyRect, guiContent, EditorStyles.miniButtonRight))
                {
                    // Duplicate the currently assigned profile and save it as a new profile
                    var origin = profileRef;
                    var path = AssetDatabase.GetAssetPath(m_Profile.objectReferenceValue);

                    path = IsAssetInReadOnlyPackage(path)
                        // We may be in a read only package, in that case we need to clone the volume profile in an
                        // editable area, such as the root of the project.
                        ? AssetDatabase.GenerateUniqueAssetPath(Path.Combine("Assets", Path.GetFileName(path)))
                        // Otherwise, duplicate next to original asset.
                        : AssetDatabase.GenerateUniqueAssetPath(path);

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
                    actualTarget.profile = null; // Make sure we're not using an instantiated profile anymore
                    assetHasChanged = true;
                }
            }

            EditorGUILayout.Space();

            if (m_Profile.objectReferenceValue == null && !actualTarget.HasInstantiatedProfile())
            {
                if (assetHasChanged)
                    m_ComponentList.Clear(); // Asset wasn't null before, do some cleanup
            }
            else
            {
                if (assetHasChanged || profileRef != m_ComponentList.asset)
                {
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                    RefreshEffectListEditor(profileRef);
                }

                if (!multiEdit)
                {
                    m_ComponentList.OnGUI();
                    EditorGUILayout.Space();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        static bool IsAssetInReadOnlyPackage(string path)
        {
            Assert.IsNotNull(path);
            var info = PackageManager.PackageInfo.FindForAssetPath(path);
            return info != null && (info.source != PackageSource.Local && info.source != PackageSource.Embedded);
        }
    }
}
