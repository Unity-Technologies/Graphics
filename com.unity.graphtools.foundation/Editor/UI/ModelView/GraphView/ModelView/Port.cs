using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
// ReSharper disable InconsistentNaming

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// UI for a <see cref="IPortModel"/>.
    /// Allows connection of <see cref="Edge"/>s.
    /// Handles dropping of elements on top of them to create an edge.
    /// </summary>
    public class Port : ModelView, ISelectionDraggerTarget
    {
        public static readonly string ussClassName = "ge-port";
        public static readonly string highlightedModifierUssClassName = ussClassName.WithUssModifier("highlighted");
        public static readonly string willConnectModifierUssClassName = ussClassName.WithUssModifier("will-connect");
        public static readonly string connectedModifierUssClassName = ussClassName.WithUssModifier("connected");
        public static readonly string notConnectedModifierUssClassName = ussClassName.WithUssModifier("not-connected");
        public static readonly string inputModifierUssClassName = ussClassName.WithUssModifier("direction-input");
        public static readonly string outputModifierUssClassName = ussClassName.WithUssModifier("direction-output");
        public static readonly string capacityNoneModifierUssClassName = ussClassName.WithUssModifier("capacity-none");
        public static readonly string hiddenModifierUssClassName = ussClassName.WithUssModifier("hidden");
        public static readonly string dropHighlightAcceptedClass = ussClassName.WithUssModifier("drop-highlighted");
        public static readonly string dropHighlightDeniedClass = dropHighlightAcceptedClass.WithUssModifier("denied");
        public static readonly string portDataTypeClassNamePrefix = ussClassName.WithUssModifier("data-type-");
        public static readonly string portTypeModifierClassNamePrefix = ussClassName.WithUssModifier("type-");
        public static readonly string iconClass = "ge-icon";

        /// <summary>
        /// The prefix for the data type uss class name
        /// </summary>
        public static readonly string dataTypeClassPrefix = iconClass.WithUssModifier("data-type-");

        /// <summary>
        /// The USS class name used for vertical ports.
        /// </summary>
        public static readonly string verticalModifierUssClassName = ussClassName.WithUssModifier("vertical");

        public static readonly string connectorPartName = "connector-container";
        public static readonly string constantEditorPartName = "constant-editor";

        static readonly CustomStyleProperty<Color> k_PortColorProperty = new CustomStyleProperty<Color>("--port-color");

        int m_ConnectedEdgesCount = Int32.MinValue;

        static Color DefaultPortColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(193 / 255f, 193 / 255f, 193 / 255f);
                }

                return new Color(90 / 255f, 90 / 255f, 90 / 255f);
            }
        }

        List<IEdgeModel> m_LastUsedEdges;

        GraphView GraphView => RootView as GraphView;

        EdgeConnector m_EdgeConnector;

        public IPortModel PortModel => Model as IPortModel;

        public EdgeConnector EdgeConnector
        {
            get => m_EdgeConnector;
            protected set
            {
                ConnectorElement.ReplaceManipulator(ref m_EdgeConnector, value);
            }
        }

        public VisualElement ConnectorElement
        {
            get;
            private set;
        }

        public bool WillConnect
        {
            set => EnableInClassList(willConnectModifierUssClassName, value);
        }

        public bool Highlighted
        {
            set
            {
                EnableInClassList(highlightedModifierUssClassName, value);
                var connectedEdges = PortModel.GetConnectedEdges();
                for (int i = 0; i < connectedEdges.Count; i++)
                {
                    var edge = connectedEdges[i].GetView<Edge>(RootView);
                    if (edge != null)
                        edge.UpdateFromModel();
                }
            }
        }

        public Color PortColor { get; protected set; } = DefaultPortColor;

        protected string m_CurrentDropHighlightClass = dropHighlightAcceptedClass;

        /// <inheritdoc />
        public virtual bool CanAcceptDrop(IReadOnlyList<IGraphElementModel> droppedElements)
        {
            // Only one element can be dropped at once.
            if (droppedElements.Count != 1)
                return false;

            // The elements that can be dropped: a variable declaration from the Blackboard and any node with a single input or output (eg.: variable and constant nodes).
            switch (droppedElements[0])
            {
                case IVariableDeclarationModel variableDeclaration:
                    return CanAcceptDroppedVariable(variableDeclaration);
                case ISingleInputPortNodeModel singleInputNode:
                case ISingleOutputPortNodeModel singleOutputNode:
                    return GetPortToConnect(droppedElements[0]) != null;
                default:
                    return false;
            }
        }

        bool CanAcceptDroppedVariable(IVariableDeclarationModel variableDeclaration)
        {
            if (PortModel.Capacity == PortCapacity.None)
                return false;

            if (!variableDeclaration.IsInputOrOutput())
                return PortModel.Direction == PortDirection.Input && variableDeclaration.DataType == PortModel.DataTypeHandle;

            var isTrigger = variableDeclaration.IsInputOrOutputTrigger();
            if (isTrigger && PortModel.PortType != PortType.Execution || !isTrigger && PortModel.DataTypeHandle != variableDeclaration.DataType)
                return false;

            var isInput = variableDeclaration.Modifiers == ModifierFlags.Read;
            if (isInput && PortModel.Direction != PortDirection.Input || !isInput && PortModel.Direction != PortDirection.Output)
                return false;

            return true;
        }

        /// <inheritdoc />
        public virtual void ClearDropHighlightStatus()
        {
            RemoveFromClassList(m_CurrentDropHighlightClass);
        }

        /// <inheritdoc />
        public virtual void SetDropHighlightStatus(IReadOnlyList<IGraphElementModel> dropCandidates)
        {
            m_CurrentDropHighlightClass = CanAcceptDrop(dropCandidates) ? dropHighlightAcceptedClass : dropHighlightDeniedClass;
            AddToClassList(m_CurrentDropHighlightClass);
        }

        /// <inheritdoc />
        public virtual void PerformDrop(IReadOnlyList<IGraphElementModel> dropCandidates)
        {
            if (GraphView == null)
                return;

            var selectable = dropCandidates.Single();
            var portToConnect = GetPortToConnect(selectable);
            Assert.IsNotNull(portToConnect);

            var toPort = portToConnect.Direction == PortDirection.Input ? portToConnect : PortModel;
            var fromPort = portToConnect.Direction == PortDirection.Input ? PortModel : portToConnect;

            GraphView.Dispatch(new CreateEdgeCommand(toPort, fromPort, true));
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(PortConnectorWithIconPart.Create(connectorPartName, Model, this, ussClassName));
            PartList.AppendPart(PortConstantEditorPart.Create(constantEditorPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            ConnectorElement = this.SafeQ(connectorPartName) ?? this;
            EdgeConnector = new EdgeConnector(GraphView);

            AddToClassList(ussClassName);
            this.AddStylesheet("Port.uss");
        }

        /// <inheritdoc />
        public override bool HasModelDependenciesChanged()
        {
            if (m_LastUsedEdges == null || m_LastUsedEdges.Count != PortModel.GetConnectedEdges().Count())
                return true;
            for (var i = 0; i < m_LastUsedEdges.Count; i++)
                if (m_LastUsedEdges[i] != PortModel.GetConnectedEdges().ElementAt(i))
                    return false;
            return true;
        }

        /// <inheritdoc/>
        public override void AddModelDependencies()
        {
            foreach (var edgeModel in PortModel.GetConnectedEdges())
            {
                AddDependencyToEdgeModel(edgeModel);
            }
        }

        /// <inheritdoc />
        public override bool HasForwardsDependenciesChanged()
        {
            return m_ConnectedEdgesCount != PortModel.GetConnectedEdges().Count;
        }

        /// <inheritdoc />
        public override void AddForwardDependencies()
        {
            base.AddForwardDependencies();

            var edges = PortModel.GetConnectedEdges();
            m_ConnectedEdgesCount = edges.Count;
            foreach (var edgeModel in edges)
            {
                var ui = edgeModel.GetView(RootView);
                if (ui != null)
                    Dependencies.AddForwardDependency(ui, DependencyTypes.Geometry);
            }
        }

        /// <summary>
        /// Add <paramref name="edgeModel"/> as a model dependency to this element.
        /// </summary>
        /// <param name="edgeModel">The model to add as a dependency.</param>
        public void AddDependencyToEdgeModel(IEdgeModel edgeModel)
        {
            // When edge is created/deleted, port connector needs to be updated (filled/unfilled).
            Dependencies.AddModelDependency(edgeModel);
        }

        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            base.OnCustomStyleResolved(evt);
            var currentColor = PortColor;

            if (evt.customStyle.TryGetValue(k_PortColorProperty, out var portColorValue))
                PortColor = portColorValue;

            if (currentColor != PortColor && PartList.GetPart(connectorPartName) is PortConnectorWithIconPart portConnector)
            {
                portConnector.UpdateFromModel();
            }
        }

        protected static string GetClassNameSuffixForType(PortType t)
        {
            return t.ToString().ToLower();
        }

        bool PortHasOption(PortModelOptions portOptions, PortModelOptions optionToCheck)
        {
            return (portOptions & optionToCheck) != 0;
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            var hidden = PortModel != null && PortHasOption(PortModel.Options, PortModelOptions.Hidden);
            EnableInClassList(hiddenModifierUssClassName, hidden);

            bool portIsConnected = PortModel.IsConnected();
            EnableInClassList(connectedModifierUssClassName, portIsConnected);
            EnableInClassList(notConnectedModifierUssClassName, !portIsConnected);

            EnableInClassList(inputModifierUssClassName, PortModel.Direction == PortDirection.Input);
            EnableInClassList(outputModifierUssClassName, PortModel.Direction == PortDirection.Output);
            EnableInClassList(capacityNoneModifierUssClassName, PortModel.Capacity == PortCapacity.None);
            EnableInClassList(verticalModifierUssClassName, PortModel.Orientation == PortOrientation.Vertical);

            this.PrefixEnableInClassList(portDataTypeClassNamePrefix, GetClassNameSuffixForDataType(PortModel.PortDataType));

            this.PrefixEnableInClassList(portTypeModifierClassNamePrefix, GetClassNameSuffixForType(PortModel.PortType));

            tooltip = PortModel.Orientation == PortOrientation.Horizontal ? PortModel.ToolTip :
                string.IsNullOrEmpty(PortModel.ToolTip) ? PortModel.UniqueName :
                PortModel.UniqueName + "\n" + PortModel.ToolTip;
        }

        static readonly Dictionary<Type, string> k_TypeClassNameSuffix = new Dictionary<Type, string>();

        internal static string GetClassNameSuffixForDataType(Type thisPortType)
        {
            if (thisPortType == null)
                return String.Empty;

            if (thisPortType.IsSubclassOf(typeof(Component)))
                return "component";
            if (thisPortType.IsSubclassOf(typeof(GameObject)))
                return "game-object";
            if (thisPortType.IsSubclassOf(typeof(Rigidbody)) || thisPortType.IsSubclassOf(typeof(Rigidbody2D)))
                return "rigidbody";
            if (thisPortType.IsSubclassOf(typeof(Transform)))
                return "transform";
            if (thisPortType.IsSubclassOf(typeof(Texture)) || thisPortType.IsSubclassOf(typeof(Texture2D)))
                return "texture2d";
            if (thisPortType.IsSubclassOf(typeof(KeyCode)))
                return "key-code";
            if (thisPortType.IsSubclassOf(typeof(Material)))
                return "material";
            if (thisPortType == typeof(Object))
                return "object";
            if (thisPortType == typeof(MissingPort))
                return "missing-port";

            if (!k_TypeClassNameSuffix.TryGetValue(thisPortType, out var kebabCaseName))
            {
                kebabCaseName = thisPortType.Name.ToKebabCase();
                k_TypeClassNameSuffix.Add(thisPortType, kebabCaseName);
            }
            return kebabCaseName;
        }

        public Vector3 GetGlobalCenter()
        {
            if (GraphView != null && GraphView.GetPortCenterOverride(this, out var overriddenPosition))
            {
                return overriddenPosition;
            }

            var connector = GetConnector();
            var localCenter = new Vector2(connector.layout.width * .5f, connector.layout.height * .5f);
            return connector.LocalToWorld(localCenter);
        }

        public VisualElement GetConnector()
        {
            var portConnector = PartList.GetPart(connectorPartName) as PortConnectorPart;
            return portConnector?.Connector ?? portConnector?.Root ?? this;
        }

        IPortModel GetPortToConnect(IGraphElementModel selectable)
        {
            return (selectable as IPortNodeModel)?.GetPortFitToConnectTo(PortModel);
        }
    }
}
