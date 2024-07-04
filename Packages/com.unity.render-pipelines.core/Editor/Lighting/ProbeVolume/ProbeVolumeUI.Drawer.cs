using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    using CED = CoreEditorDrawer<SerializedProbeVolume>;

    static partial class ProbeVolumeUI
    {
        internal static readonly CED.IDrawer Inspector = CED.Group(
            CED.Group(
                Drawer_VolumeContent,
                Drawer_RebakeWarning // This needs to be last to avoid popping in the UI
            )
        );

        static void Drawer_BakeToolBar(SerializedProbeVolume serialized, Editor owner)
        {
            if (!ProbeReferenceVolume.instance.isInitialized) return;

            ProbeVolume pv = (serialized.serializedObject.targetObject as ProbeVolume);

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
                serialized.size.vector3Value = Vector3.Max(bounds.size + new Vector3(minBrickSize, minBrickSize, minBrickSize), Vector3.zero);
            }
        }

        static int s_SubdivisionRangeID = "SubdivisionRange".GetHashCode();

        static void SubdivisionRange(SerializedProbeVolume serialized, int maxSimplicationLevel, float minDistance)
        {
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, Styles.s_DistanceBetweenProbes, serialized.minSubdivisionLevel);
            EditorGUI.BeginProperty(rect, Styles.s_DistanceBetweenProbes, serialized.maxSubdivisionLevel);
            EditorGUI.BeginProperty(rect, Styles.s_DistanceBetweenProbes, serialized.overridesSubdivision);

            var checkbox = new Rect(rect) { width = 14 + 9, x = rect.x + 2 };
            serialized.overridesSubdivision.boolValue = EditorGUI.Toggle(checkbox, serialized.overridesSubdivision.boolValue);

            using (new EditorGUI.DisabledScope(!serialized.overridesSubdivision.boolValue))
            {
                EditorGUIUtility.labelWidth -= checkbox.width;
                rect.xMin = checkbox.xMax - 4;
                int id = GUIUtility.GetControlID(s_SubdivisionRangeID, FocusType.Keyboard, rect);
                rect = EditorGUI.PrefixLabel(rect, id, Styles.s_DistanceBetweenProbes);
                EditorGUIUtility.labelWidth += checkbox.width;

                // Make sure data is valid
                float maxLevelOverride = Mathf.Min(serialized.maxSubdivisionLevel.intValue, maxSimplicationLevel);
                float minLevelOverride = Mathf.Min(serialized.minSubdivisionLevel.intValue, maxLevelOverride);

                EditorGUI.BeginChangeCheck();
                EditorGUI.MinMaxSlider(rect, ref minLevelOverride, ref maxLevelOverride, 0, maxSimplicationLevel);
                if (EditorGUI.EndChangeCheck())
                {
                    GUIUtility.keyboardControl = id;

                    serialized.minSubdivisionLevel.intValue = Mathf.RoundToInt(minLevelOverride);
                    serialized.maxSubdivisionLevel.intValue = Mathf.RoundToInt(maxLevelOverride);
                }

                ProbeVolumeLightingTab.DrawSimplificationLevelsMarkers(rect, minDistance, 0, maxSimplicationLevel,
                    serialized.minSubdivisionLevel.intValue, serialized.maxSubdivisionLevel.intValue);
            }

            EditorGUI.EndProperty();
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();
        }

        static void Drawer_VolumeContent(SerializedProbeVolume serialized, Editor owner)
        {
            ProbeVolume pv = (serialized.serializedObject.targetObject as ProbeVolume);
            var bakingSet = ProbeVolumeLightingTab.GetSceneBakingSetForUI(pv.gameObject.scene);

            EditorGUILayout.PropertyField(serialized.mode);
            if (serialized.mode.intValue == (int)ProbeVolume.Mode.Local)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(serialized.size, Styles.s_Size);
                if (EditorGUI.EndChangeCheck())
                    serialized.size.vector3Value = Vector3.Max(serialized.size.vector3Value, Vector3.zero);

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

            EditorGUILayout.PropertyField(serialized.overrideRendererFilters, Styles.s_OverrideRendererFilters);
            if (serialized.overrideRendererFilters.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.objectLayerMask, Styles.s_ObjectLayerMask);
                EditorGUILayout.PropertyField(serialized.minRendererVolumeSize, Styles.s_MinRendererVolumeSize);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(serialized.fillEmptySpaces);

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
            ProbeVolume pv = (serialized.serializedObject.targetObject as ProbeVolume);

            if (pv.mightNeedRebaking)
            {
                EditorGUILayout.Space();
                var helpBoxRect = GUILayoutUtility.GetRect(new GUIContent(Styles.s_ProbeVolumeChangedMessage, EditorGUIUtility.IconContent("Warning@2x").image), EditorStyles.helpBox);
                EditorGUI.HelpBox(helpBoxRect, Styles.s_ProbeVolumeChangedMessage, MessageType.Warning);
            }
        }
    }
}
