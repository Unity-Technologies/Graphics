using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;

namespace UnityEditor.ShaderGraph.Drawing
{
    class ChangeExposedFlagAction : IGraphDataAction
    {
        internal ChangeExposedFlagAction(ShaderInput shaderInput, bool newIsExposed)
        {
            this.shaderInputReference = shaderInput;
            this.newIsExposedValue = newIsExposed;
            this.oldIsExposedValue = shaderInput.generatePropertyBlock;
        }

        void ChangeExposedFlag(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out ChangeExposedFlagAction");
            AssertHelpers.IsNotNull(shaderInputReference, "ShaderInputReference is null while carrying out ChangeExposedFlagAction");
            // The Undos are currently handled in ShaderInputPropertyDrawer but we want to move that out from there and handle here
            //graphData.owner.RegisterCompleteObjectUndo("Change Exposed Toggle");
            shaderInputReference.generatePropertyBlock = newIsExposedValue;
        }

        public Action<GraphData> modifyGraphDataAction => ChangeExposedFlag;

        // Reference to the shader input being modified
        internal ShaderInput shaderInputReference { get; private set; }

        // New value of whether the shader input should be exposed to the material inspector
        internal bool newIsExposedValue { get; private set; }
        internal bool oldIsExposedValue { get; private set; }
    }

