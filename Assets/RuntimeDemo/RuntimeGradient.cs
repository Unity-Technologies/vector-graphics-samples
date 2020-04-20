using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VectorGraphics;

public class RuntimeGradient : MonoBehaviour
{
    void Start()
    {
        var tessOptions = new VectorUtils.TessellationOptions() {
            StepDistance = 100.0f,
            MaxCordDeviation = 0.5f,
            MaxTanAngleDeviation = 0.1f,
            SamplingStepSize = 0.01f
        };

        // Prepare the fill
        var fill = new GradientFill() {
            Type = GradientFillType.Linear,
            Stops = new GradientStop[] {
                new GradientStop() { Color = Color.blue, StopPercentage = 0.0f },
                new GradientStop() { Color = Color.red, StopPercentage = 1.0f },
            }
        };

        // Build the scene
        var cornerRad = new Vector2(10,10);
        var rect = VectorUtils.BuildRectangleContour(new Rect(0, 0, 100, 100), cornerRad, cornerRad, cornerRad, cornerRad);
        var scene = new Scene() {
            Root = new SceneNode() {
                Shapes = new List<Shape> {
                    new Shape() {
                        Contours = new BezierContour[] { rect },
                        Fill = fill
                    }
                }
            }
        };

        // Dynamically import the SVG data, and tessellate the resulting vector scene.
        var geoms = VectorUtils.TessellateScene(scene, tessOptions);
        
        // Build a sprite with the tessellated geometry.
        var sprite = VectorUtils.BuildSprite(geoms, 10.0f, VectorUtils.Alignment.Center, Vector2.zero, 16, true);
        GetComponent<SpriteRenderer>().sprite = sprite;
    }

    void OnDisable()
    {
        GameObject.Destroy(GetComponent<SpriteRenderer>().sprite);
    }
}
