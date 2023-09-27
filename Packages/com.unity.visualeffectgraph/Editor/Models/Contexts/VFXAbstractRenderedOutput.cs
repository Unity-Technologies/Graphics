using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.VFX
{
    abstract class VFXAbstractRenderedOutput : VFXContext
    {
        public enum BlendMode
        {
            Additive,
            Alpha,
            AlphaPremultiplied,
            Opaque,
        }
        public enum ZWriteMode
        {
            Default,
            Off,
            On
        }
        public enum CullMode
        {
            Default,
            Front,
            Back,
            Off
        }

        public enum ZTestMode
        {
            Default,
            Less,
            Greater,
            LEqual,
            GEqual,
            Equal,
            NotEqual,
            Always
        }

        [VFXSetting, Header("Render States"), Tooltip("Specifies the transparency and blending method for rendering the particles to the screen.")]
        public BlendMode blendMode = BlendMode.Alpha;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies how the particle geometry is culled. This can be used to hide the front or back facing sides or make the mesh double-sided.")]
        protected CullMode cullMode = CullMode.Default;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies whether the particle is writing to the depth buffer.")]
        protected ZWriteMode zWriteMode = ZWriteMode.Default;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("Specifies how the particle rendering is affected by the depth buffer. By default, particles render if they are closer to the camera than solid objects in the scene.")]
        protected ZTestMode zTestMode = ZTestMode.Default;

        [VFXSetting, Tooltip("When enabled, transparent pixels under the specified alpha threshold will be discarded."), SerializeField]
        protected bool useAlphaClipping = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, particles write to the velocity buffer, allowing them to be blurred with the Motion Blur post processing effect.")]
        protected bool generateMotionVector = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), FormerlySerializedAs("excludeFromTAA"), SerializeField, Tooltip("When enabled, particles will not be affected by temporal upscaling and anti-aliasing.")]
        protected bool excludeFromTUAndAA = false;

        public virtual bool isBlendModeOpaque { get { return blendMode == BlendMode.Opaque; } }

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), Range(-50, 50), FormerlySerializedAs("materialOffset"), Delayed, SerializeField, Tooltip("Specifies an offset applied to the material render queue.")]
        protected int sortingPriority = 0;

        public virtual int GetMaterialSortingPriority()
        {
            return sortingPriority;
        }

        public virtual bool hasMotionVector
        {
            get
            {
                return subOutput.supportsMotionVector
                    && implementsMotionVector
                    && generateMotionVector;
            }
        }

        public virtual bool hasAlphaClipping => useAlphaClipping;

        public virtual bool implementsMotionVector { get { return false; } }

        public virtual bool hasExcludeFromTUAndAA => subOutput.supportsExcludeFromTUAndAA && excludeFromTUAndAA;

        protected VFXAbstractRenderedOutput(VFXDataType dataType) : base(VFXContextType.Output, dataType, VFXDataType.None) { }


        public override IEnumerable<int> GetFilteredOutEnumerators(string name)
        {
            return subOutput.GetFilteredOutEnumerators(name);
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                if (!subOutput.supportsSortingPriority)
                    yield return nameof(sortingPriority);
            }
        }

        public VFXSRPSubOutput subOutput
        {
            get
            {
                if (m_CurrentSubOutput == null)
                    m_CurrentSubOutput = GetOrCreateSubOutput();
                return m_CurrentSubOutput;
            }
        }

        private VFXSRPSubOutput CreateDefaultSubOutput()
        {
            var defaultSubOutput = ScriptableObject.CreateInstance<VFXSRPSubOutput>();
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

        public override void OnSRPChanged()
        {
            m_CurrentSubOutput = null;
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

            // Reference equals because we only need to remove actual null sub-output, not the ones that cannot be deserialized
            // Because we want to keep reference to unknown SRP outputs. No log because this is internal clean up
            m_SubOutputs.RemoveAll(s => object.ReferenceEquals(s, null));

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
                objs.Add(data);
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
            if (!string.IsNullOrEmpty(blendModeStr))
                writer.WriteLine(blendModeStr);
            if (hasMotionVector && !isBlendModeOpaque)
                writer.WriteLine("Blend 1 SrcAlpha OneMinusSrcAlpha"); //Blend 1 Off, but allow clipping in forward pass for second render target
        }

        public override void Sanitize(int version)
        {
            if (version < 3) // Fix Blend Modes and useAlphaClipping
            {
                int blendModeValue = (int)blendMode;
                switch (blendModeValue)
                {
                    case 0: // No change required for 0 and 1 (Additive and AlphaBlend)
                    case 1:
                        break;
                    case 2: // Masked
                        SetSettingValue("useAlphaClipping", true);
                        SetSettingValue("blendMode", (int)BlendMode.Opaque);
                        break;
                    case 3: // Alpha Premultiplied
                        SetSettingValue("blendMode", (int)BlendMode.AlphaPremultiplied);

                        break;
                    case 4: // Opaque
                        SetSettingValue("blendMode", (int)BlendMode.Opaque);
                        break;
                    default:
                        break;
                }
            }
            base.Sanitize(version);
        }

        public Material FindMaterial()
        {
            var graph = GetGraph();
            if (graph)
            {
                var resource = graph.GetResource();
                if (resource)
                    return resource.FindMaterial(this);
            }

            return null;
        }

        [SerializeField]
        private List<VFXSRPSubOutput> m_SubOutputs;

        [NonSerialized]
        private VFXSRPSubOutput m_CurrentSubOutput;
    }
}
