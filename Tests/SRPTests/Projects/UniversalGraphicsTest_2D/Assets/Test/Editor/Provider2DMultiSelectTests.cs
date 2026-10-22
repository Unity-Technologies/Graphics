using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

class Provider2DMultiSelectTests : Provider2DTestBase
{
    static readonly Shadow2DProviderSources_PropertyDrawer s_ShadowDrawer = new Shadow2DProviderSources_PropertyDrawer();

    static GUIContent PickLightSource(SerializedObject serializedObject, string name)
    {
        Light2DProviderSources sources;
        Provider2DTestFixtures.PrepareLightSources(serializedObject, out sources);

        var names = sources.GetSourceNames();
        int index = Provider2DTestFixtures.IndexOfName(names, name);
        Assert.GreaterOrEqual(index, 0, "source '" + name + "' was not listed");
        return names[index];
    }

    [Test]
    public void SingleLight_SelectingCustomProvider_AssignsProvider()
    {
        var light = NewLight("light");

        var serializedObject = new SerializedObject(light);
        Light2DEditorUtility.ApplyLightTypeSelection(
            serializedObject, PickLightSource(serializedObject, Provider2DTestFixtures.kLightProviderName));

        Assert.AreEqual(Light2D.LightType.Provider, light.lightType);
        Assert.IsNotNull(light.light2DProvider, "provider was not assigned");
    }

    [Test]
    public void MultipleLights_SelectingCustomProvider_AssignsProviderToAll()
    {
        var lightA = NewLight("lightA");
        var lightB = NewLight("lightB");

        var serializedObject = new SerializedObject(new UnityEngine.Object[] { lightA, lightB });
        Light2DEditorUtility.ApplyLightTypeSelection(
            serializedObject, PickLightSource(serializedObject, Provider2DTestFixtures.kLightProviderName));

        Assert.AreEqual(Light2D.LightType.Provider, lightA.lightType, "lightA type");
        Assert.AreEqual(Light2D.LightType.Provider, lightB.lightType, "lightB type");
        Assert.IsNotNull(lightA.light2DProvider, "lightA provider");
        Assert.IsNotNull(lightB.light2DProvider, "lightB provider");
        Assert.AreNotSame(lightA.light2DProvider, lightB.light2DProvider, "both lights share one provider instance");
    }

    [Test]
    public void MultipleLights_SelectingBuiltInType_AppliesToAll()
    {
        var lightA = NewLight("lightA");
        var lightB = NewLight("lightB");

        var serializedObject = new SerializedObject(new UnityEngine.Object[] { lightA, lightB });

        // Both lights target every sorting layer, so lightB turning Global clashes with lightA once per layer.
        foreach (var layer in SortingLayer.layers)
            LogAssert.Expect(LogType.Error, $"More than one global light on layer {layer.name} for light blend style index 0");

        Light2DEditorUtility.ApplyLightTypeSelection(serializedObject, PickLightSource(serializedObject, "Global"));

        Assert.AreEqual(Light2D.LightType.Global, lightA.lightType, "lightA type");
        Assert.AreEqual(Light2D.LightType.Global, lightB.lightType, "lightB type");
    }

    // A light left on Provider with no provider had no mesh to clear, which threw on the next update.
    [Test]
    public void MultipleLights_AfterSelectingCustomProvider_UpdateMeshDoesNotThrow()
    {
        var lightA = NewLight("lightA");
        var lightB = NewLight("lightB");
        lightA.lightType = Light2D.LightType.Global;
        lightB.lightType = Light2D.LightType.Global;

        var serializedObject = new SerializedObject(new UnityEngine.Object[] { lightA, lightB });
        Light2DEditorUtility.ApplyLightTypeSelection(
            serializedObject, PickLightSource(serializedObject, Provider2DTestFixtures.kLightProviderName));

        Assert.DoesNotThrow(() => lightA.UpdateMesh(true), "lightA UpdateMesh");
        Assert.DoesNotThrow(() => lightB.UpdateMesh(true), "lightB UpdateMesh");
    }

    // Lights whose GameObjects carry different components offer different sources, so the dropdown
    // must not offer one that only some of them can take -- applying it would silently skip the rest.
    [Test]
    public void LightsWithDifferentComponents_OnlyOfferWhatTheyShare()
    {
        var withProvider = NewLight("withProvider");
        var withoutProvider = NewLightWithoutProvider("withoutProvider");

        var serializedObject = new SerializedObject(new UnityEngine.Object[] { withProvider, withoutProvider });
        Light2DProviderSources sources;
        int selectedIndex = Provider2DTestFixtures.PrepareLightSources(serializedObject, out sources);

        var offered = Light2DEditorUtility.CollectCommonSourceNames(serializedObject.targetObjects, sources.GetSourceNames());

        Assert.AreEqual(-1, Provider2DTestFixtures.IndexOfName(offered, Provider2DTestFixtures.kLightProviderName),
            "a provider only one light can use was offered for both");
        Assert.GreaterOrEqual(Provider2DTestFixtures.IndexOfName(offered, "Global"), 0,
            "the built-in types are available to every light");
    }

