# Graph Target

A Target determines the end point compatibility of a shader you generate using Shader Graph. You can select Targets for each Shader Graph asset, and use the [Graph Settings Menu](Graph-Settings-Menu.md) to change the Targets.

![image](images/GraphSettings_Menu.png)

Targets hold information such as the required generation format, and variables that allow compatibility with different render pipelines or integration features like [Visual Effect Graph](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest). You can select any number of Targets for each Shader Graph asset. If a Target you select isn't compatible with other Targets you've already selected, an error message that explains the problem appears.

Target Settings are specific to each Target, and can vary between assets depending on which Targets you've selected. Be aware that Universal Render Pipeline (URP) Target Settings and High Definition Render Pipeline (HDRP) Target Settings might change in future versions.

Typically, each Target you select generates a valid subshader from the graph. For example, a Shader Graph asset with both URP and HDRP Targets will generate two subshaders. When you use a graph that targets multiple render pipelines, you must reimport the Shader Graph asset if you change the active render pipeline. This updates the Material Inspector for any Materials that use your graph.
