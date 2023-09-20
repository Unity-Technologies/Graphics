using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;

namespace RenderPipelineGraphicsSettings
{
    [TestFixture]
    class StripperTests
    {
        #region Strippers

        static HashSet<Type> s_EnabledStrippers = new();

        private struct ActiveStrippersScope : IDisposable
        {
            private readonly List<FieldInfo> m_Fields;

            public ActiveStrippersScope(List<Type> activeStrippers)
            {
                m_Fields = new List<FieldInfo>();
                foreach (var stripper in activeStrippers)
                {
                    var field = stripper.GetField("k_Invoked", BindingFlags.Static | BindingFlags.Public);
                    if (field != null)
                    {
                        field.SetValue(null, false);
                        m_Fields.Add(field);
                        s_EnabledStrippers.Add(stripper);
                    }
                }
            }

            public void Dispose()
            {
                foreach (var field in m_Fields)
                {
                    Assert.IsTrue((bool)field.GetValue(null), "CanRemoveSettings was not invoked");
                }

                m_Fields.Clear();
                s_EnabledStrippers.Clear();
            }
        }

        class RemoveAStripper : IRenderPipelineGraphicsSettingsStripper<A>
        {
            public static bool k_Invoked = false;

            public bool active => s_EnabledStrippers.Contains(typeof(RemoveAStripper));

            public bool CanRemoveSettings(A setting)
            {
                k_Invoked = true;
                return true;
            }
        }

        class KeepAStripper : IRenderPipelineGraphicsSettingsStripper<A>
        {
            public static bool k_Invoked = false;

            public bool active => s_EnabledStrippers.Contains(typeof(KeepAStripper));

            public bool CanRemoveSettings(A setting)
            {
                k_Invoked = true;
                return false;
            }
        }

        class PrivateCtorAStripper : IRenderPipelineGraphicsSettingsStripper<A>
        {
            public bool active => s_EnabledStrippers.Contains(typeof(PrivateCtorAStripper));

            private PrivateCtorAStripper()
            {

            }

            public bool CanRemoveSettings(A setting)
            {
                return true;
            }
        }

        class WrongStripper : IStripper
        {
            public bool active => s_EnabledStrippers.Contains(typeof(PrivateCtorAStripper));
        }

        #endregion

        #region SettingsDefinitions

        class StripperTestsAsset : RenderPipelineAsset
        {
            protected override RenderPipeline CreatePipeline()
            {
                throw new NotImplementedException();
            }
        }

        [Serializable]
        [SupportedOnRenderPipeline(typeof(StripperTestsAsset))]
        public abstract class Base : IRenderPipelineGraphicsSettings
        {
            int IRenderPipelineGraphicsSettings.version => 0;
            public string name;
            public Base() => name = GetType().Name;
            public override string ToString() => name;

            public virtual bool isAvailableInPlayerBuild => false;
        }

        [Serializable]
        class A : Base
        {
            public override bool isAvailableInPlayerBuild => true;
        }

        [Serializable]
        class B : Base
        {
            public override bool isAvailableInPlayerBuild => true;
        }

        [Serializable]
        class C : Base
        {
        }

        #endregion

        private static A k_A = new A();
        private static B k_B = new B();
        private static C k_C = new C();

        static string k_ResultsDirectory = "Packages/com.unity.render-pipelines.core/Tests/Editor/RenderPipeline/SettingsStrippingResults";

