using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using UnityEditor.Categorization;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Converter
{
    internal class RenderPipelineConverterVisualElement : VisualElement
    {
        const string k_Uxml = "Packages/com.unity.render-pipelines.core/Editor-PrivateShared/Tools/Converter/Window/RenderPipelineConverterVisualElement.uxml";
        const string k_Uss = "Packages/com.unity.render-pipelines.core/Editor-PrivateShared/Tools/Converter/Window/RenderPipelineConverterVisualElement.uss";

        static Lazy<VisualTreeAsset> s_VisualTreeAsset = new Lazy<VisualTreeAsset>(() => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Uxml));
        static Lazy<StyleSheet> s_StyleSheet = new Lazy<StyleSheet>(() => AssetDatabase.LoadAssetAtPath<StyleSheet>(k_Uss));

        Node<ConverterInfo> m_ConverterInfo;

        public string displayName => m_ConverterInfo.name;
        public string description => m_ConverterInfo.description;

        public ConverterState state => m_ConverterInfo.data.state;
        public IRenderPipelineConverter converter => m_ConverterInfo.data.converter as IRenderPipelineConverter;

        public bool isSelectedAndEnabled => converter.isEnabled && state.isSelected;

        VisualElement m_RootVisualElement;
        HeaderFoldout m_HeaderFoldout;
        VisualElement m_ListViewHeader;
        HelpBox m_NoItemsFound;
        HelpBox m_PressScan;
        RenderPipelineConverterVisualElementListFilter m_Filter;
        MultiColumnTreeView m_TreeView;

        public Action converterSelected;

        public RenderPipelineConverterVisualElement(Node<ConverterInfo> converterInfo)
        {
            m_ConverterInfo = converterInfo;

            m_RootVisualElement = new VisualElement();
            s_VisualTreeAsset.Value.CloneTree(m_RootVisualElement);
            m_RootVisualElement.styleSheets.Add(s_StyleSheet.Value);

            m_HeaderFoldout = m_RootVisualElement.Q<HeaderFoldout>("conveterFoldout");
            m_HeaderFoldout.text = displayName;
            m_HeaderFoldout.tooltip = (converter.isEnabled) ? description : converter.isDisabledMessage;
            m_HeaderFoldout.SetEnabled(converter.isEnabled);
            m_HeaderFoldout.value = state.isExpanded;
            m_HeaderFoldout.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                state.isExpanded = evt.newValue;
            });
            m_HeaderFoldout.showEnableCheckbox = true;
            m_HeaderFoldout.documentationURL = converterInfo.helpUrl;

            m_HeaderFoldout.enableToggle.SetValueWithoutNotify(state.isSelected);
            m_HeaderFoldout.enableToggle.RegisterCallback<ClickEvent>((evt) =>
            {
                state.isSelected = !state.isSelected;
                converterSelected?.Invoke();
                UpdateConversionInfo();
            });

            var allLabel = m_RootVisualElement.Q<Label>("all");
            allLabel.RegisterCallback<ClickEvent>((evt) =>
            {
                SetItemsActive(true);
                Refresh();
            });
            var noneLabel = m_RootVisualElement.Q<Label>("none");
            noneLabel.RegisterCallback<ClickEvent>((evt) =>
            {
                SetItemsActive(false);
                Refresh();
            });

            m_ListViewHeader = m_RootVisualElement.Q("listViewHeader");

            m_NoItemsFound = m_RootVisualElement.Q<HelpBox>("noItemsFoundHelpBox");
            m_NoItemsFound.style.display = DisplayStyle.None;

            m_PressScan = m_RootVisualElement.Q<HelpBox>("pressScanHelpBox");

            m_TreeView = m_RootVisualElement.Q<MultiColumnTreeView>("converterItemsTreeView");
            m_TreeView.SetRootItems<ConverterItemState>(state.filteredItems);

            m_Filter = m_RootVisualElement.Q<RenderPipelineConverterVisualElementListFilter>("listViewFilter");
            m_Filter.Bind(state);
            m_Filter.onFilterChanged += () =>
            {
                state.ApplyFilter();
                m_TreeView.SetRootItems<ConverterItemState>(state.filteredItems);
                m_TreeView.RefreshItems();
            };

            var isSelectedColumn = m_TreeView.columns["selected"];
            isSelectedColumn.makeCell = () =>
            {
                var toggle = new Toggle();
                toggle.AddToClassList("render-pipeline-converter-items-toggle");
                return toggle;
            };
            isSelectedColumn.bindCell = (VisualElement element, int index) =>
            {
                ConverterItemState itemState = m_TreeView.GetItemDataForIndex<ConverterItemState>(index);
                var toggle = (element as Toggle);

                if (toggle.userData is ConverterItemState previousBindItem)
                {
                    toggle.UnregisterCallback<ClickEvent>(previousBindItem.OnSelectionChanged);
                    previousBindItem.onIsSelectedChanged -= OnSelectionChanged;
                }  

                toggle.userData = itemState;
                if (itemState.item.isEnabled)
                {
                    toggle.SetEnabled(true);
                    toggle.tooltip = "Select/Deselect this item for conversion";
                    toggle.SetValueWithoutNotify(itemState.isSelected);
                }
                else
                {
                    toggle.SetEnabled(false);
                    toggle.tooltip = itemState.item.isDisabledMessage;
                    toggle.SetValueWithoutNotify(false);
                }
                toggle.RegisterCallback<ClickEvent>(itemState.OnSelectionChanged);
                itemState.onIsSelectedChanged += OnSelectionChanged;
                
            };

            var iconColumn = m_TreeView.columns["icon"];
            iconColumn.makeCell = () =>
            {
                var icon = new Image();
                icon.AddToClassList("render-pipeline-converter-items-icon");
                return icon;
            };
            iconColumn.bindCell = (VisualElement element, int index) =>
            {
                var item = m_TreeView.GetItemDataForIndex<ConverterItemState>(index).item;
                var icon = item.icon;
                if (icon != null)
                    (element as Image).image = icon;
            };

            var nameColumn = m_TreeView.columns["name"];
            nameColumn.makeCell = () =>
            {
                var label = new Label();
                label.AddToClassList("render-pipeline-converter-items-name-label");
                return label;
            };
            nameColumn.bindCell = (VisualElement element, int index) =>
            {
                ConverterItemState itemState = m_TreeView.GetItemDataForIndex<ConverterItemState>(index);
                var label = (element as Label);

                if (label.userData is ConverterItemState previousBindItem)
                {
                    label.UnregisterCallback<ClickEvent>(previousBindItem.OnClicked);
                }

                label.text = itemState.item.name;
                label.userData = itemState;
                label.RegisterCallback<ClickEvent>(itemState.OnClicked);
            };

            var infoColumn = m_TreeView.columns["info"];
            infoColumn.stretchable = true;
            infoColumn.makeCell = () =>
            {
                var label = new Label();
                label.AddToClassList("render-pipeline-converter-items-name-label");
                return label;
            };
            infoColumn.bindCell = (VisualElement element, int index) =>
            {
                (element as Label).text = m_TreeView.GetItemDataForIndex<ConverterItemState>(index).item.info;
            };
            
            var stateColumn = m_TreeView.columns["state"];
            stateColumn.makeCell = () => new Image();
            stateColumn.bindCell = (VisualElement element, int index) =>
            {
                (Status Status, string Message) conversionResult = m_TreeView.GetItemDataForIndex<ConverterItemState>(index).conversionResult;

                Texture2D icon = null;
                Status status = conversionResult.Status;
                switch (status)
                {
                    case Status.Pending:
                        icon = CoreEditorStyles.iconPending;
                        break;
                    case Status.Error:
                        icon = CoreEditorStyles.iconFail;
                        break;
                    case Status.Warning:
                        icon = CoreEditorStyles.iconWarn;
                        break;
                    case Status.Success:
                        icon = CoreEditorStyles.iconComplete;
                        break;
                }

                var image = (element as Image);
                image.image = icon;
                image.tooltip = conversionResult.Message;
            };

            Add(m_RootVisualElement);
            Refresh();
        }

        private void SetItemsActive(bool value)
        {
            foreach (var itemState in state.items)
            {
                if (itemState.item.isEnabled)
                    itemState.isSelected = value;
            }
        }

        public void UpdateInfo()
        {
            UpdateConversionInfo();
            UpdateSelectedConverterItemsLabel();
            UpdateAllNoneLabels();
        }

        public void OnSelectionChanged(bool isSelected)
        {
            UpdateSelectedConverterItemsLabel();
            UpdateAllNoneLabels();
        }

        void UpdateSelectedConverterItemsLabel()
        {
            var text = $" ({state.selectedItemsCount} of {state.items.Count})";
            m_RootVisualElement.Q<Label>("converterStats").text = text;
        }

        void UpdateAllNoneLabels()
        {
            var allLabel = m_RootVisualElement.Q<Label>("all");
            var noneLabel = m_RootVisualElement.Q<Label>("none");

            var count = state.items.Count;
            int selectedCount = state.selectedItemsCount;

            bool noneSelected = selectedCount == 0;
            bool allSelected = selectedCount == count;
            SetSelected(allLabel, allSelected);
            SetSelected(noneLabel, noneSelected);
        }

        void SetSelected(Label label, bool selected)
        {
            if (selected)
            {
                label.RemoveFromClassList("not_selected");
                label.AddToClassList("selected");
            }
            else
            {
                label.RemoveFromClassList("selected");
                label.AddToClassList("not_selected");
            }
        }

        void UpdateConversionInfo()
        {
            if (state.isInitialized)
            {
                m_PressScan.style.display = DisplayStyle.None;
                if (state.items.Count > 0)
                {
                    state.ApplyFilter();
                    m_NoItemsFound.style.display = DisplayStyle.None;
                    m_ListViewHeader.style.display = DisplayStyle.Flex;
                    m_TreeView.style.display = DisplayStyle.Flex;
                    m_Filter.Update(state);
                }
                else
                {
                    m_NoItemsFound.style.display = DisplayStyle.Flex;
                    m_ListViewHeader.style.display = DisplayStyle.None;
                    m_TreeView.style.display = DisplayStyle.None;
                }
            }
            else
            {
                m_PressScan.style.display = DisplayStyle.Flex;

                m_NoItemsFound.style.display = DisplayStyle.None;
                m_ListViewHeader.style.display = DisplayStyle.None;
                m_TreeView.style.display = DisplayStyle.None;
            }
        }

        public void Refresh()
        {
            m_TreeView.RefreshItems();
            UpdateInfo();
            m_HeaderFoldout.SetEnabled(converter.isEnabled);
        }

        public void Scan(Action onScanFinish)
        {
            state.Clear();
            state.isLoading = true;
            converter.Scan(OnConverterCompleteDataCollection);

            void OnConverterCompleteDataCollection(List<IRenderPipelineConverterItem> items)
            {
                foreach(var item in items)
                {
                    var converterItemState = new ConverterItemState()
                    {
                        item = item,
                        isSelected = item.isEnabled,
                    };

                    state.AddItem(converterItemState);
                }

                state.isLoading = false;
                state.isInitialized = true;
                m_HeaderFoldout.value = true; // Expand the foldout when we perform a search

                m_TreeView.SetRootItems<ConverterItemState>(state.filteredItems);

                Refresh();
                onScanFinish?.Invoke();
            }
        }

        public void Convert(string progressTitle, StringBuilder sb)
        {
            if (state.pending == 0)
            {
                sb.AppendLine($"[{displayName}] Skipping conversion.");
                return;
            }

            sb.AppendLine($"[{displayName}]");

            converter.BeforeConvert();
            int itemIndex = 0;
            int itemToConvertIndex = 0;
            foreach (var itemState in state.items)
            {
                if (EditorUtility.DisplayCancelableProgressBar(progressTitle,
                    $"({itemToConvertIndex} of {state.pending}) {itemState.item.name}",
                    itemToConvertIndex / (float)state.pending))
                    break;

                if (!itemState.hasConverted && itemState.isSelected)
                {
                    try
                    {
                        var status = converter.Convert(itemState.item, out var message);
                        switch (status)
                        {
                            case Status.Pending:
                                throw new InvalidOperationException("Converter returned a pending status when converting. This is not supported.");
                            case Status.Error:
                            case Status.Warning:
                                sb.AppendLine($"\t- {itemState.item.name} ({status}) ({message})");
                                break;
                            case Status.Success:
                            {
                                sb.AppendLine($"\t- {itemState.item.name} ({status})");
                                message = "Conversion successful!";
                            }
                                break;
                        }

                        itemState.conversionResult.Status = status;
                        itemState.conversionResult.Message = message;
                    }
                    catch(Exception ex)
                    {
                        Debug.LogError($"Exception {ex.Message} while converting {itemState.item.name} from {displayName}");
                    }
                    itemToConvertIndex++;
                }
                itemIndex++;
            }
            converter.AfterConvert();

            Refresh();

            sb.AppendLine(state.ToString());
        }
    }
}
