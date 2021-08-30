# World To Viewport Point

Menu Path : **Operator > Camera > World To Viewport Point**

The **World To Viewport Point** Operator transforms a position into viewport space. The output viewport space is normalized and relative to the camera. The bottom-left of the camera is (0,0) and the top-right is (1,1). The z position is in world units from the camera.

## Operator settings

| **Setting** | **Type** | **Description**                                              |
| ----------- | -------- | ------------------------------------------------------------ |
| **Camera**  | Enum     | Specifies which Camera to sample the depth of. The options are:<br/>&#8226; **Main**: Uses the first Camera in the scene with the **MainCamera** tag.<br/>&#8226; **Custom**: Uses the Camera you specify in the **Camera** port. |

## Operator properties

| **Input**    | **Type** | **Description**                                              |
| ------------ | -------- | ------------------------------------------------------------ |
| **Position** | Position | A position to be projected to the camera viewport space.     |
| **Camera**   | Camera   | The Camera to use.<br/>This property only appears if you set **Camera** to **Custom**. |

| **Output**               | **Type** | **Description**                                              |
| ------------------------ | -------- | ------------------------------------------------------------ |
| **viewport Position** | Vector3  | The input position transformed in viewport space, normalized, and relative to the camera. The bottom-left of the camera is (0,0); the top-right is (1,1). The z position is in world units from the camera. |
