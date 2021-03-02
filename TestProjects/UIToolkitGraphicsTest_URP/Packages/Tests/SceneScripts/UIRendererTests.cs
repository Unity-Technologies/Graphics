using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Rendering;

public class UIRendererTests : GraphicTestBase
{
    public VectorImage triangleImage;
    public VectorImage linGradientImage;
    public VectorImage radGradientImage;
    public VectorImage roundrectImage;
    public RenderTexture clearTexture;

    private VisualElement m_BlinkElement;
    private VisualElement m_Scroller;
    private float m_ScrollerHeight = 1000000.0f;

    AtlasTest m_AtlasTest = new AtlasTest();

    void setup()
    {
        // Scissor clipping container
        var container = new VisualElement() {
            name = "container",
            renderHints = RenderHints.ClipWithScissors,
            style = { position = Position.Absolute, left = 5, top = 5, width = 390, height = 290, overflow = Overflow.Hidden }
        };
        var bg = new VisualElement() {
            name = "bg",
            style = { position = Position.Absolute, left = -25, top = -25, width = 450, height = 350, backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1.0f) }
        };
        container.Add(bg);

        {
            // 4 rows of 50 skinned-transformed elements with varying colors and opacities and clip rects
            for (int i = 0; i < 200; ++i)
            {
                float w = 2;
                float h = 2;
                var ve = new VisualElement() {
                    name = "VE: " + i,
                    usageHints = UsageHints.DynamicTransform,
                    style =
                    {
                        position = Position.Absolute,
                        left = (i % 50) * (w + 2) + 2,
                        top = (i / 50) * (h + 2) + 2,
                        width = w,
                        height = h,
                        backgroundColor = ComputeUniqueColor(i, 400),
                        overflow = Overflow.Hidden,
                        opacity = (Mathf.Cos((i / 200.0f) * Mathf.PI) * 0.5f + 0.5f) * 0.9f + 0.1f
                    }
                };
                container.Add(ve);
            }
        }

        float x = 2;
        float y = 18;

        {
            // Shader-discard clipping
            var clipper = new VisualElement() {
                name = "clipper",
                style = { position = Position.Absolute, left = x, top = y, width = 50, height = 50, backgroundColor = Color.green, overflow = Overflow.Hidden }
            };
            var clippee = new VisualElement() {
                name = "clippee",
                style = { position = Position.Absolute, left = -25, top = -25, width = 50, height = 50, backgroundColor = Color.blue }
            };
            clipper.Add(clippee);
            container.Add(clipper);

            x += 52;
            clipper = new VisualElement()
            {
                name = "clipper",
                style = { position = Position.Absolute, left = x, top = y, width = 50, height = 50, backgroundColor = Color.green, overflow = Overflow.Hidden }
            };
            clippee = new VisualElement()
            {
                name = "clippee",
                style = { position = Position.Absolute, left = 25, top = -25, width = 50, height = 50, backgroundColor = Color.blue }
            };
            clipper.Add(clippee);
            container.Add(clipper);

            x += 52;
            clipper = new VisualElement()
            {
                name = "clipper",
                style = { position = Position.Absolute, left = x, top = y, width = 50, height = 50, backgroundColor = Color.green, overflow = Overflow.Hidden }
            };
            clippee = new VisualElement()
            {
                name = "clippee",
                style = { position = Position.Absolute, left = 25, top = 25, width = 50, height = 50, backgroundColor = Color.blue }
            };
            clipper.Add(clippee);
            container.Add(clipper);

            x += 52;
            clipper = new VisualElement()
            {
                name = "clipper",
                style = { position = Position.Absolute, left = x, top = y, width = 50, height = 50, backgroundColor = Color.green, overflow = Overflow.Hidden }
            };
            clippee = new VisualElement()
            {
                name = "clippee",
                style = { position = Position.Absolute, left = -25, top = 25, width = 50, height = 50, backgroundColor = Color.blue }
            };
            clipper.Add(clippee);
            container.Add(clipper);
        }

        x = 2;
        y += 52;

