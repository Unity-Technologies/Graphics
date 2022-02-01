using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class MathNode : NodeModel
    {
        static ValueType[] DefaultAllowedInputs = { ValueType.Float, ValueType.Int };
        public virtual ValueType[] ValueInputTypes => DefaultAllowedInputs;
        public Value GetValue(IPortModel port)
        {
            return port.GetValue();
        }

        public abstract Value Evaluate();

        public virtual bool CheckInputs(out string errorMessage)
        {
            errorMessage = null;
            var badPorts = InputsByDisplayOrder
                .Where(p => !ValueInputTypes.Contains(p.GetValue().Type))
                .ToList();
            foreach (var p in badPorts)
            {
                if (errorMessage == null)
                    errorMessage = "";
                else
                    errorMessage += "\n";

                errorMessage = $"Port {p.UniqueName} in Node {DisplayTitle} has value of unexpected type {p.GetValue().Type}";
            }
            return badPorts.Count == 0;
        }
    }
}
