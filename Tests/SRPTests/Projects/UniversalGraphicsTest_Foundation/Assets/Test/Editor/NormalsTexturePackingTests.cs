using NUnit.Framework;
using UnityEngine.TestTools.Graphics.Shaders;

namespace Unity.Testing.SRP.Universal.Foundation.Editor
{
    class NormalsTexturePackingTests
    {
        const string k_OctShaderPath = "Assets/Test/Editor/NormalsTexturePackingOct.hlsl";
        const string k_DirectShaderPath = "Assets/Test/Editor/NormalsTexturePackingDirect.hlsl";

        const float k_OctMaxErrorDegrees = 0.15f;
        const float k_OctMinErrorDegrees = 1e-4f;
        const float k_HalfPrecisionMaxErrorDegrees = 0.05f;

        ShaderTestFramework m_ShaderTestFramework;

        [SetUp]
        public void SetUp()
        {
            m_ShaderTestFramework = new ShaderTestFramework();
        }

        [TearDown]
        public void TearDown()
        {
            m_ShaderTestFramework.Dispose();
        }

        float MaxErrorDegrees(string shaderPath, string functionCall)
        {
            var shader = m_ShaderTestFramework.LoadShader(shaderPath, new HlslShaderParams(functionCall, typeof(float)));
            return m_ShaderTestFramework.ExecuteShader<float>(shader);
        }

        static string SweepCall(int stepCount) => $"MaxNormalRoundTripErrorDegrees({stepCount})";

        [Test]
        public void OctPacking_RoundTripsEveryNormal_WithinTwelveBitPrecision([Values(16, 64, 256)] int stepCount)
        {
            var maxErrorDegrees = MaxErrorDegrees(k_OctShaderPath, SweepCall(stepCount));

            Assert.That(maxErrorDegrees, Is.InRange(k_OctMinErrorDegrees, k_OctMaxErrorDegrees));
        }

        [Test]
        public void DirectPacking_RoundTripsEveryNormal_WithinHalfPrecision([Values(16, 64, 256)] int stepCount)
        {
            var maxErrorDegrees = MaxErrorDegrees(k_DirectShaderPath, SweepCall(stepCount));

            Assert.That(maxErrorDegrees, Is.InRange(0.0f, k_HalfPrecisionMaxErrorDegrees));
        }

        [Test]
        public void OctPacking_RoundTripsAxisAlignedNormals_WithinTwelveBitPrecision()
        {
            var maxErrorDegrees = MaxErrorDegrees(k_OctShaderPath, "MaxAxisAlignedRoundTripErrorDegrees()");

            Assert.That(maxErrorDegrees, Is.InRange(k_OctMinErrorDegrees, k_OctMaxErrorDegrees));
        }

        [Test]
        public void OctPacking_IsLessPreciseThanDirectPacking()
        {
            var octErrorDegrees = MaxErrorDegrees(k_OctShaderPath, SweepCall(64));
            var directErrorDegrees = MaxErrorDegrees(k_DirectShaderPath, SweepCall(64));

            Assert.That(octErrorDegrees, Is.GreaterThan(directErrorDegrees));
        }
    }
}
