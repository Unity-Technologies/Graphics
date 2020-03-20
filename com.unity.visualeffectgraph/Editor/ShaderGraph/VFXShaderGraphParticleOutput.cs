using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;

using UnityObject = UnityEngine.Object;


namespace UnityEditor.VFX
{
    class VFXShaderGraphParticleOutput : VFXAbstractParticleOutput
    {
        [SerializeField, VFXSetting]
        public ShaderGraphVfxAsset shaderGraph;

        public override void OnEnable()
        {
            base.OnEnable();
        }

        void RefreshShaderGraphObject()
        {
            if (shaderGraph == null && !object.ReferenceEquals(shaderGraph, null))
            {
                string assetPath = AssetDatabase.GetAssetPath(shaderGraph.GetInstanceID());

                var newShaderGraph = AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(assetPath);
                if (newShaderGraph != null)
                {
                    shaderGraph = newShaderGraph;
                }
            }
        }

        public override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);
            if (!object.ReferenceEquals(shaderGraph, null))
                dependencies.Add(shaderGraph.GetInstanceID());
        }

        protected VFXShaderGraphParticleOutput(bool strip = false) : base(strip) {}
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
                {
                    var type = GetSGPropertyType(property);
                    PropertyInfo info = property.GetType().GetProperty("value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    return VFXConverter.ConvertTo(info?.GetValue(property), type);
                }
            }
        }

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                foreach (var setting in base.filteredOutSettings)
                    yield return setting;
                if (shaderGraph != null)
                {
                    yield return "colorMapping";
                    yield return "useAlphaClipping";
                }
                if (!VFXViewPreference.displayExperimentalOperator)
                    yield return "shaderGraph";
            }
        }

        public override bool supportsUV => base.supportsUV && shaderGraph == null;
        public override bool exposeAlphaThreshold
        {
            get
            {
                RefreshShaderGraphObject();
                if (shaderGraph == null)
                {
                    if (base.exposeAlphaThreshold)
                        return true;
                }
                else
                {
                    if (!shaderGraph.HasOutput(ShaderGraphVfxAsset.AlphaThresholdSlotId)) //alpha threshold isn't controlled by shadergraph
                        return true;
                }
                return false;
            }
        }
        public override bool supportSoftParticles => base.supportSoftParticles && shaderGraph == null;
        public override bool hasAlphaClipping
        {
            get
            {
                RefreshShaderGraphObject();
                bool noShaderGraphAlphaThreshold = shaderGraph == null && useAlphaClipping;
                bool ShaderGraphAlphaThreshold = shaderGraph != null && shaderGraph.HasOutput(ShaderGraphVfxAsset.AlphaThresholdSlotId);
                return noShaderGraphAlphaThreshold || ShaderGraphAlphaThreshold;
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                IEnumerable<VFXPropertyWithValue> properties = base.inputProperties;
                RefreshShaderGraphObject();
                if (shaderGraph != null)
                {
                    var shaderGraphProperties = new List<VFXPropertyWithValue>();
                    foreach (var property in shaderGraph.properties
                             .Where(t => !t.hidden)
                             .Select(t => new { property = t, type = GetSGPropertyType(t) })
                             .Where(t => t.type != null))
                    {
                        if (property.property.propertyType == PropertyType.Vector1)
                        {
                            var prop = property.property as Vector1ShaderProperty;

                            if (prop.floatType == FloatType.Slider)
                                shaderGraphProperties.Add(new VFXPropertyWithValue(new VFXProperty(property.type, property.property.referenceName, new VFXPropertyAttribute(VFXPropertyAttribute.Type.kRange, prop.rangeValues.x, prop.rangeValues.y)), GetSGPropertyValue(property.property)));
                            else if (prop.floatType == FloatType.Integer)
                                shaderGraphProperties.Add(new VFXPropertyWithValue(new VFXProperty(typeof(int), property.property.referenceName), VFXConverter.ConvertTo(GetSGPropertyValue(property.property), typeof(int))));
                            else
                                shaderGraphProperties.Add(new VFXPropertyWithValue(new VFXProperty(property.type, property.property.referenceName), GetSGPropertyValue(property.property)));
                        }
                        else
                            shaderGraphProperties.Add(new VFXPropertyWithValue(new VFXProperty(property.type, property.property.referenceName), GetSGPropertyValue(property.property)));
                    }

                    properties = properties.Concat(shaderGraphProperties);
                }
                return properties;
            }
        }

        protected class PassInfo
        {
            public int[] vertexPorts;
            public int[] pixelPorts;
        }

        protected class RPInfo
        {
            public Dictionary<string, PassInfo> passInfos;
            HashSet<int> m_AllPorts;

            public IEnumerable<int> allPorts
            {
                get
                {
                    if (m_AllPorts == null)
                    {
                        m_AllPorts = new HashSet<int>();
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

        protected static readonly RPInfo hdrpInfo = new RPInfo
        {
            passInfos = new Dictionary<string, PassInfo>()
            {
                { "Forward", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.ColorSlotId, ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } },
                { "DepthOnly", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } }
            }
        };
        protected static readonly RPInfo hdrpLitInfo = new RPInfo
        {
            passInfos = new Dictionary<string, PassInfo>()
            {
                { "GBuffer", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.BaseColorSlotId, ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.MetallicSlotId, ShaderGraphVfxAsset.SmoothnessSlotId, ShaderGraphVfxAsset.EmissiveSlotId, ShaderGraphVfxAsset.NormalSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } },
                { "Forward", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.BaseColorSlotId, ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.MetallicSlotId, ShaderGraphVfxAsset.SmoothnessSlotId, ShaderGraphVfxAsset.EmissiveSlotId, ShaderGraphVfxAsset.NormalSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId } } },
                { "DepthOnly", new PassInfo()  { vertexPorts = new int[] {}, pixelPorts = new int[] { ShaderGraphVfxAsset.AlphaSlotId, ShaderGraphVfxAsset.AlphaThresholdSlotId, ShaderGraphVfxAsset.NormalSlotId } } }
            }
        };

        protected override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            foreach (var exp in base.CollectGPUExpressions(slotExpressions))
                yield return exp;

            RefreshShaderGraphObject();
            if (shaderGraph != null)
            {
                foreach (var sgProperty in shaderGraph.properties)
                {
                    yield return slotExpressions.First(o => o.name == sgProperty.referenceName);
                }
            }
        }

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var def in base.additionalDefines)
                    yield return def;


                RefreshShaderGraphObject();

                if (shaderGraph != null)
                {
                    yield return "VFX_SHADERGRAPH";
                    RPInfo info = currentRP;

                    foreach (var port in info.allPorts)
                    {
                        var portInfo = shaderGraph.GetOutput(port);
                        if (!string.IsNullOrEmpty(portInfo.referenceName))
                            yield return $"HAS_SHADERGRAPH_PARAM_{portInfo.referenceName.ToUpper()}";
                    }

                    bool needsPosWS = false;

                    // Per pass define
                    foreach (var kvPass in graphCodes)
                    {
                        GraphCode graphCode = kvPass.Value;

                        var pixelPorts = currentRP.passInfos[kvPass.Key].pixelPorts;

                        bool readsNormal = (graphCode.requirements.requiresNormal & ~NeededCoordinateSpace.Tangent) != 0;
                        bool readsTangent = (graphCode.requirements.requiresTangent & ~NeededCoordinateSpace.Tangent) != 0 ||
                            (graphCode.requirements.requiresBitangent & ~NeededCoordinateSpace.Tangent) != 0 ||
                            (graphCode.requirements.requiresViewDir & NeededCoordinateSpace.Tangent) != 0;

                        bool hasNormalPort = pixelPorts.Any(t => t == ShaderGraphVfxAsset.NormalSlotId) && shaderGraph.HasOutput(ShaderGraphVfxAsset.NormalSlotId);

                        if (readsNormal || readsTangent || hasNormalPort) // needs normal
                            yield return $"SHADERGRAPH_NEEDS_NORMAL_{kvPass.Key.ToUpper()}";

                        if (readsTangent || hasNormalPort) // needs tangent
                            yield return $"SHADERGRAPH_NEEDS_TANGENT_{kvPass.Key.ToUpper()}";

                        needsPosWS |= graphCode.requirements.requiresPosition != NeededCoordinateSpace.None ||
                            graphCode.requirements.requiresScreenPosition ||
                            graphCode.requirements.requiresViewDir != NeededCoordinateSpace.None;
                    }

                    // TODO Put that per pass ?
                    if (needsPosWS)
                        yield return "VFX_NEEDS_POSWS_INTERPOLATOR";
                }
            }
        }

        protected virtual RPInfo currentRP
        {
            get { return hdrpInfo; }
        }


        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var mapper = base.GetExpressionMapper(target);

            switch (target)
            {
                case VFXDeviceTarget.CPU:
                    break;
                case VFXDeviceTarget.GPU:

                    RefreshShaderGraphObject();
                    if (shaderGraph != null)
                    {
                        foreach (var tex in shaderGraph.textureInfos.Where(t => t.texture != null).OrderBy(t => t.name))
                        {
                            switch (tex.texture.dimension)
                            {
                                case TextureDimension.Tex2D:
                                    mapper.AddExpression(new VFXTexture2DValue(tex.texture.GetInstanceID(), VFXValue.Mode.Variable), tex.name, -1);
                                    break;
                                case TextureDimension.Tex3D:
                                    mapper.AddExpression(new VFXTexture3DValue(tex.texture.GetInstanceID(), VFXValue.Mode.Variable), tex.name, -1);
                                    break;
                                case TextureDimension.Cube:
                                    mapper.AddExpression(new VFXTextureCubeValue(tex.texture.GetInstanceID(), VFXValue.Mode.Variable), tex.name, -1);
                                    break;
                                case TextureDimension.Tex2DArray:
                                    mapper.AddExpression(new VFXTexture2DArrayValue(tex.texture.GetInstanceID(), VFXValue.Mode.Variable), tex.name, -1);
                                    break;
                                case TextureDimension.CubeArray:
                                    mapper.AddExpression(new VFXTextureCubeArrayValue(tex.texture.GetInstanceID(), VFXValue.Mode.Variable), tex.name, -1);
                                    break;
                            }
                        }
                    }
                    break;
            }

            return mapper;
        }

        static bool IsTexture(PropertyType type)
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

        public override IEnumerable<string> fragmentParameters
        {
            get
            {
                RefreshShaderGraphObject();
                if (shaderGraph != null)
                    foreach (var param in shaderGraph.properties)
                        if (!IsTexture(param.propertyType)) // Remove exposed textures from list of interpolants
                            yield return param.referenceName;
            }
        }

        public virtual bool isLitShader { get => false; }

        Dictionary<string, GraphCode> graphCodes;

        public override bool SetupCompilation()
        {
            if (!base.SetupCompilation()) return false;
            RefreshShaderGraphObject();
            if (shaderGraph != null)
            {
                if (!isLitShader && shaderGraph.lit)
                {
                    Debug.LogError("You must use an unlit vfx master node with an unlit output");
                    return false;
                }
                if (isLitShader && !shaderGraph.lit)
                {
                    Debug.LogError("You must use a lit vfx master node with a lit output");
                    return false;
                }

                graphCodes = currentRP.passInfos.ToDictionary(t => t.Key, t => shaderGraph.GetCode(t.Value.pixelPorts.Select(u => shaderGraph.GetOutput(u)).Where(u => !string.IsNullOrEmpty(u.referenceName)).ToArray()));
            }

            return true;
        }

        public override void EndCompilation()
        {
            if (graphCodes != null)
                graphCodes.Clear();
        }

        public override IEnumerable<KeyValuePair<string, VFXShaderWriter>> additionalReplacements
        {
            get
            {
                foreach (var rep in base.additionalReplacements)
                    yield return rep;

                RefreshShaderGraphObject();

                if (shaderGraph != null)
                {
                    RPInfo info = currentRP;

                    foreach (var port in info.allPorts)
                    {
                        var portInfo = shaderGraph.GetOutput(port);
                        if (!string.IsNullOrEmpty(portInfo.referenceName))
                            yield return new KeyValuePair<string, VFXShaderWriter>($"${{SHADERGRAPH_PARAM_{portInfo.referenceName.ToUpper()}}}", new VFXShaderWriter($"{portInfo.referenceName}_{portInfo.id}"));
                    }

                    foreach (var kvPass in graphCodes)
                    {
                        GraphCode graphCode = kvPass.Value;

                        var preProcess = new VFXShaderWriter();
                        if (graphCode.requirements.requiresCameraOpaqueTexture)
                            preProcess.WriteLine("#define REQUIRE_OPAQUE_TEXTURE");
                        if (graphCode.requirements.requiresDepthTexture)
                            preProcess.WriteLine("#define REQUIRE_DEPTH_TEXTURE");
                        preProcess.WriteLine("${VFXShaderGraphFunctionsInclude}\n");
                        yield return new KeyValuePair<string, VFXShaderWriter>("${SHADERGRAPH_PIXEL_CODE_" + kvPass.Key.ToUpper() + "}", new VFXShaderWriter(preProcess.ToString() + graphCode.code));

                        var callSG = new VFXShaderWriter("//Call Shader Graph\n");
                        callSG.builder.AppendLine($"{shaderGraph.inputStructName} INSG = ({shaderGraph.inputStructName})0;");

                        if (graphCode.requirements.requiresNormal != NeededCoordinateSpace.None)
                        {
                            callSG.builder.AppendLine("float3 WorldSpaceNormal = normalize(normalWS.xyz);");
                            if ((graphCode.requirements.requiresNormal & NeededCoordinateSpace.World) != 0)
                                callSG.builder.AppendLine("INSG.WorldSpaceNormal = WorldSpaceNormal;");
                            if ((graphCode.requirements.requiresNormal & NeededCoordinateSpace.Object) != 0)
                                callSG.builder.AppendLine("INSG.ObjectSpaceNormal = mul(WorldSpaceNormal, (float3x3)UNITY_MATRIX_M);");
                            if ((graphCode.requirements.requiresNormal & NeededCoordinateSpace.View) != 0)
                                callSG.builder.AppendLine("INSG.ViewSpaceNormal = mul(WorldSpaceNormal, (float3x3)UNITY_MATRIX_I_V);");
                            if ((graphCode.requirements.requiresNormal & NeededCoordinateSpace.Tangent) != 0)
                                callSG.builder.AppendLine("INSG.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);");
                        }
                        if (graphCode.requirements.requiresTangent != NeededCoordinateSpace.None)
                        {
                            callSG.builder.AppendLine("float3 WorldSpaceTangent = normalize(tangentWS.xyz);");
                            if ((graphCode.requirements.requiresTangent & NeededCoordinateSpace.World) != 0)
                                callSG.builder.AppendLine("INSG.WorldSpaceTangent =  WorldSpaceTangent;");
                            if ((graphCode.requirements.requiresTangent & NeededCoordinateSpace.Object) != 0)
                                callSG.builder.AppendLine("INSG.ObjectSpaceTangent =  TransformWorldToObjectDir(WorldSpaceTangent);");
                            if ((graphCode.requirements.requiresTangent & NeededCoordinateSpace.View) != 0)
                                callSG.builder.AppendLine("INSG.ViewSpaceTangent = TransformWorldToViewDir(WorldSpaceTangent);");
                            if ((graphCode.requirements.requiresTangent & NeededCoordinateSpace.Tangent) != 0)
                                callSG.builder.AppendLine("INSG.TangentSpaceTangent = float3(1.0f, 0.0f, 0.0f);");
                        }

                        if (graphCode.requirements.requiresBitangent != NeededCoordinateSpace.None)
                        {
                            callSG.builder.AppendLine("float3 WorldSpaceBiTangent =  normalize(bitangentWS.xyz);");
                            if ((graphCode.requirements.requiresBitangent & NeededCoordinateSpace.World) != 0)
                                callSG.builder.AppendLine("INSG.WorldSpaceBiTangent =  WorldSpaceBiTangent;");
                            if ((graphCode.requirements.requiresBitangent & NeededCoordinateSpace.Object) != 0)
                                callSG.builder.AppendLine("INSG.ObjectSpaceBiTangent =  TransformWorldToObjectDir(WorldSpaceBiTangent);");
                            if ((graphCode.requirements.requiresBitangent & NeededCoordinateSpace.View) != 0)
                                callSG.builder.AppendLine("INSG.ViewSpaceBiTangent = TransformWorldToViewDir(WorldSpaceBiTangent);");
                            if ((graphCode.requirements.requiresBitangent & NeededCoordinateSpace.Tangent) != 0)
                                callSG.builder.AppendLine("INSG.TangentSpaceBiTangent = float3(0.0f, 1.0f, 0.0f);");
                        }

                        if (graphCode.requirements.requiresPosition != NeededCoordinateSpace.None || graphCode.requirements.requiresScreenPosition || graphCode.requirements.requiresViewDir != NeededCoordinateSpace.None)
                        {
                            callSG.builder.AppendLine("float3 posRelativeWS = VFXGetPositionRWS(i.VFX_VARYING_POSWS);");
                            callSG.builder.AppendLine("float3 posAbsoluteWS = VFXGetPositionAWS(i.VFX_VARYING_POSWS);");

                            if ((graphCode.requirements.requiresPosition & NeededCoordinateSpace.World) != 0)
                                callSG.builder.AppendLine("INSG.WorldSpacePosition = posRelativeWS;");
                            if ((graphCode.requirements.requiresPosition & NeededCoordinateSpace.Object) != 0)
                                callSG.builder.AppendLine("INSG.ObjectSpacePosition = TransformWorldToObject(posRelativeWS);");
                            if ((graphCode.requirements.requiresPosition & NeededCoordinateSpace.View) != 0)
                                callSG.builder.AppendLine("INSG.ViewSpacePosition = TransformPositionVFXToView(i.VFX_VARYING_POSWS);");
                            if ((graphCode.requirements.requiresPosition & NeededCoordinateSpace.Tangent) != 0)
                                callSG.builder.AppendLine("INSG.TangentSpacePosition = float3(0.0f, 0.0f, 0.0f);");
                            if ((graphCode.requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) != 0)
                                callSG.builder.AppendLine("INSG.AbsoluteWorldSpacePosition = posAbsoluteWS;");

                            if (graphCode.requirements.requiresScreenPosition)
                                callSG.builder.AppendLine("INSG.ScreenPosition = ComputeScreenPos(VFXTransformPositionWorldToClip(i.VFX_VARYING_POSWS), _ProjectionParams.x);");

                            if (graphCode.requirements.requiresViewDir != NeededCoordinateSpace.None)
                            {
                                callSG.builder.AppendLine("float3 V = GetWorldSpaceNormalizeViewDir(VFXGetPositionRWS(i.VFX_VARYING_POSWS));");
                                if ((graphCode.requirements.requiresViewDir & NeededCoordinateSpace.World) != 0)
                                    callSG.builder.AppendLine("INSG.WorldSpaceViewDirection = V;");
                                if ((graphCode.requirements.requiresViewDir & NeededCoordinateSpace.Object) != 0)
                                    callSG.builder.AppendLine("INSG.ObjectSpaceViewDirection =  TransformWorldToObjectDir(V);");
                                if ((graphCode.requirements.requiresViewDir & NeededCoordinateSpace.View) != 0)
                                    callSG.builder.AppendLine("INSG.ViewSpaceViewDirection = TransformWorldToViewDir(V);");
                                if ((graphCode.requirements.requiresViewDir & NeededCoordinateSpace.Tangent) != 0)
                                    callSG.builder.AppendLine("INSG.TangentSpaceViewDirection = mul(tbn, V);");
                            }
                        }

                        if (graphCode.requirements.requiresMeshUVs.Contains(UVChannel.UV0))
                        {
                            callSG.builder.AppendLine("INSG.uv0.xy = i.uv;");
                        }

                        if (graphCode.requirements.requiresTime)
                        {
                            callSG.builder.AppendLine("INSG.TimeParameters = _TimeParameters.xyz;");
                        }

                        if (taskType == VFXTaskType.ParticleMeshOutput)
                        {
                            for (UVChannel uv = UVChannel.UV1; uv <= UVChannel.UV3; ++uv)
                            {
                                if (graphCode.requirements.requiresMeshUVs.Contains(uv))
                                {
                                    int uvi = (int)uv;
                                    yield return new KeyValuePair<string, VFXShaderWriter>($"VFX_SHADERGRAPH_HAS_UV{uvi}", new VFXShaderWriter("1")); // TODO put that in additionalDefines
                                    callSG.builder.AppendLine($"INSG.uv{uvi} = i.uv{uvi};");
                                }
                            }

                            if (graphCode.requirements.requiresVertexColor)
                            {
                                yield return new KeyValuePair<string, VFXShaderWriter>($"VFX_SHADERGRAPH_HAS_COLOR", new VFXShaderWriter("1")); // TODO put that in additionalDefines
                                callSG.builder.AppendLine($"INSG.VertexColor = i.vertexColor;");
                            }
                        }

                        callSG.builder.Append($"\n{shaderGraph.outputStructName} OUTSG = {shaderGraph.evaluationFunctionName}(INSG");

                        if (graphCode.properties.Any())
                            callSG.builder.Append("," + graphCode.properties.Select(t => IsTexture(t.propertyType) ? (t.propertyType == PropertyType.Texture2D ? $"{t.referenceName}, sampler{t.referenceName}, {t.referenceName}_TexelSize" : $"{t.referenceName}, sampler{t.referenceName}") : t.referenceName).Aggregate((s, t) => s + ", " + t));

                        callSG.builder.AppendLine(");");

                        var pixelPorts = currentRP.passInfos[kvPass.Key].pixelPorts;
                        if (pixelPorts.Any(t => t == ShaderGraphVfxAsset.AlphaThresholdSlotId) && shaderGraph.HasOutput(ShaderGraphVfxAsset.AlphaThresholdSlotId))
                        {
                            callSG.builder.AppendLine(
@"#if (USE_ALPHA_TEST || WRITE_MOTION_VECTOR_IN_FORWARD) && defined(VFX_VARYING_ALPHATHRESHOLD)
i.VFX_VARYING_ALPHATHRESHOLD = OUTSG.AlphaThreshold_7;
#endif");
                        }

                        yield return new KeyValuePair<string, VFXShaderWriter>("${SHADERGRAPH_PIXEL_CALL_" + kvPass.Key.ToUpper() + "}", callSG);
                    }
                }
            }
        }
    }
}
