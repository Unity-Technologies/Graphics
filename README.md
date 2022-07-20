## NOTE: We have migrated reported issues to FogBugz. You can only log further issues via the Unity bug tracker. To see how, read [this](https://unity3d.com/unity/qa/bug-reporting).

## NOTE 2: We are rolling out changes to how we develop the Graphics Packages. Development will move to an internal repo and changes will be mirrored to this public repo. You will continue to see changes at the PR level and pull in changes from this public repo. You can find more information and track our progress [here](https://forum.unity.com/threads/important-upcoming-changes-to-the-graphics-repository.1239826/).

## NOTE 3: Some folders have been moved. Read more [here](./README-FOLDER-CHANGES.md) on how to merge your custom branches with the new folder layout.

# Unity Scriptable Render Pipeline
The Scriptable Render Pipeline (SRP) is a Unity feature designed to give artists and developers the tools they need to create modern, high-fidelity graphics in Unity. Unity provides two pre-built Scriptable Render Pipelines:

* The Universal Render Pipeline (URP) for use on all platforms.
* The High Definition Render Pipeline (HDRP) for use on compute shader compatible platforms.

Unity is committed to an open and transparent development process for SRP and the pre-built Render Pipelines. This means that you can browse this repository to see what features are currently in development.

For more information about the packages in this repository, see the following:

* [Scriptable Render Pipeline Core](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@latest/index.html)
* [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html)
* [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html)
* [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html)
* [Visual Effect Graph](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest/index.html)

## Branches and package releases

The packages in this repository are distributed as [Core packages](https://docs.unity3d.com/Manual/pack-core.html) in the Unity editor.
The package vendoring process happens multiple times in each Unity release. The vendoring happens from the latest changeset of the release branch for each Unity release.
A tag is generated on the changeset used to vendor a specific Unity release.

Release branches are defined as follows:
- **master** branch is used for main developement and it always maps to the latest **Unity Alpha** release.
- **{unity-version}/staging** maps to beta and released Unity versions. f.ex, **2021.1/staging** maps to Unity 2021.1.
- **{package-major-version}.x.x/release** is used for Unity 2020.x and below. f.ex, **10.x.x/release** maps to Unity 2020.3 LTS.

If you need to find if a specific changeset is included in a specific Unity version, you can search tags for the Unity version.
On GitHub, you can do that by clicking on the **Branch** drop-down then clicking the **Tags** tab. Typing `2021.2` will list all changesets tagged to each Unity version.

## Modifying package source

You can download and install the packages of this repositories on your Unity project and modify the package source code.
You can do that by one of the following methods:

1. Clone this repository in any folder on your computer. [Install them as local packages](https://docs.unity3d.com/Manual/upm-ui-local.html) into your project.
2. Clone this repository inside a `Packages` folder in your Unity project.

### Cloning the repository using the GitHub Desktop App:

1. Open the GitHub Desktop App and click **File > Clone repository**.
2. Click the **URL** tab and enter the following URL: https://github.com/Unity-Technologies/Graphics.
3. Click the **Choose…** button and navigate to your Unity Project’s base folder.
4. Click the **Clone** button.

Make sure you have Git LFS extension installed as that's required.

After you clone the repository, open your console application of choice in the Graphics folder and run the following console command:

`\> git checkout 2021.1.16f1.2801 (or the latest tag)`

<a name="ConsoleCommands"></a>

### Cloning the repository using console commands:

Open your console application of choice and run the following console commands:

```
\> cd <Path to your Unity project>

\> git clone https://github.com/Unity-Technologies/Graphics

\> cd Graphics

\>  git checkout 2021.1.16f1.2801 (or the latest tag)
```

## Sample Scenes in Graphics

Unity provides sample Scenes to use with SRP. You can find these Scenes in the [Graphics GitHub repository](https://github.com/Unity-Technologies/Graphics). To add the Scenes to your Project, clone the repository into your Project's Assets folder.


## Package versions on Unity 2020.3 LTS and below

On Unity 2020.3 LTS and below, the packages in this repository were not Core packages. Instead they were regular packages and different versions could be installed to different versions of Unity.
The compatibility of Unity versions and package versions were as follows:

- **Unity 2023.1 is compatible with SRP versions 15.x.x**
- **Unity 2022.2/3 is compatible with SRP versions 14.x.x**
- **Unity 2022.1 is compatible with SRP versions 13.x.x**
- **Unity 2021.2/3 is compatible with SRP versions 12.x.x**
- **Unity 2021.1 is compatible with SRP versions 11.x.x**
- **Unity 2020.2 is compatible with SRP versions 10.x.x**
- **Unity 2020.1 is compatible with SRP versions 8.x.x**
- **Unity 2019.3 is compatible with SRP versions 7.x.x**
- **Unity 2019.2 is compatible with SRP versions 6.x.x**
- **Unity 2019.1 is compatible with SRP vertsios 5.x.x**

The above list is a guideline for major versions of SRP, but there are often multiple minor versions that you can use for a certain version of Unity. To determine which minor versions of SRP you can use:

1. In your Unity Project, open the Package Manager window (menu: **Window > Package Manager**).
2. In the list of packages, find **Core RP Library**. To find this package in older versions of Unity, you may need to expose preview packages. To do this, click the **Advanced** button at the top of the window then, in the context menu, click **Show preview packages**.
3. Click the drop-down arrow to the left of the package entry then click **See all versions**. This shows a list that contains every package version compatible with your version of Unity.

After you decide which version of SRP to use:

1. Go to the [Unity Graphics repository](https://github.com/Unity-Technologies/Graphics).
2. Click the **Branch** drop-down then click the **Tags** tab.
3. Find the tag that corresponds to the version of SRP you want to use. When you clone the repository, you use this tag to check out the correct branch.
