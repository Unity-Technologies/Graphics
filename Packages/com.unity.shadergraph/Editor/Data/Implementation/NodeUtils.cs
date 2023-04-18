using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor.ShaderGraph;
using System.Text.RegularExpressions;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering.ShaderGraph;

namespace UnityEditor.Graphing
{
    class SlotConfigurationException : Exception
    {
        public SlotConfigurationException(string message)
            : base(message)
        { }
    }

    static class NodeUtils
    {
        static string NodeDocSuffix = "-Node";

        public static void SlotConfigurationExceptionIfBadConfiguration(AbstractMaterialNode node, IEnumerable<int> expectedInputSlots, IEnumerable<int> expectedOutputSlots)
        {
            var missingSlots = new List<int>();

            var inputSlots = expectedInputSlots as IList<int> ?? expectedInputSlots.ToList();
            missingSlots.AddRange(inputSlots.Except(node.GetInputSlots<MaterialSlot>().Select(x => x.id)));

            var outputSlots = expectedOutputSlots as IList<int> ?? expectedOutputSlots.ToList();
            missingSlots.AddRange(outputSlots.Except(node.GetOutputSlots<MaterialSlot>().Select(x => x.id)));

            if (missingSlots.Count == 0)
                return;

            var toPrint = missingSlots.Select(x => x.ToString());

            throw new SlotConfigurationException(string.Format("Missing slots {0} on node {1}", string.Join(", ", toPrint.ToArray()), node));
        }

        public static IEnumerable<IEdge> GetAllEdges(AbstractMaterialNode node)
        {
            var result = new List<IEdge>();
            var validSlots = ListPool<MaterialSlot>.Get();

            validSlots.AddRange(node.GetInputSlots<MaterialSlot>());
            for (int index = 0; index < validSlots.Count; index++)
            {
                var inputSlot = validSlots[index];
                result.AddRange(node.owner.GetEdges(inputSlot.slotReference));
            }

            validSlots.Clear();
            validSlots.AddRange(node.GetOutputSlots<MaterialSlot>());
            for (int index = 0; index < validSlots.Count; index++)
            {
                var outputSlot = validSlots[index];
                result.AddRange(node.owner.GetEdges(outputSlot.slotReference));
            }

            ListPool<MaterialSlot>.Release(validSlots);
            return result;
        }

        public static string GetDuplicateSafeNameForSlot(AbstractMaterialNode node, int slotId, string name)
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            node.GetSlots(slots);

            name = name.Trim();
            return GraphUtil.SanitizeName(slots.Where(p => p.id != slotId).Select(p => p.RawDisplayName()), "{0} ({1})", name);
        }

        // CollectNodesNodeFeedsInto looks at the current node and calculates
        // which child nodes it depends on for it's calculation.
        // Results are returned depth first so by processing each node in
        // order you can generate a valid code block.
        public enum IncludeSelf
        {
            Include,
            Exclude
        }

        public static SlotReference DepthFirstCollectRedirectNodeFromNode(RedirectNodeData node)
        {
            var inputSlot = node.FindSlot<MaterialSlot>(RedirectNodeData.kInputSlotID);
            foreach (var edge in node.owner.GetEdges(inputSlot.slotReference))
            {
                // get the input details
                var outputSlotRef = edge.outputSlot;
                var inputNode = outputSlotRef.node;
                // If this is a redirect node we continue to look for the top one
                if (inputNode is RedirectNodeData redirectNode)
                {
                    return DepthFirstCollectRedirectNodeFromNode(redirectNode);
                }
                return outputSlotRef;
            }
            // If no edges it is the first redirect node without an edge going into it and we should return the slot ref
            return node.GetSlotReference(RedirectNodeData.kInputSlotID);
        }

