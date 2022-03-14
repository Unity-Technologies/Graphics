using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A list of <see cref="IModelUIPart"/>.
    /// </summary>
    public class ModelUIPartList
    {
        List<IModelUIPart> m_Parts = new List<IModelUIPart>();

        public IReadOnlyList<IModelUIPart> Parts => m_Parts;

        /// <summary>
        /// Adds a part to this list.
        /// </summary>
        /// <param name="child">The part to add.</param>
        public void AppendPart(IModelUIPart child)
        {
            if (child != null)
                m_Parts.Add(child);
        }

        /// <summary>
        /// Gets the part with <see cref="IModelUIPart.PartName"/> equal to <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The part name to match.</param>
        /// <returns>The part found, or null if no part was found.</returns>
        public IModelUIPart GetPart(string name)
        {
            for (int i = 0; i < m_Parts.Count; i++)
            {
                var part = m_Parts[i];
                if (part.PartName == name)
                    return part;
            }

            return null;
        }

        /// <summary>
        /// Inserts a <see cref="IModelUIPart"/> before the part named <paramref name="beforeChild"/>.
        /// </summary>
        /// <param name="beforeChild">The name of the part before which <paramref name="child"/> should be inserted.</param>
        /// <param name="child">The part to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">If there is no part named <paramref name="beforeChild"/>.</exception>
        public void InsertPartBefore(string beforeChild, IModelUIPart child)
        {
            if (child != null)
            {
                var index = -1;
                for (int i = 0; i < m_Parts.Count; i++)
                {
                    var part = m_Parts[i];
                    if (part.PartName == beforeChild)
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    m_Parts.Insert(index, child);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(beforeChild), beforeChild, "Part not found");
                }
            }
        }

        /// <summary>
        /// Inserts a <see cref="IModelUIPart"/> after the part named <paramref name="afterChild"/>.
        /// </summary>
        /// <param name="afterChild">The name of the part after which <paramref name="child"/> should be inserted.</param>
        /// <param name="child">The part to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">If there is no part named <paramref name="afterChild"/>.</exception>
        public void InsertPartAfter(string afterChild, IModelUIPart child)
        {
            if (child != null)
            {
                var index = -1;
                for (int i = 0; i < m_Parts.Count; i++)
                {
                    var part = m_Parts[i];
                    if (part.PartName == afterChild)
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    m_Parts.Insert(index + 1, child);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(afterChild), afterChild, "Part not found");
                }
            }
        }

        /// <summary>
        /// Replaces the <see cref="IModelUIPart"/> named <paramref name="componentToReplace"/> by <paramref name="child"/>.
        /// </summary>
        /// <param name="componentToReplace">The name of the part to replace.</param>
        /// <param name="child">The part to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException">If there is no part named <paramref name="componentToReplace"/>.</exception>
        public void ReplacePart(string componentToReplace, IModelUIPart child)
        {
            if (child != null)
            {
                var index = -1;
                for (int i = 0; i < m_Parts.Count; i++)
                {
                    var part = m_Parts[i];
                    if (part.PartName == componentToReplace)
                    {
                        index = i;
                        break;
                    }
                }

                if (index != -1)
                {
                    m_Parts.RemoveAt(index);
                    m_Parts.Insert(index, child);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(componentToReplace), componentToReplace, "Part not found");
                }
            }
        }

        /// <summary>
        /// Removes the <see cref="IModelUIPart"/> named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the part to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">If there is no part named <paramref name="name"/>.</exception>
        public void RemovePart(string name)
        {
            var index = -1;
            for (int i = 0; i < m_Parts.Count; i++)
            {
                var part = m_Parts[i];
                if (part.PartName == name)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                m_Parts.RemoveAt(index);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(name), name, "Part not found");
            }
        }
    }
}
