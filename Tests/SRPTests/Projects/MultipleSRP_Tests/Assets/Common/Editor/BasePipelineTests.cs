using Common;
using NUnit.Framework;

namespace SRPGraphicsSettings
{
    public class BasePipelineTests
    {
        RenderPipelineScope m_RenderPipelineScope;

        [SetUp]
        public virtual void Setup()
        {
            m_RenderPipelineScope = new RenderPipelineScope(true, true);
        }

        [TearDown]
        public virtual void TearDown()
        {
            m_RenderPipelineScope.Dispose();
        }
    }
}
