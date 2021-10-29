# Visual effect bounds

Unity uses the bounds of a visual effect to determine whether to render it or not. If a camera can't see the bounds of an effect, then it culls and doesn't render the effect. The cumulative bounds of each System within a visual effect define the bounds of the visual effect. It's important that the bounds of each System correctly encapsulate the System:
- If the bounds are too large, cameras process the visual effect even if individual particles aren't on screen. This results in wasted resources.
- If the bounds are too small, Unity may cull the visual effect even if some of the effect's particles are still on screen.

Each System in a visual effect defines its bounds in the [Initialize Context](Context-Initialize.md). By default, Unity calculates the bounds of each System automatically, but you can change this behavior and use other methods to define the bounds. The Initialize Context's **Bounds Setting Mode** property controls the method the visual effect uses. The bound calculation methods are:

- **Manual**: You set the bounds directly in the Initialize Context. You can calculate the bounds dynamically using Operators and send the output to the Initialize Context's **Bounds** input ports.
- **Recorded**: Allows you to record the System from the Target visual effect GameObject panel. For information on how to do this, see [Bounds Recording](#bounds-recording). In this mode, you can also calculate the bounds using Operators and pass them to the Initialize Context, like in **Manual**. This overrides any recorded bounds.
- **Automatic**: Unity calculates the bounds automatically. Note: This will force the culling flags of the VFX asset to "Always recompute bounds and simulate".

The Initialize Context also contains a **Bounds Padding** input port. This is a Vector3 that enlarges the per-axis bounds of the System. If a System uses **Recorded** or **Automatic** bounds, Unity calculates the bounds of the System during the Update Context. This means that any changes to the size, position, scale, or pivot of particles that occur in the Output Context don't affect the bounds during that frame. Adding padding to the bounds helps to mitigate this effect.

## Bounds recording

The [Target Visual Effect GameObject panel](VisualEffectGraphWindow.md#target-visual-effect-gameobject) in the Visual Effect Graph window includes the **Bounds Recording** section which helps you set the bounds of your Systems. If you set a System's **Bounds Setting Mode** to **Recorded**, the tool calculates the bounds of the System as the visual effect plays.

![](Images/target-go-not-recording.png)

> The Target Visual Effect GameObject panel interface while not recording.

![](Images/target-go-recording.png)

> The Target Visual Effect GameObject panel interface while recording.

You can visualize the bounds that the recorder is saving. When the recorder is active, look at the visual effect in the Scene view. The bounds appear as a red box around the visual effect. If you want to visualize the bounds of specific Systems, select them in the tool window or select their Initialize Context.

![](Images/bounds-preview.png)

> A visual effect and a preview of the bounds Unity is recording.

While recording, you can **Pause**, **Play**, **Restart**, or event change the **Play Rate**. This enables you to speed up the recording or simulate various spawn positions. When you are happy with the calculated bounds, click **Apply Bounds** to apply the recorded bounds to the System.
