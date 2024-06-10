# Post-Process
The post process shaders can be used to apply modifications to the rendered image once the scene has been drawn.
#### Edge Detection
The edge detection shader checks the four neighboring pixels to the current one to find “edges” or places where the normal or depth has changed rapidly. It creates a mask where edges exist and then uses the mask to blend between the original scene color and the edge color.
#### Half Tone
The halftone shader turns the rendered image into a halftone image - simulating the pattern of larger and smaller circle patterns that you might see in newsprint or comic books. First it generates a procedural grid of signed distance field circles - one for each of red, green, and blue. Then it uses inverse lerp to convert the SDF circle grid into dots - where the size of the dot represents the brightness of the color at that location. Finally it combines the red, green, and blue dot grids into one color.
#### Rain On Lens
The rain-on-the-lens post process shader applies refraction to the rendered scene as if there were rain on the camera lens - so some areas of the image are warped by rain drops and other areas are distorted by drips running down the screen.
#### Underwater
The underwater post process shader makes the scene look like it’s under water by applying several effects including blurring the screen around the edges, distorting the image is large, ripple patterns, and applying a blue/green fog based on the scene depth.
#### VHS
The VHS post process shader mimics the appearance of the scene being played back on an old VHS video cassette recorder. Artifacts include scan line jitter, read head drift, chromatic aberration, and color degradation in the YIQ color space.