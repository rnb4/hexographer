using System;
using System.Collections.Generic;
using Godot;

namespace Hexographer.Core.Hex;

/// <summary>
/// Provides mathematical operations for hex grids including line drawing,
/// distance calculations, and geometric algorithms.
/// </summary>
public static class HexMath
{
    /// <summary>
    /// Calculates the distance between two hex coordinates.
    /// </summary>
    public static int Distance(HexCoord a, HexCoord b)
    {
        return (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.S - b.S)) / 2;
    }

    /// <summary>
    /// Returns all hexes on a line between two coordinates (inclusive).
    /// Uses linear interpolation with hex rounding.
    /// </summary>
    public static IEnumerable<HexCoord> Line(HexCoord a, HexCoord b)
    {
        int n = Distance(a, b);

        if (n == 0)
        {
            yield return a;
            yield break;
        }

        for (int i = 0; i <= n; i++)
        {
            float t = (float)i / n;
            yield return HexLerp(a, b, t);
        }
    }

    /// <summary>
    /// Linearly interpolates between two hex coordinates and rounds to nearest hex.
    /// </summary>
    public static HexCoord HexLerp(HexCoord a, HexCoord b, float t)
    {
        // Add small offset to avoid ambiguous rounding at edges
        const float epsilon = 1e-6f;

        float q = Lerp(a.Q + epsilon, b.Q + epsilon, t);
        float r = Lerp(a.R + epsilon, b.R + epsilon, t);

        return HexLayout.HexRound(q, r);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    /// <summary>
    /// Returns all hexes within a rectangular region defined by two corners.
    /// </summary>
    public static IEnumerable<HexCoord> Rectangle(HexCoord corner1, HexCoord corner2)
    {
        int minQ = Math.Min(corner1.Q, corner2.Q);
        int maxQ = Math.Max(corner1.Q, corner2.Q);
        int minR = Math.Min(corner1.R, corner2.R);
        int maxR = Math.Max(corner1.R, corner2.R);

        for (int q = minQ; q <= maxQ; q++)
        {
            for (int r = minR; r <= maxR; r++)
            {
                yield return new HexCoord(q, r);
            }
        }
    }

    /// <summary>
    /// Returns all hexes in a parallelogram defined by width and height in hex units.
    /// </summary>
    public static IEnumerable<HexCoord> Parallelogram(HexCoord origin, int width, int height)
    {
        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                yield return new HexCoord(origin.Q + q, origin.R + r);
            }
        }
    }

    /// <summary>
    /// Returns all hexes in a triangular region.
    /// </summary>
    public static IEnumerable<HexCoord> Triangle(HexCoord origin, int size)
    {
        for (int q = 0; q <= size; q++)
        {
            for (int r = 0; r <= size - q; r++)
            {
                yield return new HexCoord(origin.Q + q, origin.R + r);
            }
        }
    }

    /// <summary>
    /// Returns all hexes in a hexagonal region (filled circle).
    /// </summary>
    public static IEnumerable<HexCoord> HexagonalRegion(HexCoord center, int radius)
    {
        return center.GetHexesInRange(radius);
    }

    /// <summary>
    /// Rotates a hex coordinate 60 degrees clockwise around the origin.
    /// </summary>
    public static HexCoord RotateClockwise(HexCoord hex)
    {
        return new HexCoord(-hex.R, -hex.S);
    }

    /// <summary>
    /// Rotates a hex coordinate 60 degrees counter-clockwise around the origin.
    /// </summary>
    public static HexCoord RotateCounterClockwise(HexCoord hex)
    {
        return new HexCoord(-hex.S, -hex.Q);
    }

    /// <summary>
    /// Rotates a hex coordinate around a center point by the specified number of 60-degree steps.
    /// Positive steps rotate clockwise.
    /// </summary>
    public static HexCoord RotateAround(HexCoord hex, HexCoord center, int steps)
    {
        // Translate to origin
        var relative = hex - center;

        // Normalize steps to 0-5
        steps = ((steps % 6) + 6) % 6;

        // Apply rotation
        for (int i = 0; i < steps; i++)
        {
            relative = RotateClockwise(relative);
        }

        // Translate back
        return relative + center;
    }

    /// <summary>
    /// Reflects a hex coordinate across the q-axis (through origin).
    /// </summary>
    public static HexCoord ReflectQ(HexCoord hex)
    {
        return new HexCoord(hex.Q, hex.S);
    }

    /// <summary>
    /// Reflects a hex coordinate across the r-axis (through origin).
    /// </summary>
    public static HexCoord ReflectR(HexCoord hex)
    {
        return new HexCoord(hex.S, hex.R);
    }

    /// <summary>
    /// Reflects a hex coordinate across the s-axis (through origin).
    /// </summary>
    public static HexCoord ReflectS(HexCoord hex)
    {
        return new HexCoord(hex.R, hex.Q);
    }

    /// <summary>
    /// Finds the direction (0-5) from one hex to its neighbor.
    /// Returns -1 if the hexes are not neighbors.
    /// </summary>
    public static int GetDirection(HexCoord from, HexCoord to)
    {
        var diff = to - from;

        if (diff == new HexCoord(1, 0)) return 0;   // East
        if (diff == new HexCoord(1, -1)) return 1;  // Northeast
        if (diff == new HexCoord(0, -1)) return 2;  // Northwest
        if (diff == new HexCoord(-1, 0)) return 3;  // West
        if (diff == new HexCoord(-1, 1)) return 4;  // Southwest
        if (diff == new HexCoord(0, 1)) return 5;   // Southeast

        return -1; // Not neighbors
    }

    /// <summary>
    /// Returns the opposite direction (rotated 180 degrees).
    /// </summary>
    public static int OppositeDirection(int direction)
    {
        return (direction + 3) % 6;
    }

    /// <summary>
    /// Performs flood fill starting from a hex, returning all connected hexes
    /// that satisfy the given predicate.
    /// </summary>
    public static IEnumerable<HexCoord> FloodFill(
        HexCoord start,
        Func<HexCoord, bool> canFill,
        int maxDistance = int.MaxValue)
    {
        var visited = new HashSet<HexCoord>();
        var queue = new Queue<(HexCoord Coord, int Distance)>();

        if (!canFill(start))
            yield break;

        queue.Enqueue((start, 0));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, distance) = queue.Dequeue();
            yield return current;

            if (distance >= maxDistance)
                continue;

            foreach (var neighbor in current.GetNeighbors())
            {
                if (visited.Contains(neighbor))
                    continue;

                if (!canFill(neighbor))
                    continue;

                visited.Add(neighbor);
                queue.Enqueue((neighbor, distance + 1));
            }
        }
    }

    /// <summary>
    /// Checks if a hex is visible from another hex using line-of-sight.
    /// Uses a predicate to determine if a hex blocks vision.
    /// </summary>
    public static bool HasLineOfSight(HexCoord from, HexCoord to, Func<HexCoord, bool> blocksVision)
    {
        foreach (var hex in Line(from, to))
        {
            if (hex != from && hex != to && blocksVision(hex))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns all hexes visible from a point within a given range.
    /// </summary>
    public static IEnumerable<HexCoord> FieldOfView(
        HexCoord origin,
        int range,
        Func<HexCoord, bool> blocksVision)
    {
        var visible = new HashSet<HexCoord> { origin };

        foreach (var target in origin.GetHexesInRange(range))
        {
            if (target == origin)
                continue;

            if (HasLineOfSight(origin, target, blocksVision))
            {
                visible.Add(target);
            }
        }

        return visible;
    }

    /// <summary>
    /// A* pathfinding between two hexes.
    /// Returns null if no path exists.
    /// </summary>
    public static List<HexCoord>? FindPath(
        HexCoord start,
        HexCoord goal,
        Func<HexCoord, bool> isPassable,
        Func<HexCoord, HexCoord, int>? moveCost = null)
    {
        moveCost ??= (_, _) => 1;

        var openSet = new PriorityQueue<HexCoord, int>();
        var cameFrom = new Dictionary<HexCoord, HexCoord>();
        var gScore = new Dictionary<HexCoord, int> { [start] = 0 };

        openSet.Enqueue(start, Distance(start, goal));

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current == goal)
            {
                // Reconstruct path
                var path = new List<HexCoord> { current };
                while (cameFrom.ContainsKey(current))
                {
                    current = cameFrom[current];
                    path.Add(current);
                }
                path.Reverse();
                return path;
            }

            foreach (var neighbor in current.GetNeighbors())
            {
                if (!isPassable(neighbor))
                    continue;

                int tentativeG = gScore[current] + moveCost(current, neighbor);

                if (!gScore.TryGetValue(neighbor, out int currentG) || tentativeG < currentG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    int fScore = tentativeG + Distance(neighbor, goal);
                    openSet.Enqueue(neighbor, fScore);
                }
            }
        }

        return null; // No path found
    }

    /// <summary>
    /// Calculates the bounding box of a set of hex coordinates.
    /// </summary>
    public static (HexCoord Min, HexCoord Max) GetBounds(IEnumerable<HexCoord> hexes)
    {
        int minQ = int.MaxValue, maxQ = int.MinValue;
        int minR = int.MaxValue, maxR = int.MinValue;

        foreach (var hex in hexes)
        {
            minQ = Math.Min(minQ, hex.Q);
            maxQ = Math.Max(maxQ, hex.Q);
            minR = Math.Min(minR, hex.R);
            maxR = Math.Max(maxR, hex.R);
        }

        return (new HexCoord(minQ, minR), new HexCoord(maxQ, maxR));
    }

    /// <summary>
    /// Wraps a hex coordinate within a rectangular map of given dimensions.
    /// Useful for wraparound maps.
    /// </summary>
    public static HexCoord Wrap(HexCoord hex, int width, int height)
    {
        int q = ((hex.Q % width) + width) % width;
        int r = ((hex.R % height) + height) % height;
        return new HexCoord(q, r);
    }
}
