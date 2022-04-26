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
            nodeModel.GraphModel = this;
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

        public override bool CheckIntegrity(Verbosity errors)
        {
            return true;
        }
    }
}
