using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class SGVariableDeclarationModel : DeclarationModel, IVariableDeclarationModel
    {
        public IGroupModel ParentGroup { get; set; }
        public IEnumerable<IGraphElementModel> ContainedModels { get; }
        public TypeHandle DataType { get; set; }
        public ModifierFlags Modifiers { get; set; }
        public string Tooltip { get; set; }
        public IConstant InitializationModel { get; }
        public bool IsExposed { get; set; }

        IConstant m_InitializationValue;

        public string GetVariableName()
        {
            throw new System.NotImplementedException();
        }

        public void CreateInitializationValue()
        {
            throw new System.NotImplementedException();
        }

        public bool IsUsed()
        {
            throw new System.NotImplementedException();
        }
    }
}
