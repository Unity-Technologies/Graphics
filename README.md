# Unity Scriptable Render Pipeline testbed

**NOTE**: this is a testbed for a Unity feature that has not shipped yet! The latest commits in this project does not work
with any public Unity version, and things in it might and will be broken.

"Scriptable Render Pipelines" is a potential future Unity feature, think "Command Buffers, take two". We plan to ship the feature, and a
new modern built-in rendering pipeline with it. For now you can look around if you're _really_ curious, but like said above, this is
not useful for any public Unity version yet.

There's a more detailed overview document here: [ScriptableRenderLoop google doc](https://docs.google.com/document/d/1e2jkr_-v5iaZRuHdnMrSv978LuJKYZhsIYnrDkNAuvQ/edit?usp=sharing)

Did we mention it's a very WIP, no promises, may or might not ship feature, anything and everything in it can change? It totally is.

## For Unity 2017.1 beta users
SRP depends on PostProcessing submodule. Perform the following instructions to get a working copy of SRP:
* git clone https://github.com/Unity-Technologies/ScriptableRenderLoop
* git checkout unity-2017.1b5 (or the latest tag)
* git submodule update --init --recursive --remote

## For Unity 5.6 beta users

* Unity 5.6 **beta 5-7** should use an older revision of this project, [tagged unity-5.6.0b5](../../releases/tag/unity-5.6.0b5) (commit `2209522d` on 2016 Dec 14).
  "BasicRenderLoopScene" scene is the basic example, need to pick basic render pipeline in Graphics Settings to use it.
  All the other scenes and render pipelines may or might not work. Use of Windows/DX11 is preferred.
* Unity 5.6 **beta 1-4** should use an older revision of this project, [tagged unity-5.6.0b1](../../releases/tag/unity-5.6.0b1) (commit `acc230b` on 2016 Nov 23).
  "BasicRenderLoopScene" scene is the basic example, with the scriptable render pipeline defaulting to off; enable it by enabling the component on the camera.
  All the other scenes may or might not work. Use of Windows/DX11 is preferred.
