using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Attribute")]
    class VFXSetAttribute : VFXBlock
    {
        public class Settings
        {
            [StringProvider(typeof(AttributeProvider))]
            public string attribute = VFXAttribute.All.First();
        }

        public override string name { get { return "SetAttribute"; } }
        public override VFXContextType compatibleContexts { get { return VFXContextType.kInitAndUpdate; } }
        public override VFXDataType compatibleData { get { return VFXDataType.kParticle; } }
        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                return new List<VFXAttributeInfo>() { new VFXAttributeInfo(currentAttribute, VFXAttributeMode.Write) };
            }
        }

        public override string functionName
        {
            get
            {
                return base.functionName + "_" + currentAttribute.name;
            }
        }

        public override string source
        {
            get
            {
                return string.Format("{0} = i{0};", currentAttribute.name);
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            UpdateInputFromSettings();
        }

        protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);
            if (cause == InvalidationCause.kSettingChanged)
            {
                UpdateInputFromSettings();
            }
        }

        private VFXAttribute currentAttribute
        {
            get
            {
                return VFXAttribute.Find(GetSettings<Settings>().attribute, VFXAttributeLocation.Current);
            }
        }

        private void UpdateInputFromSettings()
        {
            var attribute = currentAttribute;
            var expression = new VFXAttributeExpression(attribute);
            AddSlot(VFXSlot.Create(new VFXProperty(VFXExpression.TypeToType(expression.ValueType), "i" + attribute.name), VFXSlot.Direction.kInput));
            if (inputSlots.Count == 2)
            {
                CopyLink(inputSlots[0], inputSlots[1]);
                RemoveSlot(inputSlots[0]);
            }
        }
    }
}
