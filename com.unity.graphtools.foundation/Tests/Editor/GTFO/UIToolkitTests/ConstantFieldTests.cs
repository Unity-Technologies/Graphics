using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIToolkitTests
{
    public class ConstantFieldTests : GtfTestFixture
    {
        [Test]
        public void GetRegisterCallbackWorks()
        {
            var constant = new BooleanConstant();
            var field = new ConstantField(constant, null, null);
            Assert.IsNotNull(field.GetRegisterCallback());
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        enum Answer
        {
            Yes,
            No,
        }

        [Test]
        public void GetRegisterCallbackWorksForEnum()
        {
            var constant = new EnumConstant();
            var typeHandle = TypeHandleHelpers.GenerateTypeHandle(typeof(Answer));
            constant.Initialize(typeHandle);

            var field = new ConstantField(constant, null, null);
            Assert.IsNotNull(field.GetRegisterCallback());
        }

        [Test]
        public void GetNewValueWorks()
        {
            var e = ChangeEvent<int>.GetPooled(10, 11);
            var v = ConstantField.GetNewValue(e);
            Assert.AreEqual(11, v);
        }

        [Test]
        public void ConstantValueIsDisplayedAfterCallToUpdateDisplayedValue()
        {
            var constant = new IntConstant();
            constant.Value = 42;
            var field = new ConstantField(constant, null, null);
            field.UpdateDisplayedValue();

            var editField = field.SafeQ(null, BaseField<int>.ussClassName) as BaseField<int>;
            Assert.IsNotNull(editField);
            Assert.AreEqual(42, editField.value);
        }

        [Test]
        public void MixedValueIsDisplayedForConnectedPortAfterCallToUpdateDisplayedValue()
        {
            var node1 = GraphModel.CreateNode<IONodeModel>();
            node1.InputCount = 1;
            node1.DefineNode();
            var node2 = GraphModel.CreateNode<IONodeModel>();
            node2.OutputCount = 1;
            node2.DefineNode();

            var constant = new IntConstant();
            var field = new ConstantField(constant, node1.GetInputPorts().First(), null);
            var editField = field.SafeQ(null, BaseField<int>.ussClassName) as BaseField<int>;
            Assert.IsNotNull(editField);

            field.UpdateDisplayedValue();

#if UNITY_2021_2_OR_NEWER
            Assert.IsFalse(editField.showMixedValue);
            GraphModel.CreateEdge(node1.GetInputPorts().First(), node2.GetOutputPorts().First());
            field.UpdateDisplayedValue();
            Assert.IsTrue(editField.showMixedValue);
#else
            GraphModel.CreateEdge(node1.GetInputPorts().First(), node2.GetOutputPorts().First());
            field.UpdateDisplayedValue();
            Assert.AreEqual(0, editField.value);
#endif
        }

        [Test]
        public void OnChangedIsCalledWhenFieldValueChanges()
        {
            bool onChangeCalled = false;

            var constant = new IntConstant();
            constant.Value = 42;
            var field = new ConstantField(constant, null, null, _ => onChangeCalled = true);
            Window.rootVisualElement.Add(field);

            var editField = field.SafeQ(null, BaseField<int>.ussClassName) as BaseField<int>;
            Assert.IsNotNull(editField);
            editField.value = 78;

            Assert.IsTrue(onChangeCalled);
        }
    }
}
