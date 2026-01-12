using System;
using System.Collections.Generic;

namespace Hexographer.Core.Hex;

/// <summary>
/// Immutable struct representing a hex coordinate in axial form (q, r).
/// The third cube coordinate s is derived as s = -q - r.
/// </summary>
public readonly struct HexCoord : IEquatable<HexCoord>
{
    /// <summary>Column coordinate (axial q, cube q)</summary>
    public readonly int Q;

    /// <summary>Row coordinate (axial r, cube r)</summary>
    public readonly int R;

    /// <summary>Derived cube coordinate: s = -q - r</summary>
    public int S => -Q - R;

    public HexCoord(int q, int r)
    {
        Q = q;
        R = r;
    }

    /// <summary>
    /// Creates a HexCoord from cube coordinates.
    /// Validates that q + r + s = 0.
    /// </summary>
    public static HexCoord FromCube(int q, int r, int s)
    {
        if (q + r + s != 0)
            throw new ArgumentException($"Cube coordinates must satisfy q + r + s = 0. Got q={q}, r={r}, s={s}");
        return new HexCoord(q, r);
    }

    /// <summary>
    /// Creates a HexCoord from offset coordinates (column, row).
    /// </summary>
    /// <param name="col">Column in offset grid</param>
    /// <param name="row">Row in offset grid</param>
    /// <param name="orientation">Hex orientation</param>
    /// <param name="offsetType">Whether odd or even columns/rows are offset</param>
    public static HexCoord FromOffset(int col, int row, HexOrientation orientation, OffsetType offsetType = OffsetType.Odd)
    {
        int offset = offsetType == OffsetType.Odd ? 1 : 0;

        if (orientation == HexOrientation.FlatTop)
        {
            // Odd-q or even-q offset
            int q = col;
            int r = row - (col + offset * (col & 1)) / 2;
            return new HexCoord(q, r);
        }
        else
        {
            // Odd-r or even-r offset
            int q = col - (row + offset * (row & 1)) / 2;
            int r = row;
            return new HexCoord(q, r);
        }
    }

    /// <summary>
    /// Converts this HexCoord to offset coordinates.
    /// </summary>
    public (int Col, int Row) ToOffset(HexOrientation orientation, OffsetType offsetType = OffsetType.Odd)
    {
        int offset = offsetType == OffsetType.Odd ? 1 : 0;

        if (orientation == HexOrientation.FlatTop)
        {
            int col = Q;
            int row = R + (Q + offset * (Q & 1)) / 2;
            return (col, row);
        }
        else
        {
            int col = Q + (R + offset * (R & 1)) / 2;
            int row = R;
            return (col, row);
        }
    }

    /// <summary>
    /// Returns the 6 neighboring hex coordinates.
    /// </summary>
    public IEnumerable<HexCoord> GetNeighbors()
    {
        return new[]
        {
            new HexCoord(Q + 1, R),      // East
            new HexCoord(Q + 1, R - 1),  // Northeast
            new HexCoord(Q, R - 1),      // Northwest
            new HexCoord(Q - 1, R),      // West
            new HexCoord(Q - 1, R + 1),  // Southwest
            new HexCoord(Q, R + 1)       // Southeast
        };
    }

    /// <summary>
    /// Gets the neighbor in the specified direction (0-5).
    /// Direction 0 is East, incrementing counter-clockwise.
    /// </summary>
    public HexCoord GetNeighbor(int direction)
    {
        direction = ((direction % 6) + 6) % 6; // Normalize to 0-5
        return direction switch
        {
            0 => new HexCoord(Q + 1, R),
            1 => new HexCoord(Q + 1, R - 1),
            2 => new HexCoord(Q, R - 1),
            3 => new HexCoord(Q - 1, R),
            4 => new HexCoord(Q - 1, R + 1),
            5 => new HexCoord(Q, R + 1),
            _ => this
        };
    }

    /// <summary>
    /// Calculates the distance between this hex and another.
    /// </summary>
    public int DistanceTo(HexCoord other)
    {
        return (Math.Abs(Q - other.Q) + Math.Abs(R - other.R) + Math.Abs(S - other.S)) / 2;
    }

    /// <summary>
    /// Returns all hexes within the specified radius (inclusive).
    /// </summary>
    public IEnumerable<HexCoord> GetHexesInRange(int radius)
    {
        for (int q = -radius; q <= radius; q++)
        {
            int r1 = Math.Max(-radius, -q - radius);
            int r2 = Math.Min(radius, -q + radius);
            for (int r = r1; r <= r2; r++)
            {
                yield return new HexCoord(Q + q, R + r);
            }
        }
    }

    /// <summary>
    /// Returns all hexes at exactly the specified distance (ring).
    /// </summary>
    public IEnumerable<HexCoord> GetRing(int radius)
    {
        if (radius == 0)
        {
            yield return this;
            yield break;
        }

        var current = new HexCoord(Q - radius, R + radius); // Start at southwest corner

        for (int direction = 0; direction < 6; direction++)
        {
            for (int step = 0; step < radius; step++)
            {
                yield return current;
                current = current.GetNeighbor(direction);
            }
        }
    }

    /// <summary>
    /// Returns hexes in a spiral pattern from center outward.
    /// </summary>
    public IEnumerable<HexCoord> GetSpiral(int radius)
    {
        yield return this;
        for (int r = 1; r <= radius; r++)
        {
            foreach (var hex in GetRing(r))
            {
                yield return hex;
            }
        }
    }

    public static HexCoord operator +(HexCoord a, HexCoord b) => new(a.Q + b.Q, a.R + b.R);
    public static HexCoord operator -(HexCoord a, HexCoord b) => new(a.Q - b.Q, a.R - b.R);
    public static HexCoord operator *(HexCoord a, int scale) => new(a.Q * scale, a.R * scale);
    public static bool operator ==(HexCoord a, HexCoord b) => a.Q == b.Q && a.R == b.R;
    public static bool operator !=(HexCoord a, HexCoord b) => !(a == b);

    public bool Equals(HexCoord other) => Q == other.Q && R == other.R;
    public override bool Equals(object? obj) => obj is HexCoord other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Q, R);
    public override string ToString() => $"Hex({Q}, {R})";

    public static readonly HexCoord Zero = new(0, 0);
}

/// <summary>
/// Specifies whether odd or even columns/rows are offset in offset coordinate systems.
/// </summary>
public enum OffsetType
{
    Odd,
    Even
}