        {
            // Stencil clipping
            var clipper = new VisualElement()
            {
                name = "clipper",
                style =
                {
                    position = Position.Absolute, left = x, top = y, width = 50, height = 50, backgroundColor = Color.green, overflow = Overflow.Hidden,
                    borderTopLeftRadius = 25
                }
            };
            var clippee = new VisualElement()
            {
                name = "clippee",
                style = { position = Position.Absolute, left = -25, top = -25, width = 50, height = 50, backgroundColor = Color.blue }
            };
            clipper.Add(clippee);
            container.Add(clipper);

            x += 52;
            clipper = new VisualElement()
            {
                name = "clipper",
                style =
                {
                    position = Position.Absolute, left = x, top = y, width = 50, height = 50, backgroundColor = Color.green, overflow = Overflow.Hidden,
                    borderTopRightRadius = 25
                }
            };
            clippee = new VisualElement()
            {
                name = "clippee",
                style = { position = Position.Absolute, left = 25, top = -25, width = 50, height = 50, backgroundColor = Color.blue }
            };
            clipper.Add(clippee);
            container.Add(clipper);

            x += 52;
            clipper = new VisualElement()
            {
                name = "clipper",
                style =
                {
                    position = Position.Absolute, left = x, top = y, width = 50, height = 50, backgroundColor = Color.green, overflow = Overflow.Hidden,
                    borderBottomRightRadius = 25
                }
            };
            clippee = new VisualElement()
            {
                name = "clippee",
                style = { position = Position.Absolute, left = 25, top = 25, width = 50, height = 50, backgroundColor = Color.blue }
            };
            clipper.Add(clippee);
            container.Add(clipper);

            x += 52;
            clipper = new VisualElement()
            {
                name = "clipper",
                style =
                {
                    position = Position.Absolute, left = x, top = y, width = 50, height = 50, backgroundColor = Color.green, overflow = Overflow.Hidden,
                    borderBottomLeftRadius = 25
                }
            };
            clippee = new VisualElement()
            {
                name = "clippee",
                style = { position = Position.Absolute, left = -25, top = 25, width = 50, height = 50, backgroundColor = Color.blue }
            };
            clipper.Add(clippee);
            container.Add(clipper);
        }

        x = 2;
        y += 52;

        {
            // Nested bone transforms
            var parent = new VisualElement() {
                name = "parent",
                usageHints = UsageHints.DynamicTransform,
                style =
                {
                    position = Position.Absolute, left = x, top = y, width = 50, height = 50, backgroundColor = Color.red
                }
            };

            var child = new VisualElement() {
                name = "child",
                usageHints = UsageHints.DynamicTransform,
                style =
                {
                    position = Position.Absolute, left = 0, top = 0, width = 10, height = 10, backgroundColor = Color.blue
                }
            };
            child.transform.position = new Vector3(5, 5, 0);
            child.transform.scale = new Vector3(2, 2, 1);

            parent.Add(child);
            container.Add(parent);
        }

        x += 52;

        {
            // Nested group transforms
            var parent = new VisualElement()
            {
                name = "parent",
                usageHints = UsageHints.GroupTransform,
                style =
                {
                    position = Position.Absolute, left = x, top = y, width = 50, height = 50, backgroundColor = Color.red
                }
            };

            var child = new VisualElement()
            {
                name = "child",
                usageHints = UsageHints.GroupTransform,
                style =
                {
                    position = Position.Absolute, left = 0, top = 0, width = 10, height = 10, backgroundColor = Color.blue
                }
            };
            child.transform.position = new Vector3(5, 5, 0);
            child.transform.scale = new Vector3(2, 2, 1);

            parent.Add(child);
            container.Add(parent);
        }

        x += 52;

