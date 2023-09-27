using System;
using System.Collections;
using System.Linq;
using System.Reflection;

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

        [AnalyticInfo(eventName: "uVFXGraphUsage", vendorKey: "unity.vfxgraph", maxEventsPerHour: 10, maxNumberOfElements: 1000, version: 4)]
        internal class Analytic : IAnalytic
        {
            public Analytic(VFXAnalytics.UsageEventData data) { m_Data = data; }
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = m_Data;
                error = null;
                return true;
            }

            VFXAnalytics.UsageEventData m_Data;
        }

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
                .Setup(x => x.SendAnalytic(It.IsAny<IAnalytic>()))
                .Callback<IAnalytic>((data) => m_SentData = CopyUsageEventData((UnityEditor.VFX.VFXAnalytics.Analytic)data))
                .Returns(AnalyticsResult.Ok);
        }

        [TearDown]
        public void Cleanup()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
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
            m_EditorAnalyticsMock.Verify(x => x.SendAnalytic(It.IsAny<IAnalytic>()), Times.Once);

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
            m_EditorAnalyticsMock.Verify(x => x.SendAnalytic(It.IsAny<IAnalytic>()), Times.Once);

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
            m_EditorAnalyticsMock.Verify(x => x.SendAnalytic(It.IsAny<IAnalytic>()), Times.Once);

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
            m_EditorAnalyticsMock.Verify(x => x.SendAnalytic(It.IsAny<IAnalytic>()), Times.Once);

            Assert.AreEqual(1, m_SentData.nb_vfx_opened);
            Assert.AreEqual(VFXAnalytics.EventKind.Quit.ToString(), m_SentData.event_kind);
        }

        [Test]
        public void OnBuildReport_Test_Invalid_Path()
        {
            // Arrange
            IAnalytic sentData = null;
            var vfxAnalytics = new VFXAnalytics(m_EditorAnalyticsMock.Object);
            var buildReportMock = new Mock<IBuildReport>();
            buildReportMock.SetupGet(x => x.packedAssetsInfoPath).Returns(new[] { "Built-in Texture2D: sactx-0-512x1024-DXT5|BC3-New Sprite Atlas-41152f59" });
            m_EditorAnalyticsMock.Setup(x => x.SendAnalytic(It.IsAny<IAnalytic>())).Callback<IAnalytic>(x => sentData = x);

            // Act
            var buildReportMethodInfo = typeof(VFXAnalytics).GetMethod("OnPostprocessBuildInternal", BindingFlags.Instance | BindingFlags.NonPublic);
            buildReportMethodInfo.Invoke(vfxAnalytics, new object[] { buildReportMock.Object });

            // Assert
            m_EditorAnalyticsMock.Verify(x => x.SendAnalytic(It.IsAny<IAnalytic>()), Times.Once);
            Assert.NotNull(sentData);
            Assert.IsTrue(sentData.TryGatherData(out var data, out var errors));
            Assert.IsNull(errors);
            var vfxData = (VFXAnalytics.UsageEventData)data;
            Assert.AreEqual(0, vfxData.nb_vfx_assets);
        }

        [Test]
        public void OnBuildReport_Test_Duplicated_Paths()
        {
            // Arrange
            IAnalytic sentData = null;
            var vfxAnalytics = new VFXAnalytics(m_EditorAnalyticsMock.Object);
            var buildReportMock = new Mock<IBuildReport>();
            buildReportMock.SetupGet(x => x.packedAssetsInfoPath).Returns(new[] { "Assets/effect.vfx", "Assets/effect.vfx" });
            m_EditorAnalyticsMock.Setup(x => x.SendAnalytic(It.IsAny<IAnalytic>())).Callback<IAnalytic>(x => sentData = x);

            // Act
            var buildReportMethodInfo = typeof(VFXAnalytics).GetMethod("OnPostprocessBuildInternal", BindingFlags.Instance | BindingFlags.NonPublic);
            buildReportMethodInfo.Invoke(vfxAnalytics, new object[] { buildReportMock.Object });

            // Assert
            m_EditorAnalyticsMock.Verify(x => x.SendAnalytic(It.IsAny<IAnalytic>()), Times.Once);
            Assert.NotNull(sentData);
            Assert.IsTrue(sentData.TryGatherData(out var data, out var errors));
            Assert.IsNull(errors);
            var vfxData = (VFXAnalytics.UsageEventData)data;
            Assert.AreEqual(1, vfxData.nb_vfx_assets);
        }

        private VFXAnalytics.UsageEventData CopyUsageEventData(UnityEditor.VFX.VFXAnalytics.Analytic analytic)
        {
            VFXAnalytics.UsageEventData source = analytic.m_Data;
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
