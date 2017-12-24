using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXStaticMeshOutput : VFXContext
    {
        [VFXSetting]
        private Shader shader; // not serialized here but in VFXDataMesh

        protected VFXStaticMeshOutput() : base(VFXContextType.kOutput, VFXDataType.kMesh, VFXDataType.kNone) {}

        public override void OnEnable()
        {
            base.OnEnable();
            shader = ((VFXDataMesh)GetData()).shader;
        }

        protected override void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model == this && cause == VFXModel.InvalidationCause.kSettingChanged)
                ((VFXDataMesh)GetData()).shader = shader;

            base.OnInvalidate(model, cause);
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Mesh), "mesh"));
                yield return new VFXPropertyWithValue(new VFXProperty(typeof(Transform), "transform"));

                if (shader != null)
                    for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); ++i)
                    {
                        string propertyName = ShaderUtil.GetPropertyName(shader, i);
                        Type propertyType = null;
                        switch (ShaderUtil.GetPropertyType(shader, i))
                        {
                            case ShaderUtil.ShaderPropertyType.Color:
                                propertyType = typeof(Color);
                                break;
                            case ShaderUtil.ShaderPropertyType.Vector:
                                propertyType = typeof(Vector4);
                                break;
                            case ShaderUtil.ShaderPropertyType.Float:
                            case ShaderUtil.ShaderPropertyType.Range:
                                propertyType = typeof(float);
                                break;
                            case ShaderUtil.ShaderPropertyType.TexEnv:
                            {
                                switch (ShaderUtil.GetTexDim(shader, i))
                                {
                                    case TextureDimension.Tex2D:
                                        propertyType = typeof(Texture2D);
                                        break;
                                    case TextureDimension.Tex3D:
                                        propertyType = typeof(Texture3D);
                                        break;
                                    default:
                                        break; // TODO
                                }
                                break;
                            }
                            default:
                                break;
                        }

                        if (propertyType != null)
                            yield return new VFXPropertyWithValue(new VFXProperty(propertyType, propertyName));
                    }
            }
        }

        public override string name { get { return "Static Mesh Output"; } }
        public override string codeGeneratorTemplate { get { return null; } }
        public override VFXTaskType taskType { get { return VFXTaskType.kOutput; } }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            switch (target)
            {
                case VFXDeviceTarget.GPU:
                {
                    var mapper = new VFXExpressionMapper();
                    for (int i = 2; i < GetNbInputSlots(); ++i)
                        mapper.AddExpression(GetInputSlot(i).GetExpression(), GetInputSlot(i).property.name, -1);
                    return mapper;
                }

                case VFXDeviceTarget.CPU:
                {
                    var mapper = new VFXExpressionMapper();
                    mapper.AddExpression(GetInputSlot(0).GetExpression(), "mesh", -1);
                    mapper.AddExpression(GetInputSlot(1).GetExpression(), "transform", -1);
                    return mapper;
                }

                default:
                    return null;
            }
        }
    }
}
