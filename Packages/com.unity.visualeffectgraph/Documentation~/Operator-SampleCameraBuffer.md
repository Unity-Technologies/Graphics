# Sample CameraBuffer

Menu Path : **Operator > Sampling > Sample CameraBuffer**

 The **Sample Texture2D** Operator samples a CameraBuffer for specific **Pixel Dimensions** and **UV**.

This translates to a Sample call on the texture in High-Level Shading Language (HLSL).

## Operator properties

| **Input**            | **Type**                             | **Description**                               |
| -------------------- | ------------------------------------ | --------------------------------------------- |
| **CameraBuffer**     | [CameraBuffer](Type-CameraBuffer.md) | The camera buffer this Operator samples from. |
| **Pixel Dimensions** | Vector2                              | The camera pixel dimensions                   |
| **UV**               | Vector2                              | The UV to sample the CameraBuffer at.         |

| **Output** | **Type** | **Description**                    |
| ---------- | -------- | ---------------------------------- |
| **s**      | Vector4  | The sampled value from the texture |

## Limitations

This Operator only runs on the GPU, therefore it does not work when plugged into **Spawner Context** ports.
