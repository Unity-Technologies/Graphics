namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Default implementation for <see cref="IToolbarProvider"/>.
    /// </summary>
    public class ToolbarProvider : IToolbarProvider
    {
        /// <inheritdoc />
        public bool ShowButton(string buttonName)
        {
            return buttonName != MainToolbar.EnableTracingButton && buttonName != MainToolbar.BuildAllButton;
        }
    }
}
