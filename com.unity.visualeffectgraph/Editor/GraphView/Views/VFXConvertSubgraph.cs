using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEditor.Experimental.GraphView;

using NodeID = System.UInt32;

namespace UnityEditor.VFX.UI
{
    static class VFXConvertSubgraph
    {
        public static void ConvertToSubgraphContext(VFXView sourceView, IEnumerable<Controller> controllers,Rect rect)
        {
            var ctx = new Context();
            ctx.ConvertToSubgraphContext(sourceView, controllers, rect);
        }


        public static void ConvertToSubgraphOperator(VFXView sourceView, IEnumerable<Controller> controllers, Rect rect)
        {
            var ctx = new Context();
            ctx.ConvertToSubgraphOperator(sourceView, controllers, rect);
        }

        public static void ConvertToSubgraphBlock(VFXView sourceView, IEnumerable<Controller> controllers, Rect rect)
        {
            var ctx = new Context();
            ctx.ConvertToSubgraphBlock(sourceView, controllers, rect);
        }

        enum Type
        {
            Context,
            Operator,
            Block
        }

        static VisualEffectObject CreateUniquePath(VFXView sourceView, Type type)
        {
            string graphPath = AssetDatabase.GetAssetPath(sourceView.controller.model.asset);
            string graphName = Path.GetFileNameWithoutExtension(graphPath);
            string graphDirPath = Path.GetDirectoryName(graphPath);

            switch (type)
            {
                case Type.Operator:
                    {
                        string targetSubgraphPath = string.Format("{0}/{1}_SubgraphOperator.vfxoperator", graphDirPath, graphName);
                        int cpt = 1;
                        while (File.Exists(targetSubgraphPath))
                        {
                            targetSubgraphPath = string.Format("{0}/{1}_SubgraphOperator_{2}.vfxoperator", graphDirPath, graphName, cpt++);
                        }
                        return VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphOperator>(targetSubgraphPath);
                    }
                case Type.Context:
                    {
                        string targetSubgraphPath = string.Format("{0}/{1}_Subgraph.vfx", graphDirPath, graphName);
                        int cpt = 1;
                        while (File.Exists(targetSubgraphPath))
                        {
                            targetSubgraphPath = string.Format("{0}/{1}_Subgraph_{2}.vfx", graphDirPath, graphName, cpt++);
                        }
                        return VisualEffectAssetEditorUtility.CreateNewAsset(targetSubgraphPath);
                    }
                case Type.Block:
                    {
                        string targetSubgraphPath = string.Format("{0}/{1}_SubgraphBlock.vfxblock", graphDirPath, graphName);
                        int cpt = 1;
                        while (File.Exists(targetSubgraphPath))
                        {
                            targetSubgraphPath = string.Format("{0}/{1}_SubgraphBlock_{2}.vfxblock", graphDirPath, graphName, cpt++);
                        }
                        return VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphBlock>(targetSubgraphPath);
                    }
            }
            return null;
        }

        class Context
        {
            List<VFXParameterNodeController> parameterNodeControllers;

            VFXViewController m_SourceController;
            List<Controller> m_SourceControllers;
            VFXView m_SourceView;
            VFXModel m_SourceNode;
            IVFXSlotContainer m_SourceSlotContainer;
            VFXNodeController m_SourceNodeController;
            Dictionary<string, VFXParameterNodeController> m_SourceParameters;

            VFXViewController m_TargetController;
            List<VFXNodeController> m_TargetControllers;
            List<VFXParameterController> m_TargetParameters = new List<VFXParameterController>();
            VisualEffectObject m_TargetSubgraph;

            Rect m_Rect;


            void Init(VFXView sourceView, IEnumerable<Controller> controllers)
            {
                this.m_SourceView = sourceView;

                m_SourceControllers = controllers.Concat(sourceView.controller.dataEdges.Where(t => controllers.Contains(t.input.sourceNode) && controllers.Contains(t.output.sourceNode))).Distinct().ToList();
                parameterNodeControllers = m_SourceControllers.OfType<VFXParameterNodeController>().ToList();


                m_SourceController = sourceView.controller;
                VFXGraph sourceGraph = m_SourceController.graph;
                m_SourceController.useCount++;

                m_SourceParameters = new Dictionary<string, VFXParameterNodeController>();

                foreach (var parameterNode in parameterNodeControllers)
                {
                    m_SourceParameters[parameterNode.exposedName] = parameterNode;
                }

            }

