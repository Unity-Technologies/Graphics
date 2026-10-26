using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// A provider written against the older three-argument OnBeforeRender: if only the Camera
// overload were called it would silently push no shape, and its caster would keep drawing whatever
// geometry was serialized with it.
[Serializable]
class OlderSignatureShadowProvider : ShadowShape2DProvider
{
    public int olderCalls;

    public override bool IsRequiredComponentData(Component sourceComponent) { return sourceComponent is Provider2DTestMarker; }

    public override void OnBeforeRender(Component sourceComponent, Bounds worldCullingBounds, ShadowShape2D persistantShadowShape)
    {
        olderCalls++;
    }
}

[Serializable]
class CurrentSignatureShadowProvider : ShadowShape2DProvider
{
    public int currentCalls;

    public override bool IsRequiredComponentData(Component sourceComponent) { return sourceComponent is Provider2DTestMarker; }

    public override void OnBeforeRender(Camera camera, Component sourceComponent, Bounds worldCullingBounds, ShadowShape2D persistantShadowShape)
    {
        currentCalls++;
    }
}

// Overrides both, to pin down the order and the accepted cost of calling both.
[Serializable]
class BothSignaturesShadowProvider : ShadowShape2DProvider
{
    public string order = "";

    public override bool IsRequiredComponentData(Component sourceComponent) { return sourceComponent is Provider2DTestMarker; }

    public override void OnBeforeRender(Component sourceComponent, Bounds worldCullingBounds, ShadowShape2D persistantShadowShape)
    {
        order += "old;";
    }

    public override void OnBeforeRender(Camera camera, Component sourceComponent, Bounds worldCullingBounds, ShadowShape2D persistantShadowShape)
    {
        order += "new;";
    }
}

class ShadowProviderCallbackTests : Provider2DTestBase
{
    Component NewSourceComponent()
    {
        return NewGameObject("shadowSource").AddComponent<Provider2DTestMarker>();
    }

    [Test]
    public void ProviderOverridingOnlyTheOlderOverload_IsCalled()
    {
        var provider = new OlderSignatureShadowProvider();
        ShapeProviderUtility.CallOnBeforeRender(provider, NewSourceComponent(), new ShadowMesh2D(), default, null);

        Assert.AreEqual(1, provider.olderCalls, "a provider on the older signature was never called");
    }

    [Test]
    public void ProviderOverridingOnlyTheCurrentOverload_IsCalled()
    {
        var provider = new CurrentSignatureShadowProvider();
        ShapeProviderUtility.CallOnBeforeRender(provider, NewSourceComponent(), new ShadowMesh2D(), default, null);

        Assert.AreEqual(1, provider.currentCalls);
    }

    // Both run, older first, so a provider that has moved to the Camera signature sees the old one
    // land before it rather than after.
    [Test]
    public void ProviderOverridingBoth_GetsTheOlderOverloadFirst()
    {
        var provider = new BothSignaturesShadowProvider();
        ShapeProviderUtility.CallOnBeforeRender(provider, NewSourceComponent(), new ShadowMesh2D(), default, null);

        Assert.AreEqual("old;new;", provider.order);
    }

    [Test]
    public void ProviderOnAnInactiveGameObject_IsNotCalled()
    {
        var source = NewSourceComponent();
        source.gameObject.SetActive(false);

        var provider = new OlderSignatureShadowProvider();
        ShapeProviderUtility.CallOnBeforeRender(provider, source, new ShadowMesh2D(), default, null);

        Assert.AreEqual(0, provider.olderCalls);
    }
}