        {
            // VectorImage tests

            // The triangle VectorImage was generated from this SVG:
            // <svg width="20cm" height="8cm" version="1.1" xmlns="http://www.w3.org/2000/svg">
            //    <polygon fill="red" points="0,300 300,300 150,0" />
            // </svg>
            var triangle = new VisualElement() {
                name = "triangle",
                style =
                {
                    position = Position.Absolute, left = x, top = y, width = 24, height = 24,
                    backgroundImage = new StyleBackground(triangleImage)
                }
            };
            container.Add(triangle);

            var clippingTriangle = new VisualElement() {
                name = "clippingTriangle",
                style =
                {
                    position = Position.Absolute, left = x, top = y + 26, width = 24, height = 24,
                    backgroundImage = new StyleBackground(triangleImage),
                    overflow = Overflow.Hidden
                }
            };
            // Test SVG-clipping by adding a blue child (should be clipped by the triangle)
            clippingTriangle.Add(new VisualElement() { style = { backgroundColor = Color.blue, width = 24, height = 24 } });
            container.Add(clippingTriangle);

            x += 26;

            // The linear gradient VectorImage was generated from this SVG:
            // <svg width="20cm" height="8cm" version="1.1" xmlns="http://www.w3.org/2000/svg">
            //   <defs>
            //     <linearGradient id="MyGradient">
            //       <stop offset="0%" stop-color="#FF0000" />
            //       <stop offset="100%" stop-color="#0000FF" />
            //     </linearGradient>
            //   </defs>
            //   <rect fill="url(#MyGradient)" x="0" y="0" width="100" height="100"/>
            // </svg>
            var linGrad = new VisualElement() {
                name = "linGrad",
                style =
                {
                    position = Position.Absolute, left = x, top = y, width = 24, height = 24,
                    backgroundImage = new StyleBackground(linGradientImage)
                }
            };
            container.Add(linGrad);

            // The radial gradient VectorImage was generated from this SVG:
            // <svg width="8cm" height="4cm" viewBox="0 0 800 400" version="1.1" xmlns="http://www.w3.org/2000/svg">
            //   <defs>
            //     <radialGradient id="MyGradient" gradientUnits="userSpaceOnUse" cx="50" cy="50" r="50" fx="50" fy="50">
            //       <stop offset="0%" stop-color="red" />
            //       <stop offset="100%" stop-color="blue" />
            //     </radialGradient>
            //   </defs>
            //    <rect fill="url(#MyGradient)" x="0" y="0" width="100" height="100"/>
            // </svg>
            var radGrad = new VisualElement() {
                name = "radGrad",
                style =
                {
                    position = Position.Absolute, left = x, top = y + 26, width = 24, height = 24,
                    backgroundImage = new StyleBackground(radGradientImage)
                }
            };
            container.Add(radGrad);

            x += 26;

            // Test rectangle clipping triggere by UVs
            var UVs = new Rect[]
            {
                new Rect(0.0f, 0.0f, 0.5f, 1.0f),
                new Rect(0.0f, 0.0f, 1.0f, 0.5f),
                new Rect(0.5f, 0.0f, 0.5f, 1.0f),
                new Rect(0.0f, 0.5f, 1.0f, 0.5f)
            };
            for (int i = 0; i < UVs.Length; ++i)
            {
                float left = x + (i / 2) * 26;
                float top = y + (i % 2) * 26;
                var image = new Image() {
                    name = "uvImage" + i,
                    vectorImage = triangleImage,
                    uv = UVs[i],
                    scaleMode = ScaleMode.StretchToFill,
                    style = { position = Position.Absolute, left = left, top = top, width = 24, height = 24 }
                };
                container.Add(image);
            }

            x += 52;

            // Test 9-sliced SVGs. The roundrect VectorImage was generated from this SVG:
            // <svg width="20cm" height="8cm" version="1.1" xmlns="http://www.w3.org/2000/svg">
            //   <rect fill="none" stroke="lime" stroke-width="2" rx="5" ry="5" width="20" height="20" />
            // </svg>
            var sliced = new VisualElement() {
                name = "sliced",
                style =
                {
                    unitySliceLeft = 5, unitySliceTop = 5, unitySliceRight = 5, unitySliceBottom = 5,
                    position = Position.Absolute, left = x, top = y, width = 50, height = 50,
                    backgroundImage = new StyleBackground(roundrectImage)
                }
            };
            container.Add(sliced);
        }

        x = 2;
        y += 52;

        {
            // Borders
            var ve = new VisualElement() {
                name = "VE",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 50, height = 50,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red,
                    borderTopColor = Color.red,
                    borderRightColor = Color.red,
                    borderBottomColor = Color.red,
                    borderLeftWidth = 1,
                    borderTopWidth = 2,
                    borderRightWidth = 4,
                    borderBottomWidth = 8
                }
            };
            container.Add(ve);

            x += 52;

