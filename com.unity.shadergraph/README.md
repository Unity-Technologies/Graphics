# Shader Graph

![Screenshot of Shader Graph](https://forum.unity.com/proxy.php?image=https%3A%2F%2Flh5.googleusercontent.com%2FUhB18UehZFk8jMo_2V3GW-hD2wARAcQWu6FGzcUvTByHNc51w_mLZBvB6Re5GcTHJQlPHOtzi14wUPvi_yUgWTAp3-HZU463JmxL9NSjJS5yALBSAj1Bdk8yL8zXkRVe-0crKz5F&hash=49458e7088a5be61b288167af65b6faf "Shader Graph")

A Shader Graph enables you to build shaders visually. Instead of hand writing code you create and connect nodes in a graph network. The graph framework gives instant feedback on the changes, and it’s simple enough that new users can become involved in shader creation.

## Disclaimer

This repository is under active development. Everything is subject to change. The `master` branch is our current development branch and may not work on the latest publicly available version of Unity. Unless you intend to modify Shader Graph or want to try out the very latest and unsupported features, we recommend that you acquire Shader Graph through the Unity Package Manager: 

1. Open a Unity project. 
2. Open the **Package Manager** window (**Window** &gt; **Package Manager**).
3. In the **Package Manager** window, in the **Packages** menu, select **Unity Registry**. 
4. Select either **Universal RP** or **High Definition RP**, depending on your project needs.
    Unity installs your chosen render pipeline and Shader Graph. Shader Graph isn't compatible with Unity's built-in render pipeline. 

## Instructions

If you want to try out the latest features, we recommend using Shader Graph through the [SRP repository](https://github.com/Unity-Technologies/Graphics), which has the Shader Graph submodule setup as a submodule. Otherwise you will not have any Master Node backends available and thus your shaders will be pink. This also ensures that you get a compatible set of render pipeline and Shader Graph versions.
