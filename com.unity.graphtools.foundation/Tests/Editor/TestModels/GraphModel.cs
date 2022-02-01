using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    public class GraphModel : BasicModel.GraphModel
    {
        public override Type DefaultStencilType => typeof(TestStencil);

        protected override Type GetEdgeType(IPortModel toPort, IPortModel fromPort)
        {
            return typeof(EdgeModel);
        }

        public bool IsRegistered(IGraphElementModel element)
        {
            return GetElementsByGuid().ContainsValue(element);
        }

        protected override IEdgeModel InstantiateEdge(IPortModel toPort, IPortModel fromPort, SerializableGUID guid = default)
        {
            var edgeModel = base.InstantiateEdge(toPort, fromPort, guid);

            if (edgeModel is EdgeModel testEdgeModel)
            {
                testEdgeModel.SetGraphModel(this);
            }

            return edgeModel;
        }

        protected override INodeModel InstantiateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SerializableGUID guid = default, Action<INodeModel> initializationCallback = null)
        {
            if (nodeTypeToCreate == null)
                throw new ArgumentNullException(nameof(nodeTypeToCreate));

            BasicModel.NodeModel nodeModel;
            if (typeof(IConstant).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = new ConstantNodeModel { Value = (IConstant)Activator.CreateInstance(nodeTypeToCreate) };
            else if (typeof(BasicModel.NodeModel).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = (BasicModel.NodeModel)Activator.CreateInstance(nodeTypeToCreate);
            else
                throw new ArgumentOutOfRangeException(nameof(nodeTypeToCreate));

            nodeModel.Position = position;
            nodeModel.Guid = guid.Valid ? guid : SerializableGUID.Generate();
            nodeModel.Title = nodeName;
            nodeModel.AssetModel = AssetModel;
            initializationCallback?.Invoke(nodeModel);
            nodeModel.OnCreateNode();

            return nodeModel;
        }

        public override IVariableNodeModel CreateVariableNode(IVariableDeclarationModel declarationModel,
            Vector2 position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return this.CreateNode<VariableNodeModel>(declarationModel.DisplayTitle, position, guid, v => v.DeclarationModel = declarationModel, spawnFlags);
        }

        protected override Type GetDefaultVariableDeclarationType()
        {
            return typeof(VariableDeclarationModel);
        }

        protected override IVariableDeclarationModel InstantiateVariableDeclaration(Type variableTypeToCreate,
            TypeHandle variableDataType, string variableName, ModifierFlags modifierFlags, bool isExposed,
            IConstant initializationModel, SerializableGUID guid, Action<IVariableDeclarationModel, IConstant> initializationCallback = null)
        {
            var vdm = base.InstantiateVariableDeclaration(variableTypeToCreate, variableDataType, variableName, modifierFlags, isExposed, initializationModel, guid, initializationCallback);

            if (vdm is VariableDeclarationModel testVdm)
                testVdm.SetGraphModel(this);

            return vdm;
        }

        protected override Type GetPlacematType()
        {
            return typeof(PlacematModel);
        }

        protected override IPlacematModel InstantiatePlacemat(Rect position, SerializableGUID guid)
        {
            var placematModel = base.InstantiatePlacemat(position, guid);

            if (placematModel is PlacematModel testPlacematModel)
                testPlacematModel.SetGraphModel(this);

            return placematModel;
        }

        protected override Type GetStickyNoteType()
        {
            return typeof(StickyNoteModel);
        }

        protected override IStickyNoteModel InstantiateStickyNote(Rect position)
        {
            var stickyNoteModel = base.InstantiateStickyNote(position);

            if (stickyNoteModel is StickyNoteModel testStickyNoteModel)
                testStickyNoteModel.SetGraphModel(this);

            return stickyNoteModel;
        }

        public override bool CheckIntegrity(Verbosity errors)
        {
            return true;
        }
    }
}
