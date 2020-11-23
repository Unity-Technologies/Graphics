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
        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.None)]
        protected SerializableType m_customType;

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.None)]
        protected ScriptableObject m_instance;

        private void ResolveCustomCallbackInstance()
        {
            //m_instance always prevails on m_customType, we cannot modify m_customType twice.
            if (m_instance != null)
                m_customType = m_instance.GetType();

            //m_instance is null in three cases :
            // - Newly created VFXSpawnerCustomWrapper, m_customType changed by SetSettingValue.
            // - VFXSpawnerCallbacks has been suppressed, in that case, m_customType.text can display a message and m_customType == null.
            // - VFXSpawnerCallbacks has been restored, m_customType != null, rebuild the m_instance.
            if (m_customType != null && m_instance == null)
                m_instance = CreateInstance(m_customType);
        }

        public override void Sanitize(int version)
        {
            if (m_instance == null && !object.ReferenceEquals(m_customType, null))
            {
                //Handle previous name based serialization, these class move to UnityEngine.VFX
                if (m_customType.text.StartsWith("UnityEditor.VFX.LoopAndDelay,"))
                    m_customType = typeof(LoopAndDelay);
                else if (m_customType.text.StartsWith("UnityEditor.VFX.SetSpawnTime,"))
                    m_customType = typeof(SetSpawnTime);
                else if (m_customType.text.StartsWith("UnityEditor.VFX.SpawnOverDistance,"))
                    m_customType = typeof(SpawnOverDistance);
                else if (m_customType.text.StartsWith("IncrementStripIndexOnStart,"))
                    m_customType = typeof(IncrementStripIndexOnStart);

                ResolveCustomCallbackInstance();
            }
            base.Sanitize(version);
        }

        protected internal override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);
            ResolveCustomCallbackInstance();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            ResolveCustomCallbackInstance();
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly)
        {
            base.CollectDependencies(objs, ownedOnly);
            objs.Add(m_instance);
        }

        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);
            if (customBehavior != null)
                dependencies.Add(customBehavior.GetInstanceID());
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (m_instance == null)
                    return Enumerable.Empty<VFXPropertyWithValue>();
                return PropertiesFromType(m_instance.GetType().GetRecursiveNestedType(GetInputPropertiesTypeName()));
            }
        }

        protected override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            //Type isn't reachable ... but we already stored a type, log an error.
            if (m_customType == null
                && !object.ReferenceEquals(m_customType, null)
                && !string.IsNullOrEmpty(m_customType.text))
            {
                manager.RegisterError("CustomSpawnerIDNotFound", VFXErrorType.Error, "Can't found assembly : " + m_customType.text);
            }

            if (customBehavior == null && m_customType != null)
            {
                manager.RegisterError("CustomSpawnerIDInvalid", VFXErrorType.Error, "Invalid scriptable object : " + (Type)m_customType);
            }
        }

        public override sealed string name
        {
            get
            {
                if (m_instance != null)
                    return ObjectNames.NicifyVariableName(m_instance.GetType().Name);
                return "null";
            }
        }

        public override sealed MonoScript customBehavior { get { return MonoScript.FromScriptableObject(m_instance); } }
        public override sealed VFXTaskType spawnerType { get { return VFXTaskType.CustomCallbackSpawner; } }
    }
}
