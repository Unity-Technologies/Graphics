using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    class GraphElementModelTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);


        [Test]
        [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
        public void GraphElementModelLatestVersionIsMaxVersion()
        {
            var maxVersion = Enum.GetValues(typeof(GraphElementModel.SerializationVersion)).Cast<GraphElementModel.SerializationVersion>().Max();
            Assert.That(GraphElementModel.SerializationVersion.Latest, Is.Not.EqualTo(default(GraphElementModel.SerializationVersion)));
            Assert.That(GraphElementModel.SerializationVersion.Latest, Is.EqualTo(maxVersion));
        }
    }
}
