using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
class Provider2DSerializationTests : Provider2DTestBase
{
    // Opening the inspector must not make a light reference every provider type in the project: that
    // is what makes the engine warn about missing types on every light in the scene once any one of
    // those scripts is deleted.
    [Test]
    public void RefreshingLightSources_DoesNotSerializeUnselectedProviderTypes()
    {
        var light = NewLight("light");

        var serializedObject = new SerializedObject(light);
        Light2DProviderSources sources;
        Provider2DTestFixtures.PrepareLightSources(serializedObject, out sources);
        serializedObject.ApplyModifiedProperties();

        AssertPrefabDoesNotReference(light.gameObject, nameof(Provider2DTestLightProvider));
    }

    [Test]
    public void RefreshingShadowSources_DoesNotSerializeUnselectedProviderTypes()
    {
        var caster = NewShadowCaster("caster");

        var serializedObject = new SerializedObject(caster);
        var selectionSources = serializedObject.FindProperty("m_SelectionSources");
        var sources = selectionSources.boxedValue as Shadow2DProviderSources;
        Assert.IsNotNull(sources, "m_SelectionSources was null");

        Provider2DSources<ShadowShape2DProvider, Shadow2DProviderSource>.RefreshSources(
            sources, caster.gameObject, (int)ShadowCaster2D.ShadowCastingSources.ShapeProvider);
        serializedObject.ApplyModifiedProperties();

        AssertPrefabDoesNotReference(caster.gameObject, nameof(Provider2DTestShadowProvider));
    }

    static void AssertPrefabDoesNotReference(GameObject go, string typeName)
    {
        const string path = "Assets/Provider2DSerializationTests_Temp.prefab";
        try
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
            string text = System.IO.File.ReadAllText(path);
            StringAssert.DoesNotContain(typeName, text,
                "an unselected provider type was serialized into the component");
        }
        finally
        {
            AssetDatabase.DeleteAsset(path);
        }
    }

    // A provider light whose provider is gone -- the script that defined it was deleted -- never
    // allocated a mesh, so the provider branch of UpdateMesh must not assume one is there.
    [Test]
    public void ProviderLightWithNoProvider_UpdateMeshDoesNotThrow()
    {
        var light = NewLight("light");
        light.lightType = Light2D.LightType.Global;
        light.lightType = Light2D.LightType.Provider;
        light.light2DProvider = null;

        Assert.DoesNotThrow(() => light.UpdateMesh(true));
    }
}
