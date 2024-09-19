
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    static class ShaderGraphShortcuts
    {
        static MaterialGraphEditWindow GetFocusedShaderGraphEditorWindow()
        {
            return EditorWindow.focusedWindow as MaterialGraphEditWindow;
        }

        static GraphEditorView GetGraphEditorView()
        {
            return GetFocusedShaderGraphEditorWindow().graphEditorView;
        }

        static MaterialGraphView GetGraphView()
        {
            return GetGraphEditorView().graphView;
        }

        static bool GetMousePositionIsInGraphView(out Vector2 pos)
        {
            pos = default;
            var graphView = GetGraphView();
            var windowRoot = GetFocusedShaderGraphEditorWindow().rootVisualElement;
            var windowMousePosition = windowRoot.ChangeCoordinatesTo(windowRoot.parent, graphView.cachedMousePosition);

            if (!graphView.worldBound.Contains(windowMousePosition))
                return false; // don't create nodes if they aren't on the graph view.

            pos = graphView.contentViewContainer.WorldToLocal(graphView.cachedMousePosition);
            return true;
        }

        static void CreateNode<T>() where T : AbstractMaterialNode
        {
            if (!GetMousePositionIsInGraphView(out var graphMousePosition))
                return;

            var positionRect = new Rect(graphMousePosition, Vector2.zero);

            var graphView = GetGraphView();
            var graph = graphView.graph;
            AbstractMaterialNode node = Activator.CreateInstance<T>();

            var drawState = node.drawState;
            drawState.position = positionRect;
            node.drawState = drawState;

            graph.owner.RegisterCompleteObjectUndo("Add " + node.name);
            graphView.graph.AddNode(node);
        }

        static HashSet<(KeyCode key, ShortcutModifiers modifier)> reservedShortcuts = new HashSet<(KeyCode key, ShortcutModifiers modifier)> {
                (KeyCode.A, ShortcutModifiers.None), // Frame All
                (KeyCode.F, ShortcutModifiers.None), // Frame Selection
                (KeyCode.Space, ShortcutModifiers.None), // Summon Searcher (for node creation)
                (KeyCode.C, ShortcutModifiers.Action), // Copy
                (KeyCode.X, ShortcutModifiers.Action), // cut
                (KeyCode.V, ShortcutModifiers.Action), // Paste
                (KeyCode.Z, ShortcutModifiers.Action), // Undo
                (KeyCode.Y, ShortcutModifiers.Action), // Redo
                (KeyCode.D, ShortcutModifiers.Action), // Duplicate
            };

        static void CheckBindings(string name)
        {
            if (!ShortcutManager.instance.IsShortcutOverridden(name))
                return;

            var customBinding = ShortcutManager.instance.GetShortcutBinding(name);

            foreach(var keyCombo in customBinding.keyCombinationSequence)
            {
                if (reservedShortcuts.Contains((keyCombo.keyCode, keyCombo.modifiers)))
                {
                    throw new Exception($"The binding for {name} ({keyCombo}) conflicts with a built-in shortcut. Please go to Edit->Shortcuts... and change the binding.");
                }
            }
        }

        internal static string GetKeycodeForContextMenu(string id)
        {
            const string kKeycodePrefixAlt = "&";
            const string kKeycodePrefixShift = "#";
            const string kKeycodePrefixAction = "%";
            const string kKeycodePrefixControl = "^";
            const string kKeycodePrefixNoModifier = "_";

            var binding = ShortcutManager.instance.GetShortcutBinding(id);
            foreach (var keyCombo in binding.keyCombinationSequence)
            {
                var sb = new StringBuilder();

                if (keyCombo.alt) sb.Append(kKeycodePrefixAlt);
                if (keyCombo.shift) sb.Append(kKeycodePrefixShift);
                if (keyCombo.action) sb.Append(kKeycodePrefixAction);
                if (keyCombo.control) sb.Append(kKeycodePrefixControl);
                if (keyCombo.modifiers == ShortcutModifiers.None) sb.Append(kKeycodePrefixNoModifier);

                sb.Append(keyCombo.keyCode);
                return sb.ToString();
            }

            return "";
        }

        #region File
        [Shortcut("ShaderGraph/File: Save", typeof(MaterialGraphEditWindow), KeyCode.S, ShortcutModifiers.Action)]
        static void Save(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            GetFocusedShaderGraphEditorWindow().SaveAsset();
        }

        [Shortcut("ShaderGraph/File: Save As...", typeof(MaterialGraphEditWindow), KeyCode.S, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        static void SaveAs(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            GetFocusedShaderGraphEditorWindow().SaveAs();
        }

        [Shortcut("ShaderGraph/File: Close Tab", typeof(MaterialGraphEditWindow), KeyCode.F4, ShortcutModifiers.Action)]
        static void CloseTab(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var editorWindow = GetFocusedShaderGraphEditorWindow();
            if (editorWindow.PromptSaveIfDirtyOnQuit())
                editorWindow.Close();
        }

        #endregion

        #region Toolbar
        [Shortcut("ShaderGraph/Toolbar: Toggle Blackboard", typeof(MaterialGraphEditWindow), KeyCode.Alpha1, ShortcutModifiers.Shift)]
        static void ToggleBlackboard(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var graphEditor = GetGraphEditorView();
            graphEditor.viewSettings.isBlackboardVisible = !graphEditor.viewSettings.isBlackboardVisible;
            graphEditor.UserViewSettingsChangeCheck(graphEditor.colorManager.activeIndex);
        }

        [Shortcut("ShaderGraph/Toolbar: Toggle Inspector", typeof(MaterialGraphEditWindow), KeyCode.Alpha2, ShortcutModifiers.Shift)]
        static void ToggleInspector(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var graphEditor = GetGraphEditorView();
            graphEditor.viewSettings.isInspectorVisible = !graphEditor.viewSettings.isInspectorVisible;
            graphEditor.UserViewSettingsChangeCheck(graphEditor.colorManager.activeIndex);
        }

        [Shortcut("ShaderGraph/Toolbar: Toggle Main Preview", typeof(MaterialGraphEditWindow), KeyCode.Alpha3, ShortcutModifiers.Shift)]
        static void ToggleMainPreview(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var graphEditor = GetGraphEditorView();
            graphEditor.viewSettings.isPreviewVisible = !graphEditor.viewSettings.isPreviewVisible;
            graphEditor.UserViewSettingsChangeCheck(graphEditor.colorManager.activeIndex);
        }

        [Shortcut("ShaderGraph/Toolbar: Cycle Color Mode", typeof(MaterialGraphEditWindow), KeyCode.Alpha4, ShortcutModifiers.Shift)]
        static void CycleColorMode(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            var graphEditor = GetGraphEditorView();

            var nextIndex = graphEditor.colorManager.activeIndex + 1;
            if (nextIndex >= graphEditor.colorManager.providersCount)
                nextIndex = 0;

            graphEditor.UserViewSettingsChangeCheck(nextIndex);
        }
        #endregion

        #region Selection
        internal const string summonDocumentationShortcutID = "ShaderGraph/Selection: Summon Documentation";
        [Shortcut(summonDocumentationShortcutID, typeof(MaterialGraphEditWindow), KeyCode.F1)]
        static void Documentation(ShortcutArguments args)
        {
            CheckBindings(summonDocumentationShortcutID);
            foreach (var selected in GetGraphView().selection)
                if (selected is IShaderNodeView nodeView && nodeView.node.documentationURL != null)
                {
                    System.Diagnostics.Process.Start(nodeView.node.documentationURL);
                    break;
                }
        }

        internal const string nodeGroupShortcutID = "ShaderGraph/Selection: Node Group";
        [Shortcut(nodeGroupShortcutID, typeof(MaterialGraphEditWindow), KeyCode.G, ShortcutModifiers.Action)]
        static void Group(ShortcutArguments args)
        {
            CheckBindings(nodeGroupShortcutID);
            var graphView = GetGraphView();
            foreach(var selected in graphView.selection)
                if ((selected is IShaderNodeView nodeView && nodeView.node is AbstractMaterialNode)
                    || selected.GetType() == typeof(Drawing.StickyNote))
                {
                    graphView.GroupSelection();
                    break;
                }
        }

        internal const string nodeUnGroupShortcutID = "ShaderGraph/Selection: Node Ungroup";
        [Shortcut(nodeUnGroupShortcutID, typeof(MaterialGraphEditWindow), KeyCode.U, ShortcutModifiers.Action)]
        static void UnGroup(ShortcutArguments args)
        {
            CheckBindings(nodeUnGroupShortcutID);
            var graphView = GetGraphView();
            foreach (var selected in graphView.selection)
                if ((selected is IShaderNodeView nodeView && nodeView.node is AbstractMaterialNode)
                    || selected.GetType() == typeof(Drawing.StickyNote))
                {
                    graphView.RemoveFromGroupNode();
                    break;
                }
        }

        internal const string nodePreviewShortcutID = "ShaderGraph/Selection: Toggle Node Previews";
        [Shortcut(nodePreviewShortcutID, typeof(MaterialGraphEditWindow), KeyCode.T, ShortcutModifiers.Action)]
        static void ToggleNodePreviews(ShortcutArguments args)
        {
            CheckBindings(nodePreviewShortcutID);
            bool shouldHide = false;
            // Toggle all node previews if none are selected. Otherwise, update only the selected node previews.
            var selection = GetGraphView().selection;
            if (selection.Count == 0)
            {
                var graph = GetGraphView().graph;
                var nodes = graph.GetNodes<AbstractMaterialNode>();
                foreach (AbstractMaterialNode node in nodes)
                    if (node.previewExpanded && node.hasPreview)
                    {
                        shouldHide = true;
                        break;
                    }
                
                graph.owner.RegisterCompleteObjectUndo("Toggle Previews");
                foreach (AbstractMaterialNode node in nodes)
                    node.previewExpanded = !shouldHide;
            }
            else
            {
                foreach (var selected in selection)
                    if (selected is IShaderNodeView nodeView)
                    {
                        if (nodeView.node.previewExpanded && nodeView.node.hasPreview)
                        {
                            shouldHide = true;
                            break;
                        }
                    }
                GetGraphView().SetPreviewExpandedForSelectedNodes(!shouldHide);
            }
        }

        internal const string nodeCollapsedShortcutID = "ShaderGraph/Selection: Toggle Node Collapsed";
        [Shortcut(nodeCollapsedShortcutID, typeof(MaterialGraphEditWindow), KeyCode.P, ShortcutModifiers.Action)]
        static void ToggleNodeCollapsed(ShortcutArguments args)
        {
            CheckBindings(nodeCollapsedShortcutID);
            bool shouldCollapse = false;
            foreach (var selected in GetGraphView().selection)
                if (selected is MaterialNodeView nodeView)
                {
                    if (nodeView.expanded && nodeView.CanToggleNodeExpanded())
                    {
                        shouldCollapse = true;
                        break;
                    }
                }
            GetGraphView().SetNodeExpandedForSelectedNodes(!shouldCollapse);
        }

        internal const string createRedirectNodeShortcutID = "ShaderGraph/Selection: Insert Redirect";
        [Shortcut(createRedirectNodeShortcutID, typeof(MaterialGraphEditWindow), KeyCode.R, ShortcutModifiers.Action)]
        static void InsertRedirect(ShortcutArguments args)
        {
            CheckBindings(createRedirectNodeShortcutID);

            if (!GetMousePositionIsInGraphView(out var graphMousePosition))
                return;

            foreach (var selected in GetGraphView().selection)
            {
                if (selected is Edge edge)
                {
                    int weight = 1;
                    var pos = graphMousePosition * weight;
                    int count = weight;
                    foreach(var cp in edge.edgeControl.controlPoints)
                    {
                        pos += cp;
                        count++;
                    }
                    pos /= count;
                    pos = GetGraphView().contentViewContainer.LocalToWorld(pos);
                     GetGraphView().CreateRedirectNode(pos, edge);
                }
            }
        }
        #endregion

        #region Add Specific Node

        [Shortcut("ShaderGraph/Add Node: Lerp", typeof(MaterialGraphEditWindow), KeyCode.L, ShortcutModifiers.Alt)]
        static void CreateLerp(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<LerpNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Multiply", typeof(MaterialGraphEditWindow), KeyCode.M, ShortcutModifiers.Alt)]
        static void CreateMultiply(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<MultiplyNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Add", typeof(MaterialGraphEditWindow), KeyCode.A, ShortcutModifiers.Alt)]
        static void CreateAdd(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<AddNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Sample Texture 2D", typeof(MaterialGraphEditWindow), KeyCode.X, ShortcutModifiers.Alt)]
        static void CreateSampleTexture2D(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SampleTexture2DNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Float", typeof(MaterialGraphEditWindow), KeyCode.Alpha1, ShortcutModifiers.Alt)]
        static void CreateFloat(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector1Node>();
        }

        [Shortcut("ShaderGraph/Add Node: Vector2", typeof(MaterialGraphEditWindow), KeyCode.Alpha2, ShortcutModifiers.Alt)]
        static void CreateVec2(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector2Node>();
        }

        [Shortcut("ShaderGraph/Add Node: Vector3", typeof(MaterialGraphEditWindow), KeyCode.Alpha3, ShortcutModifiers.Alt)]
        static void CreateVec3(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector3Node>();
        }

        [Shortcut("ShaderGraph/Add Node: Vector4", typeof(MaterialGraphEditWindow), KeyCode.Alpha4, ShortcutModifiers.Alt)]
        static void CreateVec4(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<Vector4Node>();
        }

        [Shortcut("ShaderGraph/Add Node: Split", typeof(MaterialGraphEditWindow), KeyCode.E, ShortcutModifiers.Alt)]
        static void CreateSplit(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SplitNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Tiling and Offset", typeof(MaterialGraphEditWindow), KeyCode.O, ShortcutModifiers.Alt)]
        static void CreateTilingAndOffset(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<TilingAndOffsetNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Time", typeof(MaterialGraphEditWindow), KeyCode.T, ShortcutModifiers.Alt)]
        static void CreateTime(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<TimeNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Position", typeof(MaterialGraphEditWindow), KeyCode.V, ShortcutModifiers.Alt)]
        static void CreatePosition(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<PositionNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Subtract", typeof(MaterialGraphEditWindow), KeyCode.S, ShortcutModifiers.Alt)]
        static void CreateSubtract(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SubtractNode>();
        }

        [Shortcut("ShaderGraph/Add Node: UV", typeof(MaterialGraphEditWindow), KeyCode.U, ShortcutModifiers.Alt)]
        static void CreateUV(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<UVNode>();
        }

        [Shortcut("ShaderGraph/Add Node: One Minus", typeof(MaterialGraphEditWindow), KeyCode.I, ShortcutModifiers.Alt)]
        static void CreateOneMinus(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<OneMinusNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Branch", typeof(MaterialGraphEditWindow), KeyCode.Y, ShortcutModifiers.Alt)]
        static void CreateBranch(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<BranchNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Divide", typeof(MaterialGraphEditWindow), KeyCode.D, ShortcutModifiers.Alt)]
        static void CreateDivide(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<DivideNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Combine", typeof(MaterialGraphEditWindow), KeyCode.K, ShortcutModifiers.Alt)]
        static void CreateCombine(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<CombineNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Power", typeof(MaterialGraphEditWindow), KeyCode.P, ShortcutModifiers.Alt)]
        static void CreatePower(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<PowerNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Saturate", typeof(MaterialGraphEditWindow), KeyCode.Q, ShortcutModifiers.Alt)]
        static void CreateSaturate(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SaturateNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Remap", typeof(MaterialGraphEditWindow), KeyCode.R, ShortcutModifiers.Alt)]
        static void CreateRemap(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<RemapNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Normal Vector", typeof(MaterialGraphEditWindow), KeyCode.N, ShortcutModifiers.Alt)]
        static void CreateNormalVector(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<NormalVectorNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Color", typeof(MaterialGraphEditWindow), KeyCode.C, ShortcutModifiers.Alt)]
        static void CreateColor(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<ColorNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Blend", typeof(MaterialGraphEditWindow), KeyCode.B, ShortcutModifiers.Alt)]
        static void CreateBlend(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<BlendNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Step", typeof(MaterialGraphEditWindow), KeyCode.J, ShortcutModifiers.Alt)]
        static void CreateStep(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<StepNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Clamp", typeof(MaterialGraphEditWindow), KeyCode.Equals, ShortcutModifiers.Alt)]
        static void CreateClamp(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<ClampNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Smoothstep", typeof(MaterialGraphEditWindow), KeyCode.BackQuote, ShortcutModifiers.Alt)]
        static void CreateSmoothstep(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SmoothstepNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Fresnel", typeof(MaterialGraphEditWindow), KeyCode.F, ShortcutModifiers.Alt)]
        static void CreateFresnel(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<FresnelNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Custom Function", typeof(MaterialGraphEditWindow), KeyCode.Semicolon, ShortcutModifiers.Alt)]
        static void CreateCFN(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<CustomFunctionNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Dot Product", typeof(MaterialGraphEditWindow), KeyCode.Period, ShortcutModifiers.Alt)]
        static void CreateDotProduct(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<DotProductNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Normalize", typeof(MaterialGraphEditWindow), KeyCode.Z, ShortcutModifiers.Alt)]
        static void CreateNormalize(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<NormalizeNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Absolute", typeof(MaterialGraphEditWindow), KeyCode.Backslash, ShortcutModifiers.Alt)]
        static void CreateAbsolute(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<AbsoluteNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Negate", typeof(MaterialGraphEditWindow), KeyCode.Minus, ShortcutModifiers.Alt)]
        static void CreateNegate(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<NegateNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Fraction", typeof(MaterialGraphEditWindow), KeyCode.Slash, ShortcutModifiers.Alt)]
        static void CreateFraction(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<FractionNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Swizzle", typeof(MaterialGraphEditWindow), KeyCode.W, ShortcutModifiers.Alt)]
        static void CreateSwizzle(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<SwizzleNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Gradient", typeof(MaterialGraphEditWindow), KeyCode.G, ShortcutModifiers.Alt)]
        static void CreateGradient(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<GradientNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Cross Product", typeof(MaterialGraphEditWindow), KeyCode.H, ShortcutModifiers.Alt)]
        static void CreateCrossProduct(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<CrossProductNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Boolean", typeof(MaterialGraphEditWindow), KeyCode.Alpha0, ShortcutModifiers.Alt)]
        static void CreateBoolean(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<BooleanNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Floor", typeof(MaterialGraphEditWindow), KeyCode.LeftBracket, ShortcutModifiers.Alt)]
        static void CreateFloor(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<FloorNode>();
        }

        [Shortcut("ShaderGraph/Add Node: Ceiling", typeof(MaterialGraphEditWindow), KeyCode.RightBracket, ShortcutModifiers.Alt)]
        static void CreateCeiling(ShortcutArguments args)
        {
            CheckBindings(MethodInfo.GetCurrentMethod().GetCustomAttribute<ShortcutAttribute>().displayName);
            CreateNode<CeilingNode>();
        }
        #endregion
    }
}


