using System;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Graphics.Profiler.UI
{
    [UxmlElement]
    [Serializable]
    partial class U2DGraphicsStatisticView : VisualElement
    {
        const string k_UXML = "Packages/com.unity.render-pipelines.universal/Editor/2D/Profiler/UI/U2DGraphicsStatisticView/U2DGraphicsStatisticView.uxml";

        public Label NormalTexturesLabel { get; private set; }
        public Label LightTexturesLabel { get; private set; }
        public Label LightBatchesLabel { get; private set; }
        public Label LightTrianglesLabel { get; private set; }
        public Label ShadowTexturesLabel { get; private set; }
        public Label ShadowCastersLabel { get; private set; }
        public Label ShadowTrianglesLabel { get; private set; }
        public Label RenderPassTimeLabel { get; private set; }
        public Label DrawShadowTimeLabel { get; private set; }
        public Label NormalPassTimeLabel { get; private set; }
        public Label ShadowPassTimeLabel { get; private set; }
        Toggle m_LiveUpdate;


        public U2DGraphicsStatisticView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UXML);
            visualTree.CloneTree(this);

            // Query and cache label references
            NormalTexturesLabel = this.Q<Label>("NormalTexturesLabel");
            LightTexturesLabel = this.Q<Label>("LightTexturesLabel");
            LightBatchesLabel = this.Q<Label>("LightBatchesLabel");
            LightTrianglesLabel = this.Q<Label>("LightTrianglesLabel");
            ShadowTexturesLabel = this.Q<Label>("ShadowTexturesLabel");
            ShadowCastersLabel = this.Q<Label>("ShadowCastersLabel");
            ShadowTrianglesLabel = this.Q<Label>("ShadowTrianglesLabel");
            RenderPassTimeLabel = this.Q<Label>("RenderPassTimeLabel");
            DrawShadowTimeLabel = this.Q<Label>("DrawShadowTimeLabel");
            NormalPassTimeLabel = this.Q<Label>("NormalPassTimeLabel");
            ShadowPassTimeLabel = this.Q<Label>("ShadowPassTimeLabel");
            m_LiveUpdate = this.Q<Toggle>("EnableStatisticsToggle");
        }

        public void SetStatistic(long normalTextures, long lightTextures, long lightBatches, long lightTriangles,
            long shadowTextures, long shadowCasters, long shadowTriangles,
            float renderPassTime, float drawShadowTime, float normalPassTime, float shadowPassTime)
        {
            NormalTexturesLabel.text = normalTextures.ToString();
            LightTexturesLabel.text = lightTextures.ToString();
            LightBatchesLabel.text = lightBatches.ToString();
            LightTrianglesLabel.text = lightTriangles.ToString();
            ShadowTexturesLabel.text = shadowTextures.ToString();
            ShadowCastersLabel.text = shadowCasters.ToString();
            ShadowTrianglesLabel.text = shadowTriangles.ToString();
            RenderPassTimeLabel.text = $"{renderPassTime:F2} ms";
            DrawShadowTimeLabel.text = $"{drawShadowTime:F2} ms";
            NormalPassTimeLabel.text = $"{normalPassTime:F2} ms";
            ShadowPassTimeLabel.text = $"{shadowPassTime:F2} ms";
        }

        public bool IsLiveUpdateEnabled()
        {
            return m_LiveUpdate.value;
        }
    }
}
