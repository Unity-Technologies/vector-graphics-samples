using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Unity.VectorGraphics;

[ExecuteInEditMode]
public class ShapeClipper : MonoBehaviour
{
    public Transform clipperPosition;
    public float borderWidth = 1.0f;
    public bool clip;

    private Scene m_Scene;
    private Shape m_Rectangle;
    private SceneNode m_Clipper;
    private VectorUtils.TessellationOptions m_Options;
    private VectorUtils.TextureAtlas m_TexAtlas;
    private Mesh m_Mesh;

    void OnEnable()
    {
        // Build the vector scene, which consist of a rectangle, clipped by a circle.
        var circle = new Shape();
        VectorUtils.MakeCircleShape(circle, Vector2.zero, 4.0f);
        m_Clipper = new SceneNode()
        {
            Transform = Matrix2D.identity,
            Shapes = new List<Shape> { circle }
        };

        m_Rectangle = new Shape();
        VectorUtils.MakeRectangleShape(m_Rectangle, new Rect(0, 0, 10, 10));
        m_Rectangle.Fill = new SolidFill() { Color = Color.blue };
        m_Rectangle.PathProps = new PathProperties() {
            Stroke = new Stroke() { Color = Color.red }
        };

        m_Scene = new Scene() {
            Root = new SceneNode() { Shapes = new List<Shape> { m_Rectangle } }
        };

        m_Options = new VectorUtils.TessellationOptions()
        {
            StepDistance = 1.0f,
            MaxCordDeviation = float.MaxValue,
            MaxTanAngleDeviation = Mathf.PI / 2.0f,
            SamplingStepSize = 0.01f
        };

        m_Mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = m_Mesh;
    }

    void Update()
    {
        // Update the thickness and clipper
        m_Rectangle.PathProps.Stroke.HalfThickness = borderWidth / 2;
        m_Scene.Root.Clipper = clip ? m_Clipper : null;

        // Move the clipper position
        m_Clipper.Transform = Matrix2D.Translate(clipperPosition.transform.localPosition);

        // Tessellate the vector scene
        var geoms = VectorUtils.TessellateScene(m_Scene, m_Options);
        VectorUtils.FillMesh(m_Mesh, geoms, 1.0f);
    }
}
