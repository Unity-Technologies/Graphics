# Shader Graph

![Screenshot of Shader Graph](https://forum.unity.com/proxy.php?image=https%3A%2F%2Flh5.googleusercontent.com%2FUhB18UehZFk8jMo_2V3GW-hD2wARAcQWu6FGzcUvTByHNc51w_mLZBvB6Re5GcTHJQlPHOtzi14wUPvi_yUgWTAp3-HZU463JmxL9NSjJS5yALBSAj1Bdk8yL8zXkRVe-0crKz5F&hash=49458e7088a5be61b288167af65b6faf "Shader Graph")

A Shader Graph enables you to build shaders visually. Instead of hand writing code you create and connect nodes in a graph network. The graph framework gives instant feedback on the changes, and it’s simple enough that new users can become involved in shader creation.

Unless you intend to modify Shader Graph or want to try out the latest and unsupported features, Unity recommends that you install Shader Graph through the Unity Package Manager:

1. Open a Unity project.
2. Open the **Package Manager** window (**Window** &gt; **Package Manager**).
3. In the **Package Manager** window, in the **Packages** menu, select **Unity Registry**.
4. Do one of the following, based on your project needs:
    - To use Shader Graph and the [Universal Render Pipeline (URP)](https://docs.unity3d.com/Manual/urp/urp-introduction.html) in your project, select **Universal RP**.
    - To use Shader Graph and the [High Definition Render Pipeline (HDRP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest), select **High Definition RP**.
    - To use Shader Graph with Unity's [Built-In Render Pipeline](https://docs.unity3d.com/2020.3/Documentation/Manual/built-in-render-pipeline.html), select **Shader Graph**.

Unity recommends using Shader Graph with URP or HDRP.

## Instructions

If you want to try out the latest features, we recommend obtaining the most recent version of Shader Graph through the [Unity Scriptable Render Pipeline (SRP) repository](https://github.com/Unity-Technologies/Graphics), which includes the Shader Graph project as a Git submodule. For more information on Git submodules, see [Git's documentation on Submodules](https://git-scm.com/book/en/v2/Git-Tools-Submodules).

If you don't install Shader Graph through the SRP repository, you don't have any Master Node backends available and your shaders are invalid. Invalid shaders appear pink in the Unity Editor. Installing through the repository also ensures you have a compatible set of render pipeline and Shader Graph versions.

For more detailed instructions for installing from the repository, see the [SRP repository's README](https://github.com/Unity-Technologies/Graphics/blob/master/README.md).
