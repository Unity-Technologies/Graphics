using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor.Rendering;

// TODO(Nicholas): deduplicate with LocalVolumetricFogUI.Drawer.cs.
namespace UnityEditor.Experimental.Rendering
{
    using CED = CoreEditorDrawer<SerializedProbeVolume>;

    static partial class ProbeVolumeUI
    {
        internal static readonly CED.IDrawer Inspector = CED.Group(
            CED.Group(
                Drawer_VolumeContent,
                Drawer_BakeToolBar
            )
        );

        static void Drawer_BakeToolBar(SerializedProbeVolume serialized, Editor owner)
        {
            if (!ProbeReferenceVolume.instance.isInitialized) return;

            Bounds bounds = new Bounds();
            bool foundABound = false;
            bool performFitting = false;
            bool performFittingOnlyOnSelection = false;

            bool ContributesToGI(Renderer renderer)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject) & StaticEditorFlags.ContributeGI;
                return (flags & StaticEditorFlags.ContributeGI) != 0;
            }

            void ExpandBounds(Bounds currBound)
            {
                if (!foundABound)
                {
                    bounds = currBound;
                    foundABound = true;
                }
                else
                {
                    bounds.Encapsulate(currBound);
                }
            }

            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Fit to Scene"), EditorStyles.miniButton))
            {
                performFitting = true;
            }
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Fit to Selection"), EditorStyles.miniButton))
            {
                performFitting = true;
                performFittingOnlyOnSelection = true;
            }

