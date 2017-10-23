using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    class AttributeProvider : IStringProvider
    {
        public string[] GetAvailableString()
        {
            return VFXAttribute.All;
        }
    }

    abstract class VFXAttributeParameter : VFXOperator
    {
        [VFXSetting]
        [StringProvider(typeof(AttributeProvider))]
        public string attribute = VFXAttribute.All.First();


        override public string name { get { return string.Format("{0} {1}", location.ToString(), attribute); } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var attribute = VFXAttribute.Find(this.attribute);
            var expression = new VFXAttributeExpression(attribute, location);
            return new VFXExpression[] { expression };
        }

        abstract public VFXAttributeLocation location { get; }
    }
}
