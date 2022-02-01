using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public enum StickyNoteTheme
    {
        Classic,
        Black,
        Dark,
        Orange,
        Green,
        Blue,
        Red,
        Purple,
        Teal
    }

    public enum StickyNoteFontSize
    {
        Small,
        Medium,
        Large,
        Huge
    }

    /// <summary>
    /// UI for a <see cref="StickyNoteModel"/>.
    /// </summary>
    public class StickyNote : GraphElement
    {
        public new class UxmlFactory : UxmlFactory<StickyNote> {}

        public static readonly Vector2 defaultSize = new Vector2(200, 160);

        public new static readonly string ussClassName = "ge-sticky-note";
        static readonly string themeClassNamePrefix = ussClassName.WithUssModifier("theme-");
        static readonly string sizeClassNamePrefix = ussClassName.WithUssModifier("size-");

        public static readonly string selectionBorderElementName = "selection-border";
        public static readonly string disabledOverlayElementName = "disabled-overlay";
        public static readonly string titleContainerPartName = "title-container";
        public static readonly string contentContainerPartName = "text-container";
        public static readonly string resizerPartName = "resizer";

        protected VisualElement m_ContentContainer;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        public IStickyNoteModel StickyNoteModel => Model as IStickyNoteModel;

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(EditableTitlePart.Create(titleContainerPartName, Model, this, ussClassName, true));
            PartList.AppendPart(StickyNoteContentPart.Create(contentContainerPartName, Model, this, ussClassName));
            PartList.AppendPart(FourWayResizerPart.Create(resizerPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            var selectionBorder = new SelectionBorder { name = selectionBorderElementName };
            selectionBorder.AddToClassList(ussClassName.WithUssElement(selectionBorderElementName));
            Add(selectionBorder);
            m_ContentContainer = selectionBorder.ContentContainer;

            base.BuildElementUI();

            var disabledOverlay = new VisualElement { name = disabledOverlayElementName, pickingMode = PickingMode.Ignore };
            hierarchy.Add(disabledOverlay);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(ussClassName);
            this.AddStylesheet("StickyNote.uss");
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            var newPos = StickyNoteModel.PositionAndSize;
            style.left = newPos.x;
            style.top = newPos.y;
            style.width = newPos.width;
            style.height = newPos.height;

            this.PrefixEnableInClassList(themeClassNamePrefix, StickyNoteModel.Theme.ToKebabCase());
            this.PrefixEnableInClassList(sizeClassNamePrefix, StickyNoteModel.TextSize.ToKebabCase());
        }

        public static IEnumerable<string> GetThemes()
        {
            return Enum.GetNames(typeof(StickyNoteTheme));
        }

        public static IEnumerable<string> GetSizes()
        {
            return Enum.GetNames(typeof(StickyNoteFontSize));
        }
    }
}
