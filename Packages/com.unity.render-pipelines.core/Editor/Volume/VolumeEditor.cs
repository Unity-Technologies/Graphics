using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Rendering.Analytics;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    [CustomEditor(typeof(Volume))]
    [CanEditMultipleObjects]
    sealed class VolumeEditor : Editor
    {
        const string k_TemplatePath = "Packages/com.unity.render-pipelines.core/Editor/UXML/VolumeEditor.uxml";

        static class Styles
        {
            public static readonly string isGlobalDropdownTooltip = L10n.Tr("Global Volumes affect the Camera wherever the Camera is in the Scene. Local Volumes affect the Camera if they encapsulate the Camera within the bounds of their Collider.");
            public static readonly GUIContent addBoxCollider = EditorGUIUtility.TrTextContent("Add a Box Collider");
            public static readonly GUIContent sphereBoxCollider = EditorGUIUtility.TrTextContent("Add a Sphere Collider");
            public static readonly GUIContent capsuleBoxCollider = EditorGUIUtility.TrTextContent("Add a Capsule Collider");
            public static readonly GUIContent meshBoxCollider = EditorGUIUtility.TrTextContent("Add a Mesh Collider");
            public static readonly GUIContent addColliderFixMessage = EditorGUIUtility.TrTextContentWithIcon("Add a Collider to this GameObject to set boundaries for the local Volume.", CoreEditorStyles.iconWarn);
            public static readonly GUIContent disableColliderFixMessage = EditorGUIUtility.TrTextContentWithIcon("Global Volumes do not need a collider. Disable or remove the collider.", CoreEditorStyles.iconWarn);
            public static readonly GUIContent enableColliderFixMessage = EditorGUIUtility.TrTextContentWithIcon("Local Volumes need a collider enabled. Enable the collider.", CoreEditorStyles.iconWarn);
            public static readonly GUIContent newLabel = EditorGUIUtility.TrTextContent("New", "Create a new profile.");
            public static readonly GUIContent saveLabel = EditorGUIUtility.TrTextContent("Save", "Save the instantiated profile");
            public static readonly GUIContent cloneLabel = EditorGUIUtility.TrTextContent("Clone", "Create a new profile and copy the content of the currently assigned profile.");
            public static readonly GUIContent enableAll = EditorGUIUtility.TrTextContent("Enable All");
            public static readonly GUIContent disableAll = EditorGUIUtility.TrTextContent("Disable All");
            public static readonly GUIContent removeAll = EditorGUIUtility.TrTextContent("Remove All");
        }

        SerializedProperty m_IsGlobal;
        SerializedProperty m_Profile;
        VolumeComponentListEditor m_ComponentList;

        Volume targetVolume => target as Volume;
        VolumeProfile profileRef => targetVolume.HasInstantiatedProfile() ? targetVolume.profile : targetVolume.sharedProfile;

        void OnEnable()
        {
            var o = new PropertyFetcher<Volume>(serializedObject);
            m_IsGlobal = o.Find(x => x.isGlobal);
            m_Profile = o.Find(x => x.sharedProfile);
        }

        void OnDisable()
        {
            m_ComponentList?.Clear();
        }

        public override VisualElement CreateInspectorGUI()
        {
            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath);
            var root = template.Instantiate();

            string ModeIntToString(int modeInt) => modeInt == 1 ? "Global" : "Local";
            var modeList = new List<int> { 0, 1 };
            var isGlobalDropdown = new PopupField<int>("Mode", modeList, m_IsGlobal.boolValue ? 1 : 0, ModeIntToString, ModeIntToString);
            isGlobalDropdown.tooltip = Styles.isGlobalDropdownTooltip;
            isGlobalDropdown.AddToClassList("unity-base-field__aligned");
            isGlobalDropdown.RegisterValueChangedCallback(evt =>
            {
                m_IsGlobal.boolValue = evt.newValue != 0;
                serializedObject.ApplyModifiedProperties();
            });

            // This is required to get notified when the property changes through Undo/Redo
            isGlobalDropdown.TrackPropertyValue(m_IsGlobal, property =>
            {
                isGlobalDropdown.SetValueWithoutNotify(property.boolValue ? 1 : 0);
            });

            // Note: Must insert directly into root at the appropriate index, using a container VisualElement breaks
            //       alignment to other PropertyFields (achieved via the "unity-base-field__aligned" class).
            root.Insert(0, isGlobalDropdown);

            // Fix me boxes
            var blendDistancePropertyField = root.Q<PropertyField>("volume-profile-blend-distance");
            root.Q("collider-fixme-box__container").Add(new IMGUIContainer(() =>
            {
                bool hasCollider = targetVolume.TryGetComponent<Collider>(out var collider);
                if (m_IsGlobal.boolValue) // Blend radius is not needed for global volumes
                {
                    if (hasCollider && collider.enabled)
                        CoreEditorUtils.DrawFixMeBox(Styles.disableColliderFixMessage, () => SetColliderEnabledWithUndo(collider, false));

                    blendDistancePropertyField.style.display = DisplayStyle.None;
                }
                else
                {
                    if (hasCollider)
                    {
                        if (!collider.enabled)
                            CoreEditorUtils.DrawFixMeBox(Styles.enableColliderFixMessage, () => SetColliderEnabledWithUndo(collider, true));
                    }
                    else
                    {
                        CoreEditorUtils.DrawFixMeBox(Styles.addColliderFixMessage, AddColliderWithUndo);
                    }
                    blendDistancePropertyField.style.display = DisplayStyle.Flex;
                }
            }));

            var assetIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Packages/com.unity.render-pipelines.core/Editor/Icons/Processed/d_VolumeProfile Icon.asset");
            root.Q<Image>("volume-profile-header__asset-icon").image = assetIcon;
            root.Q<Image>("volume-profile-objectfield__contextmenu-image").image = CoreEditorStyles.paneOptionsIcon;
            root.Q<Button>(classes: "volume-profile-objectfield__contextmenu").clicked += OnVolumeProfileContextClick;
            root.Q<Button>("volume-profile-new-button").clicked += CreateNewProfile;

            var volumeProfileObjectField = root.Q<ObjectField>(classes: "volume-profile-objectfield");
            volumeProfileObjectField.objectType = typeof(VolumeProfile);
            volumeProfileObjectField.TrackPropertyValue(m_Profile, _ =>
            {
                ClearInstantiatedProfile();
                UpdateSelectedProfile();
                UpdateElementVisibility(root);
            });

            // Analytics events
            // NOTES:
            // - schedule.Execute is needed to defer the registration until attached to panel. Otherwise the event fires during initialization.
            // - PropertyField.RegisterValueChangeCallback doesn't work as expected even with schedule.Execute, so use ChangeEvent<float> instead.
            volumeProfileObjectField.schedule.Execute(() => volumeProfileObjectField.RegisterValueChangedCallback(evt =>
            {
                VolumeProfileUsageAnalytic.Send(targetVolume, profileRef);
            }));
            var priorityPropertyField = root.Q<PropertyField>("volume-profile-priority");
            priorityPropertyField.schedule.Execute(() => priorityPropertyField.RegisterCallback<ChangeEvent<float>>(evt =>
            {
                VolumePriorityUsageAnalytic.Send(targetVolume);
            }));

            root.Q("volume-profile-component-container").Add(new IMGUIContainer(() =>
            {
                // Needs updating every frame because the profile can be instantiated from user scripts
                if (profileRef != m_ComponentList.asset)
                {
                    UpdateSelectedProfile();
                    UpdateElementVisibility(root);
                }

                // UITK-IMGUI interaction hacks:
                // - Set & restore labelWidth (global state), otherwise you get jumpy sliders on the componentList.
                // - To match IMGUI PropertyField labelWidth with UITK one, we query a UITK label width and use that.
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                var uitkPropertyFieldLabel = priorityPropertyField.Q<Label>();
                if (uitkPropertyFieldLabel != null)
                    EditorGUIUtility.labelWidth = uitkPropertyFieldLabel.layout.width + 2;

                if (!m_Profile.hasMultipleDifferentValues)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(16);
                    GUILayout.BeginVertical();
                    m_ComponentList.OnGUI();
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                }

                EditorGUIUtility.labelWidth = oldLabelWidth;
            }));

            m_ComponentList = new VolumeComponentListEditor(this);
            UpdateSelectedProfile();
            UpdateElementVisibility(root);

            Utilities.LocalizationHelper.LocalizeVisualTree(root);

            return root;
        }

        void UpdateElementVisibility(VisualElement root)
        {
            root.Q<HelpBox>("volume-profile-empty-helpbox").style.display = profileRef != null ? DisplayStyle.None : DisplayStyle.Flex;
            root.Q<Button>("volume-profile-new-button").style.display = profileRef != null ? DisplayStyle.None : DisplayStyle.Flex;
            root.Q<Label>("volume-profile-instance-profile-label").style.display = targetVolume.HasInstantiatedProfile() ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void RefreshEffectListEditor(VolumeProfile asset)
        {
            m_ComponentList.Clear();

            if (asset != null)
            {
                asset.Sanitize();
                m_ComponentList.Init(asset, new SerializedObject(asset));
            }
        }

        void UpdateSelectedProfile()
        {
            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
            RefreshEffectListEditor(profileRef);
        }

        void ClearInstantiatedProfile()
        {
            if (targetVolume.HasInstantiatedProfile())
                targetVolume.profile = null;
        }

        string VolumeProfileNameFromGameObject() => targetVolume.name + " Profile";

        void CreateNewProfile()
        {
            // By default, try to put assets in a folder next to the currently active
            // scene file. If the user isn't a scene, put them in root instead.
            var targetName = VolumeProfileNameFromGameObject();
            var scene = targetVolume.gameObject.scene;
            var asset = VolumeProfileFactory.CreateVolumeProfile(scene, targetName);
            m_Profile.objectReferenceValue = asset;

            UpdateSelectedProfile();
            ClearInstantiatedProfile();
        }

        void CloneProfile()
        {
            // Duplicate the currently assigned profile and save it as a new profile
            var origin = profileRef;
            var path = AssetDatabase.GetAssetPath(m_Profile.objectReferenceValue);
            var directory = path.Length == 0 ? "Assets" : Path.GetDirectoryName(path);
            path = $"{directory}/{ VolumeProfileNameFromGameObject() }.asset";

            path = CoreEditorUtils.IsAssetInReadOnlyPackage(path)

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

            UpdateSelectedProfile();
            ClearInstantiatedProfile();
        }

        void OnVolumeProfileContextClick()
        {
            var menu = new GenericMenu();
            menu.AddItem(Styles.newLabel, false, CreateNewProfile);

            if (profileRef != null)
            {
                var cloneLabel = targetVolume.HasInstantiatedProfile() ? Styles.saveLabel : Styles.cloneLabel;
                menu.AddItem(cloneLabel, false, CloneProfile);
                menu.AddSeparator(string.Empty);
                menu.AddItem(VolumeProfileUtils.Styles.collapseAll, false, () =>
                {
                    VolumeProfileUtils.SetComponentEditorsExpanded(m_ComponentList.editors, false);
                });
                menu.AddItem(VolumeProfileUtils.Styles.expandAll, false, () =>
                {
                    VolumeProfileUtils.SetComponentEditorsExpanded(m_ComponentList.editors, true);
                });
                menu.AddSeparator(string.Empty);
                menu.AddItem(Styles.enableAll, false, () => SetComponentsActive(true));
                menu.AddItem(Styles.disableAll, false, () => SetComponentsActive(false));
                menu.AddItem(Styles.removeAll, false, () => m_ComponentList.RemoveAllComponents());
                menu.AddItem(VolumeProfileUtils.Styles.resetAll, false, () => m_ComponentList.ResetAllComponents());
                menu.AddSeparator(string.Empty);
                menu.AddItem(VolumeProfileUtils.Styles.copyAllSettings, false,
                    () => VolumeComponentCopyPaste.CopySettings(profileRef.components));
                if (VolumeComponentCopyPaste.CanPaste(profileRef.components))
                    menu.AddItem(VolumeProfileUtils.Styles.pasteSettings, false, () =>
                    {
                        VolumeComponentCopyPaste.PasteSettings(profileRef.components);
                        VolumeManager.instance.OnVolumeProfileChanged(profileRef);
                    });
                else
                    menu.AddDisabledItem(VolumeProfileUtils.Styles.pasteSettings);
            }

            menu.ShowAsContext();
        }

        void SetComponentsActive(bool active)
        {
            foreach (var c in profileRef.components)
                c.active = active;
        }

        void SetColliderEnabledWithUndo(Collider collider, bool enabled)
        {
            Undo.RecordObject(collider, $"Set collider enabled to {enabled.ToString()}");
            collider.enabled = enabled;
        }

        void AddColliderWithUndo()
        {
            var menu = new GenericMenu();
            menu.AddItem(Styles.addBoxCollider, false, () => Undo.AddComponent<BoxCollider>(targetVolume.gameObject));
            menu.AddItem(Styles.sphereBoxCollider, false,
                () => Undo.AddComponent<SphereCollider>(targetVolume.gameObject));
            menu.AddItem(Styles.capsuleBoxCollider, false,
                () => Undo.AddComponent<CapsuleCollider>(targetVolume.gameObject));
            menu.AddItem(Styles.meshBoxCollider, false, () =>
            {
                var meshCollider = Undo.AddComponent<MeshCollider>(targetVolume.gameObject);
                meshCollider.convex = true;
            });
            menu.ShowAsContext();
        }
    }
}
