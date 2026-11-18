// Varianta C: roata panoramica
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class RoataPanoramica : Scene
{
    private const string Faster = "creste viteza rotii";
    private const string Slower = "scade viteza rotii";
    private const string Stop = "opreste progresiv roata";
    private const string ResetWheel = "reseteaza roata";
    private const float Radius = 2.45f;
    private const float CenterY = 3.3f;
    private const float SelectionDuration = 1.6f;
    private const float Spring = 24.0f;
    private const float Damping = 9.5f;

    private readonly List<SceneObject> _cabins = new();
    private readonly float[] _swing = new float[8];
    private readonly float[] _swingSpeed = new float[8];
    private SceneObject _wheel = null!;
    private Tween? _selection;
    private float _angle;
    private float _speed = 18.0f;
    private float _targetSpeed = 18.0f;
    private float _selectionTarget;
    private int? _selectedCabin;

    public override string Name => "roata panoramica";

    public override string Status =>
        $"viteza {_speed:F0}/{_targetSpeed:F0}°/s" +
        (_selectedCabin.HasValue ? $" | cabina {_selectedCabin.Value + 1}" : "") +
        (_selection != null ? " | pozitionare" : "");

    public override IEnumerable<string> ActionNames => new[] { Faster, Slower, Stop, ResetWheel }
        .Concat(Enumerable.Range(1, 8).Select(i => $"adu cabina {i} la peron"));

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 5.4f, 12.5f);
        CameraTarget = new Vector3(0.0f, 2.9f, 0.0f);

        input.Bind(Faster, Keys.Up);
        input.Bind(Slower, Keys.Down);
        input.Bind(Stop, Keys.Space);
        input.Bind(ResetWheel, Keys.R);
        for (int i = 0; i < 8; i++) input.Bind(CabinAction(i), (Keys)((int)Keys.D1 + i));

        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh hubMesh = AddMesh(Shapes.ShadedCylinder(0.48f, 0.34f, 28));
        BuildSupports(cube);
        BuildWheel(cube, hubMesh);
        BuildPlatform(cube);
        ResetWheelState();
    }

    private void BuildSupports(Mesh cube)
    {
        Color4 steel = new(0.25f, 0.31f, 0.38f, 1.0f);
        AddBar("suport stanga", cube, null, new Vector2(-1.65f, 0.1f), new Vector2(0.0f, CenterY), 0.24f, steel);
        AddBar("suport dreapta", cube, null, new Vector2(1.65f, 0.1f), new Vector2(0.0f, CenterY), 0.24f, steel);
        Box("talpa suport", cube, null, new Vector3(0.0f, 0.12f, 0.0f),
            new Vector3(4.1f, 0.24f, 0.48f), new Color4(0.2f, 0.25f, 0.31f, 1.0f));
        AddBar("traverse suport", cube, null, new Vector2(-0.95f, 0.75f), new Vector2(0.95f, 0.75f),
            0.12f, new Color4(0.36f, 0.42f, 0.48f, 1.0f));
    }

    private void BuildWheel(Mesh cube, Mesh hubMesh)
    {
        _wheel = AddObject("pivot roata", null);
        _wheel.Transform.Position = new Vector3(0.0f, CenterY, 0.0f);
        Color4 rimColor = new(0.18f, 0.52f, 0.68f, 1.0f);

        const int segments = 24;
        float length = 2.0f * Radius * MathF.Sin(MathF.PI / segments);
        for (int i = 0; i < segments; i++)
        {
            float a = 2.0f * MathF.PI * i / segments;
            float b = 2.0f * MathF.PI * (i + 1) / segments;
            Vector3 p0 = new(Radius * MathF.Cos(a), Radius * MathF.Sin(a), 0.0f);
            Vector3 p1 = new(Radius * MathF.Cos(b), Radius * MathF.Sin(b), 0.0f);
            Vector3 middle = (p0 + p1) * 0.5f;
            float tangent = MathHelper.RadiansToDegrees((a + b) * 0.5f + MathF.PI / 2.0f);
            Box("segment janta", cube, _wheel, middle, new Vector3(length, 0.13f, 0.14f), rimColor, tangent);
        }

        for (int i = 0; i < 8; i++)
        {
            float a = MathF.Tau * i / 8.0f;
            float x = Radius * MathF.Cos(a);
            float y = Radius * MathF.Sin(a);
            Box("spita", cube, _wheel, new Vector3(x / 2.0f, y / 2.0f, 0.0f),
                new Vector3(Radius, 0.11f, 0.16f), new Color4(0.45f, 0.58f, 0.67f, 1.0f),
                MathHelper.RadiansToDegrees(a));

            SceneObject cabin = AddObject($"pivot cabina {i + 1}", null, _wheel);
            cabin.Transform.Position = new Vector3(x, y, 0.18f);
            _cabins.Add(cabin);
            BuildCabin(cube, cabin, i);
        }

        SceneObject hub = AddObject("butuc", hubMesh, _wheel);
        hub.Transform.Rotation.X = 90.0f;
        hub.Tint = new Color4(0.85f, 0.62f, 0.2f, 1.0f);
    }

    private void BuildCabin(Mesh cube, SceneObject pivot, int index)
    {
        Color4[] colors =
        {
            new(0.85f, 0.28f, 0.22f, 1.0f), new(0.92f, 0.56f, 0.16f, 1.0f),
            new(0.76f, 0.72f, 0.2f, 1.0f), new(0.26f, 0.66f, 0.38f, 1.0f),
            new(0.2f, 0.58f, 0.76f, 1.0f), new(0.38f, 0.4f, 0.8f, 1.0f),
            new(0.66f, 0.36f, 0.7f, 1.0f), new(0.84f, 0.38f, 0.55f, 1.0f),
        };
        Box("bara suspendare", cube, pivot, new Vector3(0.0f, -0.16f, 0.0f),
            new Vector3(0.055f, 0.32f, 0.055f), new Color4(0.22f, 0.25f, 0.3f, 1.0f));
        Box("cabina", cube, pivot, new Vector3(0.0f, -0.35f, 0.0f),
            new Vector3(0.64f, 0.52f, 0.48f), colors[index]);
        Box("acoperis cabina", cube, pivot, new Vector3(0.0f, -0.03f, 0.0f),
            new Vector3(0.76f, 0.09f, 0.58f), new Color4(0.18f, 0.22f, 0.28f, 1.0f));
        foreach (float side in new[] { -1.0f, 1.0f })
        {
            Box("geam cabina", cube, pivot, new Vector3(0.0f, -0.35f, side * 0.245f),
                new Vector3(0.42f, 0.26f, 0.018f), new Color4(0.1f, 0.3f, 0.38f, 1.0f));
        }
    }

    private void BuildPlatform(Mesh cube)
    {
        Box("peron", cube, null, new Vector3(0.0f, 0.13f, 1.05f),
            new Vector3(3.3f, 0.22f, 1.25f), new Color4(0.4f, 0.43f, 0.47f, 1.0f));
        Box("margine peron", cube, null, new Vector3(0.0f, 0.26f, 0.48f),
            new Vector3(3.0f, 0.045f, 0.08f), new Color4(0.95f, 0.72f, 0.2f, 1.0f));
        Box("treapta peron", cube, null, new Vector3(0.0f, 0.045f, 1.55f),
            new Vector3(2.1f, 0.09f, 0.48f), new Color4(0.32f, 0.35f, 0.4f, 1.0f));
    }

    private void Box(string name, Mesh mesh, SceneObject? parent, Vector3 position,
                     Vector3 scale, Color4 tint, float rotationZ = 0.0f)
    {
        SceneObject obj = AddObject(name, mesh, parent);
        obj.Transform.Position = position;
        obj.Transform.Scale = scale;
        obj.Transform.Rotation.Z = rotationZ;
        obj.Tint = tint;
    }

    private void AddBar(string name, Mesh mesh, SceneObject? parent, Vector2 a, Vector2 b,
                        float thickness, Color4 tint)
    {
        Vector2 delta = b - a;
        Vector2 middle = (a + b) * 0.5f;
        Box(name, mesh, parent, new Vector3(middle.X, middle.Y, 0.0f),
            new Vector3(delta.Length, thickness, thickness), tint,
            MathHelper.RadiansToDegrees(MathF.Atan2(delta.Y, delta.X)));
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);
        if (input.WasPressed(Stop)) _targetSpeed = 0.0f;
        if (input.WasPressed(ResetWheel))
        {
            ResetWheelState();
            return;
        }

        if (_selection == null)
        {
            float direction = (input.IsDown(Faster) ? 1.0f : 0.0f) - (input.IsDown(Slower) ? 1.0f : 0.0f);
            _targetSpeed = Math.Clamp(_targetSpeed + direction * 36.0f * dt, -90.0f, 90.0f);
            for (int i = 0; i < 8; i++)
            {
                if (input.WasPressed(CabinAction(i)))
                {
                    SelectCabin(i);
                    break;
                }
            }
        }

        float oldSpeed = _speed;
        if (_selection != null)
        {
            if (dt > 0.0f)
            {
                _selection.Update(dt);
                _angle = _selection.Value.X;
                _speed = (_angle - _wheel.Transform.Rotation.Z) / dt;
                if (_selection.IsFinished)
                {
                    _angle = _selectionTarget;
                    _speed = 0.0f;
                    _targetSpeed = 0.0f;
                    _selection = null;
                }
            }
        }
        else if (dt > 0.0f)
        {
            float blend = 1.0f - MathF.Exp(-2.5f * dt);
            _speed += (_targetSpeed - _speed) * blend;
            _angle += _speed * dt;
        }

        float acceleration = dt > 0.0f ? (_speed - oldSpeed) / dt : 0.0f;
        _wheel.Transform.Rotation.Z = _angle;
        UpdateCabins(dt, acceleration);
    }

    private void SelectCabin(int index)
    {
        float desired = NormalizeDegrees(270.0f - index * 45.0f);
        float current = NormalizeDegrees(_angle);
        float difference = ShortestDelta(desired - current);
        _selectionTarget = _angle + difference;
        _selection = new Tween(new Vector3(_angle, 0.0f, 0.0f),
                               new Vector3(_selectionTarget, 0.0f, 0.0f),
                               SelectionDuration, Easing.EaseInOut);
        _selectedCabin = index;
        _targetSpeed = 0.0f;
        ConsolePanel.PrintMessage($"cabina {index + 1}: pozitionare la peron pe drumul scurt ({difference:F1}°)");
    }

    private void UpdateCabins(float dt, float accelerationDegrees)
    {
        if (dt > 0.0f)
        {
            int steps = Math.Max(1, (int)MathF.Ceiling(dt / 0.01f));
            float step = dt / steps;
            float alpha = MathHelper.DegreesToRadians(accelerationDegrees);
            for (int i = 0; i < _cabins.Count; i++)
            {
                float cabinAngle = MathHelper.DegreesToRadians(_angle + i * 45.0f);
                float tangentAcceleration = -alpha * Radius * MathF.Sin(cabinAngle);
                float targetSwing = MathF.Atan(tangentAcceleration / 9.81f);
                for (int j = 0; j < steps; j++)
                {
                    float swingAcceleration = -Spring * (_swing[i] - targetSwing) - Damping * _swingSpeed[i];
                    _swingSpeed[i] += swingAcceleration * step;
                    _swing[i] += _swingSpeed[i] * step;
                }
            }
        }

        for (int i = 0; i < _cabins.Count; i++)
        {
            _cabins[i].Transform.Rotation.Z = -_angle + MathHelper.RadiansToDegrees(_swing[i]);
        }
    }

    private void ResetWheelState()
    {
        _angle = 0.0f;
        _speed = 18.0f;
        _targetSpeed = 18.0f;
        _selectionTarget = 0.0f;
        _selection = null;
        _selectedCabin = null;
        Array.Clear(_swing);
        Array.Clear(_swingSpeed);
        _wheel.Transform.Rotation.Z = 0.0f;
        for (int i = 0; i < _cabins.Count; i++) _cabins[i].Transform.Rotation.Z = 0.0f;
    }

    private static string CabinAction(int index) => $"adu cabina {index + 1} la peron";

    private static float NormalizeDegrees(float angle)
    {
        angle %= 360.0f;
        return angle < 0.0f ? angle + 360.0f : angle;
    }

    private static float ShortestDelta(float angle)
    {
        angle = NormalizeDegrees(angle + 180.0f) - 180.0f;
        return angle;
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        float maxSwing = _swing.Length == 0 ? 0.0f : _swing.Max(x => MathF.Abs(x));
        yield return $"Unghi roata     : {NormalizeDegrees(_angle):F1}°";
        yield return $"Viteza reala    : {_speed:F2}°/s   tinta: {_targetSpeed:F2}°/s";
        yield return $"Cabina selectata: {(_selectedCabin.HasValue ? _selectedCabin.Value + 1 : "niciuna")}";
        yield return $"Animatie        : {(_selection != null ? $"Tween { _selection.Progress:P0}" : "rotatie amortizata")}";
        yield return $"Balans maxim    : {MathHelper.RadiansToDegrees(maxSwing):F1}°";
    }
}
