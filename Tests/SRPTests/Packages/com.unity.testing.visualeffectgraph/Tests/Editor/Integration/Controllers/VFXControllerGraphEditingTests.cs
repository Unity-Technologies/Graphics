using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.VFX;

using UnityEditor.VFX.Block;
using UnityEditor.VFX.Block.Test;
using UnityEditor.VFX.UI;
using System.Reflection;

using static UnityEditor.VFX.Test.VFXControllerTestHelper;

namespace UnityEditor.VFX.Test.Integration.Controllers
{
    [TestFixture]
    public class VFXControllerGraphEditingTests
    {
        private VFXViewController m_ViewController;
        private int m_StartUndoGroupId;

        [SetUp]
        public void CreateTestAsset()
        {
            m_ViewController = VFXControllerTestHelper.CreateTestAsset();
            m_StartUndoGroupId = Undo.GetCurrentGroup();
        }

        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [OneTimeTearDown]
        public void OnTimeCleanup()
        {
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            VFXControllerTestHelper.DestroyTestAsset(m_ViewController, m_StartUndoGroupId, null);
        }

        #pragma warning disable 0414
        static private bool[] usePosition = { true, false };

        #pragma warning restore 0414
        [Test]
        public void LinkPositionOrVectorAndDirection([ValueSource("usePosition")] bool usePosition)
        {
            var crossDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.variant.name.Contains("Cross"));
            var positionDesc = VFXLibrary.GetParameters().FirstOrDefault(o => o.variant.name.Contains("Position"));
            var vectorDesc = VFXLibrary.GetParameters().FirstOrDefault(o => o.name == "Vector");
            var directionDesc = VFXLibrary.GetParameters().FirstOrDefault(o => o.variant.name.Contains("Direction"));

            var cross = m_ViewController.AddVFXOperator(new Vector2(1, 1), crossDesc.variant);
            var position = m_ViewController.AddVFXParameter(new Vector2(2, 2), positionDesc.variant);
            var vector = m_ViewController.AddVFXParameter(new Vector2(3, 3), vectorDesc.variant);
            var direction = m_ViewController.AddVFXParameter(new Vector2(4, 4), directionDesc.variant);
            (cross as IVFXOperatorUniform).SetOperandType(typeof(Vector3));

            m_ViewController.ApplyChanges();

            Func<IVFXSlotContainer, VFXNodeController> fnFindController = delegate(IVFXSlotContainer slotContainer)
            {
                var allController = m_ViewController.allChildren.OfType<VFXNodeController>();
                return allController.FirstOrDefault(o => o.slotContainer == slotContainer);
            };

            var controllerCross = fnFindController(cross);

            var vA = new Vector3(2, 3, 4);
            position.outputSlots[0].value = new Position() { position = vA };
            vector.outputSlots[0].value = new Vector() { vector = vA };

            var vB = new Vector3(5, 6, 7);
            direction.outputSlots[0].value = new DirectionType() { direction = vB };

            var edgeControllerAppend_A = new VFXDataEdgeController(controllerCross.inputPorts.Where(o => o.portType == typeof(Vector3)).First(), fnFindController(usePosition ? position : vector).outputPorts.First());
            m_ViewController.AddElement(edgeControllerAppend_A);
            (cross as IVFXOperatorUniform).SetOperandType(typeof(Vector3));
            m_ViewController.ApplyChanges();

            var edgeControllerAppend_B = new VFXDataEdgeController(controllerCross.inputPorts.Where(o => o.portType == typeof(Vector3)).Last(), fnFindController(direction).outputPorts.First());
            m_ViewController.AddElement(edgeControllerAppend_B);
            (cross as IVFXOperatorUniform).SetOperandType(typeof(Vector3));
            m_ViewController.ApplyChanges();

            m_ViewController.ForceReload();

            Assert.AreEqual(1, cross.inputSlots[0].LinkedSlots.Count());
            Assert.AreEqual(1, cross.inputSlots[1].LinkedSlots.Count());

            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding);

            var currentA = context.Compile(cross.inputSlots[0].GetExpression()).Get<Vector3>();
            var currentB = context.Compile(cross.inputSlots[1].GetExpression()).Get<Vector3>();
            var result = context.Compile(cross.outputSlots[0].GetExpression()).Get<Vector3>();

            Assert.AreEqual((double)vA.x, (double)currentA.x, 0.001f);
            Assert.AreEqual((double)vA.y, (double)currentA.y, 0.001f);
            Assert.AreEqual((double)vA.z, (double)currentA.z, 0.001f);

