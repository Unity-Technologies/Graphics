using System;
using System.Collections.Generic;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXStaticMeshOutput : VFXContext, IVFXSubRenderer
    {
        [VFXSetting, Tooltip("Specifies the shader with which the mesh output is rendered.")]
        private Shader shader; // not serialized here but in VFXDataMesh

        [VFXSetting(VFXSettingAttribute.VisibleFlags.None), SerializeField, Header("Rendering Options")]
        protected int sortPriority = 0;

        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField, Tooltip("When enabled, the mesh output will cast shadows.")]
        protected bool castShadows = false;

        // IVFXSubRenderer interface
        // TODO Could we derive this directly by looking at the shader to know if a shadow pass is present?
        public virtual bool hasShadowCasting { get { return castShadows; } }

        public virtual bool hasMotionVector { get { return false; } } //TODO

        int IVFXSubRenderer.sortPriority
        {
            get
            {
                return sortPriority;
            }
            set
            {
                if (sortPriority != value)
                {
                    sortPriority = value;
                    Invalidate(InvalidationCause.kSettingChanged);
                }
            }
        }

        protected VFXStaticMeshOutput() : base(VFXContextType.Output, VFXDataType.Mesh, VFXDataType.None) {}

        public override void OnEnable()
        {
            base.OnEnable();
            shader = ((VFXDataMesh)GetData()).shader;
        }

        public override bool SetupCompilation()
        {
            shader = ((VFXDataMesh)GetData()).shader;

            return true;
        }

        protected override void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model == this && cause == VFXModel.InvalidationCause.kSettingChanged)
            {
                var data = (VFXDataMesh)GetData();
                data.shader = shader;
            }

            base.OnInvalidate(model, cause);
        }

        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);

            if (!object.ReferenceEquals(shader, null))
            {
                Shader shader = ((VFXDataMesh)GetData()).shader;

                dependencies.Add(shader.GetInstanceID());
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Mesh), "mesh"), VFXResources.defaultResources.mesh);
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Transform), "transform"), Transform.defaultValue);
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(uint), "subMeshMask", new VFXPropertyAttribute(VFXPropertyAttribute.Type.kBitField)), uint.MaxValue);


                if (GetData() != null)
                {
                    Shader copyShader = ((VFXDataMesh)GetData()).shader;

                    if (copyShader != null)
                    {
                        var mat = ((VFXDataMesh)GetData()).GetOrCreateMaterial();
                        var propertyAttribs = new List<object>(1);
                        for (int i = 0; i < ShaderUtil.GetPropertyCount(copyShader); ++i)
                        {
                            if (ShaderUtil.IsShaderPropertyHidden(copyShader, i) || ShaderUtil.IsShaderPropertyNonModifiableTexureProperty(copyShader, i))
                                continue;

                            Type propertyType = null;
                            object propertyValue = null;

                            var propertyName = ShaderUtil.GetPropertyName(copyShader, i);
                            var propertyNameId = Shader.PropertyToID(propertyName);

                            propertyAttribs.Clear();

                            switch (ShaderUtil.GetPropertyType(copyShader, i))
                            {
                                case ShaderUtil.ShaderPropertyType.Color:
                                    propertyType = typeof(Color);
                                    propertyValue = mat.GetColor(propertyNameId);
                                    break;
                                case ShaderUtil.ShaderPropertyType.Vector:
                                    propertyType = typeof(Vector4);
                                    propertyValue = mat.GetVector(propertyNameId);
                                    break;
                                case ShaderUtil.ShaderPropertyType.Float:
                                    propertyType = typeof(float);
                                    propertyValue = mat.GetFloat(propertyNameId);
                                    break;
                                case ShaderUtil.ShaderPropertyType.Range:
                                    propertyType = typeof(float);
                                    float minRange = ShaderUtil.GetRangeLimits(copyShader, i, 1);
                                    float maxRange = ShaderUtil.GetRangeLimits(copyShader, i, 2);
                                    propertyAttribs.Add(new RangeAttribute(minRange, maxRange));
                                    propertyValue = mat.GetFloat(propertyNameId);
                                    break;
                                case ShaderUtil.ShaderPropertyType.TexEnv:
                                {
                                    switch (ShaderUtil.GetTexDim(copyShader, i))
                                    {
                                        case TextureDimension.Tex2D:
                                            propertyType = typeof(Texture2D);
                                            break;
                                        case TextureDimension.Tex3D:
                                            propertyType = typeof(Texture3D);
                                            break;
                                        default:
                                            break;     // TODO
                                    }
                                    propertyValue = mat.GetTexture(propertyNameId);
                                    break;
                                }
                                default:
                                    break;
                            }

                            if (propertyType != null)
                            {
                                propertyAttribs.Add(new TooltipAttribute(ShaderUtil.GetPropertyDescription(copyShader, i)));
                                yield return new VFXPropertyWithValue(new VFXProperty(propertyType, propertyName, VFXPropertyAttribute.Create(propertyAttribs.ToArray())), propertyValue);
                            }
                        }
                    }
                }
            }
        }

        public override string name { get { return "Output Mesh"; } }
        public override string codeGeneratorTemplate { get { return null; } }
        public override VFXTaskType taskType { get { return VFXTaskType.Output; } }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var meshData = (VFXDataMesh)GetData();

            switch (target)
            {
                case VFXDeviceTarget.GPU:
                {
                    var mapper = new VFXExpressionMapper();
                    for (int i = 2; i < GetNbInputSlots(); ++i)
                    {
                        VFXExpression exp = GetInputSlot(i).GetExpression();
                        VFXProperty prop = GetInputSlot(i).property;

                        // As there's not shader generation here, we need expressions that can be evaluated on CPU
                        if (exp.IsAny(VFXExpression.Flags.NotCompilableOnCPU))
                            throw new InvalidOperationException(string.Format("Expression for slot {0} must be evaluable on CPU: {1}", prop.name, exp));

                        // needs to convert to srgb as color are linear in vfx graph
                        // This should not be performed for colors with the attribute [HDR] and be performed for vector4 with the attribute [Gamma]
                        // But property attributes cannot seem to be accessible from C# :(
                        if (prop.type == typeof(Color))
                            exp = VFXOperatorUtility.LinearToGamma(exp);

                        mapper.AddExpression(exp, prop.name, -1);
                    }
                    return mapper;
                }

                case VFXDeviceTarget.CPU:
                {
                    var mapper = new VFXExpressionMapper();
                    mapper.AddExpression(GetInputSlot(0).GetExpression(), "mesh", -1);
                    mapper.AddExpression(GetInputSlot(1).GetExpression(), "transform", -1);
                    mapper.AddExpression(GetInputSlot(2).GetExpression(), "subMeshMask", -1);

                    // TODO Remove this once material are serialized
                    // Add material properties
                    if (shader != null)
                    {
                        var mat = meshData.GetOrCreateMaterial();
                        for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); ++i)
                        {
                            if (ShaderUtil.IsShaderPropertyHidden(shader, i))
                            {
                                var name = ShaderUtil.GetPropertyName(shader, i);
                                var nameId = Shader.PropertyToID(name);
                                if (!mat.HasProperty(nameId))
                                    continue;

                                VFXExpression expr = null;
                                switch (ShaderUtil.GetPropertyType(shader, i))
                                {
                                    case ShaderUtil.ShaderPropertyType.Float:
                                        expr = VFXValue.Constant<float>(mat.GetFloat(nameId));
                                        break;
                                    default:
                                        break;
                                }

                                if (expr != null)
                                    mapper.AddExpression(expr, name, -1);
                            }
                        }
                    }

                    return mapper;
                }

                default:
                    return null;
            }
        }

        public override IEnumerable<VFXMapping> additionalMappings
        {
            get
            {
                yield return new VFXMapping("sortPriority", sortPriority);
            }
        }

        public override void CheckGraphBeforeImport()
        {
            base.CheckGraphBeforeImport();
            // If the graph is reimported it can be because one of its depedency such as the shadergraphs, has been changed.

            ResyncSlots(true);
        }
    }
}
