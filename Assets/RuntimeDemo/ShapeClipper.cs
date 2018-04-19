using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using Unity.VectorGraphics;

#if UNITY_EDITOR
using UnityEditor;
using Unity.VectorGraphics.Editor;
#endif

[ExecuteInEditMode]
public class ShapeClipper : MonoBehaviour
{
    public Transform clipperPosition;
    public float borderWidth = 1.0f;
    public bool clip;

    private Scene m_Scene;
    private Rectangle m_Rectangle;
    private SceneNode m_Clipper;
    private VectorUtils.TessellationOptions m_Options;
    private VectorUtils.TextureAtlas m_TexAtlas;
    private Mesh m_Mesh;

    void OnEnable()
    {
        // Build the vector scene, which consist of a gradient-fill on a rectangle,
        // clipped by a circle shape.
        m_Clipper = new SceneNode()
        {
            Transform = Matrix2D.identity,
            Drawables = new List<IDrawable> { VectorUtils.MakeCircle(Vector2.zero, 4.0f) }
        };

        var gradientFill = new GradientFill() {
            Type = GradientFillType.Radial,
            Addressing = AddressMode.Mirror,
            Stops = new GradientStop[] {
                new GradientStop() { Color = Color.red, StopPercentage = 0.05f },
                new GradientStop() { Color = Color.blue, StopPercentage = 0.95f }
            }
        };

        m_Rectangle = new Rectangle()
        {
            Position = Vector2.zero,
            Size = new Vector2(10.0f, 10.0f),
            Fill = gradientFill,
            PathProps = new PathProperties()
            {
                Stroke = new Stroke() { Color = Color.red }
            },
            FillTransform = Matrix2D.identity
        };

        m_Scene = new Scene()
        {
            Root = new SceneNode()
            {
                Drawables = new List<IDrawable> { m_Rectangle },
                Transform = Matrix2D.identity
            }
        };

        m_Options = new VectorUtils.TessellationOptions()
        {
            StepDistance = 1.0f,
            MaxCordDeviation = float.MaxValue,
            MaxTanAngleDeviation = Mathf.PI / 2.0f,
            SamplingStepSize = 0.01f
        };

#if UNITY_EDITOR
        // We're in editor, use this opportunity to pre-generate the
        // sprite atlas, and store it in an asset.
        var geoms = VectorUtils.TessellateScene(m_Scene, m_Options);
        m_TexAtlas = VectorUtils.GenerateAtlas(geoms, 128);

        var atlasPath = "Assets/RuntimeDemo/Materials/ShapeClipperAtlas.asset";
        AssetDatabase.CreateAsset(m_TexAtlas.Texture, atlasPath);

        // Assign the atlas to the material.
        // The material should use the "Unlit/VectorGradient" shader.
        GetComponent<MeshRenderer>().sharedMaterial.mainTexture = AssetDatabase.LoadMainAssetAtPath(atlasPath) as Texture2D;
#endif

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
        VectorUtils.FillUVs(geoms, m_TexAtlas);
        VectorUtils.FillMesh(m_Mesh, geoms, 1.0f);
    }
}
