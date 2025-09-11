using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal
{
    internal class RenderPipelineConverterVisualElement : VisualElement
    {
        const string k_Uxml = "Packages/com.unity.render-pipelines.universal/Editor/Converter/converter_widget_main.uxml";
        const string k_Uss = "Packages/com.unity.render-pipelines.universal/Editor/Converter/converter_widget_main.uss";

        static Lazy<VisualTreeAsset> s_VisualTreeAsset = new Lazy<VisualTreeAsset>(() => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Uxml));
        static Lazy<StyleSheet> s_StyleSheet = new Lazy<StyleSheet>(() => AssetDatabase.LoadAssetAtPath<StyleSheet>(k_Uss));

        ConverterInfo m_ConverterInfo;

        // TODO: Once attributes land on all the converters use m_ConverterInfo;
        public string displayName => converter.name;
        public string description => converter.info;


        public ConverterState state => m_ConverterInfo.state;
        public RenderPipelineConverter converter => m_ConverterInfo.converter as RenderPipelineConverter;

        public bool isActiveAndEnabled => converter.isEnabled && state.isActive;
        public bool requiresInitialization => !state.isInitialized && isActiveAndEnabled;

        VisualElement m_RootVisualElement;
        Label m_PendingLabel;
        Label m_WarningLabel;
        Label m_ErrorLabel;
        Label m_SuccessLabel;

        public Action showMoreInfo; // TODO Remove with the UX rework
        public Action converterSelected;

        public RenderPipelineConverterVisualElement(ConverterInfo converterInfo)
        {
            m_ConverterInfo = converterInfo;

            m_RootVisualElement = new VisualElement();
            s_VisualTreeAsset.Value.CloneTree(m_RootVisualElement);
            m_RootVisualElement.styleSheets.Add(s_StyleSheet.Value);

            var converterEnabledToggle = m_RootVisualElement.Q<Toggle>("converterEnabled");
            converterEnabledToggle.SetValueWithoutNotify(state.isActive);
            converterEnabledToggle.RegisterCallback<ClickEvent>((evt) =>
            {
                state.isActive = !state.isActive;
                converterSelected?.Invoke();
                UpdateConversionInfo();
                evt.StopPropagation(); // This toggle needs to stop propagation since it is inside another clickable element
            });

            var topElement = m_RootVisualElement.Q<VisualElement>("converterTopVisualElement");
            topElement.RegisterCallback<ClickEvent>((evt) => 
            {
                showMoreInfo?.Invoke();
            });

            topElement.RegisterCallback<TooltipEvent>(evt =>
            {
                // Show the tooltip around the toggle only
                var rect = converterEnabledToggle.worldBound;
                rect.position += new Vector2(150, -30); // offset it a bit to not be ove the toggle
                evt.rect = rect; // position area that triggers it
                evt.StopPropagation();
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

            ListView listView = m_RootVisualElement.Q<ListView>("converterItems");
            listView.showBoundCollectionSize = false;
            listView.makeItem = () =>
            {
                var item = new RenderPipelineConverterItemVisualElement();
                item.itemSelectionChanged += UpdateInfo;
                return item;
            };
            listView.bindItem = (element, index) =>
            {
                var item = element as RenderPipelineConverterItemVisualElement;
                item.Bind(state.items[index]);
            };
            listView.selectionChanged += obj => { converter.OnClicked(listView.selectedIndex); };

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

            Add(m_RootVisualElement);
            Refresh();
        }

        private void SetItemsActive(bool value)
        {
            foreach (var itemState in state.items)
                itemState.isActive = value;
        }

        public void UpdateInfo()
        {
            UpdateConversionInfo();
            UpdateSelectedConverterItemsLabel();
            UpdateAllNoneLabels();
        }

        void UpdateSelectedConverterItemsLabel()
        {
            var text = $"{state.selectedItemsCount}/{state.items.Count} selected";
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
            // Make sure info is up to date
            state.pending = 0;
            state.errors = 0;
            state.success = 0;
            state.warnings = 0;
            foreach (var converterItemState in state.items)
            {
                switch (converterItemState.status)
                {
                    case Status.Pending: state.pending++; break;
                    case Status.Warning: state.warnings++; break;
                    case Status.Error: state.errors++; break;
                    case Status.Success: state.success++; break;
                }
            }

            var info = GetConversionInfo();

            m_RootVisualElement.Q<Label>("converterStateInfoL").text = info.message;
            m_RootVisualElement.Q<Label>("converterStateInfoL").style.unityFontStyleAndWeight = FontStyle.Bold;
            m_RootVisualElement.Q<Image>("converterStateInfoIcon").image = info.icon;

            m_PendingLabel.text = $"{state.pending}";
            m_WarningLabel.text = $"{state.warnings}";
            m_ErrorLabel.text = $"{state.errors}";
            m_SuccessLabel.text = $"{state.success}";
        }

        private (string message, Texture2D icon) GetConversionInfo()
        {
            if (!state.isActive)
                return ("Converter Not Selected", null);

            if (!state.isInitialized)
                return ("Initialization Pending", CoreEditorStyles.iconPending);

            if (state.errors > 0)
                return ("Conversion Complete with Errors", CoreEditorStyles.iconFail);

            if (state.warnings > 0)
                return ("Conversion Pending with Warnings", CoreEditorStyles.iconWarn);

            if (state.pending > 0)
                return ("Conversion Pending", CoreEditorStyles.iconPending);

            if (state.success > 0)
                return ("Conversion Complete", CoreEditorStyles.iconComplete);

            return ("No items found to convert", CoreEditorStyles.iconInfo);
        }

        public void ShowConverterLayout()
        {
            m_RootVisualElement.Q<VisualElement>("informationVE").style.display = DisplayStyle.Flex;
            m_RootVisualElement.Q<VisualElement>("converterItems").style.display = DisplayStyle.Flex;
            m_RootVisualElement.Q<ListView>("converterItems").itemsSource = state.items;
        }

        public void HideConverterLayout()
        {
            m_RootVisualElement.Q<VisualElement>("converterItems").style.display = DisplayStyle.None;
            m_RootVisualElement.Q<VisualElement>("informationVE").style.display = DisplayStyle.None;
        }

        public void Refresh()
        {
            m_RootVisualElement.Q<ListView>("converterItems").Rebuild();
            UpdateInfo();
            m_RootVisualElement.SetEnabled(converter.isEnabled);
            m_RootVisualElement.Q<Label>("converterName").text = displayName;
            m_RootVisualElement.Q<VisualElement>("converterTopVisualElement").tooltip = (converter.isEnabled) ? description : converter.isDisabledWarningMessage;
        }

        public void Scan(Action onScanFinish)
        {
            // Create the context to call the converter init method
            List<ConverterItemDescriptor> converterItemInfos = new List<ConverterItemDescriptor>();
            var initCtx = new InitializeConverterContext { items = converterItemInfos };

            state.isLoading = true;

            converter.OnInitialize(initCtx, OnConverterCompleteDataCollection);

            void OnConverterCompleteDataCollection()
            {
                // Set the item infos list to to the right index
                state.items = new List<ConverterItemState>(converterItemInfos.Count);

                for (var j = 0; j < converterItemInfos.Count; j++)
                {
                    var converterItemState = new ConverterItemState()
                    {
                        descriptor = initCtx.items[j],
                        hasConverted = false,
                        isActive = true, // Default all the entries to true
                        status = Status.Pending
                    };

                    // The scan has already found some items that might not be convertible
                    if (!string.IsNullOrEmpty(converterItemInfos[j].warningMessage))
                    {
                        converterItemState.status = Status.Warning;
                        converterItemState.message = converterItemInfos[j].warningMessage;
                        converterItemState.isActive = false;
                    }

                    state.items.Add(converterItemState);
                }

                state.isLoading = false;
                state.isInitialized = true;

                Refresh();
                onScanFinish?.Invoke();
            }
        }

        public void Convert(string progressTitle)
        {
            if (state.pending == 0)
            {
                Debug.Log($"Skipping conversion, {converter.name} has no pending items to convert.");
                return;
            }

            var sb = new StringBuilder($"Conversion results for item: {displayName}:{Environment.NewLine}");

            converter.OnPreRun();
            int itemIndex = 0;
            int itemToConvertIndex = 0;
            foreach (var item in state.items)
            {
                if (EditorUtility.DisplayCancelableProgressBar(progressTitle,
                    $"({itemToConvertIndex} of {state.pending}) {item.descriptor.name}",
                    itemToConvertIndex / (float)state.pending))
                    break;

                if (!item.hasConverted && item.isActive)
                {
                    try
                    {
                        var itemToConvertInfo = new ConverterItemInfo()
                        {
                            index = itemIndex,
                            descriptor = item.descriptor,
                        };
                        var ctx = new RunItemContext(itemToConvertInfo);
                        converter.OnRun(ref ctx);
                        item.hasConverted = true;

                        if (ctx.didFail)
                        {
                            item.status = Status.Error;
                            item.message = ctx.info;
                            sb.AppendLine($"- {item.descriptor.name} ({item.status}) ({ctx.info})");
                        }
                        else
                        {
                            item.status = Status.Success;
                            item.message = string.Empty;
                            sb.AppendLine($"- {item.descriptor.name} ({item.status})");
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.LogError($"Exception {ex.Message} while converting {item.descriptor} from {displayName}");
                    }
                    itemToConvertIndex++;
                }
                itemIndex++;
            }
            converter.OnPostRun();

            Refresh();

            sb.AppendLine(state.ToString());
            Debug.Log(sb);
        }
    }
}
