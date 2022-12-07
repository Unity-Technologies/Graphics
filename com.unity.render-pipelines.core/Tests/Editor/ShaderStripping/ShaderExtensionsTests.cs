using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Rendering.Tests.ShaderStripping
{
    class ShaderExtensionsTests
    {
        static TestCaseData[] s_TestCaseDatas =
        {
             new TestCaseData(Shader.Find("Hidden/Internal-Colored"))
                .SetName("Given a shader from Built-in, the render pipeline tag is not found and is empty")
                .Returns((false,string.Empty)),
             new TestCaseData(Shader.Find("DummyPipeline/VariantStrippingTestsShader"))
                .SetName("Given a shader with a render pipeline tag, the pipeline is found")
                .Returns((true, "DummyPipeline"))
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public (bool, string) TryGetRenderPipelineTag(Shader shader)
        {
            return (shader.TryGetRenderPipelineTag(default, out string renderPipeline), renderPipeline);
        }
    }
}
