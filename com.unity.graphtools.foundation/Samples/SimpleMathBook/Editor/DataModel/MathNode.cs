using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class MathNode : NodeModel
    {
        static TypeHandle[] s_DefaultAllowedInputs = { TypeHandle.Float, TypeHandle.Int };
        public virtual TypeHandle[] ValueInputTypes => s_DefaultAllowedInputs;
        public Value GetValue(IPortModel port)
        {
            return port.GetValue();
        }

        public abstract Value Evaluate();

        public abstract string CompileToCSharp(MathBookGraphProcessor context);

        protected TypeHandle FirstInputType()
        {
            var t = AllInputTypes().FirstOrDefault();
            return t == default ? TypeHandle.Unknown : t;
        }

        protected IEnumerable<TypeHandle> AllInputTypes()
        {
            foreach (var portModel in InputsByDisplayOrder)
            {
                var source = portModel.GetConnectedPorts().FirstOrDefault();
                yield return source?.DataTypeHandle ?? portModel.DataTypeHandle;
            }
        }

        public virtual bool CheckInputs(out string errorMessage)
        {
            errorMessage = null;

            foreach (var p in InputsByDisplayOrder)
            {
                var source = p.GetConnectedPorts().FirstOrDefault();
                if (source != null && !ValueInputTypes.Contains(source.DataTypeHandle))
                {
                    if (errorMessage == null)
                        errorMessage = "";
                    else
                        errorMessage += "\n";

                    errorMessage = $"Port {p.UniqueName} in Node {DisplayTitle} has value of unexpected type {source.PortDataType}";
                }
            }

            return errorMessage == null;
        }

        protected override IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType, TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new MathBookPortModel
            {
                Direction = direction,
                Orientation = orientation,
                PortType = portType,
                DataTypeHandle = dataType,
                Title = portName ?? "",
                UniqueName = portId,
                Options = options,
                NodeModel = this,
                AssetModel = AssetModel
            };
        }
    }
}
