using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

namespace UnityEditor.VFX.HDRP
{
    [VFXInfo(name = "Output Particle|HDRP Volumetric Fog", category = "#4Output Advanced", experimental = true)]
    class VFXVolumetricFogOutput : VFXAbstractParticleOutput
    {
        public override string name => "Output Particle".AppendLabel("HDRP Lit", false) + "\nVolumetric Fog";
        public override string codeGeneratorTemplate => RenderPipeTemplate("VFXVolumetricFogOutput");
        public override VFXTaskType taskType => VFXTaskType.ParticleQuadOutput;

        [VFXSetting, SerializeField, Tooltip("Specifies how the fog of the output is blended with the fog in the scene.")]
        protected VFXLocalVolumetricFogBlendingMode fogBlendMode = VFXLocalVolumetricFogBlendingMode.Additive;

        [VFXSetting, SerializeField, Tooltip("When Blend Distance is above 0, controls which kind of falloff is applied to the transition area.")]
        public LocalVolumetricFogFalloffMode falloffMode = LocalVolumetricFogFalloffMode.Exponential;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the output will accept a 3D Texture modifying both the color and density of the particle.")]
        protected bool useMaskMap = false;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the particles will start fading depending on their distance to the camera.")]
        protected bool useDistanceFading = false;

        /// <summary>
        /// We only implement non commutative blend modes because it's impossible to sort VFX and Fog volumes together.
        /// </summary>
        public enum VFXLocalVolumetricFogBlendingMode
        {
            Additive = LocalVolumetricFogBlendingMode.Additive,
            Multiply = LocalVolumetricFogBlendingMode.Multiply,
            Min = LocalVolumetricFogBlendingMode.Min,
            Max = LocalVolumetricFogBlendingMode.Max,
        }

        public class VolumetricFogInputProperties
        {
            [Range(0, 1), Tooltip("Distance in meter where density will fade out towards the outside of the sphere.")]
            public float fadeRadius = 0.1f;
            [Min(0), Tooltip("How dense the fog will be. A denser fog will absorb more light, making it look darker.")]
            public float density = 1f;
        }

        public class VolumetricFogMaskProperties
        {
            [Tooltip("The 3D texture used to modify the color and density of the fog.")]
            public Texture3D mask = VFXResources.tileableGradientNoise;
            [Tooltip("Modifies the tiling of the mask texture on each axis individually.")]
            public Vector3 uvScale = Vector3.one;
            [Tooltip("Offsets the texture UVs on each axis individually..")]
            public Vector3 uvBias = Vector3.zero;
        }

