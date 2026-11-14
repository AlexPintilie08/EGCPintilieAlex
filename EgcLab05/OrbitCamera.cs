using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>Camera care orbitează în jurul unei ținte și privește mereu spre ea.</summary>
public class OrbitCamera : Camera
{
    private static readonly Vector3 WorldUp = Vector3.UnitY;

    public Vector3 Target { get; private set; }
    public float Distance { get; private set; }
    public float Azimuth { get; private set; }
    public float Elevation { get; private set; }

    public float MouseSensitivity = 0.2f;
    public float RotationSpeed = 90.0f;
    public float MoveSpeed = 3.0f;
    public float SprintFactor = 2.5f;

    public const float MinDistance = 0.5f;
    public const float MaxDistance = 50.0f;
    public const float MaxElevation = 89.0f;

    public OrbitCamera()
    {
        Reset();
    }

    public override string Name => "Orbită";

    public override Vector3 Position
    {
        get
        {
            float azimuth = MathHelper.DegreesToRadians(Azimuth);
            float elevation = MathHelper.DegreesToRadians(Elevation);
            Vector3 offset = new(
                MathF.Cos(elevation) * MathF.Cos(azimuth),
                MathF.Sin(elevation),
                MathF.Cos(elevation) * MathF.Sin(azimuth));
            return Target + Distance * offset;
        }
    }

    public override Vector3 Front => Vector3.Normalize(Target - Position);

    public override Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, WorldUp);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Azimuth += mouseDelta.X * MouseSensitivity;
            Elevation = Math.Clamp(Elevation - mouseDelta.Y * MouseSensitivity, -MaxElevation, MaxElevation);
        }

        float rotation = RotationSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Left)) Azimuth -= rotation;
        if (keyboard.IsKeyDown(Keys.Right)) Azimuth += rotation;
        if (keyboard.IsKeyDown(Keys.Up)) Elevation = Math.Clamp(Elevation + rotation, -MaxElevation, MaxElevation);
        if (keyboard.IsKeyDown(Keys.Down)) Elevation = Math.Clamp(Elevation - rotation, -MaxElevation, MaxElevation);

        if (mouse.ScrollDelta.Y != 0.0f)
        {
            Distance = Math.Clamp(Distance * MathF.Pow(0.9f, mouse.ScrollDelta.Y), MinDistance, MaxDistance);
        }

        Vector3 front = Front;
        Vector3 forward = new Vector3(front.X, 0.0f, front.Z).Normalized();
        Vector3 right = Vector3.Normalize(Vector3.Cross(front, WorldUp));
        Vector3 move = Vector3.Zero;
        if (keyboard.IsKeyDown(Keys.W)) move += forward;
        if (keyboard.IsKeyDown(Keys.S)) move -= forward;
        if (keyboard.IsKeyDown(Keys.D)) move += right;
        if (keyboard.IsKeyDown(Keys.A)) move -= right;

        if (move.LengthSquared > 0.0f)
        {
            float sprint = keyboard.IsKeyDown(Keys.LeftShift) ? SprintFactor : 1.0f;
            Target += move.Normalized() * MoveSpeed * sprint * dt;
        }
    }

    public void SetFromFps(Vector3 position, Vector3 front, float distance)
    {
        Distance = Math.Clamp(distance, MinDistance, MaxDistance);
        Target = position + Vector3.Normalize(front) * Distance;
        Vector3 offset = (position - Target) / Distance;
        Elevation = Math.Clamp(
            MathHelper.RadiansToDegrees(MathF.Asin(Math.Clamp(offset.Y, -1.0f, 1.0f))),
            -MaxElevation, MaxElevation);
        Azimuth = MathHelper.RadiansToDegrees(MathF.Atan2(offset.Z, offset.X));
    }

    public override void Reset()
    {
        Target = new Vector3(0.0f, 1.0f, 0.0f);
        Distance = 8.0f;
        Azimuth = -90.0f;
        Elevation = 20.0f;
        FieldOfView = 60.0f;
    }

    public override IEnumerable<string> Describe()
    {
        Vector3 position = Position;
        yield return $"Țintă          : ({Target.X,6:F2}, {Target.Y,6:F2}, {Target.Z,6:F2})";
        yield return $"Distanță       : {Distance,6:F2}";
        yield return $"Azimut/elevație: {Azimuth,6:F1}° / {Elevation,6:F1}°";
        yield return $"Poziție        : ({position.X,6:F2}, {position.Y,6:F2}, {position.Z,6:F2})";
        yield return $"Front          : ({Front.X,6:F2}, {Front.Y,6:F2}, {Front.Z,6:F2})";
    }
}
