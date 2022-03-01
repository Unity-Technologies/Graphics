using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public static class MathNodeExtensions
    {
        public static ValueType FirstInputType(this MathNode mathNode) => mathNode.InputsByDisplayOrder.FirstOrDefault().GetValue().Type;
        public static IEnumerable<ValueType> AllInputTypes(this MathNode mathNode) => mathNode.InputsByDisplayOrder.Select(p => p.GetValue().Type);
    }
}
