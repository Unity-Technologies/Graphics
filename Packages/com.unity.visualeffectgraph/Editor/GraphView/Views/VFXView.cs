using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using UnityEditor.Experimental;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Toolbars;
using UnityEditor.VersionControl;

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Profiling;
using PositionType = UnityEngine.UIElements.Position;
using Task = UnityEditor.VersionControl.Task;

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

    struct VFXViewSettings
    {
        private bool m_IsAttachedLocked;
        private VisualEffect m_AttachedVisualEffect;

        public void Load(bool force = false)
        {
            m_IsAttachedLocked = EditorPrefs.GetBool(nameof(m_IsAttachedLocked));
            if (EditorApplication.isPlaying || force)
            {
                var attachedVisualEffectPath = EditorPrefs.GetString(nameof(m_AttachedVisualEffect));
                if (!string.IsNullOrEmpty(attachedVisualEffectPath))
                {
                    var go = GameObject.Find(attachedVisualEffectPath);
                    if (go != null)
                    {
                        m_AttachedVisualEffect = go.GetComponent<VisualEffect>();
                    }
                }
            }
        }

        public VisualEffect AttachedVisualEffect
        {
            get => m_AttachedVisualEffect;
            set
            {
                m_AttachedVisualEffect = value;
                if (!EditorApplication.isPlaying)
                {
                    if (m_AttachedVisualEffect != null)
                    {
                        var go = m_AttachedVisualEffect.gameObject;
                        var path = go.GetComponentsInParent<UnityEngine.Transform>()
                            .Select(x => x.name)
                            .Reverse()
                            .ToArray();

                        EditorPrefs.SetString(nameof(m_AttachedVisualEffect), "/" + string.Join('/', path));
                    }
                    else
                    {
                        EditorPrefs.SetString(nameof(m_AttachedVisualEffect), null);
                    }
                }
            }
        }

        public bool AttachedLocked
        {
            get => m_IsAttachedLocked;
            set
            {
                m_IsAttachedLocked = value;
                EditorPrefs.SetBool(nameof(m_IsAttachedLocked), m_IsAttachedLocked);
            }
        }
    }

    class VFXView : GraphView, IControlledElement<VFXViewController>, IControllerListener, IDisposable
    {
        private const int MaximumNameLengthInNotification = 128;
        private const float GrayedOutGraphOpacity = 0.6f;
        private const string AttachGameObjectLabel = "attach-toolbar-button";
        private const string LockAutoAttachLabel = "lock-auto-attach";

        // Only changed in aut test to avoid dialog box
        public bool askForConfirmationBeforeDelete = true;

        internal static class Contents
        {
            public static readonly GUIContent clickToUnlock = EditorGUIUtility.TrTextContent("Click to enable auto-attachment to selection");
            public static readonly GUIContent clickToLock = EditorGUIUtility.TrTextContent("Click to disable auto-attachment to selection");
            public static readonly GUIContent attachedToGameObject = EditorGUIUtility.TrTextContent("Attached to {0}");
            public static readonly GUIContent notAttached = EditorGUIUtility.TrTextContent("Select a Game Object running this VFX to attach it");
        }

        private static readonly List<string> ToolbarElementsToDisableWhenNoAsset = new ()
        {
            nameof(VFXSaveDropdownButton),
            nameof(VFXCompileDropdownButton),
            AttachGameObjectLabel,
            LockAutoAttachLabel,
            nameof(VFXVCSDropdownButton),
            nameof(CreateFromTemplateDropDownButton)
        };

        public readonly HashSet<VFXEditableDataAnchor> allDataAnchors = new HashSet<VFXEditableDataAnchor>();

        public bool locked => m_VFXSettings.AttachedLocked;

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

        VFXViewSettings m_VFXSettings;
        VisualElement m_NoAssetElement;
        VisualElement m_LockedElement;
        Button m_BackButton;
        Vector2 m_pastCenter;

        VFXViewController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }

        void DisconnectController(VFXViewController previousController)
        {
            if (previousController.model && previousController.graph)
            {
                previousController.graph.ForceShaderDebugSymbols(VFXViewPreference.generateShadersWithDebugSymbols, false); // Remove debug symbols override from view but don't reimport (this is done by the SetCompilation below)
                previousController.graph.SetCompilationMode(VFXViewPreference.forceEditionCompilation ? VFXCompilationMode.Edition : VFXCompilationMode.Runtime);
            }

            previousController.UnregisterHandler(this);
            previousController.useCount--;

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
            if (Provider.enabled && !IsOffline())
            {
                m_VCSDropDown.SetStatus(Asset.States.None);
            }

            SceneView.duringSceneGui -= OnSceneGUI;
            OnFocus();
        }

        void ConnectController()
        {
            schedule.Execute(() =>
            {
                if (controller != null && controller.graph)
                    controller.graph.SetCompilationMode(m_IsRuntimeMode ? VFXCompilationMode.Runtime : VFXCompilationMode.Edition);
            }).ExecuteLater(1);

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

            SceneView.duringSceneGui += OnSceneGUI;
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
                if (value == null)
                {
                    m_ComponentBoard.Detach();
                    m_ProfilingBoard.Detach();
                }
                else
                {
                    m_ComponentBoard.Attach(value);
                    m_ProfilingBoard.Attach(value);
                }
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

        public bool TryRemoveCustomAttribute(string customAttributeName)
        {
            if (askForConfirmationBeforeDelete && controller.graph.IsCustomAttributeUsed(customAttributeName) && !EditorUtility.DisplayDialog("Confirmation",
                    $"The custom attribute '{customAttributeName}' is used in the graph. Removing this attribute will remove get and set nodes automatically.\nAre you sure?",
                    "Yes",
                    "No"))
            {
                return false;
            }

            controller.graph.RemoveCustomAttribute(customAttributeName);
            return true;
        }

        private bool TryRemoveCategory(string category)
        {
            if (askForConfirmationBeforeDelete && controller.parameterControllers.Any(x => string.Compare(x.model.category, category, StringComparison.OrdinalIgnoreCase) == 0) && !EditorUtility.DisplayDialog("Confirmation",
                    $"The category '{category}' contains one or more properties. Removing this category will remove these properties too.\nAre you sure?",
                    "Yes",
                    "No"))
            {
                return false;
            }

            return controller.RemoveCategory(category);
        }

        public bool TryRemoveParameter(VFXParameterController parameterController)
        {
            if (askForConfirmationBeforeDelete && parameterController.hasNodes && !EditorUtility.DisplayDialog("Confirmation",
                $"The property '{parameterController.model.exposedName}' is used in the graph. Removing this property will remove nodes automatically.\nAre you sure?",
                "Yes",
                "No"))
            {
                return false;
            }

            controller.RemoveElement(parameterController);
            return true;
        }

        public VFXViewController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != value)
                {
                    var previousController = m_Controller;

                    if (value == null)
                    {
                        m_Controller = null;
                        if (!VFXViewWindow.CloseIfNotLast(this))
                        {
                            DisconnectController(previousController);
                            NewControllerSet();
                        }
                    }
                    else
                    {
                        if (m_Controller != null)
                        {
                            DisconnectController(previousController);
                        }

                        m_Controller = value;
                        if (m_Controller != null)
                        {
                            ConnectController();
                        }

                        if (m_Controller != null)
                        {
                            NewControllerSet();
                            AttachToSelection();
                            m_ComponentBoard.ResetPlayRate();
                        }
                    }
                }
            }
        }

        public VFXGroupNode GetPickedGroupNode(Vector2 panelPosition)
        {
            List<VisualElement> picked = new List<VisualElement>();
            panel.PickAll(panelPosition, picked);

            return picked.OfType<VFXGroupNode>().FirstOrDefault();
        }

        internal VFXNodeController AddOperator(Type type)
        {
            var desc = VFXLibrary.GetOperators().FirstOrDefault(x => x.modelType == type);
            return AddNode(desc.variant, Event.current.mousePosition);
        }

        public VFXNodeController AddNode(Variant variant, Vector2 mPos)
        {
            UpdateSelectionWithNewNode();
            var groupNode = GetPickedGroupNode(mPos);

            mPos = this.ChangeCoordinatesTo(contentViewContainer, mPos);

            if (variant.modelType == typeof(VisualEffectSubgraphOperator))
            {
                var path = variant.settings.Single(x => x.Key == "path").Value as string;
                var subGraph = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraphOperator>(path);
                if (subGraph != null && (!controller.model.isSubgraph || !subGraph.GetResource().GetOrCreateGraph().subgraphDependencies.Contains(controller.model.subgraph) && subGraph.GetResource() != controller.model))
                {
                    VFXModel newModel = ScriptableObject.CreateInstance<VFXSubgraphOperator>();

                    controller.AddVFXModel(mPos, newModel);

                    newModel.SetSettingValue("m_Subgraph", subGraph);

                    UpdateSelectionWithNewNode();

                    controller.LightApplyChanges();

                    return controller.GetNewNodeController(newModel);
                }
            }
            else if (variant is VFXModelDescriptorParameters.ParameterVariant)
            {
                var parameterController = controller.parameterControllers.Single(x => x.exposedName == variant.name);
                UpdateSelectionWithNewNode();
                return controller.AddVFXParameter(mPos, parameterController, groupNode?.controller);
            }
            else
            {
                UpdateSelectionWithNewNode();

                return controller.AddNode(mPos, variant, groupNode?.controller);
            }
            return null;
        }

        public bool HasCustomAttributeConflicts(IEnumerable<VFXAttribute> customAttributes)
        {
            var conflictingAttributes = new Dictionary<VFXAttribute, VFXAttributeHelper.Match>();
            var attributeManager = controller.graph.attributesManager;
            foreach (var attribute in customAttributes)
            {
                if (controller.graph.TryFindCustomAttributeDescriptor(attribute.name, out var attributeDescriptor) && !attributeDescriptor.isReadOnly)
                {
                    attributeManager.TryFind(attribute.name, out var conflictingAttribute);
                    conflictingAttributes[attribute] = VFXAttributeHelper.GetMatch(attribute, conflictingAttribute);
                }
            }

            var message = new StringBuilder();
            var notPerfectMatches = conflictingAttributes.Where(x => x.Value.status != VFXAttributeHelper.VFXAttributeMatch.PerfectMath).ToArray();
            if (notPerfectMatches.Length > 0)
            {
                message.Append("The following custom attribute(s) already exist in the graph but have different type or name (Only differ in casing).\nYou should either rename them or change type accordingly:\n");
                foreach (var typeDifference in notPerfectMatches)
                {
                    message.AppendFormat("\n{0} ({1}) => {2} ({3})", typeDifference.Value.attribute.name, typeDifference.Value.attribute.type, typeDifference.Key.name, typeDifference.Key.type);
                }

                EditorUtility.DisplayDialog("Custom Attribute Conflict", message.ToString(), "Close");
                return true;
            }
            if (conflictingAttributes.Count > 0)
            {
                message.Append("If you continue, custom attributes shared with the imported subgraph will become 'read-only' and the description will be overwritten by the one from the subgraph.");
                return !EditorUtility.DisplayDialog("Custom Attribute Conflict", message.ToString(), "Continue", "Cancel");
            }

            return false;
        }

        readonly VisualElement m_Toolbar;
        readonly ToolbarToggle m_LockToggle;
        readonly EditorToolbarDropdown m_AttachDropDownButton;
        readonly Texture2D m_LinkedIcon;
        readonly Texture2D m_UnlinkedIcon;
        readonly VFXVCSDropdownButton m_VCSDropDown;

        private VFXCompileDropdownButton m_CompileDropDownButton;

        VFXNodeProvider m_NodeProvider;
        bool m_IsRuntimeMode;
        bool m_ForceShaderDebugSymbols;
        bool m_ForceShaderValidation;


        public static StyleSheet LoadStyleSheet(string text)
        {
            string path = string.Format("{0}/uss/{1}.uss", VisualEffectAssetEditorUtility.editorResourcesPath, text);
            return AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
        }

        public static VisualTreeAsset LoadUXML(string text)
        {
            string path = string.Format("{0}/uxml/{1}.uxml", VisualEffectAssetEditorUtility.editorResourcesPath, text);
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
        }

        public static Texture2D LoadImage(string text)
        {
            string path = string.Format("{0}/VFX/{1}.png", VisualEffectAssetEditorUtility.editorResourcesPath, text);
            return EditorGUIUtility.LoadIcon(path);
        }

        SelectionDragger m_SelectionDragger;
        RectangleSelector m_RectangleSelector;

        private void OnCreateAsset()
        {
            GraphViewTemplateWindow.ShowCreateFromTemplate(new VFXTemplateHelperInternal(), CreateNewFromTemplate);
        }

        public VFXView()
        {
            m_UseInternalClipboard = false;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SetupZoom(0.125f, 8);

            this.AddManipulator(new ContentDragger());
            m_SelectionDragger = new SelectionDragger();
            m_RectangleSelector = new RectangleSelector();
            this.AddManipulator(m_SelectionDragger);
            this.AddManipulator(m_RectangleSelector);
            this.AddManipulator(new FreehandSelector());

            var vfxViewStyleSheet = LoadStyleSheet("VFXView");
            styleSheets.Add(vfxViewStyleSheet);
            if (!EditorGUIUtility.isProSkin)
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
            m_Toolbar.styleSheets.Add(vfxViewStyleSheet);

            var saveDropDownButton = new VFXSaveDropdownButton(this);
            m_Toolbar.Add(saveDropDownButton);

            m_CompileDropDownButton = new VFXCompileDropdownButton(this);
            m_Toolbar.Add(m_CompileDropDownButton);

            m_LinkedIcon = EditorGUIUtility.LoadIcon(Path.Combine(EditorResources.iconsPath, "Linked.png"));
            m_UnlinkedIcon = EditorGUIUtility.LoadIcon(Path.Combine(EditorResources.iconsPath, "UnLinked.png"));
            m_AttachDropDownButton = new EditorToolbarDropdown(m_UnlinkedIcon, OnOpenAttachMenu);
            m_AttachDropDownButton.name = AttachGameObjectLabel;
            m_Toolbar.Add(m_AttachDropDownButton);
            m_LockToggle = new ToolbarToggle();

            m_LockToggle.style.unityTextAlign = TextAnchor.MiddleLeft;
            m_LockToggle.tooltip = locked ? Contents.clickToUnlock.text : Contents.clickToLock.text;
            m_LockToggle.name = LockAutoAttachLabel;
            m_LockToggle.RegisterCallback<ChangeEvent<bool>>(OnToggleLock);
            m_Toolbar.Add(m_LockToggle);

            m_VCSDropDown = new VFXVCSDropdownButton(this);
            m_Toolbar.Add(m_VCSDropDown);

            var templateDropDownButton = new CreateFromTemplateDropDownButton(this);
            m_Toolbar.Add(templateDropDownButton);

            m_BackButton = new ToolbarButton { tooltip = "Back to parent", name = "BackButton" };
            m_BackButton.Add(new Image { image = EditorGUIUtility.LoadIcon(Path.Combine(EditorResources.iconsPath, "back.png")) });
            m_BackButton.AddStyleSheetPath("VFXToolbar");
            m_BackButton.clicked += OnBackToParent;
            m_Toolbar.Add(m_BackButton);

            var flexSpacer = new ToolbarSpacer();
            flexSpacer.style.flexGrow = 1f;
            m_Toolbar.Add(flexSpacer);

            var toggleBlackboard = new ToolbarToggle { tooltip = "Blackboard" };
            toggleBlackboard.Add(new Image { image = EditorGUIUtility.LoadIcon(Path.Combine(VisualEffectGraphPackageInfo.assetPackagePath, "Editor/UIResources/VFX/variableswindow.png")) });
            toggleBlackboard.RegisterCallback<ChangeEvent<bool>>(ToggleBlackboard);
            m_Toolbar.Add(toggleBlackboard);

            m_ToggleComponentBoard = new ToolbarToggle { tooltip = "Displays controls for the GameObject currently attached" };
            m_ToggleComponentBoard.Add(new Image { image = EditorGUIUtility.LoadIcon(Path.Combine(VisualEffectGraphPackageInfo.assetPackagePath, "Editor/UIResources/VFX/controls.png")) });
            m_ToggleComponentBoard.style.borderRightWidth = 1;
            m_ToggleComponentBoard.RegisterCallback<ChangeEvent<bool>>(ToggleComponentBoard);
            m_Toolbar.Add(m_ToggleComponentBoard);

            m_ToggleProfilingBoard = new ToolbarToggle { tooltip = "Display profiling information about current attached GameObject." };
            m_ToggleProfilingBoard.Add(new Image { image = EditorGUIUtility.LoadIcon(Path.Combine(VisualEffectGraphPackageInfo.assetPackagePath, "Editor/UIResources/VFX/debug.png")) });
            m_ToggleProfilingBoard.style.borderRightWidth = 1;
            m_ToggleProfilingBoard.RegisterCallback<ChangeEvent<bool>>(ToggleProfilingBoard);
            m_Toolbar.Add(m_ToggleProfilingBoard);


            var helpDropDownButton = new VFXHelpDropdownButton(this);
            m_Toolbar.Add(helpDropDownButton);
            // End Toolbar

            var noAssetLabel = new Label("To begin creating Visual Effects, create a new Visual Effect Graph Asset.\n(or double-click an existing Visual Effect Graph in the project view)");
            var createButton = new Button { text = "Create new Visual Effect Graph" };
            createButton.clicked += OnCreateAsset;

            m_NoAssetElement = new VisualElement { name = "no-asset" };
            m_NoAssetElement.Add(noAssetLabel);
            m_NoAssetElement.Add(createButton);

            m_LockedElement = new VisualElement { name = "lockedContainer"};
            var lockLabel = new Label("\u2b06 Check out to modify") { name = "lockedMessage" };
            lockLabel.focusable = true;
            m_LockedElement.Add(lockLabel);

            m_Blackboard = new VFXBlackboard(this);
            bool blackboardVisible = BoardPreferenceHelper.IsVisible(BoardPreferenceHelper.Board.blackboard, true);
            if (blackboardVisible)
                Add(m_Blackboard);
            toggleBlackboard.value = blackboardVisible;

            m_ComponentBoard = new VFXComponentBoard(this);
            m_ProfilingBoard = new VFXProfilingBoard(this);

            Add(m_LockedElement);
            Add(m_NoAssetElement);
            SetToolbarEnabled(false);

            m_VFXSettings = new VFXViewSettings();
            m_VFXSettings.Load();
            m_LockToggle.value = m_VFXSettings.AttachedLocked;

            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<ValidateCommandEvent>(ValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(ExecuteCommand);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);


            graphViewChanged = VFXGraphViewChanged;
            elementResized = VFXElementResized;
            canPasteSerializedData = VFXCanPaste;

            viewDataKey = "VFXView";
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        internal bool GetIsRuntimeMode() => m_IsRuntimeMode;
        internal bool GetForceShaderDebugSymbols() => m_ForceShaderDebugSymbols;

        public void Dispose()
        {
            controller = null;
            UnregisterCallback<DragUpdatedEvent>(OnDragUpdated);
            UnregisterCallback<DragPerformEvent>(OnDragPerform);
            UnregisterCallback<ValidateCommandEvent>(ValidateCommand);
            UnregisterCallback<ExecuteCommandEvent>(ExecuteCommand);
            UnregisterCallback<AttachToPanelEvent>(OnEnterPanel);
            UnregisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            UnregisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.EnteredEditMode)
            {
                m_VFXSettings.Load(true);
                TryAttachTo(m_VFXSettings.AttachedVisualEffect, false);
            }

            m_ComponentBoard.SetDebugMode(VFXUIDebug.Modes.None);
        }

        void OnOpenAttachMenu()
        {
            var attachPanel = ScriptableObject.CreateInstance<VFXAttachPanel>();
            attachPanel.SetView(this);
            var bounds = new Rect(ViewToScreenPosition(m_AttachDropDownButton.worldBound.position), m_AttachDropDownButton.worldBound.size);
            bounds.xMin++;
            attachPanel.ShowAsDropDown(bounds, attachPanel.WindowSize, new[] { PopupLocation.BelowAlignLeft });
        }

        internal void ToggleRuntimeMode()
        {
            m_IsRuntimeMode = !m_IsRuntimeMode;
            controller.graph.SetCompilationMode(m_IsRuntimeMode ? VFXCompilationMode.Runtime : VFXCompilationMode.Edition);
        }

        internal void ToggleForceShaderDebugSymbols()
        {
            m_ForceShaderDebugSymbols = !m_ForceShaderDebugSymbols;
            controller.graph.ForceShaderDebugSymbols(m_ForceShaderDebugSymbols);
        }

        internal bool GetShaderValidation() => m_ForceShaderValidation;

        internal void ToggleShaderValidationChanged()
        {
            m_ForceShaderValidation = !m_ForceShaderValidation;
            controller.graph.SetForceShaderValidation(m_ForceShaderValidation);
        }

        [NonSerialized]
        Dictionary<VFXModel, List<VFXIconBadge>> m_InvalidateBadges = new();

        [NonSerialized]
        List<VFXIconBadge> m_CompileBadges = new();
        [NonSerialized]
        List<VFXModel> m_ModelsWithHiddenBadges = new();

        private void SetToolbarEnabled(bool enabled)
        {
            ToolbarElementsToDisableWhenNoAsset.ForEach(x => m_Toolbar.Q<VisualElement>(x).SetEnabled(enabled));

        }

        public void UpdateBadges(IVFXErrorReporter report)
        {
            if (report == null)
            {
                return;
            }
            foreach (var model in report.dirtyModels)
            {
                ClearAllErrors(model, report.origin);
                foreach (var reportError in report.GetDirtyModelErrors(model))
                {
                    RegisterError(model, report.origin, reportError.error, reportError.type, reportError.description);
                }
            }
            report.ClearDirtyModels();
        }

        private void RegisterError(VFXModel model, VFXErrorOrigin errorOrigin, string error, VFXErrorType type, string description)
        {
            VisualElement target = null;
            VisualElement targetParent = null;

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
            }

            if (target != null && targetParent != null)
            {
                var badge = new VFXIconBadge(description, type);
                var badgeHolder = target.Children().SingleOrDefault(x => x.name == "BadgesHolder");
                if (badgeHolder == null)
                {
                    badgeHolder = new VisualElement { name = "BadgesHolder" };
                    badgeHolder.AddStyleSheetPath("VFXIconBadge");
                    target.Add(badgeHolder);
                }
                badgeHolder.Add(badge);

                if (errorOrigin == VFXErrorOrigin.Compilation)
                {
                    m_CompileBadges.Add(badge);
                }
                else
                {
                    List<VFXIconBadge> badges;
                    if (!m_InvalidateBadges.TryGetValue(model, out badges))
                    {
                        badges = new List<VFXIconBadge>();
                        m_InvalidateBadges[model] = badges;
                    }
                    badges.Add(badge);
                }
                badge.AddManipulator(new Clickable(() =>
                {
                    m_ModelsWithHiddenBadges.Add(model);
                    badge.RemoveFromHierarchy();
                }));
                badge.AddManipulator(new ContextualMenuManipulator(e =>
                {
                    e.menu.AppendAction("Hide", _ =>
                    {
                        m_ModelsWithHiddenBadges.Add(model);
                        badge.RemoveFromHierarchy();
                    });

                    if (type != VFXErrorType.Error)
                    {
                        e.menu.AppendAction("Ignore", _ =>
                        {
                            badge.RemoveFromHierarchy();
                            model.IgnoreError(error);
                        });
                    }
                    e.StopImmediatePropagation();
                }));
            }
        }

        private void ClearAllErrors(VFXModel model, VFXErrorOrigin errorOrigin)
        {
            if (errorOrigin == VFXErrorOrigin.Compilation)
            {
                foreach (var badge in m_CompileBadges)
                {
                    badge.RemoveFromHierarchy();
                }
                m_CompileBadges.Clear();
            }
            else
            {
                if (!object.ReferenceEquals(model, null))
                {
                    if (m_InvalidateBadges.TryGetValue(model, out var badges))
                    {
                        foreach (var badge in badges)
                        {
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
            board.BringToFront();
        }

        public bool TryAttachTo(VisualEffect visualEffect, bool showNotification)
        {
            if (m_Controller == null || m_Controller.graph == null || visualEffect == null)
            {
                return false;
            }

            bool attached = false;

            VisualEffectAsset controllerAsset = controller.graph.visualEffectResource.asset;
            if (controllerAsset != null && controllerAsset == visualEffect.visualEffectAsset)
            {
                attached = m_ComponentBoard.Attach(visualEffect);
                m_ProfilingBoard.Attach(visualEffect);
            }

            if (attached && showNotification)
            {
                var vfxWindow = VFXViewWindow.GetWindowNoShow(this);
                if (vfxWindow != null)
                {
                    string truncatedObjectName = TruncateName(visualEffect.name, MaximumNameLengthInNotification);
                    vfxWindow.ShowNotification(new GUIContent($"Attached to {truncatedObjectName}"), 1.5);
                    vfxWindow.Repaint();
                }
            }

            m_VFXSettings.AttachedVisualEffect = attachedComponent;
            UpdateToolbarButtons();
            return attached;
        }

        internal void Detach()
        {
            m_ComponentBoard.Detach();
            m_ProfilingBoard.Detach();

            UpdateToolbarButtons();
        }

        internal void AttachToSelection()
        {
            TryAttachTo((Selection.activeObject as GameObject)?.GetComponent<VisualEffect>(), true);
        }

        private void UpdateToolbarButtons()
        {
            if (attachedComponent != null)
            {
                m_AttachDropDownButton.AddToClassList("checked");
                m_AttachDropDownButton.icon = m_LinkedIcon;
            }
            else
            {
                m_AttachDropDownButton.RemoveFromClassList("checked");
                m_AttachDropDownButton.icon = m_UnlinkedIcon;
            }

            m_LockToggle.tooltip = locked ? Contents.clickToUnlock.text : Contents.clickToLock.text;

            m_AttachDropDownButton.tooltip = attachedComponent != null && !string.IsNullOrEmpty(attachedComponent.name)
                ? string.Format(Contents.attachedToGameObject.text, TruncateName(attachedComponent.name, MaximumNameLengthInNotification))
                : Contents.notAttached.text;
        }

        string TruncateName(string nameToTruncate, int maxLength)
        {
            return nameToTruncate.Length > maxLength
                ? nameToTruncate.Substring(0, maxLength) + "..."
                : nameToTruncate;
        }

        void OnUndoPerformed()
        {
            foreach (var anchor in allDataAnchors)
            {
                anchor.ForceUpdate();
            }
        }

        void OnBackToParent()
        {
            VFXViewWindow.GetWindow(this).PopResource();
        }

        void OnToggleLock(ChangeEvent<bool> evt)
        {
            m_VFXSettings.AttachedLocked = !locked;
            if (!locked)
            {
                AttachToSelection();
            }
            else
            {
                UpdateToolbarButtons();
            }
        }

        void ToggleBlackboard(ChangeEvent<bool> e)
        {
            ToggleBlackboard();
        }

        public void ToggleComponentBoard()
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
            m_ComponentBoard.RefreshInitializeErrors();
        }

        public void ToggleDebugPanels()
        {
            if (m_ProfilingBoard.parent != null && attachedComponent != null)
            {
                m_ProfilingBoard.TogglePanelsVisibility();
            }
        }

        public void ToggleProfilingBoard()
        {
            m_ToggleProfilingBoard.value = !m_ToggleProfilingBoard.value;
        }

        void OnToggleProfilingBoard()
        {
            if (m_ProfilingBoard.parent == null)
            {
                Insert(childCount - 1, m_ProfilingBoard);
                m_ProfilingBoard.Enable();
                m_ProfilingBoard.RegisterCallback<GeometryChangedEvent>(OnFirstProfilingBoardGeometryChanged);
            }
            else
            {
                m_ProfilingBoard.Disable();
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

        void OnFirstProfilingBoardGeometryChanged(GeometryChangedEvent e)
        {
            if (m_FirstResize)
            {
                m_ProfilingBoard.ValidatePosition();
                m_ProfilingBoard.UnregisterCallback<GeometryChangedEvent>(OnFirstProfilingBoardGeometryChanged);
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

        void OnGeometryChanged(GeometryChangedEvent e)
        {
            m_FirstResize = true;
            m_ComponentBoard.ValidatePosition();
            m_ProfilingBoard.ValidatePosition();
            m_Blackboard.ValidatePosition();
        }

        Toggle m_ToggleComponentBoard;
        void ToggleComponentBoard(ChangeEvent<bool> e)
        {
            ToggleComponentBoard();
        }

        Toggle m_ToggleProfilingBoard;
        void ToggleProfilingBoard(ChangeEvent<bool> e)
        {
            OnToggleProfilingBoard();
        }

        public void OnVisualEffectComponentChanged(IEnumerable<VisualEffect> visualEffects)
        {
            if (attachedComponent == null || attachedComponent.visualEffectAsset != controller.graph.visualEffectResource.asset)
            {
                Detach();
                foreach (VisualEffect visualEffect in visualEffects)
                {
                    TryAttachTo(visualEffect, true);
                }
            }
        }

        public void Delete()
        {
            Delete(null, AskUser.DontAskUser);
        }

        void Delete(string cmd, AskUser askUser)
        {
            if (!IsLocked())
            {
                var selectionCopy = selection.ToList();
                var shouldClearBlackboardSelection = false;
                foreach (var category in selectionCopy.OfType<VFXBlackboardCategory>().ToArray())
                {
                    if (TryRemoveCategory(category.title))
                    {
                        selectionCopy.Remove(category);
                        blackboard.RemoveCategory(category.title);
                        shouldClearBlackboardSelection = true;
                    }
                    selectionCopy.RemoveAll(x => x is VFXBlackboardField field && field.controller.model.category == category.title);
                }

                foreach (var parameter in selectionCopy.OfType<VFXBlackboardField>().ToArray())
                {
                    selectionCopy.Remove(parameter);
                    shouldClearBlackboardSelection |= blackboard.RemoveParameter(parameter.PropertyItem);
                }

                foreach (var attributeField in selectionCopy.OfType<VFXBlackboardAttributeField>().ToArray())
                {
                    selectionCopy.Remove(attributeField);
                    if (attributeField.attribute.isEditable)
                    {
                        shouldClearBlackboardSelection |= blackboard.RemoveCustomAttribute(attributeField.attribute);
                    }
                    else
                    {
                        Debug.LogWarning(attributeField.attribute.isBuiltIn
                            ? "You cannot delete built-in attributes"
                            : "You cannot delete attributes imported by subgraphs");
                    }
                }

                if (shouldClearBlackboardSelection)
                {
                    blackboard.EmptySelection();
                }

                // Remove VFX items from the editor selection
                var newSelection = Selection.objects.ToList();
                newSelection.RemoveAll(x => x is VFXModel);

                selection.Clear();
                Selection.objects = newSelection.ToArray();
                controller.Remove(selectionCopy.OfType<IControlledElement>().Select(t => t.controller).ToArray(), true);
            }
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
                    UpdateSystems();
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
                m_ComponentBoard.controller = null;
                m_ProfilingBoard.controller = null;
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

        public bool IsAssetEditable() => controller.model == null || controller.model.IsAssetEditable();

        void NewControllerSet()
        {
            m_Blackboard.controller = controller;
            m_ComponentBoard.controller = controller;
            m_ProfilingBoard.controller = controller;
            m_VCSDropDown.style.display = (Provider.enabled && !IsOffline()) ? DisplayStyle.Flex : DisplayStyle.None;

            if (controller != null)
            {
                m_NoAssetElement.RemoveFromHierarchy();
                SetToolbarEnabled(true);

                bool isSubGraph = this.controller.model.isSubgraph;
                m_CompileDropDownButton.SetEnabled(!isSubGraph);
                m_AttachDropDownButton.SetEnabled(!isSubGraph);
                m_LockToggle.SetEnabled(!isSubGraph);

                OnFocus();
            }
            else
            {
                VFXSlotContainerEditor.SceneViewVFXSlotContainerOverlay.UpdateFromVFXView(this, null);
                if (m_NoAssetElement.parent == null)
                {
                    Add(m_NoAssetElement);
                    SetToolbarEnabled(false);
                }
            }

            if (m_VFXSettings.AttachedVisualEffect != null)
            {
                TryAttachTo(m_VFXSettings.AttachedVisualEffect, true);
            }
        }

        bool IsLocked() => controller != null && !IsAssetEditable();

        public void OnFocus()
        {
            if (IsLocked())
            {
                m_LockedElement.style.display = DisplayStyle.Flex;
                m_Blackboard.LockUI();
                m_ComponentBoard.LockUI();
                contentViewContainer.style.opacity = GrayedOutGraphOpacity;
                m_Systems.ForEach(x => x.SetEnabled(false));
                GetAllContexts().Select(x => x.Q<VFXContextBorder>()).ToList().ForEach(x => x.style.opacity = GrayedOutGraphOpacity);
            }
            else
            {
                m_LockedElement.style.display = DisplayStyle.None;
                m_Blackboard.UnlockUI();
                m_ComponentBoard.UnlockUI();
                contentViewContainer.style.opacity = 1f;
                m_Systems.ForEach(x => x.SetEnabled(true));
                GetAllContexts().Select(x => x.Q<VFXContextBorder>()).ToList().ForEach(x => x.style.opacity = 1f);
            }

            UpdateVCSState();
            // When we get the focus by clicking on a selected node we should update the editor selection
            // Otherwise (when we clicked in the void) don't discard the current selection (keep the selected GO for instance)
            if (selection.Any(x => x.HitTest(m_pastCenter)))
            {
                UpdateGlobalSelection();
            }

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

            if (controller?.graph == null)
            {
                return;
            }

            rectToFit =  controller.graph.UIInfos.uiBounds;
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
                    newElement.controller.model.RefreshErrors();
                    if (m_UpdateSelectionWithNewNode)
                    {
                        if (!selectionCleared)
                        {
                            selectionCleared = true;
                            base.ClearSelection();
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
            if (Event.current != null && Event.current.modifiers != EventModifiers.None)
            {
                return;
            }

            if (IsLocked())
            {
                return;
            }

            GUIView guiView = panel.InternalGetGUIView();
            if (guiView == null)
                return;
            Vector2 point = ScreenToViewPosition(ctx.screenMousePosition);

            List<VisualElement> picked = new List<VisualElement>();
            panel.PickAll(point, picked);

            VFXContextUI context = picked.OfType<VFXContextUI>().FirstOrDefault();

            if (context != null)
            {
                if (context.canHaveBlocks)
                    context.OnCreateBlock(point);
            }
            else
            {
                VFXDataEdge edge = picked.OfType<VFXDataEdge>().FirstOrDefault();
                if (edge != null)
                    VFXFilterWindow.Show(point, ctx.screenMousePosition, new VFXNodeProvider(controller, (d, v) => AddNodeOnEdge(d, v, edge.controller), null, new Type[] { typeof(VFXOperator) }));
                else
                    VFXFilterWindow.Show(point, ctx.screenMousePosition, m_NodeProvider);
            }
        }

        public void CreateNewFromTemplate(string templatePath, string assetPath)
        {
            VisualEffectAssetEditorUtility.CreateTemplateAsset(assetPath, templatePath);
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
            var window = VFXViewWindow.GetWindow(vfxAsset, false);
            // The window can be null if we try to create a new asset from an editor window with another asset opened
            window?.LoadAsset(vfxAsset, null);
        }

        public void CreateTemplateSystem(string path, Vector2 tPos, VFXGroupNode groupNode, bool centerInView)
        {
            var resource = VisualEffectResource.GetResourceAtPath(path);
            if (resource != null)
            {
                VFXViewController templateController = VFXViewController.GetController(resource, true);
                templateController.useCount++;

                if (centerInView)
                {
                    var pos = contentViewContainer.style.translate.value;
                    var scale = contentViewContainer.style.scale;
                    tPos.x = (resolvedStyle.width / 2f - pos.x.value) / scale.value.value.x - templateController.graph.UIInfos.uiBounds.width / 2f;
                    tPos.y = (resolvedStyle.height / 2f - pos.y.value) / scale.value.value.x - templateController.graph.UIInfos.uiBounds.height / 2f;
                }

                var data = VFXCopy.SerializeElements(templateController.allChildren, templateController.graph.UIInfos.uiBounds, null, null, null);
                VFXPaste.UnserializeAndPasteElements(controller, tPos, data, this, groupNode != null ? groupNode.controller : null);

                templateController.useCount--;
            }
        }

        internal void Compile()
        {
            VFXLibrary.LogUnsupportedSRP();

            if (controller.model.isSubgraph)
                controller.graph.RecompileIfNeeded(false, false);
            else
            {
                VFXGraph.explicitCompile = true;
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(controller.model));
                VFXGraph.explicitCompile = false;
            }
            foreach (var model in m_ModelsWithHiddenBadges)
            {
                model.RefreshErrors();
            }
            m_ModelsWithHiddenBadges.Clear();
        }

        internal void OnSave()
        {
            m_ComponentBoard?.DeactivateBoundsRecordingIfNeeded(); //Avoids saving the graph with unnecessary bounds computations

            controller.graph.visualEffectResource.WriteAssetWithSubAssets();
        }

        internal void SaveAs(string newPath)
        {
            m_ComponentBoard?.DeactivateBoundsRecordingIfNeeded(); //Avoids saving the graph with unnecessary bounds computations

            var resource = controller.graph.visualEffectResource;

            var oldFilePath = AssetDatabase.GetAssetPath(resource);
            if (!AssetDatabase.CopyAsset(oldFilePath, newPath))
            {
                Debug.Log($"Could not save VFX Graph at {newPath}");
            }
        }

        public EventPropagation OnCompile()
        {
            Compile();

            return EventPropagation.Stop;
        }

        void AddVFXParameter(Vector2 pos, VFXParameterController parameterController, VFXGroupNode groupNode)
        {
            if (controller == null || parameterController == null) return;

            controller.AddVFXParameter(pos, parameterController, groupNode != null ? groupNode.controller : null);
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

        public void ToggleBlackboard()
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


            if (startAnchor is VFXDataAnchor anchor)
            {
                var controllers = controller.GetCompatiblePorts(anchor.controller, nodeAdapter);
                return controllers
                    .Where(x => !x.isSubgraphActivation)
                    .Select(t => (Port)GetDataAnchorByController(t))
                    .Where(t => t != null)
                    .ToList();
            }
            else
            {
                var controllers = controller.GetCompatiblePorts((startAnchor as VFXFlowAnchor).controller, nodeAdapter);
                return controllers.Select(t => (Port)GetFlowAnchorByController(t)).Where(t => t != null).ToList();
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

                foreach (var dataEdge in change.elementsToRemove.OfType<VFXDataEdge>())
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

            var objectsSelected = new List<UnityEngine.Object>();
            var emptyBlackboardSelection = false;
            foreach (var element in selection)
            {
                switch (element)
                {
                    case VFXNodeUI nodeUI:
                        if (nodeUI.controller.model != null)
                        {
                            objectsSelected.Add(nodeUI.controller.model);
                            emptyBlackboardSelection = true;
                        }
                        break;
                    case VFXBlackboardField blackboardField:
                        if (blackboardField.controller.model != null)
                        {
                            objectsSelected.Add(blackboardField.controller.model);
                        }
                        break;
                    case VFXBlackboardAttributeField aField:
                        var attributeField = aField;
                        var customAttribute = controller.graph.customAttributes.SingleOrDefault(x => string.Compare(attributeField.text, x.attributeName, StringComparison.OrdinalIgnoreCase) == 0);
                        objectsSelected.Add(customAttribute);
                        break;
                    case VFXBlackboardCategory category:
                        break;
                }
            }

            if (objectsSelected.Count > 0)
            {
                Selection.objects = objectsSelected.ToArray();
                if (emptyBlackboardSelection)
                {
                    blackboard.EmptySelection();
                }
            }
        }

        internal void SelectAsset()
        {
            if (Selection.activeObject != controller.model)
            {
                Selection.activeObject = controller.model.visualEffectObject;
                EditorGUIUtility.PingObject(controller.model.visualEffectObject);
            }
        }

        internal bool IsOffline() => EditorUserSettings.WorkOffline;

        internal void GetLatest()
        {
            var asset = Provider.GetAssetByPath(AssetDatabase.GetAssetPath(controller.model.visualEffectObject));
            Task task = Provider.GetLatest(asset);
            task.Wait();
            OnFocus();
        }

        internal void Checkout()
        {
            Task task = Provider.Checkout(controller.model.visualEffectObject, CheckoutMode.Both);
            task.Wait();
            OnFocus();
        }

        void PollWindowForFocus<T>() where T : EditorWindow
        {
            if (EditorWindow.HasOpenInstances<T>())
            {
                EditorApplication.delayCall += PollWindowForFocus<T>;
            }
            else
            {
                OnFocus();
            }
        }

        internal void Revert()
        {
            var asset = Provider.GetAssetByPath(AssetDatabase.GetAssetPath(controller.model.visualEffectObject));
            var assetList = new AssetList { asset };
            WindowRevert.Open(assetList);
            EditorApplication.delayCall += PollWindowForFocus<WindowRevert>;
        }

        internal void Submit()
        {
            var asset = Provider.GetAssetByPath(AssetDatabase.GetAssetPath(controller.model.visualEffectObject));
            WindowChange.Open(new AssetList { asset }, true);
            EditorApplication.delayCall += PollWindowForFocus<WindowChange>;
        }

        void UpdateVCSState()
        {
            if (Provider.enabled && !IsOffline())
            {
                m_VCSDropDown.style.display = DisplayStyle.Flex;
                if (controller != null)
                {
                    var asset = Provider.GetAssetByPath(AssetDatabase.GetAssetPath(controller.model.visualEffectObject));
                    m_VCSDropDown.SetStatus(asset?.state ?? Asset.States.None);
                }
            }
            else
            {
                m_VCSDropDown.style.display = DisplayStyle.None;
            }
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

        public void AddRangeToSelection(List<ISelectable> selectables)
        {
            selectables.ForEach(base.AddToSelection);
            UpdateGlobalSelection();
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
            base.ClearSelection();
            blackboard.EmptySelection();

            // Wait for next frame to see if anything has been selected.
            // If not, it means we clicked in the void and then we can select the VFX asser
            EditorApplication.delayCall += SelectAssetInInspector;
        }

        public void ClearSelectionFast()
        {
            base.ClearSelection();
        }

        private void SelectAssetInInspector()
        {
            var inspector = EditorWindow.HasOpenInstances<InspectorWindow>() ? EditorWindow.GetWindow<InspectorWindow>(null, false) : null;

            if (inspector == null)
            {
                return;
            }

            var inspectorObject = inspector.GetInspectedObject();

            if (inspectorObject is VFXObject && selection.Count == 0)
            {
                var assetToSelect = controller != null && controller.model != null
                    ? controller.model.isSubgraph ? controller.model.subgraph : (VisualEffectObject)controller.model.asset
                    : null;
                if (assetToSelect == null)
                    return;
                var assetToSelectPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(assetToSelect)).Replace('\\', '/');

                // This is to select the current VFX asset in the inspector.
                // But, we temporary lock the project window during the selection so that the project browser don't change directory
                var projectBrowser = EditorWindow.HasOpenInstances<ProjectBrowser>() ? EditorWindow.GetWindow<ProjectBrowser>(null, false) : null;
                if (projectBrowser != null && !projectBrowser.isLocked && assetToSelectPath != projectBrowser.GetActiveFolderPath())
                {
                    projectBrowser.isLocked = true;
                    EditorApplication.delayCall += () => UnlockProjectBrowser(inspector, projectBrowser, 4);
                }

                Selection.activeObject = assetToSelect;
            }
        }

        private void UnlockProjectBrowser(InspectorWindow inspector, ProjectBrowser projectBrowser, int maximumRetries)
        {
            if (inspector.GetInspectedObject() == Selection.activeObject)
            {
                projectBrowser.isLocked = false;
                projectBrowser.Repaint();
            }
            else if (maximumRetries-- > 1)
            {
                EditorApplication.delayCall += () => UnlockProjectBrowser(inspector, projectBrowser, maximumRetries);
            }
        }

        VFXBlackboard m_Blackboard;
        VFXComponentBoard m_ComponentBoard;
        internal VFXProfilingBoard m_ProfilingBoard;

        public VFXBlackboard blackboard
        {
            get { return m_Blackboard; }
        }


        protected internal override bool canCutSelection => canCopySelection;

        protected internal override bool canCopySelection
        {
            get
            {
                foreach (var element in selection)
                {
                    switch (element)
                    {
                        case VFXContextUI contextUI when contextUI.controller.model is not VFXBlockSubgraphContext:
                        case VFXNodeUI or Group or VFXStickyNote or VFXBlackboardFieldBase:
                            return true;
                    }
                }

                return false;
            }
        }

        IEnumerable<Controller> ElementsToController(IEnumerable<GraphElement> elements)
        {
            return elements.OfType<IControlledElement>().Select(t => t.controller);
        }

        IEnumerable<VFXCopyPasteCommon.Attribute> ElementsToVFXAttributes(IEnumerable<GraphElement> elements)
        {
            foreach (var element in elements)
            {
                if (element is VFXBlackboardAttributeField attributeField && m_Controller.graph.attributesManager.TryFind(attributeField.attribute.title, out var attribute))
                {
                    yield return new VFXCopyPasteCommon.Attribute { name = attribute.name, type = attribute.type, description = attribute.description, canDuplicate = true };
                }
                else if (element is VFXContextUI context)
                {
                    foreach (var ctxBlock in context.controller.blockControllers)
                    {
                        var ctxAttributeUsage = (IVFXAttributeUsage)ctxBlock.model;
                        foreach (var customAttribute in ctxAttributeUsage.usedAttributes.Where(x => controller.graph.attributesManager.IsCustom(x.name)))
                        {
                            yield return new VFXCopyPasteCommon.Attribute { name = customAttribute.name, type = customAttribute.type, description = customAttribute.description, canDuplicate = false };
                        }
                    }
                }
                else if (element is VFXNodeUI nodeUI && nodeUI.controller.model is IVFXAttributeUsage attributeUsage)
                {
                    foreach (var customAttribute in attributeUsage.usedAttributes.Where(x => controller.graph.attributesManager.IsCustom(x.name)))
                    {
                        yield return new VFXCopyPasteCommon.Attribute { name = customAttribute.name, type = customAttribute.type, description = customAttribute.description, canDuplicate = false };
                    }
                }
            }
        }

        VFXParameter[] ElementsToParameters(IEnumerable<GraphElement> elements, out string[] categories)
        {
            var categoriesToCopy = elements.OfType<VFXBlackboardCategory>().Where(x => x.item.canRename).Select(x => x.title).ToArray();
            var parameters = new List<VFXParameter>();
            foreach (var parameter in m_Controller.parameterControllers.Where(x => categoriesToCopy.Contains(x.model.category)))
            {
                parameters.Add(parameter.model);
            }

            foreach (var blackboardField in elements.OfType<VFXBlackboardField>())
            {
                parameters.Add(blackboardField.controller.model);
            }

            categories = categoriesToCopy;
            return parameters.Distinct().ToArray();
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
                else if (element is Node or VFXContextUI or VFXStickyNote or VFXBlackboardFieldBase)
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

            // When locked, copy in clipboard to be able to paste in another graph
            // Unwanted side effect is that it writes to the clipboard also when duplicating, but it seems acceptable
            if (IsLocked())
            {
                string data = SerializeGraphElements(elementsToCopySet);

                if (!string.IsNullOrEmpty(data))
                {
                    clipboard = data;
                }

                elementsToCopySet.Clear();
            }
        }

        internal Rect GetElementsBounds(IEnumerable<GraphElement> elements)
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
            string result = elements.Any()
                ? VFXCopy.SerializeElements(ElementsToController(elements), GetElementsBounds(elements), ElementsToVFXAttributes(elements).Distinct(), ElementsToParameters(elements, out var categories), categories)
                : string.Empty;
            Profiler.EndSample();
            return result;
        }

        public EventPropagation DuplicateSelectionWithEdges()
        {
            if (!IsLocked())
            {
                List<Controller> sourceControllers =
                    selection.OfType<IControlledElement>().Select(t => t.controller).ToList();
                Rect bounds = GetElementsBounds(selection.OfType<IControlledElement>().OfType<GraphElement>());

                object result = VFXCopy.Copy(sourceControllers, bounds);

                var targetControllers = new List<VFXNodeController>();
                VFXPaste.Paste(controller, pasteCenter, result, this, null, targetControllers);

                base.ClearSelection();
                for (int i = 0; i < sourceControllers.Count; ++i)
                {
                    if (targetControllers[i] != null)
                    {
                        CopyInputLinks(sourceControllers[i] as VFXNodeController, targetControllers[i]);

                        if (targetControllers[i] is VFXBlockController blkController)
                            AddToSelection((rootNodes[blkController.contextController] as VFXContextUI).GetAllBlocks()
                                .First(t => t.controller == blkController));
                        else
                            AddToSelection(rootNodes[targetControllers[i]]);
                    }
                }
            }

            return EventPropagation.Stop;
        }

        public void AddToSelection(VFXModel model, int id)
        {
            VFXNodeController nodeController = controller.GetRootNodeController(model, id);

            if (nodeController != null)
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
            foreach (var st in sourceController.inputPorts.Zip(targetController.inputPorts, (s, t) => new { source = s, target = t }))
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

        internal Vector2 pasteCenter
        {
            get => contentViewContainer.WorldToLocal(m_pastCenter);
            private set => m_pastCenter = value; // Should be used only for unit testing
        }

        private bool VFXCanPaste(string data)
        {
            return IsAssetEditable() && VFXPaste.CanPaste(this, data);
        }

        public void UnserializeAndPasteElements(string operationName, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            Profiler.BeginSample("VFXPaste.VFXPaste.UnserializeAndPasteElements");
            VFXPaste.UnserializeAndPasteElements(controller, pasteCenter, data, this);
            Profiler.EndSample();
        }

        private bool TryGetOverlappingContextAbove(VFXContextUI context, out VFXContextUI overlappingContext, out float distance)
        {
            var rect = context.GetPosition();
            var posY = context.controller.model.position.y;

            var overlappingContexts = new Dictionary<VFXContextUI, float>();
            foreach (var ctx in GetAllContexts())
            {
                if (ctx == context)
                {
                    continue;
                }

                var ctxRect = ctx.GetPosition();
                var ctxPosY = ctx.controller.model.position.y;

                // Skip contexts that are side by side
                if (rect.xMin - ctxRect.xMax > -5 || rect.xMax - ctxRect.xMin < 5)
                {
                    continue;
                }

                distance = posY - ctxPosY - ctxRect.height;
                if (distance < 0 && posY > ctxRect.yMin)
                {
                    overlappingContexts[ctx] = -distance;
                }
            }

            if (overlappingContexts.Any())
            {
                var keyPair = overlappingContexts.OrderByDescending(x => x.Value).First();
                overlappingContext = keyPair.Key;
                distance = keyPair.Value;
                return true;
            }

            distance = 0f;
            overlappingContext = null;
            return false;
        }

        public void PushUnderContext(VFXContextUI context, float size)
        {
            if (size < 5) return;

            foreach (var edge in edges.OfType<VFXFlowEdge>().SkipWhile(x => x.output.GetFirstAncestorOfType<VFXContextUI>() != context))
            {
                context = edge.input.GetFirstAncestorOfType<VFXContextUI>();
                if (TryGetOverlappingContextAbove(context, out var aboveContext, out var distance))
                {
                    var rect = context.GetPosition();
                    context.controller.position = new Vector2(rect.x, rect.y + distance);
                }
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
            parent.Insert(0, m_Toolbar);
            Undo.undoRedoPerformed += OnUndoPerformed;
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            Undo.undoRedoPerformed -= OnUndoPerformed;
        }

        private void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            pasteCenter = evt.mousePosition;
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
                base.ClearSelection();

                AddRangeToSelection(graphElements.Where(x => x is not VFXSystemBorder).OfType<ISelectable>().ToList());
                e.StopPropagation();
            }
        }

        public void ToggleCollapseSelection()
        {
            var state = 0;
            foreach (var nodeUI in selection.OfType<VFXNodeUI>())
            {
                if (state == 0)
                {
                    if (nodeUI.controller.superCollapsed)
                        state = 1;
                    else if (nodeUI.expanded)
                        state = 2;
                    else state = 3;
                }

                switch (state)
                {
                    case 1:
                        nodeUI.controller.superCollapsed = false;
                        nodeUI.expanded = true;
                        break;
                    case 2:
                        nodeUI.controller.superCollapsed = false;
                        nodeUI.expanded = false;
                        break;
                    case 3:
                        nodeUI.controller.superCollapsed = true;
                        break;
                }
            }
        }

        public void GroupSelection()
        {
            if (canGroupSelection)
            {
                controller.GroupNodes(selection.OfType<ISettableControlledElement<VFXNodeController>>().Select(t => t.controller).ToArray());
            }
        }

        public void CleanUnLinkedOperators()
        {
            var isolatedOperators = controller.allChildren
                .OfType<VFXOperatorController>()
                .Where(x => !x.model.inputSlots.Any(y => y.HasLink(true)) && !x.model.outputSlots.Any(y => y.HasLink(true)))
                .ToList();
            isolatedOperators.ForEach(x => controller.RemoveElement(x));
        }

        public void ConvertToSubgraphOperator()
        {
            VFXConvertSubgraph.ConvertToSubgraphOperator(this, selection.OfType<IControlledElement>().Select(t => t.controller), GetElementsBounds(selection.Where(t => !(t is Edge)).Cast<GraphElement>()));
        }

        public void ToggleNodePropertyOrInline()
        {
            var currentSelection = selection.ToArray();
            if (currentSelection.FirstOrDefault() is VFXOperatorUI operatorUI && operatorUI.controller.model is VFXInlineOperator)
            {
                foreach (var vfxOperatorUI in currentSelection.OfType<VFXOperatorUI>())
                {
                    if (vfxOperatorUI.controller.model is VFXInlineOperator)
                    {
                        var param = vfxOperatorUI.controller.ConvertToProperty();
                        var ui = GetAllNodes().Single(x => x.controller.model == param);
                        AddToSelection(ui);
                        RemoveFromSelection(vfxOperatorUI);
                    }
                }
                blackboard.Update(true);
            }
            else if (currentSelection.FirstOrDefault() is VFXParameterUI)
            {
                foreach (var vfxParameterUI in currentSelection.OfType<VFXParameterUI>())
                {
                    var inlineOperator = vfxParameterUI.controller.ConvertToInline();
                    var ui = GetAllNodes().Single(x => x.controller.model == inlineOperator);
                    AddToSelection(ui);
                    RemoveFromSelection(vfxParameterUI);
                }
            }
        }

        void InsertTemplate(Vector2 position)
        {
            void InsertFromTemplate(string templatePath, string assetPath)
            {
                CreateTemplateSystem(templatePath, position, null, false);
            }

            position = contentViewContainer.WorldToLocal(position);
            GraphViewTemplateWindow.ShowInsertTemplate(new VFXTemplateHelperInternal(), InsertFromTemplate);
        }

        public void AddStickyNote(Vector2 position, VFXGroupNode group = null)
        {
            position = contentViewContainer.WorldToLocal(position);
            controller.AddStickyNote(position, group != null ? group.controller : null);
        }

        void AddGroupNode(Vector2 position)
        {
            position = contentViewContainer.WorldToLocal(position);
            controller.AddGroupNode(position);
        }

        void EnterSubgraph(VFXModel node, bool openInNewTab)
        {
            VisualEffectResource resource = null;
            if (node is VFXSubgraphOperator subGraph)
            {
                resource = subGraph.subgraph.GetResource();
            }
            else if (node is VFXSubgraphBlock subGraph2)
            {
                resource = subGraph2.subgraph.GetResource();
            }
            else if (node is VFXSubgraphContext subGraph3)
            {
                resource = subGraph3.subgraph.GetResource();
            }

            var window = VFXViewWindow.GetWindow(resource, openInNewTab);

            if (window != null)
            {
                // This can happen if the "no asset" window is opened
                if (window.graphView.controller == null)
                {
                    window.LoadResource(resource);
                }
                window.Focus();
            }
            else
            {
                VFXViewWindow.GetWindow(this).PushResource(resource);
            }
        }

        void OnEnterSubgraph(DropdownMenuAction e)
        {
            EnterSubgraph(e.userData as VFXModel, false);
        }

        void OnCreateNodeOnEdge(DropdownMenuAction e)
        {
            CreateNodeOnEdge(e.eventInfo.mousePosition);
        }

        public void CreateNodeOnEdge(Vector2 mousePosition)
        {
            var edge = selection.OfType<VFXDataEdge>().FirstOrDefault();
            if (edge != null)
            {
                VFXFilterWindow.Show( mousePosition, ViewToScreenPosition(mousePosition), new VFXNodeProvider(controller, (d, v) => AddNodeOnEdge(d, v, edge.controller), null, new[] { typeof(VFXOperator) }));
            }
        }

        // Todo: Uncomment this code and test it
        void AddNodeOnEdge(Variant variant, Vector2 position, VFXDataEdgeController edge)
        {
            position = this.ChangeCoordinatesTo(contentViewContainer, position);

            position.x -= 60;
            position.y -= 60;

            position = contentViewContainer.ChangeCoordinatesTo(this, position);

            var newNodeController = AddNode(variant, position);

            if (newNodeController == null)
                return;

            foreach (var outputPort in newNodeController.outputPorts)
            {
                // Revert type constraint so that the edge input type is preserved
                if (controller.CreateLink(edge.input, outputPort, revertTypeConstraint: true))
                    break;
            }
            foreach (var inputPort in newNodeController.inputPorts)
            {
                if (controller.CreateLink(inputPort, edge.output))
                    break;
            }
        }

        private void OnLockedContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Copy", CopyWhenLockedAction, x => canCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled, null);
        }

        private void CopyWhenLockedAction(DropdownMenuAction obj)
        {
            CopySelectionCallback();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (controller == null)
                return;
            if (!IsAssetEditable())
            {
                evt.menu.MenuItems().Clear();
                evt.menu.AppendAction("Copy", CopyWhenLockedAction, x => canCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled, null);
                return;
            }

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
                    evt.menu.AppendAction("Open Subgraph", OnEnterSubgraph, e => DropdownMenuAction.Status.Normal, node.controller.model);
                }
                evt.menu.AppendAction("Clear Ignored Errors", a => node.controller.model.ClearIgnoredErrors(), node.controller.model.HasIgnoredErrors() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            if (evt.target is VFXDataEdge edge)
            {
                evt.menu.InsertAction(0, "Create Node", OnCreateNodeOnEdge, t => DropdownMenuAction.Status.Normal, edge.controller);
            }

            if (evt.target is VFXView)
            {
                evt.menu.InsertAction(0, "Insert Template", (e) => { InsertTemplate(mousePosition); }, (e) => controller.graph.visualEffectResource.isSubgraph ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
                evt.menu.InsertAction(1, "Create Sticky Note", (e) => { AddStickyNote(mousePosition); }, (e) => DropdownMenuAction.Status.Normal);
                evt.menu.InsertAction(2, "Create Group Node", (e) => { AddGroupNode(mousePosition); }, (e) => DropdownMenuAction.Status.Normal);

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

                var window = VFXViewWindow.GetWindow(this);
                if (window != null && window.resourceHistory.Any())
                {
                    evt.menu.AppendAction("Back To Parent Graph", e => window.PopResource());
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
                evt.menu.AppendAction("Duplicate with edges", (a) => { DuplicateSelectionWithEdges(); },
                    (a) => { return canDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled; });
                evt.menu.AppendSeparator();
            }

            if (selection.OfType<VFXOperatorUI>().Any(t => !t.superCollapsed))
                evt.menu.AppendAction("Collapse Operators", CollapseOperator, e => DropdownMenuAction.Status.Normal, true);
            if (selection.OfType<VFXOperatorUI>().Any(t => t.superCollapsed))
                evt.menu.AppendAction("Uncollapse Operators", CollapseOperator, e => DropdownMenuAction.Status.Normal, false);
            if (selection.OfType<VFXStickyNote>().Any() && evt.menu.MenuItems().OfType<DropdownMenuAction>().All(x => x.name != "Delete"))
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Delete", OnDeleteStickyNote, e => canDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            if (evt.target is GraphView || evt.target is Node)
            {
                var copyMenu = evt.menu.MenuItems().OfType<DropdownMenuAction>().SingleOrDefault(x => x.name == "Copy");
                if (copyMenu != null)
                {
                    var index = evt.menu.MenuItems().IndexOf(copyMenu);
                    evt.menu.InsertAction(index + 1, "Paste", (a) => { PasteCallback(); }, (a) => { return canPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled; });
                }
            }
        }

        static readonly string s_DeleteEventCommandName = GetDeleteEventCommandName();

        static string GetDeleteEventCommandName()
        {
            var fieldInfo = Type.GetType("UnityEngine.EventCommandNames, UnityEngine")?.GetField("Delete", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (fieldInfo != null)
            {
                if (fieldInfo.GetValue(null) is string commandName)
                {
                    return commandName;
                }

                Debug.Log("API has changed, Delete command name field is either null or not a string anymore");
            }
            else
            {
                Debug.Log("API has changed, could not retrieve Delete command name field using reflection");
            }

            return "Delete";
        }

        void OnDeleteStickyNote(DropdownMenuAction menuAction)
        {
            using var ev = ExecuteCommandEvent.GetPooled(s_DeleteEventCommandName);
            SendEvent(ev);
        }

        private void OnDuplicateBlackBoardCategory(DropdownMenuAction obj)
        {
            DuplicateBlackBoardCategorySelection();
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
            ConvertToSubgraphOperator();
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

        public void UpdateSystems()
        {
            while (m_Systems.Count() > controller.systems.Count())
            {
                VFXSystemBorder border = m_Systems.Last();
                m_Systems.RemoveAt(m_Systems.Count - 1);
                border.RemoveFromHierarchy();
            }

            foreach (var system in m_Systems)
            {
                system.Update();
            }

            while (m_Systems.Count < controller.systems.Count)
            {
                VFXSystemBorder border = new VFXSystemBorder();
                m_Systems.Add(border);
                AddElement(border);
                border.controller = controller.systems[m_Systems.Count - 1];
            }

            foreach (var context in GetAllContexts())
            {
                context.UpdateLabel();
            }
        }

        public void UpdateIsSubgraph()
        {
            if (controller == null)
                return;

            m_BackButton.style.display = controller.graph != null && controller.graph.visualEffectResource.isSubgraph && VFXViewWindow.GetWindow(this).CanPopResource()
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        internal DragAndDropVisualMode GetDragAndDropModeForVisualEffectObject(VisualEffectObject visualEffectObject)
        {
            if (visualEffectObject != null && visualEffectObject != controller.model.visualEffectObject)
            {
                var isOperator = visualEffectObject is VisualEffectSubgraphOperator;
                var isBlock = visualEffectObject is VisualEffectSubgraphBlock;
                var graph = visualEffectObject.GetResource().GetOrCreateGraph();
                graph.BuildSubgraphDependencies();
                var draggedObjectDependencies = graph.subgraphDependencies;

                // Circular dependency
                if (draggedObjectDependencies.Contains(controller.model.visualEffectObject))
                {
                    return DragAndDropVisualMode.Rejected;
                }

                var vfxIntoVfx = !isOperator && !isBlock && !controller.model.isSubgraph; // dropping a vfx into a vfx

                return vfxIntoVfx || isOperator
                    ? DragAndDropVisualMode.Move
                    : DragAndDropVisualMode.None;
            }

            return DragAndDropVisualMode.Rejected;
        }

        void OnDragUpdated(DragUpdatedEvent e)
        {
            if (controller == null || !IsAssetEditable())
                return;

            var data = DragAndDrop.GetGenericData("DragSelection");
            if (data is List<IParameterItem> items)
            {
                // Do not allow drag&drop from another graph
                if (items.All(x => x.graph != controller.graph))
                {
                    return;
                }

                // Forbid dropping a subgraph output property multiple times
                if (items.OfType<PropertyItem>().Select(x => controller.parameterControllers.SingleOrDefault(c => c.exposedName == x.title)).Any(x => x is { isOutput: true, hasNodes: true }))
                {
                    return;
                }

                if (items.Any(x => x is not IParameterCategory))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                }
            }
            else
            {
                var modes = DragAndDrop.objectReferences
                    .OfType<VisualEffectObject>()
                    .Select(GetDragAndDropModeForVisualEffectObject)
                    .Distinct()
                    .ToArray();
                if (modes.Contains(DragAndDropVisualMode.Move))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                }
                else if (modes.Contains(DragAndDropVisualMode.Rejected))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                }
            }
        }

        void OnDragPerform(DragPerformEvent e)
        {
            if (controller == null || !IsAssetEditable())
                return;
            var groupNode = GetPickedGroupNode(e.mousePosition);

            var data = DragAndDrop.GetGenericData("DragSelection");
            if (data is List<IParameterItem> items && items.Any(t => t is ParameterItem))
            {
                var mousePosition = contentViewContainer.WorldToLocal(e.mousePosition);
                UpdateSelectionWithNewNode();
                foreach (var item in items)
                {
                    if (item is PropertyItem)
                    {
                        var parameterController = controller.parameterControllers.Single(x => x.exposedName == item.title);
                        AddVFXParameter(mousePosition - new Vector2(50, 20), parameterController, groupNode);
                        mousePosition.y += 40;
                    }
                    else if (item is AttributeItem)
                    {
                        var attributeParameter = ScriptableObject.CreateInstance<VFXAttributeParameter>();
                        controller.AddVFXModel(mousePosition, attributeParameter);
                        attributeParameter.SetSettingValue("attribute", item.title);
                        mousePosition.y += 120;
                    }
                }
                blackboard.EmptySelection();
                DragAndDrop.AcceptDrag();
            }
            else
            {
                DragAndDrop.AcceptDrag();
                var offset = Vector2.zero;

                foreach (var draggedObject in DragAndDrop.objectReferences.OfType<VisualEffectObject>())
                {
                    var mousePosition = contentViewContainer.WorldToLocal(e.mousePosition) + offset;

                    var dropMode = GetDragAndDropModeForVisualEffectObject(draggedObject);
                    if (dropMode == DragAndDropVisualMode.Rejected)
                    {
                        Debug.LogWarning($"Could not drag & drop asset '{draggedObject.name}' because it's not compatible with the graph");
                        continue;
                    }
                    if (dropMode == DragAndDropVisualMode.None)
                    {
                        Debug.LogWarning($"Could not drag & drop asset '{draggedObject.name}' because a VFX Graph cannot be dropped into a subgraph");
                        continue;
                    }

                    if (draggedObject is VisualEffectAsset || draggedObject is VisualEffectSubgraphOperator)
                    {
                        if (draggedObject is VisualEffectSubgraphOperator subgraphOperator)
                        {
                            var graph = subgraphOperator.GetResource().GetOrCreateGraph();
                            if (HasCustomAttributeConflicts(graph.attributesManager.GetCustomAttributes()))
                            {
                                continue;
                            }
                        }
                        VFXModel newModel = draggedObject is VisualEffectAsset
                            ? ScriptableObject.CreateInstance<VFXSubgraphContext>()
                            : ScriptableObject.CreateInstance<VFXSubgraphOperator>();

                        UpdateSelectionWithNewNode();
                        controller.AddVFXModel(mousePosition, newModel);

                        newModel.SetSettingValue("m_Subgraph", draggedObject);
                    }
                    else if (draggedObject is VisualEffectSubgraphBlock subgraphBlock && !controller.model.isSubgraph) //can't drag a vfx subgraph block in a subgraph operator or a subgraph block
                    {
                        VFXContextType contextKind = subgraphBlock.GetResource().GetOrCreateGraph().children.OfType<VFXBlockSubgraphContext>().First().compatibleContextType;
                        VFXModelDescriptor<VFXContext> contextType = VFXLibrary.GetContexts().First(t => t.modelType == typeof(VFXBasicInitialize));
                        if ((contextKind & VFXContextType.Update) == VFXContextType.Update)
                            contextType = VFXLibrary.GetContexts().First(t => t.modelType == typeof(VFXBasicUpdate));
                        else if ((contextKind & VFXContextType.Spawner) == VFXContextType.Spawner)
                            contextType = VFXLibrary.GetContexts().First(t => t.modelType == typeof(VFXBasicSpawner));
                        else if ((contextKind & VFXContextType.Output) == VFXContextType.Output)
                            contextType = VFXLibrary.GetContexts().First(t => t.modelType == typeof(VFXPlanarPrimitiveOutput) && t.model.taskType == VFXTaskType.ParticleQuadOutput);

                        UpdateSelectionWithNewNode();
                        VFXContext ctx = controller.AddVFXContext(mousePosition, contextType.variant);

                        VFXModel newModel = ScriptableObject.CreateInstance<VFXSubgraphBlock>();

                        newModel.SetSettingValue("m_Subgraph", subgraphBlock);

                        UpdateSelectionWithNewNode();
                        ctx.AddChild(newModel);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not drag & drop asset '{draggedObject.name}' because subgraph blocks cannot be added to a subgraph operator or subgraph block");
                        continue;
                    }

                    offset += new Vector2(20, 20);
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

        public void DuplicateBlackboardFieldSelection()
        {
            var selectedBlackFields = selection
                .OfType<VFXBlackboardField>()
                .ToList();
            string lastDuplicateParameter = null;
            selectedBlackFields.ForEach(x => lastDuplicateParameter = blackboard.DuplicateParameter(x));
        }

        public string DuplicateBlackBoardCategory(string category, string newCategoryName = null, string[] parametersToDuplicate = null)
        {
            if (IsLocked())
                return null;

            if (string.IsNullOrEmpty(newCategoryName))
            {
                newCategoryName = VFXParameterController.MakeNameUnique(category, controller.graph.UIInfos.categories?.Select(x => x.name).ToHashSet() ?? new HashSet<string>());
            }

            controller.graph.UIInfos.categories ??= new List<VFXUI.CategoryInfo>();
            controller.graph.UIInfos.categories.Add(new VFXUI.CategoryInfo { name = newCategoryName });

            var newParametersControllers = parametersToDuplicate == null || parametersToDuplicate.Length == 0
                ? controller.parameterControllers.Where(x => x.model.category == category).ToArray()
                : controller.parameterControllers.Where(x => parametersToDuplicate.Contains(x.exposedName)).ToArray();

            for (var i = 0; i < newParametersControllers.Length; i++)
            {
                var parameterController = newParametersControllers[i];
                var newParameterName = VFXParameterController.MakeNameUnique(m_Controller, parameterController.exposedName);
                var newVfxParameter = VFXParameter.Duplicate(newParameterName, parameterController.model);
                controller.AddVFXModel(Vector2.zero, newVfxParameter);

                bool groupChanged = false;
                controller.SyncControllerFromModel(ref groupChanged);

                var newParameterController = blackboard.controller.parameterControllers.Single(x => x.model == newVfxParameter);
                newVfxParameter.category = newCategoryName;

                if (parameterController.spaceableAndMasterOfSpace)
                {
                    newParameterController.space = parameterController.space;
                }

                // Only notify for the last one
                blackboard.AddParameter(newParameterController, i == newParametersControllers.Length - 1);
            }

            m_Controller.graph.SetExpressionValueDirty();

            return newCategoryName;
        }

        public VFXParameterController DuplicateBlackboardField(VFXBlackboardField blackboardField)
        {
            if (IsLocked())
                return null;

            var copyName = VFXParameterController.MakeNameUnique(m_Controller, blackboardField.controller.exposedName);
            var newVfxParameter = VFXParameter.Duplicate(copyName, blackboardField.controller.model);
            controller.AddVFXModel(Vector2.zero, newVfxParameter);

            bool groupChanged = false;
            controller.SyncControllerFromModel(ref groupChanged);

            var newParameterController = blackboard.controller.parameterControllers.Single(x => x.model == newVfxParameter);

            if (blackboardField.controller.spaceableAndMasterOfSpace)
            {
                newParameterController.space = blackboardField.controller.space;
            }

            m_Controller.graph.SetExpressionValueDirty();

            return newParameterController;
        }

        public void DuplicateBlackBoardCategorySelection()
        {
            var selectedBlackboardCategories = selection
                .OfType<VFXBlackboardCategory>()
                .ToList();
            selectedBlackboardCategories.ForEach(x => blackboard.DuplicateCategory(x.title));
        }

        void OnSceneGUI(SceneView sv)
        {
            try // make sure we don't break the whole scene
            {
                if (controller != null && controller.model && attachedComponent != null)
                {
                    var controllers = new List<IGizmoController>();
                    foreach (var selectable in selection)
                    {
                        if (selectable is IControlledElement<VFXParameterController> { controller: IGizmoController gizmoController })
                        {
                            controllers.Add(gizmoController);
                        }
                        else if (selectable is ISettableControlledElement<VFXNodeController> { controller: IGizmoController gizmoController2 })
                        {
                            controllers.Add(gizmoController2);
                        }
                    }

                    VFXSlotContainerEditor.SceneViewVFXSlotContainerOverlay.UpdateFromVFXView(this, controllers);
                }
                else
                {
                    VFXSlotContainerEditor.SceneViewVFXSlotContainerOverlay.UpdateFromVFXView(this, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
