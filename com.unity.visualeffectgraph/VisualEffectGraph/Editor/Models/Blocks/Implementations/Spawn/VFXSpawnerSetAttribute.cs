using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo(category = "Spawner", variantProvider = typeof(AttributeVariantReadWritableNoVariadic))]
    class VFXSpawnerSetAttribute : VFXAbstractSpawner
    {
        [VFXSetting, StringProvider(typeof(AttributeProvider))]
        public string attribute = VFXAttribute.AllReadWritable.First();

        private VFXAttribute currentAttribute
        {
            get
            {
                return VFXAttribute.Find(attribute);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(currentAttribute.type), currentAttribute.name), currentAttribute.value.GetContent());
            }
        }

        public override string name { get { return "Set Attribute " + attribute; } }
        public override VFXTaskType spawnerType { get { return VFXTaskType.SetAttributeSpawner; } }
    }
}
