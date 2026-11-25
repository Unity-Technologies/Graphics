using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.Operator;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.GraphModel
{
    [TestFixture]
    internal class VFXContextSpaceTests
    {
        [Test]
        public void SpaceConversion_Conversion_Expected_Between_Slot_Block_And_Context()
        {
            var initializeContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var positionSphere = ScriptableObject.CreateInstance<PositionShape>();
            positionSphere.SetSettingValue("shape", PositionShapeBase.Type.Sphere);
            initializeContext.AddChild(positionSphere);

            //Default is expected to be in same space between block & context
            var slotSpherePositionExpressions = VFXTestCommon.CollectParentExpression(positionSphere.inputSlots[0][0][0].GetExpression()).ToArray();
            Assert.IsTrue(slotSpherePositionExpressions.Any());
            Assert.IsFalse(slotSpherePositionExpressions.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));

            //Now switch space of block
            initializeContext.space = VFXSpace.World;
            slotSpherePositionExpressions = VFXTestCommon.CollectParentExpression(positionSphere.inputSlots[0][0][0].GetExpression()).ToArray();
            Assert.IsTrue(slotSpherePositionExpressions.Count(o => o.operation == VFXExpressionOperation.LocalToWorld) == 1);
        }

        const string testAssetName = "Assets/TmpTests/VFXGraph_Space.vfx";
        [Test]
        public void Initial_Space_Supposed_To_Be_Same_As_Context()
        {
            //Cover also case 1163442, this behavior only exists in controller
            var directoryPath = Path.GetDirectoryName(testAssetName);
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            if (File.Exists(testAssetName))
                AssetDatabase.DeleteAsset(testAssetName);

            VisualEffectAsset asset = VisualEffectAssetEditorUtility.CreateNewAsset(testAssetName);
            var resource = asset.GetResource(); // force resource creation
            var viewController = VFXViewController.GetController(resource);
            viewController.useCount++;
            var startUndoGroupId = Undo.GetCurrentGroup();

            var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            updateContext.space = VFXSpace.World;
            viewController.AddVFXModel(Vector2.zero, updateContext);
            viewController.ApplyChanges();
            viewController.ForceReload();

            var collision = ScriptableObject.CreateInstance<CollisionShape>();
            var contextController = viewController.allChildren.OfType<VFXContextController>().First();
            contextController.AddBlock(0, collision, true);

            Assert.IsTrue(collision.inputSlots.Where(o => o.spaceable).All(o => o.space == VFXSpace.World));

            viewController.useCount--;
            Undo.RevertAllDownToGroup(startUndoGroupId);
            AssetDatabase.DeleteAsset(testAssetName);
        }

        [Test]
        public void Space_MainCamera_To_Block_ProjectOnDepth()
        {
            var mainCamera = ScriptableObject.CreateInstance<MainCamera>();
            var initialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var projectOnDepth = ScriptableObject.CreateInstance<Block.PositionDepth>();
            projectOnDepth.SetSettingValue("camera", CameraMode.Custom);

            initialize.space = VFXSpace.World;
            mainCamera.outputSlots[0].Link(projectOnDepth.inputSlots[0]);
            initialize.AddChild(projectOnDepth);

            foreach (var slot in mainCamera.outputSlots[0].GetExpressionSlots())
            {
                var expressions = VFXTestCommon.CollectParentExpression(slot.GetExpression()).ToArray();
                Assert.IsFalse(expressions.Any(o => o.operation == VFXExpressionOperation.LocalToWorld || o.operation == VFXExpressionOperation.WorldToLocal));
            }

            var slotMatrixMainCameraOutput = mainCamera.outputSlots[0][0];
            var slotMatrixProjectInput = projectOnDepth.inputSlots[0][0];

            var expressionSlotMatrix = VFXTestCommon.CollectParentExpression(slotMatrixMainCameraOutput.GetExpression()).ToArray();
            Assert.IsTrue(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.ExtractMatrixFromMainCamera));
            Assert.IsFalse(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.WorldToLocal || o.operation == VFXExpressionOperation.LocalToWorld));

            expressionSlotMatrix = VFXTestCommon.CollectParentExpression(slotMatrixProjectInput.GetExpression()).ToArray();
            Assert.IsTrue(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.ExtractMatrixFromMainCamera));
            Assert.IsFalse(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.WorldToLocal || o.operation == VFXExpressionOperation.LocalToWorld));

            initialize.space = VFXSpace.Local;

            expressionSlotMatrix = VFXTestCommon.CollectParentExpression(slotMatrixMainCameraOutput.GetExpression()).ToArray();
            Assert.IsTrue(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.ExtractMatrixFromMainCamera));
            Assert.IsFalse(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.WorldToLocal || o.operation == VFXExpressionOperation.LocalToWorld)); //never at this stage

            expressionSlotMatrix = VFXTestCommon.CollectParentExpression(slotMatrixProjectInput.GetExpression()).ToArray();
            Assert.IsTrue(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.ExtractMatrixFromMainCamera));
            Assert.IsTrue(expressionSlotMatrix.Any(o => o.operation == VFXExpressionOperation.WorldToLocal)); //context in local, transform World to Local is excpected
        }
    }
}
