using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    internal interface INodeValidationExtension
    {
        internal enum Status { Warning, Error, None }

        string GetValidatorKey();
        Status GetValidationStatus(AbstractMaterialNode node, out string msg);
    }

    internal static class NodeValidation
    {
        private static List<INodeValidationExtension> s_validators;
        private static void Init()
        {
            s_validators = new();
            foreach (var type in TypeCache.GetTypesDerivedFrom(typeof(INodeValidationExtension)).Where(e => !e.IsGenericType))
            {
                var validator = (INodeValidationExtension)Activator.CreateInstance(type);
                s_validators.Add(validator);
            }
        }

        internal static void HandleValidationExtensions(AbstractMaterialNode node)
        {
            if (node.owner == null || node.owner.messageManager == null)
                return;

            if (s_validators == null)
                Init();

            foreach(var validator in s_validators)
            {
                var status = validator.GetValidationStatus(node, out var msg);

                if (node.owner.messageManager.AnyError(e => e == node.objectId))
                    node.owner.messageManager.ClearNodesFromProvider(validator, Enumerable.Repeat(node, 1));

                if (status != INodeValidationExtension.Status.None)
                {
                    var severity = status == INodeValidationExtension.Status.Warning ? Rendering.ShaderCompilerMessageSeverity.Warning : Rendering.ShaderCompilerMessageSeverity.Error;
                    node.owner.messageManager.AddOrAppendError(validator, node.objectId, new ShaderMessage(msg, severity));
                }
            }
        }
    }
}
