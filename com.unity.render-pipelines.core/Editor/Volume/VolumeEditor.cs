using System.Collections.Generic;
using UnityEngine;
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

        static List<Collider> s_RecycledColliderList = new List<Collider>();
        const string k_DisplayGizmoMenuItem = "Edit/Render Pipeline/Display Selected Volume Gizmos #&v";
        static SavedBool s_SavedState = new SavedBool("VolumeEditor.DisplayGizmo", true);

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
                isGlobal = EditorGUILayout.Popup(label, isGlobal, m_Modes);
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
                    RefreshEffectListEditor(profileRef);

                if (!multiEdit)
                {
                    m_ComponentList.OnGUI();
                    EditorGUILayout.Space();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        // TODO: Look into a better & more robust volume pre-visualization system
        [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.Selected)]
        static void OnDrawSelectedGizmo(Volume volume, GizmoType gizmoType)
        {
            if (volume.isGlobal || !s_SavedState.value)
                return;

            var colliders = s_RecycledColliderList;
            volume.GetComponents(colliders);

            if (colliders == null)
                return;

            var t = volume.transform;
            var scale = t.lossyScale;
            var invScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, scale);
            Gizmos.color = CoreRenderPipelinePreferences.volumeGizmoColor;

            // Dim the skin color a bit
            var skinColor = Gizmos.color * 0.65f;

            // Draw a separate gizmo for each collider
            foreach (var collider in colliders)
            {
                if (!collider.enabled)
                    continue;

                // We'll just use scaling as an approximation for volume skin. It's far from being
                // correct (and is completely wrong in some cases). Ultimately we'd use a distance
                // field or at least a tesselate + push modifier on the collider's mesh to get a
                // better approximation, but the current Gizmo system is a bit limited and because
                // everything is dynamic in Unity and can be changed at anytime, it's hard to keep
                // track of changes in an elegant way (which we'd need to implement a nice cache
                // system for generated volume meshes).
                switch (collider)
                {
                    case BoxCollider c:
                        Gizmos.DrawCube(c.center, c.size);
                        Gizmos.color = skinColor;
                        Gizmos.DrawCube(c.center, c.size + invScale * volume.blendDistance * 2f);
                        break;
                    case SphereCollider c:
                        // For sphere the only scale that is used is the transform.x
                        Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, Vector3.one * scale.x);
                        Gizmos.DrawSphere(c.center, c.radius);
                        Gizmos.color = skinColor;
                        Gizmos.DrawSphere(c.center, c.radius + invScale.x * volume.blendDistance);
                        break;
                    case MeshCollider c:
                        // Only convex mesh m_Colliders are allowed
                        if (!c.convex)
                            c.convex = true;

                        // Mesh pivot should be centered or this won't work
                        Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, scale);
                        Gizmos.DrawMesh(c.sharedMesh);
                        Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, scale + invScale * volume.blendDistance * 2f);
                        Gizmos.color = skinColor;
                        Gizmos.DrawMesh(c.sharedMesh);
                        break;
                    default:
                        // Nothing for capsule (DrawCapsule isn't exposed in Gizmo), terrain, wheel
                        // and other m_Colliders...
                        break;
                }
            }

            colliders.Clear();
        }

        [MenuItem(k_DisplayGizmoMenuItem, false, CoreUtils.editMenuPriority4)]
        static void MenuItem() => s_SavedState.value = !s_SavedState.value;

        [MenuItem(k_DisplayGizmoMenuItem, true, CoreUtils.editMenuPriority4)]
        static bool MenuItemValidate()
        {
            Menu.SetChecked(k_DisplayGizmoMenuItem, s_SavedState.value);
            return true;
        }
    }
}
