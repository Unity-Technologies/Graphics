using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXSphereOutput : VFXAbstractParticleOutput
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected bool specularWorkflow = false;

        public override string name { get { return "Lit Sphere Output"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticleSphere"); } }
        public override VFXTaskType taskType { get { return VFXTaskType.ParticleQuadOutput; } }
        public override bool supportsFlipbooks { get { return false; } }

        public override void OnEnable()
        {
            blendMode = BlendMode.Opaque; // TODO use masked
            base.OnEnable();
        }

        public class InputProperties
        {
            [Range(0, 1)]
            public float smoothness = 0.5f;
        }

        public class SpecularWorkFlowProperties
        {
            public Color specularColor;
        }

        public class MetallicWorkflowProperties
        {
            [Range(0, 1)]
            public float metallic = 0.5f;
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                properties = properties.Concat(PropertiesFromType(specularWorkflow ? "SpecularWorkFlowProperties" : "MetallicWorkflowProperties"));
                return properties;
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);

                foreach (var size in VFXBlockUtility.GetReadableSizeAttributes(GetData()))
                    yield return size;
            }
        }
        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            yield return slotExpressions.First(o => o.name == "smoothness");

            if (specularWorkflow)
                yield return slotExpressions.First(o => o.name == "specularColor");
            else
                yield return slotExpressions.First(o => o.name == "metallic");
        }

        protected override IEnumerable<VFXBlock> implicitPostBlock
        {
            get
            {
                var orient = CreateInstance<Orient>();
                orient.mode = Orient.Mode.FaceCameraPosition;
                yield return orient;
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return "cullMode";
                yield return "blendMode";
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                if (specularWorkflow)
                    yield return "USE_SPECULAR_WORKFLOW";
            }
        }
    }
}
