using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class BlockNodeTests
    {
        static BlockFieldDescriptor s_DescriptorA = new BlockFieldDescriptor("Test", "BlockA", string.Empty, new FloatControl(0.5f), ShaderStage.Fragment, true);
        static BlockFieldDescriptor s_DescriptorB = new BlockFieldDescriptor("Test", "BlockB", string.Empty, new NormalControl(CoordinateSpace.World), ShaderStage.Fragment, true);

        static Vector3MaterialSlot s_MaterialSlot = new Vector3MaterialSlot(0, "Test", "BlockB", SlotType.Input, Vector3.one);
        static CustomSlotBlockFieldDescriptor s_CustomSlotDescriptor = new CustomSlotBlockFieldDescriptor("Test", "CustomBlock", string.Empty,
            () => { return new Vector3MaterialSlot(0, "Test", "BlockB", SlotType.Input, Vector3.one); });

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void CanGatherBlockDescriptors()
        {
            GraphData graph = new GraphData();
            graph.AddContexts();

            Assert.IsNotNull(graph.blockFieldDescriptors);
            Assert.AreNotEqual(0, graph.blockFieldDescriptors.Count);
        }

        [Test]
        public void CanInitializeBlockNode()
        {
            var node = new BlockNode();
            node.Init(s_DescriptorA);

            Assert.IsNotNull(node.descriptor);
            Assert.AreEqual(s_DescriptorA, node.descriptor);
            Assert.AreEqual("Test.BlockA", $"{node.descriptor.tag}.{node.descriptor.name}");
        }

        [Test]
        public void CanCreateSlotFromBlockDescriptor()
        {
            var node = new BlockNode();
            node.Init(s_DescriptorA);
            List<MaterialSlot> slots = new List<MaterialSlot>();
            node.GetSlots(slots);

            Assert.IsNotNull(slots);
            Assert.AreEqual(1, slots.Count);

            var vector3Slot = slots[0] as Vector1MaterialSlot;
            Assert.IsNotNull(vector3Slot);
            Assert.AreEqual(0, vector3Slot.id);
            Assert.AreEqual(s_DescriptorA.displayName, vector3Slot.RawDisplayName());
            Assert.AreEqual(s_DescriptorA.name, vector3Slot.shaderOutputName);
            Assert.AreEqual(SlotType.Input, vector3Slot.slotType);
            Assert.AreEqual(((FloatControl)s_DescriptorA.control).value, vector3Slot.value);
            Assert.AreEqual(s_DescriptorA.shaderStage.GetShaderStageCapability(), vector3Slot.stageCapability);
        }

        [Test]
        public void CanCreateSlotFromCustomSlotBlockDescriptor()
        {
            var node = new BlockNode();
            node.Init(s_CustomSlotDescriptor);
            List<MaterialSlot> slots = new List<MaterialSlot>();
            node.GetSlots(slots);

            Assert.IsNotNull(slots);
            Assert.AreEqual(1, slots.Count);
            Assert.AreNotEqual(s_MaterialSlot, slots[0]); //We actually WANT to create a new slot in this case
            Assert.AreEqual(s_MaterialSlot.displayName, slots[0].displayName);
            Assert.AreEqual(s_MaterialSlot.valueType, slots[0].valueType);
            Assert.AreEqual(s_MaterialSlot.value, ((Vector3MaterialSlot)slots[0]).value);
        }

        [Test]
        public void CanGetRequirementsFromBlockNode()
        {
            var node = new BlockNode();
            node.Init(s_DescriptorB);

            var iMayRequireNormals = node as IMayRequireNormal;
            Assert.IsNotNull(iMayRequireNormals);

            var neededCoordinateSpace = iMayRequireNormals.RequiresNormal(ShaderStageCapability.Fragment);
            Assert.AreEqual(NeededCoordinateSpace.World, neededCoordinateSpace);
        }

        [Test]
        public void CanSerializeDescriptor()
        {
            var node = new BlockNode();
            node.Init(s_DescriptorA);
            node.OnBeforeSerialize();

            Assert.AreEqual("Test.BlockA", node.serializedDescriptor);
        }

        [Test]
        public void CanGetBlockIndex()
        {
            GraphData graph = new GraphData();
            graph.AddContexts();

            var node = new BlockNode();
            node.Init(s_DescriptorA);
            graph.AddBlock(node, graph.fragmentContext, 0);

            Assert.AreEqual(0, node.index);
        }
    }
}
