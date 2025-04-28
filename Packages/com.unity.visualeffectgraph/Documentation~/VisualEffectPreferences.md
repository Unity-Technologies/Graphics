# Visual Effect Graph preferences

The Visual Effect Graph preferences are in the Unity Preferences window. To access them:

Windows:
* Navigate to **Edit > Preferences > Visual Effects**

macOS: 
* Navigate to **Unity > Settings > Visual Effects**

## Properties:

| Name                                         | Description                                                  |
| -------------------------------------------- | ------------------------------------------------------------ |
| **Experimental Operators/Blocks**            | Displays [experimental Blocks and Operators](ExperimentalFeatures.md) in the [Node Creation Menu](GettingStarted.md#manipulating-graph-elements). |
| **Show Additional Debug info**               | Displays more detailed debug information in the Inspector when you select [Blocks](Blocks.md), [Operators](Operators.md) or [Contexts](Contexts.md) |
| **Verbose Mode for Compilation**             | Enables verbose logging in the console when Unity compiles VFX Graphs.     |
| **Experimental Shader Externalization**      | Enables externalizing shaders for Debugging purposes in the [Visual Effect Graph Asset Inspector](VisualEffectGraphAsset.md). |
| **Generate Shaders with Debug Symbols**      | Enables shader debug symbols when Unity compiles a VFX graph. |
| **Force Compilation in Edition Mode**        | Disables Graph Optimization when saving Assets. Only enable this property for debugging purposes. |
| **Multithread Update Enabled**               | Enables multi-threading when possible to speed up effects updates. Only disable this property for debugging purposes. |
| **Instancing Enabled**                       | Enables the [Instancing](Instancing.md) feature to improve scalability. Only disable this property for debugging purposes. |
| **Main Camera fallback**                     | Specifies the camera source for [MainCamera](Operator-MainCamera.md) Operators and [Blocks](Blocks.md) to use when in the editor. The options are:<br/>&#8226; **Prefer Main Camera**: If the Game view is open, Unity uses the main Camera. If the Game view is not open, but the Scene view is, Unity uses the Scene view Camera.<br/>&#8226; **Prefer Scene Camera**: If the Scene view is open, Unity uses the Scene view Camera. If the Scene view is not open, but the Game view is, Unity uses the main Camera.<br/>&#8226; **No Fallback**: Uses the main Camera even if Unity does not render to it. |
| **Target in Shader Graph**                   | Displays the [Visual Effect Target](sg-target-visual-effect.md) in the Shader Graph **Target Settings**. From VFX Graph version 23.1, use the [Support VFX Graph](sg-working-with.md) toggle instead. |
| **Authoring Prewarm Step Count Per Second**  | Specifies the number of steps each second that [Auto Reinit](VisualEffectGraphWindow.md#Toolbar) prewarms. A high value might impact performance when you author a VFX Graph. |
| **Authoring Prewarm Maximum Time**           | Specifies the maximum amount of time (in seconds) that [Auto Reinit](VisualEffectGraphWindow.md#Toolbar) can prewarm. |

