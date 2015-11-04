using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    public abstract class PropertyChunk
    {
        protected string m_PropertyName;
        protected string m_PropertyDescription;
        protected bool m_Hidden;
        protected PropertyChunk(string propertyName, string propertyDescription, bool hidden)
        {
            m_PropertyName = propertyName;
            m_PropertyDescription = propertyDescription;
            m_Hidden = hidden;
        }

        public abstract string GetPropertyString();
        public string propertyName { get { return m_PropertyName; } }
        public string propertyDescription { get { return m_PropertyDescription; } }
    }

    public class TexturePropertyChunk : PropertyChunk
    {
        private readonly Texture m_DefaultTexture;
        private readonly TextureType m_DefaultTextureType;

        public TexturePropertyChunk(string propertyName, string propertyDescription, Texture defaultTexture, TextureType defaultTextureType, bool hidden)
            : base(propertyName, propertyDescription, hidden)
        {
            m_DefaultTexture = defaultTexture;
            m_DefaultTextureType = defaultTextureType;
        }

        public override string GetPropertyString()
        {
            var result = new StringBuilder();
            if (m_Hidden)
                result.Append("[HideInInspector] ");

            result.Append(m_PropertyName);
            result.Append("(\"");
            result.Append(m_PropertyDescription);
            result.Append("\", 2D) = \"");
            result.Append(Enum.GetName(typeof(TextureType), m_DefaultTextureType).ToLower());
            result.Append("\" {}");
            return result.ToString();
        }

        public Texture defaultTexture { get { return m_DefaultTexture; } }
    }

    public class ColorPropertyChunk : PropertyChunk
    {
        private Color m_DefaultColor;

        public ColorPropertyChunk(string propertyName, string propertyDescription, Color defaultColor, bool hidden)
            : base(propertyName, propertyDescription, hidden)
        {
            m_DefaultColor = defaultColor;
        }

        public override string GetPropertyString()
        {
            var result = new StringBuilder();
            result.Append(m_PropertyName);
            result.Append("(\"");
            result.Append(m_PropertyDescription);
            result.Append("\", Color) = (");
            result.Append(m_DefaultColor.r);
            result.Append(",");
            result.Append(m_DefaultColor.g);
            result.Append(",");
            result.Append(m_DefaultColor.b);
            result.Append(",");
            result.Append(m_DefaultColor.a);
            result.Append(")");
            return result.ToString();
        }
    }

    public class FloatPropertyChunk : PropertyChunk
    {
        private readonly float m_DefaultValue;
        public FloatPropertyChunk(string propertyName, string propertyDescription, float defaultValue, bool hidden)
            : base(propertyName, propertyDescription, hidden)
        {
            m_DefaultValue = defaultValue;
        }

        public override string GetPropertyString()
        {
            var result = new StringBuilder();
            result.Append(m_PropertyName);
            result.Append("(\"");
            result.Append(m_PropertyDescription);
            result.Append("\", Float) = ");
            result.Append(m_DefaultValue);
            return result.ToString();
        }
    }

    public class VectorPropertyChunk : PropertyChunk
    {
        private readonly Vector4 m_DefaultVector;
        public VectorPropertyChunk(string propertyName, string propertyDescription, Vector4 defaultVector, bool hidden)
            : base(propertyName, propertyDescription, hidden)
        {
            m_DefaultVector = defaultVector;
        }

        public override string GetPropertyString()
        {
            var result = new StringBuilder();
            result.Append(m_PropertyName);
            result.Append("(\"");
            result.Append(m_PropertyDescription);
            result.Append("\", Vector) = (");
            result.Append(m_DefaultVector.x);
            result.Append(",");
            result.Append(m_DefaultVector.y);
            result.Append(",");
            result.Append(m_DefaultVector.z);
            result.Append(",");
            result.Append(m_DefaultVector.w);
            result.Append(")");
            return result.ToString();
        }
    }

    public class PropertyGenerator
    {
        private readonly List<PropertyChunk> m_Properties = new List<PropertyChunk>();

        public void AddShaderProperty(PropertyChunk chunk)
        {
            m_Properties.Add(chunk);
        }

        public string GetShaderString(int baseIndentLevel)
        {
            var sb = new StringBuilder();
            foreach (var prop in m_Properties)
            {
                for (var i = 0; i < baseIndentLevel; i++)
                {
                    sb.Append("\t");
                }
                sb.Append(prop.GetPropertyString());
                sb.Append("\n");
            }
            return sb.ToString();
        }

        public Dictionary<string, Texture> GetDefaultTexutres()
        {
            var result = new Dictionary<string, Texture>();

            foreach (var prop in m_Properties.OfType<TexturePropertyChunk>())
            {
                if (prop.propertyName != null)
                    result.Add(prop.propertyName, prop.defaultTexture);
            }
            return result;
        }
    }

    public class ShaderGenerator
    {
        private struct ShaderChunk
        {
            public ShaderChunk(int indentLevel, string shaderChunkString)
            {
                m_IndentLevel = indentLevel;
                m_ShaderChunkString = shaderChunkString;
            }

            private readonly int m_IndentLevel;
            private readonly string m_ShaderChunkString;

            public int chunkIndentLevel { get { return m_IndentLevel; } }
            public string chunkString { get { return m_ShaderChunkString; } }
        }

        private readonly List<ShaderChunk> m_ShaderChunks = new List<ShaderChunk>();
        private int m_IndentLevel;
        private string m_Pragma;

        public void AddPragmaChunk(string s)
        {
            m_Pragma += s;
        }

        public string GetPragmaString()
        {
            return m_Pragma;
        }

        public void AddShaderChunk(string s, bool unique)
        {
            if (unique && m_ShaderChunks.Any(x => x.chunkString == s))
                return;

            m_ShaderChunks.Add(new ShaderChunk(m_IndentLevel, s));
        }

        public void Indent() { m_IndentLevel++; }
        public void Deindent() { m_IndentLevel = Math.Max(0, m_IndentLevel - 1); }

        public string GetShaderString(int baseIndentLevel)
        {
            var sb = new StringBuilder();
            foreach (var shaderChunk in m_ShaderChunks)
            {
                var replaceString = "\n";
                for (var i = 0; i < shaderChunk.chunkIndentLevel + baseIndentLevel; i++)
                {
                    sb.Append("\t");
                    replaceString += "\t";
                }
                sb.AppendLine(shaderChunk.chunkString.Replace("\n", replaceString));
            }
            return sb.ToString();
        }

        private static string GetTemplatePath(string templateName)
        {
            var path = new List<string>
            {
                Application.dataPath,
                "UnityShaderEditor",
                "Editor",
                "Templates"
            };

            string result = path[0];
            for (int i = 1; i < path.Count; i++)
                result = Path.Combine(result, path[i]);

            result = Path.Combine(result, templateName);
            return result;
        }

        private static string AdaptNodeOutput(BaseMaterialNode node, GenerationMode mode, ConcreteSlotValueType previewChannelConcreteType)
        {
            var outputSlot = node.outputSlots.FirstOrDefault();
            if (outputSlot == null)
                return string.Empty;

            var outputConcreteType = node.GetConcreteOutputSlotValueType(outputSlot);

            var rawOutput = node.GetOutputVariableNameForSlot(node.outputSlots.FirstOrDefault(), mode);
            if (outputConcreteType == previewChannelConcreteType)
                return rawOutput;

            switch (previewChannelConcreteType)
            {
                case ConcreteSlotValueType.Vector1:
                    return string.Format("({0}).x", rawOutput);
                case ConcreteSlotValueType.Vector2:
                    switch (outputConcreteType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("half2(({0}).x, ({0}).x)", rawOutput);
                        case ConcreteSlotValueType.Vector3:
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("half2(({0}).x, ({0}).y)", rawOutput);
                        default:
                            return string.Empty;
                    }
                case ConcreteSlotValueType.Vector3:
                    switch (outputConcreteType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("half3(({0}).x, ({0}).x, ({0}).x)", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("half3(({0}).x, ({0}).y, 0.0f)", rawOutput);
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("half3(({0}).x, ({0}).y, ({0}).z)", rawOutput);
                        default:
                            return string.Empty;
                    }
                case ConcreteSlotValueType.Vector4:
                    switch (outputConcreteType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("half4(({0}).x, ({0}).x, ({0}).x, ({0}).x)", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("half4(({0}).x, ({0}).y, 0.0f, 0.0f)", rawOutput);
                        case ConcreteSlotValueType.Vector3:
                            return string.Format("half4(({0}).x, ({0}).y, ({0}).z, 0.0f)", rawOutput);
                        default:
                            return string.Empty;
                    }
                default:
                    return string.Empty;
            }
        }

        public static string GeneratePreviewShader(BaseMaterialNode node, out PreviewMode generatedShaderMode)
        {
            // figure out what kind of preview we want!
            var childNodes = node.CollectChildNodesByExecutionOrder();
            var generationMode = GenerationMode.Preview2D;
            generatedShaderMode = PreviewMode.Preview2D;
            if (childNodes.Any(x => x.previewMode == PreviewMode.Preview3D))
            {
                generationMode = GenerationMode.Preview3D;
                generatedShaderMode = PreviewMode.Preview3D;
            }

            string templateLocation = GetTemplatePath(generationMode == GenerationMode.Preview2D ? "2DPreview.template" : "3DPreview.template");
            if (!File.Exists(templateLocation))
                return null;

            string template = File.ReadAllText(templateLocation);

            var shaderBodyVisitor = new ShaderGenerator();
            var shaderInputVisitor = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var shaderPropertiesVisitor = new PropertyGenerator();
            var shaderPropertyUsagesVisitor = new ShaderGenerator();
            var vertexShaderBlock = new ShaderGenerator();

            var shaderName = "Hidden/PreviewShader/" + node.GetOutputVariableNameForSlot(node.outputSlots.First(), generationMode);
            var activeNodeList = node.CollectChildNodesByExecutionOrder();

            if (node.graph is IGenerateGraphProperties)
                (node.graph as IGenerateGraphProperties).GenerateSharedProperties(shaderPropertiesVisitor, shaderPropertyUsagesVisitor, generationMode);

            foreach (var activeNode in activeNodeList)
            {
                if (activeNode is IGeneratesFunction)
                    (activeNode as IGeneratesFunction).GenerateNodeFunction(shaderFunctionVisitor, generationMode);
                if (activeNode is IGeneratesVertexToFragmentBlock)
                    (activeNode as IGeneratesVertexToFragmentBlock).GenerateVertexToFragmentBlock(shaderInputVisitor, generationMode);
                if (activeNode is IGeneratesBodyCode)
                    (activeNode as IGeneratesBodyCode).GenerateNodeCode(shaderBodyVisitor, generationMode);
                if (activeNode is IGeneratesVertexShaderBlock)
                    (activeNode as IGeneratesVertexShaderBlock).GenerateVertexShaderBlock(vertexShaderBlock, generationMode);

                activeNode.GeneratePropertyBlock(shaderPropertiesVisitor, generationMode);
                activeNode.GeneratePropertyUsages(shaderPropertyUsagesVisitor, generationMode, ConcreteSlotValueType.Vector4);
            }

            if (shaderInputVisitor.numberOfChunks == 0)
            {
                shaderInputVisitor.AddShaderChunk("float4 color : COLOR;", true);
            }

            if (generationMode == GenerationMode.Preview2D)
                shaderBodyVisitor.AddShaderChunk("return " + AdaptNodeOutput(node, generationMode, ConcreteSlotValueType.Vector4) + ";", true);
            else
                shaderBodyVisitor.AddShaderChunk("o.Emission = " + AdaptNodeOutput(node, generationMode, ConcreteSlotValueType.Vector3) + ";", true);

            template = template.Replace("${ShaderName}", shaderName);
            template = template.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(2));
            template = template.Replace("${ShaderPropertyUsages}", shaderPropertyUsagesVisitor.GetShaderString(3));
            template = template.Replace("${ShaderInputs}", shaderInputVisitor.GetShaderString(4));
            template = template.Replace("${ShaderFunctions}", shaderFunctionVisitor.GetShaderString(3));
            template = template.Replace("${VertexShaderBody}", vertexShaderBlock.GetShaderString(4));
            template = template.Replace("${PixelShaderBody}", shaderBodyVisitor.GetShaderString(4));

            string vertexShaderBody = vertexShaderBlock.GetShaderString(4);
            if (vertexShaderBody.Length > 0)
            {
                template = template.Replace("${VertexShaderDecl}", "vertex:vert");
                template = template.Replace("${VertexShaderBody}", vertexShaderBody);
            }
            else
            {
                template = template.Replace("${VertexShaderDecl}", "");
                template = template.Replace("${VertexShaderBody}", vertexShaderBody);
            }

            return template;
        }

        public static string GenerateSurfaceShader(MaterialGraph graph, string shaderName, bool isPreview, out Dictionary<string, Texture> defaultTextures)
        {
            var templateLocation = GetTemplatePath("shader.template");

            if (!File.Exists(templateLocation))
            {
                defaultTextures = new Dictionary<string, Texture>();
                return string.Empty;
            }

            var templateText = File.ReadAllText(templateLocation);

            var shaderBodyVisitor = new ShaderGenerator();
            var shaderInputVisitor = new ShaderGenerator();
            var shaderLightFunctionVisitor = new ShaderGenerator();
            var shaderOutputSurfaceVisitor = new ShaderGenerator();
            var shaderFunctionVisitor = new ShaderGenerator();
            var shaderPropertiesVisitor = new PropertyGenerator();
            var shaderPropertyUsagesVisitor = new ShaderGenerator();
            var vertexShaderBlock = new ShaderGenerator();

            (graph.currentGraph as PixelGraph).GenerateSurfaceShader(
                shaderBodyVisitor,
                shaderInputVisitor,
                shaderLightFunctionVisitor,
                shaderOutputSurfaceVisitor,
                shaderFunctionVisitor,
                shaderPropertiesVisitor,
                shaderPropertyUsagesVisitor,
                vertexShaderBlock,
                isPreview);

            if (shaderInputVisitor.numberOfChunks == 0)
            {
                shaderInputVisitor.AddShaderChunk("float4 color : COLOR;", true);
            }

            var tagsVisitor = new ShaderGenerator();
            var blendingVisitor = new ShaderGenerator();
            var cullingVisitor = new ShaderGenerator();
            var zTestVisitor = new ShaderGenerator();
            var zWriteVisitor = new ShaderGenerator();

            var options = graph.materialOptions;
            options.GetTags(tagsVisitor);
            options.GetBlend(blendingVisitor);
            options.GetCull(cullingVisitor);
            options.GetDepthTest(zTestVisitor);
            options.GetDepthWrite(zWriteVisitor);

            var resultShader = templateText.Replace("${ShaderName}", shaderName);
            resultShader = resultShader.Replace("${ShaderPropertiesHeader}", shaderPropertiesVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ShaderPropertyUsages}", shaderPropertyUsagesVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${LightingFunctionName}", shaderLightFunctionVisitor.GetPragmaString());
            resultShader = resultShader.Replace("${LightingFunction}", shaderLightFunctionVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${SurfaceOutputStructureName}", shaderOutputSurfaceVisitor.GetPragmaString());
            resultShader = resultShader.Replace("${ShaderFunctions}", shaderFunctionVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ShaderInputs}", shaderInputVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${PixelShaderBody}", shaderBodyVisitor.GetShaderString(3));
            resultShader = resultShader.Replace("${Tags}", tagsVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Blending}", blendingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${Culling}", cullingVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZTest}", zTestVisitor.GetShaderString(2));
            resultShader = resultShader.Replace("${ZWrite}", zWriteVisitor.GetShaderString(2));

            string vertexShaderBody = vertexShaderBlock.GetShaderString(3);
            if (vertexShaderBody.Length > 0)
            {
                resultShader = resultShader.Replace("${VertexShaderDecl}", "vertex:vert");
                resultShader = resultShader.Replace("${VertexShaderBody}", vertexShaderBody);
            }
            else
            {
                resultShader = resultShader.Replace("${VertexShaderDecl}", "");
                resultShader = resultShader.Replace("${VertexShaderBody}", "");
            }

            MaterialWindow.DebugMaterialGraph("----------Shader-----------");
            MaterialWindow.DebugMaterialGraph(resultShader);
            
            defaultTextures = shaderPropertiesVisitor.GetDefaultTexutres();
            return resultShader;
        }

        public int numberOfChunks
        {
            get { return m_ShaderChunks.Count; }
        }
    }
}