            void Uninit()
            {
                foreach (var element in m_SourceControllers.Where(t => !(t is VFXDataEdgeController) && !(t is VFXParameterNodeController)))
                {
                    m_SourceController.RemoveElement(element);
                }

                foreach (var element in parameterNodeControllers)
                {
                    if (element.infos.linkedSlots == null || element.infos.linkedSlots.Count() == 0)
                        m_SourceController.RemoveElement(element);
                }

                m_TargetController.useCount--;
                m_SourceController.useCount--;
            }

            void UninitSmart()
            {
                var nodeNotToDelete = new HashSet<Controller>();

                foreach (var node in m_SourceControllers.OfType<VFXNodeController>().Where(t=>t.outputPorts.Count() > 0))
                {
                    if( nodeNotToDelete.Contains(node))
                        continue;

                    var oldBag = new HashSet<VFXNodeController>();
                    var newBag = new HashSet<VFXNodeController>();

                    oldBag.Add(node);

                    while(oldBag.Count > 0)
                    {
                        foreach( var n in oldBag)
                        {
                            if( n.outputPorts.SelectMany(t=>t.connections).Any(t=>nodeNotToDelete.Contains(t.input.sourceNode) || !m_SourceControllersWithBlocks.Contains(t.input.sourceNode)))
                            {
                                nodeNotToDelete.Add(n);
                                oldBag.Clear();
                                break;
                            }

                            foreach( var o in n.inputPorts.SelectMany(t=>t.connections).Select(t=>t.output))
                            {
                                newBag.Add(o.sourceNode);
                            }
                        }

                        oldBag.Clear();
                        var tmp = oldBag;
                        oldBag = newBag;
                        newBag = tmp;
                    }
                }

                foreach (var element in m_SourceControllers.Where(t => !(t is VFXDataEdgeController) && !(t is VFXParameterNodeController) && ! nodeNotToDelete.Contains(t)))
                {
                    m_SourceController.RemoveElement(element);
                }

                foreach (var element in parameterNodeControllers)
                {
                    if (element.infos.linkedSlots == null || element.infos.linkedSlots.Count() == 0)
                        m_SourceController.RemoveElement(element);
                }

                m_TargetController.useCount--;
                m_SourceController.useCount--;
            }


            void CopyPasteNodes()
            {
                object result = VFXCopy.Copy(m_SourceControllers, m_Rect);

                VFXPaste.Paste(m_TargetController, m_Rect.center, result, null, null, m_TargetControllers);
                List<VFXParameterController> targetParameters = new List<VFXParameterController>();

            }

            void SetupTargetParameters()
            {
                // Change each parameter created by copy paste ( and therefore a parameter copied ) to exposed
                foreach (var parameter in m_TargetController.parameterControllers)
                {
                    m_TargetParameters.Add(parameter);
                    parameter.exposed = true;
                }
            }

            public void ConvertToSubgraphContext(VFXView sourceView, IEnumerable<Controller> controllers, Rect rect)
            {
                this.m_Rect = rect;
                Init(sourceView, controllers);
                CreateUniqueSubgraph("Subgraph", VisualEffectResource.Extension,VisualEffectAssetEditorUtility.CreateNewAsset);
                CopyPasteNodes();
                m_SourceNode = ScriptableObject.CreateInstance<VFXSubgraphContext>();
                PostSetupNode();
                m_SourceControllersWithBlocks = m_SourceControllers.Concat(m_SourceControllers.OfType<VFXContextController>().SelectMany(t => t.blockControllers));
                TransferEdges();
                //TransferContextsFlowEdges();
                UninitSmart();
            }

            public void ConvertToSubgraphOperator(VFXView sourceView, IEnumerable<Controller> controllers, Rect rect)
            {
                this.m_Rect = rect;
                Init(sourceView, controllers);
                CreateUniqueSubgraph("SubgraphOperator", VisualEffectSubgraphOperator.Extension,VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphOperator>);
                CopyPasteNodes();
                m_SourceNode = ScriptableObject.CreateInstance<VFXSubgraphOperator>();
                PostSetupNode();
                m_SourceControllersWithBlocks = m_SourceControllers.Concat(m_SourceControllers.OfType<VFXContextController>().SelectMany(t => t.blockControllers));
                TransferEdges();
                TransfertOperatorOutputEdges();
                Uninit();
            }


            List<VFXBlockController> m_SourceBlockControllers;
            List<VFXBlockController> m_TargetBlocks = null;

