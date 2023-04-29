using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    /// <summary>
    /// VFX Template descriptor
    /// </summary>
    [Serializable]
    public struct VFXTemplateDescriptor : IVFXTemplateDescriptor
    {
        /// <summary>
        /// Name of the template
        /// </summary>
        public string name;
        /// <summary>
        /// Category is used to group templates together in the template window
        /// </summary>
        public string category;
        /// <summary>
        /// Give some description to your template so that we know what it's doing
        /// </summary>
        public string description;
        /// <summary>
        /// This icon is displayed next to the name in the template window
        /// </summary>
        public Texture2D icon;
        /// <summary>
        /// Thumbnail is displayed with the description in the details panel of the template window
        /// </summary>
        public Texture2D thumbnail;
        /// <summary>
        /// Internal use only
        /// </summary>
        [NonSerialized]
        public string assetGuid;
        /// <summary>
        /// Internal use only
        /// </summary>
        [NonSerialized]
        public int order;

        /// <summary>
        /// Same as the name
        /// </summary>
        public string header => name;
    }
}
