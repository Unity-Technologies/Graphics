using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.MaterialGraph.Drawing.Controls;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Input/Gradient")]
    public class GradientNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        Gradient m_Gradient;

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
                        if (currentColorKeys[i].color != newColorKeys[i].color || Math.Abs(currentColorKeys[i].time - newColorKeys[i].time) > 1e-9)
                            scope = scope < ModificationScope.Node ? ModificationScope.Node : scope;
                    }

                    for (var i = 0; i < currentAlphaKeys.Length; i++)
                    {
                        if (Math.Abs(currentAlphaKeys[i].alpha - newAlphaKeys[i].alpha) > 1e-9 || Math.Abs(currentAlphaKeys[i].time - newAlphaKeys[i].time) > 1e-9)
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

        public const int TimeInputSlotId = 0;
        const string k_TimeInputSlotName = "Time";

        public const int RGBAOutputSlotId = 1;
        const string k_RGBAOutputSlotName = "RGBA";

        public const int ROutputSlotId = 2;
        const string k_ROutputSlotName = "R";

        public const int GOutputSlotId = 3;
        const string k_GOutputSlotName = "G";

        public const int BOutputSlotId = 4;
        const string k_BOutputSlotName = "B";

        public const int AOutputSlotId = 5;
        const string k_AOutputSlotName = "A";

        public GradientNode()
        {
            name = "Gradient";

            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(TimeInputSlotId, k_TimeInputSlotName, k_TimeInputSlotName, SlotType.Input,0));
            AddSlot(new Vector4MaterialSlot(RGBAOutputSlotId, k_RGBAOutputSlotName, k_RGBAOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(ROutputSlotId, k_ROutputSlotName, k_ROutputSlotName, SlotType.Output,0));
            AddSlot(new Vector1MaterialSlot(GOutputSlotId, k_GOutputSlotName, k_GOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(BOutputSlotId, k_BOutputSlotName, k_BOutputSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(AOutputSlotId, k_AOutputSlotName, k_AOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { TimeInputSlotId, RGBAOutputSlotId, ROutputSlotId, GOutputSlotId, BOutputSlotId, AOutputSlotId });
            m_Gradient = new Gradient();
            var colorKeys = m_SerializableColorKeys.Select(k => new GradientColorKey(new Color(k.x, k.y, k.z, 1f), k.w)).ToArray();
            var alphaKeys = m_SerializableAlphaKeys.Select(k => new GradientAlphaKey(k.x, k.y)).ToArray();
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

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var rgbaOutputName = GetVariableNameForSlot(RGBAOutputSlotId);
            visitor.AddShaderChunk(string.Format("{0}4 {1} = {0}4({2}.rgb, {3}.r);", precision, rgbaOutputName, GetColorKeyName(0), GetAlphaKeyName(0)), false);

            visitor.AddShaderChunk("{", false);
            visitor.Indent();
            {
                var timeInputValue = GetSlotValue(TimeInputSlotId, generationMode);

                // Color interpolation
                for (var i = 0; i < m_Gradient.colorKeys.Length - 1; i++)
                    visitor.AddShaderChunk(string.Format("{3}.rgb = lerp({3}.rgb, {1}.rgb, smoothstep({0}.a, {1}.a, {2}));", GetColorKeyName(i), GetColorKeyName(i + 1), timeInputValue, rgbaOutputName), false);

                // Alpha interpolation
                for (var i = 0; i < m_Gradient.alphaKeys.Length - 1; i++)
                    visitor.AddShaderChunk(string.Format("{3}.a = lerp({3}.a, {1}.r, smoothstep({0}.g, {1}.g, {2}));", GetAlphaKeyName(i), GetAlphaKeyName(i + 1), timeInputValue, rgbaOutputName), false);
            }
            visitor.Deindent();
            visitor.AddShaderChunk("}", false);

            visitor.AddShaderChunk(string.Format("{0} {1} = {2}.r;", precision, GetVariableNameForSlot(ROutputSlotId), rgbaOutputName), false);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2}.g;", precision, GetVariableNameForSlot(GOutputSlotId), rgbaOutputName), false);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2}.b;", precision, GetVariableNameForSlot(BOutputSlotId), rgbaOutputName), false);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2}.a;", precision, GetVariableNameForSlot(AOutputSlotId), rgbaOutputName), false);
        }
    }
}
