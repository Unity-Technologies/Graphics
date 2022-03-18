using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    [Serializable]
    class ProcessableNodeModel : Type1FakeNodeModel
    {
        public const int ultimateAnswer = 42;

        [SerializeField]
        int m_SomeValue = ultimateAnswer;

        public int SomeValue
        {
            get => m_SomeValue;
            set => m_SomeValue = value;
        }
    }

    class SetValueCommand : ModelCommand<ProcessableNodeModel, int>
    {
        const string k_UndoStringSingular = "Set Node Value";
        const string k_UndoStringPlural = "Set Nodes Value";

        public SetValueCommand(int value, params ProcessableNodeModel[] nodes)
            : base(k_UndoStringSingular, k_UndoStringPlural, value, nodes)
        {
        }

        public static void DefaultHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SetValueCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.SomeValue = command.Value;
                    graphUpdater.MarkChanged(nodeModel, ChangeHint.Data);
                }
            }
        }
    }

    class ProcessingGraphProcessor : IGraphProcessor
    {
        public GraphProcessingResult ProcessGraph(IGraphModel graphModel, GraphChangeDescription changes)
        {
            var res = new GraphProcessingResult();
            foreach (var procNodeModel in graphModel.NodeModels.OfType<ProcessableNodeModel>())
            {
                if (procNodeModel.SomeValue != ProcessableNodeModel.ultimateAnswer)
                {
                    res.AddError("This is not the right value!",
                        procNodeModel,
                        new QuickFix("Set things right", cd => cd.Dispatch(new SetValueCommand(ProcessableNodeModel.ultimateAnswer, procNodeModel))));
                }
            }

            return res;
        }
    }

    class ProcessingStencil : ClassStencil
    {
        protected override void CreateGraphProcessors()
        {
            base.CreateGraphProcessors();
            GetGraphProcessorContainer().AddGraphProcessor(new ProcessingGraphProcessor());
        }
    }

    class GraphProcessingTests : BaseUIFixture
    {
        protected override Type CreatedGraphType => typeof(ProcessingStencil);

        protected override bool CreateGraphOnStartup => true;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            GraphView.RegisterCommandHandler<SetValueCommand>(SetValueCommand.DefaultHandler);
        }

        [UnityTest]
        public IEnumerator GraphProcessingErrorGenerationAndQuickFixEndToEndTest()
        {
            // TODO JOCE More targeted/granular tests would be required in the future, but this end-to-end test covers
            // all the basic workflows for graph processing, error generation and quick fixes
            var nodeModel1 = GraphModel.CreateNode<ProcessableNodeModel>("Node0", new Vector2(0, 0));

            // Set the "wrong" value for node 1, so we have an error badge showing up.
            nodeModel1.SomeValue = 1;
            MarkGraphModelStateDirty();
            yield return null;

            var badges = GraphView.Query<Badge>().ToList();
            Assert.AreEqual(1, badges.Count);

            // Get the quickfix associated with the badge and invoke it.
            ((IGraphProcessingErrorModel)badges[0].BadgeModel).Fix.QuickFixAction(GraphView);
            yield return null;

            // Check that once the QuickFix action has been invoked, the value of node 1 has been restored
            Assert.AreEqual(ProcessableNodeModel.ultimateAnswer, nodeModel1.SomeValue);
            yield return null;
        }
    }
}
