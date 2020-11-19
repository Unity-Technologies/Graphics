namespace UnityEditor.ShaderGraph.Drawing.Interfaces
{
    interface IResizable
    {
        void OnStartResize();
        void OnResized();

        // Depending on the return value, the ElementResizer either allows resizing past parent view edge (like in case of StickyNote) or clamps the size at the edges of parent view (like in the Graph Inspector)
        bool CanResizePastParentBounds();
    }
}