    class ChangePropertyValueAction : IGraphDataAction
    {
        void ChangePropertyValue(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out ChangePropertyValueAction");
            AssertHelpers.IsNotNull(shaderInputReference, "ShaderPropertyReference is null while carrying out ChangePropertyValueAction");
            // The Undos are currently handled in ShaderInputPropertyDrawer but we want to move that out from there and handle here
            //graphData.owner.RegisterCompleteObjectUndo("Change Property Value");
            switch (shaderInputReference)
            {
                case BooleanShaderProperty booleanProperty:
                    booleanProperty.value = ((ToggleData)newShaderInputValue).isOn;
                    break;
                case Vector1ShaderProperty vector1Property:
                    vector1Property.value = (float)newShaderInputValue;
                    break;
                case Vector2ShaderProperty vector2Property:
                    vector2Property.value = (Vector2)newShaderInputValue;
                    break;
                case Vector3ShaderProperty vector3Property:
                    vector3Property.value = (Vector3)newShaderInputValue;
                    break;
                case Vector4ShaderProperty vector4Property:
                    vector4Property.value = (Vector4)newShaderInputValue;
                    break;
                case ColorShaderProperty colorProperty:
                    colorProperty.value = (Color)newShaderInputValue;
                    break;
                case Texture2DShaderProperty texture2DProperty:
                    texture2DProperty.value.texture = (Texture)newShaderInputValue;
                    break;
                case Texture2DArrayShaderProperty texture2DArrayProperty:
                    texture2DArrayProperty.value.textureArray = (Texture2DArray)newShaderInputValue;
                    break;
                case Texture3DShaderProperty texture3DProperty:
                    texture3DProperty.value.texture = (Texture3D)newShaderInputValue;
                    break;
                case CubemapShaderProperty cubemapProperty:
                    cubemapProperty.value.cubemap = (Cubemap)newShaderInputValue;
                    break;
                case Matrix2ShaderProperty matrix2Property:
                    matrix2Property.value = (Matrix4x4)newShaderInputValue;
                    break;
                case Matrix3ShaderProperty matrix3Property:
                    matrix3Property.value = (Matrix4x4)newShaderInputValue;
                    break;
                case Matrix4ShaderProperty matrix4Property:
                    matrix4Property.value = (Matrix4x4)newShaderInputValue;
                    break;
                case SamplerStateShaderProperty samplerStateProperty:
                    samplerStateProperty.value = (TextureSamplerState)newShaderInputValue;
                    break;
                case GradientShaderProperty gradientProperty:
                    gradientProperty.value = (Gradient)newShaderInputValue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Action<GraphData> modifyGraphDataAction => ChangePropertyValue;

        // Reference to the shader input being modified
        internal ShaderInput shaderInputReference { get; set; }

        // New value of the shader property

        internal object newShaderInputValue { get; set; }
    }

    class ChangeDisplayNameAction : IGraphDataAction
    {
        void ChangeDisplayName(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out ChangeDisplayNameAction");
            AssertHelpers.IsNotNull(shaderInputReference, "ShaderInputReference is null while carrying out ChangeDisplayNameAction");
            graphData.owner.RegisterCompleteObjectUndo("Change Display Name");
            if (newDisplayNameValue != shaderInputReference.displayName)
            {
                shaderInputReference.SetDisplayNameAndSanitizeForGraph(graphData, newDisplayNameValue);
            }
        }

        public Action<GraphData> modifyGraphDataAction => ChangeDisplayName;

        // Reference to the shader input being modified
        internal ShaderInput shaderInputReference { get; set; }

        internal string newDisplayNameValue { get; set; }
    }

    class ChangeReferenceNameAction : IGraphDataAction
    {
        void ChangeReferenceName(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out ChangeReferenceNameAction");
            AssertHelpers.IsNotNull(shaderInputReference, "ShaderInputReference is null while carrying out ChangeReferenceNameAction");
            // The Undos are currently handled in ShaderInputPropertyDrawer but we want to move that out from there and handle here
            //graphData.owner.RegisterCompleteObjectUndo("Change Reference Name");
            if (newReferenceNameValue != shaderInputReference.overrideReferenceName)
            {
                graphData.SanitizeGraphInputReferenceName(shaderInputReference, newReferenceNameValue);
            }
        }

        public Action<GraphData> modifyGraphDataAction => ChangeReferenceName;

        // Reference to the shader input being modified
        internal ShaderInput shaderInputReference { get; set; }

        internal string newReferenceNameValue { get; set; }
    }

    class ResetReferenceNameAction : IGraphDataAction
    {
        void ResetReferenceName(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out ResetReferenceNameAction");
            AssertHelpers.IsNotNull(shaderInputReference, "ShaderInputReference is null while carrying out ResetReferenceNameAction");
            graphData.owner.RegisterCompleteObjectUndo("Reset Reference Name");
            shaderInputReference.overrideReferenceName = null;
        }

        public Action<GraphData> modifyGraphDataAction => ResetReferenceName;

        // Reference to the shader input being modified
        internal ShaderInput shaderInputReference { get; set; }
    }

    class DeleteShaderInputAction : IGraphDataAction
    {
        void DeleteShaderInput(GraphData graphData)
        {
            AssertHelpers.IsNotNull(graphData, "GraphData is null while carrying out DeleteShaderInputAction");
            AssertHelpers.IsNotNull(shaderInputsToDelete, "ShaderInputsToDelete is null while carrying out DeleteShaderInputAction");
            // This is called by MaterialGraphView currently, no need to repeat it here, though ideally it would live here
            //graphData.owner.RegisterCompleteObjectUndo("Delete Graph Input(s)");

            foreach (var shaderInput in shaderInputsToDelete)
            {
                graphData.RemoveGraphInput(shaderInput);
            }
        }

        public Action<GraphData> modifyGraphDataAction => DeleteShaderInput;

        // Reference to the shader input(s) being deleted
        internal IList<ShaderInput> shaderInputsToDelete { get; set; } = new List<ShaderInput>();
    }

    class ShaderInputViewController : SGViewController<ShaderInput, ShaderInputViewModel>
    {
        // Exposed for PropertyView
        internal GraphData graphData => DataStore.State;

        internal ShaderInputViewController(ShaderInput shaderInput, ShaderInputViewModel inViewModel, GraphDataStore graphDataStore)
            : base(shaderInput, inViewModel, graphDataStore)
        {
            InitializeViewModel();

            m_SgBlackboardField = new SGBlackboardField(ViewModel);
            m_SgBlackboardField.controller = this;

            m_BlackboardRowView = new SGBlackboardRow(m_SgBlackboardField, null);
            m_BlackboardRowView.expanded = SessionState.GetBool($"Unity.ShaderGraph.Input.{shaderInput.objectId}.isExpanded", false);
        }

        void InitializeViewModel()
        {
            if (Model == null)
            {
                AssertHelpers.Fail("Could not initialize shader input view model as shader input was null.");
                return;
            }
            ViewModel.model = Model;
            ViewModel.isSubGraph = DataStore.State.isSubGraph;
            ViewModel.isInputExposed = (DataStore.State.isSubGraph || (Model.isExposable && Model.generatePropertyBlock));
            ViewModel.inputName = Model.displayName;
            switch (Model)
            {
                case AbstractShaderProperty shaderProperty:
                    ViewModel.inputTypeName = shaderProperty.GetPropertyTypeString();
                    // Handles upgrade fix for deprecated old Color property
                    shaderProperty.onBeforeVersionChange += (_) => graphData.owner.RegisterCompleteObjectUndo($"Change {shaderProperty.displayName} Version");
                    break;
                case ShaderKeyword shaderKeyword:
                    ViewModel.inputTypeName = shaderKeyword.keywordType + " Keyword";
                    ViewModel.inputTypeName = shaderKeyword.isBuiltIn ? "Built-in " + ViewModel.inputTypeName : ViewModel.inputTypeName;
                    break;
                case ShaderDropdown shaderDropdown:
                    ViewModel.inputTypeName = "Dropdown";
                    break;
            }

            ViewModel.requestModelChangeAction = this.RequestModelChange;
        }

        SGBlackboardRow m_BlackboardRowView;
        SGBlackboardField m_SgBlackboardField;

        internal SGBlackboardRow BlackboardItemView => m_BlackboardRowView;

        protected override void RequestModelChange(IGraphDataAction changeAction)
        {
            DataStore.Dispatch(changeAction);
        }

        // Called by GraphDataStore.Subscribe after the model has been changed
        protected override void ModelChanged(GraphData graphData, IGraphDataAction changeAction)
        {
            switch (changeAction)
            {
                case ChangeExposedFlagAction changeExposedFlagAction:
                    // ModelChanged is called overzealously on everything
                    // but we only care if the action pertains to our Model
                    if (changeExposedFlagAction.shaderInputReference == Model)
                    {
                        ViewModel.isInputExposed = Model.generatePropertyBlock;
                        if (changeExposedFlagAction.oldIsExposedValue != changeExposedFlagAction.newIsExposedValue)
                            DirtyNodes(ModificationScope.Graph);
                        m_SgBlackboardField.UpdateFromViewModel();
                    }
                    break;

                case ChangePropertyValueAction changePropertyValueAction:
                    if (changePropertyValueAction.shaderInputReference == Model)
                    {
                        DirtyNodes(ModificationScope.Graph);
                        m_SgBlackboardField.MarkDirtyRepaint();
                    }
                    break;

                case ResetReferenceNameAction resetReferenceNameAction:
                    if (resetReferenceNameAction.shaderInputReference == Model)
                    {
                        DirtyNodes(ModificationScope.Graph);
                    }
                    break;

                case ChangeReferenceNameAction changeReferenceNameAction:
                    if (changeReferenceNameAction.shaderInputReference == Model)
                    {
                        DirtyNodes(ModificationScope.Graph);
                    }
                    break;

                case ChangeDisplayNameAction changeDisplayNameAction:
                    if (changeDisplayNameAction.shaderInputReference == Model)
                    {
                        ViewModel.inputName = Model.displayName;
                        DirtyNodes(ModificationScope.Topological);
                        m_SgBlackboardField.UpdateFromViewModel();
                    }
                    break;
            }
        }

        // TODO: This should communicate to node controllers instead of searching for the nodes themselves everytime, but that's going to take a while...
        internal void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            switch (Model)
            {
                case AbstractShaderProperty property:
                    var graphEditorView = m_BlackboardRowView.GetFirstAncestorOfType<GraphEditorView>();
                    if (graphEditorView == null)
                        return;
                    var colorManager = graphEditorView.colorManager;
                    var nodes = graphEditorView.graphView.Query<MaterialNodeView>().ToList();

                    colorManager.SetNodesDirty(nodes);
                    colorManager.UpdateNodeViews(nodes);

                    foreach (var node in DataStore.State.GetNodes<PropertyNode>())
                    {
                        node.Dirty(modificationScope);
                    }
                    break;
                case ShaderKeyword keyword:
                    foreach (var node in DataStore.State.GetNodes<KeywordNode>())
                    {
                        node.UpdateNode();
                        node.Dirty(modificationScope);
                    }

                    // Cant determine if Sub Graphs contain the keyword so just update them
                    foreach (var node in DataStore.State.GetNodes<SubGraphNode>())
                    {
                        node.Dirty(modificationScope);
                    }
                    break;
                case ShaderDropdown dropdown:
                    foreach (var node in DataStore.State.GetNodes<DropdownNode>())
                    {
                        node.UpdateNode();
                        node.Dirty(modificationScope);
                    }

                    // Cant determine if Sub Graphs contain the dropdown so just update them
                    foreach (var node in DataStore.State.GetNodes<SubGraphNode>())
                    {
                        node.Dirty(modificationScope);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void Destroy()
        {
            Cleanup();
            BlackboardItemView.RemoveFromHierarchy();
        }
    }
}
