using System.Collections.Generic;
using UnityEngine;

/*
 * Layer: UI / presentation
 * Purpose: Draws a simple X/Y scatter plot plus one best-fit line entirely with Unity UI
 * Images inside a RectTransform "plot area" - no charting asset, no world-space LineRenderer.
 * Generic over plain (x,y) points, so it isn't pendulum-specific.
 * Talks to: driven by ResultsGraphUI, which supplies the data points and axis ranges.
 */
public class UIGraphPlotter : MonoBehaviour
{
    public RectTransform plotArea;
    public GameObject pointPrefab;
    public GameObject lineSegmentPrefab;
    [Tooltip("Thickness in pixels of the drawn best-fit line.")]
    public float lineThickness = 3f;

    private readonly List<GameObject> spawned = new List<GameObject>();

    public void Clear()
    {
        foreach (var go in spawned) Destroy(go);
        spawned.Clear();
    }

    public void Plot(IList<Vector2> dataPoints, float xMin, float xMax, float yMin, float yMax, float slope, float intercept)
    {
        Clear();

        foreach (var p in dataPoints)
        {
            Vector2 anchoredPos = ToPlotSpace(p, xMin, xMax, yMin, yMax);
            GameObject dot = Instantiate(pointPrefab, plotArea);
            dot.GetComponent<RectTransform>().anchoredPosition = anchoredPos;
            // pointPrefab is kept disabled in the scene so it never renders as a stray template;
            // the clone must be explicitly re-enabled.
            dot.SetActive(true);
            spawned.Add(dot);
        }

        Vector2 lineStart = ToPlotSpace(new Vector2(xMin, (slope * xMin) + intercept), xMin, xMax, yMin, yMax);
        Vector2 lineEnd = ToPlotSpace(new Vector2(xMax, (slope * xMax) + intercept), xMin, xMax, yMin, yMax);
        DrawLineSegment(lineStart, lineEnd);
    }

    private Vector2 ToPlotSpace(Vector2 dataPoint, float xMin, float xMax, float yMin, float yMax)
    {
        float width = plotArea.rect.width;
        float height = plotArea.rect.height;
        float normalizedX = Mathf.InverseLerp(xMin, xMax, dataPoint.x);
        float normalizedY = Mathf.InverseLerp(yMin, yMax, dataPoint.y);
        return new Vector2(normalizedX * width, normalizedY * height);
    }

    // Draws a thin rotated Image between two anchored points - the standard trick for a
    // "line" made purely out of Unity UI Images, avoiding a world-space LineRenderer inside a
    // screen-space Canvas.
    private void DrawLineSegment(Vector2 from, Vector2 to)
    {
        GameObject segment = Instantiate(lineSegmentPrefab, plotArea);
        RectTransform rt = segment.GetComponent<RectTransform>();

        Vector2 direction = to - from;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = from;
        rt.sizeDelta = new Vector2(distance, lineThickness);
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        // lineSegmentPrefab is kept disabled in the scene as a template; the clone must be
        // explicitly re-enabled.
        segment.SetActive(true);
        spawned.Add(segment);
    }
}
