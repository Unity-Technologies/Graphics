using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for the text content of a sticky note.
    /// </summary>
    public class StickyNoteContentPart : BaseModelUIPart
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StickyNoteContentPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="StickyNoteContentPart"/>.</returns>
        public static StickyNoteContentPart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is IStickyNoteModel)
            {
                return new StickyNoteContentPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected EditableLabel TextLabel { get; set; }

        /// <inheritdoc />
        public override VisualElement Root => TextLabel;

        /// <summary>
        /// Initializes a new instance of the <see cref="StickyNoteContentPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected StickyNoteContentPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IStickyNoteModel)
            {
                TextLabel = new EditableLabel { name = PartName };
                TextLabel.multiline = true;
                TextLabel.RegisterCallback<ChangeEvent<string>>(OnRename);
                TextLabel.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                container.Add(TextLabel);
            }
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (TextLabel != null)
            {
                var value = (m_Model as IStickyNoteModel)?.Contents ?? String.Empty;
                TextLabel.SetValueWithoutNotify(value);
            }
        }

        protected void OnRename(ChangeEvent<string> e)
        {
            m_OwnerElement.View.Dispatch(new UpdateStickyNoteCommand(m_Model as IStickyNoteModel, null, e.newValue));
        }
    }
}
