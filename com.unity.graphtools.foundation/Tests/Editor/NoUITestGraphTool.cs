using System;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class TestCommandDispatcher : CommandDispatcher
    {
        public Func<bool> CheckIntegrity { get; set; }

        protected override void PostDispatchCommand(ICommand command)
        {
            base.PostDispatchCommand(command);

            if (CheckIntegrity != null)
                Assert.IsTrue(CheckIntegrity());
        }
    }

    public class NoUITestGraphTool : BaseGraphTool
    {
        public new TestCommandDispatcher Dispatcher => base.Dispatcher as TestCommandDispatcher;

        /// <inheritdoc />
        protected override void InitDispatcher()
        {
            base.Dispatcher = new TestCommandDispatcher();
        }
    }
}