            if (performFitting)
            {
                ProbeVolume pv = (serialized.serializedObject.targetObject as ProbeVolume);
                Undo.RecordObject(pv.transform, "Fitting Probe Volume");

                if (performFittingOnlyOnSelection)
                {
                    var transforms = Selection.transforms;
                    foreach (var transform in transforms)
                    {
                        var childrens = transform.gameObject.GetComponentsInChildren<Transform>();
                        foreach (var children in childrens)
                        {
                            Renderer childRenderer;
                            if (children.gameObject.TryGetComponent<Renderer>(out childRenderer))
                            {
                                bool childContributeGI = ContributesToGI(childRenderer) && childRenderer.gameObject.activeInHierarchy && childRenderer.enabled;

                                if (childContributeGI)
                                {
                                    ExpandBounds(childRenderer.bounds);
                                }
                            }
                        }
                    }
                }
                else
                {
                    var renderers = UnityEngine.GameObject.FindObjectsOfType<Renderer>();

                    foreach (Renderer renderer in renderers)
                    {
                        bool contributeGI = ContributesToGI(renderer) && renderer.gameObject.activeInHierarchy && renderer.enabled;

                        if (contributeGI)
                        {
                            ExpandBounds(renderer.bounds);
                        }
                    }
                }

                pv.transform.position = bounds.center;
                float minBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
                Vector3 tmpClamp = (bounds.size + new Vector3(minBrickSize, minBrickSize, minBrickSize));
                tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
                tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
                tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
                serialized.size.vector3Value = tmpClamp;
            }
        }

        static void Drawer_ToolBar(SerializedProbeVolume serialized, Editor owner)
        {
        }

        static void Drawer_VolumeContent(SerializedProbeVolume serialized, Editor owner)
        {
            if (!ProbeReferenceVolume.instance.isInitialized || !ProbeReferenceVolume.instance.enabledBySRP)
            {
                var renderPipelineAsset = UnityEngine.Rendering.RenderPipelineManager.currentPipeline;
                if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name == "HDRenderPipeline")
                {
                    EditorGUILayout.HelpBox("The probe volumes feature is disabled. The feature needs to be enabled in the HDRP Settings and on the used HDRP asset.", MessageType.Warning, wide: true);
                }
                else
                {
                    EditorGUILayout.HelpBox("The probe volumes feature is not enabled or not available on current SRP.", MessageType.Warning, wide: true);
                }

                return;
            }

            ProbeVolume pv = (serialized.serializedObject.targetObject as ProbeVolume);

            bool hasProfile = (ProbeReferenceVolume.instance.sceneData?.GetProfileForScene(pv.gameObject.scene) != null);

            EditorGUI.BeginChangeCheck();
            if (pv.mightNeedRebaking)
            {
                var helpBoxRect = GUILayoutUtility.GetRect(new GUIContent(Styles.s_ProbeVolumeChangedMessage, EditorGUIUtility.IconContent("Warning@2x").image), EditorStyles.helpBox);
                EditorGUI.HelpBox(helpBoxRect, Styles.s_ProbeVolumeChangedMessage, MessageType.Warning);
            }

            EditorGUILayout.PropertyField(serialized.globalVolume, Styles.s_GlobalVolume);
            if (!serialized.globalVolume.boolValue)
                EditorGUILayout.PropertyField(serialized.size, Styles.s_Size);

            if (!hasProfile)
            {
                EditorGUILayout.HelpBox("No profile information is set for the scene that owns this probe volume so no subdivision information can be retrieved.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(!hasProfile);
            var rect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(rect, Styles.s_HighestSubdivLevel, serialized.highestSubdivisionLevelOverride);
            EditorGUI.BeginProperty(rect, Styles.s_LowestSubdivLevel, serialized.lowestSubdivisionLevelOverride);

            // Round min and max subdiv
            int maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
            if (ProbeReferenceVolume.instance.sceneData != null)
            {
                var profile = ProbeReferenceVolume.instance.sceneData.GetProfileForScene(pv.gameObject.scene);

                if (profile != null)
                {
                    ProbeReferenceVolume.instance.SetMinBrickAndMaxSubdiv(profile.minBrickSize, profile.maxSubdivision);
                    maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision() - 1;
                }
                else
                {
                    maxSubdiv = Mathf.Max(0, maxSubdiv);
                }
            }

            EditorGUILayout.LabelField("Subdivision Overrides", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.overridesSubdivision, Styles.s_OverridesSubdivision);
            EditorGUI.BeginDisabledGroup(!serialized.overridesSubdivision.boolValue);

            int value = serialized.highestSubdivisionLevelOverride.intValue;

            // We were initialized, but we cannot know the highest subdiv statically, so we need to resort to this.
            if (serialized.highestSubdivisionLevelOverride.intValue < 0)
                serialized.highestSubdivisionLevelOverride.intValue = maxSubdiv;

            serialized.highestSubdivisionLevelOverride.intValue = Mathf.Min(maxSubdiv, EditorGUILayout.IntSlider(Styles.s_HighestSubdivLevel, serialized.highestSubdivisionLevelOverride.intValue, 0, maxSubdiv));
            serialized.lowestSubdivisionLevelOverride.intValue = Mathf.Min(maxSubdiv, EditorGUILayout.IntSlider(Styles.s_LowestSubdivLevel, serialized.lowestSubdivisionLevelOverride.intValue, 0, maxSubdiv));
            serialized.lowestSubdivisionLevelOverride.intValue = Mathf.Min(serialized.lowestSubdivisionLevelOverride.intValue, serialized.highestSubdivisionLevelOverride.intValue);
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            int minSubdivInVolume = serialized.overridesSubdivision.boolValue ? serialized.lowestSubdivisionLevelOverride.intValue : 0;
            int maxSubdivInVolume = serialized.overridesSubdivision.boolValue ? serialized.highestSubdivisionLevelOverride.intValue : maxSubdiv;
            EditorGUI.indentLevel--;

            if (hasProfile)
                EditorGUILayout.HelpBox($"The distance between probes will fluctuate between : {ProbeReferenceVolume.instance.GetDistanceBetweenProbes(maxSubdiv - maxSubdivInVolume)}m and {ProbeReferenceVolume.instance.GetDistanceBetweenProbes(maxSubdiv - minSubdivInVolume)}m", MessageType.Info);

            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 tmpClamp = serialized.size.vector3Value;
                tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
                tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
                tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
                serialized.size.vector3Value = tmpClamp;
            }

            EditorGUILayout.LabelField("Geometry Settings", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.objectLayerMask, Styles.s_ObjectLayerMask);
            EditorGUILayout.PropertyField(serialized.geometryDistanceOffset, Styles.s_GeometryDistanceOffset);
            EditorGUI.indentLevel--;
        }
    }
}
