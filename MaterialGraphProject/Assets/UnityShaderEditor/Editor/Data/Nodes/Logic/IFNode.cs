using System;
using System.Reflection;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
// PROBABLY USEFUL, BUT BROKEN AT THE MOMENT
//    [Title("Logic", "If")]
//    public class IfNode : CodeFunctionNode
//    {
//        public enum ComparisonOperationType
//        {
//            Equal = 0,
//            NotEqual,
//            GreaterThan,
//            GreaterThanOrEqual,
//            LessThan,
//            LessThanOrEqual
//        }
//
//        [SerializeField]
//        private ComparisonOperationType m_comparisonOperation = ComparisonOperationType.Equal;
//
//        public ComparisonOperationType ComparisonOperation
//        {
//            get { return m_comparisonOperation; }
//            set
//            {
//                if (m_comparisonOperation == value)
//                    return;
//
//                m_comparisonOperation = value;
//                if (onModified != null)
//                {
//                    onModified(this, ModificationScope.Graph);
//                }
//            }
//        }
//
//        public IfNode()
//        {
//            name = "If";
//        }
//
//        protected override MethodInfo GetFunctionToConvert()
//        {
//            switch (ComparisonOperation)
//            {
//                case ComparisonOperationType.Equal:
//                    return GetType().GetMethod("Unity_IfEqual", BindingFlags.Static | BindingFlags.NonPublic);
//                case ComparisonOperationType.NotEqual:
//                    return GetType().GetMethod("Unity_IfNotEqual", BindingFlags.Static | BindingFlags.NonPublic);
//                case ComparisonOperationType.GreaterThan:
//                    return GetType().GetMethod("Unity_IfGreaterThan", BindingFlags.Static | BindingFlags.NonPublic);
//                case ComparisonOperationType.GreaterThanOrEqual:
//                    return GetType().GetMethod("Unity_IfGreaterThanOrEqual", BindingFlags.Static | BindingFlags.NonPublic);
//                case ComparisonOperationType.LessThan:
//                    return GetType().GetMethod("Unity_IfLessThan", BindingFlags.Static | BindingFlags.NonPublic);
//                case ComparisonOperationType.LessThanOrEqual:
//                    return GetType().GetMethod("Unity_IfLessThanOrEqual", BindingFlags.Static | BindingFlags.NonPublic);
//                default:
//                    throw new ArgumentOutOfRangeException();
//            }
//        }
//
//        const string functionTemplate = @"
//{
//    if({comparitor})
//    {
//        result = trueValue;
//    }
//    else
//    {
//        result = falseValue;
//    }
//}
//";
//        static string Unity_IfEqual(
//            [Slot(0, Binding.None)] DynamicDimensionVector a,
//            [Slot(1, Binding.None)] DynamicDimensionVector b,
//            [Slot(2, Binding.None)] DynamicDimensionVector trueValue,
//            [Slot(3, Binding.None)] DynamicDimensionVector falseValue,
//            [Slot(4, Binding.None)] DynamicDimensionVector result)
//        {
//            return functionTemplate.Replace("{comparitor}", "a == b");
//        }
//
//        static string Unity_IfNotEqual(
//            [Slot(0, Binding.None)] DynamicDimensionVector a,
//            [Slot(1, Binding.None)] DynamicDimensionVector b,
//            [Slot(2, Binding.None)] DynamicDimensionVector trueValue,
//            [Slot(3, Binding.None)] DynamicDimensionVector falseValue,
//            [Slot(4, Binding.None)] DynamicDimensionVector result)
//        {
//            return functionTemplate.Replace("{comparitor}", "a != b");
//        }
//
//        static string Unity_IfGreaterThan(
//            [Slot(0, Binding.None)] DynamicDimensionVector a,
//            [Slot(1, Binding.None)] DynamicDimensionVector b,
//            [Slot(2, Binding.None)] DynamicDimensionVector trueValue,
//            [Slot(3, Binding.None)] DynamicDimensionVector falseValue,
//            [Slot(4, Binding.None)] DynamicDimensionVector result)
//        {
//            return functionTemplate.Replace("{comparitor}", "a > b");
//        }
//
//        static string Unity_IfGreaterThanOrEqual(
//            [Slot(0, Binding.None)] DynamicDimensionVector a,
//            [Slot(1, Binding.None)] DynamicDimensionVector b,
//            [Slot(2, Binding.None)] DynamicDimensionVector trueValue,
//            [Slot(3, Binding.None)] DynamicDimensionVector falseValue,
//            [Slot(4, Binding.None)] DynamicDimensionVector result)
//        {
//            return functionTemplate.Replace("{comparitor}", "a >= b");
//        }
//
//        static string Unity_IfLessThan(
//            [Slot(0, Binding.None)] DynamicDimensionVector a,
//            [Slot(1, Binding.None)] DynamicDimensionVector b,
//            [Slot(2, Binding.None)] DynamicDimensionVector trueValue,
//            [Slot(3, Binding.None)] DynamicDimensionVector falseValue,
//            [Slot(4, Binding.None)] DynamicDimensionVector result)
//        {
//            return functionTemplate.Replace("{comparitor}", "a < b");
//        }
//
//        static string Unity_IfLessThanOrEqual(
//            [Slot(0, Binding.None)] DynamicDimensionVector a,
//            [Slot(1, Binding.None)] DynamicDimensionVector b,
//            [Slot(2, Binding.None)] DynamicDimensionVector trueValue,
//            [Slot(3, Binding.None)] DynamicDimensionVector falseValue,
//            [Slot(4, Binding.None)] DynamicDimensionVector result)
//        {
//            return functionTemplate.Replace("{comparitor}", "a <= b");
//        }
//    }
}
