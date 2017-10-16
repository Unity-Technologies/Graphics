using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.VFX
{
    interface IStringProvider
    {
        string[] GetAvailableString();
    }

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
            var attribute = VFXAttribute.Find(this.attribute, location);
            var expression = new VFXAttributeExpression(attribute);
            return new VFXExpression[] { expression };
        }

        abstract public VFXAttributeLocation location { get; }
    }
}
