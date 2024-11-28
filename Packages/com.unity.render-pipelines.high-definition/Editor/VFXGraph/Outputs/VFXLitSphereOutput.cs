using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.Block;

namespace UnityEditor.VFX.HDRP
{
    [VFXInfo(name = "Output Particle|HDRP Lit|Sphere", category = "#5Output Debug", experimental = true)]
    class VFXLitSphereOutput : VFXAbstractParticleHDRPLitOutput
    {
        public override string name => "Output Particle".AppendLabel("HDRP Lit", false) + "\nSphere";
        public override string codeGeneratorTemplate => RenderPipeTemplate("VFXParticleSphere");
        public override VFXTaskType taskType => VFXTaskType.ParticleQuadOutput;

        protected override bool allowTextures => false;

        public override void OnEnable()
        {
            blendMode = BlendMode.Opaque; // TODO use masked
            doubleSided = false;
            base.OnEnable();
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                if (colorMode != ColorMode.None)
                    yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);

                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXBlock> implicitPostBlock
        {
            get
            {
                var orient = GetOrCreateImplicitBlock<Orient>(GetData());
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

                yield return nameof(cullMode);
                yield return nameof(blendMode);
                yield return nameof(useAlphaClipping);
                yield return nameof(doubleSided);
                yield return nameof(shaderGraph);
                yield return nameof(enableRayTracing);
            }
        }

        protected override IEnumerable<string> untransferableSettings
        {
            get
            {
                foreach (var setting in base.untransferableSettings)
                {
                    yield return setting;
                }
                yield return nameof(blendMode);
                yield return nameof(doubleSided);
                yield return nameof(shaderGraph);
                yield return nameof(enableRayTracing);
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var define in base.additionalDefines)
                    yield return define;

                yield return "_CONSERVATIVE_DEPTH_OFFSET";
            }
        }
    }
}