        public static void DepthFirstCollectNodesFromNode(List<AbstractMaterialNode> nodeList, AbstractMaterialNode node,
            IncludeSelf includeSelf = IncludeSelf.Include, List<KeyValuePair<ShaderKeyword, int>> keywordPermutation = null, bool ignoreActiveState = false)
        {
            // no where to start
            if (node == null)
                return;

            // already added this node
            if (nodeList.Contains(node))
                return;

            IEnumerable<int> ids;

            // If this node is a keyword node and we have an active keyword permutation
            // The only valid port id is the port that corresponds to that keywords value in the active permutation
            if (node is KeywordNode keywordNode && keywordPermutation != null)
            {
                var valueInPermutation = keywordPermutation.Where(x => x.Key == keywordNode.keyword).FirstOrDefault();
                ids = new int[] { keywordNode.GetSlotIdForPermutation(valueInPermutation) };
            }
            else
            {
                ids = node.GetInputSlots<MaterialSlot>().Select(x => x.id);
            }

            foreach (var slot in ids)
            {
                foreach (var edge in node.owner.GetEdges(node.GetSlotReference(slot)))
                {
                    var outputNode = edge.outputSlot.node;
                    if (outputNode != null)
                        DepthFirstCollectNodesFromNode(nodeList, outputNode, keywordPermutation: keywordPermutation, ignoreActiveState: ignoreActiveState);
                }
            }

            if (includeSelf == IncludeSelf.Include && (node.isActive || ignoreActiveState))
                nodeList.Add(node);
        }

        internal static List<AbstractMaterialNode> GetParentNodes(AbstractMaterialNode node)
        {
            List<AbstractMaterialNode> nodeList = new List<AbstractMaterialNode>();
            var ids = node.GetInputSlots<MaterialSlot>().Select(x => x.id);
            foreach (var slot in ids)
            {
                if (node.owner == null)
                    break;
                foreach (var edge in node.owner.GetEdges(node.FindSlot<MaterialSlot>(slot).slotReference))
                {
                    var outputNode = ((Edge)edge).outputSlot.node;
                    if (outputNode != null)
                    {
                        nodeList.Add(outputNode);
                    }
                }
            }
            return nodeList;
        }

        private static bool ActiveLeafExists(AbstractMaterialNode node)
        {
            //if our active state has been explicitly set to a value use it
            switch (node.activeState)
            {
                case AbstractMaterialNode.ActiveState.Implicit:
                    break;
                case AbstractMaterialNode.ActiveState.ExplicitInactive:
                    return false;
                case AbstractMaterialNode.ActiveState.ExplicitActive:
                    return true;
            }


            List<AbstractMaterialNode> parentNodes = GetParentNodes(node);
            //at this point we know we are not explicitly set to a state,
            //so there is no reason to be inactive
            if (parentNodes.Count == 0)
            {
                return true;
            }

            bool output = false;
            foreach (var parent in parentNodes)
            {
                output |= ActiveLeafExists(parent);
                if (output)
                {
                    break;
                }
            }
            return output;
        }

        private static List<AbstractMaterialNode> GetChildNodes(AbstractMaterialNode node)
        {
            List<AbstractMaterialNode> nodeList = new List<AbstractMaterialNode>();
            var ids = node.GetOutputSlots<MaterialSlot>().Select(x => x.id);
            foreach (var slot in ids)
            {
                foreach (var edge in node.owner.GetEdges(node.FindSlot<MaterialSlot>(slot).slotReference))
                {
                    var inputNode = ((Edge)edge).inputSlot.node;
                    if (inputNode != null)
                    {
                        nodeList.Add(inputNode);
                    }
                }
            }
            return nodeList;
        }

        private static bool ActiveRootExists(AbstractMaterialNode node)
        {
            //if our active state has been explicitly set to a value use it
            switch (node.activeState)
            {
                case AbstractMaterialNode.ActiveState.Implicit:
                    break;
                case AbstractMaterialNode.ActiveState.ExplicitInactive:
                    return false;
                case AbstractMaterialNode.ActiveState.ExplicitActive:
                    return true;
            }

            List<AbstractMaterialNode> childNodes = GetChildNodes(node);
            //at this point we know we are not explicitly set to a state,
            //so there is no reason to be inactive
            if (childNodes.Count == 0)
            {
                return true;
            }

            bool output = false;
            foreach (var child in childNodes)
            {
                output |= ActiveRootExists(child);
                if (output)
                {
                    break;
                }
            }
            return output;
        }

