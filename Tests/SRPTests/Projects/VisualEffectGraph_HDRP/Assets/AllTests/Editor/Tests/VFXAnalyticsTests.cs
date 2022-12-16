using System;
using System.Collections;
using System.Linq;

using NUnit.Framework;
using Moq;

using UnityEngine.Analytics;
using UnityEditor.VFX.UI;
using UnityEngine.TestTools;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXAnalyticsTests
    {
        private VFXAnalytics.UsageEventData m_SentData;
        private Mock<IEditorAnalytics> m_EditorAnalyticsMock;

        [SetUp]
        public void Setup()
        {
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            VFXAnalytics.GetInstance().GetOrCreateAnalyticsData().Clear();
            m_SentData = default;
            m_EditorAnalyticsMock = new Mock<IEditorAnalytics>();
            m_EditorAnalyticsMock.SetupGet(x => x.enabled).Returns(true);
            m_EditorAnalyticsMock.Setup(x => x.CanBeSent(It.IsAny<VFXAnalytics.UsageEventData>())).Returns(true);
            m_EditorAnalyticsMock
                .Setup(x => x.SendEventWithLimit(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<int>()))
                .Callback<string, object, int>((x, data, y) => m_SentData = CopyUsageEventData((VFXAnalytics.UsageEventData)data))
                .Returns(AnalyticsResult.Ok);
        }

        [TearDown]
        public void Cleanup()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [Test]
        public void RegisterEvent_Is_Called_Once()
        {
            // Arrange
            var vfxAnalytics = new VFXAnalytics(m_EditorAnalyticsMock.Object);
            var exception = new Exception("This is the exception message");
            var settingPath = "classtype.settingname";

            // Act
            vfxAnalytics.OnCompilationError(exception);
            vfxAnalytics.OnSpecificSettingChanged(settingPath);
            vfxAnalytics.OnQuitApplication();

            // Assert
            m_EditorAnalyticsMock.Verify(x => x.RegisterEventWithLimit("uVFXGraphUsage", 10, 1000, "unity.vfxgraph", 4), Times.Once);
        }

        [Test]
        public void OnCompilationErrorTest()
        {
            // Arrange
            var vfxAnalytics = new VFXAnalytics(m_EditorAnalyticsMock.Object);
            var exception = new Exception("This is the exception message");

            // Act
            vfxAnalytics.OnCompilationError(exception);
            vfxAnalytics.OnQuitApplication();

            // Assert
            m_EditorAnalyticsMock.Verify(x => x.SendEventWithLimit("uVFXGraphUsage", It.IsAny<object>(), 4), Times.Once);

            Assert.AreEqual(new [] { 1 }, m_SentData.compilation_error_count);
            Assert.AreEqual(new [] { exception.Message }, m_SentData.compilation_error_names);
        }

        [Test]
        public void OnSpecificSettingChanged_Test()
        {
            // Arrange
            var vfxAnalytics = new VFXAnalytics(m_EditorAnalyticsMock.Object);
            var settingPath = "classtype.settingname";

            // Act
            vfxAnalytics.OnSpecificSettingChanged(settingPath);
            vfxAnalytics.OnQuitApplication();

            // Assert
            m_EditorAnalyticsMock.Verify(x => x.SendEventWithLimit("uVFXGraphUsage", It.IsAny<object>(), 4), Times.Once);

            Assert.AreEqual(new [] { 1 }, m_SentData.specific_setting_Count);
            Assert.AreEqual(new [] { settingPath }, m_SentData.specific_setting_names);
        }

        [Test]
        public void OnSystemTemplateUsed_Test()
        {
            // Arrange
            var vfxAnalytics = new VFXAnalytics(m_EditorAnalyticsMock.Object);
            var templateName = "name_of_a_template";

            // Act
            vfxAnalytics.OnSystemTemplateCreated(templateName);
            vfxAnalytics.OnQuitApplication();

            // Assert
            m_EditorAnalyticsMock.Verify(x => x.SendEventWithLimit("uVFXGraphUsage", It.IsAny<object>(), 4), Times.Once);

            Assert.AreEqual(new [] { templateName }, m_SentData.system_template_used);
        }

        [UnityTest]
        public IEnumerator OnGraphClosed_Test()
        {
            // Arrange
            var vfxAnalytics = new VFXAnalytics(m_EditorAnalyticsMock.Object);
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var view = VFXViewWindow.GetWindow(graph, true);
            yield return null;
            view.LoadResource(graph.visualEffectResource);
            view.Show(true);
            yield return null;

            // Act
            vfxAnalytics.OnGraphClosed(view.graphView);
            vfxAnalytics.OnQuitApplication();

            // Assert
            m_EditorAnalyticsMock.Verify(x => x.SendEventWithLimit("uVFXGraphUsage", It.IsAny<object>(), 4), Times.Once);

            Assert.AreEqual(1, m_SentData.nb_vfx_opened);
            Assert.AreEqual(VFXAnalytics.EventKind.Quit.ToString(), m_SentData.event_kind);
        }

        private VFXAnalytics.UsageEventData CopyUsageEventData(VFXAnalytics.UsageEventData source)
        {
            VFXAnalytics.UsageEventData copy;
            copy.event_kind = source.event_kind;
            copy.build_target = source.build_target;
            copy.nb_vfx_assets = source.nb_vfx_assets;
            copy.nb_vfx_opened = source.nb_vfx_opened;
            copy.mean_nb_node_per_assets = source.mean_nb_node_per_assets;
            copy.stdv_nb_node_per_assets = source.stdv_nb_node_per_assets;
            copy.max_nb_node_per_assets = source.max_nb_node_per_assets;
            copy.min_nb_node_per_assets = source.min_nb_node_per_assets;
            copy.experimental_node_names = source.experimental_node_names.ToList();
            copy.experimental_node_count_per_asset = source.experimental_node_count_per_asset.ToList();
            copy.compilation_error_count = source.compilation_error_count.ToList();
            copy.compilation_error_names = source.compilation_error_names.ToList();
            copy.specific_setting_names = source.specific_setting_names.ToList();
            copy.specific_setting_Count = source.specific_setting_Count.ToList();
            copy.has_samples_installed = source.has_helpers_installed;
            copy.has_helpers_installed = source.has_helpers_installed;
            copy.system_template_used = source.system_template_used.ToList();

            return copy;
        }
    }
}
