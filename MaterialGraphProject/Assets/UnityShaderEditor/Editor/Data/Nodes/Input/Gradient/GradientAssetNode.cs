using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input/Gradient/Gradient Asset")]
    public class GradientAssetNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction
    {
        [SerializeField]
        private float m_Value;

        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        public GradientAssetNode()
        {
            name = "Gradient Asset";
            UpdateNodeAfterDeserialization();
        }

        /// -------------------------------

        Gradient m_Gradient = new Gradient();

        [SerializeField]
        Vector4[] m_SerializableColorKeys = { new Vector4(1f, 1f, 1f, 0f), new Vector4(0f, 0f, 0f, 1f), };

        [SerializeField]
        Vector2[] m_SerializableAlphaKeys = { new Vector2(1f, 0f), new Vector2(1f, 1f) };

        [GradientControl("")]
        public Gradient gradient
        {
            get
            {
                return m_Gradient;
            }
            set
            {
                var scope = ModificationScope.Nothing;

                var currentColorKeys = m_Gradient.colorKeys;
                var currentAlphaKeys = m_Gradient.alphaKeys;

                var newColorKeys = value.colorKeys;
                var newAlphaKeys = value.alphaKeys;

                if (currentColorKeys.Length != newColorKeys.Length || currentAlphaKeys.Length != newAlphaKeys.Length)
                {
                    scope = scope < ModificationScope.Graph ? ModificationScope.Graph : scope;
                }
                else
                {
                    for (var i = 0; i < currentColorKeys.Length; i++)
                    {
                        if (currentColorKeys[i].color != newColorKeys[i].color || Mathf.Abs(currentColorKeys[i].time - newColorKeys[i].time) > 1e-9)
                            scope = scope < ModificationScope.Node ? ModificationScope.Node : scope;
                    }

                    for (var i = 0; i < currentAlphaKeys.Length; i++)
                    {
                        if (Mathf.Abs(currentAlphaKeys[i].alpha - newAlphaKeys[i].alpha) > 1e-9 || Mathf.Abs(currentAlphaKeys[i].time - newAlphaKeys[i].time) > 1e-9)
                            scope = scope < ModificationScope.Node ? ModificationScope.Node : scope;
                    }
                }

                if (scope > ModificationScope.Nothing)
                {
                    gradient.SetKeys(newColorKeys, newAlphaKeys);
                    if (onModified != null)
                        onModified(this, scope);
                }
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_Gradient = new Gradient();
            var colorKeys = m_SerializableColorKeys.Select(k => new GradientColorKey(new Color(k.x, k.y, k.z, 1f), k.w)).ToArray();
            var alphaKeys = m_SerializableAlphaKeys.Select(k => new GradientAlphaKey(k.x, k.y)).ToArray();
            m_SerializableAlphaKeys = null;
            m_SerializableColorKeys = null;
            m_Gradient.SetKeys(colorKeys, alphaKeys);
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_SerializableColorKeys = gradient.colorKeys.Select(k => new Vector4(k.color.r, k.color.g, k.color.b, k.time)).ToArray();
            m_SerializableAlphaKeys = gradient.alphaKeys.Select(k => new Vector2(k.alpha, k.time)).ToArray();
        }

        string GetColorKeyName(int index)
        {
            return string.Format("{0}_ck{1}", GetVariableNameForNode(), index);
        }

        string GetAlphaKeyName(int index)
        {
            return string.Format("{0}_ak{1}", GetVariableNameForNode(), index);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            base.CollectShaderProperties(properties, generationMode);

            var colorKeys = m_Gradient.colorKeys;
            var alphaKeys = m_Gradient.alphaKeys;

            for (var i = 0; i < colorKeys.Length; i++)
            {
                var colorKey = colorKeys[i];
                properties.AddShaderProperty(new Vector4ShaderProperty
                {
                    overrideReferenceName = GetColorKeyName(i),
                    generatePropertyBlock = false,
                    value = new Vector4(colorKey.color.r, colorKey.color.g, colorKey.color.b, colorKey.time)
                });
            }

            for (var i = 0; i < alphaKeys.Length; i++)
            {
                properties.AddShaderProperty(new Vector2ShaderProperty
                {
                    overrideReferenceName = GetAlphaKeyName(i),
                    generatePropertyBlock = false,
                    value = new Vector2(alphaKeys[i].alpha, alphaKeys[i].time)
                });
            }
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);

            var colorKeys = m_Gradient.colorKeys;
            var alphaKeys = m_Gradient.alphaKeys;

            for (var i = 0; i < colorKeys.Length; i++)
            {
                var colorKey = colorKeys[i];
                properties.Add(new PreviewProperty()
                {
                    m_Name = GetColorKeyName(i),
                    m_PropType = PropertyType.Vector4,
                    m_Vector4 = new Vector4(colorKey.color.r, colorKey.color.g, colorKey.color.b, colorKey.time)
                });
            }

            for (var i = 0; i < alphaKeys.Length; i++)
            {
                properties.Add(new PreviewProperty
                {
                    m_Name = GetAlphaKeyName(i),
                    m_PropType = PropertyType.Vector2,
                    m_Vector4 = new Vector2(alphaKeys[i].alpha, alphaKeys[i].time)
                });
            }
        }

        /// -------------------------------

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new GradientMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output,0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            //if (generationMode.IsPreview())
              //  return;

            visitor.AddShaderChunk("Gradient " + GetVariableNameForNode() + ";", true);
            visitor.AddShaderChunk(string.Format("Unity_{0} ({0});", GetVariableNameForNode()), true);
        }

        string GetColorKey(int index, Color color, float time)
        {
            return string.Format("g.colors[{0}] = float4({1}, {2}, {3}, {4});", index, color.r, color.g, color.b, time);
        }

        string GetAlphaKey(int index, float alpha, float time)
        {
            return string.Format("g.alphas[{0}] = float2({1}, {2});", index, alpha, time);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            //if (generationMode.IsPreview())
              //  return;

            string[] colors = new string[8];
            for(int i = 0; i < colors.Length; i++)
                colors[i] = string.Format("g.colors[{0}] = float4(0, 0, 0, 0);", i.ToString());
            for(int i = 0; i < m_Gradient.colorKeys.Length; i++)
                colors[i] = GetColorKey(i, m_Gradient.colorKeys[i].color, m_Gradient.colorKeys[i].time);

            string[] alphas = new string[8];
            for(int i = 0; i < colors.Length; i++)
                alphas[i] = string.Format("g.alphas[{0}] = float2(0, 0);", i.ToString());
            for(int i = 0; i < m_Gradient.alphaKeys.Length; i++)
                alphas[i] = GetAlphaKey(i, m_Gradient.alphaKeys[i].alpha, m_Gradient.alphaKeys[i].time);

            visitor.AddShaderChunk(string.Format("void Unity_{0} (out Gradient Out)", GetVariableNameForNode()), true);
            visitor.AddShaderChunk("{", true);
            visitor.AddShaderChunk("Gradient g;", true);
            visitor.AddShaderChunk("g.type = 0;", true);
            visitor.AddShaderChunk(string.Format("g.colorsLength = {0};", m_Gradient.colorKeys.Length), true);
            visitor.AddShaderChunk(string.Format("g.alphasLength = {0};", m_Gradient.alphaKeys.Length), true);

            for(int i = 0; i < colors.Length; i++)
                visitor.AddShaderChunk(colors[i], true);

            for(int i = 0; i < alphas.Length; i++)
                visitor.AddShaderChunk(alphas[i], true);

            /*visitor.AddShaderChunk("g.colors[0] = float4(1,0,0,0);", true);
            visitor.AddShaderChunk("g.colors[1] = float4(0,1,0,1);", true);
            visitor.AddShaderChunk("g.colors[2] = float4(1,0,0,0);", true);
            visitor.AddShaderChunk("g.colors[3] = float4(0,1,0,1);", true);
            visitor.AddShaderChunk("g.colors[4] = float4(1,0,0,0);", true);
            visitor.AddShaderChunk("g.colors[5] = float4(0,1,0,1);", true);
            visitor.AddShaderChunk("g.colors[6] = float4(1,0,0,0);", true);
            visitor.AddShaderChunk("g.colors[7] = float4(0,1,0,1);", true);
            visitor.AddShaderChunk("g.alphas[0] = float2(1,0);", true);
            visitor.AddShaderChunk("g.alphas[1] = float2(1,1);", true);
            visitor.AddShaderChunk("g.alphas[2] = float2(1,0);", true);
            visitor.AddShaderChunk("g.alphas[3] = float2(1,1);", true);
            visitor.AddShaderChunk("g.alphas[4] = float2(1,0);", true);
            visitor.AddShaderChunk("g.alphas[5] = float2(1,1);", true);
            visitor.AddShaderChunk("g.alphas[6] = float2(1,0);", true);
            visitor.AddShaderChunk("g.alphas[7] = float2(1,1);", true);*/
            visitor.AddShaderChunk("Out = g;", true);
            visitor.AddShaderChunk("}", true);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }
    }
}
