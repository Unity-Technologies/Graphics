using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(VisualEnvironment))]
    class VisualEnvironmentEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_SkyType;
        SerializedDataParameter m_SkyAmbientMode;

        static List<GUIContent> m_SkyClassNames = null;
        static List<int> m_SkyUniqueIDs = null;

        public static List<GUIContent> skyClassNames
        {
            get
            {
                UpdateSkyAndFogIntPopupData();
                return m_SkyClassNames;
            }
        }

        public static List<int> skyUniqueIDs
        {
            get
            {
                UpdateSkyAndFogIntPopupData();
                return m_SkyUniqueIDs;
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<VisualEnvironment>(serializedObject);

            m_SkyType = Unpack(o.Find(x => x.skyType));
            m_SkyAmbientMode = Unpack(o.Find(x => x.skyAmbientMode));
        }

        static void UpdateSkyAndFogIntPopupData()
        {
            if (m_SkyClassNames == null)
            {
                m_SkyClassNames = new List<GUIContent>();
                m_SkyUniqueIDs = new List<int>();

                // Add special "None" case.
                m_SkyClassNames.Add(new GUIContent("None"));
                m_SkyUniqueIDs.Add(0);

                var skyTypesDict = SkyManager.skyTypesDict;

                foreach (KeyValuePair<int, Type> kvp in skyTypesDict)
                {
                    string name = ObjectNames.NicifyVariableName(kvp.Value.Name.ToString());
                    name = name.Replace("Settings", ""); // remove Settings if it was in the class name
                    m_SkyClassNames.Add(new GUIContent(name));
                    m_SkyUniqueIDs.Add(kvp.Key);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            UpdateSkyAndFogIntPopupData();

            EditorGUILayout.LabelField(EditorGUIUtility.TrTextContent("Sky"), EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawOverrideCheckbox(m_SkyType);
                using (new EditorGUI.DisabledScope(!m_SkyType.overrideState.boolValue))
                {
                    EditorGUILayout.IntPopup(m_SkyType.value, m_SkyClassNames.ToArray(), m_SkyUniqueIDs.ToArray(), EditorGUIUtility.TrTextContent("Type", "Specifies the type of sky this Volume uses."));
                }
            }
            PropertyField(m_SkyAmbientMode, EditorGUIUtility.TrTextContent("Ambient Mode", "Specifies how the global ambient probe is computed. Dynamic will use the currently displayed sky and static will use the sky setup in the environment lighting panel."));

            var staticLightingSky = SkyManager.GetStaticLightingSky();
            if (m_SkyAmbientMode.value.GetEnumValue<SkyAmbientMode>() == SkyAmbientMode.Static)
            {
                if (staticLightingSky == null)
                    EditorGUILayout.HelpBox("Current Static Lighting Sky use None of profile None.", MessageType.Info);
                else
                {
                    var skyType = staticLightingSky.staticLightingSkyUniqueID == 0 ? "None" : SkyManager.skyTypesDict[staticLightingSky.staticLightingSkyUniqueID].Name.ToString();
                    EditorGUILayout.HelpBox($"Current Static Lighting Sky use {skyType} of profile {staticLightingSky.profile?.name ?? "None"}.", MessageType.Info);
                }
            }
        }
    }
}
