using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model to hold error messages to be displayed by badges.
    /// </summary>
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class ErrorBadgeModel : BadgeModel, IErrorBadgeModel
    {
        [SerializeField]
        protected string m_ErrorMessage;

        /// <inheritdoc />
        public string ErrorMessage => m_ErrorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorBadgeModel"/> class.
        /// </summary>
        /// <param name="parentModel">Parent model of the badge</param>
        public ErrorBadgeModel(IGraphElementModel parentModel)
            : base(parentModel) {}
    }
}
