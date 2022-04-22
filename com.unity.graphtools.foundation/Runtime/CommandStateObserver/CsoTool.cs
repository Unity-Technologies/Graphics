using System;
using UnityEngine;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Defines a tool that uses a dispatcher and a state.
    /// </summary>
    public abstract class CsoTool : IDisposable, ICommandTarget
    {
        /// <summary>
        /// Creates and initializes a new tool.
        /// </summary>
        /// <typeparam name="T">The type of tool to create.</typeparam>
        /// <returns>The newly created tool.</returns>
        public static T Create<T>(Hash128 windowID) where T : CsoTool, new()
        {
            var tool = new T();
            tool.WindowID = windowID;
            tool.Initialize();
            return tool;
        }

        protected Hash128 WindowID { get; private set; }

        /// <summary>
        /// The command dispatcher.
        /// </summary>
        public Dispatcher Dispatcher { get; protected set; }

        /// <summary>
        /// The observer manager.
        /// </summary>
        public ObserverManager ObserverManager { get; protected set; }

        /// <summary>
        /// The state of the tool.
        /// </summary>
        public IState State { get; protected set; }

        /// <summary>
        /// Creates and initializes a command dispatcher.
        /// </summary>
        protected virtual void InitDispatcher()
        {
            Dispatcher = new Dispatcher();
        }

        /// <summary>
        /// Creates and initializes an observer manager.
        /// </summary>
        protected virtual void InitObserverManager()
        {
            ObserverManager = new ObserverManager();
        }

        /// <summary>
        /// Creates and initializes the state.
        /// </summary>
        /// <remarks>
        /// Derived tool classes should override this method to create the tool state components and add them to the <see cref="State"/>
        /// </remarks>
        protected virtual void InitState()
        {
            State = new State();
        }

        ~CsoTool()
        {
            Dispose(false);
        }

        /// <summary>
        /// Initializes the tool.
        /// </summary>
        protected virtual void Initialize()
        {
            InitDispatcher();
            InitObserverManager();
            InitState();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose implementation.
        /// </summary>
        /// <param name="disposing">When true, this method is called from IDisposable.Dispose.
        /// Otherwise it is called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (State is IDisposable disposableState)
                    disposableState.Dispose();

                Dispatcher = null;
                ObserverManager = null;
                State = null;
            }
        }

        /// <inheritdoc />
        public void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            Dispatcher.Dispatch(command, diagnosticsFlags);
        }
    }
}
