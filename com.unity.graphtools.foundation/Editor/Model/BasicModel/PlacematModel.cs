using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a placemat in a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class PlacematModel : GraphElementModel, IPlacematModel
    {
        const string k_DefaultPlacematName = "Placemat";

        [SerializeField]
        string m_Title;

        [SerializeField]
        Rect m_Position;

        [SerializeField]
        bool m_Collapsed;

        [SerializeField]
        List<string> m_HiddenElements;

        List<IGraphElementModel> m_CachedHiddenElementModels;

        /// <inheritdoc />
        public override Color DefaultColor => new Color(0.15f, 0.19f, 0.19f);

        /// <inheritdoc />
        public string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        /// <inheritdoc />
        public string DisplayTitle => Title;

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
        public bool Collapsed
        {
            get => m_Collapsed;
            set
            {
                if (!this.IsCollapsible())
                    return;

                m_Collapsed = value;
                this.SetCapability(Overdrive.Capabilities.Resizable, !m_Collapsed);
            }
        }

        public List<string> HiddenElementsGuid
        {
            get => m_HiddenElements;
            set
            {
                m_HiddenElements = value;
                m_CachedHiddenElementModels = null;
            }
        }

        /// <inheritdoc />
        public bool Destroyed { get; private set; }

        public PlacematModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Overdrive.Capabilities.Deletable,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Renamable,
                Overdrive.Capabilities.Movable,
                Overdrive.Capabilities.Resizable,
                Overdrive.Capabilities.Collapsible,
                Overdrive.Capabilities.Colorable,
                Overdrive.Capabilities.Ascendable
            });
            Title = k_DefaultPlacematName;
        }

        /// <inheritdoc />
        public void Destroy() => Destroyed = true;

        /// <inheritdoc />
        public void Move(Vector2 delta)
        {
            if (!this.IsMovable())
                return;

            PositionAndSize = new Rect(PositionAndSize.position + delta, PositionAndSize.size);
        }

        /// <inheritdoc />
        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Title = newName;
        }

        /// <inheritdoc />
        public IEnumerable<IGraphElementModel> HiddenElements
        {
            get
            {
                if (m_CachedHiddenElementModels == null)
                {
                    if (HiddenElementsGuid != null)
                    {
                        m_CachedHiddenElementModels = new List<IGraphElementModel>();
                        foreach (var elementModelGuid in HiddenElementsGuid)
                        {
                            foreach (var node in GraphModel.NodeModels)
                            {
                                if (node.Guid.ToString() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(node);
                                }
                            }

                            foreach (var sticky in GraphModel.StickyNoteModels)
                            {
                                if (sticky.Guid.ToString() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(sticky);
                                }
                            }

                            foreach (var placemat in GraphModel.PlacematModels)
                            {
                                if (placemat.Guid.ToString() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(placemat);
                                }
                            }
                        }
                    }
                }

                return m_CachedHiddenElementModels ?? Enumerable.Empty<IGraphElementModel>();
            }
            set
            {
                if (value == null)
                {
                    m_HiddenElements = null;
                }
                else
                {
                    m_HiddenElements = new List<string>(value.Select(e => e.Guid.ToString()));
                }

                m_CachedHiddenElementModels = null;
            }
        }

        /// <inheritdoc />
        public int GetZOrder()
        {
            return GraphModel.PlacematModels.IndexOfInternal(this);
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_CachedHiddenElementModels = null;

            if (Version <= SerializationVersion.GTF_V_0_8_2)
            {
                if (DefaultColor != InternalSerializedColor)
                {
                    // sets HasUserColor properly
                    Color = InternalSerializedColor;
                }
            }
        }
    }
}
