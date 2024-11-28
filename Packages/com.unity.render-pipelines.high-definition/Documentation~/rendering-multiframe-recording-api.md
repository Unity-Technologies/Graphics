# Use the multiframe rendering API

Use the multiframe rendering API to record frames that HDRP accumulates. 

## Limitations

The multi-frame rendering API internally changes the `Time.timeScale` of the Scene. This means that:

- You can't have different accumulation motion blur parameters per camera.
- Projects that already modify this parameter per frame aren't be compatible with this feature.

## Multiframe rendering API calls

The multiframe rendering API contains the following calls:

- `BeginRecording`: Call this when you want to start a multi-frame render.
- `PrepareNewSubFrame`: Call this before rendering a new subframe.
- `EndRecording`: Call this when you want to stop the multi-frame render.

The only call that takes any parameters is **BeginRecording**. Here is an explanation of the parameters:

| Parameter           | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| **Samples**         | The number of sub-frames to accumulate. This parameter overrides the number of path tracing samples in the [Volume](understand-volumes.md). |
| **ShutterInterval** | The amount of time the shutter is open between two subsequent frames. A value of **0** results in an instant shutter (no motion blur). A value of **1** means there is no (time) gap between two subsequent frames. |
| **ShutterProfile**  | An animation curve that specifies the shutter position during the shutter interval. Alternatively, you can also provide the time the shutter was fully open; and when the shutter begins closing. |

Before calling the accumulation API, the application should also set the desired `Time.captureDeltaTime`. Refer to [Combine animations in a script](rendering-combine-animation-sequences-in-script) for an example.
