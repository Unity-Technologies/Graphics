using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a sticky note in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class StickyNoteModel : GraphElementModel, IStickyNoteModel
    {
        [SerializeField]
        string m_Title;

        [SerializeField]
        string m_Contents;

        [SerializeField]
        string m_ThemeName = String.Empty;

        [SerializeField]
        string m_TextSizeName = String.Empty;

        [SerializeField]
        Rect m_Position;

        /// <inheritdoc />
        public Rect PositionAndSize
        {
            get => m_Position;
            set
            {
                var r = value;
                if (!this.IsResizable())
                    r.size = m_Position.size;

                if (!this.IsMovable())
                    r.position = m_Position.position;

                m_Position = r;
            }
        }

        /// <inheritdoc />
        public Vector2 Position
        {
            get => PositionAndSize.position;
            set
            {
                if (!this.IsMovable())
                    return;

                PositionAndSize = new Rect(value, PositionAndSize.size);
            }
        }

        /// <inheritdoc />
        public string Title
        {
            get => m_Title;
            set { if (value != null && m_Title != value) m_Title = value; }
        }

        /// <inheritdoc />
        public string DisplayTitle => Title;

        /// <inheritdoc />
        public string Contents
        {
            get => m_Contents;
            set { if (value != null && m_Contents != value) m_Contents = value; }
        }

        /// <inheritdoc />
        public string Theme
        {
            get => m_ThemeName;
            set => m_ThemeName = value;
        }

        /// <inheritdoc />
        public string TextSize
        {
            get => m_TextSizeName;
            set => m_TextSizeName = value;
        }

        /// <inheritdoc />
        public bool Destroyed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StickyNoteModel"/> class.
        /// </summary>
        public StickyNoteModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Overdrive.Capabilities.Deletable,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Renamable,
                Overdrive.Capabilities.Movable,
                Overdrive.Capabilities.Resizable,
                Overdrive.Capabilities.Ascendable
            });
            Title = string.Empty;
            Contents = string.Empty;
            Theme = StickyNoteColorTheme.Classic.ToString();
            TextSize = StickyNoteTextSize.Small.ToString();
            PositionAndSize = Rect.zero;
        }

        /// <inheritdoc />
        public void Destroy() => Destroyed = true;

        /// <inheritdoc />
        public void Move(Vector2 delta)
        {
            if (!this.IsMovable())
                return;

            Position += delta;
        }

        /// <inheritdoc />
        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Title = newName;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (Theme == "Dark")
                Theme = StickyNoteColorTheme.Black.ToString();
        }
    }
}
