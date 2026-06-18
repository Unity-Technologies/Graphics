#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering.Universal
{
    [Overlay(typeof(SceneView),
        k_OverlayID,
        "Batch Type",
        defaultDisplay = false,
        defaultDockZone = DockZone.RightColumn,
        group = OverlayAttribute.unityGroup,
        defaultDockIndex = 1)]
    class BatchingTypeDebugOverlay : Overlay, ITransientOverlay
    {
        const string k_OverlayID = "URP/Batching Type Debug";

        private bool m_ShouldDisplay;
        public bool visible => m_ShouldDisplay;

        public override void OnCreated()
        {
            var enabled = UniversalRenderPipelineDebugDisplaySettings.Instance?.renderingSettings?.batchingTypeViewEnabled ?? false;
            UpdateDisplay(enabled);

            EditorApplication.update += MonitorGRDState;

            DebugDisplaySettingsRendering.onBatchingTypeViewChanged += OnBatchingTypeViewChanged;
        }

        public override void OnWillBeDestroyed()
        {
            EditorApplication.update -= MonitorGRDState;

            DebugDisplaySettingsRendering.onBatchingTypeViewChanged -= OnBatchingTypeViewChanged;
        }

        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            root.Add(CreateColorSwatch("GRD Batch", new Color(0f, 1f, 0f)));
            root.Add(CreateColorSwatch("SRP Batch", new Color(0f, 0f, 1f)));
            root.Add(CreateColorSwatch("Unbatched", new Color(1f, 0.5f, 0f)));
            root.Add(CreateColorSwatch("Untracked", new Color(0.5f, 0.5f, 0.5f)));
            root.Add(new HelpBox(
                "Grey=not tracked by GRD (e.g. SkinnedMesh, particles)\n" +
                "Only GRD-tracked Mesh Renderers are reliable",
                HelpBoxMessageType.Info));
            return root;
        }

        private static void MonitorGRDState()
        {
            var enabled = UniversalRenderPipelineDebugDisplaySettings.Instance?.renderingSettings?.batchingTypeViewEnabled ?? false;
            if (!enabled)
            {
                return;
            }

            var asset = UniversalRenderPipeline.asset;
            if (!asset || asset.gpuResidentDrawerMode == GPUResidentDrawerMode.Disabled)
            {
                UniversalRenderPipelineDebugDisplaySettings.Instance?.renderingSettings?.SetBatchingTypeDebugEnabled(false);
            }
        }

        private void OnBatchingTypeViewChanged(bool enabled)
        {
            UpdateDisplay(enabled);

            SceneView.RepaintAll();
        }

        private void UpdateDisplay(bool enabled)
        {
            m_ShouldDisplay = enabled;

            if (enabled)
            {
                EditorApplication.delayCall += () =>
                {
                    collapsed = false;
                };
            }
        }

        private static VisualElement CreateColorSwatch(string label, Color color)
        {
            var text = new Label(label);
            text.AddToClassList("unity-base-field__label");

            var colorBox = new VisualElement
            {
                name = "color-content", style = { backgroundColor = new StyleColor(color) }
            };

            var swatch = new VisualElement();
            swatch.AddToClassList("unity-pbr-validation-color-swatch");
            swatch.Add(colorBox);

            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 }
            };
            row.Add(text);
            row.Add(swatch);
            return row;
        }
    }
}
#endif
