using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [VolumeComponentEditor(typeof(VisualEnvironment))]
    class VisualEnvironmentEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_SkyType;
        SerializedDataParameter m_SkyAmbientMode;

        GUIContent[] m_SkyClassNames = null;
        int[] m_SkyUniqueIDs = null;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<VisualEnvironment>(serializedObject);
            m_SkyType = Unpack(o.Find(x => x.skyType));
            m_SkyAmbientMode = Unpack(o.Find(x => x.skyAmbientMode));
        }

        private void UpdateAvailableSkies()
        {
            if (m_SkyClassNames == null)
            {
                List<GUIContent> skyClassNames = new List<GUIContent>();
                List<int> skyUniqueIDs = new List<int>();

                // Add special "None" case.
                skyClassNames.Add(EditorGUIUtility.TrTextContent("None"));
                skyUniqueIDs.Add(0);

                var skyTypesDict = SkyTypesCatalog.skyTypesDict;
                foreach (KeyValuePair<int, Type> kvp in skyTypesDict)
                {
                    string name = ObjectNames.NicifyVariableName(kvp.Value.Name.ToString());
                    name = name.Replace("Settings", ""); // remove Settings if it was in the class name

                    skyClassNames.Add(EditorGUIUtility.TrTextContent(name));
                    skyUniqueIDs.Add(kvp.Key);
                }

                m_SkyClassNames = skyClassNames.ToArray();
                m_SkyUniqueIDs = skyUniqueIDs.ToArray();
            }
        }

        public override void OnInspectorGUI()
        {
            UpdateAvailableSkies();

            EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Sky"), EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawOverrideCheckbox(m_SkyType);
                using (new EditorGUI.DisabledScope(!m_SkyType.overrideState.boolValue))
                {
                    EditorGUILayout.IntPopup(m_SkyType.value, m_SkyClassNames, m_SkyUniqueIDs, EditorGUIUtility.TrTextContent("Type", "Specifies the type of sky this Volume uses."));
                }
            }
            if (m_SkyType.value.intValue != 0)
                EditorGUILayout.HelpBox("You need to also add a Volume Component matching the selected type.", MessageType.Info);

            PropertyField(m_SkyAmbientMode, EditorGUIUtility.TrTextContent("Ambient Mode"));
            if (m_SkyAmbientMode.value.GetEnumValue<SkyAmbientMode>() == SkyAmbientMode.Static)
            {
                // TODO
                //var staticLightingSky = SkyManager.GetStaticLightingSky();
                //if (staticLightingSky == null)
                //    EditorGUILayout.HelpBox("Current Static Lighting Sky use None of profile None.", MessageType.Info);
                //else
                //{
                //    var skyType = staticLightingSky.staticLightingSkyUniqueID == 0 ? "None" : SkyManager.skyTypesDict[staticLightingSky.staticLightingSkyUniqueID].Name.ToString();
                //    EditorGUILayout.HelpBox($"Current Static Lighting Sky use {skyType} of profile {staticLightingSky.profile?.name ?? "None"}.", MessageType.Info);
                //}
            }
        }

    }


}
