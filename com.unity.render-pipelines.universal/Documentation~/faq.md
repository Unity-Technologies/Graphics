# Frequently asked questions (FAQ)
This section answers some frequently asked questions about the Universal Render Pipeline (URP). These questions come from the [General Graphics](https://forum.unity.com/forums/general-graphics.76/) section on our forums, from the [Unity Discord](https://discord.gg/unity) channel, and from our support teams.

For information about the High Definition Render Pipeline (HDRP), see the [HDRP documentation](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html).

## Can I use URP and HDRP at the same time?
No. They're both built with the Scriptable Render Pipeline (SRP), but their render paths and light models are different.

## Can I convert from one pipeline to the other?
You can convert from the Built-in Render Pipeline to URP. To do so, you'll have to re-write your Assets and redo the lighting in your game or app. See this upgrade guide on [installing URP into an existing Project](InstallURPIntoAProject).

You can use our upgrader to [upgrade Built-in Shaders to the URP Shaders](upgrading-your-shaders.md). For custom Shaders, you'll have to upgrade them manually.

You _should not_ swap pipeline Assets from one pipeline to another at run time, and there's no upgrader between URP and HDRP.

## How do I update the Universal Render Pipeline package?
You should update via the Package Manager. In the Unity Editor, go to __Unity__ > __Window__ > __Package Manager__, and find the __Universal RP__ package.

If you’ve added SRP code or Shader Graph manually via Github, make sure to upgrade them to the same package version as URP in your manifest file.

## Where has Dynamic Batching gone?

The Dynamic Batching checkbox has moved from the __Player Settings__ to the [__URP Asset__](universalrp-asset.md).

## How do I enable Double Sided Global Illumination in the Editor?

In the Material Inspector, find __Render Face__, and select __Both__. This means that both sides of your geometry contribute to global illumination, because URP doesn’t cull either side.
## Is this render pipeline usable for desktop apps and games?

Yes. The graphics quality and performance is scalable across platforms, so you can create apps for PCs and consoles as well as mobile devices.

## A certain feature from the Built-in Render Pipeline is not supported in URP. Will URP support it?

To see which features URP currently supports, check the [comparison table](universalrp-builtin-feature-comparison.md).
URP will not support features marked as `Not Supported`.

## Does URP support a Deferred Renderer?
Not yet. We are working on adding support for a Deferred Renderer.

## Does URP have a public roadmap?
Yes. You can [check it here](https://portal.productboard.com/8ufdwj59ehtmsvxenjumxo82/tabs/3-Universal-render-pipeline). You can add suggestions as well. To do so, you’ll have to enter your email address, but you won’t have to make an account.

## Will URP be in LTS for Unity 2019.4?
Yes.

## I’ve found a bug. How do I report it?
You can open bugs by using the [bug reporter system](https://unity3d.com/unity/qa/bug-reporting). URP bugs go through the same process as all other Unity bugs. You can also check the active list of bugs for URP in the [issue tracker](https://issuetracker.unity3d.com/product/unity/issues?utf8=%E2%9C%93&package=2&unity_version=&status=1&category=&view=hottest).

## I’ve upgraded my Project from the Built-in render pipeline to URP, but it’s not running faster. Why?

URP and the Built-in Render Pipeline have different quality settings. While the Built-in Render Pipeline configures many settings in different places like the Quality Settings, Graphics Settings, and Player Settings, all URP settings are stored in the URP Asset. The first thing to do is to check whether your URP Asset settings match the settings your Built-in render pipeline Project. For example, if you disabled MSAA or HDR in your Built-in render pipeline Project, make sure they are disabled in the URP Asset in your URP Project. For advice on configuring URP Assets, see documentation on the [URP Asset](universalrp-asset.md).

Also, make sure you are doing a fair comparison in terms of renderers. For this release, URP only supports a forward renderer, so make sure your Built-in render pipeline Project is using the forward renderer as well.

If, after comparing the settings, you still experience worse performance with URP, please [open a bug report](https://unity3d.com/unity/qa/bug-reporting) and attach your Project.
## URP doesn’t run on device X or platform Y. Is this expected?

No. Please [open a bug report](https://unity3d.com/unity/qa/bug-reporting).

## My Project takes a long time to build. Is this expected?
We are looking into how to strip Shader keywords more aggressively. You can help the Shader stripper by disabling features you don’t require for your game in the URP Asset. For more information on settings that affect shader variants and build time, see the [shader stripping documentation](shader-stripping.md).

## How do I set Camera clear flags in URP?

You can set the Background Type in the Camera Inspector to control how a Camera's color buffer is initialized.

## What rendering space does URP work in?

By default, URP uses a linear color space while rendering. You can also use a gamma color space, which is non-linear. To do so, toggle it in the Player Settings.

## How do I extend URP with scriptable render pass?

To create a scriptable render pass, you have to create a `ScriptableRendererFeature` script. This is because the scriptable render feature is a container that can have the pass in it. To create the scriptable render feature in the Editor, click on **Asset** > **Create** > **Rendering** > **Universal Render Pipeline** > **Renderer Feature**.
