# Frequently asked questions
This section answers some frequently asked questions about the Universal Render Pipeline (UniversalRP). These questions come from the [General Graphics](https://forum.unity.com/forums/general-graphics.76/) section on our forums, from the [Unity Discord](https://discord.gg/unity) channel, and from our support teams.

For information about the High Definition Render Pipeline (HDRP), please see the [HDRP documentation](https://github.com/Unity-Technologies/ScriptableRenderPipeline/wiki/High-Definition-Render-Pipeline-overview).

## Can I use UniversalRP and HDRP at the same time?
No. They're both built with the Scriptable Render Pipeline (SRP), but their render paths and light models are different.
## Can I convert from one pipeline to the other?
You can convert from the Built-in Unity render pipeline to UniversalRP. To do so, you'll have to re-write your Assets and redo the lighting in your game or app. You can use our upgrader to [upgrade Built-in Shaders to the UniversalRP Shaders](upgrading-your-shaders.md). For custom Shaders, you'll have to upgrade them manually. 

You _should not_ swap pipeline Assets from one pipeline to another at run time, and there's no upgrader between UniversalRP and HDRP. 

## How do I update the Universal Render Pipeline package?
You should update via the Package Manager. In the Unity Editor, go to __Unity__ > __Window__ > __Package Manager__, and find the __Universal RP__ package.

If you’ve added SRP code or Shader Graph manually via Github, make sure to upgrade them to the same package version as UniversalRP in your manifest file.


## Where has Dynamic Batching gone?

The Dynamic Batching checkbox has moved from the __Player Settings__ to the [__Universal Render Pipeline Asset__](universalrp-asset.md).

## How do I enable Double Sided Global Illumination in the Editor?

In the Material Inspector, find __Render Face__, and select __Both__. This means that both sides of your geometry contribute to global illumination, because UniversalRP doesn’t cull either side.
## Is this render pipeline usable for desktop apps and games?

Yes. The graphics quality and performance is scalable across platforms, so you can create apps for PCs and consoles as well as mobile devices.


## A certain feature from the Built-in render pipeline is not supported in UniversalRP. Will UniversalRP support it?

To see which features UniversalRP currently supports, check the [comparison table](universalrp-builtin-feature-comparison.md). 
UniversalRP will not support features marked as `Not Supported`. If the status of a feature is left blank in the table, this means that we intend to support the feature at some point.


## Does UniversalRP support a Deferred Renderer?
Not yet. Our goal is to add support for an optimized deferred renderer in 2019.3.
## Does UniversalRP have a public roadmap?
Yes. You can [check it here](https://portal.productboard.com/8ufdwj59ehtmsvxenjumxo82/tabs/3-Universal-render-pipeline). You can add suggestions as well. To do so, you’ll have to enter your email address, but you won’t have to make an account.

## Will UniversalRP be in LTS for 2019.4? 
Yes.

## I’ve found a bug. How do I report it?
You can open bugs by using the [bug reporter system](https://unity3d.com/unity/qa/bug-reporting). UniversalRP bugs go through the same process as all other Unity bugs. You can also check the active list of bugs for UniversalRP in the [issue tracker](https://issuetracker.unity3d.com/product/unity/issues?utf8=%E2%9C%93&package=2&unity_version=&status=1&category=&view=hottest). 

## I have an existing Project. How do I convert it from the Built-in render pipeline to UniversalRP?
Check this upgrade guide on [installing an SRP into an existing Project](installing-universalrp-into-an-existing-project.md). 

## I’ve upgraded my Project from the Built-in render pipeline to UniversalRP, but it’s not running faster. Why?

UniversalRP and the Built-in render pipeline (RP) have different quality settings. While the Built-in RP configures many settings in different places like the Quality Settings, Graphics Settings, and Player Settings, all UniversalRP settings are stored in the UniversalRP Asset. The first thing to do is to check whether your UniversalRP Asset settings match the settings your Built-in render pipeline Project. For example, if you disabled MSAA or HDR in your Built-in render pipeline Project, make sure they are disabled in the UniversalRP Asset in your UniversalRP Project. For advice on configuring UniversalRP Assets, see documentation on the [UniversalRP Asset](universalrp-asset.md).

Also, make sure you are doing a fair comparison in terms of renderers. For this release, UniversalRP only supports a forward renderer, so make sure your Built-in render pipeline Project is using the forward renderer as well. 

If, after comparing the settings, you still experience worse performance with UniversalRP, please [open a bug report](https://unity3d.com/unity/qa/bug-reporting) and attach your Project. 
## UniversalRP doesn’t run on device X or platform Y. Is this expected?

No. Please [open a bug report](https://unity3d.com/unity/qa/bug-reporting). 

## My Project takes a long time to build. Is this expected?
We are looking into how to strip Shader keywords more aggressively. You can help the Shader stripper by disabling features you don’t require for your game in the UniversalRP Asset. For more information on settings that affect shader variants and build time, see the [shader stripping documentation](shader-stripping.md). 

## Is post-processing supported in UniversalRP?
Some post-processing version 2 (PPv2) features are supported in UniversalRP. To see which features are supported, see [comparison of features in UniversalRP and the Built-in render pipeline](universalrp-builtin-feature-comparison.md). PPv2 doesn’t support mobile VR.

We are working on many optimizations for post-processing and mobile. 

## I can’t set camera clear flags with UniversalRP. Why?

We’ve deprecated camera clear flags in UniversalRP. Instead, you can set the Background Type in the Camera Inspector. 
We’ve done this because the clear flags `Depth Only` and `Don’t Care` from the Built-in render pipeline has inherent performance pitfalls. The clear flags were used for camera stacking, where one camera depends on the results of a previous camera. This is bad for performance, as it executes culling multiple times and increases bandwidth. Bandwidth cost is especially important for mobile games.

For these reasons, we're working on a solution where you can add a render pass with custom camera matrices and FOV. This way, we can provide an optimized workflow instead of creating a Camera object. We plan to expose this custom render pass in a future UniversalRP package.

## What rendering space does UniversalRP work in?

By default, UniversalRP uses a linear color space while rendering. You can also use a gamma color space, which is non-linear. To do so, toggle it in the Player Settings.

## How do I extend UniversalRP with scriptable render pass?

To create a scriptable render pass, you have to create a `ScriptableRendererFeature` script. This is because the scriptable render feature is a container that can have the pass in it. To create the scriptable render feature in the Editor, click on **Asset** > **Create** > **Rendering** > **Universal Render Pipeline** > **Renderer Feature**.

##Why does Feature X not work in the UniversalRP Asset when I use the 2D Renderer?

The 2D Renderer is an experimental feature. When it's enabled (menu: **Graphics Settings** > add the 2D Renderer Asset under **Scriptable Render Pipeline Settings**), it disables the options for 3D rendering. You can still configure the options, but they have no affect on your final product. We're working on a solution so that you can't configure them when the 2D Renderer is enabled.