            public void ConvertToSubgraphBlock(VFXView sourceView, IEnumerable<Controller> controllers, Rect rect)
            {
                this.m_Rect = rect;
                Init(sourceView, controllers);
                CreateUniqueSubgraph("SubgraphBlock", VisualEffectSubgraphBlock.Extension,VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphBlock>);

                m_SourceControllers.RemoveAll(t => t is VFXContextController); // Don't copy contexts
                CopyPasteNodes();

                m_SourceBlockControllers = m_SourceControllers.OfType<VFXBlockController>().OrderBy(t=>t.index).ToList();

                VFXContextController sourceContextController = m_SourceBlockControllers.First().contextController;

                object copyData = VFXCopy.CopyBlocks(m_SourceBlockControllers);

                var targetContext = m_TargetController.graph.children.OfType<VFXBlockSubgraphContext>().FirstOrDefault();
                if (targetContext == null)
                {
                    targetContext = ScriptableObject.CreateInstance<VFXBlockSubgraphContext>();
                    m_TargetController.graph.AddChild(targetContext);
                }
                m_TargetController.LightApplyChanges();
                targetContext.position = sourceContextController.position;
                targetContext.SetSettingValue("m_SuitableContexts", (VFXBlockSubgraphContext.ContextType)m_SourceBlockControllers.Select(t=>t.model.compatibleContexts).Aggregate((t,s)=> t & s) );
                m_TargetBlocks = new List<VFXBlockController>();

                VFXPaste.PasteBlocks(m_TargetController, copyData, targetContext, 0, m_TargetBlocks);

                var otherSourceControllers = m_SourceControllers.OfType<VFXNodeController>().Where(t => !(t is VFXBlockController)).ToList();

                //Create lost links between nodes and blocks
                foreach(var edge in m_SourceController.dataEdges.Where(t=> otherSourceControllers.Contains(t.output.sourceNode) && m_SourceBlockControllers.Contains(t.input.sourceNode)))
                {
                    var outputNode = m_TargetControllers[m_SourceControllers.IndexOf(edge.output.sourceNode)];
                    var output = outputNode.outputPorts.First(t => t.path == edge.output.path);

                    var inputBlock = m_TargetBlocks[m_SourceBlockControllers.IndexOf(edge.input.sourceNode as VFXBlockController)];
                    var input = inputBlock.inputPorts.First(t => t.path == edge.input.path);

                    m_TargetController.CreateLink(input, output);
                }

                var sourceBlock = ScriptableObject.CreateInstance<VFXSubgraphBlock>();
                m_SourceNode = sourceBlock;
                sourceContextController.model.AddChild(m_SourceNode,m_SourceBlockControllers.Select(t=>t.index).Min());
                sourceContextController.ApplyChanges();
                m_SourceNodeController = sourceContextController.blockControllers.First(t=> t.model == m_SourceNode );
                PostSetup();
                m_SourceNodeController.ApplyChanges();

                var targetContextController = m_TargetController.GetRootNodeController(targetContext, 0) as VFXContextController;

                m_SourceControllersWithBlocks = m_SourceControllers.Concat(m_SourceBlockControllers);
                TransferEdges();
                UninitSmart();
            }


            void CreateUniqueSubgraph(string typeName, string extension, Func<string,VisualEffectObject> createFunc)
            {
                string graphPath = AssetDatabase.GetAssetPath(m_SourceView.controller.model);
                string graphName;
                string graphDirPath;
                if ( string.IsNullOrEmpty(graphPath))
                {
                    graphName = m_SourceView.controller.model.name;
                    if (string.IsNullOrEmpty(graphName))
                        graphName = "New VFX";

                    graphDirPath = "Assets";
                }
                else
                {
                    graphName = Path.GetFileNameWithoutExtension(graphPath);
                    graphDirPath = Path.GetDirectoryName(graphPath).Replace('\\', '/');
                }
                

                string targetSubgraphPath = string.Format("{0}/{1}{2}{3}", graphDirPath, graphName, typeName, extension);
                int cpt = 1;

                while (File.Exists(targetSubgraphPath))
                {
                    targetSubgraphPath = string.Format("{0}/{1}_{3}_{2}{4}", graphDirPath, graphName, cpt++, typeName, extension);
                }
                m_TargetSubgraph = createFunc(targetSubgraphPath);

                m_TargetController = VFXViewController.GetController(m_TargetSubgraph.GetResource());
                m_TargetController.useCount++;
                m_TargetControllers = new List<VFXNodeController>();
            }

