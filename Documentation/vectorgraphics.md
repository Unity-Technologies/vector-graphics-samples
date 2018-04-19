# About Vector Graphics

The Vector Graphics package provides an SVG importer as well as generic vector graphics APIs.

## Requirements

This Vector Graphics package version 1.0.0 is compatible with the following versions of the Unity Editor:

* 2018.1 and later (recommended)

## Known Limitations

The SVG importer from this package implements a subset of the SVG 1.1 specification, with some limitations:

* Text elements are not yet supported [(SVG 1.1 section 10)](https://www.w3.org/TR/SVG11/text.html)
* Per-pixel masking is not supported [(SVG 1.1 section 14.4)](https://www.w3.org/TR/SVG11/masking.html#Masking)
* Filter effects are not supported [(SVG 1.1 section 15)](https://www.w3.org/TR/SVG11/filters.html)
* Any interactivity feature are not supported [(SVG 1.1 section 16)](https://www.w3.org/TR/SVG11/interact.html)
* Animations are not supported [(SVG 1.1 section 19)](https://www.w3.org/TR/SVG11/animate.html)

# Using Vector Graphics

## SVG Importer

This package provides an SVG importer that reads and interprets SVG documents and generates 2D sprites to be used by the Unity engine.

You import SVG files like any other assets.  You either drop them directly into the Assets Browser, or you use the `Assets > Import New Asset...` menu item.  Once imported, you can instantiate the resulting assets in the Hierarchy View or the Scene View.

The SVG importer has the following properties:

![SVG importer properties](images/svg_inspector.png)

**Pixels Per Unit**: How many SVG units correspond to 1 scene unit.

**Tessellation Step Distance**: At which distance are triangles generated when tessellating the paths.  A smaller step distance will result in smoother curves at the expense of more triangles.

**Gradient Resolution**: The texture size used to store gradients.

**Pivot**: The location of the pivot for the generated sprite.  This follows the same convention as regular sprites, with an additional "SVG Origin" pivot value.  When using "SVG Origin", the pivot will be the (0,0) position of the SVG document.

The tessellation settings can be provided in two ways: **Basic** or **Advanced**.  When using **Basic**, you only need to provide a **Target Resolution** and a **Zoom Factor**.  The importer will automatically configure the advanced settings to make sure your SVG document will render at a high enough tessellation for that resolution at that zoom factor.

![SVG importer advanced properties](images/svg_inspector_advanced.png)

<a name="advanced-importer-settings"></a>If you want full control over the tessellation of the SVG document, you can specify the following advanced settings:

**Step Distance**: Distance at which vertices will be generated along the paths. Lower values will result in a more dense tesselation.

**Sampling Steps**: Number of samples evaluated on paths.  More samples may lead to more precise curves, especially when they have sharp corners.

**Max Cord Deviation**: When enabled, specifies the distance on the cord to a straight line between two points after which more tesselation will be generated.

**Max Tangent Angle**: When enabled, specifies the max tangent angle (in degrees) after which more tesselation will be generated.

![Max Cord](images/constraints.png) 

The curves will subdivide for as long as every enabled constraints aren't satisfied.

The **Sprite Editor** is also available and works exactly the same way as regular sprite assets.

## Vector Graphics API

Classes and methods are provided to work with vector data directly in code.  The SVG importer internally uses these APIs to generate and tessellate the resulting sprites.  The same APIs are available to users.

The whole API is designed as a set of simple classes and structures that holds the vector data together.  This is accompanied by static methods to manipulate and transform this data.

At the core of the Vector Graphics package lives the `Scene` class, which stores a graph of vector objects.  Its `root` property is an instance of `SceneNode`, which contains a list of drawable items, a list of child nodes, a transform and a clipper (see [clipping](#clipping)).

```
public class SceneNode
{
    public List<SceneNode> children { get; set; }
    public List<IDrawable> drawables { get; set; }
    public Matrix2D transform { get; set; }
    public SceneNode clipper { get; set; }
}
```

There are two main kind of drawable instances: paths and shapes.

### Paths

Paths are drawables that are defined by a `BezierContour`, which contains an array of `BezierPathSegment` and a flag that tells if the contour is closed or not.

```
public class Path : IDrawable
{
    public BezierContour contour { get; set; }
    public PathProperties pathProps { get; set; }
}
    
public struct BezierContour
{
    public BezierPathSegment[] segments { get; set; }
    public bool closed { get; set; }
}

public struct BezierPathSegment
{
    public Vector2 p0;
    public Vector2 p1;
    public Vector2 p2;
}
```

The `BezierPathSegment` is used to define a chain of cubic Bézier curves. For a given segment, only the first point `p0` and the two control points `p1` and `p2` are specified.  The `p0` value of the next segment in the array is used to complete the curve. So, you will always need at least two segments to define a valid `BezierContour.  Using this approach allows the chaining of multiple segments and guarantees the continuity of the curve.  For example, consider this path:

![Contour](images/contour.png)

This path could be constructed like so:

```
var s = new BezierPathSegment[] {
	new BezierPathSegment() { p0 = a, p1 = b, p2 = c },
	new BezierPathSegment() { p0 = d, p1 = e, p2 = f },
	new BezierPathSegment() { p0 = g }
};

var path = new Path() {
	contour = new BezierContour() {
		segments = s,
		closed = false
	},
	pathProps = new PathProperties() {
		stroke = new Stroke() { color = Color.red, halfThickness = 1.0f }
	}
};
```

### Shapes

Just like paths, shapes are defined by a `BezierContour`, but they also provide a filling method:

```
public class Shape : Filled
{
    public BezierContour[] contours { get; set; }
}

public abstract class Filled : IDrawable
{
    public IFill fill { get; set; }
    public Matrix2D fillTransform { get; set; }
    public PathProperties pathProps { get; set; }
}
```

There are several classes that implements the `IFill` interface: 

 * `SolidFill` for a simple colored fillings
 * `TextureFill` for a texture fillings
 * `GradientFill` for linear or radial gradient fillings


**Gradients**

Gradient fills are defined by a type (`Linear` or `Radial`) and a series of colors/percentage pairs, called `stops`:

```
public class GradientFill : IFill
{
    public GradientFillType type { get; set; }
    public GradientStop[] stops { get; set; }
    public FillMode mode { get; set; }
    public AddressMode addressing { get; set; }
    public Vector2 radialFocus { get; set; }
}

public struct GradientStop
{
    public Color color { get; set; }
    public float stopPercentage { get; set; }
}
```

Consider the following linear fill, as well as the `GradientFill` instance to generate it:

![Linear Fill](images/linear_gradient.png)

```
var fill = new GradientFill() {
	type = GradientFillType.Linear,
	stops = new GradientFillStop[] {
		new GradientFillStop() { color = Color.blue, stopPercentage = 0.0f },
		new GradientFillStop() { color = Color.red, stopPercentage = 0.5f },
		new GradientFillStop() { color = Color.yellow, stopPercentage = 1.0f }
	}
};
```

The gradient addressing modes define how the colors will be chosen when the gradient coordinates fall outside of the range, as illustrated here:

![Addressing Modes](images/addressing.png)

**Fill Mode**

The filling classes also provide a fill mode, which determines how holes are defined inside the shapes.

`FillMode.NonZero` determines which points are inside shape by intersecting the contour segments with an horizontal line. The direction of the contour determines if the points are inside or outside the shape:

![NonZero Fill](images/fill_nonzero.png)

`FillMode.OddEven` also works by intersecting the segments with an horizontal line. Points inside the shape occur when an even number of segments is crossed, and points outside the shape occur when an odd number of segments is crossed:

![EvenOdd Fill](images/fill_evenodd.png)

### <a name="clipping"></a> Clipping

The `SceneNode` class has a `clipper` member which will clip the content of the node.

![Clipper](images/clipper.png)

In the above example, the repeating square shapes are clipped by an ellipse. In code, this can be done like so:

```
var ellipse = new SceneNode() {
	drawables = new List<IDrawable> { VectorUtils.MakeEllipse(ellipse, Vector2.zero, 50, 100) }
};

var squaresPattern = ...;

var squaresClipped = new SceneNode() {
	children = new List<SceneNode> { squaresPattern },
	clipper = ellipse
};
```

Note that only shapes can act as a clipper (any strokes defined in the clipper will be ignored).  The content being clipped can be any shapes and/or strokes.

*Warning: The clipping process may be an expensive operation. Clipping simple shapes with a simple clipper may perform reasonably, but any complex shape and/or clipper may cause the framerate to drop significantly.*

### Vector Graphics Rendering

The vector graphics elements can be rendered on screen by first getting a tessellated (triangulated) version of the scene.  Once you have a `VectorScene` instance in hand, you can tessellate it using the following `VectorUtils` method:

```
public static List<Geometry> TessellateScene(Scene scene, TesselationOptions options);

```

The `TesselationOptions` are reminiscent of the [advanced importer settings](#advanced-importer-settings) discussed earlier:

```
public struct TesselationOptions
{
    public float stepDistance { get; set; }
    public float maxCordDeviation { get; set; }
    public float maxTanAngleDeviation { get; set; }
    public float samplingStepSize { get; set; }
}
```

Note that `maxTanAngleDeviation` is specified in radians.

To disable the `maxCordDeviation` constraint, set it to `float.MaxValue`.  To disable the `maxTanAngleDeviation` constraint, set it to `Mathf.PI/2.0f`.  Disabling the constraints will make the tessellation faster, but may generate more vertices.

The resulting list of `Geometry` objects contains all the vertices and accompanying information required to render the scene properly.


#### Textures and gradients atlases

If the scene has any texture and/or gradients, you will have to generate a texture atlas and fill the UVs of the geometry.  These methods are part of the `VectorUtils` class:

```
public static TextureAtlas GenerateAtlas(
	IEnumerable<Geometry> geoms, // The geometry generated by the TessellateScene method
	uint rasterSize);            // The desired atlas size (128 is enough for most purposes)


public static void FillUVs(
	IEnumerable<Geometry> geoms, // The geometry for which the UVs will be filled
	TextureAtlas texAtlas);      // The texture atlas generated by the GenerateAtlas method
```

The `GenerateAtlas` method is an expensive operation. Therefore, the resulting `Texture2D` object should be cached whenever possible.  You only need to regenerate the atlas if any texture and/or gradient changed inside the scene.

The `FillUVs` method is cheap, and should be called if the vertices changed inside the geometry.

#### Drawing a tessellated scene

You can render the geometry in several ways. For example:

* Filling a `Mesh` asset
* Building a `Sprite` asset
* Using Unity's low level graphics library

For any of these methods, you should use the provided materials to draw the tessellated vector graphics content. If the scene contains texture and/or gradients, you should use the following material:

```
var mat = new Material(Shader.Find("Unlit/VectorGradient"));
```

Otherwise, you can use:

```
var mat = new Material(Shader.Find("Unlit/Vector"));
```

Filling a mesh asset can be done by the following `VectorUtils` method:

```
public static void FillMesh(
	Mesh mesh,               // The mesh to fill, which will be cleared before filling
	List<Geometry> geoms,    // The geometry resulting from the "TessellateScene" call
	float svgPixelsPerUnit,  // How many "SVG units" should fit in a "Unity unit"
	bool flipYAxis = false); // If true, the Y-axis will point downward
```

Building a sprite asset can be done by the following `VectorUtils` method:

```
public static Sprite BuildSprite(
	List<Geometry> geoms,       // The geometry resulting from the "TesselateScene" call
	float svgPixelsPerUnit,     // How many "SVG units" should fit in a "Unity unit"
	Alignment alignment,        // The sprite alignement
	Vector2 customPivot,        // If alignment is "Custom", this will be used as the custom pivot
	UInt16 gradientResolution); // The resolution used for the gradient texture
```

You can render a sprite to a `Texture2D` by using the following `VectorUtils` method:

```
public static Texture2D RenderSpriteToTexture2D(
	Sprite sprite,          // The sprite to draw
	int width, int height,  // The texture dimensions
	Material mat,           // The material to use (should be Unlit_Vector or Unlit_VectorGradient)
	int antiAliasing = 1);  // The number of samples per pixel
```

We also provide a method to render the generated sprite using immediate mode `GL` commands.  The vertex coordinates to draw in a unit square (between 0 and 1 in both X and Y directions).  This is in the `VectorUtils` class:

```
public static void RenderSprite(
	Sprite sprite,  // The sprite to draw
	Material mat);  // The material to use (should be Unlit_Vector or Unlit_VectorGradient)
```


# Document Revision History

|Date|Reason|
|---|---|
|Mar 20, 2018|Updated public APIs documentation. Matches Vector Graphics version 1.0.3-experimental.|
|Feb 01, 2018|Document created. Matches Vector Graphics version 1.0.2-experimental.|

