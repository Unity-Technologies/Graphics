using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    class GraphElementModelTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);


        [Test]
        public void GraphElementModelLatestVersionIsMaxVersion()
        {
            var maxVersion = Enum.GetValues(typeof(GraphElementModel.SerializationVersion)).Cast<GraphElementModel.SerializationVersion>().Max();
            Assert.That(GraphElementModel.SerializationVersion.Latest, Is.Not.EqualTo(default(GraphElementModel.SerializationVersion)));
            Assert.That(GraphElementModel.SerializationVersion.Latest, Is.EqualTo(maxVersion));
        }
    }
}
