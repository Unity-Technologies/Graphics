using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.VFX.Test.Integration.Compilation
{
    [TestFixture]
    public class VFXShapeBlockCompilationTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [TearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [UnityTest]
        public IEnumerator Combinatory_Position_Shape()
        {
            var vfxGraph = VFXTestCommon.CreateGraph_And_System();
            var initialize = vfxGraph.children.OfType<VFXBasicInitialize>().First();

            var orientations = new List<PositionBase.Orientation>(Enum.GetValues(typeof(PositionBase.Orientation)).OfType<PositionBase.Orientation>());
            orientations.Add(PositionBase.Orientation.Axes | PositionBase.Orientation.Direction);

            foreach (PositionBase.PositionMode positionMode in Enum.GetValues(typeof(PositionBase.PositionMode)))
            foreach (PositionBase.SpawnMode spawnMode in Enum.GetValues(typeof(PositionBase.SpawnMode)))
            foreach (PositionShapeBase.Type shape in Enum.GetValues(typeof(PositionShapeBase.Type)))
            foreach (PositionBase.Orientation orientation in orientations)
            {
                var positionShape = ScriptableObject.CreateInstance<PositionShape>();
                positionShape.SetSettingValue("positionMode", positionMode);
                positionShape.SetSettingValue("spawnMode", spawnMode);
                positionShape.SetSettingValue("shape", shape);

                //Cover maximum of written values
                positionShape.SetSettingValue("compositionPosition", AttributeCompositionMode.Blend);
                positionShape.SetSettingValue("compositionAxes", AttributeCompositionMode.Blend);
                positionShape.SetSettingValue("compositionDirection", AttributeCompositionMode.Blend);
                positionShape.SetSettingValue("applyOrientation", orientation);

                initialize.AddChild(positionShape);
            }

            foreach (var yield in VFXTestCommon.CheckCompilation(vfxGraph))
            {
                yield return yield;
            }
        }

        [UnityTest]
        public IEnumerator Combinatory_Collision_Shape()
        {
            var vfxGraph = VFXTestCommon.CreateGraph_And_System();
            var initialize = vfxGraph.children.OfType<VFXBasicInitialize>().First();

            foreach (CollisionBase.Behavior behavior in new[] { CollisionBase.Behavior.Collision, CollisionBase.Behavior.Kill })
            foreach (CollisionBase.Mode mode in Enum.GetValues(typeof(CollisionBase.Mode)))
            foreach (CollisionBase.RadiusMode radiusMode in Enum.GetValues(typeof(CollisionBase.RadiusMode)))
            foreach (CollisionShapeBase.Type shape in Enum.GetValues(typeof(CollisionShapeBase.Type)))
            {
                var collisionShape = ScriptableObject.CreateInstance<CollisionShape>();
                collisionShape.SetSettingValue("behavior", behavior);
                collisionShape.SetSettingValue("mode", mode);
                collisionShape.SetSettingValue("radiusMode", radiusMode);
                collisionShape.SetSettingValue("shape", shape);

                //Cover maximum of written values
                collisionShape.SetSettingValue("roughSurface", true);

                initialize.AddChild(collisionShape);
            }

            foreach (CollisionBase.Behavior behavior in new[] { CollisionBase.Behavior.Collision, CollisionBase.Behavior.Kill })
            foreach (CollisionBase.Mode mode in Enum.GetValues(typeof(CollisionBase.Mode)))
            foreach (CollisionBase.RadiusMode radiusMode in Enum.GetValues(typeof(CollisionBase.RadiusMode)))
            foreach (Type type in new[] { typeof(CollisionSDF), typeof(CollisionDepth) })
            {
                var collisionShape = ScriptableObject.CreateInstance<CollisionShape>();
                collisionShape.SetSettingValue("behavior", behavior);
                collisionShape.SetSettingValue("mode", mode);
                collisionShape.SetSettingValue("radiusMode", radiusMode);

                //Cover maximum of written values
                collisionShape.SetSettingValue("roughSurface", true);
                initialize.AddChild(collisionShape);
            }

            foreach (var yield in VFXTestCommon.CheckCompilation(vfxGraph))
            {
                yield return yield;
            }
        }
    }
}
