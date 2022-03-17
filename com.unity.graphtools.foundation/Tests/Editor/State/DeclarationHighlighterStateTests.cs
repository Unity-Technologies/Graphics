using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    public class DeclarationHighlighterStateTests
    {
        [Test]
        public void SetHighlightedDeclarationsWorks()
        {
            var declarations = new[]
            {
                new TestVariableDeclarationModel { CustomValue = 1 },
                new TestVariableDeclarationModel { CustomValue = 2 },
                new TestVariableDeclarationModel { CustomValue = 3 },
            };

            var otherDeclarations = new[]
            {
                new TestVariableDeclarationModel { CustomValue = 11 },
                new TestVariableDeclarationModel { CustomValue = 12 },
                new TestVariableDeclarationModel { CustomValue = 13 },
            };

            var state = new DeclarationHighlighterStateComponent();
            using (var updater = state.UpdateScope)
            {
                updater.SetHighlightedDeclarations(Hash128.Compute(0), declarations);
            }

            Assert.IsTrue(state.GetDeclarationModelHighlighted(declarations[0]));
            Assert.IsTrue(state.GetDeclarationModelHighlighted(declarations[1]));
            Assert.IsTrue(state.GetDeclarationModelHighlighted(declarations[2]));

            Assert.IsFalse(state.GetDeclarationModelHighlighted(otherDeclarations[0]));
            Assert.IsFalse(state.GetDeclarationModelHighlighted(otherDeclarations[1]));
            Assert.IsFalse(state.GetDeclarationModelHighlighted(otherDeclarations[2]));
        }

        [Test]
        public void AddingHighlightedDeclarationsFromAnotherViewWorks()
        {
            var declarations = new[]
            {
                new TestVariableDeclarationModel { CustomValue = 1 },
                new TestVariableDeclarationModel { CustomValue = 2 },
                new TestVariableDeclarationModel { CustomValue = 3 },
            };

            var otherDeclarations = new[]
            {
                new TestVariableDeclarationModel { CustomValue = 11 },
                new TestVariableDeclarationModel { CustomValue = 12 },
                new TestVariableDeclarationModel { CustomValue = 13 },
            };

            var state = new DeclarationHighlighterStateComponent();
            using (var updater = state.UpdateScope)
            {
                updater.SetHighlightedDeclarations(Hash128.Compute(0), declarations);
                updater.SetHighlightedDeclarations(Hash128.Compute(1), otherDeclarations);
            }

            Assert.IsTrue(state.GetDeclarationModelHighlighted(declarations[0]));
            Assert.IsTrue(state.GetDeclarationModelHighlighted(declarations[1]));
            Assert.IsTrue(state.GetDeclarationModelHighlighted(declarations[2]));

            Assert.IsTrue(state.GetDeclarationModelHighlighted(otherDeclarations[0]));
            Assert.IsTrue(state.GetDeclarationModelHighlighted(otherDeclarations[1]));
            Assert.IsTrue(state.GetDeclarationModelHighlighted(otherDeclarations[2]));
        }

        [Test]
        public void ReplacingHighlightedDeclarationsWorks()
        {
            var declarations = new[]
            {
                new TestVariableDeclarationModel { CustomValue = 1 },
                new TestVariableDeclarationModel { CustomValue = 2 },
                new TestVariableDeclarationModel { CustomValue = 3 },
            };

            var otherDeclarations = new[]
            {
                new TestVariableDeclarationModel { CustomValue = 11 },
                new TestVariableDeclarationModel { CustomValue = 12 },
                new TestVariableDeclarationModel { CustomValue = 13 },
            };

            var state = new DeclarationHighlighterStateComponent();
            using (var updater = state.UpdateScope)
            {
                updater.SetHighlightedDeclarations(Hash128.Compute(0), declarations);
                updater.SetHighlightedDeclarations(Hash128.Compute(0), otherDeclarations);
            }

            Assert.IsFalse(state.GetDeclarationModelHighlighted(declarations[0]));
            Assert.IsFalse(state.GetDeclarationModelHighlighted(declarations[1]));
            Assert.IsFalse(state.GetDeclarationModelHighlighted(declarations[2]));

            Assert.IsTrue(state.GetDeclarationModelHighlighted(otherDeclarations[0]));
            Assert.IsTrue(state.GetDeclarationModelHighlighted(otherDeclarations[1]));
            Assert.IsTrue(state.GetDeclarationModelHighlighted(otherDeclarations[2]));
        }

        [Test]
        public void ChangesetTracksAddedDeclarationsFromCleanState()
        {
            var declarations = new[]
            {
                new TestVariableDeclarationModel { CustomValue = 1 },
                new TestVariableDeclarationModel { CustomValue = 2 },
                new TestVariableDeclarationModel { CustomValue = 3 },
            };

            var state = new DeclarationHighlighterStateComponent();
            using (var updater = state.UpdateScope)
            {
                updater.SetHighlightedDeclarations(Hash128.Compute(0), declarations);
            }

            var changeset = state.GetAggregatedChangeset(0);
            Assert.IsTrue(changeset.ChangedModels.Contains(declarations[0]));
            Assert.IsTrue(changeset.ChangedModels.Contains(declarations[1]));
            Assert.IsTrue(changeset.ChangedModels.Contains(declarations[2]));
        }

        [Test]
        public void ChangesetTracksAddedDeclarations()
        {
            var state = new DeclarationHighlighterStateComponent();

            var step1 = new[]
            {
                new TestVariableDeclarationModel { CustomValue = 1 },
                new TestVariableDeclarationModel { CustomValue = 2 },
                new TestVariableDeclarationModel { CustomValue = 3 },
            };
            using (var updater = state.UpdateScope)
            {
                updater.SetHighlightedDeclarations(Hash128.Compute(0), step1);
            }

            var step1Version = state.CurrentVersion;

            var step2 = new[]
            {
                new TestVariableDeclarationModel { CustomValue = 1 },
                step1[1],
                new TestVariableDeclarationModel { CustomValue = 3 },
            };

            using (var updater = state.UpdateScope)
            {
                updater.SetHighlightedDeclarations(Hash128.Compute(0), step2);
            }

            // Get changes since the beginning.
            var changeset = state.GetAggregatedChangeset(0);

            Assert.IsTrue(changeset.ChangedModels.Contains(step1[0]));
            Assert.IsTrue(changeset.ChangedModels.Contains(step1[1]));
            Assert.IsTrue(changeset.ChangedModels.Contains(step1[2]));

            Assert.IsTrue(changeset.ChangedModels.Contains(step2[0]));
            Assert.IsTrue(changeset.ChangedModels.Contains(step2[1]));
            Assert.IsTrue(changeset.ChangedModels.Contains(step2[2]));

            // Get changes since after step 1.
            changeset = state.GetAggregatedChangeset(step1Version);

            Assert.IsTrue(changeset.ChangedModels.Contains(step2[0]));
            Assert.IsFalse(changeset.ChangedModels.Contains(step2[1]));
            Assert.IsTrue(changeset.ChangedModels.Contains(step2[2]));
        }
    }
}
