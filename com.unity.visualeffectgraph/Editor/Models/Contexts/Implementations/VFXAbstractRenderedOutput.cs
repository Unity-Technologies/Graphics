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
            if (m_SubOutputs == null)
                m_SubOutputs = new List<VFXSRPSubOutput>();

            m_CurrentSubOutput = GetOrCreateSubOutput();

            base.OnEnable();
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