            void PostSetupNode()
            {
                PostSetup();
                m_SourceNode.position = m_Rect.center;
                m_SourceController.graph.AddChild(m_SourceNode);
                m_SourceController.LightApplyChanges();
                m_SourceNodeController = m_SourceController.GetRootNodeController(m_SourceNode, 0);
                m_SourceNodeController.ApplyChanges();
            }
            void PostSetup()
            {
                SetupTargetParameters();
                m_SourceNode.SetSettingValue("m_Subgraph", m_TargetSubgraph);
                m_SourceSlotContainer = m_SourceNode as IVFXSlotContainer;
            }

            void TransferEdges()
            {
                for (int i = 0; i < m_TargetParameters.Count; ++i)
                {
                    var input = m_SourceNodeController.inputPorts.First(t => t.model == m_SourceSlotContainer.inputSlots[i]);
                    var output = m_SourceParameters[m_TargetParameters[i].exposedName].outputPorts.First();

                    m_TargetController.CreateLink(input, output);
                }
                TransfertDataEdges();
            }

            IEnumerable<Controller> m_SourceControllersWithBlocks;

            void TransfertDataEdges()
            {
                m_SourceControllersWithBlocks = m_SourceControllers.Concat(m_SourceControllers.OfType<VFXContextController>().SelectMany(t => t.blockControllers));
                
                // Search for links between with inputs in the selected part and the output in other parts of the graph.
                Dictionary<VFXDataAnchorController, List<VFXDataAnchorController>> traversingInEdges = new Dictionary<VFXDataAnchorController, List<VFXDataAnchorController>>();

                foreach (var edge in m_SourceController.dataEdges.Where(
                    t =>
                    {
                        if (parameterNodeControllers.Contains(t.output.sourceNode))
                            return false;
                        var inputInControllers = m_SourceControllersWithBlocks.Contains(t.input.sourceNode);
                        var outputInControllers = m_SourceControllersWithBlocks.Contains(t.output.sourceNode);

                        return inputInControllers && !outputInControllers;
                    }
                    ))
                {
                    List<VFXDataAnchorController> outputs = null;
                    if (!traversingInEdges.TryGetValue(edge.input, out outputs))
                    {
                        outputs = new List<VFXDataAnchorController>();
                        traversingInEdges[edge.input] = outputs;
                    }

                    outputs.Add(edge.output);
                }

                var newSourceInputs = traversingInEdges.Keys.ToArray();

                for (int i = 0; i < newSourceInputs.Length; ++i)
                {
                    VFXParameter newTargetParameter = m_TargetController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(t => t.model.type == newSourceInputs[i].portType));

                    m_TargetController.LightApplyChanges();

                    VFXParameterController newTargetParamController = m_TargetController.GetParameterController(newTargetParameter);
                    newTargetParamController.exposed = true;

                    var outputs = traversingInEdges[newSourceInputs[i]];

                    var linkedParameter = outputs.FirstOrDefault(t => t.sourceNode is VFXParameterNodeController);
                    if (linkedParameter != null)
                        newTargetParamController.exposedName = (linkedParameter.sourceNode as VFXParameterNodeController).parentController.exposedName;
                    else
                        newTargetParamController.exposedName = newSourceInputs[i].name;

                    //first the equivalent of sourceInput in the target

                    VFXNodeController targetNode = null;
                    Vector2 position;

                    if (newSourceInputs[i].sourceNode is VFXBlockController)
                    {
                        var blockController = newSourceInputs[i].sourceNode as VFXBlockController;
                        if (m_TargetBlocks != null)
                        {
                            targetNode = m_TargetBlocks[m_SourceBlockControllers.IndexOf(blockController)];
                            position = blockController.contextController.position;
                        }
                        else
                        {

                            var targetContext = m_TargetControllers[m_SourceControllers.IndexOf(blockController.contextController)] as VFXContextController;

                            targetNode = targetContext.blockControllers[blockController.index];
                            position = blockController.contextController.position;
                        }
                    }
                    else
                    {
                        targetNode = m_TargetControllers[m_SourceControllers.IndexOf(newSourceInputs[i].sourceNode)];
                        position = targetNode.position;
                    }

                    VFXDataAnchorController targetAnchor = targetNode.inputPorts.First(t => t.path == newSourceInputs[i].path);


                    position.y += targetAnchor.model.owner.inputSlots.IndexOf(targetAnchor.model) * 32;

                    VFXNodeController parameterNode = m_TargetController.AddVFXParameter(position - new Vector2(200, 0), newTargetParamController, null);

                    // Link the parameternode and the input in the target
                    m_TargetController.CreateLink(targetAnchor, parameterNode.outputPorts[0]);

                    if (m_SourceSlotContainer is VFXOperator)
                        (m_SourceSlotContainer as VFXOperator).ResyncSlots(true);
                    else if (m_SourceSlotContainer is VFXSubgraphBlock)
                    {
                        VFXSubgraphBlock blk = (m_SourceSlotContainer as VFXSubgraphBlock);
                        blk.RecreateCopy();
                        blk.ResyncSlots(true);
                    }
                    else if (m_SourceSlotContainer is VFXSubgraphContext)
                    {
                        VFXSubgraphContext ctx = (m_SourceSlotContainer as VFXSubgraphContext);
                        ctx.RecreateCopy();
                        ctx.ResyncSlots(true);
                    }

                    m_SourceNodeController.ApplyChanges();
                    //Link all the outputs to the matching input of the subgraph
                    foreach (var output in outputs)
                    {
                        m_SourceController.CreateLink(m_SourceNodeController.inputPorts.First(t => t.model == m_SourceSlotContainer.inputSlots.Last()), output);
                    }

                }
            }


