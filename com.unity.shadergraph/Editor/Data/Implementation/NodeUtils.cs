using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Rendering.ShaderGraph;

namespace UnityEditor.Graphing
{
    class SlotConfigurationException : Exception
    {
        public SlotConfigurationException(string message)
            : base(message)
        {}
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
                    return DepthFirstCollectRedirectNodeFromNode( redirectNode );
                }
                return outputSlotRef;
            }
            // If no edges it is the first redirect node without an edge going into it and we should return the slot ref
            return node.GetSlotReference(RedirectNodeData.kInputSlotID);
        }

        public static void DepthFirstCollectNodesFromNode(List<AbstractMaterialNode> nodeList, AbstractMaterialNode node,
            IncludeSelf includeSelf = IncludeSelf.Include, List<KeyValuePair<ShaderKeyword, int>> keywordPermutation = null)
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
            if(node is KeywordNode keywordNode && keywordPermutation != null)
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
                        DepthFirstCollectNodesFromNode(nodeList, outputNode, keywordPermutation: keywordPermutation);
                }
            }

            if (includeSelf == IncludeSelf.Include && node.isActive)
                nodeList.Add(node);
        }

        private static List<AbstractMaterialNode> GetParentNodes(AbstractMaterialNode node)
        {
            List<AbstractMaterialNode> nodeList = new List<AbstractMaterialNode>();
            var ids = node.GetInputSlots<MaterialSlot>().Select(x => x.id);
            foreach (var slot in ids)
            {
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

        private static bool ValidLeafExists(AbstractMaterialNode node)
        {
            if(!node.isValid)
            {
                return false;
            }

            List<AbstractMaterialNode> parentNodes = GetParentNodes(node);
            if(parentNodes.Count == 0)
            {
                return true;
            }

            bool output = false;
            foreach(var parent in parentNodes)
            {
                output |= ValidLeafExists(parent);
                if(output)
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

        private static bool ValidRootExists(AbstractMaterialNode node)
        {
            if (!node.isValid)
            {
                return false;
            }

            List<AbstractMaterialNode> childNodes = GetChildNodes(node);
            if (childNodes.Count == 0)
            {
                return true;
            }

            bool output = false;
            foreach (var child in childNodes)
            {
                output |= ValidRootExists(child);
                if (output)
                {
                    break;
                }
            }
            return output;

        }

        private static void ValidTreeExists(AbstractMaterialNode node, out bool validLeaf, out bool validRoot, out bool validTree)
        {
            validLeaf = ValidLeafExists(node);
            validRoot = ValidRootExists(node);
            validTree = validLeaf;
        }

        //First pass check if node is now active after a change, so just check if there is a valid "tree" : a valid upstream input path,
        // and a valid downstream output path, or "leaf" and "root". If this changes the node's active state, then anything connected may
        // change as well, so update the "forrest" or all connectected trees of this nodes leaves.
        // NOTE: I cannot think if there is any case where the entirety of the connected graph would need to change, but if there are bugs
        // on certain nodes farther away from the node not updating correctly, a possible solution may be to get the entirety of the connected
        // graph instead of just what I have declared as the "local" connected graph
        public static void UpdateNodeActiveOnEdgeChange(AbstractMaterialNode node, PooledHashSet<AbstractMaterialNode> changedNodes = null)
        {
            bool originalyActive = node.isActive;
            ValidTreeExists(node, out bool validLeaf, out bool validRoot, out bool validTree);
            if ((validTree && !originalyActive) || (!validTree && originalyActive)) 
            {
                UpdateForrest(node, validLeaf, validRoot, validTree, changedNodes, changedNodes != null);
            }

        }

        private static void UpdateForrest(AbstractMaterialNode node, bool validLeaf, bool validRoot, bool validTree, PooledHashSet<AbstractMaterialNode> changedNodes, bool getChangedNodes)
        {
            if (getChangedNodes)
            {
                changedNodes.Add(node);
            }
            List<AbstractMaterialNode> forrest = GetForrest(node);
            foreach(AbstractMaterialNode n in forrest)
            {
                ValidTreeExists(n, out bool vl, out bool vr, out bool vt);
                if(n.isActive != vt && getChangedNodes)
                {
                    changedNodes.Add(n);
                }
                n.isActive = vt;
            }
        }

        //Go to the leaves of the node
        private static List<AbstractMaterialNode> GetForrest(AbstractMaterialNode node)
        {
            List<AbstractMaterialNode> leaves = GetLeaves(node);
            List<AbstractMaterialNode> forrest = new List<AbstractMaterialNode>();
            foreach(var leaf in leaves)
            {
                if(!forrest.Contains(leaf))
                {
                    forrest.Add(leaf);
                }
                foreach(var child in GetChildNodesRecursive(leaf))
                {
                    if(!forrest.Contains(child))
                    {
                        forrest.Add(child);
                    }
                }
            }
            return forrest;
        }

        private static List<AbstractMaterialNode> GetChildNodesRecursive(AbstractMaterialNode node)
        {
            List<AbstractMaterialNode> output = new List<AbstractMaterialNode>() { node };
            List<AbstractMaterialNode> children = GetChildNodes(node);
            foreach(var child in children)
            {
                if(!output.Contains(child))
                {
                    output.Add(child);
                }
                foreach(var descendent in GetChildNodesRecursive(child))
                {
                    if(!output.Contains(descendent))
                    {
                        output.Add(descendent);
                    }
                }
            }
            return output;
        }

        private static List<AbstractMaterialNode> GetLeaves(AbstractMaterialNode node)
        {
            List<AbstractMaterialNode> parents = GetParentNodes(node);
            List<AbstractMaterialNode> output = new List<AbstractMaterialNode>();
            if(parents.Count == 0)
            {
                output.Add(node);
            }
            else
            {
                foreach(var parent in parents)
                {
                    foreach(var leaf in GetLeaves(parent))
                    {
                        if(!output.Contains(leaf))
                        {
                            output.Add(leaf);
                        }
                    }
                }
            }
            return output;
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
            if(!hasDownstream)
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

            using (var slotsHandle = ListPool<MaterialSlot>.GetDisposable())
            {
                var slots = slotsHandle.value;
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
                        ownerSlots = slot.owner.GetInputSlots<MaterialSlot>();
                    else if (!goingBackwards && slot.isInputSlot)
                        ownerSlots = slot.owner.GetOutputSlots<MaterialSlot>();
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
            while (s_SlotStack.Any())
            {
                var slot = s_SlotStack.Pop();
                ShaderStage stage;
                if (slot.stageCapability.TryGetShaderStage(out stage))
                    return slot.stageCapability;

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
                        ownerSlots = slot.owner.GetInputSlots<MaterialSlot>();
                    else if (!goingBackwards && slot.isInputSlot)
                        ownerSlots = slot.owner.GetOutputSlots<MaterialSlot>();
                    foreach (var ownerSlot in ownerSlots)
                        s_SlotStack.Push(ownerSlot);
                }
            }

            return ShaderStageCapability.All;
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
                default:
                    return "Error";
            }
        }

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
            if (GetDisplaySafeName(inName) != GetHLSLSafeName(inName))
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