            Assert.AreEqual((double)vB.normalized.x, (double)currentB.x, 0.001f);
            Assert.AreEqual((double)vB.normalized.y, (double)currentB.y, 0.001f);
            Assert.AreEqual((double)vB.normalized.z, (double)currentB.z, 0.001f);

            var expectedResult = Vector3.Cross(vA, vB.normalized);
            Assert.AreEqual((double)expectedResult.x, (double)result.x, 0.001f);
            Assert.AreEqual((double)expectedResult.y, (double)result.y, 0.001f);
            Assert.AreEqual((double)expectedResult.z, (double)result.z, 0.001f);
        }

        [Test]
        public void LinkToDirection()
        {
            var directionDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.modelType == typeof(VFXInlineOperator) && o.HasSettingValue(typeof(DirectionType)));
            var vector3Desc = VFXLibrary.GetOperators().FirstOrDefault(o => o.modelType == typeof(VFXInlineOperator) && o.HasSettingValue(typeof(Vector3)));

            var direction = m_ViewController.AddVFXOperator(new Vector2(1, 1), directionDesc.variant);
            var vector3 = m_ViewController.AddVFXOperator(new Vector2(2, 2), vector3Desc.variant);
            m_ViewController.ApplyChanges();

            Func<IVFXSlotContainer, VFXNodeController> fnFindController = delegate(IVFXSlotContainer slotContainer)
            {
                var allController = m_ViewController.allChildren.OfType<VFXNodeController>();
                return allController.FirstOrDefault(o => o.slotContainer == slotContainer);
            };

            var vA = new Vector3(8, 9, 6);
            vector3.inputSlots[0].value = vA;

            var controllerDirection = fnFindController(direction);
            var controllerVector3 = fnFindController(vector3);

            var edgeControllerAppend_A = new VFXDataEdgeController(controllerDirection.inputPorts.First(), controllerVector3.outputPorts.First());
            m_ViewController.AddElement(edgeControllerAppend_A);
            m_ViewController.ApplyChanges();

            m_ViewController.ForceReload();

            Assert.AreEqual(1, direction.inputSlots[0].LinkedSlots.Count());
            var context = new VFXExpression.Context(VFXExpressionContextOption.CPUEvaluation | VFXExpressionContextOption.ConstantFolding);

            var result = context.Compile(direction.outputSlots[0].GetExpression()).Get<Vector3>();

            Assert.AreEqual((double)vA.normalized.x, (double)result.x, 0.001f);
            Assert.AreEqual((double)vA.normalized.y, (double)result.y, 0.001f);
            Assert.AreEqual((double)vA.normalized.z, (double)result.z, 0.001f);
        }

        [Test]
        public void DeleteSubSlotWithLink()
        {
            var crossProductDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.variant.name.Contains("Cross"));
            var sinDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.variant.name.Contains("Sine"));
            var cosDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.variant.name.Contains("Cosine"));

            var crossProduct = m_ViewController.AddVFXOperator(new Vector2(0, 0), crossProductDesc.variant);
            var sin = m_ViewController.AddVFXOperator(new Vector2(8, 8), sinDesc.variant);
            var cos = m_ViewController.AddVFXOperator(new Vector2(-8, 8), cosDesc.variant);

            m_ViewController.ApplyChanges();

            crossProduct.outputSlots[0].children.ElementAt(1).Link(sin.inputSlots[0]);
            crossProduct.outputSlots[0].children.ElementAt(1).Link(cos.inputSlots[0]);

            var crossController = m_ViewController.allChildren.OfType<VFXOperatorController>().First(o => o.model.name.Contains("Cross"));
            m_ViewController.RemoveElement(crossController);

            Assert.IsFalse(cos.inputSlots[0].HasLink(true));
            Assert.IsFalse(sin.inputSlots[0].HasLink(true));
        }

        [Test]
        public void Avoid_Loop_In_Flow_Input()
        {
            var spawner_A = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var spawner_B = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var spawner_C = ScriptableObject.CreateInstance<VFXBasicSpawner>();

            spawner_B.LinkFrom(spawner_A);
            spawner_C.LinkFrom(spawner_B);

            m_ViewController.graph.AddChild(spawner_A);
            m_ViewController.graph.AddChild(spawner_B);
            m_ViewController.graph.AddChild(spawner_C);

            m_ViewController.LightApplyChanges();

            var flowAnchorController = m_ViewController.allChildren.OfType<VFXContextController>().SelectMany(o => o.flowInputAnchors.Concat(o.flowOutputAnchors));
            var outputControllers = flowAnchorController.Where(o => o.owner == spawner_C && o.direction == Experimental.GraphView.Direction.Output).ToArray();
            Assert.AreEqual(1, outputControllers.Length);

            var compatiblePorts = m_ViewController.GetCompatiblePorts(outputControllers[0], null);
            Assert.AreEqual(0, compatiblePorts.Count);
        }

        [Test]
        public void UniqueDefaultSystemNames()
        {
            VFXViewWindow window = VFXTestCommon.GetViewWindow();
            VFXView view = window.graphView;
            view.controller = m_ViewController;

            const int count = 16;
            var spawners = VFXTestCommon.CreateSpawners(view, m_ViewController, count);

            var systemNames = view.controller.graph.systemNames;
            var names = new List<string>();
            foreach (var spawner in spawners)
                names.Add(systemNames.GetUniqueSystemName(spawner.GetData()));

            Assert.IsTrue(names.Where(name => !string.IsNullOrEmpty(name)).Distinct().Count() == count, "Some spawners have the same name or are null or empty.");

            var GPUSystems = VFXTestCommon.GetFieldValue<VFXView, List<VFXSystemBorder>>(view, "m_Systems");
            VFXTestCommon.CreateSystems(view, m_ViewController, count, count);
            var uniqueSystemNames = GPUSystems.Select(system => system.controller.title).Distinct();

            Assert.IsTrue(uniqueSystemNames.Count() == count, "Some GPU systems have the same name or are null or empty.");
        }

        [UnityTest, Description("Repro from UUM-113869")]
        public IEnumerator Group_Selection_No_Delete_Empty_Groups()
        {
            //Prepare Asset
            var vfxGraph = VFXTestCommon.CreateGraph_And_System();
            var vfxPath = AssetDatabase.GetAssetPath(vfxGraph);

            AssetDatabase.ImportAsset(vfxPath);
            yield return null;

            //Prepare Controller
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsNotNull(asset);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0));

            var window = VFXTestCommon.GetWindow(asset);
            window.LoadAsset(asset, null);
            var controller = window.graphView.controller;

            controller.AddStickyNote(Vector2.one * 400, null);
            controller.AddGroupNode(500 * Vector2.right);

            for (int i = 0; i < 4; i++)
                yield return null;

            var stickyNoteController = controller.stickyNotes.Single(); // This will confirm there's only one sticky note
            controller.GroupNodes(Array.Empty<VFXNodeController>(), new[] { stickyNoteController });
            yield return null;

            Assert.AreEqual(2, controller.groupNodes.Count);
        }

        [Test]
        public void CheckMinMaxRangeParameter()
        {
            // Arrange
            var rangeParameter = m_ViewController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(t => t.modelType == typeof(uint)).variant);
            m_ViewController.LightApplyChanges();
            var rangeParameterController = m_ViewController.GetParameterController(rangeParameter);
            rangeParameterController.valueFilter = VFXValueFilter.Range;

            rangeParameterController.minValue = 0u;
            rangeParameterController.maxValue = 100u;
            rangeParameterController.value = 1u;
            m_ViewController.LightApplyChanges();

            // Act
            rangeParameterController.minValue = 200u;

            // Assert
            Assert.AreEqual(0, rangeParameterController.minValue);
            Assert.AreEqual(1, rangeParameterController.value);
            Assert.AreEqual(100, rangeParameterController.maxValue);
        }

        [Test]
        public void MakeNameUnique_WithConflict_AndZeroMaxLength_DoesNotThrow()
        {
            // Reproduces calls made by DuplicateBlackBoardCategory (VFXView.cs:3329)
            // and AddCategory (VFXBlackboard.cs:819) which pass maxLength=0 (default)
            // while the candidate name conflicts with an existing one.
            var existingNames = new HashSet<string> { "Speed" };

            string uniqueName = null;
            Assert.DoesNotThrow(() =>
            {
                uniqueName = VFXParameterController.MakeNameUnique("Speed", existingNames, true, 0, null);
            });

            Assert.IsNotNull(uniqueName);
            Assert.AreNotEqual("Speed", uniqueName);
            Assert.IsFalse(existingNames.Contains(uniqueName));
        }
    }
}
