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

namespace UnityEditor.VFX.Test.Integration.Serialization
{
    [TestFixture]
    public class VFXSerializationHDRPTests
    {
        private bool m_CreatedTestAsset;
        private bool m_CreatedTestAssetDirectory;

        private static readonly string s_Modify_SG_Property_VFX = "Assets/AllTests/Editor/Tests/Modify_SG_Property.vfx";
        private static readonly string s_Modify_SG_Property_SG_A = "Assets/AllTests/Editor/Tests/Modify_SG_Property_A.shadergraph";
        private static readonly string s_Modify_SG_Property_SG_B = "Assets/AllTests/Editor/Tests/Modify_SG_Property_B.shadergraph";
        private string m_Modify_SG_Property_VFX;
        private string m_Modify_SG_Property_SG_A;
        private string m_Modify_SG_Property_SG_B;

        [OneTimeSetUp]
        public void Setup()
        {
            VFXSerializationTestHelper.CreateSeedAsset(out m_CreatedTestAsset, out m_CreatedTestAssetDirectory);
            m_Modify_SG_Property_VFX = File.ReadAllText(s_Modify_SG_Property_VFX);
            m_Modify_SG_Property_SG_A = File.ReadAllText(s_Modify_SG_Property_SG_A);
            m_Modify_SG_Property_SG_B = File.ReadAllText(s_Modify_SG_Property_SG_B);
        }

        //Cover regression 1361601
        [UnityTest]
        public IEnumerator Modify_ShaderGraph_Property_Check_VFX_Compilation_Doesnt_Fail()
        {
            Assert.IsTrue(m_Modify_SG_Property_SG_A.Contains("Name_A"));
            Assert.IsFalse(m_Modify_SG_Property_SG_A.Contains("Name_B"));
            Assert.IsTrue(m_Modify_SG_Property_SG_B.Contains("Name_B"));
            Assert.IsFalse(m_Modify_SG_Property_SG_B.Contains("Name_A"));
            Assert.IsTrue(m_Modify_SG_Property_VFX.Contains("Name_A"));
            Assert.IsFalse(m_Modify_SG_Property_VFX.Contains("Name_B"));

            AssetDatabase.ImportAsset(s_Modify_SG_Property_VFX);

            //Actually, rename the exposed property "Name_A" into "Name_B"
            File.WriteAllText(s_Modify_SG_Property_SG_A, m_Modify_SG_Property_SG_B);
            yield return null;

            //These import aren't suppose to trigger an exception
            AssetDatabase.ImportAsset(s_Modify_SG_Property_SG_A);
            AssetDatabase.ImportAsset(s_Modify_SG_Property_VFX);

            yield return null;

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(s_Modify_SG_Property_VFX);
            var graph = asset.GetResource().GetGraph();
            graph.PrepareGraph();
            graph.GetResource().WriteAsset();

            var newVFXContent = File.ReadAllText(s_Modify_SG_Property_VFX);
            Assert.IsTrue(newVFXContent.Contains("Name_B"));
        }

        [UnityTest, Description("Cover regression UUM-563")]
        public IEnumerator ShaderGraph_Not_Reverted_On_Save()
        {
            // Create empty graph
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXTestCommon.GetWindow(graph.GetResource(), true);
            var viewController = VFXViewController.GetController(graph.GetResource(), true);
            window.graphView.controller = viewController;
            yield return null;

            // Add a static mesh output
            var staticMeshOutputContextDesc = VFXLibrary.GetContexts().Single(x => x.modelType == typeof(VFXStaticMeshOutput));
            var staticMeshOutputContext = (VFXStaticMeshOutput)window.graphView.controller.AddVFXContext(Vector2.zero, staticMeshOutputContextDesc.variant);
            var shaderGraph = AssetDatabase.LoadAssetAtPath<Shader>("Assets/AllTests/Editor/Tests/Modify_SG_Property_A.shadergraph");
            staticMeshOutputContext.SetSettingValue("shader", shaderGraph);
            window.graphView.OnSave();
            yield return null;

            // Check that the shader is correctly assigned
            var s = staticMeshOutputContext.GetSetting("shader").value;
            Assert.NotNull(staticMeshOutputContext.GetSetting("shader").value);

            // Change shader to None
            staticMeshOutputContext.SetSettingValue("shader", null);
            window.graphView.OnSave();
            yield return null;

            // Check that the shader is still set to None
            Assert.IsNull(staticMeshOutputContext.GetSetting("shader").value, "The shader was expected to be null but it didn't. Probably the previous value has been restored when saving");
        }

        [UnityTest, Description("Cover case UUM-553")]
        public IEnumerator Unexpected_Import_Issue_With_Diffusion_Profile()
        {
            var packagePath = "Assets/AllTests/Editor/Tests/Import_Diffusion_Profile_Repro_553.unitypackage";
            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            //Shouldn't log Repro_553_EmptySG_SSR_DiffProfile.shadergraph has been scheduled for reimport during the Refresh loop and Loading of it has been attempted.
            for (int i = 0; i < 4; ++i)
                yield return null;
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            try
            {
                File.WriteAllText(s_Modify_SG_Property_VFX, m_Modify_SG_Property_VFX);
                File.WriteAllText(s_Modify_SG_Property_SG_A, m_Modify_SG_Property_SG_A);
                File.WriteAllText(s_Modify_SG_Property_SG_B, m_Modify_SG_Property_SG_B);

                VFXTestCommon.DeleteAllTemporaryGraph();
            }
            finally
            {
                VFXSerializationTestHelper.CleanupSeedAsset(m_CreatedTestAsset, m_CreatedTestAssetDirectory);
            }
        }
    }
}