        [SetUp]
        public void Setup()
        {
            if (k_UpdateStrippingResult && !Directory.Exists(k_ResultsDirectory))
                Directory.CreateDirectory(k_ResultsDirectory);
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(
                    new List<IRenderPipelineGraphicsSettings>(),
                    new List<Type>())
                .SetName("Given no settings and no strippers, nothing is performed")
                .Returns(new List<IRenderPipelineGraphicsSettings>()),

            new TestCaseData(
                    new List<IRenderPipelineGraphicsSettings>() { k_A, k_B, k_C },
                    new List<Type>())
                .SetName(
                    "Given some settings without any stripper active, only settings that must be available on Player build are kept")
                .Returns(new List<IRenderPipelineGraphicsSettings>() { k_A, k_B }),

            new TestCaseData(
                    new List<IRenderPipelineGraphicsSettings>() { k_A, k_B, k_C },
                    new List<Type>() { typeof(WrongStripper) })
                .SetName("Given an wrong stripper, the stripper is not taken into account.")
                .Returns(new List<IRenderPipelineGraphicsSettings>() { k_A, k_B }),

            new TestCaseData(
                    new List<IRenderPipelineGraphicsSettings>() { k_A, k_B, k_C },
                    new List<Type>() { typeof(PrivateCtorAStripper) })
                .SetName("Given an user stripper with private constructor, the stripper is not taken into account.")
                .Returns(new List<IRenderPipelineGraphicsSettings>() { k_A, k_B }),

            new TestCaseData(
                    new List<IRenderPipelineGraphicsSettings>() { k_A, k_B, k_C },
                    new List<Type>() { typeof(RemoveAStripper) })
                .SetName("Given an user stripper, the default behaviour of the setting is overriden")
                .Returns(new List<IRenderPipelineGraphicsSettings>() { k_B }),

            new TestCaseData(
                    new List<IRenderPipelineGraphicsSettings>() { k_A, k_B, k_C },
                    new List<Type>() { typeof(KeepAStripper), typeof(RemoveAStripper) })
                .SetName("Given a settings keeper and a settings stripper, the setting is kept")
                .Returns(new List<IRenderPipelineGraphicsSettings>() { k_A, k_B })
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public List<IRenderPipelineGraphicsSettings> DoStripping(List<IRenderPipelineGraphicsSettings> settings, List<Type> activeStrippers)
        {
            using (new ActiveStrippersScope(activeStrippers))
            {
                var runtimeSettings = new List<IRenderPipelineGraphicsSettings>();
                RenderPipelineGraphicsSettingsStripper.PerformStripping(settings, runtimeSettings);
                return runtimeSettings;
            }
        }

        static bool k_UpdateStrippingResult = false;

        private void CheckForceUpdate(string path, string loggedStrippedResult)
        {
            if (k_UpdateStrippingResult)
            {
                if (File.Exists(path))
                    File.Delete(path);
                File.WriteAllText(path, loggedStrippedResult);
            }
            Assert.IsFalse(k_UpdateStrippingResult, "Note to Developer: You forgot to set back the variable `k_UpdateStrippingResult` to false");
        }

        private string GenerateFilePath(List<IRenderPipelineGraphicsSettings> settings, List<Type> activeStrippers)
        {
            string activeStrippersFilePath = activeStrippers.Count != 0
                    ? string.Join('_', activeStrippers.Select(i => i.Name))
                    : "no_strippers_active";

            string settingsFilePath = settings.Count != 0
                ? string.Join('_', settings.Select(i => i.GetType().Name))
                : "no_settings";

            return Path.GetFullPath(Path.Combine(k_ResultsDirectory, $"{activeStrippersFilePath}_{settingsFilePath}.json"));
        }

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public List<IRenderPipelineGraphicsSettings> JSONOutput(List<IRenderPipelineGraphicsSettings> settings, List<Type> activeStrippers)
        {
            var runtimeSettings = DoStripping(settings, activeStrippers);

            string path = GenerateFilePath(settings, activeStrippers);
            var loggedStrippedResult = File.ReadAllText(RenderPipelineGraphicsSettingsStripper.Report.k_OutputPath);

            CheckForceUpdate(path, loggedStrippedResult);
            Assert.AreEqual(File.ReadAllText(path), loggedStrippedResult, "Note to Developer: You can set the variable `k_UpdateStrippingResult` to true, to override the output if you changed what is reported");

            return runtimeSettings;
        }

        [Test]
        public void NullArgumentsThrowsException()
        {
            var initializedList = new List<IRenderPipelineGraphicsSettings>();
            var exception = Assert.Throws<ArgumentNullException>(() =>RenderPipelineGraphicsSettingsStripper.PerformStripping(null, initializedList));
            Assert.AreEqual("settingsList", exception.ParamName);

            exception = Assert.Throws<ArgumentNullException>(() => RenderPipelineGraphicsSettingsStripper.PerformStripping(initializedList, null));
            Assert.AreEqual("runtimeSettingsList", exception.ParamName);

        }
    }
}
