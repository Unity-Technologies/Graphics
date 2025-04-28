# Visual Effects and Timeline

Use Unity’s [Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/index.html) to control a Visual Effect Graph’s properties and [Events](#Events). To do this, add the following to a [Timeline instance](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/wf_instance.html):

- [Visual effect Control Track](#vfx-control-track)
- [Visual effect Control clip](#vfx-control-clip).

<a name="vfx-control-track"></a>

## Visual Effect Control track

The Visual Effect Control track is a type of Timeline track that determines when Timeline can send [Events](Events.md) and [Event Attribute Payloads](Events.md#event-attribute-payloads).

To control a Visual Effect in a Control track, create a Visual Effect Control Track and add a Visual Effect to it:

1. Open the Timeline Window (menu: **Window** > **Sequencing** > **Timeline**)
2. Navigate to the Hierarchy window and select a Visual Effect GameObject
3. In the Timeline window, select **Create**.
4. Save your Timeline asset.
5. Click the **+** button at the top-left of the Timeline window.
6. Select **Visual Effect** **Control Track.**
7. Select the **Visual Effect (Visual Effect)** picker (⊙) and choose a Visual Effect Asset.

You can also configure the attributes of an Event in an Event Attribute Payload in script with [VFXEventAttribute](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.SendEvent.html).

### Properties

When you select a Visual Effect Control track, the following property appears in the Inspector window:

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| Reinit       | Controls when Unity resets the Visual Effect Control Track:<br/>&#8226; **On Binding Enable Or Disable:** Resets the control track when you bind or unbind a Visual Effect in the Timeline. **Reinit** uses this setting by default. <br/>&#8226; **On Binding Enable:** Resets the  control track when you bind a Visual Effect in the Timeline.<br/> &#8226; **On Binding Disable:** Resets the  Visual Effect Control track when you unbind a Visual Effect in the Timeline.<br/>&#8226; **None:** Unity doesn’t reset the Visual Effect Control track . |

<a name="vfx-control-clip"></a>

## Visual Effect Control clip

A control clip is a section of the Timeline track that controls the Visual Effect. You can use a control clip to scrub through a Visual Effect and to add clip events and single events. A Visual Effect Control clip exists inside a Visual Effect Control track.

To add a Visual Effect Control Clip:

1. In the Clips view, right-click on an empty area within the Visual Effect Control track.
2. Select **Add Visual Effect Control Clip.**
3. Select the clip to display its properties in the inspector.

Each clip in a Visual Effect Control Track controls how that Visual Effect behaves in your scene. You can send Visual Effect events to the clip in the **Clip Events** and **Single Events** sections of the Inspector window.

### Properties

When you select a Visual Effect Control clip, the following properties appears in the Inspector window:

| **Property**   | **Subproperty**    | **Option** | **Description**                                              |
| -------------- | ------------------ | ---------- | ------------------------------------------------------------ |
| Scrubbing      |  N/A               | N/A        | Use **Scrubbing** to synchronize an effect with other animated GameObjects in the Timeline instance.Enable this option to play the Visual Effect backwards and forwards when you drag the Timeline Playhead.<br/> When you enable **Scrubbing**, **Reinit** defaults to **On Enter And Exit Clip**. This is because scrubbing can only control a Visual Effect that starts at the beginning of the Visual Effect Control clip.<br/>Enable this property to use the **Prewarm** property. <br><br/>Disable Scrubbing to:<br/>&#8226; Expose the Enable PreWarm property when you set **Reinit** to **On Enter or Exit**.<br/>&#8226; Send the Enter Event when the playhead enters the Visual Effect Control clip. |
| Reinit         |  N/A               | N/A        | Controls when Timeline initializes the Visual Effect:<br/>&#8226; **None:** Timeline does not initialize the Visual Effect again.<br/>&#8226; **On Exit Clip:** Timeline restarts the Visual Effect at the end of the Visual Effect Control clip.<br/>&#8226; **On Enter Clip:** Timeline reinitializes the Visual Effect at the beginning of the Visual Effect Control clip. When **Scrubbing** is disabled, this mode exposes the Enable PreWarm property.<br/>&#8226; **On Enter or Exit:** Timeline reinitializes the Visual Effect at the beginning  and end of the Visual Effect Control clip. Reinit uses this mode by default. When **Scrubbing** is disabled, this mode exposes the **Enable PreWarm** property. |
| Start Seed     |  N/A               | N/A        | This property represents the initial seed that Unity uses for its random number generator (see also [`VisualEffect.startSeed`](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect-startSeed.html)).<br/><br/>When you enable **Scrubbing** or when the **Reinit** mode is set to **On Enter Clip**, the Visual Effect Control clip controls this property. |
| Enable PreWarm |  N/A               | N/A        | Enable this property to start processing the Visual Effect before it appears.<br/> When you set this property, it doesn’t affect the PreWarm settings in the Visual Effect Asset. This means you can have more than one animation clip that uses the same Visual Effect Asset with different PreWarm settings.<br/><br/>This property is only available when you enable **Scrubbing** or when you set the **Reinit** mode to **On Enter Clip** or **On Enter or Exit.** |
| N/A            | PreWarm Total Time | N/A        | Simulates the Visual Effect for a given amount of time before it starts. The value for this property is in seconds.   Set this value to recalculate **PreWarm Delta Time**. |
| N/A            | PreWarm Step Count | N/A        | Sets the number of simulation steps that Unity uses to calculate the PreWarm.<br/>A higher number of steps makes the PreWarm more precise but it can impact performance. <br/>When you change this value, Unity calculates a new value for **PreWarm Delta Time**. |
| N/A            | PreWarm Delta Time | N/A        | Sets the delta time, in seconds, that Unity uses for the PreWarm.<br/>When you change this value, Unity  calculates new values for **PreWarm Total Time** and **PreWarm Step Count**. Adjust this value, instead of **PreWarm Total Time** and **PreWarm Step Count** individually, if you need to use a precise delta time for your simulation. |
| N/A            | PreWarm Event Name | N/A        | The name of the [Event](Events.md) that Unity sends when the effect starts. You can customize this name, or leave it blank to make it so that every system does not spawn by default.<br/><br/>The default value is **OnPlay.** |
| Clip Events    |  N/A               | N/A        | Select **+** to add a Visual Effect [Event](Events.md) in the VisualEffect Control clip. A Clip Event has a start and an end.<br/> When you disable scrubbing and the playhead enters the Visual Effect Control clip, it sends the **Enter** Event. |
| N/A            | Enter              | N/A        | Unity starts the Visual Effect when the Timeline Playhead is at the beginning of the Clip Event. |
| N/A            |  N/A               | Time       | The value Unity uses to determine when to play the Visual Effect within the clip. The unit changes depending on the **Time Space** mode you select. |
| N/A            |  N/A               | Time Space | &#8226; **After Clip Start**: Plays the Event after the beginning of the Visual Effect Control clip, plus the number of seconds you enter in the **Time** field.**<br/>&#8226; Before Clip End**: Plays the Event before the end of the Visual Effect Control clip, minus the number of seconds you enter in the **Time** field.<br/>&#8226; **Percentage**: Plays the Event at a percentage of the total time of the Visual Effect Control clip. For example, a **Time** value of 0 indicates the start of the clip and a value of 100 indicates the end.<br/>&#8226; **Absolute**:  Plays the Event at the second you specify in the **Time** property. |
| N/A            |  N/A               | Name       | The name of the Clip Event. The default name of the Enter Clip Event is OnPlay. This name appears in the Visual Effect Control track. |
| N/A            |  N/A               | Attributes | Select **+** to add an attribute that Timeline sends with a corresponding Visual Effect Event.. For example, [position](Operator-GetAttributePosition.md), [color](Operator-GetAttributeColor.md) or a [custom attribute](Operator-GetCustomAttribute.md). |
| N/A            | Exit               | N/A        | Unity stops the Visual Effect when the Timeline Playhead reaches the end of the clip. |
| N/A            |  N/A               | Time       | The value Unity uses to determine when to play the Visual Effect within the Clip. The unit changes depending on the **Time Space** mode you select. |
| N/A            |  N/A               | Time Space | &#8226; **After Clip Start**: Plays the event after the beginning of the Visual Effect Control clip.<br/>&#8226; **Before Clip End**: Plays the event before the end of the Visual Effect Control clip.<br/>&#8226; **Percentage**: Plays the event at a percentage of the total time of the Visual Effect Control clip. For example, a value of 0 indicates the start of the clip and 100 indicates the end.<br/>&#8226; **Absolute**:  Plays the event at a specific second in the track. |
| N/A            |  N/A               | Name       | The name of the Clip Event. The default name of the Exit Clip Event is OnStop. This name appears in the Visual Effect Control track. |
| N/A            |  N/A               | Attributes | Select **+** to add an attribute that Timeline sends with a corresponding Visual Effect Event. For example, [position](Operator-GetAttributePosition.md), [color](Operator-GetAttributeColor.md) or a [custom attribute](Operator-GetCustomAttribute.md). Unity uses these attributes to create a [VFXEventAttribute](https://docs.unity3d.com/ScriptReference/VFX.VFXEventAttribute.html) which it sends with the single Event. |
| Single Events  |  N/A               | N/A        | Select **+** to add a single Visual Effect Event in the Visual Effect Control clip. A Single Event happens once. This is useful for a burst spawn system. When you disable **Scrubbing**, Single Events only play when the timeline is playing forward. |
| N/A            | Editor Color       | N/A        | The color that indicates the Single Event in the Timeline Window. |
| N/A            | Time               | N/A        | The value Unity uses to determine when to send the Single Event within the Visual Effect Control clip. The unit changes depending on the **Time Space** mode you select. |
| N/A            | Time Space         | N/A        | &#8226; **After Clip Start**: Plays the Single Event after the beginning of the Visual Effect Control clip.<br/>&#8226; **Before Clip End**: Plays the Single Event before the end of the Visual Effect Control clip.<br/>&#8226; **Percentage**: Plays the Single Event at a percentage of the total time of the Visual Effect Control clip. For example, a value of 0 indicates the start of the clip and 100 indicates the end.<br/>&#8226; **Absolute**:  Plays the single Event at a specific second in the track. |
| N/A            | Name               | N/A        | The name of the Single Event. This name appears in the Visual Effect Control track. |
| N/A            | Attributes         | N/A        | Select **+** to add an attribute that Timeline sends with a corresponding Visual Effect Event. For example, [position](Operator-GetAttributePosition.md), [color](Operator-GetAttributeColor.md) or a [custom attribute](Operator-GetCustomAttribute.md). <br/>Unity uses these attributes to create a [VFXEventAttribute](https://docs.unity3d.com/ScriptReference/VFX.VFXEventAttribute.html) which it sends with the single Event. |

## Animating Properties

To animate the animatable properties of a Visual Effect graph in the Timeline and Animation window, assign an Animation Track to your Visual Effect GameObject. To animate a Visual Effect properties in the Timeline window:

1. Create a [Visual Effect control track](#vfx-control-track).
2. In the Timeline window, open the **+** drop down and select **Animation Track.**
3. Select the **Visual Effect (Animator)** picker (⊙) and choose a Visual Effect Asset that has an [Animator component](https://docs.unity3d.com/Manual/class-Animator.html).

This binds the Visual Effect GameObject to an animation track and adds a [Playable Director](https://docs.unity3d.com/Packages/com.unity.timeline@latest/index.html?subfolder=/manual/play_director.html) component in the Inspector window.

# Limitations

- Unity limits the amount of time you can skip when you enable **Scrubbing**. You can change this limit in **Project Settings** > **VFX** > **Max Scrub Time**. If you exceed this limit, Unity displays a warning.
- When you play a Visual Effect backwards in the Timeline in runtime, it causes a high peak in GPU use. This is because this implementation uses [Simulate](https://docs.unity3d.com/ScriptReference/VFX.VisualEffect.Simulate.html).
- When you enable scrubbing for more than one Timeline track that controls the same Visual Effect, Unity displays a warning in Scene view.

# Automatic Migration

When you upgrade to Unity version 2022.2.0a9 or newer Unity automatically migrates the Visual Effect integration in Timeline. To do this, it upgrades:

- **Visual Activation track** to **Visual Effect Control track**
- **Visual Activation clip** to **Visual Effect Control clip**

To keep the same behavior and avoid any breaking changes, Unity automatically:

- Sets up a mirrored **Visual Effect Control track** with a **Visual Effect Control clip.**
- Sets the mirrored **Visual Effect Control track’s** **Reinit** property to **None.**
- Disables **Scrubbing** on the mirrored **Visual Effect Control clip.**
- Copies the **Name** and **Attributes** property values into the **Clip Events** and **Single Events.**
