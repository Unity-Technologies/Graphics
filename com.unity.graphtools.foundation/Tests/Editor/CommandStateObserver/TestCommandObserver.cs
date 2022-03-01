using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class TestCommandObserver
    {
        public int CommandObserved { get; private set; }

        public void Observe(ICommand command)
        {
            CommandObserved++;
        }
    }
}
