// using System.Collections;
// using System.Linq;
// using Unity.GraphToolsFoundation.Editor;
// using UnityEngine;
// using Unity.GraphToolsFoundation;
// using UnityEngine.TestTools;
// using Assert = UnityEngine.Assertions.Assert;

// TODO (Brett) These tests should be part of the "SubgraphTests" harness

// namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
// {
//     class SubGraphCommandTests : BaseGraphWindowTest
//     {
//         const string k_OutputNodeName = "DefaultContextDescriptor";
//
//         SGContextNodeModel m_OutputContextNodeModel;
//
//         /// <inheritdoc />
//         protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.MemorySubGraph;
//
//         public override void SetUp()
//         {
//             base.SetUp();
//
//             m_OutputContextNodeModel = m_MainWindow.GetNodeModelFromGraphByName(k_OutputNodeName) as SGContextNodeModel;
//             Assert.IsNotNull(m_OutputContextNodeModel, "Subgraph output node must be present on graph");
//         }
//
//         [UnityTest]
//         public IEnumerator TestCanAddRemoveSubgraphOutputs()
//         {
//             Assert.IsNull(m_OutputContextNodeModel.GetInputPortForEntry("Port1"));
//
//             yield return null;
//             m_GraphView.Dispatch(new AddContextEntryCommand(m_OutputContextNodeModel, "Port1", TypeHandle.Float));
//
//             foreach (var portModel in m_OutputContextNodeModel.GetInputPorts())
//             {
//                 Debug.Log(portModel);
//             }
//
//             var newEntry = m_OutputContextNodeModel.GetInputPortForEntry("Port1");
//             Assert.IsNotNull(newEntry, "Adding a subgraph output should create a corresponding port");
//             Assert.AreEqual(newEntry.DataTypeHandle, TypeHandle.Float, "Created port should have correct type");
//
//             m_GraphView.Dispatch(new RemoveContextEntryCommand(m_OutputContextNodeModel, "Port1"));
//             yield return null;
//
//             Assert.IsNull(m_OutputContextNodeModel.GetInputPortForEntry("Port1"), "Removing a subgraph output should remove the corresponding port");
//         }
//
//         [UnityTest]
//         public IEnumerator TestRemovingSubgraphOutputRemovesEdges()
//         {
//             yield return null;
//
//             m_GraphView.Dispatch(new AddContextEntryCommand(m_OutputContextNodeModel, "Port1", TypeHandle.Vector3));
//             yield return null;
//
//             yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Vector3");
//             var vector3Node = (SGNodeModel)m_MainWindow.GetNodeModelFromGraphByName("Vector3");
//             m_GraphView.Dispatch(new CreateWireCommand(m_OutputContextNodeModel.GetInputPortForEntry("Port1"), vector3Node.OutputsById["Out"]));
//             yield return null;
//
//             m_GraphView.Dispatch(new RemoveContextEntryCommand(m_OutputContextNodeModel, "Port1"));
//             yield return null;
//
//             Assert.AreEqual(0, vector3Node.GetConnectedWires().Count(), "Removing a subgraph output should also remove connected edges");
//             Assert.AreEqual(0, m_OutputContextNodeModel.GetConnectedWires().Count(), "Removing a subgraph output should also remove connected edges");
//         }
//
//         [UnityTest]
//         public IEnumerator TestCanRenameSubgraphOutput()
//         {
//             yield return null;
//
//             m_GraphView.Dispatch(new AddContextEntryCommand(m_OutputContextNodeModel, "OriginalPort", TypeHandle.Float));
//             yield return null;
//
//             m_GraphView.Dispatch(new RenameContextEntryCommand(m_OutputContextNodeModel, "OriginalPort", "RenamedPort"));
//             yield return null;
//
//             Assert.IsNull(m_OutputContextNodeModel.GetInputPortForEntry("OriginalPort"), "After renaming an output, port with previous name no longer exists");
//             Assert.IsNotNull(m_OutputContextNodeModel.GetInputPortForEntry("RenamedPort"), "After renaming an output, port with new name exists");
//             Assert.AreEqual(TypeHandle.Float, m_OutputContextNodeModel.GetInputPortForEntry("RenamedPort").DataTypeHandle, "After renaming an output, port with new name has same type");
//         }
//
//         [UnityTest]
//         public IEnumerator TestRenamingSubgraphOutputPreservesEdges()
//         {
//             yield return null;
//
//             m_GraphView.Dispatch(new AddContextEntryCommand(m_OutputContextNodeModel, "OriginalPort", TypeHandle.Vector3));
//             yield return null;
//
//             yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Vector3");
//             var vector3Node = (SGNodeModel)m_MainWindow.GetNodeModelFromGraphByName("Vector3");
//             m_GraphView.Dispatch(new CreateWireCommand(m_OutputContextNodeModel.GetInputPortForEntry("OriginalPort"), vector3Node.OutputsById["Out"]));
//             yield return null;
//
//             m_GraphView.Dispatch(new RenameContextEntryCommand(m_OutputContextNodeModel, "OriginalPort", "RenamedPort"));
//             yield return null;
//
//             Assert.AreEqual(1, m_OutputContextNodeModel.GetConnectedWires().Count(), "After renaming an output, connected edge should be preserved");
//         }
//
//         [UnityTest]
//         public IEnumerator TestCanChangeSubgraphOutputType()
//         {
//             yield return null;
//
//             m_GraphView.Dispatch(new AddContextEntryCommand(m_OutputContextNodeModel, "Port1", TypeHandle.Float));
//             yield return null;
//
//             m_GraphView.Dispatch(new ChangeContextEntryTypeCommand(m_OutputContextNodeModel, "Port1", TypeHandle.Bool));
//             yield return null;
//
//             Assert.AreEqual(TypeHandle.Bool, m_OutputContextNodeModel.GetInputPortForEntry("Port1").DataTypeHandle, "After changing output type, port has updated type");
//         }
//     }
// }
