using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(VisualEnvironment))]
    class VisualEnvironmentEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_SkyType;
        SerializedDataParameter m_CloudType;
        SerializedDataParameter m_SkyAmbientMode;
        SerializedDataParameter m_WindOrientation;
        SerializedDataParameter m_WindSpeed;

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

        static List<GUIContent> m_CloudClassNames = null;
        static List<int> m_CloudUniqueIDs = null;

        public static List<GUIContent> cloudClassNames
        {
            get
            {
                UpdateSkyAndFogIntPopupData();
                return m_CloudClassNames;
            }
        }

        public static List<int> cloudUniqueIDs
        {
            get
            {
                UpdateSkyAndFogIntPopupData();
                return m_CloudUniqueIDs;
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<VisualEnvironment>(serializedObject);

            m_SkyType = Unpack(o.Find(x => x.skyType));
            m_CloudType = Unpack(o.Find(x => x.cloudType));
            m_SkyAmbientMode = Unpack(o.Find(x => x.skyAmbientMode));

            m_WindOrientation = Unpack(o.Find(x => x.windOrientation));
            m_WindSpeed = Unpack(o.Find(x => x.windSpeed));
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
            if (m_CloudClassNames == null)
            {
                m_CloudClassNames = new List<GUIContent>();
                m_CloudUniqueIDs = new List<int>();

                // Add special "None" case.
                m_CloudClassNames.Add(new GUIContent("None"));
                m_CloudUniqueIDs.Add(0);

                var typesDict = SkyManager.cloudTypesDict;

                foreach (KeyValuePair<int, Type> kvp in typesDict)
                {
                    string name = ObjectNames.NicifyVariableName(kvp.Value.Name.ToString());
                    name = name.Replace("Settings", ""); // remove Settings if it was in the class name
                    m_CloudClassNames.Add(new GUIContent(name));
                    m_CloudUniqueIDs.Add(kvp.Key);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            UpdateSkyAndFogIntPopupData();

            using (var scope = new OverridablePropertyScope(m_SkyType, EditorGUIUtility.TrTextContent("Sky type", "Specifies the type of sky this Volume uses."), this))
            {
                if (scope.displayed)
                    EditorGUILayout.IntPopup(m_SkyType.value, m_SkyClassNames.ToArray(), m_SkyUniqueIDs.ToArray(), scope.label);
            }

            using (var scope = new OverridablePropertyScope(m_CloudType, EditorGUIUtility.TrTextContent("Background clouds", "Specifies the type of background cloud this Volume uses."), this))
            {
                if (scope.displayed)
                    EditorGUILayout.IntPopup(m_CloudType.value, m_CloudClassNames.ToArray(), m_CloudUniqueIDs.ToArray(), scope.label);
            }

            PropertyField(m_SkyAmbientMode, EditorGUIUtility.TrTextContent("Ambient Mode", "Specifies how the global ambient probe is computed. Dynamic will use the currently displayed sky and static will use the sky setup in the environment lighting panel."));

            var staticLightingSky = SkyManager.GetStaticLightingSky();
            if (m_SkyAmbientMode.value.GetEnumValue<SkyAmbientMode>() == SkyAmbientMode.Static)
            {
                if (staticLightingSky == null)
                    EditorGUILayout.HelpBox("No Static Lighting Sky is assigned in the Environment settings.", MessageType.Info);
                else
                {
                    var skyType = staticLightingSky.staticLightingSkyUniqueID == 0 ? "no Sky" : SkyManager.skyTypesDict[staticLightingSky.staticLightingSkyUniqueID].Name.ToString();
                    var cloudType = staticLightingSky.staticLightingCloudsUniqueID == 0 ? "no Clouds" : SkyManager.cloudTypesDict[staticLightingSky.staticLightingCloudsUniqueID].Name.ToString();
                    EditorGUILayout.HelpBox($"Current Static Lighting Sky uses {skyType} and {cloudType} of profile {staticLightingSky.profile?.name ?? "None"}.", MessageType.Info);
                }
            }

            PropertyField(m_WindOrientation, EditorGUIUtility.TrTextContent("Global Orientation", "Controls the orientation of the wind relative to the X world vector."));
            PropertyField(m_WindSpeed, EditorGUIUtility.TrTextContent("Global Speed", "Controls the global wind speed in kilometers per hour."));

            if (m_WindSpeed.overrideState.boolValue && m_WindSpeed.value.floatValue != 0.0f && SceneView.lastActiveSceneView && !SceneView.lastActiveSceneView.sceneViewState.alwaysRefreshEnabled)
                EditorGUILayout.HelpBox("Wind animations in the scene view are only supported when \"Always Refresh\" is enabled.", MessageType.Info);

        }
    }

    sealed class LocalWindParameterDrawer
    {
        static readonly string[] modeNames = Enum.GetNames(typeof(WindParameter.WindOverrideMode));
        static readonly string[] modeNamesNoMultiply = { WindParameter.WindOverrideMode.Custom.ToString(), WindParameter.WindOverrideMode.Global.ToString(), WindParameter.WindOverrideMode.Additive.ToString() };
        static readonly int popupWidth = 70;

        public static bool BeginGUI(out Rect rect, GUIContent title, SerializedDataParameter parameter, SerializedProperty mode, bool excludeMultiply)
        {
            rect = EditorGUILayout.GetControlRect();
            rect.xMax -= popupWidth + 2;

            var popupRect = rect;
            popupRect.x = rect.xMax + 2;
            popupRect.width = popupWidth;
            mode.intValue = EditorGUI.Popup(popupRect, mode.intValue, excludeMultiply ? modeNamesNoMultiply : modeNames);

            if (mode.intValue == (int)WindParameter.WindOverrideMode.Additive)
            {
                var value = parameter.value.FindPropertyRelative("additiveValue");
                value.floatValue = EditorGUI.FloatField(rect, title, value.floatValue);
            }
            else if (mode.intValue == (int)WindParameter.WindOverrideMode.Multiply)
            {
                var value = parameter.value.FindPropertyRelative("multiplyValue");
                value.floatValue = EditorGUI.FloatField(rect, title, value.floatValue);
            }
            else
            {
                if (mode.intValue == (int)WindParameter.WindOverrideMode.Global)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.showMixedValue = true;
                }
                return false;
            }
            return true;
        }

        public static void EndGUI(SerializedProperty mode)
        {
            if (mode.intValue == (int)WindParameter.WindOverrideMode.Global)
            {
                EditorGUI.showMixedValue = false;
                EditorGUI.EndDisabledGroup();
            }
        }
    }

    [VolumeParameterDrawer(typeof(WindOrientationParameter))]
    sealed class WindOrientationParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var mode = parameter.value.FindPropertyRelative("mode");
            if (!LocalWindParameterDrawer.BeginGUI(out var rect, title, parameter, mode, true))
            {
                var value = parameter.value.FindPropertyRelative("customValue");
                value.floatValue = EditorGUI.Slider(rect, title, value.floatValue, 0.0f, 360.0f);
            }
            LocalWindParameterDrawer.EndGUI(mode);

            return true;
        }
    }

    [VolumeParameterDrawer(typeof(WindSpeedParameter))]
    sealed class WindSpeedParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var mode = parameter.value.FindPropertyRelative("mode");
            if (!LocalWindParameterDrawer.BeginGUI(out var rect, title, parameter, mode, false))
            {
                var value = parameter.value.FindPropertyRelative("customValue");
                value.floatValue = EditorGUI.FloatField(rect, title, value.floatValue);
            }
            LocalWindParameterDrawer.EndGUI(mode);

            return true;
        }
    }
}
