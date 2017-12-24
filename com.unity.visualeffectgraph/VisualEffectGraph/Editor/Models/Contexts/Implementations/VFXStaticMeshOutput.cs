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
        [VFXSetting, SerializeField]
        private Shader shader;

        protected VFXStaticMeshOutput() : base(VFXContextType.kOutput, VFXDataType.kNone, VFXDataType.kNone) {}

        public override bool CanBeCompiled()
        {
            return shader != null;
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
    }
}
