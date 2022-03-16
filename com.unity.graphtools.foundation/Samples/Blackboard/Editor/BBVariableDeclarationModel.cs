using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    public enum VariableType
    {
        Input,
        Output,
        Variable,
        Stuff
    }

    public abstract class BBDeclarationModel : VariableDeclarationModel
    {
        public abstract VariableType Type { get; }

        int m_SomeValue;

        public int SomeValue
        {
            get => m_SomeValue;
            set => m_SomeValue = value;
        }
    }

    public class BBInputVariableDeclarationModel : BBDeclarationModel
    {
        public override VariableType Type => VariableType.Input;
    }

    public class BBOutputVariableDeclarationModel : BBDeclarationModel
    {
        public override VariableType Type => VariableType.Output;
    }

    public class BBVariableDeclarationModel : BBDeclarationModel
    {
        public override VariableType Type => VariableType.Variable;
    }

    public class BBStuffVariableDeclarationModel : BBDeclarationModel
    {
        public override VariableType Type => VariableType.Stuff;
    }
}
