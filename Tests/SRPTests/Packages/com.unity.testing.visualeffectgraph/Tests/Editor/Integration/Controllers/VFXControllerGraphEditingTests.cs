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

        [TestCase("New Vector 3", ExpectedResult = "New Vector 3 (1)")]
        [TestCase("X (1)", ExpectedResult = "X (2)")]
        [TestCase("Speed 2", ExpectedResult = "Speed 2 (1)")]
        [TestCase("SPEED 2", ExpectedResult = "SPEED 2 (1)")]
        [TestCase("X (-1)", ExpectedResult = "X (-1) (1)")]
        [TestCase("X ( 1)", ExpectedResult = "X ( 1) (1)")]
        public string MakeNameUnique_WithSpaces_OnlyReadsParenthesizedCounter(string name)
        {
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "New Vector 3", "X (1)", "Speed 2", "X (-1)", "X ( 1)" };

            return VFXParameterController.MakeNameUnique(name, existingNames, true, VFXParameterController.kMaxExposedNameLength, null);
        }

        [Test]
        public void MakeNameUnique_WithConflictAtMaxLength_CutsNameToFitCounter()
        {
            var name = new string('a', VFXParameterController.kMaxExposedNameLength);
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { name };

            string uniqueName = VFXParameterController.MakeNameUnique(name, existingNames, true, VFXParameterController.kMaxExposedNameLength, null);

            Assert.AreEqual(new string('a', VFXParameterController.kMaxExposedNameLength - 4) + " (1)", uniqueName);
        }

        [TestCase("Seed", ExpectedResult = "Seed_1")]
        [TestCase("Speed_1", ExpectedResult = "Speed_2")]
        public string MakeNameUnique_WithoutSpaces_AppendsUnderscoreCounter(string name)
        {
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Seed", "Speed_1" };

            return VFXParameterController.MakeNameUnique(name, existingNames, false, VFXParameterController.kMaxExposedNameLength, null);
        }

        [TestCase("Group (2147483647)", true, ExpectedResult = "Group (2147483648)")]
        [TestCase("X (99999999999999999999)", true, ExpectedResult = "X (100000000000000000000)")]
        [TestCase("Seed_2147483647", false, ExpectedResult = "Seed_2147483648")]
        [TestCase("Seed_99999999999999999999", false, ExpectedResult = "Seed_100000000000000000000")]
        public string MakeNameUnique_WithCounterBeyondInt_IncrementsCounter(string name, bool allowSpace)
        {
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { name };

            return VFXParameterController.MakeNameUnique(name, existingNames, allowSpace, VFXParameterController.kMaxExposedNameLength, null);
        }

        [TestCase("_free", ExpectedResult = "_free")]
        [TestCase("_velocity", ExpectedResult = "_velocity_1")]
        [TestCase("velocity_", ExpectedResult = "velocity__1")]
        [TestCase("_1", ExpectedResult = "_2")]
        [TestCase("___", ExpectedResult = null)]
        public string MakeNameUnique_WithoutSpaces_KeepsUnderscores(string name)
        {
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "_velocity", "velocity_", "_1" };

            return VFXParameterController.MakeNameUnique(name, existingNames, false, VFXParameterController.kMaxExposedNameLength, null);
        }

        [TestCase("\tSpeed", ExpectedResult = "Speed")]
        [TestCase("Speed\u00A0", ExpectedResult = "Speed")]
        [TestCase(" \nSpeed \t", ExpectedResult = "Speed")]
        [TestCase("Taken\t", ExpectedResult = "Taken (1)")]
        [TestCase("\t", ExpectedResult = null)]
        public string MakeNameUnique_WithSpaces_TrimsWhitespace(string name)
        {
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Taken" };

            return VFXParameterController.MakeNameUnique(name, existingNames, true, VFXParameterController.kMaxExposedNameLength, null);
        }

        [Test]
        public void MakeNameUnique_WithCutEndingInWhitespace_DropsWhitespaceBeforeCounter()
        {
            var prefix = new string('a', VFXParameterController.kMaxExposedNameLength - 6);
            string name = prefix + " b (9)";
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { name };

            string uniqueName = VFXParameterController.MakeNameUnique(name, existingNames, true, VFXParameterController.kMaxExposedNameLength, null);

            Assert.AreEqual(prefix + " (10)", uniqueName);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MakeNameUnique_WithCounterFillingMaxLength_RestartsCounter(bool allowSpace)
        {
            int maxLength = VFXParameterController.kMaxExposedNameLength;
            string name = allowSpace ? "a (" + new string('9', maxLength - 4) + ")" : "a_" + new string('9', maxLength - 2);
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { name };

            string uniqueName = VFXParameterController.MakeNameUnique(name, existingNames, allowSpace, maxLength, null);

            string expectedName = allowSpace ? name.Substring(0, maxLength - 4) + " (1)" : name.Substring(0, maxLength - 2) + "_1";
            Assert.AreEqual(expectedName, uniqueName);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MakeNameUnique_DuplicatedTenTimesAtMaxLength_CountsFromOne(bool allowSpace)
        {
            int maxLength = VFXParameterController.kMaxExposedNameLength;
            string name = allowSpace ? "a (" + new string('9', maxLength - 4) + ")" : "a_" + new string('9', maxLength - 2);
            var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { name };

            string uniqueName = null;
            for (int i = 0; i < 10; ++i)
            {
                uniqueName = VFXParameterController.MakeNameUnique(name, existingNames, allowSpace, maxLength, null);
                Assert.LessOrEqual(uniqueName.Length, maxLength);
                existingNames.Add(uniqueName);
            }

            string expectedName = allowSpace ? name.Substring(0, maxLength - 5) + " (10)" : name.Substring(0, maxLength - 3) + "_10";
            Assert.AreEqual(expectedName, uniqueName);
        }

        [Test]
        public void AddVFXParameter_Vector3Twice_KeepsTypeNameInDefaultName()
        {
            VFXModelDescriptorParameters vector3Descriptor = VFXLibrary.GetParameters().First(x => x.modelType == typeof(Vector3));

            VFXParameter first = m_ViewController.AddVFXParameter(Vector2.zero, vector3Descriptor.variant);
            m_ViewController.ApplyChanges();
            VFXParameter second = m_ViewController.AddVFXParameter(Vector2.zero, vector3Descriptor.variant);
            m_ViewController.ApplyChanges();

            Assert.AreEqual("New Vector 3", first.exposedName);
            Assert.AreEqual("New Vector 3 (1)", second.exposedName);
        }

        [Test]
        public void OpenGraph_WithDuplicateParameterNames_UsesParenthesizedCounter()
        {
            VFXModelDescriptorParameters floatDescriptor = VFXLibrary.GetParameters().First(x => x.modelType == typeof(float));

            VFXParameter first = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();
            VFXParameter second = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();
            VFXParameter third = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();

            first.SetSettingValue("m_ExposedName", "Speed");
            second.SetSettingValue("m_ExposedName", "Speed");
            third.SetSettingValue("m_ExposedName", "_Value_");

            // A new controller sanitizes the parameter names of the graph
            VisualEffectResource resource = m_ViewController.model;
            m_ViewController.useCount--;
            m_ViewController = VFXViewController.GetController(resource);
            m_ViewController.useCount++;

            Assert.AreEqual("Speed", first.exposedName);
            Assert.AreEqual("Speed (1)", second.exposedName);
            Assert.AreEqual("_Value_", third.exposedName);
        }

        [Test]
        public void OpenGraph_WithDuplicateName_DoesNotTakeNameOfLaterParameter()
        {
            VFXModelDescriptorParameters floatDescriptor = VFXLibrary.GetParameters().First(x => x.modelType == typeof(float));

            VFXParameter first = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();
            VFXParameter second = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();
            VFXParameter third = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();

            first.SetSettingValue("m_ExposedName", "Speed");
            second.SetSettingValue("m_ExposedName", "Speed");
            third.SetSettingValue("m_ExposedName", "Speed (1)");

            // A new controller sanitizes the parameter names of the graph
            VisualEffectResource resource = m_ViewController.model;
            m_ViewController.useCount--;
            m_ViewController = VFXViewController.GetController(resource);
            m_ViewController.useCount++;

            Assert.AreEqual("Speed", first.exposedName);
            Assert.AreEqual("Speed (2)", second.exposedName);
            Assert.AreEqual("Speed (1)", third.exposedName);
        }

        [Test]
        public void OpenGraph_WithOverLengthName_DoesNotTakeNameOfLaterParameter()
        {
            var longName = new string('a', VFXParameterController.kMaxExposedNameLength);
            string cutName = new('a', VFXParameterController.kMaxExposedNameLength - 4);
            VFXModelDescriptorParameters floatDescriptor = VFXLibrary.GetParameters().First(x => x.modelType == typeof(float));

            VFXParameter original = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();
            VFXParameter copy = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();
            VFXParameter later = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();

            // Duplicating a category didn't apply the length limit to the copied parameters
            original.SetSettingValue("m_ExposedName", longName);
            copy.SetSettingValue("m_ExposedName", longName + " 1");
            later.SetSettingValue("m_ExposedName", cutName + " (1)");

            // A new controller sanitizes the parameter names of the graph
            VisualEffectResource resource = m_ViewController.model;
            m_ViewController.useCount--;
            m_ViewController = VFXViewController.GetController(resource);
            m_ViewController.useCount++;

            Assert.AreEqual(longName, original.exposedName);
            Assert.AreEqual(cutName + " (2)", copy.exposedName);
            Assert.AreEqual(cutName + " (1)", later.exposedName);
        }

        [Test]
        public void OpenGraph_WithOverLengthFirstName_CutsName()
        {
            string cutName = new('a', VFXParameterController.kMaxExposedNameLength - 4);
            VFXModelDescriptorParameters floatDescriptor = VFXLibrary.GetParameters().First(x => x.modelType == typeof(float));

            VFXParameter first = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();
            VFXParameter second = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();

            first.SetSettingValue("m_ExposedName", new string('a', VFXParameterController.kMaxExposedNameLength + 1));
            second.SetSettingValue("m_ExposedName", "Speed");

            // A new controller sanitizes the parameter names of the graph
            VisualEffectResource resource = m_ViewController.model;
            m_ViewController.useCount--;
            m_ViewController = VFXViewController.GetController(resource);
            m_ViewController.useCount++;

            Assert.AreEqual(cutName + " (1)", first.exposedName);
            Assert.AreEqual("Speed", second.exposedName);
        }

        [Test]
        public void OpenGraph_WithWhitespaceAroundName_TrimsName()
        {
            VFXModelDescriptorParameters floatDescriptor = VFXLibrary.GetParameters().First(x => x.modelType == typeof(float));

            VFXParameter first = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();
            VFXParameter second = m_ViewController.AddVFXParameter(Vector2.zero, floatDescriptor.variant);
            m_ViewController.ApplyChanges();

            first.SetSettingValue("m_ExposedName", "Speed\u00A0");
            second.SetSettingValue("m_ExposedName", "\tAngle");

            // A new controller sanitizes the parameter names of the graph
            VisualEffectResource resource = m_ViewController.model;
            m_ViewController.useCount--;
            m_ViewController = VFXViewController.GetController(resource);
            m_ViewController.useCount++;

            Assert.AreEqual("Speed", first.exposedName);
            Assert.AreEqual("Angle", second.exposedName);
        }
    }
}