        private static void ActiveTreeExists(AbstractMaterialNode node, out bool activeLeaf, out bool activeRoot, out bool activeTree)
        {
            activeLeaf = ActiveLeafExists(node);
            activeRoot = ActiveRootExists(node);
            activeTree = activeRoot && activeLeaf;
        }

        //First pass check if node is now active after a change, so just check if there is a valid "tree" : a valid upstream input path,
        // and a valid downstream output path, or "leaf" and "root". If this changes the node's active state, then anything connected may
        // change as well, so update the "forrest" or all connectected trees of this nodes leaves.
        // NOTE: I cannot think if there is any case where the entirety of the connected graph would need to change, but if there are bugs
        // on certain nodes farther away from the node not updating correctly, a possible solution may be to get the entirety of the connected
        // graph instead of just what I have declared as the "local" connected graph
        public static void ReevaluateActivityOfConnectedNodes(AbstractMaterialNode node, PooledHashSet<AbstractMaterialNode> changedNodes = null)
        {
            var forest = GetForest(node);
            ReevaluateActivityOfNodeList(forest, changedNodes);
        }

        public static void ReevaluateActivityOfNodeList(IEnumerable<AbstractMaterialNode> nodes, PooledHashSet<AbstractMaterialNode> changedNodes = null)
        {
            bool getChangedNodes = changedNodes != null;
            foreach (AbstractMaterialNode n in nodes)
            {
                if (n.activeState != AbstractMaterialNode.ActiveState.Implicit)
                    continue;
                ActiveTreeExists(n, out _, out _, out bool at);
                if (n.isActive != at && getChangedNodes)
                {
                    changedNodes.Add(n);
                }
                n.SetActive(at, false);
            }
        }

        //Go to the leaves of the node, then get all trees with those leaves
        private static HashSet<AbstractMaterialNode> GetForest(AbstractMaterialNode node)
        {
            var initial = new HashSet<AbstractMaterialNode> { node };

            var upstream = new HashSet<AbstractMaterialNode>();
            PreviewManager.PropagateNodes(initial, PreviewManager.PropagationDirection.Upstream, upstream);

            var forest = new HashSet<AbstractMaterialNode>();
            PreviewManager.PropagateNodes(upstream, PreviewManager.PropagationDirection.Downstream, forest);

            return forest;
        }

        public static void GetDownsteamNodesForNode(List<AbstractMaterialNode> nodeList, AbstractMaterialNode node)
        {
            // no where to start
            if (node == null)
                return;

            // Recursively traverse downstream from the original node
            // Traverse down each edge and continue on any connected downstream nodes
            // Only nodes with no nodes further downstream are added to node list
            bool hasDownstream = false;
            var ids = node.GetOutputSlots<MaterialSlot>().Select(x => x.id);
            foreach (var slot in ids)
            {
                foreach (var edge in node.owner.GetEdges(node.FindSlot<MaterialSlot>(slot).slotReference))
                {
                    var inputNode = ((Edge)edge).inputSlot.node;
                    if (inputNode != null)
                    {
                        hasDownstream = true;
                        GetDownsteamNodesForNode(nodeList, inputNode);
                    }
                }
            }

            // No more nodes downstream from here
            if (!hasDownstream)
                nodeList.Add(node);
        }

        public static void CollectNodeSet(HashSet<AbstractMaterialNode> nodeSet, MaterialSlot slot)
        {
            var node = slot.owner;
            var graph = node.owner;
            foreach (var edge in graph.GetEdges(node.GetSlotReference(slot.id)))
            {
                var outputNode = edge.outputSlot.node;
                if (outputNode != null)
                {
                    CollectNodeSet(nodeSet, outputNode);
                }
            }
        }

