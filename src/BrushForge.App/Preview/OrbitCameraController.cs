using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using BrushForge.Geometry.Bounds;

namespace BrushForge.App.Preview;

/// <summary>
/// Provides dependency-free orbit, zoom, and pan controls for a WPF
/// <see cref="Viewport3D"/> centered on generated brush geometry.
/// </summary>
internal sealed class OrbitCameraController
{
    private const double DefaultYawRadians =
        -Math.PI / 4.0;

    private const double DefaultPitchRadians = 0.45;
    private const double OrbitRadiansPerPixel = 0.01;
    private const double MinimumPitchRadians = -1.45;
    private const double MaximumPitchRadians = 1.45;
    private const double ZoomFactorPerWheelStep = 0.85;

    private readonly Viewport3D _viewport;

    private PerspectiveCamera _camera;
    private Bounds3d? _lastBounds;
    private Point3D _target;
    private Point _lastMousePosition;
    private DragMode _dragMode;
    private double _yawRadians = DefaultYawRadians;
    private double _pitchRadians = DefaultPitchRadians;
    private double _distance = 512.0;
    private double _minimumDistance = 8.0;
    private double _maximumDistance = 4096.0;

    public OrbitCameraController(
        Viewport3D viewport)
    {
        ArgumentNullException.ThrowIfNull(viewport);

        _viewport = viewport;
        _camera = EnsurePerspectiveCamera();

        _viewport.MouseDown += OnMouseDown;
        _viewport.MouseMove += OnMouseMove;
        _viewport.MouseUp += OnMouseUp;
        _viewport.MouseWheel += OnMouseWheel;
        _viewport.LostMouseCapture += OnLostMouseCapture;

        ApplyCamera();
    }

    /// <summary>
    /// Re-centers the camera on new geometry and restores its default angle.
    /// </summary>
    public void Reset(
        Bounds3d bounds)
    {
        _lastBounds = bounds;

        double largestDimension =
            Math.Max(
                bounds.Width,
                Math.Max(
                    bounds.Depth,
                    bounds.Height));

        double safeDimension =
            Math.Max(
                1.0,
                largestDimension);

        _target = new Point3D(
            bounds.Center.X,
            bounds.Center.Y,
            bounds.Center.Z);

        _distance =
            Math.Max(
                128.0,
                safeDimension * 2.4);

        _minimumDistance =
            Math.Max(
                4.0,
                safeDimension * 0.12);

        _maximumDistance =
            Math.Max(
                2048.0,
                _distance * 8.0);

        _yawRadians = DefaultYawRadians;
        _pitchRadians = DefaultPitchRadians;
        _camera = EnsurePerspectiveCamera();

        ApplyCamera();
    }

    private void OnMouseDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (
            e.ChangedButton == MouseButton.Left &&
            e.ClickCount >= 2
        ) {
            ResetToLastBounds();
            e.Handled = true;
            return;
        }

        DragMode requestedMode =
            e.ChangedButton switch
            {
                MouseButton.Left => DragMode.Orbit,
                MouseButton.Right => DragMode.Pan,
                _ => DragMode.None
            };

        if (
            requestedMode == DragMode.None ||
            _dragMode != DragMode.None
        ) {
            return;
        }

        _dragMode = requestedMode;
        _lastMousePosition =
            e.GetPosition(
                _viewport);

        _viewport.Focus();
        _viewport.CaptureMouse();
        _viewport.Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    private void OnMouseMove(
        object sender,
        MouseEventArgs e)
    {
        if (_dragMode == DragMode.None) {
            return;
        }

        bool expectedButtonIsPressed =
            _dragMode switch
            {
                DragMode.Orbit =>
                    e.LeftButton == MouseButtonState.Pressed,
                DragMode.Pan =>
                    e.RightButton == MouseButtonState.Pressed,
                _ => false
            };

        if (!expectedButtonIsPressed) {
            StopDragging();
            return;
        }

        Point currentPosition =
            e.GetPosition(
                _viewport);

        Vector movement =
            currentPosition - _lastMousePosition;

        _lastMousePosition = currentPosition;

        if (_dragMode == DragMode.Orbit) {
            Orbit(movement);
        }
        else {
            Pan(movement);
        }

        e.Handled = true;
    }

