using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractRenderedOutput : VFXContext
    {
        public enum BlendMode
        {
            Additive,
            Alpha,
            Masked,
            AlphaPremultiplied,
            Opaque,
        }

        [VFXSetting, Header("Render States")]
        public BlendMode blendMode = BlendMode.Alpha;

        public bool isBlendModeOpaque { get { return blendMode == BlendMode.Opaque || blendMode == BlendMode.Masked; } }

        protected VFXAbstractRenderedOutput(VFXDataType dataType) : base(VFXContextType.Output, dataType, VFXDataType.None) { }

        public VFXSRPSubOutput subOutput => m_CurrentSubOutput;
        private VFXSRPSubOutput CreateDefaultSubOutput()
        {
            var defaultSubOutput  = ScriptableObject.CreateInstance<VFXSRPSubOutput>();
            defaultSubOutput.Init(this);
            return defaultSubOutput;
        }

        private VFXSRPSubOutput GetOrCreateSubOutput()
        {
            VFXSRPBinder binder = VFXLibrary.currentSRPBinder;
            if (binder == null)
                return CreateDefaultSubOutput();

            Type outputDataType = binder.SRPOutputDataType;
            if (outputDataType == null)
                return CreateDefaultSubOutput();

            var outputData = m_SubOutputs.FirstOrDefault(d => d != null && d.GetType() == outputDataType);
            if (outputData == null)
            {
                outputData = (VFXSRPSubOutput)ScriptableObject.CreateInstance(outputDataType);
                m_SubOutputs.Add(outputData);
            }

            if (outputData.owner != this)
                outputData.Init(this);

            return outputData;
        }

        public override void OnEnable()
        {
            InitSubOutputs(m_SubOutputs, false);
            base.OnEnable();
        }

        public List<VFXSRPSubOutput> GetSubOutputs()
        {
            return m_SubOutputs;
        }

        public void InitSubOutputs(List<VFXSRPSubOutput> subOutputs, bool invalidate = true)
        {
            m_SubOutputs = subOutputs;
            SanitizeSubOutputs();
            m_CurrentSubOutput = GetOrCreateSubOutput();

            if (invalidate)
                Invalidate(InvalidationCause.kSettingChanged);
        }

        private void SanitizeSubOutputs()
        {
            if (m_SubOutputs == null)
            {
                m_SubOutputs = new List<VFXSRPSubOutput>();
                return;
            }

            // TODO Uncommenting this code will removed SRP data that are unknown, this is probably not what we want
            //int nbRemoved = 0;
            //if ((nbRemoved = m_SubOutputs.RemoveAll(s => s == null)) > 0)
            //    Debug.LogWarningFormat("Remove {0} SRP Sub Outputs that could not be deserialized from {1} of type {2}", nbRemoved, name, GetType());

            var subOutputsTypes = new HashSet<Type>(); // TODO For some reason constructor that takes a capacity does not exist
            for (int i = 0; i < m_SubOutputs.Count; ++i)
            {
                if (m_SubOutputs[i] == null)
                    continue;

                Type subOutputType = m_SubOutputs[i].GetType();
                if (subOutputsTypes.Contains(subOutputType))
                {
                    Debug.LogWarningFormat("Duplicate SRP Sub Output of type {0} found in {1} of type {2}. It is removed", subOutputType, name, GetType());
                    m_SubOutputs.RemoveAt(i);
                    --i;
                }
                else
                    subOutputsTypes.Add(subOutputType);
            }
        }

        public override void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly)
        {
            base.CollectDependencies(objs, ownedOnly);
            foreach (var data in m_SubOutputs)
                if (data != null)
                {
                    objs.Add(data);
                    data.CollectDependencies(objs, ownedOnly);
                }
        }

        public override VFXSetting GetSetting(string name)
        {
            VFXSetting setting = base.GetSetting(name);
            if (!setting.valid)
                setting = subOutput.GetSetting(name);
            return setting;
        }

        public override IEnumerable<VFXSetting> GetSettings(bool listHidden, VFXSettingAttribute.VisibleFlags flags)
        {
            var settings = base.GetSettings(listHidden, flags);
            settings = settings.Concat(subOutput.GetSettings(listHidden, flags));
            return settings;
        }

        protected virtual void WriteBlendMode(VFXShaderWriter writer)
        {
            var blendModeStr = subOutput.GetBlendModeStr();
            if (!String.IsNullOrEmpty(blendModeStr))
                writer.WriteLine(blendModeStr);
        }

        [SerializeField]
        private List<VFXSRPSubOutput> m_SubOutputs;

        [NonSerialized]
        private VFXSRPSubOutput m_CurrentSubOutput;
    }
}
