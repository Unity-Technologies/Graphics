namespace UnityEditor.ShaderGraph.Drawing.Interfaces
{
    interface IResizable
    {
        void OnStartResize();
        void OnResized();
        bool CanResizePastParentBounds();
    }
}
