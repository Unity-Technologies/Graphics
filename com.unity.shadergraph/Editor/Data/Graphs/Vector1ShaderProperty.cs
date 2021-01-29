using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.FloatShaderProperty")]
    [FormerName("UnityEditor.ShaderGraph.Vector1ShaderProperty")]
    [BlackboardInputInfo(0, "Float")]
    public sealed class Vector1ShaderProperty : AbstractShaderProperty<float>
    {
        internal Vector1ShaderProperty()
        {
            displayName = "Float";
        }

        public override PropertyType propertyType => PropertyType.Float;

        internal override bool isExposable => true;
        internal override bool isRenamable => true;

        string enumTagString
        {
            get
            {
                switch (enumType)
                {
                    case EnumType.CSharpEnum:
                        return $"[Enum({m_CSharpEnumType.ToString()})]";
                    case EnumType.KeywordEnum:
                        return $"[KeywordEnum({string.Join(", ", enumNames)})]";
                    default:
                        string enumValuesString = "";
                        for (int i = 0; i < enumNames.Count; i++)
                        {
                            int value = (i < enumValues.Count) ? enumValues[i] : i;
                            enumValuesString += (enumNames[i] + ", " + value + ((i != enumNames.Count - 1) ? ", " : ""));
                        }
                        return $"[Enum({enumValuesString})]";
                }
            }
        }

        internal override string GetPropertyBlockString()
        {
            string valueString = NodeUtils.FloatToShaderValueShaderLabSafe(value);

            switch (floatType)
            {
                case FloatType.Slider:
                    return $"{hideTagString}{referenceName}(\"{displayName}\", Range({NodeUtils.FloatToShaderValue(m_RangeValues.x)}, {NodeUtils.FloatToShaderValue(m_RangeValues.y)})) = {valueString}";
                case FloatType.Integer:
                    return $"{hideTagString}{referenceName}(\"{displayName}\", Int) = {valueString}";
                case FloatType.Enum:
                    return $"{hideTagString}{enumTagString}{referenceName}(\"{displayName}\", Float) = {valueString}";
                default:
                    return $"{hideTagString}{referenceName}(\"{displayName}\", Float) = {valueString}";
            }
        }

        internal override string GetPropertyAsArgumentString()
        {
            return $"{concreteShaderValueType.ToShaderString(concretePrecision.ToShaderString())} {referenceName}";
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float, referenceName, decl, concretePrecision));
        }

        [SerializeField]
        FloatType m_FloatType = FloatType.Default;

        public FloatType floatType
        {
            get => m_FloatType;
            set => m_FloatType = value;
        }

        [SerializeField]
        Vector2 m_RangeValues = new Vector2(0, 1);

        public Vector2 rangeValues
        {
            get => m_RangeValues;
            set => m_RangeValues = value;
        }

        EnumType m_EnumType = EnumType.Enum;

        public EnumType enumType
        {
            get => m_EnumType;
            set => m_EnumType = value;
        }

        Type m_CSharpEnumType;

        public Type cSharpEnumType
        {
            get => m_CSharpEnumType;
            set => m_CSharpEnumType = value;
        }

        List<string> m_EnumNames = new List<string>();

        public List<string> enumNames
        {
            get => m_EnumNames;
            set => m_EnumNames = value;
        }

        List<int> m_EnumValues = new List<int>();

        public List<int> enumValues
        {
            get => m_EnumValues;
            set => m_EnumValues = value;
        }

        internal override AbstractMaterialNode ToConcreteNode()
        {
            switch (m_FloatType)
            {
                case FloatType.Slider:
                    return new SliderNode { value = new Vector3(value, m_RangeValues.x, m_RangeValues.y) };
                case FloatType.Integer:
                    return new IntegerNode { value = (int)value };
                default:
                    var node = new Vector1Node();
                    node.FindInputSlot<Vector1MaterialSlot>(Vector1Node.InputSlotXId).value = value;
                    return node;
            }
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                floatValue = value
            };
        }

        internal override ShaderInput Copy()
        {
            return new Vector1ShaderProperty()
            {
                displayName = displayName,
                value = value,
                floatType = floatType,
                rangeValues = rangeValues,
                enumType = enumType,
                enumNames = enumNames,
                enumValues = enumValues,
            };
        }

        public override int latestVersion => 1;
        public override void OnAfterDeserialize(string json)
        {
            if (sgVersion == 0)
            {
                LegacyShaderPropertyData.UpgradeToHLSLDeclarationOverride(json, this);
                ChangeVersion(1);
            }
        }
    }

    public enum FloatType { Default, Slider, Integer, Enum }

    public enum EnumType { Enum, CSharpEnum, KeywordEnum, }
}
