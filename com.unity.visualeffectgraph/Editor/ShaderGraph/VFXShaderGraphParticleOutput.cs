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
        public ShaderGraphVfxAsset shaderGraph;

        protected VFXShaderGraphParticleOutput(bool strip = false) : base(strip) { }
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
                IEnumerable<VFXPropertyWithValue> properties = base.inputProperties;
                if (shaderGraph != null)
                {
                    properties = properties.Concat(shaderGraph.properties
                        .Where(t => !t.hidden)
                        .Select(t => new { property = t, type = GetSGPropertyType(t) })
                        .Where(t => t.type != null)
                        .Select(t => new VFXPropertyWithValue(new VFXProperty(t.type, t.property.displayName), GetSGPropertyValue(t.property))));
                }
                return properties;
            }
        }

        protected class PassInfo
        {
            public string[] vertexPorts;
            public string[] pixelPorts;
        }

        protected class RPInfo
        {
            public Dictionary<string, PassInfo> passInfos;
            HashSet<string> m_AllPorts;

            public IEnumerable<string> allPorts
            {
                get
                {
                    if (m_AllPorts == null)
                    {
                        m_AllPorts = new HashSet<string>();
                        foreach (var pass in passInfos.Values)
                        {
                            foreach (var port in pass.vertexPorts)
                                m_AllPorts.Add(port);
                            foreach (var port in pass.pixelPorts)
                                m_AllPorts.Add(port);
                        }
                    }

                    return m_AllPorts;
                }
            }
        }

        protected RPInfo hdrpInfo = new RPInfo
        {
            passInfos = new Dictionary<string, PassInfo>() {
            { "Forward",new PassInfo()  { vertexPorts = new string[]{"Position"},pixelPorts = new string[]{ "Color", "Alpha","AlphaThreshold"} } },
            { "DepthOnly",new PassInfo()  { vertexPorts = new string[]{"Position"},pixelPorts = new string[]{ "Alpha", "AlphaThreshold" } } }
        }
        };
        protected RPInfo hdrpLitInfo = new RPInfo
        {
            passInfos = new Dictionary<string, PassInfo>() {
            { "GBuffer",new PassInfo()  { vertexPorts = new string[]{"Position"},pixelPorts = new string[]{ "BaseColor", "Alpha", "Metallic", "Smoothness","Emissive", "AlphaThreshold" } } },
            { "Forward",new PassInfo()  { vertexPorts = new string[]{"Position"},pixelPorts = new string[]{ "BaseColor", "Alpha", "Metallic", "Smoothness", "Emissive", "AlphaThreshold" } } },
            { "DepthOnly",new PassInfo()  { vertexPorts = new string[]{"Position"},pixelPorts = new string[]{ "Alpha" } } }
        }
        };

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var def in base.additionalDefines)
                    yield return def;

                if( shaderGraph != null)
                {
                    yield return "VFX_SHADERGRAPH";
                    RPInfo info = currentRP;
                    foreach (var port in info.allPorts)
                    {
                        var portInfo = shaderGraph.GetOutput(port);
                        if (!string.IsNullOrEmpty(portInfo.referenceName))
                            yield return $"HAS_SHADERGRAPH_PARAM_{port.ToUpper()}";
                    }
                }
            }
        }

        protected virtual RPInfo currentRP
        {
            get { return hdrpInfo; }
        }


        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                foreach (var rep in base.additionalReplacements)
                    yield return rep;

                if (shaderGraph != null)
                {
                    RPInfo info = currentRP;

                    foreach( var port in info.allPorts)
                    {
                        var portInfo = shaderGraph.GetOutput(port);
                        if( ! string.IsNullOrEmpty(portInfo.referenceName))
                            yield return new KeyValuePair<string, VFXShaderWriter>($"${{SHADERGRAPH_PARAM_{port.ToUpper()}}}", new VFXShaderWriter($"{portInfo.referenceName}_{portInfo.id}"));
                    }

                    foreach (var kvPass in info.passInfos)
                    {
                        GraphCode graphCode = shaderGraph.GetCode(kvPass.Value.pixelPorts.Select(t => shaderGraph.GetOutput(t)).Where(t=>!string.IsNullOrEmpty(t.referenceName)).ToArray());

                        yield return new KeyValuePair<string, VFXShaderWriter>("${SHADERGRAPH_PIXEL_CODE_" + kvPass.Key.ToUpper()+"}", new VFXShaderWriter(graphCode.code));

                        var callSG = new VFXShaderWriter("//Call Shader Graph\n");
                        callSG.builder.AppendLine($"{shaderGraph.inputStructName} INSG;");
                        foreach( var property in graphCode.properties)
                        {
                            callSG.builder.AppendLine($"INSG.{property.referenceName} = {property.displayName};");
                        }
                        callSG.builder.AppendLine($"\n{shaderGraph.outputStructName} OUTSG = {shaderGraph.evaluationFunctionName}(INSG);");

                        yield return new KeyValuePair<string, VFXShaderWriter>("${SHADERGRAPH_PIXEL_CALL_" + kvPass.Key.ToUpper() + "}", callSG);
                    }
                }
            }
        }

    }
}
