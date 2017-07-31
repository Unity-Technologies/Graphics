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
        public class Settings
        {
            [StringProvider(typeof(AttributeProvider))]
            public string attribute = VFXAttribute.All.First();
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var settings = GetSettings<Settings>();
            var attribute = VFXAttribute.Find(settings.attribute, location);
            var expression = new VFXAttributeExpression(attribute);
            return new VFXExpression[] { expression };
        }

        abstract public VFXAttributeLocation location { get; }
    }
}
