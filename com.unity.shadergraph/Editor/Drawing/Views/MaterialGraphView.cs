using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.Graphing;
using Object = UnityEngine.Object;
using UnityEditor.Graphs;

using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Colors;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using Edge = UnityEditor.Experimental.GraphView.Edge;
using Node = UnityEditor.Experimental.GraphView.Node;

namespace UnityEditor.ShaderGraph.Drawing
{
    sealed class MaterialGraphView : GraphView
    {
        public MaterialGraphView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/MaterialGraphView"));
            serializeGraphElements = SerializeGraphElementsImplementation;
            canPasteSerializedData = CanPasteSerializedDataImplementation;
            unserializeAndPaste = UnserializeAndPasteImplementation;
            deleteSelection = DeleteSelectionImplementation;
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
        }

        protected override bool canCopySelection
        {
            get { return selection.OfType<IShaderNodeView>().Any(x => x.node.canCopyNode) || selection.OfType<Group>().Any() || selection.OfType<BlackboardField>().Any(); }
        }

        public MaterialGraphView(GraphData graph) : this()
        {
            this.graph = graph;
        }

        public GraphData graph { get; private set; }
        public Action onConvertToSubgraphClick { get; set; }

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            var compatibleAnchors = new List<Port>();
            var startSlot = startAnchor.GetSlot();
            if (startSlot == null)
                return compatibleAnchors;

            var startStage = startSlot.stageCapability;
            if (startStage == ShaderStageCapability.All)
                startStage = NodeUtils.GetEffectiveShaderStageCapability(startSlot, true) & NodeUtils.GetEffectiveShaderStageCapability(startSlot, false);

            foreach (var candidateAnchor in ports.ToList())
            {
                var candidateSlot = candidateAnchor.GetSlot();
                if (!startSlot.IsCompatibleWith(candidateSlot))
                    continue;

                if (startStage != ShaderStageCapability.All)
                {
                    var candidateStage = candidateSlot.stageCapability;
                    if (candidateStage == ShaderStageCapability.All)
                        candidateStage = NodeUtils.GetEffectiveShaderStageCapability(candidateSlot, true)
                            & NodeUtils.GetEffectiveShaderStageCapability(candidateSlot, false);
                    if (candidateStage != ShaderStageCapability.All && candidateStage != startStage)
                        continue;
                }

                compatibleAnchors.Add(candidateAnchor);
            }
            return compatibleAnchors;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            Vector2 mousePosition = evt.mousePosition;
            base.BuildContextualMenu(evt);
            if(evt.target is GraphView)
            {
                evt.menu.InsertAction(1, "Create Sticky Note", (e) => { AddStickyNote(mousePosition); });

                foreach (AbstractMaterialNode node in graph.GetNodes<AbstractMaterialNode>())
                {
                    if (node.hasPreview && node.previewExpanded == true)
                        evt.menu.InsertAction(2, "Collapse All Previews", CollapsePreviews, (a) => DropdownMenuAction.Status.Normal);
                    if (node.hasPreview && node.previewExpanded == false)
                        evt.menu.InsertAction(2, "Expand All Previews", ExpandPreviews, (a) => DropdownMenuAction.Status.Normal);
                }
                evt.menu.AppendSeparator();
            }

            if (evt.target is GraphView || evt.target is Node)
            {
                if (evt.target is Node node)
                {
                    if (!selection.Contains(node))
                    {
                        selection.Clear();
                        selection.Add(node);
                    }
                }

                InitializeViewSubMenu(evt);
                InitializePrecisionSubMenu(evt);

                evt.menu.AppendAction("Convert To/Sub-graph", ConvertToSubgraph, ConvertToSubgraphStatus);
                evt.menu.AppendAction("Convert To/Inline Node", ConvertToInlineNode, ConvertToInlineNodeStatus);
                evt.menu.AppendAction("Convert To/Property", ConvertToProperty, ConvertToPropertyStatus);
                evt.menu.AppendSeparator();

                var editorView = GetFirstAncestorOfType<GraphEditorView>();
                if (editorView.colorManager.activeSupportsCustom && selection.OfType<MaterialNodeView>().Any())
                {
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Color/Change...", ChangeCustomNodeColor,
                        eventBase => DropdownMenuAction.Status.Normal);

                    evt.menu.AppendAction("Color/Reset", menuAction =>
                    {
                        graph.owner.RegisterCompleteObjectUndo("Reset Node Color");
                        foreach (var selectable in selection)
                        {
                            if (selectable is MaterialNodeView nodeView)
                            {
                                nodeView.node.ResetColor(editorView.colorManager.activeProviderName);
                                editorView.colorManager.UpdateNodeView(nodeView);
                            }
                        }
                    }, eventBase => DropdownMenuAction.Status.Normal);
                }

