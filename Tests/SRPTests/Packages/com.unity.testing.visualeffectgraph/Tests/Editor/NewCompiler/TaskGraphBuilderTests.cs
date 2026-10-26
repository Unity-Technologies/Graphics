using System;
using NUnit.Framework;
using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX.Test
{
    /// <summary>
    /// Lifecycle of <see cref="TaskGraphBuilder"/>, and the parent inference it exists to provide.
    /// </summary>
    [TestFixture]
    public class TaskGraphBuilderTests
    {
        static DataPathSet Paths(params string[] subKeys)
        {
            var paths = new DataPathSet();
            foreach (var subKey in subKeys)
                paths.Add(DataPath.Root + new NameDataKey(subKey));
            return paths;
        }

        static StructuredData StructWith(string subKey)
        {
            var structured = new StructuredData();
            structured.AddSubdata(new NameDataKey(subKey), new ValueData<float>());
            return structured;
        }

        /// <summary>Adds a task declaring the given usage of "x", and binds it letting parents be inferred.</summary>
        static TaskNodeId AddBoundTask(TaskGraphBuilder builder, DataViewId containerViewId, string taskName,
            bool reads, bool writes)
        {
            var task = new TestTask(taskName);
            var bindingKey = new NameDataKey(taskName);
            task.Declare(bindingKey, reads ? Paths("x") : new DataPathSet(), writes ? Paths("x") : new DataPathSet());

            var taskId = builder.AddTask(task, taskName);
            builder.BindData(taskId, bindingKey, containerViewId);
            return taskId;
        }

        [Test]
        public void EndBuilding_HandsOverWhatWasBuilt()
        {
            var builder = new TaskGraphBuilder();
            var taskId = builder.AddTask(new TestTask("W"), "W");

            var graph = builder.EndBuilding();

            Assert.That(graph.IsValid(taskId), Is.True);
            Assert.That(graph.TaskNodes.Count, Is.EqualTo(1));
        }

        [Test]
        public void EndBuilding_MakesItUnusable()
        {
            var builder = new TaskGraphBuilder();
            builder.EndBuilding();

            Assert.Throws<InvalidOperationException>(() => builder.AddData("S", new ValueData<float>()));
            Assert.Throws<InvalidOperationException>(() => builder.AddTask(new TestTask("T")));
            Assert.Throws<InvalidOperationException>(() => _ = builder.Graph);
            Assert.Throws<InvalidOperationException>(() => builder.EndBuilding());
        }

        [Test]
        public void ReadsGoThroughGraphWhileBuilding()
        {
            var builder = new TaskGraphBuilder();
            var containerViewId = builder.AddData("S", StructWith("x"));

            // Graph exposes what has been added so far, without ending building.
            Assert.That(builder.Graph.DataContainers.Count, Is.EqualTo(1));
            Assert.That(builder.Graph.DataViews[containerViewId].DataContainer.Name, Is.EqualTo("S"));
        }

        [Test]
        public void BindData_InfersTheLastWriterAsParent()
        {
            var builder = new TaskGraphBuilder();
            var containerViewId = builder.AddData("S", StructWith("x"));

            AddBoundTask(builder, containerViewId, "W", reads: false, writes: true);
            AddBoundTask(builder, containerViewId, "M", reads: true, writes: true);
            AddBoundTask(builder, containerViewId, "C", reads: true, writes: false);

            var graph = builder.EndBuilding();

            // C reads after M wrote, so M is its parent rather than the earlier writer W.
            Assert.That(ParentCount(graph, "C"), Is.EqualTo(1));
            Assert.That(ParentNameOf(graph, "C"), Is.EqualTo("M"));
            Assert.That(ParentNameOf(graph, "M"), Is.EqualTo("W"));
        }

        [Test]
        public void BindData_InfersNoParentWithoutAPriorWrite()
        {
            var builder = new TaskGraphBuilder();
            var containerViewId = builder.AddData("S", StructWith("x"));

            AddBoundTask(builder, containerViewId, "C", reads: true, writes: false);

            var graph = builder.EndBuilding();

            Assert.That(ParentNames(graph, "C"), Is.Empty);
        }

        [Test]
        public void EachBuilderBuildsItsOwnGraph()
        {
            // A builder cannot be pointed at an existing graph, so it can never start against writes its
            // bookkeeping knows nothing about — a read would silently infer no parent from them.
            Assert.That(new TaskGraphBuilder().Graph, Is.Not.SameAs(new TaskGraphBuilder().Graph));
        }

        static DataNode DataNodeOf(IReadOnlyGraph graph, string taskName)
        {
            foreach (var dataNode in graph.DataNodes)
            {
                if (dataNode.TaskNode.Task.ToString() == taskName)
                    return dataNode;
            }

            Assert.Fail($"No data node owned by a task named '{taskName}'.");
            return default;
        }

        static int ParentCount(IReadOnlyGraph graph, string taskName) => DataNodeOf(graph, taskName).Parents.Count;

        /// <summary>Owning task name per parent, so a failure names the unexpected parent rather than counting it.</summary>
        static string[] ParentNames(IReadOnlyGraph graph, string taskName)
        {
            var parents = DataNodeOf(graph, taskName).Parents;
            var names = new string[parents.Count];
            for (int i = 0; i < parents.Count; i++)
            {
                var parentId = parents.GetId(i);
                names[i] = graph.IsValid(parentId)
                    ? graph.DataNodes[parentId].TaskNode.Task.ToString()
                    : $"<dead {parentId}>";
            }
            return names;
        }

        static string ParentNameOf(IReadOnlyGraph graph, string taskName)
        {
            var parents = DataNodeOf(graph, taskName).Parents;
            Assert.That(parents.Count, Is.EqualTo(1), $"'{taskName}' was expected to have exactly one parent.");
            return parents[0].TaskNode.Task.ToString();
        }
    }
}
