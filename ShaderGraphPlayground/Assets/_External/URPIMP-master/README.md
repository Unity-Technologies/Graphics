# URPIMP
IMP ported to URP

Tested on Unity 2021.2.11f1

## Features
- Better and ported baker shaders (originals were on old Unity CG)
- Better dilate (more precise but also WAY slower to compute - O(n^2) GPU exploding shader)
- Better padding handling
- Support for all custom shaders (in theory)
- No use of camera component (now uses GL and Graphics calls)
- No longer a window tool (it's a monobehaviour now)
- No unity billboard (was kinda useless)
- Currently bakes: Albedo + Normal + Depth

## Notes
- Well I pretty much have rewritten almost all of the C# side of the tool
- Currently the system uses Unity's GBuffer shader implementation to do all the baking
- So in theory, it should support all custom shaders created with Shader Graph, as long as those properly compile to URP's 'Deferred path'
- Since it relies on GBuffer pass, it could easily break with future versions of URP
- Obviously there is no support for skinned meshes

## Images
![sussy trees](trees.gif)
![sussy objects](wireframe.gif)

## License
[CC0](https://creativecommons.org/share-your-work/public-domain/cc0/)
*Pine004 example asset is from the free Unity Book of the Dead Environment project
