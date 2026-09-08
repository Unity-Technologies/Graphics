using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Marker the test providers key off. Without it they would match on Transform (the Provider2D
// default) or on SpriteRenderer, and so would appear in the source dropdown of every Light2D and
// ShadowCaster2D in this shared graphics-test project.
class Provider2DTestMarker : MonoBehaviour
{
}

// Providers used only by these tests. They are discovered through TypeCache, so simply existing in
// an editor assembly is enough for them to show up in the Light2D / ShadowCaster2D source dropdowns.
[Serializable]
class Provider2DTestLightProvider : Light2DProvider
{
    public float testValue = 1.0f;
    public override GUIContent ProviderName() { return new GUIContent(Provider2DTestFixtures.kLightProviderName); }
    internal override bool Internal_IsRequiredComponentData(Component sourceComponent) { return sourceComponent is Provider2DTestMarker; }
}

[Serializable]
class Provider2DTestShadowProvider : ShadowShape2DProvider
{
    public float testValue = 1.0f;
    public override GUIContent ProviderName(string componentName) { return new GUIContent(Provider2DTestFixtures.kShadowProviderName); }
    public override bool IsRequiredComponentData(Component sourceComponent) { return sourceComponent is Provider2DTestMarker; }
}

static class Provider2DTestFixtures
{
    public const string kLightProviderName = "Provider2DTest Light";
    public const string kShadowProviderName = "Provider2DTest Shadow";

    public static int IndexOfName(IList<GUIContent> names, string wanted)
    {
        for (int i = 0; i < names.Count; i++)
            if (names[i] != null && names[i].text == wanted)
                return i;
        return -1;
    }

    public static List<SelectionSource> BuiltInLightSources()
    {
        return new List<SelectionSource>
        {
            new Light2DSource_BuiltIn(new GUIContent("Spot"), Light2D.LightType.Point, 0),
            new Light2DSource_BuiltIn(new GUIContent("Freeform"), Light2D.LightType.Freeform, 0),
            new Light2DSource_BuiltIn(new GUIContent("Sprite"), Light2D.LightType.Sprite, 0),
            new Light2DSource_BuiltIn(new GUIContent("Global"), Light2D.LightType.Global, 0),
        };
    }

    /// <summary>
    /// Mirrors what Light2DEditorUtility.DrawLightTypePopup does before it draws the popup, and returns
    /// the index of the currently selected source.
    /// </summary>
    public static int PrepareLightSources(SerializedObject serializedObject, out Light2DProviderSources sources)
    {
        serializedObject.Update();

        var selectionSources = serializedObject.FindProperty("m_SelectionSources");
        sources = selectionSources.boxedValue as Light2DProviderSources;
        Assert.IsNotNull(sources, "m_SelectionSources was null");

        var light2D = serializedObject.targetObject as Light2D;
        Provider2DSources<Light2DProvider, Light2DProviderSource>.RefreshSources(sources, light2D.gameObject, 0);

        var additional = sources.GetAdditionalSources();
        additional.Clear();
        foreach (var source in BuiltInLightSources())
            additional.Add(source);

        return Provider2DSources<Light2DProvider, Light2DProviderSource>.RefreshSources(sources, light2D.gameObject, 0);
    }
}

abstract class Provider2DTestBase
{
    readonly List<GameObject> m_Created = new List<GameObject>();

    [TearDown]
    public void DestroyCreatedGameObjects()
    {
        foreach (var go in m_Created)
            if (go != null)
                UnityEngine.Object.DestroyImmediate(go);
        m_Created.Clear();
    }

    protected GameObject NewGameObject(string name)
    {
        var go = new GameObject(name);
        m_Created.Add(go);
        return go;
    }

    /// <summary>A light whose GameObject carries the marker, so the test light provider is offered for it.</summary>
    protected Light2D NewLight(string name)
    {
        var go = NewGameObject(name);
        go.AddComponent<Provider2DTestMarker>();
        return go.AddComponent<Light2D>();
    }

    /// <summary>A light with no marker, so only the built-in light types are offered for it.</summary>
    protected Light2D NewLightWithoutProvider(string name)
    {
        return NewGameObject(name).AddComponent<Light2D>();
    }

    protected ShadowCaster2D NewShadowCaster(string name)
    {
        var go = NewGameObject(name);
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<Provider2DTestMarker>();
        return go.AddComponent<ShadowCaster2D>();
    }

    protected ShadowCaster2D NewColliderShadowCaster(string name)
    {
        var go = NewGameObject(name);
        go.AddComponent<BoxCollider2D>();
        return go.AddComponent<ShadowCaster2D>();
    }
}
