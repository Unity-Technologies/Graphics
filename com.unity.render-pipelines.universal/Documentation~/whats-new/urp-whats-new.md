# What's new in URP 11

This section contains information about new features, improvements, and issues fixed in URP 11.0.

For a complete list of changes made in URP 11, refer to the [Changelog](../../changelog/CHANGELOG.html).

## Features

This section contains the overview of the new features in this release.

### Point Light shadows in URP

URP 11 adds support for Point Light shadows. Point Light shadows help you to create more realistic simulation of local sources of light, such as lamps, torches, campfires, etc.

![Point Light shadows](../Images/whats-new/urp-11/whats-new-point-light-shadows.png)

### SRP packages are part of the core

With the release of Unity 2021.1, graphics packages are relocating to the core of Unity. This move simplifies the experience of working with new Unity graphics features, as well as ensuring that your projects are always running on the latest verified graphics code.

For each release of Unity (alpha / beta / patch release) the graphics code is embedded within the main Unity installer. When you install the latest release of Unity, you also get the latest URP, HDRP, Shader Graph, VFX Graph, and more.

Tying graphics packages more closely to the main Unity release allows better testing to ensure that the graphics packages you use have been tested extensively with the version of Unity you have downloaded.

You can also use a local copy or a custom version of the graphics packages by overriding them in the manifest file.

For more information, see the following post on the forum: [SRP v11 beta is available now](https://forum.unity.com/threads/srp-v11-beta-is-available-now.1046539/).

## Improvements

This section contains the overview of the major improvements in this release.

### You can disable Post Processing for a specific URP asset

The URP asset now has the Post Processing check box. This check box turns post-processing on (check box selected) or off (check box cleared) for the current URP asset.<br/>If you clear this check box, Unity excludes post-processing shaders and textures from the build, unless one of the following conditions is true:<ul><li>Other assets in the build refer to the assets related to post-processing.</li><li>A different URP asset has the Post Processing property enabled.</li></ul>

![Post processing properties](../Images/whats-new/urp-11/urp-asset-post-processing.png)

## Issues resolved

For a complete list of issues resolved in URP 11, see the [Changelog](../../changelog/CHANGELOG.html).

## Known issues

For information on the known issues in URP 11, see the section [Known issues](../known-issues.md).