            void TransfertOperatorOutputEdges()
            {
                var traversingOutEdges = new Dictionary<VFXDataAnchorController, List<VFXDataAnchorController>>();

                foreach (var edge in m_SourceController.dataEdges.Where(
                    t =>
                    {
                        if (t.output.sourceNode is VFXParameterNodeController || t.input.sourceNode is VFXParameterNodeController)
                            return false;
                        var inputInControllers = m_SourceControllersWithBlocks.Contains(t.input.sourceNode);
                        var outputInControllers = m_SourceControllersWithBlocks.Contains(t.output.sourceNode);

                        return !inputInControllers && outputInControllers;
                    }
                    ))
                {
                    List<VFXDataAnchorController> inputs = null;
                    if (!traversingOutEdges.TryGetValue(edge.output, out inputs))
                    {
                        inputs = new List<VFXDataAnchorController>();
                        traversingOutEdges[edge.output] = inputs;
                    }

                    inputs.Add(edge.input);
                }

                var newSourceOutputs = traversingOutEdges.Keys.ToArray();

                for (int i = 0; i < newSourceOutputs.Length; ++i)
                {
                    VFXParameter newTargetParameter = m_TargetController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(t => t.model.type == newSourceOutputs[i].portType));

                    m_TargetController.LightApplyChanges();

                    VFXParameterController newTargetParamController = m_TargetController.GetParameterController(newTargetParameter);
                    newTargetParamController.isOutput = true;

                    var inputs = traversingOutEdges[newSourceOutputs[i]];

                    var linkedParameter = inputs.FirstOrDefault(t => t.sourceNode is VFXParameterNodeController);
                    if (linkedParameter != null)
                        newTargetParamController.exposedName = (linkedParameter.sourceNode as VFXParameterNodeController).parentController.exposedName;
                    else
                        newTargetParamController.exposedName = newSourceOutputs[i].name;

                    //first the equivalent of sourceInput in the target

                    VFXNodeController targetNode = null;

                    if (newSourceOutputs[i].sourceNode is VFXBlockController)
                    {
                        var blockController = newSourceOutputs[i].sourceNode as VFXBlockController;
                        if (m_TargetBlocks != null)
                        {
                            targetNode = m_TargetBlocks[m_SourceBlockControllers.IndexOf(blockController)];
                        }
                        else
                        { 

                            var targetContext = m_TargetControllers[m_SourceControllers.IndexOf(blockController.contextController)] as VFXContextController;

                            targetNode = targetContext.blockControllers[blockController.index];
                        }
                    }
                    else
                    {
                        targetNode = m_TargetControllers[m_SourceControllers.IndexOf(newSourceOutputs[i].sourceNode)];
                    }

                    VFXDataAnchorController targetAnchor = targetNode.outputPorts.First(t => t.path == newSourceOutputs[i].path);

                    VFXNodeController parameterNode = m_TargetController.AddVFXParameter(targetNode.position + new Vector2(400, 0), newTargetParamController, null);

                    // Link the parameternode and the input in the target
                    m_TargetController.CreateLink(parameterNode.inputPorts[0], targetAnchor);

                    if (m_SourceSlotContainer is VFXOperator)
                        (m_SourceSlotContainer as VFXOperator).ResyncSlots(true);
                    m_SourceNodeController.ApplyChanges();
                    //Link all the outputs to the matching input of the subgraph
                    foreach (var input in inputs)
                    {
                        m_SourceController.CreateLink(input, m_SourceNodeController.outputPorts.First(t => t.model == m_SourceSlotContainer.outputSlots.Last()));
                    }
                }
            }
            void TransferContextsFlowEdges()
            {
                var initializeContexts = m_SourceControllers.OfType<VFXContextController>().Where(t => t.model.contextType == VFXContextType.Init ||
                                                                                                    t.model.contextType == VFXContextType.Spawner ||
                                                                                                    t.model.contextType == VFXContextType.Subgraph).ToArray();

                var outputSpawners = new Dictionary<VFXContextController, List<VFXFlowAnchorController>>();
                var outputEvents = new Dictionary<string, List<VFXFlowAnchorController>>();

                foreach (var initializeContext in initializeContexts)
                {
                    for (int i = 0; i < initializeContext.flowInputAnchors.Count; ++i)
                        if (initializeContext.flowInputAnchors[i].connections.Count() > 0)
                        {

                            var outputContext = initializeContext.flowInputAnchors[i].connections.First().output.context; //output context must be linked through is it is linked with a spawner

                            if (!m_SourceControllers.Contains(outputContext))
                            {
                                if (outputContext.model.contextType == VFXContextType.Spawner /*||
                            ((outputContext.model is VFXBasicEvent) &&
                                (new string[] { VisualEffectAsset.PlayEventName, VisualEffectAsset.StopEventName }.Contains((outputContext.model as VFXBasicEvent).eventName) ||
                                    sourceController.model.isSubgraph && (outputContext.model as VFXBasicEvent).eventName == VFXSubgraphContext.triggerEventName))*/)
                                {
                                    List<VFXFlowAnchorController> inputs = null;
                                    if (!outputSpawners.TryGetValue(outputContext, out inputs))
                                    {
                                        inputs = new List<VFXFlowAnchorController>();
                                        outputSpawners.Add(outputContext, inputs);
                                    }
                                    inputs.Add(initializeContext.flowInputAnchors[i]);
                                }
                                else if (outputContext.model is VFXBasicEvent)
                                {
                                    List<VFXFlowAnchorController> inputs = null;
                                    var eventName = (outputContext.model as VFXBasicEvent).eventName;
                                    if (!outputEvents.TryGetValue(eventName, out inputs))
                                    {
                                        inputs = new List<VFXFlowAnchorController>();
                                        outputEvents.Add(eventName, inputs);
                                    }
                                    inputs.Add(initializeContext.flowInputAnchors[i]);
                                }
                            }
                        }
                }

                {

                    if (outputSpawners.Count() > 1)
                    {
                        Debug.LogWarning("More than one spawner is linked to the content if the new subgraph, some links we not be kept");
                    }

                    if (outputSpawners.Count > 0)
                    {
                        var kvContext = outputSpawners.First();

                        (m_SourceNodeController as VFXContextController).model.LinkFrom(kvContext.Key.model, 0, 2); // linking to trigger
                        CreateAndLinkEvent(m_SourceControllers, m_TargetController, m_TargetControllers, kvContext.Value, VFXSubgraphContext.triggerEventName);
                    }
                }
                { //link named events as if
                    foreach (var kv in outputEvents)
                    {
                        CreateAndLinkEvent(m_SourceControllers, m_TargetController, m_TargetControllers, kv.Value, kv.Key);
                    }
                }
            }
        }

        private static void CreateAndLinkEvent(List<Controller> sourceControllers, VFXViewController targetController, List<VFXNodeController> targetControllers, List<VFXFlowAnchorController> inputs, string eventName)
        {
            var triggerEvent = VFXBasicEvent.CreateInstance<VFXBasicEvent>();
            triggerEvent.eventName = eventName;

            targetController.graph.AddChild(triggerEvent);

            float xMiddle = 0;
            float yMin = Mathf.Infinity;

            foreach (var edge in inputs)
            {
                var targetContext = targetControllers[sourceControllers.IndexOf(edge.context)] as VFXContextController;

                var targetInputLink = edge.slotIndex;

                triggerEvent.LinkTo(targetContext.model, 0, targetInputLink);
                xMiddle += targetContext.position.x;

                if (targetContext.position.y < yMin)
                    yMin = targetContext.position.y;
            }

            triggerEvent.position = new Vector2(xMiddle / inputs.Count, yMin) - new Vector2(0, 200); // place the event above the top center of the linked contexts.
        }
    }
}
