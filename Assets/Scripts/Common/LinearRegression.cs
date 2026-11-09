using System.Collections.Generic;
using UnityEngine;

/*
 * Layer: Common utility (practical-agnostic)
 * Purpose: Ordinary least-squares linear fit y = slope * x + intercept over a set of points.
 * Used by the pendulum's T^2-vs-L analysis, but has no pendulum-specific knowledge so any
 * future practical needing a best-fit line/slope can reuse it directly.
 */
public static class LinearRegression
{
    public static void Fit(IList<Vector2> points, out float slope, out float intercept)
    {
        int n = points.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumXX = 0;
        for (int i = 0; i < n; i++)
        {
            sumX += points[i].x;
            sumY += points[i].y;
            sumXY += points[i].x * points[i].y;
            sumXX += points[i].x * points[i].x;
        }

        double denominator = (n * sumXX) - (sumX * sumX);
        if (n < 2 || Mathf.Approximately((float)denominator, 0f))
        {
            slope = 0f;
            intercept = n > 0 ? (float)(sumY / n) : 0f;
            return;
        }

        slope = (float)(((n * sumXY) - (sumX * sumY)) / denominator);
        intercept = (float)((sumY - (slope * sumX)) / n);
    }
}
