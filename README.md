# Unity Scriptable Render Pipeline testbed

**NOTE**: this is a testbed for a Unity feature that has not shipped yet! The latest commits in this project does not work
with any public Unity version, and things in it might and will be broken.

"Scriptable Render Pipelines" is a potential future Unity feature, think "Command Buffers, take two". We plan to ship the feature, and a
new modern built-in rendering pipeline with it. For now you can look around if you're _really_ curious, but like said above, this is
not useful for any public Unity version yet.

There's a more detailed overview document here: [ScriptableRenderPipeline google doc](https://docs.google.com/document/d/1e2jkr_-v5iaZRuHdnMrSv978LuJKYZhsIYnrDkNAuvQ/edit?usp=sharing)

Did we mention it's a very WIP, no promises, may or might not ship feature, anything and everything in it can change? It totally is.

## How to use the latest version
The repository no longer consists of a complete Unity project, but rather
assumes to be put inside a sub-folder of the `Assets\` folder of an existing
Unity project. Make sure that your project uses linear color space
(_Edit > Project Settings > Player_).

Perform the following instructions to get a working copy of SRP:
```
> cd <Path to your Unity project>/Assets
> git clone https://github.com/Unity-Technologies/ScriptableRenderPipeline
> cd ScriptableRenderPipeline
> git submodule update --init --recursive --remote
```

## For Unity above 2017.1 beta users
SRP depends on PostProcessing submodule. Perform the following instructions to get a working copy of SRP:
```
> git clone https://github.com/Unity-Technologies/ScriptableRenderPipeline
> cd ScriptableRenderPipeline
> git checkout unity-2017.1b5 (or the latest tag)
> git submodule update --init --recursive --remote
```

## For HDRenderPipeline:

1. Download Unity version compatible with Github release (https://github.com/Unity-Technologies/ScriptableRenderPipeline/releases)
2. Launch
3. Create a new Unity project
4. Set `Color Space` to `Linear` in Player settings, Set Antialiasing to disable in Quality settings for all configuration (Fantastic and High), Set Anisotropic Textures to "Per Textures"
5. Close Unity
6. Execute the following commands (or use GitHub interface (ask us)):
```
> cd <Path to your Unity project>/Assets
> git clone https://github.com/Unity-Technologies/ScriptableRenderPipeline
> cd ScriptableRenderPipeline
> git submodule update --init --recursive --remote   (This is to get the PostProcessing folder)
```
7. Re-open the project
8. In Graphic Settings, for render pipeline, setup the HDRenderPipelineAsset

Advice: It is recommended to make a copy of HDRenderPipelineAsset outside of the ScriptableRenderPipeline, so settings are not lost when merging. And setup this new created HDRenderPipelineAsset in GraphicSettings


## For Unity 5.6 beta users

* Unity 5.6 **beta 5-7** should use an older revision of this project, [tagged unity-5.6.0b5](../../releases/tag/unity-5.6.0b5) (commit `2209522d` on 2016 Dec 14).
  "BasicRenderLoopScene" scene is the basic example, need to pick basic render pipeline in Graphics Settings to use it.
  All the other scenes and render pipelines may or might not work. Use of Windows/DX11 is preferred.
* Unity 5.6 **beta 1-4** should use an older revision of this project, [tagged unity-5.6.0b1](../../releases/tag/unity-5.6.0b1) (commit `acc230b` on 2016 Nov 23).
  "BasicRenderLoopScene" scene is the basic example, with the scriptable render pipeline defaulting to off; enable it by enabling the component on the camera.
  All the other scenes may or might not work. Use of Windows/DX11 is preferred.
