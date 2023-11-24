using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Rendering.RenderGraphModule;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Editor window class for the Render Graph Viewer
    /// </summary>
    [MovedFrom("")]
    public partial class RenderGraphViewer : EditorWindow, ISupportsOverlays
    {
        static class Names
        {
            public const string kCaptureButton = "capture-button";
            public const string kCurrentGraphDropdown = "current-graph-dropdown";
            public const string kCurrentExecutionDropdown = "current-execution-dropdown";
            public const string kPassFilterField = "pass-filter-field";
            public const string kResourceFilterField = "resource-filter-field";
            public const string kContentContainer = "content-container";
            public const string kPassList = "pass-list";
            public const string kPassListScrollView = "pass-list-scroll-view";
            public const string kResourceListScrollView = "resource-list-scroll-view";
            public const string kResourceGridScrollView = "resource-grid-scroll-view";
            public const string kResourceGrid = "resource-grid";
            public const string kGridlineContainer = "grid-line-container";
            public const string kHoverOverlay = "hover-overlay";
            public const string kEmptyStateMessage = "empty-state-message";
        }

        static class Classes
        {
            public const string kPassListItem = "pass-list__item";
            public const string kPassListPaddingItem = "pass-list-padding-item";
            public const string kPassTitle = "pass-title";
            public const string kPassBlock = "pass-block";
            public const string kPassBlockCulledPass = "pass-block--culled";
            public const string kPassBlockAsyncPass = "pass-block--async";
            public const string kPassHighlight = "pass--highlight";
            public const string kPassHighlightBorder = "pass--highlight-border";
            public const string kPassMergeIndicator = "pass-merge-indicator";
            public const string kPassCompatibilityMessageIndicator = "pass-compatibility-message-indicator";

            public const string kPassCompatibilityMessageIndicatorAnimation =
                "pass-compatibility-message-indicator--anim";

            public const string kPassCompatibilityMessageIndicatorCompatible =
                "pass-compatibility-message-indicator--compatible";

            public const string kPassSynchronizationMessageIndicator = "pass-synchronization-message-indicator";
            public const string kResourceListItem = "resource-list__item";
            public const string kResourceListItemHighlight = "resource-list__item--highlight";
            public const string kResourceListPaddingItem = "resource-list-padding-item";
            public const string kResourceIconContainer = "resource-icon-container";
            public const string kResourceIcon = "resource-icon";
            public const string kResourceIconImported = "resource-icon--imported";
            public const string kResourceIconGlobalDark = "resource-icon--global-dark";
            public const string kResourceIconGlobalLight = "resource-icon--global-light";
            public const string kResourceIconFbfetch = "resource-icon--fbfetch";
            public const string kResourceIconTexture = "resource-icon--texture";
            public const string kResourceIconBuffer = "resource-icon--buffer";
            public const string kResourceIconAccelerationStructure = "resource-icon--acceleration-structure";
            public const string kResourceGridRow = "resource-grid__row";
            public const string kResourceGridFocusOverlay = "resource-grid-focus-overlay";
            public const string kResourceHelperLine = "resource-helper-line";
            public const string kResourceHelperLineHighlight = "resource-helper-line--highlight";
            public const string kResourceUsageRangeBlock = "usage-range-block";
            public const string kResourceUsageRangeBlockHighlight = "usage-range-block--highlight";
            public const string kResourceDependencyBlock = "dependency-block";
            public const string kResourceDependencyBlockRead = "dependency-block-read";
            public const string kResourceDependencyBlockWrite = "dependency-block-write";
            public const string kResourceDependencyBlockReadWrite = "dependency-block-readwrite";
            public const string kGridLine = "grid-line";
            public const string kGridLineHighlight = "grid-line--highlight";
        }

        const string k_TemplatePath = "Packages/com.unity.render-pipelines.core/Editor/UXML/RenderGraphViewer.uxml";

        const string k_DarkStylePath =
            "Packages/com.unity.render-pipelines.core/Editor/StyleSheets/RenderGraphViewerDark.uss";

        const string k_LightStylePath =
            "Packages/com.unity.render-pipelines.core/Editor/StyleSheets/RenderGraphViewerLight.uss";

        // keep in sync with .uss
        const int kPassWidthPx = 26;
        const int kResourceRowHeightPx = 30;
        const int kResourceColumnWidth = 220;
        const int kResourceIconSize = 16;
        const int kResourceGridMarginTopPx = 6;
        const int kDependencyBlockHeightPx = 26;
        const int kDependencyBlockWidthPx = kPassWidthPx;
        const int kPassTitleAllowanceMargin = 120;

        static readonly Color kReadWriteBlockFillColorDark = new Color32(0xA9, 0xD1, 0x36, 255);
        static readonly Color kReadWriteBlockFillColorLight = new Color32(0x67, 0x9C, 0x33, 255);

        readonly Dictionary<RenderGraph, HashSet<string>> m_RegisteredGraphs = new();
        RenderGraph.DebugData m_CurrentDebugData;

        ResourcesOverlay m_ResourcesOverlay;
        PassInspectorOverlay m_PassInspectorOverlay;
        PanManipulator m_PanManipulator;

        readonly List<PassElementInfo> m_PassElementsInfo = new(); // Indexed using visiblePassIndex
        readonly List<ResourceElementInfo> m_ResourceElementsInfo = new(); // Indexed using visibleResourceIndex
        readonly Dictionary<int, int> m_PassIdToVisiblePassIndex = new();
        readonly Dictionary<int, int> m_VisiblePassIndexToPassId = new();

        int m_CurrentHoveredVisiblePassIndex = -1;
        int m_CurrentHoveredVisibleResourceIndex = -1;
        int m_CurrentSelectedVisiblePassIndex = -1;

        PassFilter m_PassFilter = PassFilter.CulledPasses | PassFilter.RasterPasses | PassFilter.UnsafePasses | PassFilter.ComputePasses;
        PassFilterLegacy m_PassFilterLegacy = PassFilterLegacy.CulledPasses;

        ResourceFilter m_ResourceFilter =
            ResourceFilter.ImportedResources |  ResourceFilter.Textures |
            ResourceFilter.Buffers | ResourceFilter.AccelerationStructures;

        enum EmptyStateReason
        {
            None = 0,
            NoExecutionRegistered,
            NoDataAvailable,
            WaitingForCameraRender,
            EmptyPassFilterResult,
            EmptyResourceFilterResult
        };

        static readonly string[] kEmptyStateMessages =
        {
            "",
            "No Render Graph execution has been registered. Activate a viewport to trigger camera rendering.",
            "No data to display. Click refresh to capture data.",
            "Waiting for the selected camera to render. Depending on the camera, you may need to trigger rendering by selecting the Scene or Game view.",
            "No passes to display. Select a different Pass Filter to display contents.",
            "No resources to display. Select a different Resource Filter to display contents."
        };

        [MenuItem("Window/Analysis/Render Graph Viewer", false, 10006)]
        static void Init()
        {
            var window = GetWindow<RenderGraphViewer>();
            window.titleContent = new GUIContent("Render Graph Viewer");
        }

        [Flags]
        enum PassFilterLegacy
        {
            CulledPasses = 1 << 0,
        }

        [Flags]
        enum PassFilter
        {
            CulledPasses = 1 << 0,
            RasterPasses = 1 << 1,
            UnsafePasses = 1 << 2,
            ComputePasses = 1 << 3,
        }

        [Flags]
        enum ResourceFilter
        {
            ImportedResources = 1 << 0,
            Textures = 1 << 1,
            Buffers = 1 << 2,
            AccelerationStructures = 1 << 3,
        }

        class ResourceElementInfo
        {
            public VisualElement usageRangeBlock;
            public VisualElement resourceListItem;
            public VisualElement resourceHelperLine;
            public int firstPassId;
            public int lastPassId;
        }

        class ResourceRWBlock
        {
            public VisualElement element;
            public string tooltip;
            public int visibleResourceIndex;
            public bool read;
            public bool write;
            public bool frameBufferFetch;
            public bool setGlobalResource;
        }

        class PassElementInfo
        {
            public int passId;
            public VisualElement passBlock;
            public VisualElement passTitle;
            public bool isCulled;
            public bool isAsync;

            // List of resource blocks read/written to by this pass
            public readonly List<ResourceRWBlock> resourceBlocks = new();

            public VisualElement leftGridLine;
            public VisualElement rightGridLine;

            public bool hasPassCompatibilityTooltip;
            public bool isPassCompatibleToMerge;
            public bool hasAsyncDependencyTooltip;
        }

        void ResetPassBlockState()
        {
            foreach (var info in m_PassElementsInfo)
            {
                info.hasPassCompatibilityTooltip = false;
                info.isPassCompatibleToMerge = false;
                info.hasAsyncDependencyTooltip = false;

                info.passBlock.RemoveFromClassList(Classes.kPassBlockCulledPass);
                info.passBlock.tooltip = string.Empty;
                if (info.isCulled)
                {
                    info.passBlock.AddToClassList(Classes.kPassBlockCulledPass);
                    info.passBlock.tooltip = "Culled pass";
                }

                info.passBlock.RemoveFromClassList(Classes.kPassBlockAsyncPass);
                if (info.isAsync)
                {
                    info.passBlock.AddToClassList(Classes.kPassBlockAsyncPass);
                    info.passBlock.tooltip = "Async Compute Pass";
                }

                var groupedIds = GetGroupedPassIds(info.passId);
                if (groupedIds.Count > 1)
                    info.passBlock.tooltip =
                        $"{groupedIds.Count} Raster Render Passes merged into a single Native Render Pass";
            }
        }

        void UpdatePassBlocksToSelectedState(List<int> selectedPassIds)
        {
            // Hide culled/async pass indicators when a block is selected
            foreach (var info in m_PassElementsInfo)
            {
                info.passBlock.RemoveFromClassList(Classes.kPassBlockCulledPass);
                info.passBlock.RemoveFromClassList(Classes.kPassBlockAsyncPass);
                info.passBlock.tooltip = string.Empty;
            }

            foreach (var passIdInGroup in selectedPassIds)
            {
                var pass = m_CurrentDebugData.passList[passIdInGroup];

                // Native pass compatibility
                if (m_CurrentDebugData.isNRPCompiler)
                {
                    if (pass.nrpInfo.nativePassInfo != null && pass.nrpInfo.nativePassInfo.passCompatibility.Count > 0)
                    {
                        foreach (var msg in pass.nrpInfo.nativePassInfo.passCompatibility)
                        {
                            int linkedPassId = msg.Key;
                            string compatibilityMessage = msg.Value.message;
                            var linkedPassGroup = GetGroupedPassIds(linkedPassId);
                            foreach (var passIdInLinkedPassGroup in linkedPassGroup)
                            {
                                if (selectedPassIds.Contains(passIdInLinkedPassGroup))
                                    continue; // Don't show compatibility info among passes that are merged

                                if (m_PassIdToVisiblePassIndex.TryGetValue(passIdInLinkedPassGroup,
                                        out int visiblePassIndexInLinkedPassGroup))
                                {
                                    var info = m_PassElementsInfo[visiblePassIndexInLinkedPassGroup];
                                    info.hasPassCompatibilityTooltip = true;
                                    info.isPassCompatibleToMerge = msg.Value.isCompatible;
                                    info.passBlock.tooltip = compatibilityMessage;
                                }
                            }
                        }

                        // Each native pass has compatibility messages, it's enough to process the first one
                        break;
                    }
                }

                // Async compute dependencies
                if (m_PassIdToVisiblePassIndex.TryGetValue(pass.syncFromPassIndex, out int visibleSyncFromPassIndex))
                {
                    var syncFromPassInfo = m_PassElementsInfo[visibleSyncFromPassIndex];
                    syncFromPassInfo.hasAsyncDependencyTooltip = true;
                    syncFromPassInfo.passBlock.tooltip =
                        "Currently selected Async Compute Pass inserts a GraphicsFence, which this pass waits on.";
                }

                if (m_PassIdToVisiblePassIndex.TryGetValue(pass.syncToPassIndex, out int visibleSyncToPassIndex))
                {
                    var syncToPassInfo = m_PassElementsInfo[visibleSyncToPassIndex];
                    syncToPassInfo.hasAsyncDependencyTooltip = true;
                    syncToPassInfo.passBlock.tooltip =
                        "Currently selected Async Compute Pass waits on a GraphicsFence inserted after this pass.";
                }
            }
        }

        [Flags]
        enum ResourceHighlightOptions
        {
            None = 1 << 0,
            ResourceListItem = 1 << 1,
            ResourceUsageRangeBorder = 1 << 2,
            ResourceHelperLine = 1 << 3,

            All = ResourceListItem | ResourceUsageRangeBorder | ResourceHelperLine
        }

        void ClearResourceHighlight(ResourceHighlightOptions highlightOptions = ResourceHighlightOptions.All)
        {
            if (highlightOptions.HasFlag(ResourceHighlightOptions.ResourceListItem))
            {
                rootVisualElement.Query(classes: Classes.kResourceListItem).ForEach(elem =>
                {
                    elem.RemoveFromClassList(Classes.kResourceListItemHighlight);
                });
            }

            if (highlightOptions.HasFlag(ResourceHighlightOptions.ResourceUsageRangeBorder))
            {
                rootVisualElement.Query(classes: Classes.kResourceUsageRangeBlockHighlight).ForEach(elem =>
                {
                    elem.RemoveFromHierarchy();
                });
            }

            if (highlightOptions.HasFlag(ResourceHighlightOptions.ResourceHelperLine))
            {
                rootVisualElement.Query(classes: Classes.kResourceHelperLineHighlight).ForEach(elem =>
                {
                    elem.RemoveFromClassList(Classes.kResourceHelperLineHighlight);
                });
            }
        }

        void SetResourceHighlight(ResourceElementInfo info, int visibleResourceIndex, ResourceHighlightOptions highlightOptions)
        {
            if (highlightOptions.HasFlag(ResourceHighlightOptions.ResourceListItem))
            {
                info.resourceListItem.AddToClassList(Classes.kResourceListItemHighlight);
            }

            if (highlightOptions.HasFlag(ResourceHighlightOptions.ResourceUsageRangeBorder))
            {
                var usageRangeHighlightBlock = new VisualElement();
                usageRangeHighlightBlock.style.left = info.usageRangeBlock.style.left.value.value - 1.0f;
                usageRangeHighlightBlock.style.width = info.usageRangeBlock.style.width.value.value + 2.0f;
                usageRangeHighlightBlock.style.top =  visibleResourceIndex * kResourceRowHeightPx;
                usageRangeHighlightBlock.pickingMode = PickingMode.Ignore;
                usageRangeHighlightBlock.AddToClassList(Classes.kResourceUsageRangeBlockHighlight);

                rootVisualElement.Q<VisualElement>(Names.kResourceGrid).parent.Add(usageRangeHighlightBlock);
                usageRangeHighlightBlock.PlaceInFront(rootVisualElement.Q<VisualElement>(Names.kGridlineContainer));
            }

            if (highlightOptions.HasFlag(ResourceHighlightOptions.ResourceHelperLine))
            {
                info.resourceHelperLine.AddToClassList(Classes.kResourceHelperLineHighlight);
            }
        }

        [Flags]
        enum PassHighlightOptions
        {
            None = 1 << 0,
            PassBlockBorder = 1 << 1,
            PassBlockFill = 1 << 2,
            PassTitle = 1 << 3,
            PassGridLines = 1 << 4,
            ResourceRWBlocks = 1 << 5,
            PassesWithCompatibilityMessage = 1 << 6,
            PassesWithSynchronizationMessage = 1 << 7,
            ResourceGridFocusOverlay = 1 << 8,

            All = PassBlockBorder | PassBlockFill | PassTitle | PassGridLines | ResourceRWBlocks |
                  PassesWithCompatibilityMessage | PassesWithSynchronizationMessage | ResourceGridFocusOverlay
        }

        void ClearPassHighlight(PassHighlightOptions highlightOptions = PassHighlightOptions.All)
        {
            // Remove pass block & title highlight
            foreach (var info in m_PassElementsInfo)
            {
                if (highlightOptions.HasFlag(PassHighlightOptions.PassTitle))
                    info.passTitle.RemoveFromClassList(Classes.kPassHighlight);
                if (highlightOptions.HasFlag(PassHighlightOptions.PassBlockBorder))
                    info.passBlock.RemoveFromClassList(Classes.kPassHighlightBorder);
                if (highlightOptions.HasFlag(PassHighlightOptions.PassBlockFill))
                    info.passBlock.RemoveFromClassList(Classes.kPassHighlight);
                if (highlightOptions.HasFlag(PassHighlightOptions.PassesWithCompatibilityMessage))
                {
                    info.passBlock.RemoveFromClassList(Classes.kPassCompatibilityMessageIndicator);
                    info.passBlock.RemoveFromClassList(Classes.kPassCompatibilityMessageIndicatorAnimation);
                    info.passBlock.RemoveFromClassList(Classes.kPassCompatibilityMessageIndicatorCompatible);
                    info.passBlock.UnregisterCallback<TransitionEndEvent, VisualElement>(ToggleCompatiblePassAnimation);
                }

                if (highlightOptions.HasFlag(PassHighlightOptions.PassesWithSynchronizationMessage))
                    info.passBlock.RemoveFromClassList(Classes.kPassSynchronizationMessageIndicator);
            }

            // Remove grid line highlight
            if (highlightOptions.HasFlag(PassHighlightOptions.PassGridLines))
            {
                rootVisualElement.Query(classes: Classes.kGridLine).ForEach(elem =>
                {
                    elem.RemoveFromClassList(Classes.kGridLineHighlight);
                });
            }

            // Remove focus overlay
            if (highlightOptions.HasFlag(PassHighlightOptions.ResourceGridFocusOverlay))
            {
                rootVisualElement.Query(classes: Classes.kResourceGridFocusOverlay).ForEach(elem =>
                {
                    elem.RemoveFromHierarchy();
                });
            }
        }

        void SetPassHighlight(int visiblePassIndex, PassHighlightOptions highlightOptions)
        {
            if (!m_VisiblePassIndexToPassId.TryGetValue(visiblePassIndex, out int passId))
                return;

            var groupedPassIds = GetGroupedPassIds(passId);

            // Add pass block & title highlight
            List<PassElementInfo> visiblePassInfos = new();
            foreach (int groupedPassId in groupedPassIds)
            {
                if (m_PassIdToVisiblePassIndex.TryGetValue(groupedPassId, out int groupedVisiblePassIndex))
                {
                    var info = m_PassElementsInfo[groupedVisiblePassIndex];
                    if (highlightOptions.HasFlag(PassHighlightOptions.PassTitle))
                        info.passTitle.AddToClassList(Classes.kPassHighlight);
                    if (highlightOptions.HasFlag(PassHighlightOptions.PassBlockBorder))
                        info.passBlock.AddToClassList(Classes.kPassHighlightBorder);
                    if (highlightOptions.HasFlag(PassHighlightOptions.PassBlockFill))
                        info.passBlock.AddToClassList(Classes.kPassHighlight);
                    visiblePassInfos.Add(info);
                }
            }


            foreach (var info in m_PassElementsInfo)
            {
                if (groupedPassIds.Contains(info.passId))
                    continue;

                if (highlightOptions.HasFlag(PassHighlightOptions.PassesWithCompatibilityMessage) &&
                    info.hasPassCompatibilityTooltip)
                {
                    info.passBlock.AddToClassList(Classes.kPassCompatibilityMessageIndicator);
                    if (info.isPassCompatibleToMerge)
                    {
                        info.passBlock.schedule.Execute(() =>
                        {
                            info.passBlock.AddToClassList(Classes.kPassCompatibilityMessageIndicatorAnimation);
                            info.passBlock.AddToClassList(Classes.kPassCompatibilityMessageIndicatorCompatible);
                        }).StartingIn(100);
                        info.passBlock.RegisterCallback<TransitionEndEvent, VisualElement>(
                            ToggleCompatiblePassAnimation, info.passBlock);
                    }
                }

                if (highlightOptions.HasFlag(PassHighlightOptions.PassesWithSynchronizationMessage) &&
                    info.hasAsyncDependencyTooltip)
                    info.passBlock.AddToClassList(Classes.kPassSynchronizationMessageIndicator);
            }

            // Add grid line highlight
            if (highlightOptions.HasFlag(PassHighlightOptions.PassGridLines))
            {
                var firstVisiblePassInfo = visiblePassInfos[0];
                firstVisiblePassInfo.leftGridLine.AddToClassList(Classes.kGridLineHighlight);

                int nextVisiblePassIndex = FindNextVisiblePassIndex(visiblePassInfos[^1].passId + 1);
                if (nextVisiblePassIndex != -1)
                    m_PassElementsInfo[nextVisiblePassIndex].leftGridLine.AddToClassList(Classes.kGridLineHighlight);
                else
                    rootVisualElement.Query(classes: Classes.kGridLine).Last()
                        .AddToClassList(Classes.kGridLineHighlight);
            }

            if (highlightOptions.HasFlag(PassHighlightOptions.ResourceGridFocusOverlay))
            {
                int firstPassIndex = FindNextVisiblePassIndex(groupedPassIds[0]);
                int afterLastPassIndex = FindNextVisiblePassIndex(groupedPassIds[^1] + 1);
                int focusOverlayHeightPx = m_ResourceElementsInfo.Count * kResourceRowHeightPx + kResourceGridMarginTopPx;
                int leftWidth = firstPassIndex * kPassWidthPx;
                int rightWidth = (m_PassElementsInfo.Count - afterLastPassIndex) * kPassWidthPx;

                VisualElement left = new VisualElement();
                left.AddToClassList(Classes.kResourceGridFocusOverlay);
                left.style.marginTop = kResourceGridMarginTopPx;
                left.style.width = leftWidth;
                left.style.height = focusOverlayHeightPx;
                left.pickingMode = PickingMode.Ignore;

                VisualElement right = new VisualElement();
                right.AddToClassList(Classes.kResourceGridFocusOverlay);
                right.style.marginTop = kResourceGridMarginTopPx;
                right.style.marginLeft = afterLastPassIndex * kPassWidthPx;
                right.style.width = rightWidth;
                right.style.height = focusOverlayHeightPx;
                right.pickingMode = PickingMode.Ignore;

                var resourceGridScrollView = rootVisualElement.Q<ScrollView>(Names.kResourceGridScrollView);
                resourceGridScrollView.Add(left);
                resourceGridScrollView.Add(right);
            }
        }

        void ToggleCompatiblePassAnimation(TransitionEndEvent _, VisualElement element)
        {
            element.ToggleInClassList(Classes.kPassCompatibilityMessageIndicatorCompatible);
        }

        // Returns a list of passes containing the pass itself and potentially others (if merging happened)
        List<int> GetGroupedPassIds(int passId)
        {
            var pass = m_CurrentDebugData.passList[passId];
            return pass.nrpInfo?.nativePassInfo?.mergedPassIds ?? new List<int> { passId };
        }

        bool SelectResource(int visibleResourceIndex, int visiblePassIndex)
        {
            bool validResourceIndex = visibleResourceIndex >= 0 && visibleResourceIndex < m_ResourceElementsInfo.Count;
            if (!validResourceIndex)
                return false;

            var resInfo = m_ResourceElementsInfo[visibleResourceIndex];
            if (m_VisiblePassIndexToPassId.TryGetValue(visiblePassIndex, out int passId))
            {
                if (passId >= resInfo.firstPassId && passId <= resInfo.lastPassId)
                {
                    m_ResourcesOverlay.ScrollTo(visibleResourceIndex);
                    return true;
                }
            }

            return false;
        }

        void SelectPass(int visiblePassIndex)
        {
            m_CurrentSelectedVisiblePassIndex = visiblePassIndex;

            const PassHighlightOptions opts = PassHighlightOptions.PassTitle |
                                              PassHighlightOptions.PassBlockFill |
                                              PassHighlightOptions.PassGridLines |
                                              PassHighlightOptions.PassBlockBorder |
                                              PassHighlightOptions.ResourceRWBlocks |
                                              PassHighlightOptions.PassesWithCompatibilityMessage |
                                              PassHighlightOptions.PassesWithSynchronizationMessage |
                                              PassHighlightOptions.ResourceGridFocusOverlay;

            ClearPassHighlight(opts);
            ResetPassBlockState();
            m_PassInspectorOverlay.ClearContents();

            if (m_VisiblePassIndexToPassId.TryGetValue(visiblePassIndex, out int passId))
            {
                var selectedPassIds = GetGroupedPassIds(passId);
                UpdatePassBlocksToSelectedState(selectedPassIds);
                if (m_CurrentDebugData.isNRPCompiler)
                    m_PassInspectorOverlay.PopulateContents(this, selectedPassIds);
                SetPassHighlight(visiblePassIndex, opts);
            }
        }

        void HoverPass(int visiblePassIndex, int visibleResourceIndex)
        {
            if (m_CurrentSelectedVisiblePassIndex != -1)
                return;

            if (m_CurrentHoveredVisiblePassIndex != visiblePassIndex ||
                m_CurrentHoveredVisibleResourceIndex != visibleResourceIndex)
            {
                var highlight = PassHighlightOptions.PassBlockBorder |
                                PassHighlightOptions.PassGridLines |
                                PassHighlightOptions.PassBlockFill;

                if (m_CurrentSelectedVisiblePassIndex != -1)
                {
                    // Don't highlight or clear these when a pass is selected
                    highlight &= ~(PassHighlightOptions.PassTitle | PassHighlightOptions.PassBlockFill |
                                   PassHighlightOptions.PassGridLines);
                }

                if (m_CurrentHoveredVisiblePassIndex != -1)
                    ClearPassHighlight(highlight);

                if (visibleResourceIndex != -1) // Don't highlight these while mouse is on resource grid
                    highlight &= ~(PassHighlightOptions.PassTitle | PassHighlightOptions.PassBlockFill);

                if (visiblePassIndex != -1)
                    SetPassHighlight(visiblePassIndex, highlight);
            }
        }

        void HoverResourceByIndex(int visibleResourceIndex, int visiblePassIndex)
        {
            if (m_CurrentHoveredVisibleResourceIndex != visibleResourceIndex ||
                m_CurrentHoveredVisiblePassIndex != visiblePassIndex)
            {
                var highlight = ResourceHighlightOptions.ResourceUsageRangeBorder | ResourceHighlightOptions.ResourceHelperLine;
                if (m_CurrentHoveredVisibleResourceIndex >= 0 &&
                    m_CurrentHoveredVisibleResourceIndex < m_ResourceElementsInfo.Count)
                {
                    ClearResourceHighlight(highlight);
                    rootVisualElement.Q(Names.kHoverOverlay).AddToClassList(PanManipulator.k_ContentPanClassName);
                    m_PanManipulator.canStartDragging = true;
                }

                if (visibleResourceIndex != -1)
                {
                    var info = m_ResourceElementsInfo[visibleResourceIndex];
                    if (m_VisiblePassIndexToPassId.TryGetValue(visiblePassIndex, out int passId))
                    {
                        bool disablePanning = false;
                        var passInfo = m_PassElementsInfo[visiblePassIndex];
                        foreach (var res in passInfo.resourceBlocks)
                        {
                            if (res.visibleResourceIndex == visibleResourceIndex &&
                                res.setGlobalResource)
                            {
                                disablePanning = true;
                            }
                        }

                        if (passId >= info.firstPassId && passId <= info.lastPassId)
                        {
                            SetResourceHighlight(info, visibleResourceIndex, highlight);
                            disablePanning = true;
                        }

                        if (disablePanning)
                        {
                            rootVisualElement.Q(Names.kHoverOverlay)
                                .RemoveFromClassList(PanManipulator.k_ContentPanClassName);
                            m_PanManipulator.canStartDragging = false;
                        }
                    }
                }
            }
        }

        void HoverResourceGrid(int visiblePassIndex, int visibleResourceIndex)
        {
            if (m_PanManipulator is { dragActive: true })
            {
                visiblePassIndex = -1;
                visibleResourceIndex = -1;
            }

            HoverPass(visiblePassIndex, visibleResourceIndex);
            HoverResourceByIndex(visibleResourceIndex, visiblePassIndex);
            m_CurrentHoveredVisiblePassIndex = visiblePassIndex;
            m_CurrentHoveredVisibleResourceIndex = visibleResourceIndex;
        }

        void GetVisiblePassAndResourceIndex(Vector2 pos, out int visiblePassIndex, out int visibleResourceIndex)
        {
            visiblePassIndex = Math.Min(Mathf.FloorToInt(pos.x / kPassWidthPx), m_PassElementsInfo.Count - 1);
            visibleResourceIndex = Math.Min(Mathf.FloorToInt(pos.y / kResourceRowHeightPx),
                m_ResourceElementsInfo.Count - 1);
        }

        void ResourceGridHovered(MouseMoveEvent evt)
        {
            GetVisiblePassAndResourceIndex(evt.localMousePosition, out int visiblePassIndex,
                out int visibleResourceIndex);
            HoverResourceGrid(visiblePassIndex, visibleResourceIndex);
        }

        void ResourceGridClicked(ClickEvent evt)
        {
            GetVisiblePassAndResourceIndex(evt.localPosition, out int visiblePassIndex, out int visibleResourceIndex);

            bool selectedResource = SelectResource(visibleResourceIndex, visiblePassIndex);

            // Also select pass when clicking on resource
            if (selectedResource)
                SelectPass(visiblePassIndex);

            // Clicked grid background, clear selection
            if (!selectedResource)
                DeselectPass();

            evt.StopImmediatePropagation(); // Required because to root element click deselects
        }

        void PassBlockClicked(ClickEvent evt, int visiblePassIndex)
        {
            SelectPass(visiblePassIndex);
            evt.StopImmediatePropagation(); // Required because to root element click deselects
        }

        void ResourceGridTooltipDisplayed(TooltipEvent evt)
        {
            evt.tooltip = string.Empty;
            if (m_CurrentHoveredVisibleResourceIndex != -1 && m_CurrentHoveredVisiblePassIndex != -1)
            {
                var passInfo = m_PassElementsInfo[m_CurrentHoveredVisiblePassIndex];
                var resourceInfo = m_ResourceElementsInfo[m_CurrentHoveredVisibleResourceIndex];
                var passId = m_VisiblePassIndexToPassId[m_CurrentHoveredVisiblePassIndex];

                foreach (var rwBlock in passInfo.resourceBlocks)
                {
                    if (rwBlock.visibleResourceIndex == m_CurrentHoveredVisibleResourceIndex)
                    {
                        evt.tooltip = rwBlock.tooltip;
                        evt.rect = rwBlock.element.worldBound;
                        break;
                    }
                }

                if (evt.tooltip == string.Empty &&
                    passId >= resourceInfo.firstPassId && passId <= resourceInfo.lastPassId)
                {
                    evt.tooltip = "Resource is alive but not used by this pass.";
                    evt.rect = resourceInfo.usageRangeBlock.worldBound;
                }
            }

            evt.StopPropagation();
        }

        void DeselectPass()
        {
            SelectPass(-1);
            m_CurrentHoveredVisiblePassIndex = -1;
            m_CurrentHoveredVisibleResourceIndex = -1;
        }

        void KeyPressed(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
                DeselectPass();
        }

        void RequestCaptureSelectedExecution()
        {
            if (!CaptureEnabled())
                return;

            selectedRenderGraph.RequestCaptureDebugData(selectedExecutionName);

            overlayCanvas.Remove(m_ResourcesOverlay);
            overlayCanvas.Remove(m_PassInspectorOverlay);

            ClearGraphViewerUI();
            SetEmptyStateMessage(EmptyStateReason.WaitingForCameraRender);
        }

        void SelectedRenderGraphChanged(string newRenderGraphName)
        {
            foreach (var rg in m_RegisteredGraphs.Keys)
            {
                if (rg.name == newRenderGraphName)
                {
                    selectedRenderGraph = rg;
                    return;
                }
            }
            selectedRenderGraph = null;
        }

        void SelectedExecutionChanged(string newExecutionName)
        {
            selectedExecutionName = newExecutionName;
        }

        void ClearEmptyStateMessage()
        {
            rootVisualElement.Q<VisualElement>(Names.kContentContainer).style.display = DisplayStyle.Flex;
            rootVisualElement.Q<VisualElement>(Names.kEmptyStateMessage).style.display = DisplayStyle.None;
        }

        void SetEmptyStateMessage(EmptyStateReason reason)
        {
            rootVisualElement.Q<VisualElement>(Names.kContentContainer).style.display = DisplayStyle.None;

            var emptyStateElement = rootVisualElement.Q<VisualElement>(Names.kEmptyStateMessage);
            emptyStateElement.style.display = DisplayStyle.Flex;
            if (emptyStateElement[0] is TextElement emptyStateText)
                emptyStateText.text = $"{kEmptyStateMessages[(int) reason]}";
        }

        void RebuildRenderGraphPopup()
        {
            var renderGraphDropdownField = rootVisualElement.Q<DropdownField>(Names.kCurrentGraphDropdown);
            if (m_RegisteredGraphs.Count == 0 || renderGraphDropdownField == null)
            {
                selectedRenderGraph = null;
                return;
            }

            var choices = new List<string>();
            foreach (var rg in m_RegisteredGraphs.Keys)
                choices.Add(rg.name);

            renderGraphDropdownField.choices = choices;
            renderGraphDropdownField.style.display = DisplayStyle.Flex;
            renderGraphDropdownField.value = choices[0];
            SelectedRenderGraphChanged(choices[0]);
        }

        void RebuildExecutionPopup()
        {
            var executionDropdownField = rootVisualElement.Q<DropdownField>(Names.kCurrentExecutionDropdown);
            List<string> choices = new List<string>();
            if (selectedRenderGraph != null)
            {
                m_RegisteredGraphs.TryGetValue(selectedRenderGraph, out var executionSet);
                choices.AddRange(executionSet);
            }

            if (choices.Count == 0 || executionDropdownField == null)
            {
                selectedExecutionName = null;
                return;
            }

            executionDropdownField.choices = choices;
            executionDropdownField.RegisterValueChangedCallback(evt => selectedExecutionName = evt.newValue);
            executionDropdownField.value = choices[0];
            SelectedExecutionChanged(choices[0]);
        }

        void OnPassFilterChanged(ChangeEvent<Enum> evt)
        {
            m_PassFilter = (PassFilter) evt.newValue;
            RebuildGraphViewerUI();
        }

        void OnPassFilterLegacyChanged(ChangeEvent<Enum> evt)
        {
            m_PassFilterLegacy = (PassFilterLegacy) evt.newValue;
            RebuildGraphViewerUI();
        }

        void OnResourceFilterChanged(ChangeEvent<Enum> evt)
        {
            m_ResourceFilter = (ResourceFilter) evt.newValue;
            RebuildGraphViewerUI();
        }

        void RebuildPassFilterUI()
        {
            var passFilter = rootVisualElement.Q<EnumFlagsField>(Names.kPassFilterField);
            passFilter.style.display = DisplayStyle.Flex;
            // We don't know which callback was registered before, so unregister both.
            passFilter.UnregisterCallback<ChangeEvent<Enum>>(OnPassFilterChanged);
            passFilter.UnregisterCallback<ChangeEvent<Enum>>(OnPassFilterLegacyChanged);
            if (m_CurrentDebugData.isNRPCompiler)
            {
                passFilter.Init(m_PassFilter);
                passFilter.RegisterCallback<ChangeEvent<Enum>>(OnPassFilterChanged);
            }
            else
            {
                passFilter.Init(m_PassFilterLegacy);
                passFilter.RegisterCallback<ChangeEvent<Enum>>(OnPassFilterLegacyChanged);
            }
        }

        void RebuildResourceFilterUI()
        {
            var resourceFilter = rootVisualElement.Q<EnumFlagsField>(Names.kResourceFilterField);
            resourceFilter.style.display = DisplayStyle.Flex;
            resourceFilter.UnregisterCallback<ChangeEvent<Enum>>(OnResourceFilterChanged);
            resourceFilter.Init(m_ResourceFilter);
            resourceFilter.RegisterCallback<ChangeEvent<Enum>>(OnResourceFilterChanged);
        }

        void RebuildHeaderUI()
        {
            RebuildRenderGraphPopup();
            RebuildExecutionPopup();
        }

        RenderGraph m_SelectedRenderGraph;

        RenderGraph selectedRenderGraph
        {
            get => m_SelectedRenderGraph;
            set
            {
                m_SelectedRenderGraph = value;
                UpdateCaptureEnabledUIState();
            }
        }

        string m_SelectedExecutionName;

        string selectedExecutionName
        {
            get => m_SelectedExecutionName;
            set
            {
                m_SelectedExecutionName = value;
                UpdateCaptureEnabledUIState();
            }
        }

        bool CaptureEnabled() => selectedExecutionName != null && selectedRenderGraph != null;

        void UpdateCaptureEnabledUIState()
        {
            if (rootVisualElement?.childCount == 0)
                return;

            bool enabled = CaptureEnabled();
            var captureButton = rootVisualElement.Q<Button>(Names.kCaptureButton);
            captureButton.SetEnabled(enabled);

            var renderGraphDropdownField = rootVisualElement.Q<DropdownField>(Names.kCurrentGraphDropdown);
            renderGraphDropdownField.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;

            var executionDropdownField = rootVisualElement.Q<DropdownField>(Names.kCurrentExecutionDropdown);
            executionDropdownField.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        bool IsResourceVisible(RenderGraph.DebugData.ResourceData resource, RenderGraphResourceType type)
        {
            // Unused resources are always hidden
            if (resource.releasePassIndex == -1 && resource.creationPassIndex == -1)
                return false;

            if (resource.imported && !m_ResourceFilter.HasFlag(ResourceFilter.ImportedResources))
                return false;
            if (type == RenderGraphResourceType.Texture && !m_ResourceFilter.HasFlag(ResourceFilter.Textures))
                return false;
            if (type == RenderGraphResourceType.Buffer && !m_ResourceFilter.HasFlag(ResourceFilter.Buffers))
                return false;
            if (type == RenderGraphResourceType.AccelerationStructure &&
                !m_ResourceFilter.HasFlag(ResourceFilter.AccelerationStructures))
                return false;

            return true;
        }

        bool IsPassVisible(RenderGraph.DebugData.PassData pass)
        {
            if (!pass.generateDebugData)
                return false;

            if (m_CurrentDebugData.isNRPCompiler)
            {
                if (pass.culled && !m_PassFilter.HasFlag(PassFilter.CulledPasses))
                    return false;
                if (pass.type == RenderGraphPassType.Compute && !m_PassFilter.HasFlag(PassFilter.ComputePasses))
                    return false;
                if (pass.type == RenderGraphPassType.Raster && !m_PassFilter.HasFlag(PassFilter.RasterPasses))
                    return false;
                if (pass.type == RenderGraphPassType.Unsafe && !m_PassFilter.HasFlag(PassFilter.UnsafePasses))
                    return false;
            }
            else
            {
                if (pass.culled && !m_PassFilterLegacy.HasFlag(PassFilterLegacy.CulledPasses))
                    return false;
            }

            return true;
        }

        static readonly string[] k_ResourceNames =
            { "Texture Resource", "Buffer Resource", "Acceleration Structure Resource" };


        // Pass title ellipsis must be generated manually because it can't be done right for rotated text using uss.
        void TruncatePassTitle(GeometryChangedEvent evt)
        {
            const int MaxPassTitleLenghtPx = 180;

            if (evt.target is Label label)
            {
                bool wasTruncated = false;
                while (true)
                {
                    var rect = label.MeasureTextSize(label.text, 0, VisualElement.MeasureMode.Undefined, 0,
                        VisualElement.MeasureMode.Undefined);

                    if (float.IsNaN(rect.x))
                        return; // layout not ready yet

                    var needsTruncate = rect.x > MaxPassTitleLenghtPx;
                    if (!needsTruncate)
                        break;

                    label.text = label.text.Remove(label.text.Length - 1, 1);
                    wasTruncated = true;
                }

                if (wasTruncated)
                    label.text += "...";
            }
        }

        VisualElement CreatePassListItem(int passId, RenderGraph.DebugData.PassData pass, int visiblePassIndex)
        {
            var passListItem = new VisualElement();
            passListItem.AddToClassList(Classes.kPassListItem);

            var passTitle = new Label(pass.name);
            passTitle.tooltip = pass.name;
            passTitle.AddToClassList(Classes.kPassTitle);
            passTitle.RegisterCallback<GeometryChangedEvent>(TruncatePassTitle);
            passListItem.Add(passTitle);

            var passMergeIndicator = new VisualElement();
            passMergeIndicator.AddToClassList(Classes.kPassMergeIndicator);
            if (pass.nrpInfo?.nativePassInfo?.mergedPassIds.Count > 1)
            {
                // Blue line do denote merged render passes
                passMergeIndicator.style.visibility = Visibility.Visible;

                bool firstMergedPass = pass.nrpInfo.nativePassInfo.mergedPassIds[0] == passId;
                bool lastMergedPass = pass.nrpInfo.nativePassInfo.mergedPassIds[^1] == passId;

                const int kBorderRadius = 2;
                const int kEdgeMargin = 2;

                // Use margins to create a break between consecutive merged passes
                int width = kPassWidthPx;
                if (firstMergedPass)
                {
                    passMergeIndicator.style.marginLeft = kEdgeMargin;
                    passMergeIndicator.style.borderTopLeftRadius = kBorderRadius;
                    passMergeIndicator.style.borderBottomLeftRadius = kBorderRadius;
                    width -= kEdgeMargin;
                }

                if (lastMergedPass)
                {
                    passMergeIndicator.style.marginRight = kEdgeMargin;
                    passMergeIndicator.style.borderTopRightRadius = kBorderRadius;
                    passMergeIndicator.style.borderBottomRightRadius = kBorderRadius;
                    width -= kEdgeMargin;
                }

                passMergeIndicator.style.width = width;
            }

            passListItem.Add(passMergeIndicator);

            var passBlock = new VisualElement();
            passBlock.AddToClassList(Classes.kPassBlock);
            passBlock.RegisterCallback<MouseOverEvent>(_ => HoverResourceGrid(visiblePassIndex, -1));
            passBlock.RegisterCallback<MouseOutEvent>(_ => HoverResourceGrid(-1, -1));
            passBlock.RegisterCallback<MouseMoveEvent>(_ => HoverResourceGrid(visiblePassIndex, -1));
            passBlock.RegisterCallback<ClickEvent>(evt => PassBlockClicked(evt, visiblePassIndex));

            var passInfo = new PassElementInfo
            {
                passBlock = passBlock,
                passTitle = passTitle,
                passId = passId,
                isCulled = pass.culled,
                isAsync = pass.async
            };

            m_PassElementsInfo.Add(passInfo);
            passListItem.Add(passBlock);
            return passListItem;
        }

        VisualElement CreatePassGridLine(int gridLineHeightPx, int offsetPx)
        {
            var gridline = new VisualElement();
            gridline.AddToClassList(Classes.kGridLine);
            gridline.style.left = offsetPx;
            gridline.style.height = gridLineHeightPx;
            gridline.pickingMode = PickingMode.Ignore;
            return gridline;
        }

        VisualElement CreateResourceListItem(RenderGraph.DebugData.ResourceData res, RenderGraphResourceType type)
        {
            var resourceListItem = new VisualElement();
            resourceListItem.AddToClassList(Classes.kResourceListItem);

            var resourceTitleContainer = new VisualElement();

            var resourceTypeIcon = new VisualElement();
            resourceTypeIcon.AddToClassList(Classes.kResourceIcon);
            string className = type switch
            {
                RenderGraphResourceType.Texture => Classes.kResourceIconTexture,
                RenderGraphResourceType.Buffer => Classes.kResourceIconBuffer,
                RenderGraphResourceType.AccelerationStructure => Classes.kResourceIconAccelerationStructure,
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
            resourceTypeIcon.AddToClassList(className);
            resourceTypeIcon.tooltip = k_ResourceNames[(int)type];
            resourceTitleContainer.Add(resourceTypeIcon);

            var resourceLabel = new Label();
            resourceLabel.text = res.name;
            resourceLabel.tooltip = res.name;
            resourceTitleContainer.Add(resourceLabel);

            var iconContainer = new VisualElement();
            iconContainer.AddToClassList(Classes.kResourceIconContainer);

            var importedIcon = new VisualElement();
            importedIcon.AddToClassList(Classes.kResourceIcon);
            importedIcon.AddToClassList(Classes.kResourceIconImported);
            importedIcon.tooltip = "Imported resource";
            importedIcon.style.visibility = res.imported ? Visibility.Visible : Visibility.Hidden;
            iconContainer.Add(importedIcon);

            int numIcons = 1 + iconContainer.childCount;
            resourceLabel.style.maxWidth = kResourceColumnWidth - numIcons * kResourceIconSize - 4;

            resourceListItem.Add(resourceTitleContainer);
            resourceListItem.Add(iconContainer);

            return resourceListItem;
        }

        int FindNextVisiblePassIndex(int passId)
        {
            while (passId < m_CurrentDebugData.passList.Count)
            {
                if (m_PassIdToVisiblePassIndex.TryGetValue(passId, out int visiblePassIndex))
                    return visiblePassIndex;
                passId++;
            }

            return -1;
        }

        void CreateRWResourceBlockElement(int offsetPx, ResourceRWBlock block)
        {
            string accessType = null;
            if (block.read && block.write)
            {
                var triangle = new TriangleElement();
                triangle.width = kDependencyBlockWidthPx;
                triangle.height = kDependencyBlockHeightPx;
                triangle.color = EditorGUIUtility.isProSkin ? kReadWriteBlockFillColorDark : kReadWriteBlockFillColorLight;
                block.element = triangle;
                block.element.AddToClassList(Classes.kResourceDependencyBlockReadWrite);
                accessType = "Read/write";
            }
            else
            {
                block.element = new VisualElement();
                if (block.read)
                {
                    block.element.AddToClassList(Classes.kResourceDependencyBlockRead);
                    accessType = "Read";
                }
                else if (block.write)
                {
                    block.element.AddToClassList(Classes.kResourceDependencyBlockWrite);
                    accessType = "Write";
                }
            }

            if (block.frameBufferFetch)
            {
                var fbFetchIcon = new VisualElement();
                fbFetchIcon.AddToClassList(Classes.kResourceIcon);
                fbFetchIcon.AddToClassList(Classes.kResourceIconFbfetch);
                block.element.Add(fbFetchIcon);
            }
            else if (block.setGlobalResource)
            {
                var globalIcon = new VisualElement();
                globalIcon.AddToClassList(Classes.kResourceIcon);
                string globalIconAsset = block.read || block.write ? Classes.kResourceIconGlobalLight : Classes.kResourceIconGlobalDark;
                globalIcon.AddToClassList(globalIconAsset);
                block.element.Add(globalIcon);
            }

            List<string> tooltipMessages = new List<string>(3);
            if (accessType != null)
                tooltipMessages.Add($"<b>{accessType}</b> access to this resource.");
            if (block.frameBufferFetch)
                tooltipMessages.Add("Read is using <b>framebuffer fetch</b>.");
            if (block.setGlobalResource)
                tooltipMessages.Add("Updates global resource.");
            block.tooltip = string.Join("\n\n", tooltipMessages);
            block.element.style.left = offsetPx;
            block.element.AddToClassList(Classes.kResourceDependencyBlock);
        }

        VisualElement CreateResourceGridRow(
            RenderGraph.DebugData.ResourceData res,
            RenderGraphResourceType resourceType,
            int resourceIndex,
            ResourceElementInfo elementInfo,
            int visibleResourceIndex)
        {
            var row = new VisualElement();
            row.pickingMode = PickingMode.Ignore;
            row.AddToClassList(Classes.kResourceGridRow);

            // Dashed line connecting resource to first usage
            int firstVisiblePassUsingResource = FindNextVisiblePassIndex(res.creationPassIndex);
            if (firstVisiblePassUsingResource == -1)
            {
                return row; // Early out - no pass uses the resource
            }

            int firstUsePassOffsetPx = firstVisiblePassUsingResource * kPassWidthPx;
            const int resourceHelperLineMargin = 4;
            var resourceHelperLine = new VisualElement();
            resourceHelperLine.AddToClassList(Classes.kResourceHelperLine);
            resourceHelperLine.style.marginLeft = resourceHelperLineMargin;
            resourceHelperLine.style.width = firstUsePassOffsetPx - 2 * resourceHelperLineMargin;
            resourceHelperLine.pickingMode = PickingMode.Ignore;
            row.Add(resourceHelperLine);
            elementInfo.resourceHelperLine = resourceHelperLine;

            // Wide gray block indicating first <-> last use range
            var usageRangeBlock = new VisualElement();
            usageRangeBlock.AddToClassList(Classes.kResourceUsageRangeBlock);

            int passIdAfterLastUse = res.releasePassIndex + 1;
            int visiblePassAfterLastUse = FindNextVisiblePassIndex(passIdAfterLastUse);
            if (visiblePassAfterLastUse == -1)
                visiblePassAfterLastUse = m_PassElementsInfo.Count; // last pass uses resource

            int numVisiblePassesUsed = visiblePassAfterLastUse - firstVisiblePassUsingResource;

            usageRangeBlock.style.position = Position.Absolute;
            usageRangeBlock.style.left = firstUsePassOffsetPx;
            usageRangeBlock.style.width = numVisiblePassesUsed * kPassWidthPx;
            row.Add(usageRangeBlock);
            elementInfo.usageRangeBlock = usageRangeBlock;

            // Read/write/read-write blocks
            List<ResourceRWBlock> blocks = new(m_CurrentDebugData.passList.Count);
            for (int passId = 0; passId < m_CurrentDebugData.passList.Count; passId++)
                blocks.Add(new ResourceRWBlock());

            foreach (int readPassId in res.consumerList)
                blocks[readPassId].read = true;

            foreach (var writePassId in res.producerList)
                blocks[writePassId].write = true;

            for (int passId = 0; passId < blocks.Count; passId++)
            {
                ResourceRWBlock block = blocks[passId];
                if (m_PassIdToVisiblePassIndex.TryGetValue(passId, out int visiblePassIndex))
                {
                    var pass = m_CurrentDebugData.passList[passId];
                    if (resourceType == RenderGraphResourceType.Texture && pass.nrpInfo != null)
                    {
                        if (pass.nrpInfo.textureFBFetchList.Contains(resourceIndex))
                            block.frameBufferFetch = true;
                        if (pass.nrpInfo.setGlobals.Contains(resourceIndex))
                            block.setGlobalResource = true;
                    }

                    if (!block.read && !block.write && !block.frameBufferFetch && !block.setGlobalResource)
                        continue; // No need to create a visual element

                    int offsetPx = visiblePassIndex * kPassWidthPx;
                    CreateRWResourceBlockElement(offsetPx, block);
                    block.visibleResourceIndex = visibleResourceIndex;
                    row.Add(block.element);
                    m_PassElementsInfo[visiblePassIndex].resourceBlocks.Add(block);
                }
            }

            return row;
        }

        void ClearGraphViewerUI()
        {
            rootVisualElement.Q<VisualElement>(Names.kGridlineContainer).Clear();
            rootVisualElement.Q<VisualElement>(Names.kPassList).Clear();
            rootVisualElement.Q<VisualElement>(Names.kResourceListScrollView).Clear();
            rootVisualElement.Q<VisualElement>(Names.kResourceGrid).Clear();

            m_PassElementsInfo.Clear();
            m_ResourceElementsInfo.Clear();
            m_PassIdToVisiblePassIndex.Clear();
            m_VisiblePassIndexToPassId.Clear();
            m_CurrentSelectedVisiblePassIndex = -1;
            m_CurrentHoveredVisibleResourceIndex = -1;
            m_CurrentHoveredVisiblePassIndex = -1;

            ClearPassHighlight();
            ClearResourceHighlight();
        }

        void RebuildGraphViewerUI()
        {
            ClearGraphViewerUI();
            ClearEmptyStateMessage();

            if (!CaptureEnabled())
            {
                SetEmptyStateMessage(EmptyStateReason.NoExecutionRegistered);
                return;
            }

            if (m_CurrentDebugData == null)
            {
                SetEmptyStateMessage(EmptyStateReason.NoDataAvailable);
                return;
            }

            // Pass list
            var passList = rootVisualElement.Q<VisualElement>(Names.kPassList);
            int visiblePassIndex = 0;
            for (int passId = 0; passId < m_CurrentDebugData.passList.Count; passId++)
            {
                var pass = m_CurrentDebugData.passList[passId];
                if (!IsPassVisible(pass))
                    continue;

                passList.Add(CreatePassListItem(passId, pass, visiblePassIndex));
                m_PassIdToVisiblePassIndex.Add(passId, visiblePassIndex);
                m_VisiblePassIndexToPassId.Add(visiblePassIndex, passId);
                visiblePassIndex++;
            }

            int numVisiblePasses = visiblePassIndex;
            if (numVisiblePasses == 0)
            {
                ClearGraphViewerUI();
                SetEmptyStateMessage(EmptyStateReason.EmptyPassFilterResult);
                return;
            }

            ResetPassBlockState();

            // Resource list & grid
            var resourceListScrollView = rootVisualElement.Q<ScrollView>(Names.kResourceListScrollView);
            var resourceGrid = rootVisualElement.Q<VisualElement>(Names.kResourceGrid);
            resourceGrid.style.width = numVisiblePasses * kPassWidthPx + kPassTitleAllowanceMargin;

            int visibleResourceIndex = 0;
            for (int t = 0; t < (int) RenderGraphResourceType.Count; t++)
            {
                var resourceType = (RenderGraphResourceType) t;
                var resourceList = m_CurrentDebugData.resourceLists[t];
                for (int resourceIndex = 0; resourceIndex < resourceList.Count; resourceIndex++)
                {
                    var res = resourceList[resourceIndex];
                    if (!IsResourceVisible(res, resourceType))
                        continue;

                    var elementInfo = new ResourceElementInfo
                    {
                        firstPassId = res.creationPassIndex,
                        lastPassId = res.releasePassIndex
                    };
                    elementInfo.resourceListItem = CreateResourceListItem(res, resourceType);
                    resourceListScrollView.Add(elementInfo.resourceListItem);
                    resourceGrid.Add(CreateResourceGridRow(res, resourceType, resourceIndex,
                        elementInfo, visibleResourceIndex));
                    m_ResourceElementsInfo.Add(elementInfo);

                    visibleResourceIndex++;
                }
            }

            int numVisibleResources = visibleResourceIndex;
            if (numVisibleResources == 0)
            {
                ClearGraphViewerUI();
                SetEmptyStateMessage(EmptyStateReason.EmptyResourceFilterResult);
                return;
            }

            // Add a padding item to ensure horizontal scrollbar doesn't cause the listviews to get out of sync
            var resourcePaddingItem = new VisualElement();
            resourcePaddingItem.AddToClassList(Classes.kResourceListPaddingItem);
            resourceListScrollView.Add(resourcePaddingItem);

            // Grid lines
            int gridLineHeightPx = numVisibleResources * kResourceRowHeightPx + kResourceGridMarginTopPx;
            int gridLineOffsetPx = 0;
            var gridlineContainer = rootVisualElement.Q<VisualElement>(Names.kGridlineContainer);
            for (int passIndex = 0; passIndex < numVisiblePasses; passIndex++)
            {
                var gridLine = CreatePassGridLine(gridLineHeightPx, gridLineOffsetPx);
                gridlineContainer.Add(gridLine);
                gridLineOffsetPx += kPassWidthPx;
                m_PassElementsInfo[passIndex].leftGridLine = gridLine;
            }

            gridlineContainer.Add(CreatePassGridLine(gridLineHeightPx, gridLineOffsetPx));

            // Hover overlay element
            var hoverOverlay = rootVisualElement.Q(Names.kHoverOverlay);
            hoverOverlay.style.marginTop = kResourceGridMarginTopPx;
            hoverOverlay.style.width = gridLineOffsetPx;
            hoverOverlay.style.height = gridLineHeightPx;
            hoverOverlay.focusable = true;

            // Overlays
            m_ResourcesOverlay.PopulateContents(this);
            m_PassInspectorOverlay.ClearContents();
        }

        void RebuildUI()
        {
            rootVisualElement.Clear();

            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath);
            visualTreeAsset.CloneTree(rootVisualElement);

            var themeStyleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorGUIUtility.isProSkin
                    ? k_DarkStylePath
                    : k_LightStylePath);
            rootVisualElement.styleSheets.Add(themeStyleSheet);

            RebuildHeaderUI();
            RebuildGraphViewerUI();
        }

        // Initialize, register callbacks & manipulators etc. once
        void InitializePersistentElements()
        {
            // Header elements
            var captureButton = rootVisualElement.Q<Button>(Names.kCaptureButton);
            captureButton.SetEnabled(CaptureEnabled());
            captureButton.RegisterCallback<ClickEvent>(_ => RequestCaptureSelectedExecution());

            var renderGraphDropdownField = rootVisualElement.Q<DropdownField>(Names.kCurrentGraphDropdown);
            renderGraphDropdownField.RegisterValueChangedCallback(evt => SelectedRenderGraphChanged(evt.newValue));

            var executionDropdownField = rootVisualElement.Q<DropdownField>(Names.kCurrentExecutionDropdown);
            executionDropdownField.RegisterValueChangedCallback(evt => SelectedExecutionChanged(evt.newValue));

            var passFilter = rootVisualElement.Q<EnumFlagsField>(Names.kPassFilterField);
            passFilter.style.display = DisplayStyle.None; // Hidden until the compiler is known

            var resourceFilter = rootVisualElement.Q<EnumFlagsField>(Names.kResourceFilterField);
            resourceFilter.style.display = DisplayStyle.None; // Hidden until the compiler is known

            // Hover overlay
            var hoverOverlay = rootVisualElement.Q(Names.kHoverOverlay);
            hoverOverlay.RegisterCallback<MouseOverEvent>(_ => HoverResourceGrid(-1, -1));
            hoverOverlay.RegisterCallback<MouseOutEvent>(_ => HoverResourceGrid(-1, -1));
            hoverOverlay.RegisterCallback<MouseMoveEvent>(ResourceGridHovered);
            hoverOverlay.RegisterCallback<ClickEvent>(ResourceGridClicked);
            hoverOverlay.RegisterCallback<TooltipEvent>(ResourceGridTooltipDisplayed, TrickleDown.TrickleDown);
            hoverOverlay.RegisterCallback<KeyUpEvent>(KeyPressed);

            rootVisualElement.Q(Names.kContentContainer).RegisterCallback<MouseUpEvent>(_ => DeselectPass());

            // Resource grid manipulation
            var resourceListScrollView = rootVisualElement.Q<ScrollView>(Names.kResourceListScrollView);
            var passListScrollView = rootVisualElement.Q<ScrollView>(Names.kPassListScrollView);
            var resourceGridScrollView = rootVisualElement.Q<ScrollView>(Names.kResourceGridScrollView);
            m_PanManipulator = new PanManipulator(this);
            resourceGridScrollView.AddManipulator(m_PanManipulator);
            resourceGridScrollView.mode = ScrollViewMode.VerticalAndHorizontal;

            // Sync resource grid scrollbar state to resource list and pass list scrollbars
            resourceListScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            resourceListScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            resourceGridScrollView.verticalScroller.valueChanged += value =>
                resourceListScrollView.scrollOffset = new Vector2(resourceGridScrollView.scrollOffset.x, value);

            passListScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            passListScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            resourceGridScrollView.horizontalScroller.valueChanged += value =>
                passListScrollView.scrollOffset = new Vector2(value, passListScrollView.scrollOffset.y);

            // Disable mouse wheel on the scroll views that are synced to the resource grid
            resourceListScrollView.RegisterCallback<WheelEvent>(evt => evt.StopImmediatePropagation(), TrickleDown.TrickleDown);
            passListScrollView.RegisterCallback<WheelEvent>(evt => evt.StopImmediatePropagation(), TrickleDown.TrickleDown);
        }

        void OnGraphRegistered(RenderGraph graph)
        {
            m_RegisteredGraphs.Add(graph, new HashSet<string>());
            RebuildHeaderUI();
        }

        void OnGraphUnregistered(RenderGraph graph)
        {
            m_RegisteredGraphs.Remove(graph);
            RebuildHeaderUI();
            if (m_RegisteredGraphs.Count == 0)
                RebuildGraphViewerUI();
        }

        void OnExecutionRegistered(RenderGraph graph, string name)
        {
            m_RegisteredGraphs.TryGetValue(graph, out var executionList);
            Debug.Assert(executionList != null,
                $"RenderGraph {graph.name} should be registered before registering its executions.");
            executionList.Add(name);

            RebuildHeaderUI();

            // Automatically capture data when window is opened if not available yet.
            if (m_CurrentDebugData == null)
                RequestCaptureSelectedExecution();
        }

        void OnExecutionUnregistered(RenderGraph graph, string name)
        {
            m_RegisteredGraphs.TryGetValue(graph, out var executionList);
            Debug.Assert(executionList != null,
                $"RenderGraph {graph.name} should be registered before unregistering its executions.");
            executionList.Remove(name);

            RebuildHeaderUI();
        }

        void OnDebugDataCaptured()
        {
            // Refresh delayed. That way we don't break rendering if something goes wrong on the UI layer.
            EditorApplication.delayCall += () =>
            {
                if (selectedRenderGraph != null)
                {
                    var debugData = selectedRenderGraph.GetDebugData(selectedExecutionName);
                    if (debugData != null)
                    {
                        m_CurrentDebugData = debugData;

                        overlayCanvas.Add(m_ResourcesOverlay);
                        m_ResourcesOverlay.displayed = true;

                        // Pass inspector only available for NRP Compiler
                        if (m_CurrentDebugData.isNRPCompiler)
                        {
                            overlayCanvas.Add(m_PassInspectorOverlay);
                        }

                        m_PassInspectorOverlay.displayed = m_CurrentDebugData.isNRPCompiler;

                        RebuildPassFilterUI();
                        RebuildResourceFilterUI();
                        RebuildGraphViewerUI();
                    }
                }
            };
        }

        void OnEnable()
        {
            var registeredGraph = RenderGraph.GetRegisteredRenderGraphs();
            foreach (var graph in registeredGraph)
                m_RegisteredGraphs.Add(graph, new HashSet<string>());

            RenderGraph.isRenderGraphViewerActive = true;
            RenderGraph.onGraphRegistered += OnGraphRegistered;
            RenderGraph.onGraphUnregistered += OnGraphUnregistered;
            RenderGraph.onExecutionRegistered += OnExecutionRegistered;
            RenderGraph.onExecutionUnregistered += OnExecutionUnregistered;
            RenderGraph.onDebugDataCaptured += OnDebugDataCaptured;

            m_ResourcesOverlay = new ResourcesOverlay();
            m_PassInspectorOverlay = new PassInspectorOverlay();
        }

        void CreateGUI()
        {
            RebuildUI();
            InitializePersistentElements();

            // Automatically capture data when window is opened if not available yet.
            if (m_CurrentDebugData == null)
                RequestCaptureSelectedExecution();
        }

        void OnDisable()
        {
            RenderGraph.isRenderGraphViewerActive = false;
            RenderGraph.onGraphRegistered -= OnGraphRegistered;
            RenderGraph.onGraphUnregistered -= OnGraphUnregistered;
            RenderGraph.onExecutionRegistered -= OnExecutionRegistered;
            RenderGraph.onExecutionUnregistered -= OnExecutionUnregistered;
            RenderGraph.onDebugDataCaptured -= OnDebugDataCaptured;

            m_ResourcesOverlay?.Close();
            m_PassInspectorOverlay?.Close();
        }
    }

    [UxmlElement]
    internal partial class TriangleElement : VisualElement
    {
        public int width { get; set; }

        public int height { get; set; }

        public Color color { get; set; }

        public TriangleElement()
        {
            generateVisualContent += ctx =>
            {
                var painter = ctx.painter2D;
                painter.fillColor = color;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, height));
                painter.LineTo(new Vector2(0, 0));
                painter.LineTo(new Vector2(width, 0));
                painter.ClosePath();
                painter.Fill();
            };
        }
    }
}
