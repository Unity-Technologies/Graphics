# Render Pipeline Debug

The Render Pipeline Debug window, accessible through the Window -> Analysis -> Render Pipeline Debug entry in the Editor, is a Scriptable Render Pipeline specific window containing many debugging and visualization tools aimed at helping programmers, artists and designers understand and solve any issue they may have quickly. It currently contains mostly graphics related tools but it can be extended to any other field (animation, gameplay, ...). The present documentation will cover the existing debug tools.

The Render Pipeline Debug Window can also be accessed from the runtime in Play Mode or in the standalone player on any device. To display it, users must either press control + backspace on a keyboard or L3+R3 on a controller. The runtime player however has some limitation compared to the Editor counterpart due to input limitations.
Navigation is done using the controller D-Pad or the arrows to change the current active item and shoulder buttons or page up/down to change the current page. Read-only items (such as the FPS counter for example) can be kept visible in the top right corner of the screen even when disabling the debug menu by pressing either X/Square or Right Shift. This is particularly useful when users want to track particular values without cluttering the screen.

The Render Pipeline Debug window is made of multiple panels each gathering debug items specific to a given field.

