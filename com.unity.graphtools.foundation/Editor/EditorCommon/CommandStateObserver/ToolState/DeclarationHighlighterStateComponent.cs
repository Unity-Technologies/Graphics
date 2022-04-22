using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Stores the <see cref="IDeclarationModel"/> to highlight.
    /// </summary>
    public class DeclarationHighlighterStateComponent : StateComponent<DeclarationHighlighterStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="DeclarationHighlighterStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<DeclarationHighlighterStateComponent>
        {
            /// <summary>
            /// Sets the list of declaration to highlight.
            /// </summary>
            /// <param name="sourceStateHashGuid">The unique identifier of the object that requests elements to be highlighted.</param>
            /// <param name="declarations">The declarations to highlight.</param>
            public void SetHighlightedDeclarations(Hash128 sourceStateHashGuid, IEnumerable<IDeclarationModel> declarations)
            {
                var newDeclarations = declarations.ToList();
                HashSet<IDeclarationModel> changedDeclarations = new HashSet<IDeclarationModel>(newDeclarations);

                if (m_State.m_HighlightedDeclarations.TryGetValue(sourceStateHashGuid, out var currentDeclarations))
                {
                    // changedDeclarations = changedDeclarations XOR currentDeclarations
                    changedDeclarations.SymmetricExceptWith(currentDeclarations);
                }

                m_State.m_HighlightedDeclarations[sourceStateHashGuid] = newDeclarations;

                m_State.CurrentChangeset.ChangedModels.UnionWith(changedDeclarations);
                m_State.SetUpdateType(UpdateType.Partial);
            }
        }

        ChangesetManager<SimpleChangeset<IDeclarationModel>> m_ChangesetManager = new ChangesetManager<SimpleChangeset<IDeclarationModel>>();

        /// <inheritdoc />
        protected override IChangesetManager ChangesetManager => m_ChangesetManager;

        SimpleChangeset<IDeclarationModel> CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        // The highlighted declaration, grouped by the id of the object (usually a view) that
        // asked for the elements to be highlighted.
        Dictionary<Hash128, List<IDeclarationModel>> m_HighlightedDeclarations;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeclarationHighlighterStateComponent" /> class.
        /// </summary>
        public DeclarationHighlighterStateComponent()
        {
            m_HighlightedDeclarations = new Dictionary<Hash128, List<IDeclarationModel>>();
        }

        /// <summary>
        /// Gets the highlighted state of a declaration model.
        /// </summary>
        /// <param name="model">The declaration model.</param>
        /// <returns>True is the UI for the model should be highlighted. False otherwise.</returns>
        public bool GetDeclarationModelHighlighted(IDeclarationModel model)
        {
            if (model != null)
            {
                foreach (var declarationForView in m_HighlightedDeclarations)
                {
                    if (declarationForView.Value.Contains(model))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <returns>The aggregated changeset.</returns>
        public SimpleChangeset<IDeclarationModel> GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is DeclarationHighlighterStateComponent highlighterStateComponent)
            {
                m_HighlightedDeclarations = highlighterStateComponent.m_HighlightedDeclarations;
                highlighterStateComponent.m_HighlightedDeclarations = null;
            }
        }
    }
}