        public class DistanceFadeProperties
        {
            [Min(0), Tooltip("Distance at which density fading starts."), Delayed]
            public float distanceFadeStart = 10000;
            [Min(0), Tooltip("Distance at which density fading ends."), Delayed]
            public float distanceFadeEnd = 10000;
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);

                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
            }
        }

        public override VFXOutputUpdate.Features outputUpdateFeatures
            => base.outputUpdateFeatures | VFXOutputUpdate.Features.VolumetricFog;

        public override bool NeedsOutputUpdate() => false;

        public override sealed bool CanBeCompiled()
        {
            return (VFXLibrary.currentSRPBinder is VFXHDRPBinder) && base.CanBeCompiled();
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                foreach (var prop in base.inputProperties)
                    yield return prop;

                foreach (var prop in PropertiesFromType(nameof(VolumetricFogInputProperties)))
                    yield return prop;

                if (useMaskMap)
                    foreach (var prop in PropertiesFromType(nameof(VolumetricFogMaskProperties)))
                        yield return prop;

                if (useDistanceFading)
                    foreach (var prop in PropertiesFromType(nameof(DistanceFadeProperties)))
                        yield return prop;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            var vfxNamedExpressions = slotExpressions as VFXNamedExpression[] ?? slotExpressions.ToArray();
            foreach (var exp in base.CollectGPUExpressions(vfxNamedExpressions))
                yield return exp;

            yield return vfxNamedExpressions.First(o => o.name == nameof(VolumetricFogInputProperties.fadeRadius));
            yield return new VFXNamedExpression(VFXValue.Constant((int)falloffMode), nameof(falloffMode));
            yield return new VFXNamedExpression(VFXValue.Constant((int)fogBlendMode), nameof(fogBlendMode));
            if (useMaskMap)
            {
                yield return vfxNamedExpressions.First(o => o.name == nameof(VolumetricFogMaskProperties.mask));
                yield return vfxNamedExpressions.First(o => o.name == nameof(VolumetricFogMaskProperties.uvScale));
                yield return vfxNamedExpressions.First(o => o.name == nameof(VolumetricFogMaskProperties.uvBias));
            }

            var densityExpr = vfxNamedExpressions.First(o => o.name == nameof(VolumetricFogInputProperties.density));
            yield return new VFXNamedExpression
            {
                name = densityExpr.name,
                exp = new VFXExpressionMax(densityExpr.exp, VFXValue.Constant(0.0f)),
            };

            if (useMaskMap)
            {
                var maskTextureExpression = vfxNamedExpressions.First(o => o.name == nameof(VolumetricFogMaskProperties.mask));
                var condition = VFXOperatorUtility.TextureFormatEquals(maskTextureExpression.exp, TextureFormat.Alpha8);

                yield return new VFXNamedExpression
                {
                    name = "isTextureAlpha8",
                    exp = condition
                };
            }

            if (useDistanceFading)
            {
                var distanceFadeEndExpr = vfxNamedExpressions.First(o => o.name == nameof(DistanceFadeProperties.distanceFadeStart));
                var distanceFadeStartExpr = vfxNamedExpressions.First(o => o.name == nameof(DistanceFadeProperties.distanceFadeEnd));
                var diff = new VFXExpressionSubtract(distanceFadeEndExpr.exp, distanceFadeStartExpr.exp);
                var fadeDistance = new VFXExpressionMax(diff, VFXValue.Constant(0.0001f));
                var rcpFadeDistance = new VFXExpressionDivide(VFXValue.Constant(1.0f), fadeDistance);
                yield return new VFXNamedExpression
                {
                    name = "rcpDistanceFadeLength",
                    exp = rcpFadeDistance,
                };
                var endTimesRcpDistanceFade = new VFXExpressionMul(rcpFadeDistance, distanceFadeEndExpr.exp);
                yield return new VFXNamedExpression
                {
                    name = "endTimesRcpDistanceFadeLength",
                    exp = endTimesRcpDistanceFade,
                };
            }
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var localSpace = ((VFXDataParticle)GetData()).space == VFXSpace.Local;
            var mapper = base.GetExpressionMapper(target);
            if (target == VFXDeviceTarget.GPU && localSpace)
            {
                mapper ??= new VFXExpressionMapper();
                mapper.AddExpression(VFXBuiltInExpression.LocalToWorld, "localToWorld", -1);
            }

            return mapper;
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;

                yield return nameof(blendMode);
                yield return nameof(useAlphaClipping);
                yield return nameof(colorMapping);
                yield return nameof(cullMode);
                yield return nameof(zWriteMode);
                yield return nameof(zTestMode);
                yield return nameof(uvMode);
                yield return nameof(useSoftParticle);
                yield return nameof(indirectDraw);
                yield return nameof(castShadows);
                yield return nameof(useExposureWeight);
                yield return nameof(sort);
                yield return nameof(sortMode);
                yield return nameof(enableRayTracing);
                yield return nameof(revertSorting);
                yield return nameof(excludeFromTUAndAA);
                yield return nameof(computeCulling);
                yield return nameof(vfxSystemSortPriority);
                yield return nameof(sortingPriority);
            }
        }

        protected override IEnumerable<string> untransferableSettings
        {
            get
            {
                foreach (var setting in base.untransferableSettings)
                    yield return setting;
                foreach (var setting in filteredOutSettings)
                    yield return setting;
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var define in base.additionalDefines)
                    yield return define;

                if (useMaskMap)
                {
                    yield return "HDRP_VOLUMETRIC_MASK";

                    if (useDistanceFading)
                         yield return "HDRP_VOLUMETRIC_DISTANCE_FADING";
                }

                if (frustumCulling)
                    yield return "VFX_FEATURE_FRUSTUM_CULL";

                yield return "VFX_PRIMITIVE_QUAD";

                var data = GetData();

                if (data.IsCurrentAttributeWritten(VFXAttribute.AngleX)
                    || data.IsCurrentAttributeWritten(VFXAttribute.AngleY)
                    || data.IsCurrentAttributeWritten(VFXAttribute.AngleZ)
                    || data.IsCurrentAttributeWritten(VFXAttribute.AxisX)
                    || data.IsCurrentAttributeWritten(VFXAttribute.AxisY)
                    || data.IsCurrentAttributeWritten(VFXAttribute.AxisZ))
                {
                    yield return "VFX_APPLY_ANGULAR_ROTATION";
                }
            }
        }

        public override bool HasSorting() => false;
        public override bool isRayTraced => false;

        protected override VFXShaderWriter renderState
        {
            get
            {
                var writer = new VFXShaderWriter();

                writer.WriteLine("ZWrite Off");
                writer.WriteLine("ZTest Always");
                writer.WriteLine("Cull Off");

                FogVolumeAPI.ComputeBlendParameters((LocalVolumetricFogBlendingMode)fogBlendMode, out var srcColorBlend, out var srcAlphaBlend, out var dstColorBlend, out var dstAlphaBlend, out var colorBlendOp, out var alphaBlendOp);

                writer.WriteLine($"Blend {srcColorBlend} {dstColorBlend}, {srcAlphaBlend} {dstAlphaBlend}");
                writer.WriteLine($"BlendOp {colorBlendOp}, {alphaBlendOp}");

                return writer;
            }
        }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            if (!HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.supportVolumetrics ?? false)
            {
                report.RegisterError("VolumetricFogDisabled", VFXErrorType.Warning,
                    $"The current HDRP Asset does not support volumetric fog. To fix this error, go to the Lighting section of your HDRP asset and enable 'Volumetric Fog'.", this);
            }

            var data = GetData();
            if (data != null)
            {
                if (!data.IsCurrentAttributeWritten(VFXAttribute.Size) && !data.IsCurrentAttributeWritten(VFXAttribute.ScaleX))
                {
                    report.RegisterError("SizeTooSmall", VFXErrorType.Warning,
                        $"The size of the fog particle is not modified. This can make the volumetric fog effect invisible because the default size is too small. To fix this, add a size block in your system and increase it's value.", this);
                }
                if (data.IsCurrentAttributeWritten(VFXAttribute.ScaleY) || data.IsCurrentAttributeWritten(VFXAttribute.ScaleZ))
                {
                    report.RegisterError("ScaleYZIgnored", VFXErrorType.Warning,
                        $"The scale on Y and Z axis are ignored by the volumetric fog. Configure your scale component to X only to remove this message.", this);
                }
            }
        }

        public override VFXContextCompiledData PrepareCompiledData()
        {
            var compiledData = base.PrepareCompiledData();
            var outputTask = compiledData.tasks.Last();

            outputTask.bufferMappings.Add(VFXDataParticle.k_IndirectBufferName);
            outputTask.bufferMappings.Add("maxSliceCount");

            compiledData.AllocateIndirectBuffer();

            compiledData.buffers.Add(new VFXContextBufferDescriptor
            {
                baseName = "maxSliceCount",
                size = 1,
                bufferSizeMode = VFXContextBufferSizeMode.FixedSize,
                bufferTarget = GraphicsBuffer.Target.Structured,
                stride = sizeof(uint),
                bufferCount = 1,
            });

            // Volumetric output task need to be inserted before the output work
            compiledData.tasks.Insert(0, new VFXTask
            {
                doesGenerateShader = true,
                templatePath = VisualEffectGraphPackageInfo.assetPackagePath + "/Shaders/VFXVolumetricFogUpdate",
                additionalDefines = new string[] { "VFX_VOLUMETRIC_FOG_PASS_CLEAR", "HAVE_VFX_MODIFICATION" },
                type = VFXTaskType.PerCameraUpdate,
                shaderType = VFXTaskShaderType.ComputeShader,
                bufferMappings = new() { "maxSliceCount" },
                name = "Clear",
            });

            compiledData.tasks.Insert(1, new VFXTask
            {
                doesGenerateShader = true,
                templatePath = VisualEffectGraphPackageInfo.assetPackagePath + "/Shaders/VFXVolumetricFogUpdate",
                additionalDefines = new string[] { "VFX_VOLUMETRIC_FOG_PASS_0", "HAVE_VFX_MODIFICATION" },
                type = VFXTaskType.PerCameraUpdate,
                shaderType = VFXTaskShaderType.ComputeShader,
                bufferMappings = new() { "maxSliceCount"},
                name = "Count",
            });

            compiledData.tasks.Insert(2, new VFXTask
            {
                doesGenerateShader = true,
                templatePath = VisualEffectGraphPackageInfo.assetPackagePath + "/Shaders/VFXVolumetricFogUpdate",
                additionalDefines = new string[] { "VFX_VOLUMETRIC_FOG_PASS_1", "HAVE_VFX_MODIFICATION" },
                type = VFXTaskType.PerCameraUpdate,
                shaderType = VFXTaskShaderType.ComputeShader,
                bufferMappings = new() { "maxSliceCount", new VFXTask.BufferMapping(VFXDataParticle.k_IndirectBufferName, "outputBuffer")},
                name = "Fill",
            });

            return compiledData;
        }
    }
}
