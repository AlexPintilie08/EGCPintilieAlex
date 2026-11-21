// Cerinta suplimentara de la laborator: masina si sfera orbitala
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class MasinaScene : Scene
{
    private const string ToggleDrive = "porneste / opreste masina";
    private const string NextEase = "schimba easing-ul masinii";
    private const string ResetCar = "reseteaza masina si orbita";
    private static readonly Vector3 Start = new(-4.2f, 0.0f, 0.0f);
    private static readonly Vector3 End = new(4.2f, 0.0f, 0.0f);
    private const float DriveDuration = 3.5f;
    private const float WheelRadius = 0.36f;
    private const float OrbitRadius = 2.35f;

    private readonly List<(SceneObject Pivot, float Radius)> _wheels = new();
    private SceneObject _car = null!;
    private SceneObject _orbitPivot = null!;
    private SceneObject _orbitalSphere = null!;
    private Tween _drive = null!;
    private int _easeIndex = 3;
    private int _driveEaseIndex = 3;
    private bool _driving = true;

    public override string Name => "masina";

    public override string Status =>
        $"{Easing.All[_easeIndex].Name}{(_easeIndex == _driveEaseIndex ? "" : " (urmatoarea tura)")}; " +
        $"{(_driving ? "in mers" : "oprita")}; progres {_drive.Progress:P0}";

    public override IEnumerable<string> ActionNames => new[] { ToggleDrive, NextEase, ResetCar };

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(7.0f, 6.0f, 14.0f);
        CameraTarget = new Vector3(0.0f, 0.8f, 0.0f);

        input.Bind(ToggleDrive, Keys.Space);
        input.Bind(NextEase, Keys.E);
        input.Bind(ResetCar, Keys.R);

        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh tire = AddMesh(Shapes.ShadedCylinder(WheelRadius, 0.22f, 24));
        Mesh rim = AddMesh(Shapes.ShadedCylinder(0.22f, 0.07f, 20));
        Mesh sphere = AddMesh(Shapes.ShadedSphere(0.28f, 24, 12));

        BuildRoad(cube);
        BuildCar(cube, tire, rim, sphere);
        ResetMotion();
    }

    private void BuildRoad(Mesh cube)
    {
        Box("drum", cube, null, new Vector3(0.0f, -0.07f, 0.0f),
            new Vector3(12.0f, 0.14f, 3.8f), new Color4(0.12f, 0.14f, 0.17f, 1.0f));
        Box("margine drum", cube, null, new Vector3(0.0f, 0.01f, -1.72f),
            new Vector3(11.5f, 0.02f, 0.045f), new Color4(0.85f, 0.86f, 0.82f, 1.0f));
        Box("margine drum", cube, null, new Vector3(0.0f, 0.01f, 1.72f),
            new Vector3(11.5f, 0.02f, 0.045f), new Color4(0.85f, 0.86f, 0.82f, 1.0f));
        for (int i = 0; i < 14; i++)
        {
            float x = -5.2f + i * 0.8f;
            Box("marcaj drum", cube, null, new Vector3(x, 0.01f, 0.0f),
                new Vector3(0.42f, 0.02f, 0.07f), new Color4(0.9f, 0.9f, 0.86f, 1.0f));
        }
    }

    private void BuildCar(Mesh cube, Mesh tire, Mesh rim, Mesh sphere)
    {
        _car = AddObject("pivot masina", null);
        _car.Transform.Scale = Vector3.One;
        Color4 blue = new(0.04f, 0.22f, 0.78f, 1.0f);
        Color4 lightBlue = new(0.08f, 0.38f, 0.98f, 1.0f);
        Color4 glass = new(0.035f, 0.16f, 0.23f, 1.0f);
        Color4 silver = new(0.72f, 0.78f, 0.84f, 1.0f);
        Color4 dark = new(0.045f, 0.055f, 0.07f, 1.0f);

        Box("caroserie", cube, _car, new Vector3(0.0f, 0.58f, 0.0f),
            new Vector3(3.45f, 0.48f, 1.55f), blue);
        Box("capota", cube, _car, new Vector3(0.98f, 0.82f, 0.0f),
            new Vector3(1.35f, 0.22f, 1.5f), lightBlue);
        Box("portbagaj", cube, _car, new Vector3(-1.12f, 0.79f, 0.0f),
            new Vector3(0.9f, 0.22f, 1.5f), blue);
        Box("habitaclu", cube, _car, new Vector3(-0.12f, 1.06f, 0.0f),
            new Vector3(1.55f, 0.66f, 1.24f), blue);
        Box("plafon", cube, _car, new Vector3(-0.12f, 1.42f, 0.0f),
            new Vector3(0.95f, 0.12f, 1.2f), lightBlue);
        Box("parbriz", cube, _car, new Vector3(0.62f, 1.18f, 0.0f),
            new Vector3(0.08f, 0.46f, 1.13f), glass, 20.0f);
        Box("luneta", cube, _car, new Vector3(-0.86f, 1.18f, 0.0f),
            new Vector3(0.08f, 0.46f, 1.13f), glass, -20.0f);
        foreach (float side in new[] { -1.0f, 1.0f })
        {
            Box("geam lateral", cube, _car, new Vector3(-0.12f, 1.15f, side * 0.635f),
                new Vector3(0.72f, 0.34f, 0.035f), glass);
            Box("maner", cube, _car, new Vector3(-0.18f, 0.78f, side * 0.79f),
                new Vector3(0.26f, 0.035f, 0.03f), silver);
            Box("far", cube, _car, new Vector3(1.72f, 0.72f, side * 0.52f),
                new Vector3(0.12f, 0.14f, 0.25f), new Color4(1.0f, 0.88f, 0.42f, 1.0f));
            Box("stop", cube, _car, new Vector3(-1.73f, 0.7f, side * 0.52f),
                new Vector3(0.12f, 0.16f, 0.24f), new Color4(0.9f, 0.045f, 0.035f, 1.0f));
        }
        Box("bara fata", cube, _car, new Vector3(1.76f, 0.47f, 0.0f),
            new Vector3(0.14f, 0.16f, 1.68f), silver);
        Box("grila", cube, _car, new Vector3(1.81f, 0.57f, 0.0f),
            new Vector3(0.035f, 0.19f, 0.45f), dark);
        Box("bara spate", cube, _car, new Vector3(-1.78f, 0.45f, 0.0f),
            new Vector3(0.14f, 0.16f, 1.68f), silver);

        for (int xSide = -1; xSide <= 1; xSide += 2)
        {
            for (int zSide = -1; zSide <= 1; zSide += 2)
            {
                SceneObject wheel = AddObject("pivot roata", null, _car);
                wheel.Transform.Position = new Vector3(xSide * 1.12f, WheelRadius, zSide * 0.83f);
                AddWheelPart("pneu", tire, wheel, Vector3.Zero, dark, 90.0f);
                AddWheelPart("janta", rim, wheel, new Vector3(0.0f, 0.0f, zSide * 0.12f), silver, 90.0f);
                for (int spoke = 0; spoke < 2; spoke++)
                {
                    Box("spita", cube, wheel, new Vector3(0.0f, 0.0f, zSide * 0.13f),
                        new Vector3(0.3f, 0.035f, 0.025f), silver, spoke * 90.0f);
                }
                _wheels.Add((wheel, WheelRadius));
            }
        }

        _orbitPivot = AddObject("pivot orbita sfera", null, _car);
        _orbitPivot.Transform.Position = new Vector3(0.0f, 1.05f, 0.0f);
        _orbitPivot.Spin = new Vector3(0.0f, 75.0f, 0.0f);
        _orbitalSphere = AddObject("sfera orbitala", sphere, _orbitPivot);
        _orbitalSphere.Transform.Position = new Vector3(OrbitRadius, 0.0f, 0.0f);
        _orbitalSphere.Tint = new Color4(1.0f, 0.52f, 0.06f, 1.0f);
    }

    private SceneObject AddWheelPart(string name, Mesh mesh, SceneObject parent, Vector3 position, Color4 tint, float rotationX)
    {
        SceneObject part = AddObject(name, mesh, parent);
        part.Transform.Position = position;
        part.Transform.Rotation.X = rotationX;
        part.Tint = tint;
        return part;
    }

    private SceneObject Box(string name, Mesh mesh, SceneObject? parent, Vector3 position,
                            Vector3 scale, Color4 tint, float rotationZ = 0.0f)
    {
        SceneObject box = AddObject(name, mesh, parent);
        box.Transform.Position = position;
        box.Transform.Scale = scale;
        box.Transform.Rotation.Z = rotationZ;
        box.Tint = tint;
        return box;
    }

    public override void Update(float dt, Input input)
    {
        bool reset = input.WasPressed(ResetCar);
        if (input.WasPressed(ToggleDrive)) _driving = !_driving;
        if (input.WasPressed(NextEase))
        {
            _easeIndex = (_easeIndex + 1) % Easing.All.Length;
            ConsolePanel.PrintMessage($"easing selectat: {Easing.All[_easeIndex].Name}; se aplica la urmatorul capat al traseului");
        }

        base.Update(dt, input);
        if (_driving) AdvanceDrive(dt);
        if (reset) ResetMotion();
    }

    private void AdvanceDrive(float dt)
    {
        if (dt <= 0.0f) return;
        float oldX = _car.Transform.Position.X;
        float duration = _drive.Duration;

        if (_easeIndex != _driveEaseIndex)
        {
            float toBoundary = _drive.Elapsed % duration == 0.0f
                ? 0.0f
                : (MathF.Floor(_drive.Elapsed / duration) + 1.0f) * duration - _drive.Elapsed;
            if (dt >= toBoundary)
            {
                if (toBoundary > 0.0f) _drive.Update(toBoundary);
                int boundaryIndex = (int)MathF.Round(_drive.Elapsed / duration);
                Vector3 from = boundaryIndex % 2 == 1 ? _drive.To : _drive.From;
                Vector3 to = Vector3.DistanceSquared(from, Start) < 1e-5f ? End : Start;
                _drive = new Tween(from, to, duration, Easing.All[_easeIndex].Function, pingPong: true);
                _driveEaseIndex = _easeIndex;
                _drive.Update(dt - toBoundary);
            }
            else
            {
                _drive.Update(dt);
            }
        }
        else
        {
            _drive.Update(dt);
        }

        _car.Transform.Position = _drive.Value;
        float distance = _car.Transform.Position.X - oldX;
        foreach (var wheel in _wheels)
        {
            float wheelTurn = MathHelper.RadiansToDegrees(-distance / wheel.Radius);
            wheel.Pivot.Transform.Rotation.Z += wheelTurn;
        }
    }

    private void ResetMotion()
    {
        _drive = new Tween(Start, End, DriveDuration, Easing.EaseInOut, pingPong: true);
        _easeIndex = 3;
        _driveEaseIndex = 3;
        _driving = true;
        _car.Transform.Position = Start;
        foreach (var wheel in _wheels) wheel.Pivot.Transform.Rotation.Z = 0.0f;
        _orbitPivot.Transform.Rotation = Vector3.Zero;
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        Vector3 pivot = _orbitPivot.WorldPosition;
        Vector3 ball = _orbitalSphere.WorldPosition;
        yield return $"Easing          : {Easing.All[_driveEaseIndex].Name}" +
                     (_driveEaseIndex == _easeIndex ? "" : $"; urmatorul: {Easing.All[_easeIndex].Name}");
        yield return $"Progres / stare : {_drive.Progress:F2} / {(_driving ? "in mers" : "oprita")}";
        yield return $"Pozitie masina  : ({_car.WorldPosition.X:F2}, {_car.WorldPosition.Y:F2}, {_car.WorldPosition.Z:F2})";
        yield return $"Raza orbita     : {Vector3.Distance(pivot, ball):F2}";
        yield return $"Unghi orbital   : {_orbitPivot.Transform.Rotation.Y:F1}°";
    }
}
