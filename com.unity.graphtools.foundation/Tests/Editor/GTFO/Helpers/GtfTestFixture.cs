using UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO
{
    public class GtfTestFixture : BaseUIFixture
    {
        /// <inheritdoc />
        protected override bool CreateGraphOnStartup => true;
    }
}
