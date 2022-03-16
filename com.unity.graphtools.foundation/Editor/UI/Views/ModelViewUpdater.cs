using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Observer that updates a <see cref="IBaseModelView"/>.
    /// </summary>
    public class ModelViewUpdater : StateObserver
    {
        IBaseModelView m_View;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelViewUpdater" /> class.
        /// </summary>
        /// <param name="view">The <see cref="IBaseModelView"/> to update.</param>
        /// <param name="observedStateComponents">The state components that can cause the view to be updated.</param>
        public ModelViewUpdater(IBaseModelView view, params IStateComponent[] observedStateComponents) :
            base(observedStateComponents)
        {
            m_View = view;
        }

        /// <inheritdoc/>
        public override void Observe()
        {
            m_View?.UpdateFromModel();
        }
    }
}