        public static void CollectNodeSet(HashSet<AbstractMaterialNode> nodeSet, AbstractMaterialNode node)
        {
            if (!nodeSet.Add(node))
            {
                return;
            }

            using (ListPool<MaterialSlot>.Get(out var slots))
            {
                node.GetInputSlots(slots);
                foreach (var slot in slots)
                {
                    CollectNodeSet(nodeSet, slot);
                }
            }
        }

        public static void CollectNodesNodeFeedsInto(List<AbstractMaterialNode> nodeList, AbstractMaterialNode node, IncludeSelf includeSelf = IncludeSelf.Include)
        {
            if (node == null)
                return;

            if (nodeList.Contains(node))
                return;

            foreach (var slot in node.GetOutputSlots<MaterialSlot>())
            {
                foreach (var edge in node.owner.GetEdges(slot.slotReference))
                {
                    var inputNode = edge.inputSlot.node;
                    CollectNodesNodeFeedsInto(nodeList, inputNode);
                }
            }
            if (includeSelf == IncludeSelf.Include)
                nodeList.Add(node);
        }

        public static string GetDocumentationString(string pageName)
        {
            return Documentation.GetPageLink(pageName.Replace(" ", "-") + NodeDocSuffix);
        }

        static Stack<MaterialSlot> s_SlotStack = new Stack<MaterialSlot>();

