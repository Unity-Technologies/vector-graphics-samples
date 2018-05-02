# About Vector Graphics

The Vector Graphics package provides an SVG importer as well as generic vector graphics APIs.

## Requirements

This Vector Graphics package version 1.0.0 is compatible with the following versions of the Unity Editor:
2018.1 and later (recommended)

## Known limitations

The SVG importer in this package implements a subset of the SVG 1.1 specification, with some limitations:

* Text elements are not yet supported [(SVG 1.1 section 10)](https://www.w3.org/TR/SVG11/text.html)
* Per-pixel masking is not supported [(SVG 1.1 section 14.4)](https://www.w3.org/TR/SVG11/masking.html#Masking)
* Filter effects are not supported [(SVG 1.1 section 15)](https://www.w3.org/TR/SVG11/filters.html)
* Any interactivity feature are not supported [(SVG 1.1 section 16)](https://www.w3.org/TR/SVG11/interact.html)
* Animations are not supported [(SVG 1.1 section 19)](https://www.w3.org/TR/SVG11/animate.html)

# Using Vector Graphics

## SVG importer

This package provides an SVG importer that reads and interprets SVG documents and generates 2D sprites for use in Unity.

You import SVG files into the Unity Editor like any other assets. Either drop them directly into the Assets folder in the Projects window, or select `Assets > Import New Asset` from the menu bar. When imported, you can instantiate the resulting assets in the Hierarchy View or the Scene View.

|**Property**|**Function**|
|:-----------|:-----------|
|**Pixels Per Unit**|Number of SVG units that correspond to 1 scene unit.|
|**Tessellation Step Distance**|Distance at which Unity generates triangles when tessellating the paths. A smaller step distance will result in smoother curves at the expense of more triangles.|
|**Gradient Resolution**|Texture size used to store gradients.|
|**Pivot**|Location of the pivot for the generated sprite. This follows the same convention as regular sprites, with an additional SVG Origin pivot value. When using SVG Origin, the pivot is the (0,0) position of the SVG document.|


You can provide tessellation settings in two ways: **Basic** or **Advanced**. 

### Basic tessellation

![SVG importer properties](images/svg_inspector.png)

When using **Basic**, you only need to provide a **Target Resolution** and a **Zoom Factor**. Then the importer automatically configures the advanced settings to make sure your SVG document renders at a high enough tessellation for that resolution at that zoom factor.

<a name="advanced-importer-settings"></a>
### Advanced tessellation

![SVG importer properties](images/svg_inspector_advanced.png)

If you want full control over the tessellation of the SVG document, you can specify the following advanced settings:

|**Property**|**Function**|
|:-----------|:-----------|
|**Step Distance**|Distance at which the importer generates vertices along the paths. Lower values result in a more dense tessellation.|
|**Sampling Steps**|Number of samples the importer evaluates on paths. More samples may result in more precise curves, especially when the curves have sharp corners.|
|**Max Cord Deviation**|Distance on the cord to a straight line between two points after which the importer generates more tessellation.|
|**Max Tangent Angle**|Maximum tangent angle (in degrees) after which the importer generates tessellation.|

![Max Cord](images/constraints.png) 

The importer subdivides curves for as long as every enabled constraint isn't satisfied.

The **Sprite Editor** is also available and works exactly the same way as regular sprite assets.

## Vector Graphics API

The provided classes and methods enable you to work with vector data directly in code. The SVG importer uses these APIs internally to generate and tessellate the resulting sprites. 

The Vector Graphics API is a set of simple classes and structures that holds the vector data together. This is accompanied by static methods to manipulate and transform this data.

At the core of the Vector Graphics package is the `Scene` class, which stores a graph of vector objects. Its `Root` property is an instance of `SceneNode`, which contains a list of drawable items, a list of child nodes, a transform and a clipper (see [clipping](#clipping)).

```
public class SceneNode
{
    public List<SceneNode> Children { get; set; }
    public List<IDrawable> Drawables { get; set; }
    public Matrix2D Transform { get; set; }
    public SceneNode Clipper { get; set; }
}
```

There are two main kind of drawable instances: paths and shapes.

### Paths

Paths are drawables that are defined by a `BezierContour`. A `BezierContour` contains a `BezierPathSegment` array and a flag that indicates whether the contour is closed or not.

```
public class Path : IDrawable
{
    public BezierContour Contour { get; set; }
    public PathProperties PathProps { get; set; }
}
    
public struct BezierContour
{
    public BezierPathSegment[] Segments { get; set; }
    public bool Closed { get; set; }
}

public struct BezierPathSegment
{
    public Vector2 P0;
    public Vector2 P1;
    public Vector2 P2;
}
```

The `BezierPathSegment` array defines a chain of cubic Bézier curves. The above segment specifies only the first point, `P0`, and two control points, `P1` and `P2`. The `Path` class uses the `P0` value of the next segment in the array to complete the curve. So, you will always need at least two segments to define a valid `BezierContour`. Using this approach allows the chaining of multiple segments and guarantees the continuity of the curve. For example, consider this path:

![Contour](images/contour.png)

You could construct this path like so:

```
var segments = new BezierPathSegment[] {
	new BezierPathSegment() { P0 = a, P1 = b, P2 = c },
	new BezierPathSegment() { P0 = d, P1 = e, P2 = f },
	new BezierPathSegment() { P0 = g }
};

var path = new Path() {
	contour = new BezierContour() {
		Segments = segments,
		Closed = false
	},
	pathProps = new PathProperties() {
		Stroke = new Stroke() { Color = Color.red, HalfThickness = 1.0f }
	}
};
```

### Shapes

Just like paths, shapes are defined by a `BezierContour`, but they also provide a filling method:

```
public class Shape : Filled
{
    public BezierContour[] Contours { get; set; }
}

public abstract class Filled : IDrawable
{
    public IFill Fill { get; set; }
    public Matrix2D FillTransform { get; set; }
    public PathProperties PathProps { get; set; }
}
```

Several classes implement the `IFill` interface:

* `SolidFill` for a simple colored fillings
* `TextureFill` for a texture fillings
* `GradientFill` for linear or radial gradient fillings

### Gradients

Gradient fills are defined by a `Linear` or `Radial` type and a series of colors/percentage pairs, called *stops*:

```
public class GradientFill : IFill
{
    public GradientFillType Type { get; set; }
    public GradientStop[] Stops { get; set; }
    public FillMode Mode { get; set; }
    public AddressMode Addressing { get; set; }
    public Vector2 RadialFocus { get; set; }
}

public struct GradientStop
{
    public Color Color { get; set; }
    public float StopPercentage { get; set; }
}
```

Consider the following linear fill, as well as the `GradientFill` instance to generate it:

![Linear Fill](images/linear_gradient.png)

```
var fill = new GradientFill() {
	Type = GradientFillType.Linear,
	Stops = new GradientFillStop[] {
		new GradientFillStop() { Color = Color.blue, StopPercentage = 0.0f },
		new GradientFillStop() { Color = Color.red, StopPercentage = 0.5f },
		new GradientFillStop() { Color = Color.yellow, StopPercentage = 1.0f }
	}
};
```

The gradient addressing modes define how Unity displays the color when the gradient coordinates fall outside of the range, as illustrated here:

![Addressing Modes](images/addressing.png)


### Fill Mode

The filling classes also provide a fill mode, which determines how holes are defined inside the shapes.

`FillMode.NonZero` determines which points are inside a shape by intersecting the contour segments with an horizontal line. The direction of the contour determines whether the points are inside or outside the shape:

![NonZero Fill](images/fill_nonzero.png)

`FillMode.OddEven` also works by intersecting the segments with an horizontal line. Points inside the shape occur when an even number of segments is crossed, and points outside the shape occur when an odd number of segments is crossed:

![EvenOdd Fill](images/fill_evenodd.png)

<a name="clipping"></a>
### Clipping

The `SceneNode` class has a clipper member which will clip the content of the node.

![Clipper](images/clipper.png)

In the above example, the repeating square shapes are clipped by an ellipse. In code, this can be done like so:

```
var ellipse = new SceneNode() {
	Drawables = new List<IDrawable> { VectorUtils.MakeEllipse(ellipse, Vector2.zero, 50, 100) }
};

var squaresPattern = ...;

var squaresClipped = new SceneNode() {
	Children = new List<SceneNode> { squaresPattern },
	Clipper = ellipse
};
```

Note that only shapes can act as a clipper (the clipping process ignores any strokes defined in the clipper). The content being clipped can be any shapes and/or strokes.

*Warning: The clipping process can be an expensive operation. Clipping simple shapes with a simple clipper may perform reasonably, but any complex shape and/or clipper may cause the frame rate to drop significantly.*

### Rendering vector graphics

To render vector graphics elements on screen, first get a tessellated (triangulated) version of the scene. When you have a VectorScene instance set up, you can tessellate it using the following `VectorUtils` method:

```
public static List<Geometry> TessellateScene(Scene scene, TesselationOptions options);
```

The `TesselationOptions` are similar to the [advanced importer settings](#advanced-importer-settings):

```
public struct TesselationOptions
{
    public float StepDistance { get; set; }
    public float MaxCordDeviation { get; set; }
    public float MaxTanAngleDeviation { get; set; }
    public float SamplingStepSize { get; set; }
}
```

Note that `maxTanAngleDeviation` is specified in radians.

To disable the `maxCordDeviation` constraint, set it to `float.MaxValue`. To disable the `maxTanAngleDeviation` constraint, set it to `Mathf.PI/2.0f`. Disabling the constraints will make the tessellation faster, but may generate more vertices.

The resulting list of `Geometry` objects contains all the vertices and accompanying information required to render the scene properly.

### Textures and gradients atlases

If the scene has any textures or gradients, you will have to generate a texture atlas and fill the UVs of the geometry. These methods are part of the `VectorUtils` class:

```
public static TextureAtlas GenerateAtlas(
	IEnumerable<Geometry> geoms, // The geometry generated by the TessellateScene method
	uint rasterSize);            // The desired atlas size (128 is enough for most purposes)

public static void FillUVs(
	IEnumerable<Geometry> geoms, // The geometry for which the UVs will be filled
	TextureAtlas texAtlas);      // The texture atlas generated by the GenerateAtlas method
```

The `GenerateAtlas` method is an expensive operation, so cache the resulting Texture2D object whenever possible. You only need to regenerate the atlas when a texture or gradient changes inside the scene.

When vertices change inside the geometry, call the `FillUVs` method, which is cheap.
Drawing a tessellated scene
You can render the geometry in several ways. For example:

* Filling a `Mesh` asset
* Building a `Sprite` asset
* Using Unity's low level graphics library

For any of these methods, use the provided materials to draw the tessellated vector graphics content. If the scene contains textures or gradients, use the following material:

```
var mat = new Material(Shader.Find("Unlit/VectorGradient"));
```

Otherwise, you can use:

```
var mat = new Material(Shader.Find("Unlit/Vector"));
```

To fill a mesh asset, use the following `VectorUtils` method:

```
public static void FillMesh(
	Mesh mesh,               // The mesh to fill, which will be cleared before filling
	List<Geometry> geoms,    // The geometry resulting from the "TessellateScene" call
	float svgPixelsPerUnit,  // How many "SVG units" should fit in a "Unity unit"
	bool flipYAxis = false); // If true, the Y-axis will point downward
```

To build a sprite asset, use the following `VectorUtils` method:

```
public static Sprite BuildSprite(
	List<Geometry> geoms,       // The geometry resulting from the "TesselateScene" call
	float svgPixelsPerUnit,     // How many "SVG units" should fit in a "Unity unit"
	Alignment alignment,        // The sprite alignement
	Vector2 customPivot,        // If alignment is "Custom", this will be used as the custom pivot
	UInt16 gradientResolution); // The resolution used for the gradient texture
```

To render a sprite to a `Texture2D`, use the following `VectorUtils` method:

```
public static Texture2D RenderSpriteToTexture2D(
	Sprite sprite,          // The sprite to draw
	int width, int height,  // The texture dimensions
	Material mat,           // The material to use (should be Unlit_Vector or Unlit_VectorGradient)
	int antiAliasing = 1);  // The number of samples per pixel
```

To render the generated sprite using immediate mode `GL` commands, use the `RenderSprite` method in the `VectorUtils` class to draw the sprite into a unit square (a box between 0 and 1 in both X and Y directions):

```
public static void RenderSprite(
	Sprite sprite,  // The sprite to draw
	Material mat);  // The material to use (should be Unlit_Vector or Unlit_VectorGradient)
```

## Document Revision History

|Date  | Reason |
|:-----|:-------|
|May 2, 2018|Matches Vector Graphics 1.0.0-preview.7|
|Mar 20, 2018|Updated public APIs documentation. Matches Vector Graphics version 1.0.3-experimental.|
|Feb 01, 2018|Document created. Matches Vector Graphics version 1.0.2-experimental.|

