using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A GraphElement to display the properties of a <see cref="IVariableDeclarationModel"/>.
    /// </summary>
    public class BlackboardVariablePropertyView : GraphElement
    {
        public static new readonly string ussClassName = "ge-blackboard-variable-property-view";
        public static readonly string rowUssClassName = ussClassName.WithUssElement("row");
        public static readonly string rowLabelUssClassName = ussClassName.WithUssElement("row-label");
        public static readonly string rowControlUssClassName = ussClassName.WithUssElement("row-control");

        public static readonly string rowTypeSelectorUssClassName = ussClassName.WithUssElement("row-type-selector");
        public static readonly string rowExposedUssClassName = ussClassName.WithUssElement("row-exposed");
        public static readonly string rowTooltipUssClassName = ussClassName.WithUssElement("row-tooltip");
        public static readonly string rowInitValueUssClassName = ussClassName.WithUssElement("row-init-value");

        protected Button m_TypeSelectorButton;
        protected Toggle m_ExposedToggle;
        protected TextField m_TooltipTextField;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardVariablePropertyView"/> class.
        /// </summary>
        public BlackboardVariablePropertyView()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();
            BuildRows();
        }

        protected virtual void BuildRows()
        {
            AddTypeSelector();
            AddExposedToggle();
            AddInitializationField();
            AddTooltipField();
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (Model is IVariableDeclarationModel variableDeclarationModel)
            {
                if (m_TypeSelectorButton != null)
                    m_TypeSelectorButton.text = GetTypeDisplayText();

                m_ExposedToggle?.SetValueWithoutNotify(variableDeclarationModel.IsExposed);
                m_TooltipTextField?.SetValueWithoutNotify(variableDeclarationModel.Tooltip);
            }
        }

        protected string GetTypeDisplayText()
        {
            if (Model is IVariableDeclarationModel variableDeclarationModel)
            {
                var stencil = View.GraphTool.ToolState.GraphModel.Stencil;
                return variableDeclarationModel.DataType.GetMetadata(stencil).FriendlyName;
            }

            return "";
        }

        protected static VisualElement MakeRow(string labelText, VisualElement control, string newRowUssClassName)
        {
            var row = new VisualElement { name = "blackboard-variable-property-view-row" };
            row.AddToClassList(BlackboardVariablePropertyView.rowUssClassName);
            if (!string.IsNullOrEmpty(newRowUssClassName))
                row.AddToClassList(newRowUssClassName);

            // TODO: Replace this with a variable pill/token and set isExposed appropriately
            var label = new Label(labelText);
            label.AddToClassList(rowLabelUssClassName);
            row.Add(label);

            if (control != null)
            {
                control.AddToClassList(rowControlUssClassName);
                row.Add(control);
            }

            return row;
        }

        protected void AddRow(string labelText, VisualElement control, string newRowUssClassName)
        {
            var row = MakeRow(labelText, control, newRowUssClassName);
            Add(row);
        }

        protected void InsertRow(int index, string labelText, VisualElement control, string newRowUssClassName)
        {
            var row = MakeRow(labelText, control, newRowUssClassName);
            Insert(index, row);
        }

        protected void AddTypeSelector()
        {
            void OnClick()
            {
                var pos = new Vector2(m_TypeSelectorButton.layout.xMin, m_TypeSelectorButton.layout.yMax);
                pos = m_TypeSelectorButton.parent.LocalToWorld(pos);
                // PF: FIX weird searcher position computation
                pos.y += 21;

                SearcherService.ShowVariableTypes(
                    (Stencil)Model.GraphModel.Stencil,
                    View.GraphTool.Preferences,
                    pos,
                    (t, i) => OnTypeChanged(t)
                );
            }

            m_TypeSelectorButton = new Button(OnClick) { text = GetTypeDisplayText() };
            AddRow("Type", m_TypeSelectorButton, rowTypeSelectorUssClassName);
        }

        protected void AddExposedToggle()
        {
            if (Model is IVariableDeclarationModel variableDeclarationModel)
            {
                m_ExposedToggle = new Toggle { value = variableDeclarationModel.IsExposed };
                m_ExposedToggle.RegisterValueChangedCallback(OnExposedChanged);
                AddRow("Exposed", m_ExposedToggle, rowExposedUssClassName);
            }
        }

        protected void AddTooltipField()
        {
            if (Model is IVariableDeclarationModel variableDeclarationModel)
            {
                m_TooltipTextField = new TextField
                {
                    isDelayed = true,
                    value = variableDeclarationModel.Tooltip
                };
                AddRow("Tooltip", m_TooltipTextField, rowTooltipUssClassName);
            }
        }

        protected void AddInitializationField()
        {
            VisualElement initializationElement = null;

            if (Model is IVariableDeclarationModel variableDeclarationModel)
            {
                if (variableDeclarationModel.InitializationModel == null)
                {
                    var stencil = (Stencil)View.GraphTool.ToolState.GraphModel.Stencil;
                    if (stencil.RequiresInitialization(variableDeclarationModel))
                    {
                        initializationElement = new Button(OnInitializationButton) { text = "Create Init value" };
                    }
                }
                else
                {
                    initializationElement = InlineValueEditor.CreateEditorForConstant(
                        View,
                        null,
                        variableDeclarationModel.InitializationModel,
                        (_, v) =>
                        {
                            GraphView.Dispatch(new UpdateConstantValueCommand(variableDeclarationModel.InitializationModel, v, variableDeclarationModel));
                        },
                        false);
                }
            }

            if (initializationElement != null)
                AddRow("Initialization", initializationElement, rowInitValueUssClassName);
        }

        protected virtual void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_ExposedToggle?.UnregisterValueChangedCallback(OnExposedChanged);
            m_TooltipTextField.UnregisterValueChangedCallback(OnTooltipChanged);
        }

        protected virtual void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_ExposedToggle?.RegisterValueChangedCallback(OnExposedChanged);
            m_TooltipTextField.RegisterValueChangedCallback(OnTooltipChanged);
        }

        protected void OnInitializationButton()
        {
            GraphView.Dispatch(new InitializeVariableCommand(Model as IVariableDeclarationModel));
        }

        protected void OnTypeChanged(TypeHandle handle)
        {
            GraphView.Dispatch(new ChangeVariableTypeCommand(Model as IVariableDeclarationModel, handle));
        }

        protected void OnExposedChanged(ChangeEvent<bool> evt)
        {
            GraphView.Dispatch(new ExposeVariableCommand(Model as IVariableDeclarationModel, m_ExposedToggle.value));
        }

        protected void OnTooltipChanged(ChangeEvent<string> evt)
        {
            GraphView.Dispatch(new UpdateTooltipCommand(Model as IVariableDeclarationModel, m_TooltipTextField.value));
        }
    }
}
