using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Extensions
{
    class ConstantNodeSpawningTests
    {
        [Test]
        public void TestConstantEditorExtensionMethodsExistForBasicTypes()
        {
            var expectedTypes = new[]
            {
                typeof(StringConstant), typeof(BooleanConstant), typeof(IntConstant),
                typeof(DoubleConstant), typeof(FloatConstant),
                typeof(Vector2Constant), typeof(Vector3Constant), typeof(Vector4Constant), typeof(QuaternionConstant),
                typeof(EnumConstant), typeof(ColorConstant),
                typeof(AnimationClipConstant), typeof(MeshConstant), typeof(Texture2DConstant), typeof(Texture3DConstant),
                typeof(EnumConstant)
            };
            for (var i = 0; i < expectedTypes.Length; i++)
            {
                var type = expectedTypes[i];

                var constantExtMethod = ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(typeof(GraphView), type, ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);

                Assert.IsNotNull(constantExtMethod, $"No constant editor for {type.Name}");
            }
        }
    }
}
