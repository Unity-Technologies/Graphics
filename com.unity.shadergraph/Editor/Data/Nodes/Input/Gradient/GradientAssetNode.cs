using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Gradient", "Gradient Asset")]
    public class GradientAssetNode : AbstractMaterialNode, IGeneratesFunction
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

		string GetFunctionName()
        {
            return string.Format("Unity_{0}", GetVariableNameForNode());
        }

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

                if(!GradientUtils.CheckEquivalency(m_Gradient, value))
                    scope = scope < ModificationScope.Graph ? ModificationScope.Graph : scope;

                if (scope > ModificationScope.Nothing)
                {
                    var newColorKeys = value.colorKeys;
                    var newAlphaKeys = value.alphaKeys;

                    gradient.SetKeys(newColorKeys, newAlphaKeys);
                    Dirty(ModificationScope.Graph);
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

        public override bool hasPreview { get { return false; } }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new GradientMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output,0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
			registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.AppendLine("Gradient {0} ()",
                    GetFunctionName(),
                    FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision));
                using (s.BlockScope())
                {
                    GradientUtils.GetGradientDeclaration(m_Gradient, ref s);
                    s.AppendLine("return g;", true);
                }
            });
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return string.Format("{0}()", GetFunctionName());
        }
    }
}