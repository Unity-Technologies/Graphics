using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.VFX;
using UnityEngine.VFX;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Profiling;
using System.Reflection;
using UnityEditor.VersionControl;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    /// <summary>
    /// Unexpected public API VFXViewModicationProcessor : Use a custom UnityEditor.AssetModificationProcessor.
    /// </summary>
    [Obsolete("Unexpected public API VFXViewModicationProcessor : Use a custom UnityEditor.AssetModificationProcessor")]
    public class VFXViewModicationProcessor : UnityEditor.AssetModificationProcessor
    {
        /// <summary>
        /// Initialized to false by default.
        /// Obsolete API : Use a custom UnityEditor.AssetModificationProcessor and implement OnWillMoveAsset if you relied on this behavior.
        /// </summary>
        public static bool assetMoved = false;
    }

    class VFXViewModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        public static bool assetMoved = false;

        private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
        {
            assetMoved = true;
            return AssetMoveResult.DidNotMove;
        }
    }

    class EdgeDragInfo : VisualElement
    {
        VFXView m_View;
        public EdgeDragInfo(VFXView view)
        {
            m_View = view;
            var tpl = Resources.Load<VisualTreeAsset>("uxml/EdgeDragInfo");
            tpl.CloneTree(this);

            this.AddStyleSheetPath("EdgeDragInfo");

            m_Text = this.Q<Label>("title");

            pickingMode = PickingMode.Ignore;
            m_Text.pickingMode = PickingMode.Ignore;
        }

        Label m_Text;

        public void StartEdgeDragInfo(VFXDataAnchor draggedAnchor, VFXDataAnchor overAnchor)
        {
            string error = null;
            if (draggedAnchor != overAnchor)
            {
                if (draggedAnchor.direction == overAnchor.direction)
                {
                    if (draggedAnchor.direction == Direction.Input)
                        error = "You must link an input to an output";
                    else
                        error = "You must link an output to an input";
                }
                else if (draggedAnchor.controller.connections.Any(t => draggedAnchor.direction == Direction.Input ? t.output == overAnchor.controller : t.input == overAnchor.controller))
                {
                    error = "An edge with the same input and output already exists";
                }
                else if (!draggedAnchor.controller.model.CanLink(overAnchor.controller.model))
                {
                    error = "The input and output have incompatible types";
                }
                else
                {
                    bool can = draggedAnchor.controller.CanLink(overAnchor.controller);

                    if( !can )
                    {
                        if( ! draggedAnchor.controller.CanLinkToNode(overAnchor.controller.sourceNode,null))
                            error = "The edge would create a loop in the operators";
                        else
                            error = "Link impossible for an unknown reason";
                    }

                    
                }
            }
            if (error == null)
                style.display = DisplayStyle.None;
            else
                m_Text.text = error;

            var layout = overAnchor.connector.parent.ChangeCoordinatesTo(m_View, overAnchor.connector.layout);

            style.top = layout.yMax + 16;
            style.left = layout.xMax;
        }
    }

    class VFXView : GraphView, IControlledElement<VFXViewController>, IControllerListener
    {
        public HashSet<VFXEditableDataAnchor> allDataAnchors = new HashSet<VFXEditableDataAnchor>();

        void IControllerListener.OnControllerEvent(ControllerEvent e)
        {
            if (e is VFXRecompileEvent)
            {
                var recompileEvent = e as VFXRecompileEvent;
                foreach (var anchor in allDataAnchors)
                {
                    anchor.OnRecompile(recompileEvent.valueOnly);
                }
            }
        }

        VisualElement m_NoAssetLabel;
        VisualElement m_LockedElement;

        VFXViewController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }

        public Button checkoutButton;

        void DisconnectController()
        {
            if (controller.model && controller.graph)
                controller.graph.SetCompilationMode(VFXViewPreference.forceEditionCompilation ? VFXCompilationMode.Edition : VFXCompilationMode.Runtime);


            m_Controller.UnregisterHandler(this);
            m_Controller.useCount--;

            serializeGraphElements = null;
            unserializeAndPaste = null;
            deleteSelection = null;
            nodeCreationRequest = null;

            elementsAddedToGroup = null;
            elementsRemovedFromGroup = null;
            groupTitleChanged = null;

            m_GeometrySet = false;

            // Remove all in view now that the controller has been disconnected.
            foreach (var element in rootGroupNodeElements.Values)
            {
                RemoveElement(element);
            }
            foreach (var element in groupNodes.Values)
            {
                RemoveElement(element);
            }
            foreach (var element in dataEdges.Values)
            {
                RemoveElement(element);
            }
            foreach (var element in flowEdges.Values)
            {
                RemoveElement(element);
            }
            foreach (var system in m_Systems)
            {
                RemoveElement(system);
            }

            groupNodes.Clear();
            stickyNotes.Clear();
            rootNodes.Clear();
            rootGroupNodeElements.Clear();
            m_Systems.Clear();
            VFXExpression.ClearCache();
            m_NodeProvider = null;

            if (m_Controller.graph)
            {
                m_Controller.graph.errorManager.onClearAllErrors -= ClearAllErrors;
                m_Controller.graph.errorManager.onRegisterError -= RegisterError;
            }
        }

        void ConnectController()
        {
            schedule.Execute(() =>
            {
                if (controller != null && controller.graph)
                    controller.graph.SetCompilationMode(m_IsRuntimeMode ? VFXCompilationMode.Runtime : VFXCompilationMode.Edition);
            }).ExecuteLater(1);

            if (m_Controller.graph)
            {
                m_Controller.graph.errorManager.onClearAllErrors += ClearAllErrors;
                m_Controller.graph.errorManager.onRegisterError += RegisterError;
            }

            m_Controller.RegisterHandler(this);
            m_Controller.useCount++;

            serializeGraphElements = SerializeElements;
            unserializeAndPaste = UnserializeAndPasteElements;
            deleteSelection = Delete;
            nodeCreationRequest = OnCreateNode;

            elementsAddedToGroup = ElementAddedToGroupNode;
            elementsRemovedFromGroup = ElementRemovedFromGroupNode;
            groupTitleChanged = GroupNodeTitleChanged;

            m_NodeProvider = new VFXNodeProvider(controller, (d, mPos) => AddNode(d, mPos), null, GetAcceptedTypeNodes());


            //Make sure a subgraph block as a block subgraph  context
            if (controller.model.isSubgraph && controller.model.subgraph is VisualEffectSubgraphBlock)
            {
                if (!controller.graph.children.Any(t => t is VFXBlockSubgraphContext))
                {
                    controller.graph.AddChild(VFXBlockSubgraphContext.CreateInstance<VFXBlockSubgraphContext>(), 0);
                }
            }
        }

        IEnumerable<Type> GetAcceptedTypeNodes()
        {
            if (!controller.model.isSubgraph)
                return null;
            return new Type[] { typeof(VFXOperator) };
        }

        public VisualEffect attachedComponent
        {
            get
            {
                return m_ComponentBoard.GetAttachedComponent();
            }

            set
            {
                if (m_ComponentBoard.parent == null)
                    m_ToggleComponentBoard.value = true;

                if (value == null)
                    m_ComponentBoard.Detach();
                else
                    m_ComponentBoard.Attach(value);
            }
        }

        public void RemoveAnchorEdges(VFXDataAnchor anchor)
        {
            foreach (var edge in dataEdges.Where(t => t.Value.input == anchor || t.Value.output == anchor).ToArray())
            {
                if (edge.Value.input == anchor)
                    edge.Value.output.Disconnect(edge.Value);
                else
                    edge.Value.input.Disconnect(edge.Value);

                RemoveElement(edge.Value);
                dataEdges.Remove(edge.Key);
            }
        }

        public void RemoveNodeEdges(VFXNodeUI node)
        {
            foreach (var edge in dataEdges.Where(t => t.Value.input.node == node || t.Value.output.node == node).ToArray())
            {
                RemoveElement(edge.Value);
                dataEdges.Remove(edge.Key);
            }
        }

        public VFXViewController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        DisconnectController();
                    }
                    m_Controller = value;
                    if (m_Controller != null)
                    {
                        ConnectController();
                    }
                    NewControllerSet();
                }
            }
        }

        public VFXGroupNode GetPickedGroupNode(Vector2 panelPosition)
        {
            List<VisualElement> picked = new List<VisualElement>();
            panel.PickAll(panelPosition, picked);

            return picked.OfType<VFXGroupNode>().FirstOrDefault();
        }

        public VFXNodeController AddNode(VFXNodeProvider.Descriptor d, Vector2 mPos)
        {

            UpdateSelectionWithNewNode();
            var groupNode = GetPickedGroupNode(mPos);

            mPos = this.ChangeCoordinatesTo(contentViewContainer, mPos);


            if (d.modelDescriptor is string)
            {
                string path = d.modelDescriptor as string;

                if (path.StartsWith(VisualEffectAssetEditorUtility.templatePath) || ((VFXResources.defaultResources.userTemplateDirectory.Length > 0) && path.StartsWith(VFXResources.defaultResources.userTemplateDirectory)))
                    CreateTemplateSystem(path, mPos, groupNode);
                else
                {
                    if (Path.GetExtension(path) == VisualEffectSubgraphOperator.Extension)
                    {
                        var subGraph = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraphOperator>(path);
                        if (subGraph != null && (!controller.model.isSubgraph || !subGraph.GetResource().GetOrCreateGraph().subgraphDependencies.Contains(controller.model.subgraph) && subGraph.GetResource() != controller.model))
                        {
                            ;
                            VFXModel newModel = VFXSubgraphOperator.CreateInstance<VFXSubgraphOperator>() as VFXModel;

                            controller.AddVFXModel(mPos, newModel);

                            newModel.SetSettingValue("m_Subgraph", subGraph);

                            UpdateSelectionWithNewNode();

                            controller.LightApplyChanges();

                            return controller.GetNewNodeController(newModel);
                        }
                    }
                }
            }
            else if (d.modelDescriptor is GroupNodeAdder)
            {
                controller.AddGroupNode(mPos);
            }
            else if (d.modelDescriptor is VFXParameterController)
            {
                var parameter = d.modelDescriptor as VFXParameterController;

                UpdateSelectionWithNewNode();
                return controller.AddVFXParameter(mPos, parameter, groupNode != null ? groupNode.controller : null);
            }
            else
            {
                UpdateSelectionWithNewNode();
                return controller.AddNode(mPos, d.modelDescriptor, groupNode != null ? groupNode.controller : null);
            }
            return null;
        }

        VFXNodeProvider m_NodeProvider;
        VisualElement m_Toolbar;
        ToolbarButton m_SaveButton;

        private bool m_IsRuntimeMode = false;
        private bool m_ForceShaderValidation = false;


        public static StyleSheet LoadStyleSheet(string text)
        {
            string path = string.Format("{0}/Editor Default Resources/uss/{1}.uss", VisualEffectGraphPackageInfo.assetPackagePath, text);
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
        }

        public static VisualTreeAsset LoadUXML(string text)
        {
            string path = string.Format("{0}/Editor Default Resources/uxml/{1}.uxml", VisualEffectGraphPackageInfo.assetPackagePath, text);
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }

        public static Texture2D LoadImage(string text)
        {
            string path = string.Format("{0}/Editor Default Resources/VFX/{1}.png", VisualEffectGraphPackageInfo.assetPackagePath, text);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        SelectionDragger m_SelectionDragger;
        RectangleSelector m_RectangleSelector;

        public void OnCreateAsset()
        {
            string filePath = EditorUtility.SaveFilePanelInProject("", "New Graph", "vfx", "Create new VisualEffect Graph");
            if (!string.IsNullOrEmpty(filePath))
            {
                VisualEffectAssetEditorUtility.CreateTemplateAsset(filePath);

                VFXViewWindow.currentWindow.LoadAsset(AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(filePath), null);
            }
        }

        public VFXView()
        {
            SetupZoom(0.125f, 8);

            this.AddManipulator(new ContentDragger());
            m_SelectionDragger = new SelectionDragger();
            m_RectangleSelector = new RectangleSelector();
            this.AddManipulator(m_SelectionDragger);
            this.AddManipulator(m_RectangleSelector);
            this.AddManipulator(new FreehandSelector());

            styleSheets.Add(LoadStyleSheet("VFXView"));
            if( ! EditorGUIUtility.isProSkin)
            { 
                styleSheets.Add(LoadStyleSheet("VFXView-light"));
            }
            else
            {
                styleSheets.Add(LoadStyleSheet("VFXView-dark"));
            }

            AddLayer(-1);
            AddLayer(1);
            AddLayer(2);

            focusable = true;

            m_Toolbar = new UnityEditor.UIElements.Toolbar();

            var toggleAutoCompile = new ToolbarToggle();
            toggleAutoCompile.text = "Auto";
            toggleAutoCompile.style.unityTextAlign = TextAnchor.MiddleRight;
            toggleAutoCompile.SetValueWithoutNotify(true);
            toggleAutoCompile.RegisterCallback<ChangeEvent<bool>>(OnToggleCompile);
            m_Toolbar.Add(toggleAutoCompile);

            var compileButton = new ToolbarButton(OnCompile);
            compileButton.style.unityTextAlign = TextAnchor.MiddleLeft;
            compileButton.text = "Compile";
            m_Toolbar.Add(compileButton);

            m_SaveButton = new ToolbarButton(OnSave);
            m_SaveButton.style.unityTextAlign = TextAnchor.MiddleLeft;
            m_SaveButton.text = "Save";
            m_Toolbar.Add(m_SaveButton);

            var spacer = new ToolbarSpacer();
            spacer.style.width = 12f;
            m_Toolbar.Add(spacer);

            var selectAssetButton = new ToolbarButton(() => { SelectAsset(); });
            selectAssetButton.text = "Show in Project";
            m_Toolbar.Add(selectAssetButton);

            spacer = new ToolbarSpacer();
            spacer.style.width = 10;
            m_Toolbar.Add(spacer);

            checkoutButton = new ToolbarButton(() => { Checkout(); });
            checkoutButton.text = "Check Out";
            checkoutButton.visible = false;
            checkoutButton.AddToClassList("toolbarItem");
            m_Toolbar.Add(checkoutButton);

            var flexSpacer = new ToolbarSpacer();
            flexSpacer.style.flexGrow = 1f;
            m_Toolbar.Add(flexSpacer);

            var toggleBlackboard = new ToolbarToggle();
            toggleBlackboard.text = "Blackboard";
            toggleBlackboard.RegisterCallback<ChangeEvent<bool>>(ToggleBlackboard);
            m_Toolbar.Add(toggleBlackboard);

            m_ToggleComponentBoard = new ToolbarToggle();
            m_ToggleComponentBoard.text = "Target GameObject";
            m_ToggleComponentBoard.RegisterCallback<ChangeEvent<bool>>(ToggleComponentBoard);
            m_Toolbar.Add(m_ToggleComponentBoard);

            var showDebugMenu = new ToolbarMenu();
            showDebugMenu.text = "Advanced";
            showDebugMenu.menu.AppendAction("Runtime Mode (Forced)", OnRuntimeModeChanged, RuntimeModeStatus);
            showDebugMenu.menu.AppendAction("Shader Validation (Forced)", OnShaderValidationChanged, ShaderValidationStatus);
            showDebugMenu.menu.AppendSeparator();
            showDebugMenu.menu.AppendAction("Refresh UI", OnRefreshUI, DropdownMenuAction.Status.Normal);
            m_Toolbar.Add(showDebugMenu);

            // End Toolbar

            m_NoAssetLabel = new Label("\n\n\nTo begin creating Visual Effects, create a new Visual Effect Graph Asset.\n(or double-click an existing Visual Effect Graph in the project view)") { name = "no-asset"};
            m_NoAssetLabel.style.position = PositionType.Absolute;
            m_NoAssetLabel.style.left = new StyleLength(40f);
            m_NoAssetLabel.style.right = new StyleLength(40f);
            m_NoAssetLabel.style.top = new StyleLength(40f);
            m_NoAssetLabel.style.bottom = new StyleLength(140f);
            m_NoAssetLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_NoAssetLabel.style.fontSize = new StyleLength(12f);
            m_NoAssetLabel.style.color = Color.white * 0.75f;
            Add(m_NoAssetLabel);


            var createButton = new Button() { text = "Create new Visual Effect Graph" };
            m_NoAssetLabel.Add(createButton);
            createButton.clicked += OnCreateAsset;

            m_LockedElement = new Label("Asset is Locked");
            m_LockedElement.style.position = PositionType.Absolute;
            m_LockedElement.style.left = 0f;
            m_LockedElement.style.right = new StyleLength(0f);
            m_LockedElement.style.top = new StyleLength(0f);
            m_LockedElement.style.bottom = new StyleLength(0f);
            m_LockedElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            m_LockedElement.style.fontSize = new StyleLength(72f);
            m_LockedElement.style.color = Color.white * 0.75f;
            m_LockedElement.style.display = DisplayStyle.None;
            m_LockedElement.focusable = true;
            //m_LockedElement.RegisterCallback<MouseDownEvent>(e => e.StopPropagation());
            m_LockedElement.RegisterCallback<KeyDownEvent>(e => e.StopPropagation());


            m_Blackboard = new VFXBlackboard(this);
            bool blackboardVisible = BoardPreferenceHelper.IsVisible(BoardPreferenceHelper.Board.blackboard, true);
            if (blackboardVisible)
                Add(m_Blackboard);
            toggleBlackboard.value = blackboardVisible;

            m_ComponentBoard = new VFXComponentBoard(this);
#if _ENABLE_RESTORE_BOARD_VISIBILITY
            bool componentBoardVisible = BoardPreferenceHelper.IsVisible(BoardPreferenceHelper.Board.componentBoard, false);
            if (componentBoardVisible)
                ShowComponentBoard();
            toggleComponentBoard.value = componentBoardVisible;
#endif

            Add(m_LockedElement);
            Add(m_Toolbar);
            m_Toolbar.SetEnabled(false);

            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<ValidateCommandEvent>(ValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(ExecuteCommand);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            graphViewChanged = VFXGraphViewChanged;

            elementResized = VFXElementResized;

            viewDataKey = "VFXView";

            RegisterCallback<GeometryChangedEvent>(OnFirstResize);
        }

        void OnRefreshUI(DropdownMenuAction action)
        {
            Resync();
        }

        void OnRuntimeModeChanged(DropdownMenuAction action)
        {
            m_IsRuntimeMode = !m_IsRuntimeMode;
            controller.graph.SetCompilationMode(m_IsRuntimeMode ? VFXCompilationMode.Runtime : VFXCompilationMode.Edition);
        }

        DropdownMenuAction.Status RuntimeModeStatus(DropdownMenuAction action)
        {
            if (m_IsRuntimeMode)
                return DropdownMenuAction.Status.Checked;
            else
                return DropdownMenuAction.Status.Normal;
        }

        void OnShaderValidationChanged(DropdownMenuAction action)
        {
            m_ForceShaderValidation = !m_ForceShaderValidation;
            controller.graph.SetForceShaderValidation(m_ForceShaderValidation);
        }

        DropdownMenuAction.Status ShaderValidationStatus(DropdownMenuAction action)
        {
            if (VFXGraphCompiledData.k_FnVFXResource_SetCompileInitialVariants == null)
                return DropdownMenuAction.Status.Disabled;
            else if (m_ForceShaderValidation)
                return DropdownMenuAction.Status.Checked;
            else
                return DropdownMenuAction.Status.Normal;
        }

        [NonSerialized]
        Dictionary<VFXModel, List<IconBadge>> m_InvalidateBadges = new Dictionary<VFXModel, List<IconBadge>>();

        [NonSerialized]
        List<IconBadge> m_CompileBadges = new List<IconBadge>();

        private void RegisterError(VFXModel model, VFXErrorOrigin errorOrigin,string error,VFXErrorType type, string description)
        {
            VisualElement target = null;
            VisualElement targetParent = null;
            SpriteAlignment alignement = SpriteAlignment.TopLeft;

            if (model is VFXSlot)
            {
                var slot = (VFXSlot)model;
                // todo manage parameter slot if they can have error

                var nodeController = controller.GetNodeController(slot.owner as VFXModel, 0);
                if (nodeController == null)
                    return;
                var anchorController = (slot.direction == VFXSlot.Direction.kInput ? nodeController.inputPorts : nodeController.outputPorts).FirstOrDefault(t => t.model == slot);
                if (anchorController == null)
                    return;

                targetParent = GetNodeByController(nodeController);
                target = (targetParent as VFXNodeUI).GetPorts(slot.direction == VFXSlot.Direction.kInput, slot.direction != VFXSlot.Direction.kInput).FirstOrDefault(t => t.controller == anchorController);
                alignement = slot.direction == VFXSlot.Direction.kInput ? SpriteAlignment.LeftCenter : SpriteAlignment.RightCenter;
            }
            else if (model is IVFXSlotContainer)
            {
                var node = model;
                var nodeController = controller.GetNodeController(node, 0);
                if (nodeController == null)
                    return;
                target = GetNodeByController(nodeController);
                if (target == null)
                    return;
                if (nodeController is VFXBlockController blkController)
                {
                    VFXNodeUI targetContext = GetNodeByController(blkController.contextController);
                    if (targetContext == null)
                        return;
                    targetParent = targetContext.parent;
                }
                else
                {
                    targetParent = target.parent;
                }
                target = (target as VFXNodeUI).titleContainer;
                alignement = SpriteAlignment.LeftCenter;
            }

            if (target != null && targetParent != null)
            {
                var badge = type == VFXErrorType.Error ? IconBadge.CreateError(description) : IconBadge.CreateComment(description);
                targetParent.Add(badge);
                badge.AttachTo(target, alignement);

                if(errorOrigin == VFXErrorOrigin.Compilation)
                {
                    m_CompileBadges.Add(badge);
                }
                else
                {
                    List<IconBadge> badges;
                    if (!m_InvalidateBadges.TryGetValue(model, out badges))
                    {
                        badges = new List<IconBadge>();
                        m_InvalidateBadges[model] = badges;
                    }
                    badges.Add(badge);

                }
                badge.AddManipulator(new Clickable(() =>
                {
                    badge.Detach();
                    badge.RemoveFromHierarchy();
                }));
                badge.AddManipulator(new DownClickable(()=>
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(EditorGUIUtility.TrTextContent("Hide"), false, () =>
                    {
                        badge.Detach();
                        badge.RemoveFromHierarchy();
                    });

                    if (type != VFXErrorType.Error)
                    {
                        menu.AddItem(EditorGUIUtility.TrTextContent("Ignore"), false, () =>
                        {
                            badge.Detach();
                            badge.RemoveFromHierarchy();
                            model.IgnoreError(error);
                        });
                    }
                    menu.ShowAsContext();
                }
                ));
            }
        }

        private void ClearAllErrors(VFXModel model, VFXErrorOrigin errorOrigin)
        {
            if (errorOrigin == VFXErrorOrigin.Compilation )
            {
                foreach (var badge in m_CompileBadges)
                {
                    badge.Detach();
                    badge.RemoveFromHierarchy();
                }
                m_CompileBadges.Clear();
            }
            else
            {
                if (!object.ReferenceEquals(model,null))
                {
                    List<IconBadge> badges;
                    if (m_InvalidateBadges.TryGetValue(model, out badges))
                    {
                        foreach (var badge in badges)
                        {
                            badge.Detach();
                            badge.RemoveFromHierarchy();
                        }
                        m_InvalidateBadges.Remove(model);
                    }
                }
                else
                    throw new InvalidOperationException("Can't clear in Invalidate mode without a model");

            }
        }

        public void SetBoardToFront(GraphElement board)
        {
            board.SendToBack();
            board.PlaceBehind(m_Toolbar);
        }

        void OnUndoPerformed()
        {
            foreach (var anchor in allDataAnchors)
            {
                anchor.ForceUpdate();
            }
        }

        void ToggleBlackboard(ChangeEvent<bool> e)
        {
            if (m_Blackboard.parent == null)
            {
                Insert(childCount - 1, m_Blackboard);
                BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
                m_Blackboard.RegisterCallback<GeometryChangedEvent>(OnFirstBlackboardGeometryChanged);
                m_Blackboard.style.position = PositionType.Absolute;
            }
            else
            {
                m_Blackboard.RemoveFromHierarchy();
                BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, false);
            }
        }

        void ToggleComponentBoard()
        {
            if (m_ComponentBoard.parent == null)
            {
                Insert(childCount - 1, m_ComponentBoard);
                BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.componentBoard, true);
                m_ComponentBoard.RegisterCallback<GeometryChangedEvent>(OnFirstComponentBoardGeometryChanged);
            }
            else
            {
                m_ComponentBoard.RemoveFromHierarchy();
                BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.componentBoard, false);
            }
        }

        void OnFirstComponentBoardGeometryChanged(GeometryChangedEvent e)
        {
            if (m_FirstResize)
            {
                m_ComponentBoard.ValidatePosition();
                m_ComponentBoard.UnregisterCallback<GeometryChangedEvent>(OnFirstComponentBoardGeometryChanged);
            }
        }

        void OnFirstBlackboardGeometryChanged(GeometryChangedEvent e)
        {
            if (m_FirstResize)
            {
                m_Blackboard.ValidatePosition();
                m_Blackboard.UnregisterCallback<GeometryChangedEvent>(OnFirstBlackboardGeometryChanged);
            }
        }

        public bool m_FirstResize = false;

        void OnFirstResize(GeometryChangedEvent e)
        {
            m_FirstResize = true;
            m_ComponentBoard.ValidatePosition();
            m_Blackboard.ValidatePosition();
            UnregisterCallback<GeometryChangedEvent>(OnFirstResize);
        }

        Toggle m_ToggleComponentBoard;
        void ToggleComponentBoard(ChangeEvent<bool> e)
        {
            ToggleComponentBoard();
        }

        public void OnVisualEffectComponentChanged(IEnumerable<VisualEffect> visualEffects)
        {
            m_ComponentBoard.OnVisualEffectComponentChanged(visualEffects);
        }

        void Delete(string cmd, AskUser askUser)
        {
            var selection = this.selection.ToArray();
            var parametersToRemove = Enumerable.Empty<VFXParameterController>();
            foreach (var category in selection.OfType<VFXBlackboardCategory>())
            {
                parametersToRemove = parametersToRemove.Concat(controller.RemoveCategory(m_Blackboard.GetCategoryIndex(category)));
            }
            controller.Remove(selection.OfType<IControlledElement>().Select(t => t.controller).Concat(parametersToRemove.Cast<Controller>()), true);
        }

        void IControlledElement.OnControllerChanged(ref ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                ControllerChanged(e.change);
            }
            else if (e.controller is VFXNodeController)
            {
                UpdateUIBounds();
                if (e.controller is VFXContextController && e.target is VFXContextUI)
                {
                    m_ComponentBoard.UpdateEventList();
                    UpdateSystemNames();
                }
            }
        }

        bool m_InControllerChanged;
        void ControllerChanged(int change)
        {
            if (change == VFXViewController.Change.assetName)
                return;

            m_InControllerChanged = true;

            if (change == VFXViewController.Change.groupNode)
            {
                Profiler.BeginSample("VFXView.SyncGroupNodes");
                SyncGroupNodes();
                Profiler.EndSample();

                var groupNodes = this.groupNodes;
                foreach (var groupNode in groupNodes.Values)
                {
                    Profiler.BeginSample("VFXGroupNode.SelfChange");
                    groupNode.SelfChange();
                    Profiler.EndSample();
                }
                return;
            }
            if (change == VFXViewController.Change.destroy)
            {
                m_Blackboard.controller = null;
                controller = null;
                return;
            }
            Profiler.BeginSample("VFXView.ControllerChanged");
            if (change == VFXViewController.AnyThing)
            {
                SyncNodes();
            }

            Profiler.BeginSample("VFXView.SyncStickyNotes");
            SyncStickyNotes();
            Profiler.EndSample();
            Profiler.BeginSample("VFXView.SyncEdges");
            SyncEdges(change);
            Profiler.EndSample();
            Profiler.BeginSample("VFXView.SyncGroupNodes");
            SyncGroupNodes();
            Profiler.EndSample();

            if (controller != null)
            {
                if (change == VFXViewController.AnyThing)
                {
                    // if the asset is destroyed somehow, fox example if the user delete the asset, update the controller and update the window.
                    var asset = controller.model;
                    if (asset == null)
                    {
                        this.controller = null;
                        return;
                    }
                }
            }

            m_InControllerChanged = false;
            if (change != VFXViewController.Change.dataEdge)
                UpdateSystems();

            if (m_UpdateUIBounds)
            {
                Profiler.BeginSample("VFXView.UpdateUIBounds");
                UpdateUIBounds();
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        public bool IsAssetEditable()
        {
            return controller.model != null && controller.model.IsAssetEditable();
        }

        void NewControllerSet()
        {
            m_Blackboard.controller = controller;
            m_ComponentBoard.controller = controller;

            if (controller != null)
            {
                m_NoAssetLabel.RemoveFromHierarchy();
                m_Toolbar.SetEnabled(true);

                if (IsAssetEditable())
                {
                    m_LockedElement.style.display = DisplayStyle.None;
                    m_Blackboard.UnlockUI();
                }
                else
                {
                    m_LockedElement.style.display = DisplayStyle.Flex;
                    m_Blackboard.LockUI();
                }
            }
            else
            {
                if (m_NoAssetLabel.parent == null)
                {
                    Add(m_NoAssetLabel);
                    m_Toolbar.SetEnabled(false);
                }
            }
        }

        public void OnFocus()
        {
            if (controller != null && controller.model.asset != null && !IsAssetEditable())
            {
                if (m_LockedElement.style.display != DisplayStyle.Flex)
                {
                    m_LockedElement.style.display = DisplayStyle.Flex;
                    this.RemoveManipulator(m_SelectionDragger);
                    this.RemoveManipulator(m_RectangleSelector);
                    m_LockedElement.Focus();
                }
                m_Blackboard.LockUI();
            }
            else
            {
                if (m_LockedElement.style.display != DisplayStyle.None)
                {
                    m_LockedElement.style.display = DisplayStyle.None;
                    this.AddManipulator(m_SelectionDragger);
                    this.AddManipulator(m_RectangleSelector);
                }
                m_Blackboard.UnlockUI();
            }

            if (m_Blackboard.parent != null)
                m_LockedElement.PlaceInFront(contentViewContainer);
        }

        public void FrameNewController()
        {
            if (panel != null)
            {
                FrameAfterAWhile();
            }
            else
            {
                RegisterCallback<GeometryChangedEvent>(OnFrameNewControllerWithPanel);
            }
        }

        void FrameAfterAWhile()
        {


            var rectToFit = contentViewContainer.layout;
            var frameTranslation = Vector3.zero;
            var frameScaling = Vector3.one;

            rectToFit = controller.graph.UIInfos.uiBounds;

            if (rectToFit.width <= 50 || rectToFit.height <= 50)
            {
                return;
            }

            Rect rectAvailable = layout;

            float validateFloat = rectAvailable.x + rectAvailable.y + rectAvailable.width + rectAvailable.height;
            if (float.IsInfinity(validateFloat) || float.IsNaN(validateFloat))
            {
                schedule.Execute(FrameAfterAWhile);
                return;
            }

            CalculateFrameTransform(rectToFit, rectAvailable, 30, out frameTranslation, out frameScaling);

            Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);

            UpdateViewTransform(frameTranslation, frameScaling);

            contentViewContainer.MarkDirtyRepaint();
        }

        bool m_GeometrySet = false;

        void OnFrameNewControllerWithPanel(GeometryChangedEvent e)
        {
            m_GeometrySet = true;
            FrameAfterAWhile();
            UnregisterCallback<GeometryChangedEvent>(OnFrameNewControllerWithPanel);
        }

        Dictionary<VFXNodeController, VFXNodeUI> rootNodes = new Dictionary<VFXNodeController, VFXNodeUI>();
        Dictionary<Controller, GraphElement> rootGroupNodeElements = new Dictionary<Controller, GraphElement>();


        public GraphElement GetGroupNodeElement(Controller controller)
        {
            GraphElement result = null;
            rootGroupNodeElements.TryGetValue(controller, out result);
            return result;
        }

        Dictionary<VFXGroupNodeController, VFXGroupNode> groupNodes = new Dictionary<VFXGroupNodeController, VFXGroupNode>();
        Dictionary<VFXStickyNoteController, VFXStickyNote> stickyNotes = new Dictionary<VFXStickyNoteController, VFXStickyNote>();

        void OnOneNodeGeometryChanged(GeometryChangedEvent e)
        {
            m_GeometrySet = true;
            (e.target as GraphElement).UnregisterCallback<GeometryChangedEvent>(OnOneNodeGeometryChanged);
        }

        bool m_UpdateSelectionWithNewNode;

        public void UpdateSelectionWithNewNode()
        {
            m_UpdateSelectionWithNewNode = true;
        }

        void SyncNodes()
        {
            Profiler.BeginSample("VFXView.SyncNodes");
            if (controller == null)
            {
                foreach (var element in rootNodes.Values.ToArray())
                {
                    SafeRemoveElement(element);
                }
                rootNodes.Clear();
                rootGroupNodeElements.Clear();
            }
            else
            {
                elementsAddedToGroup = null;
                elementsRemovedFromGroup = null;

                Profiler.BeginSample("VFXView.SyncNodes:Delete");
                var deletedControllers = rootNodes.Keys.Except(controller.nodes).ToArray();

                foreach (var deletedController in deletedControllers)
                {
                    SafeRemoveElement(rootNodes[deletedController]);
                    rootNodes.Remove(deletedController);
                    rootGroupNodeElements.Remove(deletedController);
                }
                Profiler.EndSample();
                bool needOneListenToGeometry = !m_GeometrySet;

                Profiler.BeginSample("VFXView.SyncNodes:Create");

                bool selectionCleared = false;

                foreach (var newController in controller.nodes.Except(rootNodes.Keys).ToArray())
                {
                    VFXNodeUI newElement = null;
                    if (newController is VFXContextController)
                    {
                        newElement = new VFXContextUI();
                    }
                    else if (newController is VFXOperatorController)
                    {
                        newElement = new VFXOperatorUI();
                    }
                    else if (newController is VFXParameterNodeController)
                    {
                        newElement = new VFXParameterUI();
                    }
                    else
                    {
                        throw new InvalidOperationException("Can't find right ui for controller" + newController.GetType().Name);
                    }
                    Profiler.BeginSample("VFXView.SyncNodes:AddElement");
                    FastAddElement(newElement);
                    Profiler.EndSample();
                    rootNodes[newController] = newElement;
                    rootGroupNodeElements[newController] = newElement;
                    (newElement as ISettableControlledElement<VFXNodeController>).controller = newController;
                    if (needOneListenToGeometry)
                    {
                        needOneListenToGeometry = false;
                        newElement.RegisterCallback<GeometryChangedEvent>(OnOneNodeGeometryChanged);
                    }
                    newElement.controller.model.RefreshErrors(controller.graph);
                    if (m_UpdateSelectionWithNewNode)
                    {
                        if (!selectionCleared)
                        {
                            selectionCleared = true;
                            ClearSelection();
                        }
                        AddToSelection(newElement);
                    }
                }
                m_UpdateSelectionWithNewNode = false;
                Profiler.EndSample();

                elementsAddedToGroup = ElementAddedToGroupNode;
                elementsRemovedFromGroup = ElementRemovedFromGroupNode;
            }

            Profiler.EndSample();
        }

        static FieldInfo s_Member_ContainerLayer = typeof(GraphView).GetField("m_ContainerLayers", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo s_Method_GetLayer = typeof(GraphView).GetMethod("GetLayer", BindingFlags.NonPublic | BindingFlags.Instance);

        public void FastAddElement(GraphElement graphElement)
        {
            if (graphElement.IsResizable())
            {
                graphElement.hierarchy.Add(new Resizer());
                graphElement.style.borderBottomWidth = 6;
            }

            int newLayer = graphElement.layer;
            if (!(s_Member_ContainerLayer.GetValue(this) as IDictionary).Contains(newLayer))
            {
                AddLayer(newLayer);
            }
            (s_Method_GetLayer.Invoke(this, new object[] { newLayer }) as VisualElement).Add(graphElement);
        }

        bool m_UpdateUIBounds = false;
        void UpdateUIBounds()
        {
            if (!m_GeometrySet) return;
            if (m_InControllerChanged)
            {
                m_UpdateUIBounds = true;
                return;
            }
            m_UpdateUIBounds = false;

            if (panel != null)
            {
                panel.InternalValidateLayout();
                controller.graph.UIInfos.uiBounds = GetElementsBounds(rootGroupNodeElements.Values.Concat(groupNodes.Values.Cast<GraphElement>()));
            }
        }

        void SyncGroupNodes()
        {
            if (controller == null)
            {
                foreach (var kv in groupNodes)
                {
                    RemoveElement(kv.Value);
                }
                groupNodes.Clear();
            }
            else
            {
                var deletedControllers = groupNodes.Keys.Except(controller.groupNodes).ToArray();

                foreach (var deletedController in deletedControllers)
                {
                    RemoveElement(groupNodes[deletedController]);
                    groupNodes.Remove(deletedController);
                }

                foreach (var newController in controller.groupNodes.Except(groupNodes.Keys))
                {
                    var newElement = new VFXGroupNode();
                    FastAddElement(newElement);
                    newElement.controller = newController;
                    groupNodes.Add(newController, newElement);
                }
            }
        }

        void SyncStickyNotes()
        {
            if (controller == null)
            {
                foreach (var kv in stickyNotes)
                {
                    SafeRemoveElement(kv.Value);
                }
                rootGroupNodeElements.Clear();
                stickyNotes.Clear();
            }
            else
            {
                var deletedControllers = stickyNotes.Keys.Except(controller.stickyNotes).ToArray();

                foreach (var deletedController in deletedControllers)
                {
                    SafeRemoveElement(stickyNotes[deletedController]);
                    rootGroupNodeElements.Remove(deletedController);
                    stickyNotes.Remove(deletedController);
                }

                foreach (var newController in controller.stickyNotes.Except(stickyNotes.Keys))
                {
                    var newElement = new VFXStickyNote();
                    newElement.controller = newController;
                    FastAddElement(newElement);
                    rootGroupNodeElements[newController] = newElement;
                    stickyNotes[newController] = newElement;
                }
            }
        }

        public void SafeRemoveElement(GraphElement element)
        {
            VFXGroupNode.inRemoveElement = true;

            RemoveElement(element);

            VFXGroupNode.inRemoveElement = false;
        }

        Dictionary<VFXDataEdgeController, VFXDataEdge> dataEdges = new Dictionary<VFXDataEdgeController, VFXDataEdge>();
        Dictionary<VFXFlowEdgeController, VFXFlowEdge> flowEdges = new Dictionary<VFXFlowEdgeController, VFXFlowEdge>();

        void SyncEdges(int change)
        {
            if (change == VFXViewController.Change.ui)
                return; // for the moment ui changes don't have an impact on edges
            if (change != VFXViewController.Change.flowEdge)
            {
                if (controller == null)
                {
                    foreach (var element in dataEdges.Values)
                    {
                        RemoveElement(element);
                    }
                    dataEdges.Clear();
                }
                else
                {
                    var deletedControllers = dataEdges.Keys.Except(controller.dataEdges).ToArray();

                    foreach (var deletedController in deletedControllers)
                    {
                        var edge = dataEdges[deletedController];
                        if (edge.input != null)
                        {
                            edge.input.Disconnect(edge);
                        }
                        if (edge.output != null)
                        {
                            edge.output.Disconnect(edge);
                        }
                        RemoveElement(edge);
                        dataEdges.Remove(deletedController);
                    }

                    foreach (var newController in controller.dataEdges.Except(dataEdges.Keys).ToArray())
                    {
                        // SyncEdges could be called before the VFXNodeUI have been created, it that case ignore them and trust that they will be created later when the
                        // nodes arrive.
                        if (GetNodeByController(newController.input.sourceNode) == null || GetNodeByController(newController.output.sourceNode) == null)
                        {
                            if (change != VFXViewController.Change.dataEdge)
                            {
                                Debug.LogError("Can't match nodes for a data edge after nodes should have been updated.");
                            }
                            continue;
                        }

                        var newElement = new VFXDataEdge();
                        FastAddElement(newElement);
                        newElement.controller = newController;

                        dataEdges.Add(newController, newElement);
                        if (newElement.input != null)
                            newElement.input.node.RefreshExpandedState();
                        if (newElement.output != null)
                            newElement.output.node.RefreshExpandedState();
                    }
                }
            }

            if (change != VFXViewController.Change.dataEdge)
            {
                if (controller == null)
                {
                    foreach (var element in flowEdges.Values)
                    {
                        RemoveElement(element);
                    }
                    flowEdges.Clear();
                }
                else
                {
                    var deletedControllers = flowEdges.Keys.Except(controller.flowEdges).ToArray();

                    foreach (var deletedController in deletedControllers)
                    {
                        var edge = flowEdges[deletedController];
                        if (edge.input != null)
                        {
                            edge.input.Disconnect(edge);
                        }
                        if (edge.output != null)
                        {
                            edge.output.Disconnect(edge);
                        }
                        RemoveElement(edge);
                        flowEdges.Remove(deletedController);
                    }

                    foreach (var newController in controller.flowEdges.Except(flowEdges.Keys))
                    {
                        var newElement = new VFXFlowEdge();
                        FastAddElement(newElement);
                        newElement.controller = newController;
                        flowEdges.Add(newController, newElement);
                    }
                }
            }
        }

        public Vector2 ScreenToViewPosition(Vector2 position)
        {
            GUIView guiView = panel.InternalGetGUIView();
            if (guiView == null)
                return position;
            return position - guiView.screenPosition.position;
        }

        public Vector2 ViewToScreenPosition(Vector2 position)
        {
            GUIView guiView = panel.InternalGetGUIView();
            if (guiView == null)
                return position;
            return position + guiView.screenPosition.position;
        }

        void OnCreateNode(NodeCreationContext ctx)
        {
            GUIView guiView = panel.InternalGetGUIView();
            if (guiView == null)
                return;
            Vector2 point = ScreenToViewPosition(ctx.screenMousePosition);

            List<VisualElement> picked = new List<VisualElement>();
            panel.PickAll(point, picked);

            VFXContextUI context = picked.OfType<VFXContextUI>().FirstOrDefault();

            if (context != null)
            {
                if(context.canHaveBlocks)
                    context.OnCreateBlock(point);
            }
            else
            {
                VFXDataEdge edge = picked.OfType<VFXDataEdge>().FirstOrDefault();
                if (edge != null)
                    VFXFilterWindow.Show(VFXViewWindow.currentWindow, point, ctx.screenMousePosition, new VFXNodeProvider(controller, (d, v) => AddNodeOnEdge(d, v, edge.controller), null, new Type[] { typeof(VFXOperator) }));
                else
                VFXFilterWindow.Show(VFXViewWindow.currentWindow, point, ctx.screenMousePosition, m_NodeProvider);
            }
        }

        public void CreateTemplateSystem(string path, Vector2 tPos, VFXGroupNode groupNode)
        {
            var resource = VisualEffectResource.GetResourceAtPath(path);
            if (resource != null)
            {
                VFXViewController templateController = VFXViewController.GetController(resource, true);
                templateController.useCount++;

                var data = VFXCopy.SerializeElements(templateController.allChildren, templateController.graph.UIInfos.uiBounds);
                VFXPaste.UnserializeAndPasteElements(controller, tPos, data, this, groupNode != null ? groupNode.controller : null);

                templateController.useCount--;
            }
        }

        void OnToggleCompile(ChangeEvent<bool> e)
        {
            VFXViewWindow.currentWindow.autoCompile = !VFXViewWindow.currentWindow.autoCompile;
        }

        void OnCompile()
        {
            if (controller.model.isSubgraph)
                controller.graph.RecompileIfNeeded(false, false);
            else
            {
                VFXGraph.explicitCompile = true;
                using (var reporter = new VFXCompileErrorReporter(controller.graph.errorManager))
                {
                    VFXGraph.compileReporter = reporter;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(controller.model));
                    VFXGraph.compileReporter = null;
                }
                VFXGraph.explicitCompile = false;
            }
        }

        void OnSave()
        {
            var graphToSave = new HashSet<VFXGraph>();
            GetGraphsRecursively(controller.graph,graphToSave);

            foreach(var graph in graphToSave)
            {
                if (EditorUtility.IsDirty(graph) || UnityEngine.Object.ReferenceEquals(graph, controller.graph))
                {
                    graph.UpdateSubAssets();
                    graph.GetResource().WriteAsset();
                }
            }
        }

        void GetGraphsRecursively(VFXGraph start,HashSet<VFXGraph> graphs)
        {
            if (graphs.Contains(start))
                return;
            graphs.Add(start);
            foreach(var child in start.children)
            {
                if (child is VFXSubgraphOperator ope)
                {
                    if (ope.subgraph != null)
                    {
                        var graph = ope.subgraph.GetResource().GetOrCreateGraph();
                        GetGraphsRecursively(graph, graphs);
                    }
                }
                else if (child is VFXSubgraphContext subCtx)
                {
                    if (subCtx.subgraph != null)
                    {
                        var graph = subCtx.subgraph.GetResource().GetOrCreateGraph();
                        GetGraphsRecursively(graph, graphs);
                    }
                }
                else if( child is VFXContext ctx)
                {
                    foreach( var block in ctx.children.Cast<VFXBlock>())
                    {
                        if( block is VFXSubgraphBlock subBlock)
                        {
                            if( subBlock.subgraph!= null)
                            {
                                var graph = subBlock.subgraph.GetResource().GetOrCreateGraph();
                                GetGraphsRecursively(graph, graphs);
                            }
                        }
                    }
                }
            }
        }

        public EventPropagation Compile()
        {
            OnCompile();

            return EventPropagation.Stop;
        }

        void AddVFXParameter(Vector2 pos, VFXParameterController parameterController, VFXGroupNode groupNode)
        {
            if (controller == null || parameterController == null) return;

            controller.AddVFXParameter(pos, parameterController, groupNode != null ? groupNode.controller : null);

        }

        public EventPropagation Resync()
        {
            foreach (var node in rootNodes.Values)
                node.RemoveFromHierarchy();

            rootNodes.Clear();
            foreach (var node in nodes.ToList())
                node.RemoveFromHierarchy();

            foreach (var edge in dataEdges.Values)
                edge.RemoveFromHierarchy();
            dataEdges.Clear();

            foreach (var edge in flowEdges.Values)
                edge.RemoveFromHierarchy();
            flowEdges.Clear();

            foreach (var edge in edges.ToList())
                edge.RemoveFromHierarchy();

            if (controller != null)
                controller.ForceReload();
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDot()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.None, "expGraph_None.dot");
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotReduced()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.Reduction, "expGraph_Reduction.dot");
            return EventPropagation.Stop;
        }

        public EventPropagation OutputToDotConstantFolding()
        {
            if (controller == null) return EventPropagation.Stop;
            DotGraphOutput.DebugExpressionGraph(controller.graph, VFXExpressionContextOption.ConstantFolding, "expGraph_ConstantFolding.dot");
            return EventPropagation.Stop;
        }

        IEnumerable<VisualEffect> GetActiveComponents()
        {
            if (attachedComponent != null)
                yield return attachedComponent;
            else
            {
                foreach (var component in UnityEngine.VFX.VFXManager.GetComponents())
                    yield return component;
            }
        }

        public EventPropagation ReinitComponents()
        {
            foreach (var component in GetActiveComponents())
                component.Reinit();
            return EventPropagation.Stop;
        }

        public EventPropagation ReinitAndPlayComponents()
        {
            foreach (var component in GetActiveComponents())
            {
                component.Reinit();
                component.Play();
            }
            return EventPropagation.Stop;
        }

        public IEnumerable<VFXContextUI> GetAllContexts()
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer.Children())
                {
                    if (element is VFXContextUI)
                    {
                        yield return element as VFXContextUI;
                    }
                }
            }
        }

        public IEnumerable<VFXNodeUI> GetAllNodes()
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer.Children())
                {
                    if (element is VFXNodeUI)
                    {
                        yield return element as VFXNodeUI;
                    }
                }
            }
        }

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            if (controller == null) return null;


            if (startAnchor is VFXDataAnchor)
            {
                var controllers = controller.GetCompatiblePorts((startAnchor as  VFXDataAnchor).controller, nodeAdapter);
                return controllers.Select(t => (Port)GetDataAnchorByController(t as VFXDataAnchorController)).ToList();
            }
            else
            {
                var controllers = controller.GetCompatiblePorts((startAnchor as VFXFlowAnchor).controller, nodeAdapter);
                return controllers.Select(t => (Port)GetFlowAnchorByController(t as VFXFlowAnchorController)).ToList();
            }
        }

        public IEnumerable<VFXFlowAnchor> GetAllFlowAnchors(bool input, bool output)
        {
            foreach (var context in GetAllContexts())
            {
                foreach (VFXFlowAnchor anchor in context.GetFlowAnchors(input, output))
                {
                    yield return anchor;
                }
            }
        }

        void VFXElementResized(VisualElement element)
        {
            if (element is IVFXResizable)
            {
                (element as IVFXResizable).OnResized();
            }
        }

        GraphViewChange VFXGraphViewChanged(GraphViewChange change)
        {
            if (change.movedElements != null && change.movedElements.Count > 0)
            {
                HashSet<IVFXMovable> movables = new HashSet<IVFXMovable>(change.movedElements.OfType<IVFXMovable>());
                foreach (var groupNode in groupNodes.Values)
                {
                    var containedElements = groupNode.containedElements;

                    if (containedElements != null && containedElements.Intersect(change.movedElements).Count() > 0)
                    {
                        groupNode.UpdateGeometryFromContent();
                        movables.Add(groupNode);
                    }
                }

                foreach (var groupNode in change.movedElements.OfType<VFXGroupNode>())
                {
                    var containedElements = groupNode.containedElements;
                    if (containedElements != null)
                    {
                        foreach (var node in containedElements.OfType<IVFXMovable>())
                        {
                            movables.Add(node);
                        }
                    }
                }

                foreach (var movable in movables)
                {
                    movable.OnMoved();
                }
            }
            else if (change.elementsToRemove != null)
            {
                controller.Remove(change.elementsToRemove.OfType<IControlledElement>().Where(t => t.controller != null).Select(t => t.controller));
                
                foreach( var dataEdge in change.elementsToRemove.OfType<VFXDataEdge>())
                {
                    RemoveElement(dataEdge);
                    dataEdges.Remove(dataEdge.controller);
                }
            }

            return change;
        }

        public VFXNodeUI GetNodeByController(VFXNodeController controller)
        {
            if (controller is VFXBlockController)
            {
                var blockController = (controller as VFXBlockController);
                VFXContextUI context = GetNodeByController(blockController.contextController) as VFXContextUI;

                return context.GetAllBlocks().FirstOrDefault(t => t.controller == blockController);
            }
            return GetAllNodes().FirstOrDefault(t => t.controller == controller);
        }

        public VFXDataAnchor GetDataAnchorByController(VFXDataAnchorController controller)
        {
            if (controller == null)
                return null;

            VFXNodeUI node = GetNodeByController(controller.sourceNode);
            if (node == null)
            {
                Debug.LogError("Can't find the node for a given node controller");
                return null;
            }

            VFXDataAnchor anchor = node.GetPorts(controller.direction == Direction.Input, controller.direction == Direction.Output).FirstOrDefault(t => t.controller == controller);
            if (anchor == null)
            {
                // Can happen because the order of the DataWatch is not controlled
                node.ForceUpdate();
                anchor = node.GetPorts(controller.direction == Direction.Input, controller.direction == Direction.Output).FirstOrDefault(t => t.controller == controller);
            }
            return anchor;
        }

        public VFXFlowAnchor GetFlowAnchorByController(VFXFlowAnchorController controller)
        {
            if (controller == null)
                return null;
            return GetAllFlowAnchors(controller.direction == Direction.Input, controller.direction == Direction.Output).Where(t => t.controller == controller).FirstOrDefault();
        }

        public IEnumerable<VFXDataAnchor> GetAllDataAnchors(bool input, bool output)
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer.Children())
                {
                    if (element is VFXNodeUI)
                    {
                        var ope = element as VFXNodeUI;
                        foreach (VFXDataAnchor anchor in ope.GetPorts(input, output))
                            yield return anchor;
                        if (element is VFXContextUI)
                        {
                            var context = element as VFXContextUI;

                            foreach (VFXBlockUI block in context.GetAllBlocks())
                            {
                                foreach (VFXDataAnchor anchor in block.GetPorts(input, output))
                                    yield return anchor;
                            }
                        }
                    }
                }
            }
        }

        public VFXDataEdge GetDataEdgeByController(VFXDataEdgeController controller)
        {
            foreach (var layer in contentViewContainer.Children())
            {
                foreach (var element in layer.Children())
                {
                    if (element is VFXDataEdge)
                    {
                        VFXDataEdge candidate = element as VFXDataEdge;
                        if (candidate.controller == controller)
                            return candidate;
                    }
                }
            }
            return null;
        }

        public void UpdateGlobalSelection()
        {
            if (controller == null) return;

            var objectSelected = selection.OfType<VFXNodeUI>().Select(t => t.controller.model).Concat(selection.OfType<VFXContextUI>().Select(t => t.controller.model).Cast<VFXModel>()).Where(t => t != null).ToArray();

            if (objectSelected.Length > 0)
            {
                Selection.objects = objectSelected;
                Selection.objects = objectSelected;
                return;
            }

            var blackBoardSelected = selection.OfType<BlackboardField>().Select(t => t.GetFirstAncestorOfType<VFXBlackboardRow>().controller.model).ToArray();

            if (blackBoardSelected.Length > 0)
            {
                Selection.objects = blackBoardSelected;
                return;
            }
        }

        void SelectAsset()
        {
            if (Selection.activeObject != controller.model)
            {
                Selection.activeObject = controller.model.visualEffectObject;
                EditorGUIUtility.PingObject(controller.model.visualEffectObject);
            }
        }

        void Checkout()
        {
            Task task = Provider.Checkout(controller.model.visualEffectObject, CheckoutMode.Both);
            task.Wait();
            OnFocus();
        }

        void ElementAddedToGroupNode(Group groupNode, IEnumerable<GraphElement> elements)
        {
            (groupNode as VFXGroupNode).ElementsAddedToGroupNode(elements);
        }

        void ElementRemovedFromGroupNode(Group groupNode, IEnumerable<GraphElement> elements)
        {
            (groupNode as VFXGroupNode).ElementsRemovedFromGroupNode(elements);
        }

        void GroupNodeTitleChanged(Group groupNode, string title)
        {
            (groupNode as VFXGroupNode).GroupNodeTitleChanged(title);
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            UpdateGlobalSelection();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            UpdateGlobalSelection();
        }

        public override void ClearSelection()
        {
            bool selectionEmpty = selection.Count() == 0;
            base.ClearSelection();
            if (!selectionEmpty)
                UpdateGlobalSelection();
        }

        VFXBlackboard m_Blackboard;
        VFXComponentBoard m_ComponentBoard;

        public VFXBlackboard blackboard
        {
            get { return m_Blackboard; }
        }


        protected internal override bool canCopySelection
        {
            get { return selection.OfType<VFXNodeUI>().Any() || selection.OfType<Group>().Any() || selection.OfType<VFXContextUI>().Any(t => !(t.controller.model is VFXBlockSubgraphContext)) || selection.OfType<VFXStickyNote>().Any(); }
        }

        IEnumerable<Controller> ElementsToController(IEnumerable<GraphElement> elements)
        {
            return elements.OfType<IControlledElement>().Select(t => t.controller);
        }

        void CollectElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> elementsToCopySet)
        {
            foreach (var element in elements)
            {
                if (element is Group)
                {
                    CollectElements((element as Group).containedElements, elementsToCopySet);
                    elementsToCopySet.Add(element);
                }
                else if (element is Node || element is VFXContextUI || element is VFXStickyNote)
                {
                    elementsToCopySet.Add(element);
                }
            }
        }

        protected internal override void CollectCopyableGraphElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> elementsToCopySet)
        {
            CollectElements(elements, elementsToCopySet);

            var nodeuis = new HashSet<VFXNodeUI>(elementsToCopySet.SelectMany(t => t.Query().OfType<VFXNodeUI>().ToList()));
            var contextuis = new HashSet<VFXContextUI>(elementsToCopySet.OfType<VFXContextUI>());

            foreach (var edge in edges.ToList())
            {
                if (edge is VFXDataEdge)
                {
                    if (nodeuis.Contains(edge.input.GetFirstAncestorOfType<VFXNodeUI>()) && nodeuis.Contains(edge.output.GetFirstAncestorOfType<VFXNodeUI>()))
                    {
                        elementsToCopySet.Add(edge);
                    }
                }
                else
                {
                    if (contextuis.Contains(edge.input.GetFirstAncestorOfType<VFXContextUI>()) && contextuis.Contains(edge.output.GetFirstAncestorOfType<VFXContextUI>()))
                    {
                        elementsToCopySet.Add(edge);
                    }
                }
            }
        }

        Rect GetElementsBounds(IEnumerable<GraphElement> elements)
        {
            Rect[] elementBounds = elements.Where(t => !(t is VFXEdge)).Select(t => contentViewContainer.WorldToLocal(t.worldBound)).ToArray();
            if (elementBounds.Length < 1) return Rect.zero;

            Rect bounds = elementBounds[0];

            for (int i = 1; i < elementBounds.Length; ++i)
            {
                bounds = Rect.MinMaxRect(Mathf.Min(elementBounds[i].xMin, bounds.xMin), Mathf.Min(elementBounds[i].yMin, bounds.yMin), Mathf.Max(elementBounds[i].xMax, bounds.xMax), Mathf.Max(elementBounds[i].yMax, bounds.yMax));
            }

            // Round to avoid changes in the asset because of the zoom level.
            bounds.x = Mathf.Round(bounds.x);
            bounds.y = Mathf.Round(bounds.y);
            bounds.width = Mathf.Round(bounds.width);
            bounds.height = Mathf.Round(bounds.height);

            return bounds;
        }

        public string SerializeElements(IEnumerable<GraphElement> elements)
        {
            Profiler.BeginSample("VFXCopy.SerializeElements");
            string result = VFXCopy.SerializeElements(ElementsToController(elements), GetElementsBounds(elements));
            Profiler.EndSample();
            return result;
        }

        public EventPropagation DuplicateSelectionWithEdges()
        {
            List<Controller> sourceControllers = selection.OfType<IControlledElement>().Select(t => t.controller).ToList();
            Rect bounds = GetElementsBounds(selection.OfType<IControlledElement>().OfType<GraphElement>());

            object result = VFXCopy.Copy(sourceControllers, bounds);

            var targetControllers = new List<VFXNodeController>();
            VFXPaste.Paste(controller, pasteCenter, result, this, null, targetControllers);

            ClearSelection();
            for (int i = 0; i < sourceControllers.Count; ++i)
            {
                if (targetControllers[i] != null)
                {
                    CopyInputLinks(sourceControllers[i] as VFXNodeController, targetControllers[i]);

                    if (targetControllers[i] is VFXBlockController blkController)
                        AddToSelection((rootNodes[blkController.contextController] as VFXContextUI).GetAllBlocks().First(t => t.controller == blkController));
                    else
                    AddToSelection(rootNodes[targetControllers[i]]);
                }
            }


            return EventPropagation.Stop;
        }

        public void AddToSelection(VFXModel model,int id)
        {
            VFXNodeController nodeController = controller.GetRootNodeController(model, id);

            if( nodeController != null)
            {
                AddToSelection(rootNodes[nodeController]);
            }
        }

        public void AddParameterToSelection(VFXParameter parameter)
        {
            VFXParameterController parameterController = controller.GetParameterController(parameter);
            if (parameterController != null)
            {
                m_Blackboard.AddToSelection(m_Blackboard.GetRowFromController(parameterController).field);
            }
        }

        void CopyInputLinks(VFXNodeController sourceController, VFXNodeController targetController)
        {
            foreach (var st in sourceController.inputPorts.Zip(targetController.inputPorts, (s, t) => new { source = s, target = t}))
            {
                CopyInputLinks(st.source, st.target);
            }
            if (sourceController is VFXContextController sourceContext && targetController is VFXContextController targetContext)
            {
                foreach (var st in sourceContext.blockControllers.Zip(targetContext.blockControllers, (s, t) => new { source = s, target = t }))
                {
                    CopyInputLinks(st.source, st.target);
                }
            }
        }

        void CopyInputLinks(VFXDataAnchorController sourceSlot, VFXDataAnchorController targetSlot)
        {
            if (sourceSlot.portType != targetSlot.portType)
                return;
            if (sourceSlot.HasLink())
                controller.CreateLink(targetSlot, controller.dataEdges.First(t => t.input == sourceSlot).output);
        }

        Vector2 pasteCenter
        {
            get
            {
                Vector2 center = layout.size * 0.5f;

                center = this.ChangeCoordinatesTo(contentViewContainer, center);

                return center;
            }
        }

        public void UnserializeAndPasteElements(string operationName, string data)
        {
            Profiler.BeginSample("VFXPaste.VFXPaste.UnserializeAndPasteElements");
            VFXPaste.UnserializeAndPasteElements(controller, pasteCenter, data, this);
            Profiler.EndSample();
        }

        const float k_MarginBetweenContexts = 30;
        public void PushUnderContext(VFXContextUI context, float size)
        {
            if (size < 5) return;

            HashSet<VFXContextUI> contexts = new HashSet<VFXContextUI>();

            contexts.Add(context);

            var flowEdges = edges.ToList().OfType<VFXFlowEdge>().ToList();

            int contextCount = 0;

            while (contextCount < contexts.Count())
            {
                contextCount = contexts.Count();
                foreach (var flowEdge in flowEdges)
                {
                    VFXContextUI topContext = flowEdge.output.GetFirstAncestorOfType<VFXContextUI>();
                    VFXContextUI bottomContext = flowEdge.input.GetFirstAncestorOfType<VFXContextUI>();
                    if (contexts.Contains(topContext)  && !contexts.Contains(bottomContext))
                    {
                        float topContextBottom = topContext.layout.yMax;
                        float newTopContextBottom = topContext.layout.yMax + size;
                        if (topContext == context)
                        {
                            newTopContextBottom -= size;
                            topContextBottom -= size;
                        }
                        float bottomContextTop = bottomContext.layout.yMin;

                        if (topContextBottom < bottomContextTop && newTopContextBottom + k_MarginBetweenContexts > bottomContextTop)
                        {
                            contexts.Add(bottomContext);
                        }
                    }
                }
            }

            contexts.Remove(context);

            foreach (var c in contexts)
            {
                c.controller.position = c.GetPosition().min + new Vector2(0, size);
            }
        }

        bool canGroupSelection
        {
            get
            {
                return canCopySelection && !selection.Any(t => t is Group);
            }
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            Undo.undoRedoPerformed += OnUndoPerformed;
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            Undo.undoRedoPerformed -= OnUndoPerformed;
        }

        public void ValidateCommand(ValidateCommandEvent evt)
        {
            if (evt.commandName == "SelectAll")
            {
                evt.StopPropagation();
                if (evt.imguiEvent != null)
                {
                    evt.imguiEvent.Use();
                }
            }
        }

        public void ExecuteCommand(ExecuteCommandEvent e)
        {
            if (e.commandName == "SelectAll")
            {
                ClearSelection();

                foreach (var element in graphElements.ToList())
                {
                    AddToSelection(element);
                }
                e.StopPropagation();
            }
        }

        void GroupSelection()
        {
            controller.GroupNodes(selection.OfType<ISettableControlledElement<VFXNodeController>>().Select(t => t.controller));
        }

        void AddStickyNote(Vector2 position, VFXGroupNode group = null)
        {
            position = contentViewContainer.WorldToLocal(position);
            controller.AddStickyNote(position, group != null ? group.controller : null);
        }

        void OnCreateNodeInGroupNode(DropdownMenuAction e)
        {
            //The targeted groupnode will be determined by a PickAll later
            VFXFilterWindow.Show(VFXViewWindow.currentWindow, e.eventInfo.mousePosition, ViewToScreenPosition(e.eventInfo.mousePosition), m_NodeProvider);
        }

        void OnEnterSubgraph(DropdownMenuAction e)
        {
            var node = e.userData as VFXModel;
            if (node is VFXSubgraphOperator subGraph)
            {
                VFXViewWindow.currentWindow.PushResource(subGraph.subgraph.GetResource());
            }
            else if (node is VFXSubgraphBlock subGraph2)
            {
                VFXViewWindow.currentWindow.PushResource(subGraph2.subgraph.GetResource());
            }
            else if (node is VFXSubgraphContext subGraph3)
            {
                VFXViewWindow.currentWindow.PushResource(subGraph3.subgraph.GetResource());
            }
        }

        void OnCreateNodeOnEdge(DropdownMenuAction e)
        {
            VFXFilterWindow.Show(VFXViewWindow.currentWindow, e.eventInfo.mousePosition, ViewToScreenPosition(e.eventInfo.mousePosition), new VFXNodeProvider(controller, (d, v) => AddNodeOnEdge(d, v, e.userData as VFXDataEdgeController), null, new Type[] { typeof(VFXOperator)}));
        }

        void AddNodeOnEdge(VFXNodeProvider.Descriptor desc, Vector2 position, VFXDataEdgeController edge)
        {
            position = this.ChangeCoordinatesTo(contentViewContainer, position);

            position.x -= 60;
            position.y -= 60;

            position = contentViewContainer.ChangeCoordinatesTo(this, position);

            var newNodeController = AddNode(desc, position);

            if (newNodeController == null)
                return;

            foreach (var outputPort in newNodeController.outputPorts)
            {
                if (controller.CreateLink(edge.input, outputPort))
                    break;
            }
            foreach (var inputPort in newNodeController.inputPorts)
            {
                if (controller.CreateLink(inputPort, edge.output))
                    break;
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (controller == null || !IsAssetEditable())
                return;

            if (evt.target is VFXGroupNode || evt.target is VFXSystemBorder) // Default behaviour only shows the OnCreateNode if the target is the view itself.
                evt.target = this;

            base.BuildContextualMenu(evt);

            Vector2 mousePosition = evt.mousePosition;

            if (evt.target is VFXNodeUI node)
            {
                evt.menu.InsertAction(evt.target is VFXContextUI ? 1 : 0, "Group Selection", (e) => { GroupSelection(); },
                    (e) => { return canGroupSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled; });

                if ((node.controller.model is VFXSubgraphOperator subOp && subOp.subgraph != null) || (node.controller.model is VFXSubgraphContext subCont && subCont.subgraph != null) || (node.controller.model is VFXSubgraphBlock subBlk && subBlk.subgraph != null))
                {
                    evt.menu.AppendAction("Enter Subgraph", OnEnterSubgraph, e => DropdownMenuAction.Status.Normal, node.controller.model);
                }
                    evt.menu.AppendAction("Clear Ignored Errors",a=>node.controller.model.ClearIgnoredErrors(), node.controller.model.HasIgnoredErrors()?DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            if (evt.target is VFXDataEdge edge)
            {
                evt.menu.InsertAction(0, "Create Node", OnCreateNodeOnEdge, t => DropdownMenuAction.Status.Normal, edge.controller);
            }

            if (evt.target is VFXView)
            {
                evt.menu.InsertAction(1, "Create Sticky Note", (e) => { AddStickyNote(mousePosition); }, (e) => DropdownMenuAction.Status.Normal);

                if (evt.triggerEvent is IMouseEvent)
                {
                    foreach (var system in m_Systems)
                    {
                        Rect bounds = system.worldBound;
                        if (bounds.Contains((evt.triggerEvent as IMouseEvent).mousePosition))
                        {
                            evt.menu.InsertSeparator("", 2);
                            evt.menu.InsertAction(3, string.IsNullOrEmpty(system.controller.title) ? "Name System" : "Rename System", a => system.OnRename(), e => DropdownMenuAction.Status.Normal);
                            break;
                        }
                    }
                }

                if (VFXViewWindow.currentWindow != null && VFXViewWindow.currentWindow.resourceHistory.Count() > 0)
                {
                    evt.menu.AppendAction(" Back To Parent Graph", e => VFXViewWindow.currentWindow.PopResource());
                }
            }

            if (evt.target is VFXContextUI)
            {
                var context = evt.target as VFXContextUI;
                evt.menu.InsertSeparator("", 2);
                evt.menu.InsertAction(3, string.IsNullOrEmpty(context.controller.model.label) ? "Name Context" : "Rename Context", a => context.OnRename(), e => DropdownMenuAction.Status.Normal);
            }


            if (selection.OfType<VFXNodeUI>().Any() && evt.target is VFXNodeUI)
            {
                if (selection.OfType<VFXOperatorUI>().Any() && !selection.OfType<VFXNodeUI>().Any(t => !(t is VFXOperatorUI) && !(t is VFXParameterUI)))
                    evt.menu.InsertAction(3, "Convert To Subgraph Operator", ToSubgraphOperator, e => DropdownMenuAction.Status.Normal);
                else if (SelectionHasCompleteSystems())
                    evt.menu.InsertAction(3, "Convert To Subgraph", ToSubgraphContext, e => DropdownMenuAction.Status.Normal);
                else if (selection.OfType<VFXBlockUI>().Any() && selection.OfType<VFXBlockUI>().Select(t => t.context).Distinct().Count() == 1)
                {
                    evt.menu.InsertAction(3, "Convert to Subgraph Block", ToSubgraphBlock, e => DropdownMenuAction.Status.Normal);
                }
            }
            if (evt.target is GraphView || evt.target is Node || evt.target is Group)
            {
                evt.menu.AppendAction("Duplicate with edges" , (a) => { DuplicateSelectionWithEdges(); },
                    (a) => { return canDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled; });
                evt.menu.AppendSeparator();
            }

            if (selection.OfType<VFXOperatorUI>().Any(t => !t.superCollapsed))
                evt.menu.AppendAction("Collapse Operators", CollapseOperator, e => DropdownMenuAction.Status.Normal, true);
            if (selection.OfType<VFXOperatorUI>().Any(t => t.superCollapsed))
                evt.menu.AppendAction("Uncollapse Operators", CollapseOperator, e => DropdownMenuAction.Status.Normal, false);
        }

        void CollapseOperator(DropdownMenuAction a)
        {
            bool collapse = (bool)a.userData;

            foreach (var ope in selection.OfType<VFXOperatorUI>())
                ope.controller.superCollapsed = collapse;
        }

        public bool SelectionHasCompleteSystems()
        {
            HashSet<VFXContextUI> selectedContextUIs = new HashSet<VFXContextUI>(selection.OfType<VFXContextUI>());
            if (selectedContextUIs.Count() < 1)
                return false;

            var relatedContext = selectedContextUIs.Select(t => t.controller.model);

            //Adding manually VFXBasicGPUEvent, it doesn't appears as dependency.
            var outputContextDataFromGPUEvent = relatedContext.OfType<VFXBasicGPUEvent>().SelectMany(o => o.outputContexts);
            relatedContext = relatedContext.Concat(outputContextDataFromGPUEvent);
            var selectedContextDatas = relatedContext.Select(o => o.GetData()).Where(o => o != null);

            var selectedContextDependencies = selectedContextDatas.SelectMany(o => o.allDependenciesIncludingNotCompilable);
            var allDatas = selectedContextDatas.Concat(selectedContextDependencies);

            var allDatasHash = new HashSet<VFXData>(allDatas);
            foreach (var context in GetAllContexts())
            {
                var model = context.controller.model;
                if (model is VFXBlockSubgraphContext)
                    return false;

                //We should exclude model.contextType == VFXContextType.Event of this condition.
                //If VFXConvertSubgraph.TransferContextsFlowEdges has been fixed & renabled.
                if (allDatasHash.Contains(model.GetData()) && !selectedContextUIs.Contains(context))
                    return false;
            }

            return true;
        }

        void ToSubgraphBlock(DropdownMenuAction a)
        {
            VFXConvertSubgraph.ConvertToSubgraphBlock(this, selection.OfType<IControlledElement>().Select(t => t.controller), GetElementsBounds(selection.Where(t => !(t is Edge)).Cast<GraphElement>()));
        }

        void ToSubgraphOperator(DropdownMenuAction a)
        {
            VFXConvertSubgraph.ConvertToSubgraphOperator(this, selection.OfType<IControlledElement>().Select(t => t.controller), GetElementsBounds(selection.Where(t => !(t is Edge)).Cast<GraphElement>()));
        }

        void ToSubgraphContext(DropdownMenuAction a)
        {
            VFXConvertSubgraph.ConvertToSubgraphContext(this, selection.OfType<IControlledElement>().Select(t => t.controller), GetElementsBounds(selection.Where(t => !(t is Edge)).Cast<GraphElement>()));
        }

        List<VFXSystemBorder> m_Systems = new List<VFXSystemBorder>();

        public ReadOnlyCollection<VFXSystemBorder> systems
        {
            get { return m_Systems.AsReadOnly(); }
        }

        public void UpdateSystemNames()
        {
            if (m_Systems != null)
                foreach (var system in m_Systems)
                {
                    system.Update();
                }
        }

        public void UpdateSystems()
        {
            while (m_Systems.Count() > controller.systems.Count())
            {
                VFXSystemBorder border = m_Systems.Last();
                m_Systems.RemoveAt(m_Systems.Count - 1);
                border.RemoveFromHierarchy();
            }

            UpdateSystemNames();

            while (m_Systems.Count() < controller.systems.Count())
            {
                VFXSystemBorder border = new VFXSystemBorder();
                m_Systems.Add(border);
                AddElement(border);
                border.controller = controller.systems[m_Systems.Count() - 1];
            }

            foreach (var context in GetAllContexts())
            {
                context.UpdateLabel();
            }
        }

        void OnDragUpdated(DragUpdatedEvent e)
        {
            if (controller == null || !IsAssetEditable())
                return;

            if (DragAndDrop.GetGenericData("DragSelection") != null && selection.Any(t => t is VFXBlackboardField && (t as VFXBlackboardField).GetFirstAncestorOfType<VFXBlackboardRow>() != null))
            {
                VFXBlackboardField selectedField = selection.OfType<VFXBlackboardField>().Where(t => t.GetFirstAncestorOfType<VFXBlackboardRow>() != null).First();

                if (selectedField.controller.isOutput && selectedField.controller.nodeCount > 0)
                {
                    return;
                }

                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                e.StopPropagation();
            }
            else
            {
                var references = DragAndDrop.objectReferences.OfType<VisualEffectAsset>().Cast<VisualEffectObject>().Concat(DragAndDrop.objectReferences.OfType<VisualEffectSubgraphOperator>());
                VisualEffectObject draggedObject = references.FirstOrDefault();
                bool isOperator = draggedObject is VisualEffectSubgraphOperator;

                if (draggedObject != null && draggedObject != controller.model.visualEffectObject)
                {
                    var draggedObjectDependencies = draggedObject.GetResource().GetOrCreateGraph().subgraphDependencies;
                    bool vfxIntovfx = !isOperator && !controller.model.isSubgraph && !draggedObjectDependencies.Contains(controller.model.subgraph); // dropping a vfx into a vfx
                    bool operatorIntovfx = isOperator && !controller.model.isSubgraph; //dropping an operator into a vfx
                    bool operatorIntoOperator = isOperator && controller.model.visualEffectObject is VisualEffectSubgraphOperator && !draggedObjectDependencies.Contains(controller.model.visualEffectObject); //dropping an operator into a vfx
                    if (vfxIntovfx || operatorIntovfx || operatorIntoOperator)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        e.StopPropagation();
                    }
                    return;
                }

                var droppedBlocks = DragAndDrop.objectReferences.OfType<VisualEffectSubgraphBlock>();
                if (droppedBlocks.Count() > 0 && !controller.model.isSubgraph)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    e.StopPropagation();
                }
            }
        }

        void OnDragPerform(DragPerformEvent e)
        {
            if (controller == null || !IsAssetEditable())
                return;
            var groupNode = GetPickedGroupNode(e.mousePosition);

            if (DragAndDrop.GetGenericData("DragSelection") != null && selection.Any(t => t is BlackboardField && (t as BlackboardField).GetFirstAncestorOfType<VFXBlackboardRow>() != null))
            {
                var rows = selection.OfType<BlackboardField>().Select(t => t.GetFirstAncestorOfType<VFXBlackboardRow>()).Where(t => t != null).ToArray();
                if (rows.Length > 0)
                {
                    DragAndDrop.AcceptDrag();
                    Vector2 mousePosition = contentViewContainer.WorldToLocal(e.mousePosition);

                    UpdateSelectionWithNewNode();
                    float cpt = 0;
                    foreach (var row in rows)
                    {
                        AddVFXParameter(mousePosition - new Vector2(50, 20) + cpt * new Vector2(0,40), row.controller, groupNode);
                        ++cpt;
                    }
                    e.StopPropagation();
                }
            }
            else
            {
                DragAndDrop.AcceptDrag();
                var references = DragAndDrop.objectReferences.OfType<VisualEffectAsset>().Cast<VisualEffectObject>().Concat(DragAndDrop.objectReferences.OfType<VisualEffectSubgraphOperator>());

                VisualEffectObject draggedObject = references.FirstOrDefault();
                bool isOperator = draggedObject is VisualEffectSubgraphOperator;

                if (draggedObject != null && draggedObject != controller.model.visualEffectObject)
                {
                    var draggedObjectDependencies = draggedObject.GetResource().GetOrCreateGraph().subgraphDependencies;
                    bool vfxIntovfx = !isOperator && !controller.model.isSubgraph && !draggedObjectDependencies.Contains(controller.model.subgraph); // dropping a vfx into a vfx
                    bool operatorIntovfx = isOperator && !controller.model.isSubgraph; //dropping an operator into a vfx
                    bool operatorIntoOperator = isOperator && controller.model.visualEffectObject is VisualEffectSubgraphOperator && !draggedObjectDependencies.Contains(controller.model.visualEffectObject); //dropping an operator into a vfx
                    if (vfxIntovfx || operatorIntovfx || operatorIntoOperator)
                    {
                        Vector2 mousePosition = contentViewContainer.WorldToLocal(e.mousePosition);
                        VFXModel newModel = (references.First() is VisualEffectAsset) ? VFXSubgraphContext.CreateInstance<VFXSubgraphContext>() as VFXModel : VFXSubgraphOperator.CreateInstance<VFXSubgraphOperator>() as VFXModel;


                        UpdateSelectionWithNewNode();
                        controller.AddVFXModel(mousePosition, newModel);

                        newModel.SetSettingValue("m_Subgraph", references.First());

                        //TODO add to picked groupnode
                        e.StopPropagation();
                    }
                }
                else if (!controller.model.isSubgraph) //can't drag a vfx subgraph block in a subgraph operator or a subgraph block
                {
                    var droppedBlocks = DragAndDrop.objectReferences.OfType<VisualEffectSubgraphBlock>();
                    VisualEffectSubgraphBlock droppedBlock = droppedBlocks.FirstOrDefault();
                    if (droppedBlock != null)
                    {
                        Vector2 mousePosition = contentViewContainer.WorldToLocal(e.mousePosition);

                        VFXContextType contextKind = droppedBlocks.First().GetResource().GetOrCreateGraph().children.OfType<VFXBlockSubgraphContext>().First().compatibleContextType;
                        VFXModelDescriptor<VFXContext> contextType = VFXLibrary.GetContexts().First(t => t.modelType == typeof(VFXBasicInitialize));
                        if ((contextKind & VFXContextType.Update) == VFXContextType.Update)
                            contextType = VFXLibrary.GetContexts().First(t => t.modelType == typeof(VFXBasicUpdate));
                        else if ((contextKind & VFXContextType.Spawner) == VFXContextType.Spawner)
                            contextType = VFXLibrary.GetContexts().First(t => t.modelType == typeof(VFXBasicSpawner));
                        else if ((contextKind & VFXContextType.Output) == VFXContextType.Output)
                            contextType = VFXLibrary.GetContexts().First(t => t.modelType == typeof(VFXPlanarPrimitiveOutput) && t.model.taskType == VFXTaskType.ParticleQuadOutput);

                        UpdateSelectionWithNewNode();
                        VFXContext ctx = controller.AddVFXContext(mousePosition, contextType);

                        VFXModel newModel = VFXSubgraphBlock.CreateInstance<VFXSubgraphBlock>();

                        newModel.SetSettingValue("m_Subgraph", droppedBlocks.First());

                        UpdateSelectionWithNewNode();
                        ctx.AddChild(newModel);


                        //TODO add to picked groupnode
                        e.StopPropagation();
                    }
                }
            }
        }

        public void AssetMoved()
        {
            foreach (var item in this.Query<VFXNodeUI>().ToList())
            {
                item.AssetMoved();
            }
        }

        VFXEdgeDragInfo m_EdgeDragInfo;

        public void StartEdgeDragInfo(VFXDataAnchor draggerAnchor, VFXDataAnchor overAnchor)
        {
            if (m_EdgeDragInfo == null)
            {
                m_EdgeDragInfo = new VFXEdgeDragInfo(this);
                Add(m_EdgeDragInfo);
                m_EdgeDragInfo.style.display = DisplayStyle.None;
            }

            m_EdgeDragInfo.StartEdgeDragInfo(draggerAnchor, overAnchor);
        }

        public void StopEdgeDragInfo()
        {
            if (m_EdgeDragInfo != null)
                m_EdgeDragInfo.StopEdgeDragInfo();
        }
    }
}
