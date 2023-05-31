using System;
using NUnit.Framework;
using UnityEngine.Rendering;

class DebugMangerTests
{
    [Test]
    public void WindowStateCallbackIsTriggerred()
    {
        bool called = false;
        Action<DebugManager.UIMode, bool> action = (mode, open) =>
        {
            Assert.AreEqual(DebugManager.UIMode.EditorMode, mode);
            Assert.AreEqual(true, open);
            called = true;
        };

        DebugManager.windowStateChanged += action;

        DebugManager.instance.displayEditorUI = true;

        Assert.AreEqual(true, called);
        DebugManager.windowStateChanged -= action;

        DebugManager.instance.displayEditorUI = false;
    }
}
