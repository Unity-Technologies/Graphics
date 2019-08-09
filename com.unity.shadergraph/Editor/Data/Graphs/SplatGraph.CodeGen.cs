using System.Linq;

namespace UnityEditor.ShaderGraph
{
    partial class SplatGraph
    {
        public void GenerateCode(ShaderStringBuilder sb, GraphContext graphContext, FunctionRegistry functionRegistry, PropertyCollector shaderProperties, GenerationMode generationMode)
        {
            bool splatting = !generationMode.IsPreview();

            ShaderStringBuilder[] splatFunctions = null;
            if (splatting)
            {
                splatFunctions = new ShaderStringBuilder[m_Orders.Length - 1]; // We don't do splatting on the last order.
                for (int i = 0; i < m_Orders.Length - 1; ++i)
                {
                    ref readonly var order = ref m_Orders[i];
                    var splatFunctionName = $"SplatFunction_Order{i}";

                    var splatFunction = splatFunctions[i] = new ShaderStringBuilder();
                    splatFunction.Append($"void {splatFunctionName}({graphContext.graphInputStructName} IN");

                    // splat function inputs
                    foreach (var inputVar in order.InputVars)
                        splatFunction.Append($", {inputVar.ShaderTypeString} {inputVar.Name}");

                    //// splat function input derivatives
                    //foreach (var d in m_SplatGraph.InputDerivatives)
                    //    splatFunction.Append($", {d.VariableType} ddx_{d.VariableName}, {d.VariableType} ddy_{d.VariableName}");

                    // splat function outputs
                    foreach (var outputVar in order.OutputVars)
                    {
                        if (outputVar.InOut)
                            splatFunction.Append($", inout {outputVar.ShaderTypeString} {outputVar.Name}");
                        else
                            splatFunction.Append($", out {outputVar.ShaderTypeString} out{outputVar.Name}");
                    }

                    splatFunction.AppendLine(")");
                    splatFunction.AppendLine("{");
                    splatFunction.IncreaseIndent();
                }
            }

            for (int i = 0; i < m_Orders.Length; ++i)
            {
                var orderSplatting = splatting && i != m_Orders.Length - 1;
                ref readonly var order = ref m_Orders[i];

                foreach (var splatNode in order.Nodes)
                {
                    var node = splatNode.SgNode;

                    if (node is IGeneratesFunction functionNode)
                    {
                        functionRegistry.builder.currentNode = node;
                        functionNode.GenerateNodeFunction(functionRegistry, graphContext, generationMode);
                        functionRegistry.builder.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                    }

                    if (node is ISplatLoopNode splatLoopNode)
                    {
                        sb.currentNode = node;
                        splatLoopNode.GenerateSetupCode(sb, graphContext, generationMode);
                        sb.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                    }

                    ref var outputString = ref orderSplatting && splatNode.RunsPerSplat ? ref splatFunctions[i] : ref sb;
                    outputString.currentNode = node;

                    if (node is IGeneratesBodyCode bodyNode)
                    {
                        bodyNode.GenerateNodeCode(outputString, graphContext, generationMode);

                        //var differentiable = node.SgNode as IDifferentiable;
                        //foreach (var differentiateSlot in node.DifferentiateOutputSlots)
                        //{
                        //    var derivative = differentiable.GetDerivative(differentiateSlot);
                        //    var funcVars = new (string ddx, string ddy)[derivative.FuncVariableInputSlotIds.Count];
                        //    for (int i = 0; i < derivative.FuncVariableInputSlotIds.Count; ++i)
                        //    {
                        //        var inputSlotId = derivative.FuncVariableInputSlotIds[i];
                        //        var inputSlot = node.Node.FindInputSlot<MaterialSlot>(inputSlotId);
                        //        var (value, ddx, ddy) = node.Node.GetSlotValueWithDerivative(inputSlotId, generationMode);
                        //        funcVars[i] = (ddx, ddy);
                        //    }
                        //    var outputSlot = node.Node.FindOutputSlot<MaterialSlot>(differentiateSlot);
                        //    var typeString = outputSlot.concreteValueType.ToShaderString(node.Node.concretePrecision);
                        //    var ddxVar = $"ddx_{node.Node.GetVariableNameForSlot(differentiateSlot)} = {string.Format(derivative.Function(generationMode), funcVars.Select(v => v.ddx).ToArray())}";
                        //    var ddyVar = $"ddy_{node.Node.GetVariableNameForSlot(differentiateSlot)} = {string.Format(derivative.Function(generationMode), funcVars.Select(v => v.ddy).ToArray())}";
                        //    if (ddxVar.Length > 60)
                        //    {
                        //        splatFunction.AppendLine($"{typeString} {ddxVar};");
                        //        splatFunction.AppendLine($"{typeString} {ddyVar};");
                        //    }
                        //    else
                        //        splatFunction.AppendLine($"{typeString} {ddxVar}, {ddyVar};");
                        //}
                    }

                    outputString.ReplaceInCurrentMapping(PrecisionUtil.Token, node.concretePrecision.ToShaderString());
                    node.CollectShaderProperties(shaderProperties, generationMode);
                }

                if (orderSplatting)
                {
                    var splatFunction = splatFunctions[i];
                    var splatFunctionName = $"SplatFunction_Order{i}";

                    // Declare the output arrays in main function.
                    // Assign the output from nodes to the out parameters inside the splat function.
                    foreach (var outputVar in order.OutputVars.Where(output => !output.InOut))
                    {
                        splatFunction.AppendLine($"out{outputVar.Name} = {outputVar.Name};");
                        sb.AppendLine($"{outputVar.ShaderTypeString} splat{outputVar.Name}[{graphContext.splatCount}];");
                    }

                    splatFunction.DecreaseIndent();
                    splatFunction.AppendLine("}");

                    // Call the splat function for each splat.
                    for (int splat = 0; splat < graphContext.splatCount; ++splat)
                    {
                        if (splat == 4)
                        {
                            sb.AppendLine($"#ifdef {GraphData.kSplatCount8Keyword}");
                            sb.IncreaseIndent();
                        }

                        sb.AppendIndentation();
                        sb.Append($"{splatFunctionName}(IN");
                        foreach (var inputVar in order.InputVars)
                        {
                            var varName = inputVar.Name;
                            if (inputVar.Type == SplatFunctionInputType.SplatProperty)
                                varName = $"{varName}{splat}";
                            else if (inputVar.Type == SplatFunctionInputType.SplatArray)
                                varName = $"splat{varName}[{splat}]";
                            sb.Append($", {varName}");
                        }
                        //foreach (var derivative in m_SplatGraph.InputDerivatives)
                        //    sb.Append($", ddx_{derivative.VariableName}, ddy_{derivative.VariableName}");
                        foreach (var outputVar in order.OutputVars)
                        {
                            if (outputVar.InOut)
                                sb.Append($", {outputVar.Name}");
                            else
                                sb.Append($", splat{outputVar.Name}[{splat}]");
                        }
                        sb.Append(");");
                        sb.AppendNewLine();

                        if (splat == 7)
                        {
                            sb.DecreaseIndent();
                            sb.AppendLine("#endif");
                        }
                    }

                    foreach (var outputVar in order.OutputVars.Where(output => !output.InOut))
                        sb.AppendLine($"{outputVar.ShaderTypeString} {outputVar.Name} = splat{outputVar.Name}[0];");

                    functionRegistry.ProvideFunction(splatFunctionName, s => s.Concat(splatFunction));
                }
            }
        }
    }
}
