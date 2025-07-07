# Keyword parameter reference

There are three types of keywords: Boolean, Enum, and Built-in. Each keyword type has a few specific parameters in addition to the many parameters that all keyword types have in common.

## Common parameters

Parameters that all keyword types have in common.

| **Name**           | **Type** | **Description**                                              |
| ------------------ | -------- | ------------------------------------------------------------ |
| **Display Name**   | String   | The display name of the keyword. Unity shows this name in the title bar of nodes that reference the corresponding keyword, and also in the Material Inspector if you expose that keyword. |
| **Exposed**        | Boolean  | When you set this parameter to **true**, Unity displays this keyword in the Material Inspector. If you set it to **false**, the keyword does not appear in the Material Inspector.<br/><br/>If you intend to access a GLOBAL shader variable, be sure to add it as you would normally add an input variable, but deselect **Exposed**.|
| **Reference Name** | String   | The internal name for the keyword in the shader.<br/><br/>If you overwrite the Reference Name parameter, take note of the following:<ul><li>Keyword Reference Names are always in full capitals, so Unity converts all lowercase letters to uppercase.<li>If the Reference Name contains any characters that HLSL does not support, Unity replaces those characters with underscores.<li> Right-click on a Reference Name, and select **Reset Reference** to revert to the default Reference Name.</ul> |
| **Definition**     | Enum     | Sets how the keyword is defined in the shader. Determines when to compile keyword variants.<br/><br/>The options are:<ul><li>**Shader Feature**: Unity only compiles keyword variants when a Material selects the relevant option. For this option to be available in the Player, a Material selecting it must exist at build-time.<li>**Multi Compile**: Pre-compiles all the variant possibilities. This is slower and uses more memory, but allows the option to be dynamically switched in the Player.<li>**Predefined**: The render pipeline defines this keyword and controls the settings for it.</ul> |
| **Scope**          | Enum     | Sets the scope at which to define the keyword.<br/><br/>The following options are available:<ul><li>**Global keywords**: Defines keyword for the entire project, and it counts towards the global keyword limit.<li>**Local keywords**: Defines keyword for only one shader, which has its own local keyword limit.</ul>When you use Predefined keywords, Unity disables this field. |
| **Stages** | N/A | Set the stage the keyword applies to.<br/><br/>The following options are available:<ul><li>**All** - Applies this keyword to all shader stages.<<li>**Vertex** - Applies this keyword to the vertex stage.<li>**Fragment** - Applies this keyword to the fragment stage.</ul> |

<a name="BooleanKeywords"></a>

## Boolean keywords

Parameter specific to Boolean keywords in addition to the [common parameters](#common-parameters).

| **Name**    | **Type** | **Description**                                              |
| ----------- | -------- | ------------------------------------------------------------ |
| **Default** | Boolean  | Enable this parameter to set the keyword's default state to on, and disable it to set the keyword's default state to off.<br/><br/>This parameter determines the value to use for the keyword when Shader Graph generates previews. It also defines the keyword's default value when you use this shader to create a new Material. |

<a name="EnumKeywords"></a>

## Enum keywords

Parameters specific to Enum keywords in addition to the [common parameters](#common-parameters).

| **Name**    | **Type**         | **Description**                                              |
| ----------- | ---------------- | ------------------------------------------------------------ |
| **Default** | Enum             | Select an entry from the drop-down menu to determine which value to use for the keyword when Shader Graph generates previews. This also defines the keyword's default value when you use this shader to create a new Material. When you edit the Entries list, Shader Graph automatically updates the options in this control. |
| **Entries** | Reorderable List | This list defines all the states for the keyword. Each state has a separate **Entry Name** and **Reference Suffix**.<ul><li> **Entry Name**: The name displayed in drop-down menus for the keyword on the [Internal Inspector](Internal-Inspector.md) and the Material Inspector. Shader Graph also uses this name for port labels on nodes that reference the keyword.<li> **Reference Suffix**: This identifies the final keyword, presented in the format `Reference_ReferenceSuffix`.</ul> |

When you define an Enum keyword, Shader Graph displays labels for each state consisting of a version of the Enum's **Entry Name**  appended to the main **Reference** name.

> [!NOTE]
> Special characters such as `(`, `)`, `!`, or `@` are not valid in the **Entry Name** of an Enum keyword. Shader Graph converts invalid characters to underscores (`_`).

<a name="BuiltinKeywords"></a>

## Built-in keywords

The parameters of built-in keywords depend on their type, which is always of either the Boolean or Enum type, and you cannot edit their values.

All Built-in keyword fields in the **Node Settings** tab of the [Graph Inspector](Internal-Inspector.md) are grayed out except for the **Default** field, which you can enable or disable to show the differences in Shader Graph previews. You also cannot expose Built-in keywords in the Material Inspector.

## Additional resources

* [Introduction to keywords in Shader Graph](Keywords-concepts.md)
* [Manage keywords in Shader Graph](Keywords-manage.md)
