using NUnit.Framework;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    partial class RenderingDebuggerTests
    {
        /// <summary>
        /// Class for the rendering debugger settings.
        /// </summary>
        public class TestDebugDisplaySettings : DebugDisplaySettings<TestDebugDisplaySettings>
        {
            public abstract class TestDebugDisplaySettingsData : IDebugDisplaySettingsData
            {
                #region IDebugDisplaySettingsData

                /// <inheritdoc/>
                public bool AreAnySettingsActive => false;

                /// <inheritdoc/>
                public bool IsPostProcessingAllowed => true;

                /// <inheritdoc/>
                public bool IsLightingActive => true;

                public abstract IDebugDisplaySettingsPanelDisposable CreatePanel();
                public bool TryGetScreenClearColor(ref UnityEngine.Color _) => false;

                #endregion
            }

            class Test1Panel : TestDebugDisplaySettingsData
            {
                [DisplayInfo(name = "Test 1", order = 1)]
                private class StatsPanel : DebugDisplaySettingsPanel
                {
                    public override DebugUI.Flags Flags => DebugUI.Flags.RuntimeOnly;

                    public StatsPanel()
                    {
                        AddWidget(new DebugUI.BoolField() { displayName = "element", flags = DebugUI.Flags.FrequentlyUsed });
                    }
                }

                /// <inheritdoc/>
                public override IDebugDisplaySettingsPanelDisposable CreatePanel()
                {
                    return new StatsPanel();
                }
            }

            class Test2Panel : TestDebugDisplaySettingsData
            {
                [DisplayInfo(name = "Test 2", order = 2)]
                private class StatsPanel : DebugDisplaySettingsPanel
                {
                    public override DebugUI.Flags Flags => DebugUI.Flags.RuntimeOnly;

                    public StatsPanel()
                    {
                        AddWidget(new DebugUI.BoolField() { displayName = "element", flags = DebugUI.Flags.FrequentlyUsed });
                    }
                }

                /// <inheritdoc/>
                public override IDebugDisplaySettingsPanelDisposable CreatePanel()
                {
                    return new StatsPanel();
                }
            }

            class Test3Panel : TestDebugDisplaySettingsData
            {
                [DisplayInfo(name = "Test 3", order = 3)]
                private class StatsPanel : DebugDisplaySettingsPanel
                {
                    public override DebugUI.Flags Flags => DebugUI.Flags.RuntimeOnly;

                    public StatsPanel()
                    {
                        AddWidget(new DebugUI.BoolField() { displayName = "element", flags = DebugUI.Flags.FrequentlyUsed });
                    }
                }

                /// <inheritdoc/>
                public override IDebugDisplaySettingsPanelDisposable CreatePanel()
                {
                    return new StatsPanel();
                }
            }


            public TestDebugDisplaySettings()
            {
                Reset();
            }

            /// <inheritdoc/>
            public override void Reset()
            {
                m_Settings.Clear();

                // Add them in an unsorted way
                Add(new Test3Panel());
                Add(new Test1Panel());
                Add(new Test2Panel());
            }
        }

        [Test]
        public void TestOrderAndPanelName()
        {
            var debugDisplaySettingsUI = new DebugDisplaySettingsUI();
            debugDisplaySettingsUI.RegisterDebug(TestDebugDisplaySettings.Instance);

            var panelTest1Index = DebugManager.instance.PanelIndex("Test 1");
            Assert.IsTrue(panelTest1Index >= 0);
            var panelTest2Index = DebugManager.instance.PanelIndex("Test 2");
            Assert.IsTrue(panelTest2Index >= 0);
            var panelTest3Index = DebugManager.instance.PanelIndex("Test 3");
            Assert.IsTrue(panelTest3Index >= 0);

            Assert.True(panelTest1Index < panelTest2Index);
            Assert.True(panelTest2Index < panelTest3Index);

            debugDisplaySettingsUI.UnregisterDebug();
        }
    }
}
