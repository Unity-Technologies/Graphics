using UnityEngine;
using UnityEngine.Experimental.Rendering;
using NUnit.Framework;

[TestFixture]
public class CullResultsTest
{
    void InspectCullResults(Camera camera, CullResults cullResults, ScriptableRenderContext renderContext)
    {
        VisibleReflectionProbe[] probes = cullResults.visibleReflectionProbes;

        Assert.AreEqual(1, probes.Length, "Incorrect reflection probe count");

        VisibleReflectionProbe probeA = probes[0];
        Assert.NotNull(probeA.texture, "probe texture");

        VisibleLight[] lights = cullResults.visibleLights;
        Assert.AreEqual(3, lights.Length, "Incorrect light count");

        LightType[] expectedTypes = new LightType[] { LightType.Directional, LightType.Spot, LightType.Point };
        for (int i = 0; i != 2; i++)
        {
            Assert.AreEqual(expectedTypes[i], lights[i].lightType,
                "Incorrect light type for light " + i.ToString() + " (" + lights[i].light.name + ")");
        }

        // @TODO..
    }

    [Test]
    public void TestReflectionProbes()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Editor/Tests/TestScene.unity");
        RenderLoopTestFixtureInstance.Run(InspectCullResults);
    }
}
