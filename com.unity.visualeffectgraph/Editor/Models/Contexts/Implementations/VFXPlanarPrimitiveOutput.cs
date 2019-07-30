using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;


namespace UnityEditor.VFX
{

    class VFXShaderGraphParticleOutput : VFXAbstractParticleOutput
    {
        [VFXSetting, SerializeField]
        protected ShaderGraphVfxAsset shaderGraph;

        public class InputProperties
        {
            public Texture2D mainTexture = VFXResources.defaultResources.particleTexture;
        }
        static Type GetSGPropertyType(AbstractShaderProperty property)
        {
            switch (property.propertyType)
            {
                case PropertyType.Color:
                    return typeof(Color);
                case PropertyType.Texture2D:
                    return typeof(Texture2D);
                case PropertyType.Texture2DArray:
                    return typeof(Texture2DArray);
                case PropertyType.Texture3D:
                    return typeof(Texture3D);
                case PropertyType.Cubemap:
                    return typeof(Cubemap);
                case PropertyType.Gradient:
                    return null;
                case PropertyType.Boolean:
                    return typeof(bool);
                case PropertyType.Vector1:
                    return typeof(float);
                case PropertyType.Vector2:
                    return typeof(Vector2);
                case PropertyType.Vector3:
                    return typeof(Vector3);
                case PropertyType.Vector4:
                    return typeof(Vector4);
                case PropertyType.Matrix2:
                    return null;
                case PropertyType.Matrix3:
                    return null;
                case PropertyType.Matrix4:
                    return typeof(Matrix4x4);
                case PropertyType.SamplerState:
                default:
                    return null;
            }
        }

        public static object GetSGPropertyValue(AbstractShaderProperty property)
        {
            switch (property.propertyType)
            {
                case PropertyType.Texture2D:
                    return ((Texture2DShaderProperty)property).value.texture;
                case PropertyType.Texture3D:
                    return ((Texture3DShaderProperty)property).value.texture;
                case PropertyType.Cubemap:
                    return ((CubemapShaderProperty)property).value.cubemap;
                case PropertyType.Texture2DArray:
                    return ((Texture2DArrayShaderProperty)property).value.textureArray;
                default:
                    PropertyInfo info = property.GetType().GetProperty("value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    return info?.GetValue(property);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = null;
                if (shaderGraph == null)
                    properties = base.inputProperties;
                else
                    properties = shaderGraph.properties
                        .Where(t => !t.hidden)
                        .Select(t => new { property = t, type = GetSGPropertyType(t) })
                        .Where(t => t.type != null)
                        .Select(t => new VFXPropertyWithValue(new VFXProperty(t.type, t.property.displayName), GetSGPropertyValue(t.property)));
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;
            if (shaderGraph == null)
            {
                yield return slotExpressions.First(o => o.name == "mainTexture");
            }
            else
            {
                //TODO
            }
        }

    }
    [VFXInfo(variantProvider = typeof(VFXPlanarPrimitiveVariantProvider))]
    class VFXPlanarPrimitiveOutput : VFXShaderGraphParticleOutput
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), SerializeField]
        protected VFXPrimitiveType primitiveType = VFXPrimitiveType.Quad;

        //[VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector)]
        public bool useGeometryShader = false;

        public override string name { get { return primitiveType.ToString() + " Output"; } }
        public override string codeGeneratorTemplate { get { return RenderPipeTemplate("VFXParticlePlanarPrimitive"); } }
        public override VFXTaskType taskType
        {
            get
            {
                if (useGeometryShader)
                    return VFXTaskType.ParticlePointOutput;

                return VFXPlanarPrimitiveHelper.GetTaskType(primitiveType);
            }
        }
        public override bool supportsUV { get { return true; } }
        public override bool implementsMotionVector { get { return true; } }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var def in base.additionalDefines)
                    yield return def;

                if (useGeometryShader)
                    yield return "USE_GEOMETRY_SHADER";

                yield return VFXPlanarPrimitiveHelper.GetShaderDefine(primitiveType);
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Color, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.Alpha, VFXAttributeMode.Read);
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
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);

                if (usesFlipbook)
                    yield return new VFXAttributeInfo(VFXAttribute.TexIndex, VFXAttributeMode.Read);
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = base.inputProperties;
                if (primitiveType == VFXPrimitiveType.Octagon)
                    properties = properties.Concat(PropertiesFromType(typeof(VFXPlanarPrimitiveHelper.OctagonInputProperties)));
                return properties;
            }
        }

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;
            if (primitiveType == VFXPrimitiveType.Octagon)
                yield return slotExpressions.First(o => o.name == "cropFactor");
        }
    }
}