    private void OnMouseUp(
        object sender,
        MouseButtonEventArgs e)
    {
        bool completesDrag =
            (_dragMode == DragMode.Orbit &&
             e.ChangedButton == MouseButton.Left) ||
            (_dragMode == DragMode.Pan &&
             e.ChangedButton == MouseButton.Right);

        if (!completesDrag) {
            return;
        }

        StopDragging();
        e.Handled = true;
    }

    private void OnMouseWheel(
        object sender,
        MouseWheelEventArgs e)
    {
        double wheelSteps =
            e.Delta / 120.0;

        _distance =
            Math.Clamp(
                _distance *
                Math.Pow(
                    ZoomFactorPerWheelStep,
                    wheelSteps),
                _minimumDistance,
                _maximumDistance);

        ApplyCamera();
        e.Handled = true;
    }

    private void OnLostMouseCapture(
        object sender,
        MouseEventArgs e)
    {
        StopDragging(
            releaseCapture: false);
    }

    private void Orbit(
        Vector movement)
    {
        _yawRadians -=
            movement.X *
            OrbitRadiansPerPixel;

        _pitchRadians =
            Math.Clamp(
                _pitchRadians +
                (movement.Y * OrbitRadiansPerPixel),
                MinimumPitchRadians,
                MaximumPitchRadians);

        ApplyCamera();
    }

    private void Pan(
        Vector movement)
    {
        Vector3D lookDirection =
            _camera.LookDirection;

        if (lookDirection.LengthSquared <= 0.000001) {
            return;
        }

        lookDirection.Normalize();

        Vector3D right =
            Vector3D.CrossProduct(
                lookDirection,
                new Vector3D(
                    0.0,
                    0.0,
                    1.0));

        if (right.LengthSquared <= 0.000001) {
            return;
        }

        right.Normalize();

        Vector3D screenUp =
            Vector3D.CrossProduct(
                right,
                lookDirection);

        screenUp.Normalize();

        double viewportHeight =
            Math.Max(
                1.0,
                _viewport.ActualHeight);

        double visibleWorldHeight =
            2.0 *
            _distance *
            Math.Tan(
                _camera.FieldOfView *
                Math.PI /
                360.0);

        double worldUnitsPerPixel =
            visibleWorldHeight /
            viewportHeight;

        Vector3D translation =
            (-right *
             movement.X *
             worldUnitsPerPixel) +
            (screenUp *
             movement.Y *
             worldUnitsPerPixel);

        _target += translation;

        ApplyCamera();
    }

    private void ResetToLastBounds()
    {
        if (_lastBounds is Bounds3d bounds) {
            Reset(bounds);
        }
    }

    private void ApplyCamera()
    {
        double horizontalDistance =
            _distance *
            Math.Cos(
                _pitchRadians);

        Vector3D offset = new(
            horizontalDistance *
            Math.Cos(
                _yawRadians),
            horizontalDistance *
            Math.Sin(
                _yawRadians),
            _distance *
            Math.Sin(
                _pitchRadians));

        Point3D position =
            _target + offset;

        _camera.Position = position;
        _camera.LookDirection =
            _target - position;
        _camera.UpDirection =
            new Vector3D(
                0.0,
                0.0,
                1.0);
        _camera.FieldOfView = 38.0;
        _camera.NearPlaneDistance =
            Math.Max(
                0.1,
                _distance / 1000.0);
        _camera.FarPlaneDistance =
            Math.Max(
                4096.0,
                _distance * 20.0);
    }

    private PerspectiveCamera EnsurePerspectiveCamera()
    {
        if (_viewport.Camera is PerspectiveCamera camera) {
            if (!camera.IsFrozen) {
                return camera;
            }

            PerspectiveCamera mutableCamera =
                camera.CloneCurrentValue();

            _viewport.Camera = mutableCamera;

            return mutableCamera;
        }

        PerspectiveCamera createdCamera = new();
        _viewport.Camera = createdCamera;

        return createdCamera;
    }

    private void StopDragging(
        bool releaseCapture = true)
    {
        _dragMode = DragMode.None;
        _viewport.Cursor = Cursors.Arrow;

        if (
            releaseCapture &&
            _viewport.IsMouseCaptured
        ) {
            _viewport.ReleaseMouseCapture();
        }
    }

    private enum DragMode
    {
        None,
        Orbit,
        Pan
    }
}
