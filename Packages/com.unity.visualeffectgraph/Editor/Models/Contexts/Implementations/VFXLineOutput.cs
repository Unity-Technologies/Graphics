using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.VFX
{
    [VFXHelpURL("Context-OutputLine")]
    [VFXInfo]
    class VFXLineOutput : VFXAbstractParticleOutput
    {
        public override string name { get { return "Output Particle Line"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate(useNativeLines ? "VFXParticleLinesHW" : "VFXParticleLinesSW"); } }
        public override VFXTaskType taskType { get { return useNativeLines ? VFXTaskType.ParticleLineOutput : VFXTaskType.ParticleQuadOutput; } }
        public override bool implementsMotionVector { get { return true; } }

        [VFXSetting, SerializeField, FormerlySerializedAs("targetFromAttributes"), Tooltip("When enabled, a custom offset from the particle position can be specified for the particle line to connect to. When disabled, the line connects with the particle’s Target Position attribute.")]
        protected bool useTargetOffset = true;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the Line Output will render native line primitives. These might be faster on some platforms, but they cannot be anti-aliased.")]
        protected bool useNativeLines = false;

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return "cullMode";
                yield return "colorMapping";
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            cullMode = CullMode.Off;
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);

                if (useTargetOffset)
                {
                    yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);

                    yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);

                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.ReadWrite);
                    yield return new VFXAttributeInfo(VFXAttribute.TargetPosition, VFXAttributeMode.Write);
                }
                else
                {
                    yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                    yield return new VFXAttributeInfo(VFXAttribute.TargetPosition, VFXAttributeMode.Read);
                }
            }
        }

        public class TargetOffsetProperties
        {
            [Tooltip("Sets an offset from the particle position for the line to connect to.")]
            public Vector3 targetOffset = Vector3.up;
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            if (useTargetOffset)
                yield return slotExpressions.First(o => o.name == "targetOffset");
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                if (useTargetOffset)
                    properties = PropertiesFromType("TargetOffsetProperties").Concat(properties);

                return properties;
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                if (useTargetOffset)
                    yield return "TARGET_FROM_ATTRIBUTES";
            }
        }
    }
}
