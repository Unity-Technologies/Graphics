using UnityEngine.Graphing;
using System.Collections.Generic;

namespace UnityEngine.MaterialGraph
{
    [Title("Art/Adjustments/Levels")]
    public class LevelsNode : Function1Input, IGeneratesFunction
    {
        [SerializeField]
        private float m_InputMin = 0.0f;
        [SerializeField]
        private float m_InputMax = 1.0f;
        [SerializeField]
        private float m_InputGamma = 1.0f;
        [SerializeField]
        private float m_OutputMin = 0.0f;
        [SerializeField]
        private float m_OutputMax = 1.0f;

        public float inputMin
        {
            get { return m_InputMin; }
            set
            {
                if (m_InputMin == value)
                    return;
                m_InputMin = value;
                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }

        public float inputMax
        {
            get { return m_InputMax; }
            set
            {
                if (m_InputMax == value)
                    return;
                m_InputMax = value;
                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }

        public float inputGamma
        {
            get { return m_InputGamma; }
            set
            {
                if (m_InputGamma == value)
                    return;
                m_InputGamma = value;
                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }

        public float outputMin
        {
            get { return m_OutputMin; }
            set
            {
                if (m_OutputMin == value)
                    return;
                m_OutputMin = value;
                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }

        public float outputMax
        {
            get { return m_OutputMax; }
            set
            {
                if (m_OutputMax == value)
                    return;
                m_OutputMax = value;
                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }

        public LevelsNode()
        {
            name = "Levels";
        }

        protected override string GetFunctionName()
        {
            return "unity_levels_" + precision;
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
            {
                var propGuid = GetVariableNameForNode();
                visitor.AddShaderChunk(precision + " inputMin" + propGuid +";", true);
                visitor.AddShaderChunk(precision + " inputMax" + propGuid + ";", true);
                visitor.AddShaderChunk(precision + " inputInvGamma" + propGuid + ";", true);
                visitor.AddShaderChunk(precision + " outputMin" + propGuid + ";", true);
                visitor.AddShaderChunk(precision + " outputMax" + propGuid + ";", true);
            }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (generationMode.IsPreview())
                return;

            float inputInvGamma = 1.0f / m_InputGamma;
            var propGuid = GetVariableNameForNode();

            visitor.AddShaderChunk(precision + " inputMin" + propGuid + " = " + m_InputMin + ";", true);
            visitor.AddShaderChunk(precision + " inputMax" + propGuid + " = " + m_InputMax + ";", true);
            visitor.AddShaderChunk(precision + " inputInvGamma" + propGuid + " = " + inputInvGamma + ";", true);
            visitor.AddShaderChunk(precision + " outputMin" + propGuid + " = " + m_OutputMin + ";", true);
            visitor.AddShaderChunk(precision + " outputMax" + propGuid + " = " + m_OutputMax + ";", true);
        }

        protected override string GetFunctionCallBody(string inputValue)
        {
            string propGuid = GetVariableNameForNode();
            return GetFunctionName() + "(" + inputValue +
                ", inputMin" + propGuid +
                ", inputMax" + propGuid +
                ", inputInvGamma" + propGuid +
                ", outputMin" + propGuid +
                ", outputMax" + propGuid +
                ");";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk("inline " + precision + outputDimension + " unity_level_" + precision + " (" + precision + outputDimension + " arg1, "
                                        + precision + " inputMin, "
                                        + precision + " inputMax, "
                                        + precision + " inputInvGamma, "
                                        + precision + " outputMin, "
                                        + precision + " outputMax)", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk(precision + inputDimension + " colorMinClamped = max(arg1 - inputMin, 0.0);", false);
            outputString.AddShaderChunk(precision + inputDimension + " colorMinMaxClamped = min(colorMinClamped / (inputMax - inputMin), 1.0);", false);
            outputString.AddShaderChunk("return lerp(outputMin, outputMax, pow(colorMinMaxClamped, inputInvGamma));", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            var propGuid = GetVariableNameForNode();

            base.CollectPreviewMaterialProperties(properties);
            float inputInvGamma = 1.0f / m_InputGamma;
            properties.Add(new PreviewProperty { m_Name = "inputMin" + propGuid, m_PropType = PropertyType.Float, m_Float = m_InputMin } );
            properties.Add(new PreviewProperty { m_Name = "inputMax" + propGuid, m_PropType = PropertyType.Float, m_Float = m_InputMax } );
            properties.Add(new PreviewProperty { m_Name = "inputInvGamma" + propGuid, m_PropType = PropertyType.Float, m_Float = inputInvGamma });
            properties.Add(new PreviewProperty { m_Name = "outputMin" + propGuid, m_PropType = PropertyType.Float, m_Float = m_OutputMin });
            properties.Add(new PreviewProperty { m_Name = "outputMax" + propGuid, m_PropType = PropertyType.Float, m_Float = m_OutputMax });
        }
    }
}
