using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    class GraphChangeDescriptionTests
    {
        [Test]
        public void UnionWorks()
        {
            var newModels1 = new[]
            {
                new TestNodeModel(),
                new TestNodeModel(),
            };

            var changedModels1 = new[]
            {
                new TestNodeModel(),
                new TestNodeModel(),
            };

            var changedModelsAndHints1 = new Dictionary<IGraphElementModel, IReadOnlyList<ChangeHint>>
            {
                { changedModels1[0], new List<ChangeHint> { ChangeHint.Data } },
                { changedModels1[1], new List<ChangeHint> { ChangeHint.Data } },
            };

            var deletedModels1 = new[]
            {
                new TestNodeModel(),
                new TestNodeModel(),
            };

            var change = new GraphChangeDescription(newModels1, changedModelsAndHints1, deletedModels1);

            var newModels2 = new[]
            {
                new TestNodeModel(),
                new TestNodeModel(),
            };

            var changedModelsAndHints2 = new Dictionary<IGraphElementModel, IReadOnlyList<ChangeHint>>
            {
                { changedModels1[0], new List<ChangeHint> { ChangeHint.GraphTopology } },
                { changedModels1[1], new List<ChangeHint> { ChangeHint.GraphTopology } },
            };

            var deletedModels2 = new[]
            {
                new TestNodeModel(),
                new TestNodeModel(),
            };

            change.Union(newModels2, changedModelsAndHints2, deletedModels2);

            Assert.IsTrue(change.NewModels.Contains(newModels1[0]));
            Assert.IsTrue(change.NewModels.Contains(newModels1[1]));
            Assert.IsTrue(change.NewModels.Contains(newModels2[0]));
            Assert.IsTrue(change.NewModels.Contains(newModels2[1]));

            Assert.IsTrue(change.DeletedModels.Contains(deletedModels1[0]));
            Assert.IsTrue(change.DeletedModels.Contains(deletedModels1[1]));
            Assert.IsTrue(change.DeletedModels.Contains(deletedModels2[0]));
            Assert.IsTrue(change.DeletedModels.Contains(deletedModels2[1]));

            foreach (var model in changedModels1)
            {
                Assert.IsTrue(change.ChangedModels.TryGetValue(model, out var changeHints));
                Assert.IsTrue(changeHints.Contains(ChangeHint.Data));
                Assert.IsTrue(changeHints.Contains(ChangeHint.GraphTopology));
            }
        }
    }
}
