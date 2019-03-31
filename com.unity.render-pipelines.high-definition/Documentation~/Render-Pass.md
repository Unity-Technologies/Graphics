# Render Pass

The Render pass option controls when an object is rendered during the frame. Available render passes depends on the shader and the surface type. 

| **Property**           | **Description**                                              |
| ---------------------- | ------------------------------------------------------------ |
| **Default**            | Object will be rendered with all other objects of the same [Surface Type](Surface-Type.html). |
| **Before refraction**  | Transparent only. Object will be rendered before the refraction pass. |
| **Low resolution**     | Transparent only. Object will be rendered after regular transparent objects in half resolution. |
| **After post-process** | Unlit shader only. Object will be rendered after all post processes. |

#### Note on After post-process Render Pass:

The After post-process pass comes with a few constraints: When TAA is enabled, objects using this pass will not be able to benefit from the depth buffer for occlusion. When TAA is disabled, objects can be occluded normally.