                if (selection.OfType<IShaderNodeView>().Count() == 1)
                {
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Open Documentation _F1", SeeDocumentation, SeeDocumentationStatus);
                }
                if (selection.OfType<IShaderNodeView>().Count() == 1 && selection.OfType<IShaderNodeView>().First().node is SubGraphNode)
                {
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Open Sub Graph", OpenSubGraph, (a) => DropdownMenuAction.Status.Normal);
                }
            }
            evt.menu.AppendSeparator();
            // This needs to work on nodes, groups and properties
            if ((evt.target is Node) || (evt.target is StickyNote))
            {
                evt.menu.AppendAction("Group Selection %g", _ => GroupSelection(), (a) =>
                {
                    List<ISelectable> filteredSelection = new List<ISelectable>();

                    foreach (ISelectable selectedObject in selection)
                    {
                        if (selectedObject is Group)
                            return DropdownMenuAction.Status.Disabled;
                        GraphElement ge = selectedObject as GraphElement;
                        if (ge.userData is IGroupItem)
                        {
                            filteredSelection.Add(ge);
                        }
                    }

                    if (filteredSelection.Count > 0)
                        return DropdownMenuAction.Status.Normal;

                    return DropdownMenuAction.Status.Disabled;
                });

                evt.menu.AppendAction("Ungroup Selection %u", _ => RemoveFromGroupNode(), (a) =>
                {
                    List<ISelectable> filteredSelection = new List<ISelectable>();

                    foreach (ISelectable selectedObject in selection)
                    {
                        if (selectedObject is Group)
                            return DropdownMenuAction.Status.Disabled;
                        GraphElement ge = selectedObject as GraphElement;
                        if (ge.userData is IGroupItem)
                        {
                            if (ge.GetContainingScope() is Group)
                                filteredSelection.Add(ge);
                        }
                    }

                    if (filteredSelection.Count > 0)
                        return DropdownMenuAction.Status.Normal;

                    return DropdownMenuAction.Status.Disabled;
                });
            }

            if (evt.target is ShaderGroup shaderGroup)
            {
                if (!selection.Contains(shaderGroup))
                {
                    selection.Add(shaderGroup);
                }

                var data = shaderGroup.userData;
                int count = evt.menu.MenuItems().Count;
                evt.menu.InsertAction(count, "Delete Group and Contents", (e) => RemoveNodesInsideGroup(e, data), DropdownMenuAction.AlwaysEnabled);
            }

            if (evt.target is BlackboardField)
            {
                PopulateBlackboardField(evt);
            }

            if (evt.target is BlackboardCateogrySection)
            {
                PopulateBlackboardCateogrySectionMenu(evt);
            }
        }

        void PopulateBlackboardField(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Delete", (e) => DeleteSelectionImplementation("Delete", AskUser.DontAskUser),(e) => canDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Duplicate", (e) => DuplicateSelection(), (a) => (canDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled));

            BlackboardField blackboardField = evt.target as BlackboardField;
            int indexInsideCategory = blackboardField.parent.IndexOf(blackboardField);
            int categoryIndex = blackboardField.parent.parent.IndexOf(blackboardField.parent);
            ShaderInput input = graph.GetInputCategory(categoryIndex).GetInput(indexInsideCategory);

            InputCategory myCategory = graph.GetContainingInputCategory(input);
            foreach (InputCategory otherCategory in graph.categories)
            {
                int count = otherCategory.inputs.Count() + 1;
                for (int x = 0; x < count; x++)
                {
                    // TODO: After testing is done, disable moving to self.
                    string menuString = "Drag & Drop To/" + otherCategory.header + "/Index " + x.ToString();
                    evt.menu.AppendAction(menuString, (e) => graph.MoveInput(input, myCategory, otherCategory, x), DropdownMenuAction.AlwaysEnabled);
                }
            }
        }

        void PopulateBlackboardCateogrySectionMenu(ContextualMenuPopulateEvent evt)
        {
            BlackboardCateogrySection section = evt.target as BlackboardCateogrySection;
            InputCategory category = graph.GetInputCategory(section.GetIndexWithinBlackboard());

            // TODO: Current blackboard sections don't support remaing
            // evt.menu.AppendAction("Rename", (a) => category.blackboardSection.OpenTextEditor(), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Rename", (a) => category.header = BlackboardCateogrySection.GetRandomTrollName(), DropdownMenuAction.AlwaysEnabled);

            // Don't allow deletion of the last remaining category
            DropdownMenuAction.Status deletionStatus = (graph.categories.Count() > 1)
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled;
            evt.menu.AppendAction("Delete", (a) => RemoveCategory(category), deletionStatus);
            evt.menu.AppendAction("Delete All", (a) => RemoveCategoryAndContents(category), deletionStatus);

            evt.menu.AppendAction("Collapse", (a) => ToggleCollapse(category), DropdownMenuAction.AlwaysEnabled);

            evt.menu.AppendSeparator("/");

            int count = graph.categories.Count() + 1;
            int myIndex = graph.GetInputCategoryIndex(category);
            for (int x = 0; x < count; x++)
            {
                // TODO: After testing is done, disable moving to self.
                string menuString = "Drag & Drop To/Index " + x.ToString();
                evt.menu.AppendAction(menuString, (e) => graph.MoveInputCategory(category, x), DropdownMenuAction.AlwaysEnabled);
            }

            evt.menu.AppendSeparator("/");

            evt.menu.AppendAction($"Insert New/Vector1", (a) => graph.AddShaderInput(new Vector1ShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Vector2", (a) => graph.AddShaderInput(new Vector2ShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Vector3", (a) => graph.AddShaderInput(new Vector3ShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Vector4", (a) => graph.AddShaderInput(new Vector4ShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Color", (a) => graph.AddShaderInput(new ColorShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Texture2D", (a) => graph.AddShaderInput(new Texture2DShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Texture2D Array", (a) => graph.AddShaderInput(new Texture2DArrayShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Texture3D", (a) => graph.AddShaderInput(new Texture3DShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Cubemap", (a) => graph.AddShaderInput(new CubemapShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Boolean", (a) => graph.AddShaderInput(new BooleanShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Matrix2x2", (a) => graph.AddShaderInput(new Matrix2ShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Matrix3x3", (a) => graph.AddShaderInput(new Matrix3ShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Matrix4x4", (a) => graph.AddShaderInput(new Matrix4ShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/SamplerState", (a) => graph.AddShaderInput(new SamplerStateShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction($"Insert New/Gradient", (a) => graph.AddShaderInput(new GradientShaderProperty(), category), DropdownMenuAction.AlwaysEnabled);
        }

        void RemoveCategory(InputCategory category)
        {
            foreach (ShaderInput input in category.inputs)
            {
                graph.AddShaderInputToDefaultCategory(input);
            }

            RemoveCategoryAndContents(category);
        }

        void RemoveCategoryAndContents(InputCategory category)
        {
            graph.RemoveInputCategory(category);
        }

        void ToggleCollapse(InputCategory category)
        {
            category.ToggleCollapse();
        }

        void RemoveNodesInsideGroup(DropdownMenuAction action, GroupData data)
        {
            graph.owner.RegisterCompleteObjectUndo("Delete Group and Contents");
            var groupItems = graph.GetItemsInGroup(data);
            graph.RemoveElements(groupItems.OfType<AbstractMaterialNode>().ToArray(), new IEdge[] {}, new [] {data}, groupItems.OfType<StickyNoteData>().ToArray());
        }

        private void InitializePrecisionSubMenu(ContextualMenuPopulateEvent evt)
        {
            // Default the evt.menu buttons to disabled
            DropdownMenuAction.Status inheritPrecisionAction = DropdownMenuAction.Status.Disabled;
            DropdownMenuAction.Status floatPrecisionAction = DropdownMenuAction.Status.Disabled;
            DropdownMenuAction.Status halfPrecisionAction = DropdownMenuAction.Status.Disabled;

            // Check which precisions are available to switch to
            foreach (MaterialNodeView selectedNode in selection.Where(x => x is MaterialNodeView).Select(x => x as MaterialNodeView))
            {
                if (selectedNode.node.precision != Precision.Inherit)
                    inheritPrecisionAction = DropdownMenuAction.Status.Normal;
                if (selectedNode.node.precision != Precision.Float)
                    floatPrecisionAction = DropdownMenuAction.Status.Normal;
                if (selectedNode.node.precision != Precision.Half)
                    halfPrecisionAction = DropdownMenuAction.Status.Normal;
            }

            // Create the menu options
            evt.menu.AppendAction("Precision/Inherit", _ => SetNodePrecisionOnSelection(Precision.Inherit), (a) => inheritPrecisionAction);
            evt.menu.AppendAction("Precision/Float", _ => SetNodePrecisionOnSelection(Precision.Float), (a) => floatPrecisionAction);
            evt.menu.AppendAction("Precision/Half", _ => SetNodePrecisionOnSelection(Precision.Half), (a) => halfPrecisionAction);
        }

        private void InitializeViewSubMenu(ContextualMenuPopulateEvent evt)
        {
            // Default the menu buttons to disabled
            DropdownMenuAction.Status expandPreviewAction = DropdownMenuAction.Status.Disabled;
            DropdownMenuAction.Status collapsePreviewAction = DropdownMenuAction.Status.Disabled;
            DropdownMenuAction.Status minimizeAction = DropdownMenuAction.Status.Disabled;
            DropdownMenuAction.Status maximizeAction = DropdownMenuAction.Status.Disabled;

            // Initialize strings
            string expandPreviewText = "View/Expand Previews";
            string collapsePreviewText = "View/Collapse Previews";
            string expandPortText = "View/Expand Ports";
            string collapsePortText = "View/Collapse Ports";
            if (selection.Count == 1)
            {
                collapsePreviewText = "View/Collapse Preview";
                expandPreviewText = "View/Expand Preview";
            }

            // Check if we can expand or collapse the ports/previews
            foreach (MaterialNodeView selectedNode in selection.Where(x => x is MaterialNodeView).Select(x => x as MaterialNodeView))
            {
                if (selectedNode.node.hasPreview)
                {
                    if (selectedNode.node.previewExpanded)
                        collapsePreviewAction = DropdownMenuAction.Status.Normal;
                    else
                        expandPreviewAction = DropdownMenuAction.Status.Normal;
                }

                if (selectedNode.CanToggleExpanded())
                {
                    if (selectedNode.expanded)
                        minimizeAction = DropdownMenuAction.Status.Normal;
                    else
                        maximizeAction = DropdownMenuAction.Status.Normal;
                }
            }

            // Create the menu options
            evt.menu.AppendAction(collapsePortText, _ => SetNodeExpandedOnSelection(false), (a) => minimizeAction);
            evt.menu.AppendAction(expandPortText, _ => SetNodeExpandedOnSelection(true), (a) => maximizeAction);

            evt.menu.AppendSeparator("View/");

            evt.menu.AppendAction(expandPreviewText, _ => SetPreviewExpandedOnSelection(true), (a) => expandPreviewAction);
            evt.menu.AppendAction(collapsePreviewText, _ => SetPreviewExpandedOnSelection(false), (a) => collapsePreviewAction);
        }

        void ChangeCustomNodeColor(DropdownMenuAction menuAction)
        {
            // Color Picker is internal :(
            var t = typeof(EditorWindow).Assembly.GetTypes().FirstOrDefault(ty => ty.Name == "ColorPicker");
            var m = t?.GetMethod("Show", new[] {typeof(Action<Color>), typeof(Color), typeof(bool), typeof(bool)});
            if (m == null)
            {
                Debug.LogWarning("Could not invoke Color Picker for ShaderGraph.");
                return;
            }

            var editorView = GetFirstAncestorOfType<GraphEditorView>();
            var defaultColor = Color.gray;
            if (selection.FirstOrDefault(sel => sel is MaterialNodeView) is MaterialNodeView selNode1)
            {
                defaultColor = selNode1.GetColor();
                defaultColor.a = 1.0f;
            }

            void ApplyColor(Color pickedColor)
            {
                foreach (var selectable in selection)
                {
                    if(selectable is MaterialNodeView nodeView)
                    {
                        nodeView.node.SetColor(editorView.colorManager.activeProviderName, pickedColor);
                        editorView.colorManager.UpdateNodeView(nodeView);
                    }
                }
            }
            graph.owner.RegisterCompleteObjectUndo("Change Node Color");
            m.Invoke(null, new object[] {(Action<Color>) ApplyColor, defaultColor, true, false});
        }

        protected override bool canDeleteSelection
        {
            get
            {
                Debug.Log("Selection Count... " + selection.Count());
                foreach (ISelectable isss in selection) Debug.Log("my type is... " + isss.GetType() + "  --> " + (isss is IShaderNodeView).ToString());
                Debug.Log(selection.Any(x => !(x is IShaderNodeView nodeView) || nodeView.node.canDeleteNode).ToString());
                return selection.Any(x => !(x is IShaderNodeView nodeView) || nodeView.node.canDeleteNode);
            }
        }

        public void GroupSelection()
        {
            var title = "New Group";
            var groupData = new GroupData(title, new Vector2(10f,10f));

            graph.owner.RegisterCompleteObjectUndo("Create Group Node");
            graph.CreateGroup(groupData);

            foreach (var element in selection.OfType<GraphElement>())
            {
                if (element.userData is IGroupItem groupItem)
                {
                    graph.SetGroup(groupItem, groupData);
                }
            }
        }

        public void AddStickyNote(Vector2 position)
        {
            position = contentViewContainer.WorldToLocal(position);
            string title = "New Note";
            string content = "Write something here";
            var stickyNoteData  = new StickyNoteData(title, content, new Rect(position.x, position.y, 200, 160));
            graph.owner.RegisterCompleteObjectUndo("Create Sticky Note");
            graph.AddStickyNote(stickyNoteData);
        }

        public void RemoveFromGroupNode()
        {
            graph.owner.RegisterCompleteObjectUndo("Ungroup Node(s)");
            foreach (var element in selection.OfType<GraphElement>())
            {
                if (element.userData is IGroupItem)
                {
                    Group group = element.GetContainingScope() as Group;
                    if (group != null)
                    {
                        group.RemoveElement(element);
                    }
                }
            }
        }

        public void SetNodeExpandedOnSelection(bool state)
        {
            graph.owner.RegisterCompleteObjectUndo("Toggle Expansion");
            foreach (MaterialNodeView selectedNode in selection.Where(x => x is MaterialNodeView).Select(x => x as MaterialNodeView))
            {
                if(selectedNode.CanToggleExpanded())
                    selectedNode.expanded = state;
            }
        }

        public void SetPreviewExpandedOnSelection(bool state)
        {
            graph.owner.RegisterCompleteObjectUndo("Toggle Preview Visibility");
            foreach (MaterialNodeView selectedNode in selection.Where(x => x is MaterialNodeView).Select(x => x as MaterialNodeView))
            {
                selectedNode.node.previewExpanded = state;
            }
        }

        public void SetNodePrecisionOnSelection(Precision inPrecision)
        {
            var editorView = GetFirstAncestorOfType<GraphEditorView>();
            IEnumerable<MaterialNodeView> nodes = selection.Where(x => x is MaterialNodeView node && node.node.canSetPrecision).Select(x => x as MaterialNodeView);

            graph.owner.RegisterCompleteObjectUndo("Set Precisions");
            editorView.colorManager.SetNodesDirty(nodes);

            foreach (MaterialNodeView selectedNode in nodes)
            {
                selectedNode.node.precision = inPrecision;
            }

            // Reflect the data down
            graph.ValidateGraph();
            editorView.colorManager.UpdateNodeViews(nodes);

            // Update the views
            foreach (MaterialNodeView selectedNode in nodes)
                selectedNode.node.Dirty(ModificationScope.Topological);
        }

        void CollapsePreviews(DropdownMenuAction action)
        {
            graph.owner.RegisterCompleteObjectUndo("Collapse Previews");
            foreach (AbstractMaterialNode node in graph.GetNodes<AbstractMaterialNode>())
            {
                node.previewExpanded = false;
            }
        }

        void ExpandPreviews(DropdownMenuAction action)
        {
            graph.owner.RegisterCompleteObjectUndo("Expand Previews");
            foreach (AbstractMaterialNode node in graph.GetNodes<AbstractMaterialNode>())
            {
                node.previewExpanded = true;
            }
        }

        void SeeDocumentation(DropdownMenuAction action)
        {
            var node = selection.OfType<IShaderNodeView>().First().node;
            if (node.documentationURL != null)
                System.Diagnostics.Process.Start(node.documentationURL);
        }

        void OpenSubGraph(DropdownMenuAction action)
        {
            SubGraphNode subgraphNode = selection.OfType<IShaderNodeView>().First().node as SubGraphNode;

            var path = AssetDatabase.GetAssetPath(subgraphNode.asset);
            ShaderGraphImporterEditor.ShowGraphEditWindow(path);
        }

        DropdownMenuAction.Status SeeDocumentationStatus(DropdownMenuAction action)
        {
            if (selection.OfType<IShaderNodeView>().First().node.documentationURL == null)
                return DropdownMenuAction.Status.Disabled;
            return DropdownMenuAction.Status.Normal;
        }

        DropdownMenuAction.Status ConvertToPropertyStatus(DropdownMenuAction action)
        {
            if (selection.OfType<IShaderNodeView>().Any(v => v.node != null))
            {
                if (selection.OfType<IShaderNodeView>().Any(v => v.node is IPropertyFromNode))
                    return DropdownMenuAction.Status.Normal;
                return DropdownMenuAction.Status.Disabled;
            }
            return DropdownMenuAction.Status.Hidden;
        }

        void ConvertToProperty(DropdownMenuAction action)
        {
            graph.owner.RegisterCompleteObjectUndo("Convert to Property");
            var selectedNodeViews = selection.OfType<IShaderNodeView>().Select(x => x.node).ToList();
            foreach (var node in selectedNodeViews)
            {
                if (!(node is IPropertyFromNode))
                    continue;

                var converter = node as IPropertyFromNode;
                var prop = converter.AsShaderProperty();
                graph.SanitizeGraphInputName(prop);
                graph.AddShaderInputToDefaultCategory(prop);

                var propNode = new PropertyNode();
                propNode.drawState = node.drawState;
                propNode.groupGuid = node.groupGuid;
                graph.AddNode(propNode);
                propNode.propertyGuid = prop.guid;

                var oldSlot = node.FindSlot<MaterialSlot>(converter.outputSlotId);
                var newSlot = propNode.FindSlot<MaterialSlot>(PropertyNode.OutputSlotId);

                foreach (var edge in graph.GetEdges(oldSlot.slotReference))
                    graph.Connect(newSlot.slotReference, edge.inputSlot);

                graph.RemoveNode(node);
            }
        }

        DropdownMenuAction.Status ConvertToInlineNodeStatus(DropdownMenuAction action)
        {
            if (selection.OfType<IShaderNodeView>().Any(v => v.node != null))
            {
                if (selection.OfType<IShaderNodeView>().Any(v => v.node is PropertyNode))
                    return DropdownMenuAction.Status.Normal;
                return DropdownMenuAction.Status.Disabled;
            }
            return DropdownMenuAction.Status.Hidden;
        }

        void ConvertToInlineNode(DropdownMenuAction action)
        {
            graph.owner.RegisterCompleteObjectUndo("Convert to Inline Node");
            var selectedNodeViews = selection.OfType<IShaderNodeView>()
                .Select(x => x.node)
                .OfType<PropertyNode>();

            foreach (var propNode in selectedNodeViews)
                ((GraphData)propNode.owner).ReplacePropertyNodeWithConcreteNode(propNode);
        }

        void DuplicateSelection()
        {
            graph.owner.RegisterCompleteObjectUndo("Duplicate Blackboard Property");

            List<ShaderInput> selectedProperties = new List<ShaderInput>();
            foreach (var selectable in selection)
            {
                ShaderInput shaderProp = (ShaderInput)((BlackboardField)selectable).userData;
                if (shaderProp != null)
                {
                    selectedProperties.Add(shaderProp);
                }
            }

            CopyPasteGraph copiedProperties = new CopyPasteGraph("", null, null, null, selectedProperties,
                null, null, null, graph);

            GraphViewExtensions.InsertCopyPasteGraph(this, copiedProperties);

            // TODO: Needs to be pre section, not the entire thing.
            graph.SectionChangesHappened();
        }

        DropdownMenuAction.Status ConvertToSubgraphStatus(DropdownMenuAction action)
        {
            if (onConvertToSubgraphClick == null) return DropdownMenuAction.Status.Hidden;
            return selection.OfType<IShaderNodeView>().Any(v => v.node != null && v.node.allowedInSubGraph && !(v.node is SubGraphOutputNode) ) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden;
        }

        void ConvertToSubgraph(DropdownMenuAction action)
        {
            onConvertToSubgraphClick();
        }

        string SerializeGraphElementsImplementation(IEnumerable<GraphElement> elements)
        {
            var groups = elements.OfType<ShaderGroup>().Select(x => x.userData);
            var nodes = elements.OfType<IShaderNodeView>().Select(x => x.node).Where(x => x.canCopyNode);
            var edges = elements.OfType<Edge>().Select(x => x.userData).OfType<IEdge>();
            var inputs = selection.OfType<BlackboardField>().Select(x => x.userData as ShaderInput);
            var notes = elements.OfType<StickyNote>().Select(x => x.userData);

            // Collect the property nodes and get the corresponding properties
            var propertyNodeGuids = nodes.OfType<PropertyNode>().Select(x => x.propertyGuid);
            var metaProperties = this.graph.properties.Where(x => propertyNodeGuids.Contains(x.guid));

            // Collect the keyword nodes and get the corresponding keywords
            var keywordNodeGuids = nodes.OfType<KeywordNode>().Select(x => x.keywordGuid);
            var metaKeywords = this.graph.keywords.Where(x => keywordNodeGuids.Contains(x.guid));

            var copyPasteGraph = new CopyPasteGraph(this.graph.assetGuid, groups, nodes, edges, inputs, metaProperties, metaKeywords, notes, graph);
            return JsonUtility.ToJson(copyPasteGraph, true);
        }

        bool CanPasteSerializedDataImplementation(string serializedData)
        {
            return CopyPasteGraph.FromJson(serializedData) != null;
        }

        void UnserializeAndPasteImplementation(string operationName, string serializedData)
        {
            graph.owner.RegisterCompleteObjectUndo(operationName);
            var pastedGraph = CopyPasteGraph.FromJson(serializedData);
            this.InsertCopyPasteGraph(pastedGraph);
        }

        void DeleteSelectionImplementation(string operationName, GraphView.AskUser askUser)
        {
            bool containsProperty = false;

            // Keywords need to be tested against variant limit based on multiple factors
            bool keywordsDirty = false;

            // Track dependent keyword nodes to remove them
            List<KeywordNode> keywordNodes = new List<KeywordNode>();

            foreach (var selectable in selection)
            {
                var field = selectable as BlackboardField;
                if (field != null && field.userData != null)
                {
                    switch(field.userData)
                    {
                        case AbstractShaderProperty property:
                            containsProperty = true;
                            break;
                        case ShaderKeyword keyword:
                            keywordNodes.AddRange(graph.GetNodes<KeywordNode>().Where(x => x.keywordGuid == keyword.guid));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            if(containsProperty)
            {
                if (graph.isSubGraph)
                {
                    if (!EditorUtility.DisplayDialog("Sub Graph Will Change", "If you remove a property and save the sub graph, you might change other graphs that are using this sub graph.\n\nDo you want to continue?", "Yes", "No"))
                        return;
                }
            }

            // Filter nodes that cannot be deleted
            var nodesToDelete = selection.OfType<IShaderNodeView>().Where(v => !(v.node is SubGraphOutputNode) && v.node.canDeleteNode).Select(x => x.node);

            // Add keyword nodes dependent on deleted keywords
            nodesToDelete = nodesToDelete.Union(keywordNodes);

            // If deleting a Sub Graph node whose asset contains Keywords test variant limit
            foreach(SubGraphNode subGraphNode in nodesToDelete.OfType<SubGraphNode>())
            {
                if (subGraphNode.asset == null)
                {
                    continue;
                }
                if(subGraphNode.asset.keywords.Count > 0)
                {
                    keywordsDirty = true;
                }
            }

            graph.owner.RegisterCompleteObjectUndo(operationName);
            graph.RemoveElements(nodesToDelete.ToArray(),
                selection.OfType<Edge>().Select(x => x.userData).OfType<IEdge>().ToArray(),
                selection.OfType<ShaderGroup>().Select(x => x.userData).ToArray(),
                selection.OfType<StickyNote>().Select(x => x.userData).ToArray());

            // Blackboard deletion
            foreach (var selectable in selection)
            {
                var field = selectable as BlackboardField;
                if (field != null && field.userData != null)
                {
                    var input = (ShaderInput)field.userData;
                    graph.RemoveGraphInput(input);

                    // If deleting a Keyword test variant limit
                    if(input is ShaderKeyword keyword)
                    {
                        keywordsDirty = true;
                    }
                }
            }

            // Test Keywords against variant limit
            if(keywordsDirty)
            {
                graph.OnKeywordChangedNoValidate();
            }

            selection.Clear();
        }

        public static void GetIndexToInsert(Blackboard blackboard, out int propertyIndex, out int keywordIndex)
        {
            if (blackboard == null || !blackboard.selection.Any())
            {
                propertyIndex = -1;
                keywordIndex = -1;
                return;
            }

            int highestPropertyIndex = -2;
            int highestKeywordIndex  = -2;

            foreach (ISelectable selection in blackboard.selection)
            {
                BlackboardField selectedBlackboardField = selection as BlackboardField;
                if (selectedBlackboardField != null)
                {
                    BlackboardRow row = selectedBlackboardField.GetFirstAncestorOfType<BlackboardRow>();
                    BlackboardSection section = selectedBlackboardField.GetFirstAncestorOfType<BlackboardSection>();
                    if (row == null || section == null)
                        continue;
                    VisualElement sectionContainer = section.parent;

                    int index;
                    // Property Section
                    if (sectionContainer.IndexOf(section) == 0)
                    {
                        index = section.IndexOf(row);
                        if (index > highestPropertyIndex)
                        {
                            highestPropertyIndex = index;
                        }
                    }
                    // Keyword Section
                    else if (sectionContainer.IndexOf(section) == 1)
                    {
                        index = section.IndexOf(row);
                        if (index > highestKeywordIndex)
                        {
                            highestKeywordIndex = index;
                        }
                    }
                }
            }

            propertyIndex = highestPropertyIndex + 1;
            keywordIndex = highestKeywordIndex + 1;
        }

        #region Drag and drop

        bool ValidateObjectForDrop(Object obj)
        {
            return EditorUtility.IsPersistent(obj) && (
                obj is Texture2D ||
                obj is Cubemap ||
                obj is SubGraphAsset asset && !asset.descendents.Contains(graph.assetGuid) && asset.assetGuid != graph.assetGuid ||
                obj is Texture2DArray ||
                obj is Texture3D);
        }

        void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            bool dragging = false;
            if (selection != null)
            {
                // Blackboard
                if (selection.OfType<BlackboardField>().Any())
                    dragging = true;
            }
            else
            {
                // Handle unity objects
                var objects = DragAndDrop.objectReferences;
                foreach (Object obj in objects)
                {
                    if (ValidateObjectForDrop(obj))
                    {
                        dragging = true;
                        break;
                    }
                }
            }

            if (dragging)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            }
        }

        void OnDragPerformEvent(DragPerformEvent e)
        {
            Vector2 localPos = (e.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, e.localMousePosition);

            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            if (selection != null)
            {
                // Blackboard
                if (selection.OfType<BlackboardField>().Any())
                {
                    IEnumerable<BlackboardField> fields = selection.OfType<BlackboardField>();
                    foreach (BlackboardField field in fields)
                    {
                        CreateNode(field, localPos);
                    }
                }
            }
            else
            {
                // Handle unity objects
                var objects = DragAndDrop.objectReferences;
                foreach (Object obj in objects)
                {
                    if (ValidateObjectForDrop(obj))
                    {
                        CreateNode(obj, localPos);
                    }
                }
            }
        }

        void CreateNode(object obj, Vector2 nodePosition)
        {
            var texture2D = obj as Texture2D;
            if (texture2D != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Texture");

                bool isNormalMap = false;
                if (EditorUtility.IsPersistent(texture2D) && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture2D)))
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture2D)) as TextureImporter;
                    if (importer != null)
                        isNormalMap = importer.textureType == TextureImporterType.NormalMap;
                }

                var node = new SampleTexture2DNode();
                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                graph.AddNode(node);

                if (isNormalMap)
                    node.textureType = TextureType.Normal;

                var inputslot = node.FindInputSlot<Texture2DInputMaterialSlot>(SampleTexture2DNode.TextureInputId);
                if (inputslot != null)
                    inputslot.texture = texture2D;
            }

            var textureArray = obj as Texture2DArray;
            if (textureArray != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Texture Array");

                var node = new SampleTexture2DArrayNode();
                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                graph.AddNode(node);

                var inputslot = node.FindSlot<Texture2DArrayInputMaterialSlot>(SampleTexture2DArrayNode.TextureInputId);
                if (inputslot != null)
                    inputslot.textureArray = textureArray;
            }

            var texture3D = obj as Texture3D;
            if (texture3D != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Texture 3D");

                var node = new SampleTexture3DNode();
                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                graph.AddNode(node);

                var inputslot = node.FindSlot<Texture3DInputMaterialSlot>(SampleTexture3DNode.TextureInputId);
                if (inputslot != null)
                    inputslot.texture = texture3D;
            }

            var cubemap = obj as Cubemap;
            if (cubemap != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Cubemap");

                var node = new SampleCubemapNode();
                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                graph.AddNode(node);

                var inputslot = node.FindInputSlot<CubemapInputMaterialSlot>(SampleCubemapNode.CubemapInputId);
                if (inputslot != null)
                    inputslot.cubemap = cubemap;
            }

            var subGraphAsset = obj as SubGraphAsset;
            if (subGraphAsset != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Sub-Graph");
                var node = new SubGraphNode();

                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                node.asset = subGraphAsset;
                graph.AddNode(node);
            }

            var blackboardField = obj as BlackboardField;
            if (blackboardField != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Graph Input");

                switch(blackboardField.userData)
                {
                    case AbstractShaderProperty property:
                    {
                        var node = new PropertyNode();
                        var drawState = node.drawState;
                        drawState.position =  new Rect(nodePosition, drawState.position.size);
                        node.drawState = drawState;
                        graph.AddNode(node);

                        // Setting the guid requires the graph to be set first.
                        node.propertyGuid = property.guid;
                        break;
                    }
                    case ShaderKeyword keyword:
                    {
                        var node = new KeywordNode();
                        var drawState = node.drawState;
                        drawState.position =  new Rect(nodePosition, drawState.position.size);
                        node.drawState = drawState;
                        graph.AddNode(node);

                        // Setting the guid requires the graph to be set first.
                        node.keywordGuid = keyword.guid;
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion
    }

    static class GraphViewExtensions
    {
        // Sorts based on their position on the blackboard
        internal class PropertyOrder : IComparer<ShaderInput>
        {
            GraphData graphData;

            internal PropertyOrder(GraphData data)
            {
                graphData = data;
            }

            public int Compare(ShaderInput x, ShaderInput y)
            {
                if (graphData.GetContainingCategory(x).GetInputIndex(x) > graphData.GetContainingCategory(y).GetInputIndex(y)) return 1;
                else return -1;
            }
        }

        internal static void InsertCopyPasteGraph(this MaterialGraphView graphView, CopyPasteGraph copyGraph)
        {
            if (copyGraph == null)
                return;

            graphView.graph.SectionChangesHappened();

            // Keywords need to be tested against variant limit based on multiple factors
            bool keywordsDirty = false;

            Blackboard blackboard = graphView.GetFirstAncestorOfType<GraphEditorView>().blackboardProvider.blackboard;

            // Get the position to insert the new shader inputs per section
            int propertyIndex;
            int keywordIndex;
            MaterialGraphView.GetIndexToInsert(blackboard, out propertyIndex, out keywordIndex);

            // Make new inputs from the copied graph
            foreach (ShaderInput input in copyGraph.inputs)
            {
                ShaderInput copiedInput;

                switch(input)
                {
                    case AbstractShaderProperty property:
                        // Increment for next within the same section
                        copiedInput = DuplicateShaderInputs(input, graphView.graph, propertyIndex);
                        if (propertyIndex > 0) propertyIndex++;

                        // Update the property nodes that depends on the copied node
                        var dependentPropertyNodes = copyGraph.GetNodes<PropertyNode>().Where(x => x.propertyGuid == input.guid);
                        foreach (var node in dependentPropertyNodes)
                        {
                            node.owner = graphView.graph;
                            node.propertyGuid = copiedInput.guid;
                        }
                        break;

                    case ShaderKeyword shaderKeyword:
                        // Don't duplicate built-in keywords within the same graph
                        if (KeywordUtil.IsBuiltinKeyword(input as ShaderKeyword)
                            && graphView.graph.keywords.Where(p => p.referenceName == input.referenceName).Any())
                        {
                            continue;
                        }

                        // Increment for next within the same section
                        copiedInput = DuplicateShaderInputs(input, graphView.graph, keywordIndex);
                        if (keywordIndex > 0) keywordIndex++;

                        // Update the keyword nodes that depends on the copied node
                        var dependentKeywordNodes = copyGraph.GetNodes<KeywordNode>().Where(x => x.keywordGuid == input.guid);
                        foreach (var node in dependentKeywordNodes)
                        {
                            node.owner = graphView.graph;
                            node.keywordGuid = copiedInput.guid;
                        }

                        // Pasting a new Keyword so need to test against variant limit
                        keywordsDirty = true;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Pasting a Sub Graph node that contains Keywords so need to test against variant limit
            foreach(SubGraphNode subGraphNode in copyGraph.GetNodes<SubGraphNode>())
            {
                if(subGraphNode.asset.keywords.Count > 0)
                {
                    keywordsDirty = true;
                }
            }

            // Test Keywords against variant limit
            if(keywordsDirty)
            {
                graphView.graph.OnKeywordChangedNoValidate();
            }

            using (var remappedNodesDisposable = ListPool<AbstractMaterialNode>.GetDisposable())
            {
                using (var remappedEdgesDisposable = ListPool<IEdge>.GetDisposable())
                {
                    var remappedNodes = remappedNodesDisposable.value;
                    var remappedEdges = remappedEdgesDisposable.value;
                    graphView.graph.PasteGraph(copyGraph, remappedNodes, remappedEdges);

                    if (graphView.graph.assetGuid != copyGraph.sourceGraphGuid)
                    {
                        // Compute the mean of the copied nodes.
                        Vector2 centroid = Vector2.zero;
                        var count = 1;
                        foreach (var node in remappedNodes)
                        {
                            var position = node.drawState.position.position;
                            centroid = centroid + (position - centroid) / count;
                            ++count;
                        }

                        // Get the center of the current view
                        var viewCenter = graphView.contentViewContainer.WorldToLocal(graphView.layout.center);

                        foreach (var node in remappedNodes)
                        {
                            var drawState = node.drawState;
                            var positionRect = drawState.position;
                            var position = positionRect.position;
                            position += viewCenter - centroid;
                            positionRect.position = position;
                            drawState.position = positionRect;
                            node.drawState = drawState;
                        }
                    }

                    // Add new elements to selection
                    graphView.ClearSelection();
                    graphView.graphElements.ForEach(element =>
                        {
                            if (element is Edge edge && remappedEdges.Contains(edge.userData as IEdge))
                                graphView.AddToSelection(edge);

                            if (element is IShaderNodeView nodeView && remappedNodes.Contains(nodeView.node))
                                graphView.AddToSelection((Node)nodeView);
                        });
                }
            }
        }

        static ShaderInput DuplicateShaderInputs(ShaderInput original, GraphData graph, int index)
        {
            ShaderInput copy = original.Copy();
            graph.SanitizeGraphInputName(copy);
            // TODO: We don't want to use the default here
            // graph.AddShaderInput(copy, index);
            graph.AddShaderInputToDefaultCategory(copy);
            copy.generatePropertyBlock = original.generatePropertyBlock;
            return copy;
        }
    }
}
