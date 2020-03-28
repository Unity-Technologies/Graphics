using System;
using NUnit.Framework;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace Tests
{
    public class ExpectedSettings
    {
        static IEnumerable GraphicsJobsValidBuildTargets
        {
            get
            {
                yield return new TestCaseData(BuildTarget.Android);
                yield return new TestCaseData(BuildTarget.iOS);
                yield return new TestCaseData(BuildTarget.Lumin);
                yield return new TestCaseData(BuildTarget.PS4);
                yield return new TestCaseData(BuildTarget.Stadia);
                yield return new TestCaseData(BuildTarget.StandaloneLinux64);
                yield return new TestCaseData(BuildTarget.StandaloneOSX);
                yield return new TestCaseData(BuildTarget.StandaloneWindows);
                yield return new TestCaseData(BuildTarget.StandaloneWindows64);
                yield return new TestCaseData(BuildTarget.Switch);
                yield return new TestCaseData(BuildTarget.tvOS);
                yield return new TestCaseData(BuildTarget.WebGL);
                yield return new TestCaseData(BuildTarget.WSAPlayer);
                yield return new TestCaseData(BuildTarget.XboxOne);
            }
        }

        private static bool GraphicsJobsEnabledByDefault(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneOSX:
                case BuildTarget.Android:
                case BuildTarget.iOS:
                case BuildTarget.Lumin:
                case BuildTarget.tvOS:
                case BuildTarget.WebGL:
                    return false;
                case BuildTarget.PS4:
                case BuildTarget.Stadia:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.Switch:
                case BuildTarget.WSAPlayer:
                case BuildTarget.XboxOne:
                    return true;
            }
            throw new System.ArgumentException("Unhandled BuildTarget case '" + buildTarget.ToString() + "'", nameof(buildTarget));
        }

        [Test]
        [TestCaseSource("GraphicsJobsValidBuildTargets")]
        public void GraphicsJobsDefaultSetting(BuildTarget buildTarget)
        {
            bool expectedValue = GraphicsJobsEnabledByDefault(buildTarget);
            bool actualValue = PlayerSettings.GetGraphicsJobsForPlatform(buildTarget);
            Assert.That(actualValue, Is.EqualTo(expectedValue));
        }

        [Test]
        public void Physics_AutoSyncTransformsShouldBeDisabled()
        {
            Assert.That(Physics.autoSyncTransforms, Is.False, "Physics.autoSyncTransforms should be OFF by default.");
        }

        [Test]
        public void Physics_ReuseCollisionCallbacksShouldBeEnabled()
        {
            Assert.That(Physics.reuseCollisionCallbacks, Is.True, "Physics.reuseCollisionCallbacks should be ON by default.");
        }

        [Test]
        public void Physics2D_AutoSyncTransformsShouldBeDisabled()
        {
            Assert.That(Physics2D.autoSyncTransforms, Is.False, "Physics2D.AutoSyncTransforms should be OFF by default.");
        }

        [Test]
        public void Physics2D_ReuseCollisionCallbacksShouldBeEnabled()
        {
            Assert.That(Physics2D.reuseCollisionCallbacks, Is.True, "Physics2D.reuseCollisionCallbacks should be ON by default.");
        }

        [Test]
        public void EditorSettings_SerializationModeShouldBeForceText()
        {
            Assert.That(EditorSettings.serializationMode, Is.EqualTo(SerializationMode.ForceText), "EditorSettings.serializationMode should be ForceText by default.");
        }

        [Test]
        public void ProjectSettings_ShouldBeTextSerialized()
        {
            foreach (var settingsFile in Directory.EnumerateFiles("ProjectSettings"))
            {
                var settingsFilename = Path.GetFileName(settingsFile);
                Assert.That(settingsFilename, Is.Not.Null, "Failed to get name of Settings File");
                if (settingsFilename.Equals("ProjectVersion.txt", StringComparison.OrdinalIgnoreCase))
                {
                    using (var fs = File.OpenRead(settingsFile))
                    {
                        const string projectVersionString = "m_EditorVersion";
                        byte[] buffer = new byte[projectVersionString.Length];
                        var read = fs.Read(buffer, 0, projectVersionString.Length);
                        Assert.That(read, Is.EqualTo(projectVersionString.Length), "Could not read string from ProjectVersion.txt");
                        var encoding = new UTF8Encoding(true);
                        Assert.That(encoding.GetString(buffer), Is.EqualTo(projectVersionString), "ProjectVersion.txt does not start with m_EditorVersion");
                    }
                }
                else if (settingsFilename.Equals("XRSettings.asset", StringComparison.OrdinalIgnoreCase))
                {
                    //This should be JSON serialised
                    using (var fs = File.OpenText(settingsFile))
                    {
                        var allText = fs.ReadToEnd();
                        Assert.That(allText.StartsWith("{"), Is.True, "XRSettings.asset is not text serialised");
                        Assert.That(allText.TrimEnd().EndsWith("}"), Is.True, "XRSettings.asset is not text serialised");
                    }
                }
                else if (Path.GetExtension(settingsFilename).Equals(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    using (var fs = File.OpenRead(settingsFile))
                    {
                        const string unityMagicString = "%YAML 1.1";
                        byte[] buffer = new byte[unityMagicString.Length];
                        var read = fs.Read(buffer, 0, unityMagicString.Length);
                        Assert.That(read, Is.EqualTo(unityMagicString.Length), "Could not read string from " + settingsFile);
                        var encoding = new UTF8Encoding(true);
                        Assert.That(encoding.GetString(buffer), Is.EqualTo(unityMagicString), $"{settingsFile} does not start with {unityMagicString}");
                    }
                }
                else
                {
                    throw new Exception($"Unexpected file found {settingsFile}");
                }
            }
        }
    }
}