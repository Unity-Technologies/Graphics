using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEditor.Rendering;

// TODO(Nicholas): deduplicate with LocalVolumetricFogUI.Drawer.cs.
namespace UnityEditor.Experimental.Rendering
{
    using CED = CoreEditorDrawer<SerializedProbeVolume>;

    static partial class ProbeVolumeUI
    {
        [System.Flags]
        enum Expandable
        {
            Volume = 1 << 0,
            Probes = 1 << 1,
            Baking = 1 << 2
        }

        readonly static ExpandedState<Expandable, ProbeVolume> k_ExpandedStateVolume = new ExpandedState<Expandable, ProbeVolume>(Expandable.Volume, "HDRP");
        readonly static ExpandedState<Expandable, ProbeVolume> k_ExpandedStateProbes = new ExpandedState<Expandable, ProbeVolume>(Expandable.Probes, "HDRP");
        readonly static ExpandedState<Expandable, ProbeVolume> k_ExpandedStateBaking = new ExpandedState<Expandable, ProbeVolume>(Expandable.Baking, "HDRP");

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

                (serialized.serializedObject.targetObject as ProbeVolume).transform.position = bounds.center;

                float minBrickSize = ProbeReferenceVolume.instance.MinBrickSize();
                Vector3 tmpClamp = (bounds.size  + new Vector3(minBrickSize, minBrickSize, minBrickSize));
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
            if (!ProbeReferenceVolume.instance.isInitialized)
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

            EditorGUI.BeginChangeCheck();
            if ((serialized.serializedObject.targetObject as ProbeVolume).mightNeedRebaking)
            {
                EditorGUILayout.HelpBox("The probe volume has changed since last baking or the data was never baked.\nPlease bake lighting in the lighting panel to update the lighting data.", MessageType.Warning, wide: true);
            }

            EditorGUILayout.PropertyField(serialized.size, Styles.s_Size);

            var rect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(rect, Styles.s_MinMaxSubdivSlider, serialized.minSubdivisionMultiplier);
            EditorGUI.BeginProperty(rect, Styles.s_MinMaxSubdivSlider, serialized.maxSubdivisionMultiplier);

            // Round min and max subdiv
            float maxSubdiv = ProbeReferenceVolume.instance.GetMaxSubdivision(1) - 1;
            float min = Mathf.Round(serialized.minSubdivisionMultiplier.floatValue * maxSubdiv) / maxSubdiv;
            float max = Mathf.Round(serialized.maxSubdivisionMultiplier.floatValue * maxSubdiv) / maxSubdiv;

            EditorGUILayout.MinMaxSlider(Styles.s_MinMaxSubdivSlider, ref min, ref max, 0, 1);
            serialized.minSubdivisionMultiplier.floatValue = Mathf.Max(0.01f, min);
            serialized.maxSubdivisionMultiplier.floatValue = Mathf.Max(0.01f, max);
            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            EditorGUILayout.HelpBox($"The probe subdivision will fluctuate between {ProbeReferenceVolume.instance.GetMaxSubdivision(serialized.minSubdivisionMultiplier.floatValue)} and {ProbeReferenceVolume.instance.GetMaxSubdivision(serialized.maxSubdivisionMultiplier.floatValue)}", MessageType.Info);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 tmpClamp = serialized.size.vector3Value;
                tmpClamp.x = Mathf.Max(0f, tmpClamp.x);
                tmpClamp.y = Mathf.Max(0f, tmpClamp.y);
                tmpClamp.z = Mathf.Max(0f, tmpClamp.z);
                serialized.size.vector3Value = tmpClamp;
            }
        }
    }
}
