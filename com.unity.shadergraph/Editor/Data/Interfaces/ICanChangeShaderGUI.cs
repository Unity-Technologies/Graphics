namespace UnityEditor.Graphing
{
    interface ICanChangeShaderGUI
    {
        string ShaderGUIOverride
        {
            get;
            set;
        }

        bool OverrideEnabled
        {
            get;
            set;
        }
    }
}
