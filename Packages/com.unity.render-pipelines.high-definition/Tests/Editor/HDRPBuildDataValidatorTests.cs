using System;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Tests
{
    class HDRPBuildDataValidatorTests
    {
        [SetUp]
        public void Setup()
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset)
                Assert.Ignore("This is an HDRP Tests, and the current pipeline is not HDRP.");
        }

        [Test]
        public void ValidatePlatform()
        {
            using (UnityEngine.Pool.GenericPool<StringBuilder>.Get(out var stb))
            {
                UnityEditor.BuildTarget[] buildTargets = (UnityEditor.BuildTarget[])Enum.GetValues(typeof(UnityEditor.BuildTarget));
                foreach (var buildTarget in buildTargets)
                {
                    FieldInfo fieldInfo = typeof(UnityEditor.BuildTarget).GetField(buildTarget.ToString());
                    if(fieldInfo.GetCustomAttribute<ObsoleteAttribute>() != null)
                        continue;
                    
                    stb.Clear();
                    HDRPBuildDataValidator.ValidatePlatform(buildTarget, stb);
                    bool isSupported = HDUtils.IsSupportedBuildTargetAndDevice(buildTarget, out GraphicsDeviceType _);
                    if (isSupported)
                        Assert.IsEmpty(stb.ToString());
                    else
                        Assert.IsNotEmpty(stb.ToString());
                }
            }
        }

        [Test]
        public void ValidateRenderPipelineAssetsAreAtLastVersionReturnsTrue()
        {
            using (UnityEngine.Pool.ListPool<HDRenderPipelineAsset>.Get(out var list))
            {
                var asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
                list.Add(asset);

                using (UnityEngine.Pool.GenericPool<StringBuilder>.Get(out var stb))
                {
                    stb.Clear();

                    HDRPBuildDataValidator.ValidateRenderPipelineAssetsAreAtLastVersion(list, stb);
                    Assert.IsTrue(string.IsNullOrEmpty(stb.ToString()));
                }

                ScriptableObject.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ValidateRenderPipelineAssetsAreNotAtLastVersionReturnsFalse()
        {
            using (UnityEngine.Pool.ListPool<HDRenderPipelineAsset>.Get(out var list))
            {
                var asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
                list.Add(asset);

                using (UnityEngine.Pool.GenericPool<StringBuilder>.Get(out var stb))
                {
                    stb.Clear();
                    asset.m_Version = HDRenderPipelineAsset.Version.First;
                    HDRPBuildDataValidator.ValidateRenderPipelineAssetsAreAtLastVersion(list, stb);
                    Assert.IsFalse(string.IsNullOrEmpty(stb.ToString()));
                }

                ScriptableObject.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ValidateNullRenderPipelineGlobalSettingsReturnsFalse()
        {
            using (UnityEngine.Pool.GenericPool<StringBuilder>.Get(out var stb))
            {
                stb.Clear();
                HDRPBuildDataValidator.ValidateRenderPipelineGlobalSettings(null, stb);
                Assert.IsFalse(string.IsNullOrEmpty(stb.ToString()));
            }
        }

        [Test]
        public void ValidateRenderPipelineGlobalSettingsNotAtLastVersionReturnsFalse()
        {
            using (UnityEngine.Pool.GenericPool<StringBuilder>.Get(out var stb))
            {
                stb.Clear();
                var asset = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettings>();
                asset.m_Version = HDRenderPipelineGlobalSettings.Version.First;
                HDRPBuildDataValidator.ValidateRenderPipelineGlobalSettings(asset, stb);
                ScriptableObject.DestroyImmediate(asset);

                Assert.IsFalse(string.IsNullOrEmpty(stb.ToString()));
            }
        }

        [Test]
        public void ValidateRenderPipelineGlobalSettingsIsAtLastVersionReturnsTrue()
        {
            using (UnityEngine.Pool.GenericPool<StringBuilder>.Get(out var stb))
            {
                stb.Clear();
                var asset = ScriptableObject.CreateInstance<HDRenderPipelineGlobalSettings>();
                HDRPBuildDataValidator.ValidateRenderPipelineGlobalSettings(asset, stb);
                ScriptableObject.DestroyImmediate(asset);

                Assert.IsTrue(string.IsNullOrEmpty(stb.ToString()));
            }
        }
    }
}
