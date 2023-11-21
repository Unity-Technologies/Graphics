using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class CommandBufferGenerator
    {
        // When adding new functions to the core command buffer and you want to expose them to the rendergraph command buffers please
        // add them to the appropriate lists below.

        // Functions going in all command buffers
        static List<FunctionInfo> baseFunctions = new List<FunctionInfo> {
                new FunctionInfo("SetGlobalFloat", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetGlobalInt", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetGlobalInteger", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetGlobalVector", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetGlobalColor", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetGlobalMatrix", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetGlobalFloatArray", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetGlobalVectorArray", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetGlobalMatrixArray", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetGlobalTexture", textureArg: "value", modifiesGlobalState: true),
                new FunctionInfo("SetGlobalBuffer", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetGlobalConstantBuffer", textureArg: "", modifiesGlobalState: true),
                "SetLateLatchProjectionMatrices",
                "MarkLateLatchMatrixShaderPropertyID",
                "UnmarkLateLatchMatrix",
                new FunctionInfo("EnableShaderKeyword", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("EnableKeyword", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("DisableShaderKeyword", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("DisableKeyword", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetKeyword", textureArg: "", modifiesGlobalState: true),
                "SetShadowSamplingMode",
                "SetSinglePassStereo",
                "IssuePluginEvent",
                "IssuePluginEventAndData",
                "IssuePluginCustomBlit",
                "IssuePluginCustomTextureUpdateV2",
                "SetInvertCulling",
                "EnableScissorRect",
                "DisableScissorRect",
                "SetViewport",
                "SetGlobalDepthBias",
                "BeginSample",
                "EndSample",
                "IncrementUpdateCount",
                "SetViewProjectionMatrices",
                "SetupCameraProperties",
                "InvokeOnRenderObjectCallbacks"
            };

        // Functions for compute only
        static List<FunctionInfo> computeFunctions = new List<FunctionInfo> {
                "SetComputeFloatParam",
                "SetComputeIntParam",
                "SetComputeVectorArrayParam",
                "SetComputeMatrixParam",
                "SetComputeMatrixArrayParam",
                "SetComputeFloatParams",
                "SetComputeIntParams",
                new FunctionInfo("SetComputeTextureParam", textureArg: "rt"),
                "SetComputeBufferParam",
                "SetComputeConstantBufferParam",
                "SetComputeFloatParam",
                "SetComputeIntParam",
                "SetComputeVectorParam",
                "SetComputeVectorArrayParam",
                "SetComputeMatrixParam",
                "DispatchCompute",
                "BuildRayTracingAccelerationStructure",
                "SetRayTracingAccelerationStructure",
                "SetRayTracingBufferParam",
                "SetRayTracingConstantBufferParam",
                new FunctionInfo("SetRayTracingTextureParam", textureArg: "rt"),
                "SetRayTracingFloatParam",
                "SetRayTracingFloatParams",
                "SetRayTracingIntParam",
                "SetRayTracingIntParams",
                "SetRayTracingVectorParam",
                "SetRayTracingVectorArrayParam",
                "SetRayTracingMatrixParam",
                "SetRayTracingMatrixArrayParam",
                "DispatchRays",
                "SetBufferData",
                "SetBufferCounterValue",
                "CopyCounterValue"
            };

        // Functions for raster (native render passes) only
        static List<FunctionInfo> rasterFunctions = new List<FunctionInfo> {
                new FunctionInfo("DrawMesh", textureArg : "", modifiesGlobalState : false, triggersRasterization: true),
                new FunctionInfo("DrawMultipleMeshes", textureArg : "", modifiesGlobalState : false, triggersRasterization: true),
                new FunctionInfo("DrawRenderer", textureArg : "", modifiesGlobalState : false, triggersRasterization: true),
                new FunctionInfo("DrawRendererList", textureArg : "", modifiesGlobalState : false, triggersRasterization: true),
                new FunctionInfo("DrawProcedural", textureArg : "", modifiesGlobalState : false, triggersRasterization: true),
                new FunctionInfo("DrawProceduralIndirect", textureArg : "", modifiesGlobalState : false, triggersRasterization: true),
                new FunctionInfo("DrawMeshInstanced", textureArg : "", modifiesGlobalState : false, triggersRasterization: true),
                new FunctionInfo("DrawMeshInstancedProcedural", textureArg : "", modifiesGlobalState : false, triggersRasterization: true),
                new FunctionInfo("DrawMeshInstancedIndirect", textureArg : "", modifiesGlobalState : false, triggersRasterization: true),
                new FunctionInfo("DrawOcclusionMesh", textureArg : "", modifiesGlobalState : false, triggersRasterization: true),
                new FunctionInfo("SetInstanceMultiplier", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("ClearRenderTarget", textureArg : "", modifiesGlobalState : false, triggersRasterization: true),
                new FunctionInfo("SetFoveatedRenderingMode", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("ConfigureFoveatedRendering", textureArg: "", modifiesGlobalState: true),
                new FunctionInfo("SetWireframe", textureArg: "", modifiesGlobalState: true),
            };

        // Functions for unsafe (wrapper around Commandbuffer) only
        static List<FunctionInfo> unsafeFunctions = new List<FunctionInfo> {
                "SetRenderTarget",
                "Clear"
            };
        // Generated file header
        static string preamble =
@"
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Profiling;
using Unity.Profiling;
using UnityEngine.Rendering.RenderGraphModule;

// NOTE  NOTE  NOTE  NOTE  NOTE  NOTE  NOTE  NOTE  NOTE
//
// This file is automatically generated by reflection on the UnityEngine.Rendering.CommandBuffer type.
// If you changed the command buffer and want to expose the changes here please open and SRP project
// ""Edit/Rendering/Generate Core CommandBuffers"" menu option.
// This will generate the new command buffer C# files in the project root.
//
// Note that while automated,this doesn't mean you won't have to think. Please consider any new methods on the command
// buffer if they are safe to be executed on the async compute queue or not, if they can be executed inside a
// native render pass or not,... and add the function to the appropriate lists in CommandBufferGenerator.cs in the
// com.unity.render-pipelines.core\Editor\CommandBuffers\CommandBufferGenerator\CommandBufferGenerator.cs.
// If you are unsure please ask the RenderGraph package owners for advise.
//
// Once generated, review the generated file and move the approved files into:
// <unity root>\Packages\com.unity.render-pipelines.core\Runtime\CommandBuffers\
//
// NOTE  NOTE  NOTE  NOTE  NOTE  NOTE  NOTE  NOTE  NOTE
namespace UnityEngine.Rendering
{
";

        // Generated class header
        static string classPreamble =
@"
    /// <summary>
    /// @docString@
    /// </summary>
    public class @type_name@ : @base_type@
    {
        // @type_name@ is not created by users. The render graph creates them and passes them to the execute callback of the graph pass.
        internal @type_name@(CommandBuffer wrapped, RenderGraphPass executingPass, bool isAsync) : base(wrapped, executingPass, isAsync) { }
";


        // Generated interface header
        static string interfacePeamble =
@"
    /// <summary>
    /// @docString@
    /// </summary>
    public interface @type_name@ @base_type@
    {
";

        // Generated file footer
        static string postamble =
@"
    }
}
";

        // Generated individual function template
        static string functionTemplate =
@"
        /// <summary>Wraps [{0}](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.{0}.html) on a CommandBuffer.</summary>
{1}
        public void {0}{5}({2}) {6} {{ {4} m_WrappedCommandBuffer.{0}({3}); }}
";

        // Generated individual function template
        static string interfaceFunctionTemplate =
@"
        /// <summary>Wraps [{0}](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.{0}.html) on a CommandBuffer.</summary>
{1}
        public void {0}{5}({2}) {6};
";

        static string paramDocTemplate =
@"        /// <param name=""{0}"">[See CommandBuffer documentation](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.{1}.html)</param>";

        static string typeParamDocTemplate =
@"        /// <typeparam name=""{0}"">[See CommandBuffer documentation](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.{1}.html)</typeparam>";

        struct FunctionInfo
        {
            public FunctionInfo(string name, string textureArg, bool modifiesGlobalState = false, bool triggersRasterization = false)
            {
                this.name = name;
                this.textureArgs = textureArg;
                this.modifiesGlobalState = modifiesGlobalState;
                this.triggersRasterization = triggersRasterization;
            }

            public static implicit operator FunctionInfo(string value)
            {
                return new FunctionInfo(value, "", false);
            }

            // Name of the function to expose, all overloads are exposed with it.
            public string name;
            // Name of a texture pointer argument. This will receive extra validation for the Rendergraph.
            public string textureArgs;
            // Indicate if the function modifies global render state.
            public bool modifiesGlobalState;
            // Indicate if the function actually triggers rasterization and may thus cause pixel shader invocations.
            public bool triggersRasterization;
        }



        /// <summary>
        /// Generate a commandbuffer type exposing only certain methods of the base commandbuffer class
        /// </summary>
        /// <param name="className">Name of the new class</param>
        /// <param name="docString">Description of the class</param>
        /// <param name="baseType">Classes from which this new class inherits</param>
        /// <param name="isInterface">Whether or not the new class is an interface</param>
        /// <param name="validateRaster">Whether or not the functions in this class should validate that a raster commandbuffer has any render targets</param>
        /// <param name="functionList">List of functions on commandbuffer to expose</param>
        static void GenerateCommandBufferType(string className, string docString, string baseType, bool isInterface, bool validateRaster, IEnumerable<FunctionInfo> functionList)
        {
            StringBuilder result = new StringBuilder();
            result.Append(preamble);

            var str = (isInterface) ? interfacePeamble : classPreamble;
            str = str.Replace("@type_name@", className);
            str = str.Replace("@base_type@", baseType);
            str = str.Replace("@docString@", docString);
            result.Append(str);

            Type commandBufferType = typeof(CommandBuffer);
            //To restrict return properties. If all properties are required don't provide flag.
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var methods = commandBufferType.GetMethods(flags);

            foreach (var method in methods)
            {
                bool allowed = false;
                FunctionInfo info = new FunctionInfo();
                foreach (var fn in functionList)
                {
                    if (fn.name == method.Name)
                    {
                        allowed = true;
                        info = fn;
                        break;
                    }
                }
                if (!allowed) continue;

                StringBuilder argList = new StringBuilder();
                StringBuilder typedArgList = new StringBuilder();
                StringBuilder argDocList = new StringBuilder();
                StringBuilder validationCode = new StringBuilder();
                StringBuilder genericArgList = new StringBuilder();
                StringBuilder genericConstraints = new StringBuilder();

                if (info.modifiesGlobalState)
                {
                    validationCode.Append("ThrowIfGlobalStateNotAllowed(); ");
                }

                if (validateRaster && info.triggersRasterization)
                {
                    validationCode.Append("ThrowIfRasterNotAllowed(); ");
                }

                var arguments = method.GetParameters();
                bool separator = false;
                foreach (var arg in arguments)
                {
                    if (separator)
                    {
                        argList.Append(", ");
                        typedArgList.Append(", ");
                        argDocList.Append("\n");
                    }
                    separator = true;

                    // Comma separated argument lists
                    argList.Append(arg.Name);

                    // Typed, comma separated argument lists
                    // Texture args are replace to take a texture handle so we can do extra validation on them
                    if (arg.Name == info.textureArgs)
                    {
                        typedArgList.Append("TextureHandle");
                    }
                    else
                    {
                        typedArgList.Append(DecorateArgumentTypeString(arg));
                    }
                    typedArgList.Append(" ");
                    typedArgList.Append(arg.Name);

                    // Parameter docs
                    argDocList.AppendFormat(paramDocTemplate, arg.Name, method.Name);

                    if (arg.Name == info.textureArgs)
                    {
                        validationCode.Append("ValidateTextureHandle(" + arg.Name + "); ");
                    }
                }

                if (method.ContainsGenericParameters)
                {
                    // Hack: The current command buffer only inclues very simple single generic functions. We just hard code these but in the future we might need reflection on the generic arguments if needed.
                    genericArgList.Append("<T>");
                    genericConstraints.Append("where T : struct");
                    argDocList.Append("\n");
                    argDocList.AppendFormat(typeParamDocTemplate, "T", method.Name);
                }

                result.AppendFormat((isInterface) ? interfaceFunctionTemplate : functionTemplate, method.Name, argDocList.ToString(), typedArgList.ToString(), argList.ToString(), validationCode.ToString(), genericArgList.ToString(), genericConstraints.ToString());
            }

            result.Append(postamble);
            var outputFile = className + ".cs";
            File.WriteAllText(outputFile, result.ToString());
            Debug.Log("Wrote generated file to: " + Path.GetFullPath(outputFile) + " please review it, and if approved, move it to the 'com.unity.render-pipelines.core/Runtime/CommandBuffers/' folder.");
        }

        // Replace CLI types with c# shorthands for cleaner code...
        static string CSharpFlavour(Type type)
        {
            if (type == typeof(int))
            {
                return "int";
            }
            else if (type == typeof(uint))
            {
                return "uint";
            }
            else if (type == typeof(float))
            {
                return "float";
            }
            else if (type == typeof(string))
            {
                return "string";
            }
            else if (type == typeof(float))
            {
                return "float";
            }
            else if (type == typeof(byte))
            {
                return "byte";
            }
            else if (type == typeof(char))
            {
                return "float";
            }
            else if (type == typeof(bool))
            {
                return "bool";
            }
            return type.Name;
        }

        // Return A c# code string for a given parameter. Works for most common cases
        // and simple generics.
        static string DecorateArgumentTypeString(ParameterInfo arg)
        {
            var t = arg.ParameterType;
            if (t.HasElementType)
            {

                // Determine whether the type is an array.
                if (t.IsArray)
                {
                    string prefix = "";
                    if (arg.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
                    {
                        prefix = "params ";
                    }

                    return prefix + CSharpFlavour(t.GetElementType()) + "[]";
                }

                // Determine whether the type is an 'in' argument,
                else if (arg.IsIn)
                {
                    return "in " + CSharpFlavour(t.GetElementType());
                }

                // Determine whether the type is a reference. 'in' is also a reference but this was already handled above.
                else if (t.IsByRef)
                {
                    return "ref " + CSharpFlavour(t.GetElementType());
                }
                else
                {
                    throw new NotImplementedException("Implement special type handling for " + t.Name);
                }
            }
            else
            {
                // Is this an undefined generic
                if (t.ContainsGenericParameters)
                {
                    //The current command buffer only includes very simple single generic functions. We just hard code these but in the future we might need reflection on the generic arguments if needed.
                    // We strip the last two chars at the name is normally something like MyClass`1 for generics.
                    var types = t.GenericTypeArguments;
                    if (types.Length > 1) throw new NotImplementedException("Implement generic handling for more than 1 argument");
                    return t.Name.Substring(0, t.Name.Length - 2) + "<T>";
                }
                // This is a defined generic like List<int>
                else if (t.IsConstructedGenericType)
                {
                    var types = t.GenericTypeArguments;
                    if (types.Length > 1) throw new NotImplementedException("Implement generic handling for more than 1 argument");
                    return t.Name.Substring(0, t.Name.Length - 2) + "<" + CSharpFlavour(types[0]) + ">";
                }
                else
                {
                    return CSharpFlavour(t);
                }
            }
        }

        [MenuItem("Edit/Rendering/Generate Core CommandBuffers")]
        static void GenerateCommandBufferTypes()
        {
            GenerateCommandBufferType("IBaseCommandBuffer", "This interface declares functions shared by several command buffer types.", "", true, false, baseFunctions);
            GenerateCommandBufferType("IRasterCommandBuffer", "This interface declares functions that are specific to a rasterization command buffer.", ": IBaseCommandBuffer", true, false, rasterFunctions);
            GenerateCommandBufferType("IComputeCommandBuffer", "This interface declares functions that are specific to a compute command buffer.", ": IBaseCommandBuffer", true, false, computeFunctions);
            GenerateCommandBufferType("IUnsafeCommandBuffer", "This interface declares functions that are specific to an unsafe command buffer.", ": IBaseCommandBuffer, IRasterCommandBuffer, IComputeCommandBuffer", true, false, unsafeFunctions);

            GenerateCommandBufferType("RasterCommandBuffer", "A command buffer that is used with a rasterization render graph pass.", "BaseCommandBuffer, IRasterCommandBuffer", false, true, baseFunctions.Concat(rasterFunctions));
            GenerateCommandBufferType("ComputeCommandBuffer",  "A command buffer that is used with a compute render graph pass.", "BaseCommandBuffer, IComputeCommandBuffer", false, false, baseFunctions.Concat(computeFunctions));
            GenerateCommandBufferType("UnsafeCommandBuffer", "A command buffer that is used with an unsafe render graph pass.", "BaseCommandBuffer, IUnsafeCommandBuffer", false, false, baseFunctions.Concat(unsafeFunctions).Concat(rasterFunctions).Concat(computeFunctions));
        }
    }
}