            ve = new VisualElement() {
                name = "VE",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 50, height = 50,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red,
                    borderTopColor = Color.red,
                    borderRightColor = Color.red,
                    borderBottomColor = Color.red,
                    borderLeftWidth = 1,
                    borderTopWidth = 2,
                    borderRightWidth = 4,
                    borderBottomWidth = 8,
                    borderTopLeftRadius = 8,
                    borderTopRightRadius = 8,
                    borderBottomRightRadius = 8,
                    borderBottomLeftRadius = 8
                }
            };
            container.Add(ve);

            // Keep this element to make it blink over a few frames.
            // This is enough to trigger the WebGL buffer sup-update bug (UIE-117).
            m_BlinkElement = ve;

            x += 52;

            ve = new VisualElement() {
                name = "VE",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 50, height = 50,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red,
                    borderTopColor = Color.red,
                    borderRightColor = Color.red,
                    borderBottomColor = Color.red,
                    borderLeftWidth = 1,
                    borderTopWidth = 2,
                    borderRightWidth = 4,
                    borderBottomWidth = 8,
                    borderTopLeftRadius = 16,
                    borderTopRightRadius = 16,
                    borderBottomRightRadius = 16,
                    borderBottomLeftRadius = 16
                }
            };
            container.Add(ve);

            x += 52;

            ve = new VisualElement() {
                name = "VE",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 50, height = 50,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red,
                    borderTopColor = Color.red,
                    borderRightColor = Color.red,
                    borderBottomColor = Color.red,
                    borderLeftWidth = 1,
                    borderTopWidth = 2,
                    borderRightWidth = 4,
                    borderBottomWidth = 8,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 8,
                    borderBottomRightRadius = 12,
                    borderBottomLeftRadius = 16
                }
            };
            container.Add(ve);

            x += 52;

            ve = new VisualElement() {
                name = "VE",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 50, height = 50,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red,
                    borderTopColor = Color.red,
                    borderRightColor = Color.red,
                    borderBottomColor = Color.red,
                    borderLeftWidth = 1,
                    borderTopWidth = 2,
                    borderRightWidth = 4,
                    borderBottomWidth = 8,
                    borderTopLeftRadius = 50,
                    borderTopRightRadius = 50,
                    borderBottomRightRadius = 50,
                    borderBottomLeftRadius = 50
                }
            };
            container.Add(ve);
        }

        x = 2;
        y += 52;

        {
            // Sub-pixel borders (should stay visible)
            var ve = new VisualElement() {
                name = "VE",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 50, height = 50,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red,
                    borderTopColor = Color.red,
                    borderRightColor = Color.red,
                    borderBottomColor = Color.red,
                    borderLeftWidth = 1,
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1
                }
            };
            ve.transform.scale = new Vector3(0.5f, 0.5f, 1.0f);
            container.Add(ve);
        }

        x += 52;

        {
            // Visible child in hidden parent
            var parent = new VisualElement() {
                name = "Parent",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 50, height = 50,
                    backgroundColor = Color.blue,
                    visibility = Visibility.Hidden
                }
            };
            var child = new VisualElement() {
                name = "Child",
                style =
                {
                    position = Position.Absolute, left = 10, top = 10,  width = 30, height = 30,
                    backgroundColor = Color.red,
                    visibility = Visibility.Visible
                }
            };
            parent.Add(child);
            container.Add(parent);
        }

        x += 52;

        {
            // Border rounding to nearest integer should yield consistent border width (case 1131952)
            //
            // WARNING: PADDING, WIDTH, HEIGHT, SCALING AND BORDER VALUES HAVE BEEN CAREFULLY CHOSEN TO REPRODUCE THE
            // ERROR WHILE AVOIDING POSITIONING BOUNDARIES ON PIXEL HALVES TO AVOID INSTABILITIES. TO BE VALID, THIS
            // TEST MUST EXECUTE ON A MONITOR WHERE THE DPI SCALING IS A WHOLE NUMBER (E.G. 100%, 200%). WHEN MOVING
            // THIS TEST CASE, MAKE SURE TO POSITION THE PARENT ELEMENT ON A PIXEL BOUNDARY (I.E. USE WHOLE NUMBERS FOR
            // LEFT/TOP).
            //
            // The idea behind this test is to ensure that we effectively displace borders so that the observed width
            // remains constant independently of the position of the border. For example, a border with a width of
            // 1.5 pixels in the shader must be displaced so its width becomes 2 pixels. As a result, with point
            // sampling, we obtain consistent border widths without any need for alignment with the pixel grid.
            //
            // The following represents what this case does for a row of pixels lit by left borders.
            //   ______________
            //  |  ___________ |
            //  | |  ______  | |
            //  | | |  __  | | |
            //  | | | |__| | | | << Consider the 4 left borders at this level
            //  | | |______| | |
            //  | |__________| |
            //  |______________|
            //
            // In the following,
            // | is a pixel boundary
            // O is the position of a 1/4, 1/2, or 3/4 pixel boundary
            // B is a left border mesh
            // X is a pixel that is colored by the border.
            //
            // Pixel Grid: | O O O | O O O | O O O | O O O | O O O | O O O | O O O | O O O | O O O | O O O | O O O | O O O | O O O |
            // Input Mesh:   BBBBBBBBBBBBB               BBBBBBBBBBBBB               BBBBBBBBBBBBB               BBBBBBBBBBBBB
            //        0.25px padding        2px padding   1.5px border  2px padding   1.5px border  2px padding   1.5px border
            //
            // Without the displacement, we would get the following result (inconsistent border widths):
            // Result A:   XXXXXXXXXXXXXXXX                XXXXXXXX                XXXXXXXXXXXXXXXX                XXXXXXXX
            //                 2 pixels                    1 pixel                     2 pixels                    1 pixel
            //
            // The displacement expands the mesh this way, which ensures that 2 pixels are overlapped, yielding a
            // consistent border width:
            // Displaced:    BBBBBBBBBBBBBBBB            BBBBBBBBBBBBBBBB            BBBBBBBBBBBBBBBB            BBBBBBBBBBBBBBBB
            // Result B:   XXXXXXXXXXXXXXXX                XXXXXXXXXXXXXXXX        XXXXXXXXXXXXXXXX                XXXXXXXXXXXXXXXX
            //                 2 pixels                        2 pixels                2 pixels                        2 pixels
            //
            // Note that all values are not applied as-is but with a 4x multiplier. A scaling factor of 0.25x is
            // applied to the parent element to compensate. As a result, the layout takes place with integers, which
            // prevents the layout engine from altering the test case by rounding positions.
            var parent = new VisualElement
            {
                name = "Parent",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 200, height = 200,
                    paddingBottom = 1,
                    paddingLeft = 1,
                    paddingRight = 1,
                    paddingTop = 1,
                    backgroundColor = Color.blue
                }
            };
            parent.transform.scale = new Vector3(0.25f, 0.25f, 1);
            VisualElement current = parent;
            for (int i = 0; i < 6; ++i)
            {
                var child = new VisualElement
                {
                    name = "Child" + i,
                    style =
                    {
                        flexGrow = 1,
                    }
                };
                current.Add(child);
                current = child;
            }
            current = parent.hierarchy.ElementAt(0);
            do
            {
                current.style.paddingBottom = 8;
                current.style.paddingLeft = 8;
                current.style.paddingRight = 8;
                current.style.paddingTop = 8;
                current.style.borderBottomColor = Color.white;
                current.style.borderLeftColor = Color.white;
                current.style.borderRightColor = Color.white;
                current.style.borderTopColor = Color.white;
                current.style.borderBottomWidth = 6;
                current.style.borderLeftWidth = 6;
                current.style.borderRightWidth = 6;
                current.style.borderTopWidth = 6;
                current = current.hierarchy.childCount != 0 ? current.hierarchy.ElementAt(0) : null;
            }
            while (current != null);
            container.Add(parent);
        }

        x += 52;

        {
            // Cascading opacity
            var parent = new VisualElement()
            {
                name = "Parent",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 50, height = 50,
                    backgroundColor = Color.red,
                    opacity = 0.7f
                }
            };
            var child = new VisualElement()
            {
                name = "Child",
                style =
                {
                    position = Position.Absolute, left = 10, top = 10,  width = 30, height = 30,
                    backgroundColor = Color.green,
                    opacity = 0.7f,
                }
            };
            var child2 = new VisualElement()
            {
                name = "Child2",
                style =
                {
                    position = Position.Absolute, left = 15, top = 15,  width = 20, height = 20,
                    backgroundColor = Color.blue,
                    opacity = 1.0f,
                }
            };
            child.Add(child2);
            parent.Add(child);
            container.Add(parent);
        }

        y = 18;
        x = 5 * 52 + 2;

        {
            // Colored Borders
            var square = new VisualElement() {
                name = "Parent",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 24, height = 24,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red, borderTopColor = Color.green, borderRightColor = Color.cyan, borderBottomColor = Color.yellow,
                    borderTopWidth = 5, borderLeftWidth = 7, borderBottomWidth = 9, borderRightWidth = 11,
                }
            };
            container.Add(square);

            y += 26;

            var fan = new VisualElement() {
                name = "Parent",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 24, height = 24,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red, borderTopColor = Color.green, borderRightColor = Color.cyan, borderBottomColor = Color.yellow,
                    borderTopWidth = 10, borderLeftWidth = 10, borderBottomWidth = 10, borderRightWidth = 10,
                    borderTopLeftRadius = 10, borderTopRightRadius = 10, borderBottomRightRadius = 10, borderBottomLeftRadius = 10,
                }
            };
            container.Add(fan);

            y += 26;

            var borderedFan = new VisualElement() {
                name = "Parent",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 24, height = 24,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red, borderTopColor = Color.green, borderRightColor = Color.cyan, borderBottomColor = Color.yellow,
                    borderTopWidth = 5, borderLeftWidth = 5, borderBottomWidth = 5, borderRightWidth = 5,
                    borderTopLeftRadius = 10, borderTopRightRadius = 10, borderBottomRightRadius = 10, borderBottomLeftRadius = 10,
                }
            };
            container.Add(borderedFan);

            y += 26;

            var complex = new VisualElement() {
                name = "Parent",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 24, height = 24,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red, borderTopColor = Color.green, borderRightColor = Color.cyan, borderBottomColor = Color.yellow,
                    borderTopWidth = 5, borderLeftWidth = 10, borderBottomWidth = 5, borderRightWidth = 10,
                    borderTopLeftRadius = 7, borderTopRightRadius = 7, borderBottomRightRadius = 7, borderBottomLeftRadius = 7,
                }
            };
            container.Add(complex);
        }

        y += 26;

        {
            // Percent
            var colored = new VisualElement() {
                name = "Parent",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 24, height = 50,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red, borderTopColor = Color.green, borderRightColor = Color.cyan, borderBottomColor = Color.yellow,
                    borderTopWidth = 5, borderLeftWidth = 5, borderBottomWidth = 5, borderRightWidth = 5,
                    borderTopLeftRadius = Length.Percent(100), borderTopRightRadius = Length.Percent(100),
                    borderBottomRightRadius = Length.Percent(100), borderBottomLeftRadius = Length.Percent(100),
                }
            };
            container.Add(colored);
        }

        y += 52;

        {
            // Seams
            var square = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 24, height = 24,
                    borderLeftWidth = 9.49f,
                    borderRightWidth = 9.49f,
                    borderTopWidth = 9.49f,
                    borderBottomWidth = 9.49f,
                    borderLeftColor = Color.green,
                    borderRightColor = Color.green,
                    borderTopColor = Color.green,
                    borderBottomColor = Color.green,
                }
            };
            container.Add(square);
        }

        y += 26;

        {
            // Element in group in clip
            var clipper = new VisualElement() {
                name = "Clipper",
                style =
                {
                    position = Position.Absolute, left = x, top = y, width = 24, height = 24,
                    backgroundColor = Color.red,
                    overflow = Overflow.Hidden
                }
            };
            var group = new VisualElement() {
                name = "Group",
                usageHints = UsageHints.GroupTransform,
                style =
                {
                    position = Position.Absolute, left = 0, top = -28, width = 24, height = 48,
                    backgroundColor = Color.green
                }
            };
            clipper.Add(group);
            var clippee = new VisualElement() {
                style =
                {
                    position = Position.Absolute, left = 4, top = 32, width = 24, height = 24,
                    backgroundColor = Color.blue
                }
            };
            group.Add(clippee);
            container.Add(clipper);
        }

        y += 26;

        {
            // Visible child in hidden stencil-clipped parent
            var parent = new VisualElement() {
                name = "Parent",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 24, height = 24,
                    backgroundColor = Color.blue,
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    overflow = Overflow.Hidden,
                    visibility = Visibility.Hidden
                }
            };
            var child = new VisualElement() {
                name = "Child",
                style =
                {
                    position = Position.Absolute, left = 10, top = 10,  width = 24, height = 24,
                    backgroundColor = Color.red,
                    visibility = Visibility.Visible
                }
            };
            parent.Add(child);
            container.Add(parent);
        }

        y += 26;

        {
            // Clipping should be inside borders
            var parent = new VisualElement() {
                name = "Parent",
                style =
                {
                    position = Position.Absolute, left = x, top = y,  width = 24, height = 24,
                    backgroundColor = Color.blue,
                    borderLeftColor = Color.red,
                    borderTopColor = Color.red,
                    borderRightColor = Color.red,
                    borderBottomColor = Color.red,
                    borderLeftWidth = 1,
                    borderTopWidth = 2,
                    borderRightWidth = 3,
                    borderBottomWidth = 4,
                    overflow = Overflow.Hidden
                }
            };
            var child = new VisualElement() {
                name = "Child",
                style =
                {
                    position = Position.Absolute, left = -1, top = -2,  width = 24, height = 24,
                    backgroundColor = Color.green
                }
            };
            parent.Add(child);
            container.Add(parent);
        }

        // Second Mini Column
        x = 5 * 52 + 1 * 26 + 2;
        y = 18;

        // Case 1222517: Content-Box Clipping
        {
            // Use padding to clip
            {
                var scaler = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute, left = x, top = y, width = 48, height = 48,
                        backgroundColor = Color.white,
                    }
                };
                scaler.transform.scale = new Vector3(0.5f, 0.5f, 1);
                container.Add(scaler);

                var paddingClipper = new TextElement
                {
                    style =
                    {
                        width = 48, height = 48,
                        backgroundColor = Color.red,
                        paddingLeft = 10, paddingRight = 10, paddingTop = 10, paddingBottom = 10,
                        unityOverflowClipBox = OverflowClipBox.ContentBox,
                        overflow = Overflow.Hidden,
                    }
                };
                scaler.Add(paddingClipper);

                var paddingContent = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute, left = -10, top = -10, width = 48, height = 48,
                        backgroundColor = Color.green
                    }
                };
                paddingClipper.Add(paddingContent);
            }

            y += 26;

            // Use border to clip
            {
                var scaler = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute, left = x, top = y, width = 48, height = 48,
                        backgroundColor = Color.white,
                    }
                };
                scaler.transform.scale = new Vector3(0.5f, 0.5f, 1);
                container.Add(scaler);

                var borderClipper = new TextElement
                {
                    style =
                    {
                        width = 48, height = 48,
                        backgroundColor = Color.red,
                        borderLeftWidth = 10, borderRightWidth = 10, borderTopWidth = 10, borderBottomWidth = 10,
                        borderLeftColor = Color.blue, borderRightColor = Color.blue, borderTopColor = Color.blue, borderBottomColor = Color.blue,
                        unityOverflowClipBox = OverflowClipBox.ContentBox,
                        overflow = Overflow.Hidden,
                    }
                };
                scaler.Add(borderClipper);

                var borderContent = new VisualElement
                {
                    style =
                    {
                        position = Position.Absolute, left = -10, top = -10, width = 48, height = 48,
                        backgroundColor = Color.green
                    }
                };
                borderClipper.Add(borderContent);
            }
        }

        // Clipping with group transform using large offset (case 1296815)
        {
            var ve = new VisualElement() {
                style =
                {
                    position = Position.Absolute,
                    left = 210, top = 18,
                    width = 50, height = 50,
                    backgroundColor = Color.green,
                    overflow = Overflow.Hidden
                }
            };

            #if UNITY_EDITOR_OSX || UNITY_ANDROID
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
            {
                // MacGL and Android GL have issue with large group-transform offsets + clipping,
                // possibly because of lower fragment shading float precision.
                m_ScrollerHeight = 2000.0f;
            }
            #endif

            m_Scroller = new VisualElement() {
                name = "Long",
                usageHints = UsageHints.GroupTransform,
                style =
                {
                    position = Position.Absolute,
                    left = 0, top = 0,
                    width = 50, height = m_ScrollerHeight,
                    backgroundColor = Color.blue
                }
            };
            ve.Add(m_Scroller);

            var redClipper = new VisualElement() {
                name = "RedClipper",
                style =
                {
                    position = Position.Absolute,
                    left = 10, top = m_ScrollerHeight / 2 + 10,
                    width = 30, height = 30,
                    backgroundColor = Color.white,
                    overflow = Overflow.Hidden
                }
            };
            m_Scroller.Add(redClipper);

            var red = new VisualElement() {
                name = "Red",
                style =
                {
                    position = Position.Absolute,
                    left = 10, top = 10,
                    width = 300, height = 300,
                    backgroundColor = Color.red
                }
            };
            redClipper.Add(red);

            container.Add(ve);
        }

        m_AtlasTest.Initialize(container, 210, 226);

        // Clear Test (Case 1277149)
        {
            // Another panel is drawn into clearTexture. No content is actually rendered besides the background being
            // cleared to (1,1,1,0.5) as per user input, which must be stored as (0.5,0.5,0.5,0.5) in clearTexture.
            // Unfortunately we cannot display this result properly because we don't support premultiplied textures
            // yet. However, at the moment of writing these lines, the reference image has been validated taking the
            // double multiplication by alpha into account. The displayed result is not expected to change as long as
            // we don't introduce support for premultiplied textures so this incorrect result should remain as-is for
            // the time being.
            var clearContent = new Image
            {
                style =
                {
                    position = Position.Absolute, left = 288, top = 70, width = 24, height = 24
                },
                image = clearTexture
            };
            container.Add(clearContent);
        }

        GetComponent<UIDocument>().rootVisualElement.Add(container);
    }

    void OnDisable()
    {
        m_AtlasTest.Destroy();
    }

    private int m_FrameCount;
    void Start()
    {
        setup();
        m_FrameCount = 0;
    }

    void Update()
    {
        ++m_FrameCount;

        if (m_FrameCount == 5)
        {
            m_BlinkElement.style.display = DisplayStyle.None;

            var p = m_Scroller.transform.position;
            p.y = -m_ScrollerHeight / 2;
            m_Scroller.transform.position = p;
        }
        else if (m_FrameCount == 7)
        {
            m_BlinkElement.style.display = DisplayStyle.Flex;
        }
    }

    public static Color ComputeUniqueColor(int index, int numColors)
    {
        numColors = Mathf.Clamp(numColors, 1, int.MaxValue);

        float hueAngleStep = 360f / (float)numColors;
        float hueLoopOffset = hueAngleStep * 0.5f;

        float hueAngle = index * hueAngleStep;
        float loops = (int)(hueAngle / 360f);
        float hue = ((hueAngle % 360f + (loops * hueLoopOffset % 360f)) / 360f);

        return Color.HSVToRGB(hue, 1f, 1f);
    }

    public static Texture2D[] GenerateAtlasTestTextures()
    {
        return new Texture2D[0];
    }

    public void DestroyAtlasTestTextures(Texture2D[] textures)
    {
        for (int i = 0; i < textures.Length; ++i)
            DestroyImmediate(textures[i]);
    }

    public class AtlasTest
    {
        Texture2D[] m_Textures;

        public void Initialize(VisualElement root, int left, int top)
        {
            m_Textures = GenerateTextures(32);

            var container = new VisualElement
            {
                name = "AtlasTest",
                style =
                {
                    position = Position.Absolute, left = left, top = top, width = 50, height = 50,
                    flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap
                }
            };
            root.Add(container);

            for (int i = 0; i < 10; ++i)
            {
                for (int j = 0; j < 10; ++j)
                {
                    int index = j + i * 10;
                    var ve = new Image
                    {
                        style = { width = 5, height = 5 },
                        image = m_Textures[index % m_Textures.Length],
                        scaleMode = ScaleMode.StretchToFill,
                        name = "AtlasTest" + index
                    };
                    container.Add(ve);
                }
            }
        }

        public void Destroy()
        {
            if (m_Textures != null)
                for (int i = 0; i < m_Textures.Length; ++i)
                    DestroyImmediate(m_Textures[i]);
        }

        static Texture2D[] GenerateTextures(int count)
        {
            var textures = new Texture2D[count];

            for (int i = 0; i < count; ++i)
            {
                // Conversion to base 4 where each "digit" is converted to 0, 0.333, 0.666 or 1. Black is skipped
                float r = (((i + 1) >> 0) % 4) / 3f;
                float g = (((i + 1) >> 2) % 4) / 3f;
                float b = (((i + 1) >> 4) % 4) / 3f;
                Color color = new Color(r, g, b, 1);
                // Alternate between atlassable and non-atlassable sizes.
                int size = i % 2 == 0 ? 128 : 16;
                textures[i] = CreateTexture(size, size, color);
            }

            return textures;
        }

        static Texture2D CreateTexture(int width, int height, Color color)
        {
            var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
            var pixels = new Color32[width * height];
            for (int i = 0; i < width * height; ++i)
                pixels[i] = color;
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return tex;
        }
    }
}
