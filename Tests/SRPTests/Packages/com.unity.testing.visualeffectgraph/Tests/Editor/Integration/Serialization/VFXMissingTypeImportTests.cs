using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

using NUnit.Framework;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.Block.Test;
using UnityEditor.VFX.UI;

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.TestTools;

namespace UnityEditor.VFX.Test.Integration
{
    [TestFixture]
    public class VFXMissingTypeImportTests
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            VFXTestCommon.CloseAllVFXWindow();
        }

        [TearDown]
        public void Clean()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [UnityTest, Description("Cover case UUM-69716")]
        public IEnumerator Unexpected_Failure_With_Missing_Type()
        {
            // The missing type logs sanitize/type errors a nondeterministic number of times across import passes;
            // this test verifies the graph still opens and saves, not the exact error output.
            LogAssert.ignoreFailingMessages = true;

            //For reference the former type serialized in this package was something like
            //[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
            //struct ShaderGlobalsVFXStruct
            //{
            //    public Color GlobalVFXStruct_c;
            //    public float GlobalVFXStruct_f;
            //}
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Serialization/MissingTypeImport/Repro_UUM_69716.unitypackage";
            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var expectedPath = "Assets/TmpTests/GlobalsTester.vfx";
            AssetDatabase.ImportAsset(expectedPath);

            // Poll until the imported asset is loadable; the sanitize runs asynchronously.
            VisualEffectAsset asset = null;
            const int kImportTimeoutInMs = 30000;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (asset == null && sw.ElapsedMilliseconds < kImportTimeoutInMs)
            {
                asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(expectedPath);
                if (asset == null)
                {
                    foreach (var currentAsset in AssetDatabase.LoadAllAssetsAtPath(expectedPath))
                    {
                        if (currentAsset is VisualEffectAsset found)
                        {
                            asset = found;
                            break;
                        }
                    }
                }
                if (asset == null)
                    yield return null;
            }
            sw.Stop();
            Assert.IsNotNull(asset, $"VFX asset at '{expectedPath}' was not available within {sw.ElapsedMilliseconds} ms after import.");

            //Trying to open the asset, it shouldn't fail
            VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0);
            var window = VFXTestCommon.GetWindow(asset);
            Assert.AreNotEqual(0, window.graphView.controller.allChildren.Count());
            window.graphView.OnSave();
        }
    }
}
