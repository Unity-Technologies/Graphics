# Get Skinned Mesh World Root Transform

Menu Path: **Operator > Sampling > Get Skinned Mesh World Root Transform**

Use the **Get Skinned Mesh World Root Transform** Operator to get the transform of a root bone in a skinned mesh, relative to the world.

The following use the settings in this Operators:

- The [Sample Skinned Mesh](Operator-SampleMesh.md) Operator. Refer to the **Skinned Transform** setting.
- The [Set Position (Skinned Mesh)](Block-SetPosition(Mesh).md) Block. Refer to the **Skinned Transform** setting.

## Operator settings

| **Property**  | **Type** | **Description**                                              |
| ------------- | -------- | ------------------------------------------------------------ |
| **Transform** | Enum     | **(Inspector)** Specify which transform to output from the Skinned Mesh Renderer.<ul><li>**WorldRootBoneTransform**: Output the world transform of the root bone. The Operator becomes a [Get Skinned Mesh World Root Transform](Operator-SkinnedWorldTransform.md) Operator.</li><li>**LocalRootBoneTransform**: Output the transform of the root bone relative to the Skinned Mesh Renderer. The Operator becomes a [Get Skinned Mesh Local Root Transform](Operator-SkinnedLocalTransform.md) Operator.</li></ul>|
| **Frame**     | Enum     | **(Inspector)** Specify which frame to get the transform from.<ul><li>**Current**: Get the transform from the current frame.</li><li>**Previous**: Get the transform from the previous frame. The [Sample Skinned Mesh](Operator-SampleMesh.md) Operator uses this setting to calculate velocity correctly.</li></ul> |

### Operator properties

| **Input**                 | **Type**              | **Description**                                              |
| ------------------------- | --------------------- | ------------------------------------------------------------ |
| **Skinned Mesh Renderer** | Skinned Mesh Renderer | Specify the Skinned Mesh Renderer to sample. This is a reference to a component within the scene. To assign a Skinned Mesh Renderer to this port, create a Skinned Mesh Renderer property in the [Blackboard](Blackboard.md) and expose it.<br/><br/>This property only appears if you set **Source** to **Skinned Mesh Renderer** |

| **Output**    | **Type**  | **Description**                                              |
| ------------- | --------- | ------------------------------------------------------------ |
| **Transform** | Transform | The transform specified in the **Transform** Operator setting. The output is in world space if you set **Transform** to **WorldRootBoneTransform**, or in local space if you set **Transform** to **LocalRootBoneTransform**. |
