namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for sticky notes.
    /// </summary>
    public interface IStickyNoteModel : IMovable, IHasTitle, IRenamable, IResizable, IDestroyable
    {
        /// <summary>
        /// The text content of the note.
        /// </summary>
        string Contents { get; set; }

        /// <summary>
        /// The theme to use to display the note.
        /// </summary>
        string Theme { get; set; }

        /// <summary>
        /// The size of the text used to display the note.
        /// </summary>
        string TextSize { get; set; }
    }
}