    // Mirrors ShadowCaster2DEditor.OnInspectorGUI up to the point the drawer draws its dropdown.
    static List<GUIContent> PrepareShadowSources(UnityEngine.Object[] targets, List<GUIContent[]> guiContent)
    {
        var additionalSources = new List<SelectionSource>
        {
            new Shadow2DSource_ShapeEditor(new GUIContent("Shape Editor"), int.MinValue),
        };

        for (int i = 0; i < targets.Length; i++)
        {
            var targetObj = new SerializedObject(targets[i]);
            var selectionSources = targetObj.FindProperty("m_SelectionSources");
            Shadow2DProviderSources.SetAdditionalSources(selectionSources, additionalSources);
        }

        GUIContent selectedContent;
        bool showMixedContent;
        return s_ShadowDrawer.CollectTargetSources(targets, guiContent, out selectedContent, out showMixedContent);
    }

    [Test]
    public void SingleShadowCaster_SelectingCustomProvider_AssignsProvider()
    {
        var casterA = NewShadowCaster("casterA");
        var targets = new UnityEngine.Object[] { casterA };

        var guiContent = new List<GUIContent[]>();
        var commonContent = PrepareShadowSources(targets, guiContent);

        int index = Provider2DTestFixtures.IndexOfName(commonContent, Provider2DTestFixtures.kShadowProviderName);
        Assert.GreaterOrEqual(index, 0, "custom shadow provider was not listed");

        s_ShadowDrawer.ApplySelection(targets, guiContent, commonContent[index]);

        Assert.AreEqual(ShadowCaster2D.ShadowCastingSources.ShapeProvider, casterA.shadowCastingSource);
        Assert.IsInstanceOf<Provider2DTestShadowProvider>(casterA.shadowShape2DProvider, "casterA provider");
        Assert.AreSame(casterA.gameObject, casterA.shadowShape2DComponent.gameObject, "casterA source component");
    }

    [Test]
    public void MultipleShadowCasters_SelectingCustomProvider_AssignsProviderToAll()
    {
        var casterA = NewShadowCaster("casterA");
        var casterB = NewShadowCaster("casterB");
        var targets = new UnityEngine.Object[] { casterA, casterB };

        var guiContent = new List<GUIContent[]>();
        var commonContent = PrepareShadowSources(targets, guiContent);

        int index = Provider2DTestFixtures.IndexOfName(commonContent, Provider2DTestFixtures.kShadowProviderName);
        Assert.GreaterOrEqual(index, 0, "custom shadow provider was not listed");

        s_ShadowDrawer.ApplySelection(targets, guiContent, commonContent[index]);

        Assert.AreEqual(ShadowCaster2D.ShadowCastingSources.ShapeProvider, casterA.shadowCastingSource, "casterA source");
        Assert.AreEqual(ShadowCaster2D.ShadowCastingSources.ShapeProvider, casterB.shadowCastingSource, "casterB source");
        Assert.IsInstanceOf<Provider2DTestShadowProvider>(casterA.shadowShape2DProvider, "casterA provider");
        Assert.IsInstanceOf<Provider2DTestShadowProvider>(casterB.shadowShape2DProvider, "casterB provider");

        // Each caster must read the SpriteRenderer on its own GameObject, not the first caster's.
        Assert.AreSame(casterA.gameObject, casterA.shadowShape2DComponent.gameObject, "casterA source component");
        Assert.AreSame(casterB.gameObject, casterB.shadowShape2DComponent.gameObject, "casterB source component");
    }

    // Casters whose GameObjects carry different components have different casting sources, so the
    // dropdown can only offer what they have in common.
    [Test]
    public void ShadowCastersWithDifferentComponents_OnlyOfferWhatTheyShare()
    {
        var spriteCaster = NewShadowCaster("spriteCaster");
        var colliderCaster = NewColliderShadowCaster("colliderCaster");
        var targets = new UnityEngine.Object[] { spriteCaster, colliderCaster };

        var guiContent = new List<GUIContent[]>();
        List<GUIContent> commonContent = null;
        Assert.DoesNotThrow(() => commonContent = PrepareShadowSources(targets, guiContent));

        Assert.AreEqual(-1, Provider2DTestFixtures.IndexOfName(commonContent, Provider2DTestFixtures.kShadowProviderName),
            "a provider only one caster can use was offered for both");

        int shapeEditorIndex = Provider2DTestFixtures.IndexOfName(commonContent, "Shape Editor");
        Assert.GreaterOrEqual(shapeEditorIndex, 0, "the shape editor is available to every caster");

        Assert.DoesNotThrow(() => s_ShadowDrawer.ApplySelection(targets, guiContent, commonContent[shapeEditorIndex]));
        Assert.AreEqual(ShadowCaster2D.ShadowCastingSources.ShapeEditor, spriteCaster.shadowCastingSource, "spriteCaster source");
        Assert.AreEqual(ShadowCaster2D.ShadowCastingSources.ShapeEditor, colliderCaster.shadowCastingSource, "colliderCaster source");
    }
}
