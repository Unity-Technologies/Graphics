using System;
using System.Collections;
using System.Text;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.VFX.Block;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VersionControl;
using UnityEngine.TestTools;

namespace UnityEditor.VFX.Test.Integration
{
    [TestFixture]
    public class VFXShaderGenerationHDRPTests
    {
        [OneTimeTearDown]
        public void TearDown()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [Test]
        public void ShaderGraph_Opaque_Expecting_No_Sorting()
        {
            var vfxPath = VFXShaderGenerationTestHelpers.ShaderGraphOutputPrepare("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest-unlit-opaque.shadergraph_1", VFXAbstractRenderedOutput.BlendMode.Alpha);
            VFXShaderGenerationTestHelpers.ShaderGraphSortingVerify(vfxPath, false);
        }

        [Test]
        public void ShaderGraph_Opaque_Expecting_Sorting_When_Forced()
        {
            var vfxPath = VFXShaderGenerationTestHelpers.ShaderGraphOutputPrepare("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest-unlit-opaque.shadergraph_1", VFXAbstractRenderedOutput.BlendMode.Alpha, VFXAbstractParticleOutput.SortActivationMode.On);
            VFXShaderGenerationTestHelpers.ShaderGraphSortingVerify(vfxPath, true);
        }

        [Test]
        public void ShaderGraph_AlphaBlend_Expecting_Sorting()
        {
            var vfxPath = VFXShaderGenerationTestHelpers.ShaderGraphOutputPrepare("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest-unlit-alphablend.shadergraph_1", VFXAbstractRenderedOutput.BlendMode.Opaque);
            VFXShaderGenerationTestHelpers.ShaderGraphSortingVerify(vfxPath, true);
        }

        [Test]
        public void ShaderGraph_Interpolators_Generation()
        {
            string vfxPath = "Assets/AllTests/Editor/Tests/SGInterpolatorTest.vfx";
            AssetDatabase.ImportAsset(vfxPath);
            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath).GetResource();

            var quadOutputSrc = vfx.GetShaderSource(2);
            var meshOutputSrc = vfx.GetShaderSource(3);

            void CheckShaderStructs(string source, uint[] expectedResults)
            {
                var graphPropertiesFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(source, "GraphProperties", "ForwardOnly");
                var fragInputFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(source, "FragInputsVFX", "ForwardOnly");
                var varyingsFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(source, "VaryingsMeshToPS", "ForwardOnly");
                var packedVaryingFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(source, "PackedVaryingsMeshToPS", "ForwardOnly");

                Assert.AreEqual(expectedResults[0], graphPropertiesFields.Length);
                Assert.AreEqual(expectedResults[1], fragInputFields.Length);
                Assert.AreEqual(expectedResults[2], varyingsFields.Length);
                Assert.AreEqual(expectedResults[3], packedVaryingFields.Length);

                Assert.IsTrue(varyingsFields.Any(f => f.name == "_FragPerElement_i"));
                Assert.IsTrue(varyingsFields.Any(f => f.name == "_FragPerElement_f2"));
            }

            //Instancing is expected to be enabled for most of SG Output
            Assert.IsTrue(quadOutputSrc.Contains("#pragma multi_compile_instancing"));
            Assert.IsTrue(meshOutputSrc.Contains("#pragma multi_compile_instancing"));

            CheckShaderStructs(quadOutputSrc, new uint[] { 9, 5, 6, 6 });
            CheckShaderStructs(meshOutputSrc, new uint[] { 9, 5, 7, 6 });
        }

        [Test]
        public void ShaderGraph_Insure_GlobalProperties_No_Leak_In_Interpolator()
        {
            var vfxPath = "Assets/AllTests/VFXTests/GraphicsTests/35_ShaderGraphGenerationFTP/VFX/VFX - GlobalShaderProperty.vfx";
            AssetDatabase.ImportAsset(vfxPath);
            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath).GetResource();
            Assert.IsNotNull(vfx);

            var source = vfx.GetShaderSource(2);
            Assert.IsTrue(source.Contains("float4 _VFXGlobalColor;"));

            var graphPropertiesFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(source, "GraphProperties", "ForwardOnly");
            var fragInputFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(source, "FragInputsVFX", "ForwardOnly");
            var varyingsFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(source, "VaryingsMeshToPS", "ForwardOnly");
            var packedVaryingFields = VFXTestShaderSrcUtils.GetStructFieldsFromSource(source, "PackedVaryingsMeshToPS", "ForwardOnly");

            Assert.AreEqual(0, graphPropertiesFields.Length);
            Assert.AreEqual(0, fragInputFields.Length);
            Assert.AreEqual(3, varyingsFields.Length);
            Assert.AreEqual(3, packedVaryingFields.Length);

            Assert.AreEqual("positionCS", varyingsFields[0].name);
            Assert.AreEqual("positionRWS", varyingsFields[1].name);
            Assert.AreEqual("instanceID", varyingsFields[2].name);

            Assert.AreEqual("positionCS", packedVaryingFields[0].name);
            Assert.AreEqual("positionRWS", packedVaryingFields[1].name);
            Assert.AreEqual("instanceID", packedVaryingFields[2].name);
        }

        public class WrapperWindow : EditorWindow
        {
            public Action onGUIDelegate;
            public bool testRun;

            public void OnGUI()
            {
                try
                {
                    onGUIDelegate.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                }
                testRun = true;
                Close();
            }
        }

        [UnityTest, Description("Cover UUM-8053")]
        public IEnumerator ShaderGraph_Unlit_Transparent_Inspector()
        {
            var vfxPath = VFXShaderGenerationTestHelpers.ShaderGraphOutputPrepare("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest-unlit-alphablend.shadergraph_1", VFXAbstractRenderedOutput.BlendMode.Alpha, VFXAbstractParticleOutput.SortActivationMode.On);
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            var outputContext = ((VFXGraph)vfxAsset.GetResource().graph).children.OfType<VFXAbstractParticleOutput>().FirstOrDefault();

            var window = ScriptableObject.CreateInstance<WrapperWindow>();
            window.position = new Rect(0, 0, 512, 512);
            window.onGUIDelegate += () =>
            {
                var editor = Editor.CreateEditor(outputContext);
                editor.serializedObject.Update();
                editor.OnInspectorGUI();
                editor.serializedObject.ApplyModifiedProperties();
            };
            window.Show();
            yield return null;

            Assert.IsTrue(window.testRun);
            ScriptableObject.DestroyImmediate(window);
        }
    }
}
