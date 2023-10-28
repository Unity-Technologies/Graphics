using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    interface IVFXShaderGraphOutput
    {
        ShaderGraphVfxAsset GetShaderGraph();
    }

    static class VFXShaderGraphHelpers
    {
        public static string GetMissingShaderGraphErrorMessage(ShaderGraphVfxAsset shader)
        {
            var instanceID = shader.GetInstanceID();
            var missingShaderPath = AssetDatabase.GetAssetPath(instanceID);
            if (!string.IsNullOrEmpty(missingShaderPath))
            {
                return $" cannot be compiled because a Shader Graph asset located here '{missingShaderPath}' is missing.";
            }

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(shader, out var guid, out var localID);
            return $" cannot be compiled because a Shader Graph with GUID '{guid}' is missing.\nYou might find the missing file by searching on your disk this guid in .meta files.";
        }

        private static Type GetPropertyType(AbstractShaderProperty property)
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
                case PropertyType.Float:
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

        private static object GetPropertyValue(AbstractShaderProperty property)
        {
            switch (property.propertyType)
            {
                case PropertyType.Texture2D:
                    return ((Texture2DShaderProperty) property).value.texture;
                case PropertyType.Texture3D:
                    return ((Texture3DShaderProperty) property).value.texture;
                case PropertyType.Cubemap:
                    return ((CubemapShaderProperty) property).value.cubemap;
                case PropertyType.Texture2DArray:
                    return ((Texture2DArrayShaderProperty) property).value.textureArray;
                default:
                {
                    var type = GetPropertyType(property);
                    PropertyInfo info = property.GetType().GetProperty("value",
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    return VFXConverter.ConvertTo(info?.GetValue(property), type);
                }
            }
        }

        public static IEnumerable<VFXPropertyWithValue> GetProperties(ShaderGraphVfxAsset shaderGraph)
        {
            foreach (var property in shaderGraph.properties)
            {
                if (property.hidden)
                    continue;

                var type = GetPropertyType(property);
                if (type == null)
                    continue;

                if (property.propertyType == PropertyType.Float)
                {
                    if (property is Vector1ShaderProperty prop)
                    {
                        if (prop.floatType == FloatType.Slider)
                            yield return new VFXPropertyWithValue(
                                new VFXProperty(type, property.referenceName,
                                    new RangeAttribute(prop.rangeValues.x, prop.rangeValues.y)),
                                GetPropertyValue(property));
                        else if (prop.floatType == FloatType.Integer)
                            yield return new VFXPropertyWithValue(new VFXProperty(typeof(int), property.referenceName),
                                VFXConverter.ConvertTo(GetPropertyValue(property), typeof(int)));
                        else
                            yield return new VFXPropertyWithValue(new VFXProperty(type, property.referenceName),
                                GetPropertyValue(property));
                    }
                }
                else
                    yield return new VFXPropertyWithValue(new VFXProperty(type, property.referenceName),
                        GetPropertyValue(property));
            }
        }

        public static IEnumerable<VFXNamedExpression> GetTextureConstant(ShaderGraphVfxAsset shaderGraph)
        {
            foreach (var tex in shaderGraph.textureInfos)
            {
                switch (tex.dimension)
                {
                    case TextureDimension.Tex2D:
                        yield return new VFXNamedExpression(
                            new VFXTexture2DValue(tex.instanceID, VFXValue.Mode.Variable), tex.name);
                        break;
                    case TextureDimension.Tex3D:
                        yield return new VFXNamedExpression(
                            new VFXTexture3DValue(tex.instanceID, VFXValue.Mode.Variable), tex.name);
                        break;
                    case TextureDimension.Cube:
                        yield return new VFXNamedExpression(
                            new VFXTextureCubeValue(tex.instanceID, VFXValue.Mode.Variable), tex.name);
                        break;
                    case TextureDimension.Tex2DArray:
                        yield return new VFXNamedExpression(
                            new VFXTexture2DArrayValue(tex.instanceID, VFXValue.Mode.Variable), tex.name);
                        break;
                    case TextureDimension.CubeArray:
                        yield return new VFXNamedExpression(
                            new VFXTextureCubeArrayValue(tex.instanceID, VFXValue.Mode.Variable), tex.name);
                        break;
                }
            }
        }

        public static bool IsTexture(PropertyType type)
        {
            switch (type)
            {
                case PropertyType.Texture2D:
                case PropertyType.Texture2DArray:
                case PropertyType.Texture3D:
                case PropertyType.Cubemap:
                    return true;
                default:
                    return false;
            }
        }

        public static ShaderGraphVfxAsset GetShaderGraph(VFXContext context)
        {
            if (context is IVFXShaderGraphOutput shaderGraphOutput)
                return shaderGraphOutput.GetShaderGraph();
            return null;
        }

        public static void GetShaderGraphParameter(VFXContext context, out List<string> fragmentParameters, out List<string> vertexParameter)
        {
            fragmentParameters = null;
            vertexParameter = null;
            var shaderGraph = GetShaderGraph(context);
            if (shaderGraph != null)
            {
                fragmentParameters = new List<string>();
                vertexParameter = new List<string>();

                foreach (var param in shaderGraph.fragmentProperties)
                    if (!IsTexture(param.propertyType)) // Remove exposed textures from list of interpolants
                        fragmentParameters.Add(param.referenceName);

                foreach (var param in shaderGraph.vertexProperties)
                    if (!IsTexture(param.propertyType)) // Remove exposed textures from list of interpolants
                        vertexParameter.Add(param.referenceName);
            }
        }
    }
}
