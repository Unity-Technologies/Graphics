using System;
using System.Collections.Generic;
using UnityEditor.U2D.Graphics.Profiler.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.U2D.Graphics.Profiler
{
    [Serializable]
    class U2DGraphicsProfilerView : VisualElement
    {
        const string k_UXML = "Packages/com.unity.render-pipelines.universal/Editor/2D/Profiler/UI/U2DGraphicsProfilerView/U2DGraphicsProfilerView.uxml";
        U2DGraphicsHierarchyView m_U2DGraphicsHierarchyView;
        U2DGraphicsStatisticView m_StatisticView;

        public U2DGraphicsProfilerView()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UXML);
            visualTree.CloneTree(this);
            var splitView = this.Q<TwoPaneSplitView>();
            splitView.fixedPaneInitialDimension = 300;
            splitView.fixedPaneIndex = 0;
            m_U2DGraphicsHierarchyView = new U2DGraphicsHierarchyView();
            m_StatisticView = new U2DGraphicsStatisticView();
            var twoPaneSplitView = this.Q<TwoPaneSplitView>();
            twoPaneSplitView.Add(m_StatisticView);
            twoPaneSplitView.Add(m_U2DGraphicsHierarchyView);

#if ENABLE_PROFILER && PROFILER_INSTALLED
            this.Q<Label>("noProfiling").style.display = DisplayStyle.None;
            twoPaneSplitView.style.display = DisplayStyle.Flex;
#else
            this.Q<Label>("noProfiling").style.display = DisplayStyle.Flex;
            twoPaneSplitView.style.display = DisplayStyle.None;
#endif
        }

        public void SetHierarchyData(List<U2DGraphicsHierarchyNodeData>[] categorizedData)
        {
            m_U2DGraphicsHierarchyView.SetData(categorizedData);
        }

        public void SetStatistic(long normalTextures, long lightTextures, long lightBatches, long lightTriangles,
            long shadowTextures, long shadowCasters, long shadowTriangles,
            float renderPassTime, float drawShadowTime, float normalPassTime, float shadowPassTime)
        {
            m_StatisticView.SetStatistic(normalTextures, lightTextures, lightBatches, lightTriangles,
                shadowTextures, shadowCasters, shadowTriangles,
                renderPassTime, drawShadowTime, normalPassTime, shadowPassTime);
        }

        public bool IsLiveUpdateEnabled()
        {
            return m_StatisticView.IsLiveUpdateEnabled();
        }
    }
}
