using Godot;
using System;

namespace Hexographer.Editor.UI;

/// <summary>
/// Handles pan and zoom control for the map viewport.
/// Attach to a Camera2D inside the SubViewport.
/// </summary>
public partial class ViewportController : Camera2D
{
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 4.0f;
    private const float ZoomStep = 0.1f;

    private bool _isPanning;
    private bool _spaceHeld;
    private Vector2 _panStartMousePos;
    private Vector2 _panStartCameraPos;

    /// <summary>
    /// Event fired when zoom level changes.
    /// </summary>
    public event Action<float>? ZoomChanged;

    /// <summary>
    /// Current zoom level (1.0 = 100%).
    /// </summary>
    public float ZoomLevel { get; private set; } = 1.0f;

    public override void _Ready()
    {
        // Ensure camera starts centered
        Position = Vector2.Zero;
        Zoom = Vector2.One;
    }

    public override void _Input(InputEvent @event)
    {
        // Handle space key for pan mode
        if (@event is InputEventKey key)
        {
            if (key.Keycode == Key.Space)
            {
                _spaceHeld = key.Pressed;
                if (!key.Pressed && _isPanning)
                {
                    _isPanning = false;
                }
            }
        }

        // Handle mouse input for pan and zoom
        if (@event is InputEventMouseButton mouseButton)
        {
            HandleMouseButton(mouseButton);
        }
        else if (@event is InputEventMouseMotion mouseMotion && _isPanning)
        {
            HandlePan(mouseMotion);
        }
    }

    private void HandleMouseButton(InputEventMouseButton mouseButton)
    {
        switch (mouseButton.ButtonIndex)
        {
            // Middle mouse button for pan
            case MouseButton.Middle:
                if (mouseButton.Pressed)
                {
                    StartPan(mouseButton.Position);
                }
                else
                {
                    _isPanning = false;
                }
                break;

            // Left mouse + space for pan
            case MouseButton.Left when _spaceHeld:
                if (mouseButton.Pressed)
                {
                    StartPan(mouseButton.Position);
                }
                else
                {
                    _isPanning = false;
                }
                break;

            // Mouse wheel for zoom
            case MouseButton.WheelUp:
                if (mouseButton.Pressed)
                {
                    ZoomAt(mouseButton.Position, ZoomStep);
                }
                break;

            case MouseButton.WheelDown:
                if (mouseButton.Pressed)
                {
                    ZoomAt(mouseButton.Position, -ZoomStep);
                }
                break;
        }

        // Double-click middle mouse to reset view
        if (mouseButton.ButtonIndex == MouseButton.Middle &&
            mouseButton.DoubleClick)
        {
            ResetView();
        }
    }

    private void StartPan(Vector2 mousePos)
    {
        _isPanning = true;
        _panStartMousePos = mousePos;
        _panStartCameraPos = Position;
    }

    private void HandlePan(InputEventMouseMotion mouseMotion)
    {
        // Calculate delta in screen space, then convert to world space
        var delta = mouseMotion.Position - _panStartMousePos;
        // Divide by zoom to convert screen movement to world movement
        Position = _panStartCameraPos - delta / Zoom;
    }

    /// <summary>
    /// Zooms centered on the given screen position.
    /// </summary>
    private void ZoomAt(Vector2 screenPos, float delta)
    {
        var oldZoom = ZoomLevel;
        var newZoom = Mathf.Clamp(ZoomLevel + delta, MinZoom, MaxZoom);

        if (Mathf.IsEqualApprox(oldZoom, newZoom))
            return;

        // Get the world position under the mouse before zoom
        var worldPosBefore = GetGlobalMousePosition();

        // Apply new zoom
        ZoomLevel = newZoom;
        Zoom = new Vector2(ZoomLevel, ZoomLevel);

        // Get the world position under the mouse after zoom
        var worldPosAfter = GetGlobalMousePosition();

        // Adjust position to keep the point under the mouse stationary
        Position += worldPosBefore - worldPosAfter;

        ZoomChanged?.Invoke(ZoomLevel);
    }

    /// <summary>
    /// Sets the zoom level programmatically.
    /// </summary>
    public void SetZoom(float level)
    {
        ZoomLevel = Mathf.Clamp(level, MinZoom, MaxZoom);
        Zoom = new Vector2(ZoomLevel, ZoomLevel);
        ZoomChanged?.Invoke(ZoomLevel);
    }

    /// <summary>
    /// Resets the view to center with default zoom.
    /// </summary>
    public void ResetView()
    {
        Position = Vector2.Zero;
        ZoomLevel = 1.0f;
        Zoom = Vector2.One;
        ZoomChanged?.Invoke(ZoomLevel);
    }

    /// <summary>
    /// Gets the minimum zoom level.
    /// </summary>
    public float GetMinZoom() => MinZoom;

    /// <summary>
    /// Gets the maximum zoom level.
    /// </summary>
    public float GetMaxZoom() => MaxZoom;
}
