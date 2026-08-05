using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    using CED = CoreEditorDrawer<SerializedProbeVolume>;

    static partial class ProbeVolumeUI
    {
        internal static readonly CED.IDrawer k_Inspector = CED.Group(
            CED.Group(
                Drawer_VolumeContent,
                Drawer_RebakeWarning // This needs to be last to avoid popping in the UI
            )
        );

        static void Drawer_BakeToolBar(SerializedProbeVolume serialized, Editor owner)
        {
            if (!ProbeReferenceVolume.instance.isInitialized) return;

            ProbeVolume pv = (serialized.m_SerializedObject.targetObject as ProbeVolume);

            GIContributors.ContributorFilter? filter = null;

            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Fit to All Scenes", "Fit this Adaptive Probe Volume to cover all loaded Scenes. "), EditorStyles.miniButton))
                filter = GIContributors.ContributorFilter.All;
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Fit to Scene", "Fit this Adaptive Probe Volume to the renderers in the same Scene."), EditorStyles.miniButton))
                filter = GIContributors.ContributorFilter.Scene;
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Fit to Selection", "Fits this Adaptive Probe Volume to the selected renderer(s). Lock the Inspector to make additional selections."), EditorStyles.miniButton))
                filter = GIContributors.ContributorFilter.Selection;

            if (filter.HasValue)
            {
                Undo.RecordObject(pv.transform, "Fitting Adaptive Probe Volume");

                // Get minBrickSize from scene baking set if available
                var bakingSet = ProbeVolumeLightingTab.GetSceneBakingSetForUI(pv.gameObject.scene);
                float minBrickSize = bakingSet != null ? bakingSet.minBrickSize : ProbeReferenceVolume.instance.MinBrickSize();

                var bounds = pv.ComputeBounds(filter.Value, pv.gameObject.scene);
                pv.transform.position = bounds.center;
                serialized.m_Size.vector3Value = Vector3.Max(bounds.size + new Vector3(minBrickSize, minBrickSize, minBrickSize), Vector3.zero);
            }
        }

        static readonly int k_SubdivisionRangeID = "SubdivisionRange".GetHashCode();

        static void SubdivisionRange(SerializedProbeVolume serialized, int maxSimplicationLevel, float minDistance)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.k_DistanceBetweenProbes, serialized.m_MinSubdivisionLevel);
            EditorGUI.BeginProperty(rect, Styles.k_DistanceBetweenProbes, serialized.m_MaxSubdivisionLevel);
            EditorGUI.BeginProperty(rect, Styles.k_DistanceBetweenProbes, serialized.m_OverridesSubdivision);

            var checkbox = new Rect(rect) { width = 14 + 9, x = rect.x + 2 };
            serialized.m_OverridesSubdivision.boolValue = EditorGUI.Toggle(checkbox, serialized.m_OverridesSubdivision.boolValue);

            using (new EditorGUI.DisabledScope(!serialized.m_OverridesSubdivision.boolValue))
            {
                EditorGUIUtility.labelWidth -= checkbox.width;
                rect.xMin = checkbox.xMax - 4;
                int id = GUIUtility.GetControlID(k_SubdivisionRangeID, FocusType.Keyboard, rect);
                rect = EditorGUI.PrefixLabel(rect, id, Styles.k_DistanceBetweenProbes);
                EditorGUIUtility.labelWidth += checkbox.width;

                // Make sure data is valid
                float maxLevelOverride = Mathf.Min(serialized.m_MaxSubdivisionLevel.intValue, maxSimplicationLevel);
                float minLevelOverride = Mathf.Min(serialized.m_MinSubdivisionLevel.intValue, maxLevelOverride);

                EditorGUI.BeginChangeCheck();
                EditorGUI.MinMaxSlider(rect, ref minLevelOverride, ref maxLevelOverride, 0, maxSimplicationLevel);
                if (EditorGUI.EndChangeCheck())
                {
                    GUIUtility.keyboardControl = id;

                    serialized.m_MinSubdivisionLevel.intValue = Mathf.RoundToInt(minLevelOverride);
                    serialized.m_MaxSubdivisionLevel.intValue = Mathf.RoundToInt(maxLevelOverride);
                }

                ProbeVolumeLightingTab.DrawSimplificationLevelsMarkers(rect, minDistance, 0, maxSimplicationLevel,
                    serialized.m_MinSubdivisionLevel.intValue, serialized.m_MaxSubdivisionLevel.intValue);
            }

            EditorGUI.EndProperty();
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();
        }

        static void Drawer_VolumeContent(SerializedProbeVolume serialized, Editor owner)
        {
            ProbeVolume pv = (serialized.m_SerializedObject.targetObject as ProbeVolume);
            var bakingSet = ProbeVolumeLightingTab.GetSceneBakingSetForUI(pv.gameObject.scene);

            EditorGUILayout.PropertyField(serialized.m_Mode);
            if (serialized.m_Mode.intValue == (int)ProbeVolume.Mode.Local)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.m_Size, Styles.k_Size);
                if (EditorGUI.EndChangeCheck())
                    serialized.m_Size.vector3Value = Vector3.Max(serialized.m_Size.vector3Value, Vector3.zero);

                Drawer_BakeToolBar(serialized, owner);
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Subdivision Override", EditorStyles.boldLabel);
            bool isFreezingPlacement = bakingSet != null && bakingSet.freezePlacement && AdaptiveProbeVolumes.CanFreezePlacement();
            using (new EditorGUI.DisabledScope(isFreezingPlacement))
            {
                // Get settings from scene profile if available
                int simplificationLevels = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
                float minDistance = ProbeReferenceVolume.instance.MinDistanceBetweenProbes();
                if (bakingSet != null)
                {
                    simplificationLevels = bakingSet.simplificationLevels;
                    minDistance = bakingSet.minDistanceBetweenProbes;
                }
                if (simplificationLevels < 0)
                {
                    simplificationLevels = 5;
                    minDistance = 1;
                }

                SubdivisionRange(serialized, simplificationLevels, minDistance);
            }

            if (isFreezingPlacement)
            {
                CoreEditorUtils.DrawFixMeBox("The placement is frozen in the baking settings. To change these values uncheck the Freeze Placement in the Adaptive Probe Volumes tab of the Lighting Window.", MessageType.Info, "Open", () =>
                {
                    ProbeVolumeLightingTab.OpenBakingSet(bakingSet);
                });
            }

            EditorGUILayout.LabelField("Geometry Settings", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serialized.m_OverrideRendererFilters, Styles.k_OverrideRendererFilters);
            if (serialized.m_OverrideRendererFilters.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.m_ObjectLayerMask, Styles.k_ObjectLayerMask);
                EditorGUILayout.PropertyField(serialized.m_MinRendererVolumeSize, Styles.k_MinRendererVolumeSize);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serialized.m_FillEmptySpaces);

            if (bakingSet == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The scene this Adaptive Probe Volume is part of does not belong to any Baking Set.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(Lightmapping.isRunning || bakingSet == null))
            {
                ProbeVolumeLightingTab.BakeAPVButton();
            }
        }

        static void Drawer_RebakeWarning(SerializedProbeVolume serialized, Editor owner)
        {
            ProbeVolume pv = (serialized.m_SerializedObject.targetObject as ProbeVolume);

            if (pv.mightNeedRebaking)
            {
                EditorGUILayout.Space();
                var helpBoxRect = GUILayoutUtility.GetRect(new GUIContent(Styles.k_ProbeVolumeChangedMessage, EditorGUIUtility.IconContent("Warning@2x").image), EditorStyles.helpBox);
                EditorGUI.HelpBox(helpBoxRect, Styles.k_ProbeVolumeChangedMessage, MessageType.Warning);
            }
        }
    }
}