        public static ShaderStage GetEffectiveShaderStage(MaterialSlot initialSlot, bool goingBackwards)
        {
            var graph = initialSlot.owner.owner;
            s_SlotStack.Clear();
            s_SlotStack.Push(initialSlot);
            while (s_SlotStack.Any())
            {
                var slot = s_SlotStack.Pop();
                ShaderStage stage;
                if (slot.stageCapability.TryGetShaderStage(out stage))
                    return stage;

                if (goingBackwards && slot.isInputSlot)
                {
                    foreach (var edge in graph.GetEdges(slot.slotReference))
                    {
                        var node = edge.outputSlot.node;
                        s_SlotStack.Push(node.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId));
                    }
                }
                else if (!goingBackwards && slot.isOutputSlot)
                {
                    foreach (var edge in graph.GetEdges(slot.slotReference))
                    {
                        var node = edge.inputSlot.node;
                        s_SlotStack.Push(node.FindInputSlot<MaterialSlot>(edge.inputSlot.slotId));
                    }
                }
                else
                {
                    var ownerSlots = Enumerable.Empty<MaterialSlot>();
                    if (goingBackwards && slot.isOutputSlot)
                        ownerSlots = slot.owner.GetInputSlots<MaterialSlot>(slot);
                    else if (!goingBackwards && slot.isInputSlot)
                        ownerSlots = slot.owner.GetOutputSlots<MaterialSlot>(slot);
                    foreach (var ownerSlot in ownerSlots)
                        s_SlotStack.Push(ownerSlot);
                }
            }
            // We default to fragment shader stage if all connected nodes were compatible with both.
            return ShaderStage.Fragment;
        }

        public static ShaderStageCapability GetEffectiveShaderStageCapability(MaterialSlot initialSlot, bool goingBackwards)
        {
            var graph = initialSlot.owner.owner;
            s_SlotStack.Clear();
            s_SlotStack.Push(initialSlot);
            ShaderStageCapability capabilities = ShaderStageCapability.All;
            while (s_SlotStack.Any())
            {
                var slot = s_SlotStack.Pop();

                // Clear any stages from the total capabilities that this slot doesn't support (e.g. if this is vertex, clear pixel)
                capabilities &= slot.stageCapability;
                // Can early out if we know nothing is compatible, otherwise we have to keep checking everything we can reach.
                if (capabilities == ShaderStageCapability.None)
                    return capabilities;

                if (goingBackwards && slot.isInputSlot)
                {
                    foreach (var edge in graph.GetEdges(slot.slotReference))
                    {
                        var node = edge.outputSlot.node;
                        s_SlotStack.Push(node.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId));
                    }
                }
                else if (!goingBackwards && slot.isOutputSlot)
                {
                    foreach (var edge in graph.GetEdges(slot.slotReference))
                    {
                        var node = edge.inputSlot.node;
                        s_SlotStack.Push(node.FindInputSlot<MaterialSlot>(edge.inputSlot.slotId));
                    }
                }
                else
                {
                    var ownerSlots = Enumerable.Empty<MaterialSlot>();
                    if (goingBackwards && slot.isOutputSlot)
                        ownerSlots = slot.owner.GetInputSlots<MaterialSlot>(slot);
                    else if (!goingBackwards && slot.isInputSlot)
                        ownerSlots = slot.owner.GetOutputSlots<MaterialSlot>(slot);
                    foreach (var ownerSlot in ownerSlots)
                        s_SlotStack.Push(ownerSlot);
                }
            }

            return capabilities;
        }

        public static string GetSlotDimension(ConcreteSlotValueType slotValue)
        {
            switch (slotValue)
            {
                case ConcreteSlotValueType.Vector1:
                    return String.Empty;
                case ConcreteSlotValueType.Vector2:
                    return "2";
                case ConcreteSlotValueType.Vector3:
                    return "3";
                case ConcreteSlotValueType.Vector4:
                    return "4";
                case ConcreteSlotValueType.Matrix2:
                    return "2x2";
                case ConcreteSlotValueType.Matrix3:
                    return "3x3";
                case ConcreteSlotValueType.Matrix4:
                    return "4x4";
                case ConcreteSlotValueType.PropertyConnectionState:
                    return String.Empty;
                default:
                    return "Error";
            }
        }

        // NOTE: there are several bugs here.. we should use ConvertToValidHLSLIdentifier() instead
        public static string GetHLSLSafeName(string input)
        {
            char[] arr = input.ToCharArray();
            arr = Array.FindAll<char>(arr, (c => (Char.IsLetterOrDigit(c))));
            var safeName = new string(arr);
            if (safeName.Length > 1 && char.IsDigit(safeName[0]))
            {
                safeName = $"var{safeName}";
            }
            return safeName;
        }

        static readonly string[] k_HLSLNumericKeywords =
        {
            "float",
            "half",     // not technically in HLSL spec, but prob should be
            "real",     // Unity thing, but included here
            "int",
            "uint",
            "bool",
            "min10float",
            "min16float",
            "min12int",
            "min16int",
            "min16uint"
        };

        static readonly string[] k_HLSLNumericKeywordSuffixes =
        {
            "",
            "1", "2", "3", "4",
            "1x1", "1x2", "1x3", "1x4",
            "2x1", "2x2", "2x3", "2x4",
            "3x1", "3x2", "3x3", "3x4",
            "4x1", "4x2", "4x3", "4x4"
        };

        static HashSet<string> m_ShaderLabKeywords = new HashSet<string>()
        {
            // these should all be lowercase, as shaderlab keywords are case insensitive
            "properties",
            "range",
            "bind",
            "bindchannels",
            "tags",
            "lod",
            "shader",
            "subshader",
            "category",
            "fallback",
            "dependency",
            "customeditor",
            "rect",
            "any",
            "float",
            "color",
            "int",
            "integer",
            "vector",
            "matrix",
            "2d",
            "cube",
            "3d",
            "2darray",
            "cubearray",
            "name",
            "settexture",
            "true",
            "false",
            "on",
            "off",
            "separatespecular",
            "offset",
            "zwrite",
            "zclip",
            "conservative",
            "ztest",
            "alphatest",
            "fog",
            "stencil",
            "colormask",
            "alphatomask",
            "cull",
            "front",
            "material",
            "ambient",
            "diffuse",
            "specular",
            "emission",
            "shininess",
            "blend",
            "blendop",
            "colormaterial",
            "lighting",
            "pass",
            "grabpass",
            "usepass",
            "gpuprogramid",
            "add",
            "sub",
            "revsub",
            "min",
            "max",
            "logicalclear",
            "logicalset",
            "logicalcopy",
            "logicalcopyinverted",
            "logicalnoop",
            "logicalinvert",
            "logicaland",
            "logicalnand",
            "logicalor",
            "logicalnor",
            "logicalxor",
            "logicalequiv",
            "logicalandreverse",
            "logicalandinverted",
            "logicalorreverse",
            "logicalorinverted",
            "multiply",
            "screen",
            "overlay",
            "darken",
            "lighten",
            "colordodge",
            "colorburn",
            "hardlight",
            "softlight",
            "difference",
            "exclusion",
            "hslhue",
            "hslsaturation",
            "hslcolor",
            "hslluminosity",
            "zero",
            "one",
            "dstcolor",
            "srccolor",
            "oneminusdstcolor",
            "srcalpha",
            "oneminussrccolor",
            "dstalpha",
            "oneminusdstalpha",
            "srcalphasaturate",
            "oneminussrcalpha",
            "constantcolor",
            "oneminusconstantcolor",
            "constantalpha",
            "oneminusconstantalpha",
        };

        static HashSet<string> m_HLSLKeywords = new HashSet<string>()
        {
            "AppendStructuredBuffer",
            "asm",
            "asm_fragment",
            "auto",
            "BlendState",
            "break",
            "Buffer",
            "ByteAddressBuffer",
            "case",
            "catch",
            "cbuffer",
            "centroid",
            "char",
            "class",
            "column_major",
            "compile",
            "compile_fragment",
            "CompileShader",
            "const",
            "const_cast",
            "continue",
            "ComputeShader",
            "ConsumeStructuredBuffer",
            "default",
            "delete",
            "DepthStencilState",
            "DepthStencilView",
            "discard",
            "do",
            "double",
            "DomainShader",
            "dynamic_cast",
            "dword",
            "else",
            "enum",
            "explicit",
            "export",
            "extern",
            "false",
            "for",
            "friend",
            "fxgroup",
            "GeometryShader",
            "goto",
            "groupshared",
            "half",
            "Hullshader",
            "if",
            "in",
            "inline",
            "inout",
            "InputPatch",
            "interface",
            "line",
            "lineadj",
            "linear",
            "LineStream",
            "long",
            "matrix",
            "mutable",
            "namespace",
            "new",
            "nointerpolation",
            "noperspective",
            "NULL",
            "operator",
            "out",
            "OutputPatch",
            "packoffset",
            "pass",
            "pixelfragment",
            "PixelShader",
            "point",
            "PointStream",
            "precise",
            "private",
            "protected",
            "public",
            "RasterizerState",
            "reinterpret_cast",
            "RenderTargetView",
            "return",
            "register",
            "row_major",
            "RWBuffer",
            "RWByteAddressBuffer",
            "RWStructuredBuffer",
            "RWTexture1D",
            "RWTexture1DArray",
            "RWTexture2D",
            "RWTexture2DArray",
            "RWTexture3D",
            "sample",
            "sampler",
            "SamplerState",
            "SamplerComparisonState",
            "shared",
            "short",
            "signed",
            "sizeof",
            "snorm",
            "stateblock",
            "stateblock_state",
            "static",
            "static_cast",
            "string",
            "struct",
            "switch",
            "StructuredBuffer",
            "tbuffer",
            "technique",
            "technique10",
            "technique11",
            "template",
            "texture",
            "Texture1D",
            "Texture1DArray",
            "Texture2D",
            "Texture2DArray",
            "Texture2DMS",
            "Texture2DMSArray",
            "Texture3D",
            "TextureCube",
            "TextureCubeArray",
            "this",
            "throw",
            "true",
            "try",
            "typedef",
            "typename",
            "triangle",
            "triangleadj",
            "TriangleStream",
            "uniform",
            "unorm",
            "union",
            "unsigned",
            "using",
            "vector",
            "vertexfragment",
            "VertexShader",
            "virtual",
            "void",
            "volatile",
            "while"
        };

        static HashSet<string> m_ShaderGraphKeywords = new HashSet<string>()
        {
            "Gradient",
            "UnitySamplerState",
            "UnityTexture2D",
            "UnityTexture2DArray",
            "UnityTexture3D",
            "UnityTextureCube"
        };

        static bool m_HLSLKeywordDictionaryBuilt = false;

        public static bool IsHLSLKeyword(string id)
        {
            if (!m_HLSLKeywordDictionaryBuilt)
            {
                foreach (var numericKeyword in k_HLSLNumericKeywords)
                    foreach (var suffix in k_HLSLNumericKeywordSuffixes)
                        m_HLSLKeywords.Add(numericKeyword + suffix);

                m_HLSLKeywordDictionaryBuilt = true;
            }

            bool isHLSLKeyword = m_HLSLKeywords.Contains(id);

            return isHLSLKeyword;
        }

        public static bool IsShaderLabKeyWord(string id)
        {
            bool isShaderLabKeyword = m_ShaderLabKeywords.Contains(id.ToLower());
            return isShaderLabKeyword;
        }

        public static bool IsShaderGraphKeyWord(string id)
        {
            bool isShaderGraphKeyword = m_ShaderGraphKeywords.Contains(id);
            return isShaderGraphKeyword;
        }

        public static string ConvertToValidHLSLIdentifier(string originalId, Func<string, bool> isDisallowedIdentifier = null)
        {
            // Converts "  1   var  * q-30 ( 0 ) (1)   " to "_1_var_q_30_0_1"
            if (originalId == null)
                originalId = "";

            var result = Regex.Replace(originalId, @"^[^A-Za-z0-9_]+|[^A-Za-z0-9_]+$", ""); // trim leading/trailing bad characters (excl '_').
            result = Regex.Replace(result, @"[^A-Za-z0-9]+", "_"); // replace sequences of bad characters with underscores (incl '_').

            if (result.Length == 0 || Char.IsDigit(result[0]) || IsHLSLKeyword(result) || (isDisallowedIdentifier?.Invoke(result) ?? false))
                result = "_" + result;

            return result;
        }

        private static string GetDisplaySafeName(string input)
        {
            //strip valid display characters from slot name
            //current valid characters are whitespace and ( ) _ separators
            StringBuilder cleanName = new StringBuilder();
            foreach (var c in input)
            {
                if (c != ' ' && c != '(' && c != ')' && c != '_')
                    cleanName.Append(c);
            }

            return cleanName.ToString();
        }

        public static bool ValidateSlotName(string inName, out string errorMessage)
        {
            //check for invalid characters between display safe and hlsl safe name
            if (GetDisplaySafeName(inName) != GetHLSLSafeName(inName) && GetDisplaySafeName(inName) != ConvertToValidHLSLIdentifier(inName))
            {
                errorMessage = "Slot name(s) found invalid character(s). Valid characters: A-Z, a-z, 0-9, _ ( ) ";
                return true;
            }

            //if clean, return null and false
            errorMessage = null;
            return false;
        }

        public static string FloatToShaderValue(float value)
        {
            if (Single.IsPositiveInfinity(value))
            {
                return "1.#INF";
            }
            if (Single.IsNegativeInfinity(value))
            {
                return "-1.#INF";
            }
            if (Single.IsNaN(value))
            {
                return "NAN";
            }

            return value.ToString(CultureInfo.InvariantCulture);
        }

        // A number large enough to become Infinity (~FLOAT_MAX_VALUE * 10) + explanatory comment
        private const string k_ShaderLabInfinityAlternatrive = "3402823500000000000000000000000000000000 /* Infinity */";

        // ShaderLab doesn't support Scientific Notion nor Infinity. To stop from generating a broken shader we do this.
        public static string FloatToShaderValueShaderLabSafe(float value)
        {
            if (Single.IsPositiveInfinity(value))
            {
                return k_ShaderLabInfinityAlternatrive;
            }
            if (Single.IsNegativeInfinity(value))
            {
                return "-" + k_ShaderLabInfinityAlternatrive;
            }
            if (Single.IsNaN(value))
            {
                return "NAN"; // A real error has occured, in this case we should break the shader.
            }

            // For single point precision, reserve 54 spaces (e-45 min + ~9 digit precision). See floating-point-numeric-types (Microsoft docs).
            return value.ToString("0.######################################################", CultureInfo.InvariantCulture);
        }
    }
}
