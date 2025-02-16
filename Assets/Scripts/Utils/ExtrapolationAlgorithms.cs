using UnityEngine;
using System.Collections.Generic;
using System;

namespace Utils
{
public class ExtrapolationAlgorithms
{
    private const int MIN_POINTS = 3;
    private const int CATMULL_MIN_POINTS = 4;

    public static Vector3 ExtrapolatePosition(
        Queue<(Vector3 position, float time)> positionHistory,
        Vector3 currentVelocity,
        float currentTime,
        float lastUpdateTime,
        ExtrapolationType type)
    {
        if (positionHistory.Count < MIN_POINTS)
            return SimpleVelocityExtrapolation(currentVelocity, currentTime - lastUpdateTime, positionHistory);

        switch (type)
        {
            case ExtrapolationType.SimpleVelocity:
                return SimpleVelocityExtrapolation(currentVelocity, currentTime - lastUpdateTime, positionHistory);
            case ExtrapolationType.BezierCurve:
                return BezierExtrapolation(positionHistory, currentTime);
            case ExtrapolationType.CatmullRom:
                return CatmullRomExtrapolation(positionHistory, currentTime);
            default:
                return SimpleVelocityExtrapolation(currentVelocity, currentTime - lastUpdateTime, positionHistory);
        }
    }

    private static Vector3 SimpleVelocityExtrapolation(
        Vector3 currentVelocity,
        float deltaTime,
        Queue<(Vector3 position, float time)> positionHistory)
    {
        var historyArray = positionHistory.ToArray();
        var lastPosition = positionHistory.Count > 0 ? historyArray[historyArray.Length - 1].position : Vector3.zero;
        return lastPosition + currentVelocity * deltaTime;
    }

    private static Vector3 BezierExtrapolation(
        Queue<(Vector3 position, float time)> positionHistory,
        float currentTime)
    {
        if (positionHistory.Count < MIN_POINTS) return positionHistory.Peek().position;

        var historyArray = positionHistory.ToArray();
        int count = historyArray.Length;

        Vector3 p0 = historyArray[count - 3].position;
        Vector3 p1 = historyArray[count - 2].position;
        Vector3 p2 = historyArray[count - 1].position;

        float u = Mathf.Clamp01((currentTime - historyArray[count - 3].time) /
                               (historyArray[count - 1].time - historyArray[count - 3].time));
        float oneMinusU = 1f - u;

        return oneMinusU * oneMinusU * p0 +
               2f * oneMinusU * u * p1 +
               u * u * p2;
    }

    private static Vector3 CatmullRomExtrapolation(
        Queue<(Vector3 position, float time)> positionHistory,
        float currentTime)
    {
        if (positionHistory.Count < CATMULL_MIN_POINTS) return positionHistory.Peek().position;

        var historyArray = positionHistory.ToArray();
        int count = historyArray.Length;

        // Get the normalized time parameter
        float normalizedT = Mathf.Clamp01((currentTime - historyArray[0].time) / 
                                        (historyArray[count - 1].time - historyArray[0].time));
        
        // Convert normalized t to spline parameter
        float p = normalizedT * (count - 1);
        int i = Mathf.Min(Mathf.FloorToInt(p), count - 2);
        float u = p - i;

        // Get control points
        Vector3 p0 = historyArray[Mathf.Max(i - 1, 0)].position;
        Vector3 p1 = historyArray[i].position;
        Vector3 p2 = historyArray[i + 1].position;
        Vector3 p3 = historyArray[Mathf.Min(i + 2, count - 1)].position;

        // Catmull-Rom basis matrix calculation
        return 0.5f * (
            (-p0 + 3f * p1 - 3f * p2 + p3) * (u * u * u)
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * (u * u)
            + (-p0 + p2) * u
            + 2f * p1
        );
    }

}
public enum ExtrapolationType
{
    SimpleVelocity,
    BezierCurve,
    CatmullRom
}

}