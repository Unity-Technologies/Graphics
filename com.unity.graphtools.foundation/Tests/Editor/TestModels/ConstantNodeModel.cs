using System;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class ConstantNodeModel : NodeModel, IConstantNodeModel
    {
        public IPortModel OutputPort => NodeModelDefaultImplementations.GetOutputPort(this);
        public IPortModel MainOutputPort => OutputPort;

        public object ObjectValue
        {
            get => Value.ObjectValue;
            set => Value.ObjectValue = value;
        }

        public Type Type => Value.Type;
        public bool IsLocked { get; set; }
        public IConstant Value { get; set; }
        public void PredefineSetup()
        {
            Value.ObjectValue = Value.DefaultValue;
        }

        public void SetValue<T>(T value)
        {
            if (!(value is Enum) && Type != value.GetType() && !value.GetType().IsSubclassOf(Type))
                throw new ArgumentException($"can't set value of type {value.GetType().Name} in {Type.Name}");
            Value.ObjectValue = value;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            this.AddDataOutputPort(null, Value.Type.GenerateTypeHandle());
        }
    }
}
