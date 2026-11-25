using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.HLSL
{
    using CustomHLSL = UnityEditor.VFX.Block.CustomHLSL;

    [TestFixture]
    class VFXCustomHLSLBlockAttributeTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            VFXTestCommon.CloseAllVFXWindow();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        const string defaultHlslCode =
            "void TestFunction(inout VFXAttributes attributes, in float3 offset, in float speed)\n" +
            "{\n" +
            "  attributes.position += offset;\n" +
            "  attributes.velocity = float3(0, 0, speed);\n" +
            "}";

        [Test]
        public void Check_CustomHLSL_Block_Attribute_Access_Mode()
        {
            // Arrange
            var hlslBlock = VFXCustomHLSLTestHelper.CreateCustomHLSLBlock();
            hlslBlock.SetSettingValue("m_HLSLCode", defaultHlslCode);

            // Act
            hlslBlock.Invalidate(VFXModel.InvalidationCause.kSettingChanged);

            // Assert
            var expectedAttributes = new[] { VFXAttributeMode.ReadWrite, VFXAttributeMode.Write };
            Assert.AreEqual(expectedAttributes, hlslBlock.attributes.Select(x => x.mode).ToArray());
        }

        [Test]
        public void Check_CustomHLSL_Block_Attribute_Read_Access()
        {
            // Arrange
            var hlslCode =
                "void TestFunction(inout VFXAttributes attributes, in float3 offset, in float speedFactor)\n" +
                "{\n" +
                    "/* 01 */ float f = attributes.lifetime;\n" +
                    "/* 02 */ float g = Function(attributes.scaleX);\n" +
                    "/* 03 */ float v = Function(attributes.position.x);\n" +
                    "/* 04 */ float i = Function2(attributes.velocity.x, attributes.scaleY);\n" +
                    "/* 05 */ if (attributes.scaleZ > 0);\n" +
                    "/* 06 */ if (attributes.seed < 0);\n" +
                    "/* 07 */ if (attributes.age == 0);\n" +
                    "/* 08 */ if (attributes.texIndex == 0 && attributes.direction.z > 0);\n" +
                    "/* 09 */ if (4 < attributes.particleId);\n" +
                    "/* 10 */ if (attributes.alive);\n" +
                    "/* 11 */ if (4 <= attributes.pivotX);\n" +
                    "/* 12 */ if (4 >= attributes.pivotY);\n" +
                "}";

            var hlslBlock = VFXCustomHLSLTestHelper.CreateCustomHLSLBlock();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            // Act
            hlslBlock.Invalidate(VFXModel.InvalidationCause.kSettingChanged);

            // Assert
            Assert.AreEqual(14, hlslBlock.attributes.Count());
            var expectedAttributes = new[] { "lifetime", "scaleX", "position", "velocity", "scaleY", "scaleZ", "seed", "age", "texIndex", "direction", "particleId", "alive", "pivotX", "pivotY" };
            CollectionAssert.AreEquivalent(expectedAttributes, hlslBlock.attributes.Select(x => x.attrib.name));
            Assert.IsTrue(hlslBlock.attributes.All(x => x.mode == VFXAttributeMode.Read));
        }

        [Test, Description("Covers issue UUM-79389")]
        public void Check_CustomHLSL_Block_Attribute_Read_SubField_Access()
        {
            // Arrange
            var hlslCode =
                "void Test(inout VFXAttributes attributes, in float value, in float altSize)\n" +
                "{\n" +
                "    if (attributes.position.y >= value)\n" +
                "    {\n" +
                "        attributes.size = altSize;\n" +
                "    }\n" +
                "}";

            var hlslBlock = VFXCustomHLSLTestHelper.CreateCustomHLSLBlock();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            // Act
            hlslBlock.Invalidate(VFXModel.InvalidationCause.kSettingChanged);

            // Assert
            Assert.AreEqual(2, hlslBlock.attributes.Count());
            var sizeAttribute = hlslBlock.attributes.First();
            Assert.AreEqual(sizeAttribute.attrib.name, "size");
            Assert.AreEqual(sizeAttribute.mode, VFXAttributeMode.Write);

            var positionAttribute = hlslBlock.attributes.Last();
            Assert.AreEqual(positionAttribute.attrib.name, "position");
            Assert.AreEqual(positionAttribute.mode, VFXAttributeMode.Read);
        }

        [Test]
        public void Check_CustomHLSL_Block_Attribute_Write_Access()
        {
            // Arrange
            var hlslCode =
                "void TestFunction(inout VFXAttributes attributes, in float3 offset, in float speedFactor)\n" +
                "{\n" +
                    "/* 01 */ attributes.position = float3(1, 2, 3);\n" +
                    "/* 02 */ attributes.direction.x = 4;\n" +
                    "/* 03 */ attributes.velocity = attributes.targetPosition = float3(0, 0, 0);\n" +
                "}";

            var hlslBlock = VFXCustomHLSLTestHelper.CreateCustomHLSLBlock();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            // Act
            hlslBlock.Invalidate(VFXModel.InvalidationCause.kSettingChanged);

            // Assert
            Assert.AreEqual(4, hlslBlock.attributes.Count());
            var expectedAttributes = new[] { "position", "velocity", "targetPosition", "direction" };
            CollectionAssert.AreEquivalent(expectedAttributes, hlslBlock.attributes.Select(x => x.attrib.name));
            Assert.IsTrue(hlslBlock.attributes.All(x => x.mode == VFXAttributeMode.Write));
        }

        [Test]
        public void Check_CustomHLSL_Block_Attribute_ReadWrite_Access()
        {
            // Arrange
            var hlslCode =
                "void TestFunction(inout VFXAttributes attributes, in float3 offset, in float speedFactor)\n" +
                "{\n" +
                    "/* 01 */ attributes.lifetime += 4;\n" +
                    "/* 02 */ attributes.scaleX -= 4;\n" +
                    "/* 03 */ attributes.scaleY *= 4;\n" +
                    "/* 04 */ attributes.scaleZ /= 4;\n" +
                    "/* 05 */ attributes.angleX %= 4;\n" +
                    "/* 06 */ attributes.angleY++;\n" +
                    "/* 07 */ attributes.angleZ--;\n" +
                    "/* 08 */ attributes.pivotX <<= 1;\n" +
                    "/* 09 */ attributes.pivotY >>= 1;\n" +
                    // Todo: Not detected ??
                    //"/* 10 */ ++attributes.AngularVelocityX;\n" +
                    //"/* 11 */ --attributes.AngularVelocityY;\n" +
                "}";

            var hlslBlock = VFXCustomHLSLTestHelper.CreateCustomHLSLBlock();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            // Act
            hlslBlock.Invalidate(VFXModel.InvalidationCause.kSettingChanged);

            // Assert
            Assert.AreEqual(9, hlslBlock.attributes.Count());
            var expectedAttributes = new[] { "lifetime", "scaleX", "scaleY", "scaleZ", "angleX", "angleY", "angleZ", "pivotX", "pivotY", /*"AngularVelocityX", "AngularVelocityY"*/ };
            CollectionAssert.AreEquivalent(expectedAttributes, hlslBlock.attributes.Select(x => x.attrib.name));
            Assert.IsTrue(hlslBlock.attributes.All(x => x.mode == VFXAttributeMode.ReadWrite));
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_Use_CustomAttribute()
        {
            // Arrange
            var hlslCode =
                "void TestFunction_Use_CustomAttribute(in VFXAttributes attributes, in float param)\n" +
                "{\n" +
                "    attributes.position = attributes.custom * param;\n" +
                "}";
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();

            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph);
            Assert.IsTrue(graph.TryAddCustomAttribute("custom", VFXValueType.Float3, null, false, out var attribute));

            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode); //Modify m_HLSLCode after adding custom, we shouldn't try to compile invalid blocks

            // Act
            VFXTestCommon.ReimportVFXGraph(graph);

            //Wait a few frame to be sure shader compilation occured
            for (int frame = 0; frame < 4; ++frame)
                yield return null;

            // Assert
            Assert.Pass("No exception should be raised");
        }

        [UnityTest]
        public IEnumerator Check_CustomHLSL_Block_Use_CustomAttribute_HLSL_File()
        {
            // Arrange
            var hlslCode =
                "void TestFunction_CustomAttribute_HLSL_File(in VFXAttributes attributes, in float param)\n" +
                "{\n" +
                "    attributes.position = attributes.custom * param;\n" +
                "}";

            Directory.CreateDirectory(VFXTestCommon.tempBasePath);
            var hlslFilePath = $"{VFXTestCommon.tempBasePath}/mycode_{Guid.NewGuid().ToString()}.hlsl";
            File.WriteAllText(hlslFilePath, hlslCode);
            AssetDatabase.ImportAsset(hlslFilePath);

            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph);
            Assert.IsTrue(graph.TryAddCustomAttribute("custom", VFXValueType.Float3, null, false, out var attribute));

            var shaderInclude = AssetDatabase.LoadAssetAtPath<ShaderInclude>(hlslFilePath);
            hlslBlock.SetSettingValue("m_ShaderFile", shaderInclude);

            // Act
            VFXTestCommon.ReimportVFXGraph(graph);

            //Wait a few frame to be sure shader compilation occured
            for (int frame = 0; frame < 4; ++frame)
                yield return null;

            // Assert
            Assert.Pass("No exception should be raised");
        }


        [UnityTest, Description("Cover UUM_83782")]
        public IEnumerator Check_CustomHLSL_Block_Parameter_Same_Name_As_Attribute()
        {
            // Arrange
            var hlslCode =
                "void myDir(inout VFXAttributes attributes,in float3 direction)\n" +
                "{\n" +
                "   attributes.direction = direction;\n" +
                "}";
            var hlslBlock = ScriptableObject.CreateInstance<CustomHLSL>();
            hlslBlock.SetSettingValue("m_HLSLCode", hlslCode);

            // Act
            VFXCustomHLSLTestHelper.MakeSimpleGraphWithCustomHLSL(hlslBlock, out var view, out var graph);

            // Assert
            foreach (var y in VFXTestCommon.CheckCompilation(graph))
            {
                yield return y;
            }
        }
    }
}
