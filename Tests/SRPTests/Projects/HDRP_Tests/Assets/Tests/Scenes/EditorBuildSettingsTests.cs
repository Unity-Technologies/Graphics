using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

public class EditorBuildSettingsTests
{
    [Test, GraphicsTest, EditorBuildSettingsScenes]
    public void CheckBuildMissingScenes(GraphicsTestCase testCase)
    {
        // No need to assert anything, as an exception will be thrown by the `EditorBuildSettingsScenes` if any scenes
        // are missing.
    }
}
