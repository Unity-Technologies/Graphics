using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class CustomSpawnerVariant : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            var types = VFXLibrary.FindConcreteSubclasses(typeof(VFXSpawnerCallbacks))
                .Where(x => x != typeof(LoopAndDelay)) //Explicitly exclude loop and delay from listing, preferably use VFXSpawnContext settings instead
                .ToArray();
            foreach (var type in types)
            {
                yield return new Variant(
                    ObjectNames.NicifyVariableName(type.Name),
                    $"Spawn/Custom",
                    typeof(VFXSpawnerCustomWrapper),
                    new[] {new KeyValuePair<string, object>("m_customType", new SerializableType(type))});
            }
        }
    }

    [VFXInfo(variantProvider = typeof(CustomSpawnerVariant))]
    class VFXSpawnerCustomWrapper : VFXAbstractSpawner
    {
        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.None)]
        protected SerializableType m_customType;

        [SerializeField, VFXSetting(VFXSettingAttribute.VisibleFlags.None)]
        protected MonoScript m_customScript;

        private void ResolveCustomCallbackInstance()
        {
            //m_customScript always prevails on m_customType, we cannot modify m_customType twice.
            if (m_customScript != null && m_customScript.GetClass() != null)
                m_customType = m_customScript.GetClass();

            //m_customScript is null in three cases
            // - Newly created VFXSpawnerCustomWrapper, m_customType changed by SetSettingValue.
            // - VFXSpawnerCallbacks has been suppressed, in that case, m_customType.text can display a message and m_customType == null.
            // - VFXSpawnerCallbacks has been restored, m_customType != null, rebuild the m_instance.
            if (m_customScript == null && m_customType != null)
            {
                var instance = CreateInstance(m_customType);
                m_customScript = MonoScript.FromScriptableObject(instance);
            }
        }

        public override void Sanitize(int version)
        {
            if (m_customScript == null && !object.ReferenceEquals(m_customType, null))
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

        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);
            if (customBehavior != null)
            {
                dependencies.Add(customBehavior.GetInstanceID());
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                if (m_customType == null)
                    return Enumerable.Empty<VFXPropertyWithValue>();
                return PropertiesFromType(((Type)m_customType).GetRecursiveNestedType(GetInputPropertiesTypeName()));
            }
        }

        internal sealed override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            //Type isn't reachable ... but we already stored a type, log an error.
            if (m_customType == null)
            {
                if (!object.ReferenceEquals(m_customType, null) && !string.IsNullOrEmpty(m_customType.text))
                    report.RegisterError("CustomSpawnerIDNotFound", VFXErrorType.Error, "The serialized reference to a VFXSpawnerCallbacks script is missing : " + m_customType.text, this);
                else
                    report.RegisterError("CustomSpawnerIDNull", VFXErrorType.Error, "The serialized reference to a VFXSpawnerCallbacks script is missing.", this);
            }
            else if ((Type)m_customType == typeof(LoopAndDelay))
            {
                report.RegisterError("CustomSpawnerLoopAndDelay", VFXErrorType.Warning, "The block Loop And Delay is now deprecated in favor of the spawn context settings in inspector.", this);
            }

            if (customBehavior == null && m_customType != null)
            {
                if (m_customScript != null && m_customScript.GetClass() != null)
                    report.RegisterError("CustomSpawnerIDNotVFXSpawnerCallbacks", VFXErrorType.Error, string.Format("{0} isn't a VFXSpawnerCallbacks", m_customScript.GetClass()), this);
                else
                    report.RegisterError("CustomSpawnerIDInvalid", VFXErrorType.Error, "Invalid ScriptableObject : " + (Type)m_customType, this);
            }
        }

        public override sealed string name
        {
            get
            {
                if (m_customType != null)
                    return ObjectNames.NicifyVariableName(((Type)m_customType).Name);
                return "Missing VFXSpawnerCallbacks";
            }
        }

        public override sealed MonoScript customBehavior
        {
            get
            {
                if (m_customScript != null && typeof(VFXSpawnerCallbacks).IsAssignableFrom(m_customScript.GetClass()))
                    return m_customScript;
                return null;
            }
        }
        public override sealed VFXTaskType spawnerType { get { return VFXTaskType.CustomCallbackSpawner; } }
    }
}
