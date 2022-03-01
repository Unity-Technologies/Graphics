using System;
using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Base class for commands that affect models.
    /// </summary>
    /// <typeparam name="TModel">The type of the affected models.</typeparam>
    public abstract class ModelCommand<TModel> : UndoableCommand
    {
        /// <summary>
        /// List of models affected by the command.
        /// </summary>
        public IReadOnlyList<TModel> Models;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelCommand{TModel}" /> class.
        /// </summary>
        /// <param name="undoString">The string to display in the undo menu item.</param>
        protected ModelCommand(string undoString)
        {
            UndoString = undoString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelCommand{TModel}" /> class.
        /// </summary>
        /// <param name="undoStringSingular">The string to display in the undo menu item when there is only one model affected.</param>
        /// <param name="undoStringPlural">The string to display in the undo menu item when there are many models affected.</param>
        /// <param name="models">The models affected by the command.</param>
        protected ModelCommand(string undoStringSingular, string undoStringPlural, IReadOnlyList<TModel> models)
        {
            Models = models;
            UndoString = Models == null || Models.Count <= 1 ? undoStringSingular : undoStringPlural;
        }
    }

    /// <summary>
    /// Base class for commands that set a single value on one or many models.
    /// </summary>
    /// <typeparam name="TModel">The type of the models.</typeparam>
    /// <typeparam name="TValue">The type of the value to set on the models.</typeparam>
    public abstract class ModelCommand<TModel, TValue> : ModelCommand<TModel>
    {
        /// <summary>
        /// The value to set on all the affected models.
        /// </summary>
        public TValue Value;

        /// <inheritdoc cref="ModelCommand{TModel}(string)"/>
        protected ModelCommand(string undoString) : base(undoString)
        {
        }

        /// <inheritdoc cref="ModelCommand{TModel}(string, string, IReadOnlyList{TModel})"/>
        protected ModelCommand(string undoStringSingular, string undoStringPlural,
                               TValue value,
                               IReadOnlyList<TModel> models)
            : base(undoStringSingular, undoStringPlural, models)
        {
            Value = value;
        }
    }
}
