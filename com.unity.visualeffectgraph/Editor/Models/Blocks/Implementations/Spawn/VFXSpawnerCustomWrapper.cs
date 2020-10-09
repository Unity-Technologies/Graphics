using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class CustomSpawnerVariant : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "m_customType", VFXLibrary.FindConcreteSubclasses(typeof(VFXSpawnerCallbacks)).Select(o => new SerializableType(o) as object).ToArray() }
                };
            }
        }
    }

    [VFXInfo(category = "Custom", variantProvider = typeof(CustomSpawnerVariant))]
    class VFXSpawnerCustomWrapper : VFXAbstractSpawner
    {
        [SerializeField, VFXSetting]
        protected SerializableType m_customType;

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                yield return "m_customType";
            }
        }


        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);

            if (m_customType != null)
            {
                var function = ScriptableObject.CreateInstance(m_customType);
                var monoScript = MonoScript.FromScriptableObject(function);
                if (monoScript != null)
                    dependencies.Add(monoScript.GetInstanceID());
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                return customBehavior == null ? Enumerable.Empty<VFXPropertyWithValue>() : PropertiesFromType(customBehavior.GetRecursiveNestedType(GetInputPropertiesTypeName()));
            }
        }

        protected override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            if (customBehavior == null
                && !object.ReferenceEquals(m_customType, null)
                && !string.IsNullOrEmpty(m_customType.text))
            {
                manager.RegisterError("CustomSpawnerIDNotFound", VFXErrorType.Error, "Can't found assembly : " + m_customType.text);
            }
        }

        public override sealed string name
        {
            get
            {
                if (customBehavior != null)
                    return ObjectNames.NicifyVariableName(customBehavior.Name);
                return "null";
            }
        }
        public override sealed Type customBehavior { get { return (Type)m_customType; } }
        public override sealed VFXTaskType spawnerType { get { return VFXTaskType.CustomCallbackSpawner; } }
    }
}
