using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Categorization;
using UnityEditor.Rendering.Converter;
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
        ListView m_ItemsList;
        HeaderFoldout m_HeaderFoldout;
        VisualElement m_ListViewHeader;
        HelpBox m_NoItemsFound;
        HelpBox m_PressScan;
        Label m_PendingLabel;
        Label m_WarningLabel;
        Label m_ErrorLabel;
        Label m_SuccessLabel;

        public Action showMoreInfo; // TODO Remove with the UX rework
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
            m_ItemsList = m_RootVisualElement.Q<ListView>("converterItems");
            m_ItemsList.showBoundCollectionSize = false;
            m_ItemsList.makeItem = () =>
            {
                var item = new RenderPipelineConverterItemVisualElement();
                item.itemSelectionChanged += UpdateInfo;
                return item;
            };
            m_ItemsList.bindItem = (element, index) =>
            {
                var item = element as RenderPipelineConverterItemVisualElement;
                item.Bind(state.items[index]);
            };
            m_ItemsList.selectionChanged += obj =>
            {
                state.items[m_ItemsList.selectedIndex].item.OnClicked();
            };

            // setup the images
            m_RootVisualElement.Q<Image>("pendingImage").image = CoreEditorStyles.iconPending;
            m_RootVisualElement.Q<Image>("pendingImage").tooltip = "Pending";
            m_RootVisualElement.Q<Image>("warningImage").image = CoreEditorStyles.iconWarn;
            m_RootVisualElement.Q<Image>("warningImage").tooltip = "Warnings";
            m_RootVisualElement.Q<Image>("errorImage").image = CoreEditorStyles.iconFail;
            m_RootVisualElement.Q<Image>("errorImage").tooltip = "Failed";
            m_RootVisualElement.Q<Image>("successImage").image = CoreEditorStyles.iconComplete;
            m_RootVisualElement.Q<Image>("successImage").tooltip = "Success";

            // Store labels to easy update afterwards
            m_PendingLabel = m_RootVisualElement.Q<Label>("pendingLabel");
            m_WarningLabel = m_RootVisualElement.Q<Label>("warningLabel");
            m_ErrorLabel = m_RootVisualElement.Q<Label>("errorLabel");
            m_SuccessLabel = m_RootVisualElement.Q<Label>("successLabel");

            m_NoItemsFound = m_RootVisualElement.Q<HelpBox>("noItemsFoundHelpBox");
            m_NoItemsFound.style.display = DisplayStyle.None;

            m_PressScan = m_RootVisualElement.Q<HelpBox>("pressScanHelpBox");

            Add(m_RootVisualElement);
            Refresh();
        }

        private void SetItemsActive(bool value)
        {
            foreach (var itemState in state.items)
                itemState.isSelected = value;
        }

        public void UpdateInfo()
        {
            UpdateConversionInfo();
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
                    m_NoItemsFound.style.display = DisplayStyle.None;
                    m_ListViewHeader.style.display = DisplayStyle.Flex;
                    m_ItemsList.style.display = DisplayStyle.Flex;
                    m_ItemsList.itemsSource = state.items;

                    m_PendingLabel.text = $"{state.pending}";
                    m_WarningLabel.text = $"{state.warnings}";
                    m_ErrorLabel.text = $"{state.errors}";
                    m_SuccessLabel.text = $"{state.success}";
                }
                else
                {
                    m_NoItemsFound.style.display = DisplayStyle.Flex;
                    m_ListViewHeader.style.display = DisplayStyle.None;
                    m_ItemsList.style.display = DisplayStyle.None;
                }
            }
            else
            {
                m_PressScan.style.display = DisplayStyle.Flex;

                m_NoItemsFound.style.display = DisplayStyle.None;
                m_ListViewHeader.style.display = DisplayStyle.None;
                m_ItemsList.style.display = DisplayStyle.None;
            }
        }

        public void Refresh()
        {
            m_ItemsList.Rebuild();
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
                // Set the item infos list to to the right index
                state.items = new List<ConverterItemState>(items.Count);

                foreach(var item in items)
                {
                    var converterItemState = new ConverterItemState()
                    {
                        item = item,
                        isSelected = true, // Default all the entries to true
                    };

                    state.items.Add(converterItemState);
                }

                state.isLoading = false;
                state.isInitialized = true;
                m_HeaderFoldout.value = true; // Expand the foldout when we perform a search
                m_ItemsList.itemsSource = state.items;

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
