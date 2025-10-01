#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX.UI;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Experimental.VFX.Utility;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.Operator;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXCopyPasteTests
    {
        VFXViewController m_ViewController;

        const string testAssetName = "Assets/TmpTests/VFXGraph{0}.vfx";

        private int m_StartUndoGroupId;


        string lastFileName;
        static int cpt = 0;

        [SetUp]
        public void CreateTestAsset()
        {
            lastFileName = string.Format(testAssetName, cpt++);

            var directoryPath = Path.GetDirectoryName(lastFileName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }


            if (File.Exists(lastFileName))
            {
                AssetDatabase.DeleteAsset(lastFileName);
            }

            var asset = VisualEffectAssetEditorUtility.CreateNewAsset(lastFileName);

            VisualEffectResource resource = asset.GetResource(); // force resource creation

            m_ViewController = VFXViewController.GetController(resource);
            m_ViewController.useCount++;

            m_StartUndoGroupId = Undo.GetCurrentGroup();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            m_ViewController.useCount--;
            Undo.RevertAllDownToGroup(m_StartUndoGroupId);
            AssetDatabase.DeleteAsset(lastFileName);
            lastFileName = null;
        }

        [Test]
        public void CopyPasteContextWithBlock()
        {
            // Create a BasicInitialize context
            var initContextDesc = VFXLibrary.GetContexts().First(t => typeof(VFXBasicInitialize).IsAssignableFrom(t.modelType));
            var newContext = m_ViewController.AddVFXContext(new Vector2(100, 100), initContextDesc.variant);
            m_ViewController.ApplyChanges();
            Assert.AreEqual(1, m_ViewController.allChildren.Count(t => t is VFXContextController));
            var contextController = m_ViewController.allChildren.OfType<VFXContextController>().Single();
            Assert.AreEqual(contextController.model, newContext);

            // Add a block to that context
            var flipBookBlockDesc = VFXLibrary.GetBlocks().First(t => t.modelType == typeof(SetAttribute) && t.HasSettingValue(VFXAttribute.TexIndex.name));
            contextController.AddBlock(0, flipBookBlockDesc.CreateInstance());
            m_ViewController.ApplyChanges();

            // Select the created context (which now contains a single block)
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            view.controller = m_ViewController;
            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }

            // Set the original block slot values
            VFXSlot boundsSlot = newContext.GetInputSlot(0);
            AABox originalBounds = new AABox { center = Vector3.one, size = Vector3.one * 10 };
            boundsSlot.value = originalBounds;
            VFXBlock flipBookBlock = m_ViewController.contexts.First().blockControllers.First().model;
            VFXSlot minValueSlot = flipBookBlock.GetInputSlot(0);
            float originalMinValue = 123.456f;
            minValueSlot.value = originalMinValue;

            // Copy selection
            var elements = view.selection.OfType<GraphElement>().ToArray();
            var bounds = view.GetElementsBounds(elements);
            string copyData = view.SerializeElements(elements);
            // Than change value to check the original value is pasted (not the modified one after copy)
            boundsSlot.value = new AABox { center = Vector3.zero, size = Vector3.zero };
            minValueSlot.value = 789f;

            // Paste selection
            view.UnserializeAndPasteElements("paste", copyData);

            // Get the only context that has a different model than the original one (which means it's the context that we just pasted)
            var copyContextModel = view.Query()
                .OfType<VFXContextUI>()
                .ToList()
                .Select(x => x.controller)
                .Single(x => x.model != newContext).model;

            var copyBoundsSlot = copyContextModel.GetInputSlot(0);
            var copyMinSlot = copyContextModel[0].GetInputSlot(0);

            Assert.AreEqual((AABox)copyBoundsSlot.value, originalBounds);
            Assert.AreEqual((float)copyMinSlot.value, originalMinValue);
            Assert.AreEqual(view.pasteCenter + newContext.position - bounds.min, copyContextModel.position);

        }

        [Test]
        public void CopyPasteOperator()
        {
            // Create an operator
            var crossOperatorDesc = VFXLibrary.GetOperators().First(t => t.name == "Cross Product");
            var newOperator = m_ViewController.AddVFXOperator(new Vector2(100, 100), crossOperatorDesc.variant);
            m_ViewController.ApplyChanges();
            var operatorController = m_ViewController.allChildren.OfType<VFXOperatorController>().Single();
            Assert.AreEqual(operatorController.model, newOperator);

            // Select the newly created operator
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;

            view.controller = m_ViewController;
            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }


            // Setup original operator slot values
            VFXSlot aSlot = newOperator.GetInputSlot(0);
            Vector3 originalA = Vector3.one * 123;
            aSlot.value = originalA;

            // Copy selection and modify operator slot value afterwards
            var elements = view.selection.OfType<GraphElement>().ToArray();
            var bounds = view.GetElementsBounds(elements);
            string copyData = view.SerializeElements(elements);
            aSlot.value = Vector3.one * 456;

            // Paste selection
            view.UnserializeAndPasteElements("paste", copyData);

            // Retrieve the pasted operator
            var copyOperator = view.Query()
                .OfType<GraphElement>()
                .ToList()
                .OfType<VFXOperatorUI>()
                .Single(x => x.controller.model != newOperator);

            var copyASlot = copyOperator.controller.model.GetInputSlot(0);
            Assert.AreEqual(originalA, (Vector3)copyASlot.value);
            Assert.AreEqual(view.pasteCenter + newOperator.position - bounds.min, copyOperator.controller.model.position);
        }

        [Test]
        public void CopyPasteSpacableOperator()
        {
            // Create a spaceable operator
            var inlineOperatorDesc = VFXLibrary.GetOperators().First(t => t.modelType == typeof(VFXInlineOperator));
            var newOperator = m_ViewController.AddVFXOperator(new Vector2(100, 100), inlineOperatorDesc.variant);
            newOperator.SetSettingValue("m_Type", new SerializableType(typeof(DirectionType)));
            m_ViewController.ApplyChanges();
            var operatorController = m_ViewController.allChildren.OfType<VFXOperatorController>().First();
            Assert.AreEqual(newOperator, operatorController.model);

            // Select the newly created operator
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            view.controller = m_ViewController;
            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }

            // Setup original operator slot value
            VFXSlot aSlot = newOperator.GetInputSlot(0);
            Assert.IsTrue(aSlot.spaceable);
            aSlot.space = VFXSpace.World;

            // Copy selection and then modify the slot value
            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());
            aSlot.space = VFXSpace.Local;

            // Paste selection
            view.UnserializeAndPasteElements("paste", copyData);

            // Retrieve the pasted operator
            var copyOperator = view.Query()
                .OfType<GraphElement>()
                .ToList()
                .OfType<VFXOperatorUI>()
                .Single(x => x.controller.model != newOperator);

            var copyASlot = copyOperator.controller.model.GetInputSlot(0);
            Assert.AreEqual(VFXSpace.World, copyASlot.space);
        }

        [Test]
        public void CopyPasteEdges()
        {
            // Load a pre-made vfx asset into a VFX Graph window
            VisualEffectAsset asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Assets/AllTests/Editor/Tests/CopyPasteTest.vfx");
            VFXViewController controller = VFXViewController.GetController(asset.GetResource(), true);
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            view.controller = controller;
            view.ClearSelection();


            // Select all nodes in the graph
            var originalElements = view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>().ToArray();
            Assert.AreNotEqual(0, originalElements.Length);
            foreach (var element in originalElements)
            {
                view.AddToSelection(element);
            }

            // Copy / Paste selection
            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());
            view.controller = m_ViewController;
            view.UnserializeAndPasteElements("paste", copyData);

            var parameters = view.Query().OfType<VFXParameterUI>().ToList();
            Assert.AreEqual(2, parameters.Count);
            if (parameters[0].title == "Vector3")
            {
                (parameters[0], parameters[1]) = (parameters[1], parameters[0]);
            }

            var operators = view.Query().OfType<VFXOperatorUI>().ToList();
            Assert.AreEqual(2, operators.Count);

            var contexts = view.Query().OfType<VFXContextUI>().ToList();
            Assert.AreEqual(2, contexts.Count);

            if (contexts[0].controller.model is VFXBasicUpdate)
            {
                (contexts[0], contexts[1]) = (contexts[1], contexts[0]);
            }


            var dataEdges = view.Query().OfType<VFXDataEdge>().ToList();

            Assert.AreEqual(4, dataEdges.Count);

            Assert.IsNotNull(dataEdges.SingleOrDefault(t => t.output.GetFirstAncestorOfType<VFXNodeUI>() == parameters[1] &&
                                                           operators.Contains(t.input.GetFirstAncestorOfType<VFXOperatorUI>())));

            Assert.IsNotNull(dataEdges.SingleOrDefault(t => operators.Contains(t.input.GetFirstAncestorOfType<VFXOperatorUI>()) &&
                                                           operators.Contains(t.output.GetFirstAncestorOfType<VFXOperatorUI>()) &&
                                                           t.output.GetFirstAncestorOfType<VFXNodeUI>() != t.input.GetFirstAncestorOfType<VFXNodeUI>()));

            Assert.IsNotNull(dataEdges.SingleOrDefault(t => t.output.GetFirstAncestorOfType<VFXNodeUI>() == parameters[0] &&
                                                           t.input.GetFirstAncestorOfType<VFXNodeUI>() == contexts[0]));

            Assert.IsNotNull(dataEdges.FirstOrDefault(t => operators.Contains<VFXNodeUI>(t.output.GetFirstAncestorOfType<VFXNodeUI>()) &&
                                                           t.input.GetFirstAncestorOfType<VFXNodeUI>() == contexts[0].GetAllBlocks().First()));


            VFXFlowEdge flowEdge = view.Query().OfType<VFXFlowEdge>();

            Assert.IsNotNull(flowEdge);

            Assert.AreEqual(flowEdge.output.GetFirstAncestorOfType<VFXContextUI>(), contexts[1]);
            Assert.AreEqual(flowEdge.input.GetFirstAncestorOfType<VFXContextUI>(), contexts[0]);
        }

        [Test]
        public void CopyPasteBlock()
        {
            // Create a new BasicInitialize context
            var initContextDesc = VFXLibrary.GetContexts().First(t => typeof(VFXBasicInitialize).IsAssignableFrom(t.modelType));
            var newContext = m_ViewController.AddVFXContext(new Vector2(100, 100), initContextDesc.variant);
            m_ViewController.ApplyChanges();
            Assert.AreEqual(1, m_ViewController.allChildren.Count(t => t is VFXContextController));
            var contextController = m_ViewController.allChildren.OfType<VFXContextController>().First();
            Assert.AreEqual(contextController.model, newContext);

            // Add a block to that context
            var flipBookBlockDesc = VFXLibrary.GetBlocks().First(t => t.modelType == typeof(SetAttribute) && t.HasSettingValue(VFXAttribute.TexIndex.name));
            contextController.AddBlock(0, flipBookBlockDesc.CreateInstance());
            var newBlock = contextController.model.children.First();
            m_ViewController.ApplyChanges();

            // Select the created context
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            view.controller = m_ViewController;
            view.ClearSelection();
            foreach (var element in view.Query().OfType<VFXBlockUI>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }

            // Set original block slots values
            VFXBlock flipBookBlock = contextController.blockControllers.First().model;
            VFXSlot minValueSlot = flipBookBlock.GetInputSlot(0);
            float originalMinValue = 123.456f;
            minValueSlot.value = originalMinValue;

            // Copy selection and change the block value
            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());
            minValueSlot.value = 789f;

            // Paste selection
            view.UnserializeAndPasteElements("Paste", copyData);

            // Retrieve the block from the copied context
            var copyBlock = view.Query()
                .OfType<VFXBlockUI>()
                .ToList()
                .Select(x => x.controller)
                .First(x => x.model != newBlock).model;

            // Check the block slot value is the same as the original (not the modified value after copy operation)
            var copyMinSlot = copyBlock.GetInputSlot(0);
            Assert.AreEqual((float)copyMinSlot.value, originalMinValue);
        }

        [Test]
        public void CopyPast_Context_With_Objects_In_Settings()
        {
            var outputContextDesc = VFXLibrary.GetContexts().First(t => t.modelType == typeof(VFXComposedParticleOutput));
            m_ViewController.AddVFXContext(new Vector2(100, 100), outputContextDesc.variant);
            m_ViewController.ApplyChanges();

            var window = EditorWindow.GetWindow<VFXViewWindow>();
            var view = window.graphView;
            view.controller = m_ViewController;
            view.ClearSelection();
            foreach (var element in view.Query().OfType<VFXContextUI>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }

            var copyData = view.SerializeElements(view.selection.OfType<GraphElement>());
            view.UnserializeAndPasteElements("Paste", copyData);

            // Retrieve the block from the copied context
            var copiedContexts = view.Query()
                .OfType<VFXContextUI>()
                .ToList()
                .Select(x => x.controller.model).ToArray();

            Assert.AreEqual(2, copiedContexts.Length);

            Assert.IsInstanceOf<VFXComposedParticleOutput>(copiedContexts[0]);
            Assert.IsInstanceOf<VFXComposedParticleOutput>(copiedContexts[1]);

            var originalTopology = copiedContexts[0].GetSetting("m_Topology");
            var originalShading = copiedContexts[0].GetSetting("m_Shading");
            var copyTopology = copiedContexts[1].GetSetting("m_Topology");
            var copyShading = copiedContexts[1].GetSetting("m_Shading");

            Assert.IsTrue(originalTopology.valid);
            Assert.IsTrue(originalShading.valid);
            Assert.IsTrue(copyTopology.valid);
            Assert.IsTrue(copyShading.valid);

            Assert.AreEqual(originalTopology.GetType(), copyTopology.GetType());
            Assert.AreEqual(originalShading.GetType(), copyShading.GetType());
            Assert.IsFalse(ReferenceEquals(originalTopology.value, copyTopology.value));
            Assert.IsFalse(ReferenceEquals(originalShading.value, copyShading.value));
        }

        [Test]
        public void CreateTemplate()
        {
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            view.CreateTemplateSystem(VFXTestCommon.simpleParticleSystemPath, Vector2.zero, null, false);
        }

        [Test]
        public void PasteSystems()
        {
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            // Create a bunch of systems
            const int spawnerCount = 4, GPUSystemsCount = 4;
            var spawners = VFXTestCommon.CreateSpawners(view, m_ViewController, spawnerCount);
            VFXTestCommon.CreateSystems(view, m_ViewController, GPUSystemsCount, 0);

            // Copy paste them
            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }
            view.CopySelectionCallback();
            view.PasteCallback();
            m_ViewController.ApplyChanges();

            // Query unique names
            var systemNames = view.controller.graph.systemNames;

            var uniqueNames = m_ViewController.graph.children.OfType<VFXBasicSpawner>()
                .Select(x => systemNames.GetUniqueSystemName(x.GetData()))
                .Union(m_ViewController.graph.children.OfType<VFXBasicInitialize>().Select(x => systemNames.GetUniqueSystemName(x.GetData())))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var models = new HashSet<ScriptableObject>();
            m_ViewController.graph.CollectDependencies(models);
            var vfxDatas = models.OfType<VFXData>().ToArray();

            const int dataExpectedCount = 2 * (spawnerCount + GPUSystemsCount);
            Assert.AreEqual(dataExpectedCount, vfxDatas.Length, "There should be one distinct VFXData per system (8 spawners and 8 initialize");
            // Assert all names are unique, and the expected number of elements was obtained
            Assert.AreEqual(dataExpectedCount, uniqueNames.Count, "Some systems have the same name or are null or empty.");
        }

        [UnityTest, Description("UUM-46548")]
        public IEnumerator PasteMissingPointCacheAsset()
        {
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            // Create one system
            const int spawnerCount = 1, GPUSystemsCount = 1;
            var spawner = VFXTestCommon.CreateSpawners(view, m_ViewController, spawnerCount).Single();
            VFXTestCommon.CreateSystems(view, m_ViewController, GPUSystemsCount, 0);

            // Create a point cache operator
            var pCacheAssetPath = "Assets/AllTests/VFXTests/GraphicsTests/UnityLogoPrimeCount.pcache";
            var copypCacheAssetPath = Path.Combine(VFXTestCommon.tempBasePath, "pointCache.pcache");
            File.Copy(pCacheAssetPath, copypCacheAssetPath, true);
            AssetDatabase.ImportAsset(copypCacheAssetPath);
            var pointCacheAsset = AssetDatabase.LoadAssetAtPath(copypCacheAssetPath, typeof(PointCacheAsset));
            var pointCacheOperator = VFXLibrary.GetOperators().Single(x => x.modelType == typeof(VFXOperatorPointCache)).CreateInstance() as VFXOperatorPointCache;
            pointCacheOperator.SetSettingValue("Asset", pointCacheAsset);
            m_ViewController.AddVFXModel(Vector2.zero, pointCacheOperator);
            yield return null;

            // Create a set position from map
            var setPositionBlock = VFXLibrary.GetBlocks().Single(x => x.name == "Set".Label(false).AppendLiteral("Position from Map").AppendLabel("2D")).CreateInstance() as AttributeFromMap;
            var initializeContext = m_ViewController.contexts.Single(x => x.model is VFXBasicInitialize);
            initializeContext.model.LinkFrom(spawner, 0, 0);
            initializeContext.AddBlock(0, setPositionBlock);
            setPositionBlock.GetInputSlot(0).Link(pointCacheOperator.GetOutputSlot(1));
            m_ViewController.ApplyChanges();
            yield return null;

            // Copy paste them
            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }
            view.CopySelectionCallback();
            // We delete the point cache asset to check that it does not break the past operation
            AssetDatabase.DeleteAsset(copypCacheAssetPath);
            view.PasteCallback();
            m_ViewController.ApplyChanges();
            yield return null;

            Assert.AreEqual(1, spawner.outputFlowSlot.Length);
            Assert.AreEqual(1, initializeContext.model.inputFlowSlot.Length);
            Assert.AreEqual(1, initializeContext.model.outputFlowSlot.Length);
        }

        [UnityTest, Description("UUM-75894")]
        public IEnumerator CopyPasteContextWithCustomAttribute()
        {
            var viewController1 = m_ViewController;
            var window1 = EditorWindow.GetWindow<VFXViewWindow>();
            var view1 = window1.graphView;
            view1.controller = viewController1;

            // Create a BasicInitialize context
            var initContextDesc = VFXLibrary.GetContexts().First(x => x.modelType == typeof(VFXBasicInitialize));
            var newContext = viewController1.AddVFXContext(new Vector2(100, 100), initContextDesc.variant);
            viewController1.LightApplyChanges();
            var contextController = viewController1.allChildren.OfType<VFXContextController>().Single();
            yield return null;

            // Add a custom attribute
            var customAttributeName = "initPos";
            viewController1.graph.TryAddCustomAttribute(customAttributeName, VFXValueType.Float3, "No description", false, out var attribute);
            yield return null;

            // Add a block to that context
            var setInitPos = VFXLibrary.GetBlocks().First(x => x.modelType == typeof(SetAttribute)).CreateInstance();
            setInitPos.SetSettingValue("attribute", customAttributeName);
            contextController.AddBlock(0, setInitPos);
            viewController1.LightApplyChanges();
            yield return null;

            // Select the created context and copy
            view1.ClearSelection();
            view1.AddToSelection(view1.Query().OfType<VFXContextUI>().First());
            window1.graphView.CopySelectionCallback();
            yield return null;

            // Create a second asset and open window
            CreateTestAsset();
            var viewController2 = m_ViewController;
            var window2 = EditorWindow.GetWindow<VFXViewWindow>();
            var view2 = window2.graphView;
            view2.controller = viewController2;

            // Paste selection in the second window
            window2.graphView.PasteCallback();
            yield return null;

            // Check that the second window has a single custom attribute of type Vector3
            Assert.IsTrue(viewController2.graph.TryFindCustomAttributeDescriptor(customAttributeName, out var customAttributeDescriptor));
            Assert.IsNotNull(customAttributeDescriptor);
            Assert.AreEqual(CustomAttributeUtility.Signature.Vector3, customAttributeDescriptor.type);
        }

        [UnityTest, Description("UUM-75894")]
        public IEnumerator CopyPasteOperatorWithCustomAttribute()
        {
            var viewController1 = m_ViewController;
            var window1 = EditorWindow.GetWindow<VFXViewWindow>();
            var view1 = window1.graphView;
            view1.controller = viewController1;

            // Add a custom attribute
            var customAttributeName = "initPos";
            viewController1.graph.TryAddCustomAttribute(customAttributeName, VFXValueType.Float3, "No description", false, out var attribute);
            yield return null;

            // Add a block to that context
            var getInitPosDesc = VFXLibrary.GetOperators().First(x => x.modelType == typeof(VFXAttributeParameter));
            var getInitPos = viewController1.AddVFXOperator(Vector2.zero, getInitPosDesc.variant);
            getInitPos.SetSettingValue("attribute", customAttributeName);

            viewController1.LightApplyChanges();
            yield return null;

            // Select the created context and copy
            view1.ClearSelection();
            view1.AddToSelection(view1.Query().OfType<VFXOperatorUI>().First());
            window1.graphView.CopySelectionCallback();
            yield return null;

            // Create a second asset and open window
            CreateTestAsset();
            var viewController2 = m_ViewController;
            var window2 = EditorWindow.GetWindow<VFXViewWindow>();
            var view2 = window2.graphView;
            view2.controller = viewController2;

            // Paste selection in the second window
            window2.graphView.PasteCallback();
            yield return null;

            // Check that the second window has a single custom attribute of type Vector3
            Assert.IsTrue(viewController2.graph.TryFindCustomAttributeDescriptor(customAttributeName, out var customAttributeDescriptor));
            Assert.IsNotNull(customAttributeDescriptor);
            Assert.AreEqual(CustomAttributeUtility.Signature.Vector3, customAttributeDescriptor.type);
        }
        [UnityTest, Description("UUM-75893")]
        public IEnumerator CopyPasteMultipleParametersWithEdges()
        {
            VFXViewWindow window = EditorWindow.GetWindow<VFXViewWindow>();
            VFXView view = window.graphView;
            view.controller = m_ViewController;

            // Create a parameter and add a two nodes to the graph
            var parameter = m_ViewController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(x => x.modelType == typeof(float)).variant);
            m_ViewController.LightApplyChanges();

            var parameterController = m_ViewController.GetParameterController(parameter);
            parameterController.model.AddNode(new Vector2(123, 456));
            parameterController.model.AddNode(new Vector2(123, 556));
            m_ViewController.LightApplyChanges();
            var parameterNode1 = parameterController.nodes.First();
            var parameterNode2 = parameterController.nodes.Last();
            Assert.AreNotEqual(parameterNode1, parameterNode2);
            yield return null;

            // Create a Add operator
            var addOperator = VFXLibrary.GetOperators().Single(x => x.modelType == typeof(Add));
            m_ViewController.AddNode(new Vector2(300, 500), addOperator.variant, null);
            m_ViewController.LightApplyChanges();
            var addOperatorController = m_ViewController.nodes.OfType<VFXOperatorController>().Last();
            yield return null;

            // Create links
            m_ViewController.CreateLink(addOperatorController.inputPorts.First(), parameterNode1.outputPorts.Single());
            m_ViewController.CreateLink(addOperatorController.inputPorts.Skip(1).First(), parameterNode2.outputPorts.Single());
            m_ViewController.LightApplyChanges();
            yield return null;

            Assert.AreEqual(2, m_ViewController.dataEdges.Count);

            // Select all and copy/paste
            window.graphView.ExecuteCommand(ExecuteCommandEvent.GetPooled("SelectAll"));
            window.graphView.CopySelectionCallback();
            window.graphView.GetType().GetProperty(nameof(VFXView.pasteCenter), BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(window.graphView, window.graphView.contentViewContainer.LocalToWorld(new Vector2(123, 650)));
            window.graphView.PasteCallback();
            yield return null;

            Assert.AreEqual(4, m_ViewController.dataEdges.Count);
        }

    }
}
#endif
