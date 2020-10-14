# Viewport To World Point

Menu Path : **Operator > Camera > Viewport To World Point**

The **Viewport To World Point** Operator transforms a position from viewport space to world space. The input viewport space is normalized and relative to the camera. The bottom-left of the camera is (0,0) and the top-right is (1,1). The z position is in world units from the camera.

Note that [Viewport To World Point](https://docs.unity3d.com/ScriptReference/Camera.ViewportToWorldPoint.html) transforms an x-y screen position into a x-y-z position in 3D space.

Provide the function with a vector where the x-y components of the vector are the screen coordinates and the z component is the distance of the resulting plane from the camera.

## Operator settings

| **Setting** | **Type** | **Description**                                              |
| ----------- | -------- | ------------------------------------------------------------ |
| **Camera**  | Enum     | Specifies which Camera to sample the depth of. The options are:<br/>&#8226; **Main**: Uses the first Camera in the scene with the **MainCamera** tag.<br/>&#8226; **Custom**: Uses the Camera you specify in the **Camera** port. |

## Operator properties

| **Input**             | **Type** | **Description**                                              |
| --------------------- | -------- | ------------------------------------------------------------ |
| **Viewport Position** | Vector3  | A position in viewport space, normalized and relative to the camera. The bottom-left of the camera is (0,0); the top-right is (1,1). The z position is in world units from the camera. |
| **Camera**            | Camera   | The Camera to use.<br/>This property only appears if you set **Camera** to **Custom**. |

| **Output**   | **Type**                     | **Description**                          |
| ------------ | ---------------------------- | ---------------------------------------- |
| **position** | [Position](Type-Position.md) | The transformed position in world space. |