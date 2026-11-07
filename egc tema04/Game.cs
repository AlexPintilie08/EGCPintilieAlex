using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// Față de laboratorul 3: o scenă cu mai multe obiecte, fiecare cu matricea lui model,
/// o cameră cu matricele view și projection, tastatura și mouse-ul.
/// </summary>
public class Game : GameWindow
{
    // Obiecte care NU depind de OpenGL pot fi create imediat.
    private readonly Camera _camera = new();

    // Obiectele care folosesc OpenGL se creează în OnLoad (abia atunci există contextul OpenGL).
    private Shader _shader = null!;
    private readonly List<SceneObject> _scene = new();
    private readonly List<Transform2D> _initialTransforms = new();   // pentru tasta R
    private Mesh? _satelliteMesh;

    // Starea aplicației (se modifică în OnUpdateFrame, se folosește în OnRenderFrame).
    private int _selected;            // indexul obiectului selectat în _scene (tasta Tab)
    private bool _wireframe;          // contur (true) sau plin (false)
    private Vector2? _pivot;
    private float _orbitAngleDegrees;

    // Contoare afișate în titlul ferestrei.
    private readonly string _baseTitle;
    private readonly Stopwatch _fpsStopwatch = new();
    private int _frames;
    private double _lastFps;
    private bool _titleDirty = true;

    private static GameWindowSettings CreateUncappedGameWindowSettings()
    {
        GameWindowSettings settings = GameWindowSettings.Default;
        settings.UpdateFrequency = 0.0;
        return settings;
    }

    public Game(int width, int height, string title)
        : base(CreateUncappedGameWindowSettings(),
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),          // cerem OpenGL 3.3 ...
                   Profile = ContextProfile.Core,           // ... profil Core (fără modul imediat)
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.Off;
    }

    /// <summary>Apelată O SINGURĂ DATĂ, după crearea ferestrei și a contextului OpenGL.</summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        // Scena: aceleași forme din laboratorul 3, acum plasate în lume prin Transform2D.
        // Depth (z) contează doar în perspectivă (tasta O, secțiunea 7.2).
        AddObject("pătrat", Shapes.Quad(),
                  new Transform2D { Position = new Vector2(-1.8f, -1.0f), Depth = -3.0f });
        AddObject("triunghi", Shapes.Triangle(),
                  new Transform2D { Position = new Vector2(-1.2f, 1.0f) });
        AddObject("hexagon", Shapes.RegularPolygon(6, 0.5f),
                  new Transform2D { Position = new Vector2(2.0f, 1.0f) });
        AddObject("cerc", Shapes.RegularPolygon(64, 0.5f),
                  new Transform2D { Position = new Vector2(0.8f, -0.6f), Depth = 2.0f });

        _satelliteMesh = Shapes.Triangle();
        Console.WriteLine("Apasa H pentru lista comenzilor.");
        _fpsStopwatch.Restart();
    }

    /// <summary>Adaugă un obiect în scenă și reține transformarea lui inițială (pentru tasta R).</summary>
    private void AddObject(string name, Mesh mesh, Transform2D transform)
    {
        _scene.Add(new SceneObject(name, mesh, transform));
        _initialTransforms.Add(transform.Clone());
    }

    /// <summary>Apelată când se schimbă dimensiunea ferestrei.</summary>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);   // zona ferestrei în care se desenează
        _camera.Resize(e.Width, e.Height);      // proiecția se adaptează la noul raport de aspect
    }

    /// <summary>Apelată la fiecare iterație a buclei: LOGICA aplicației (intrări, mișcare, timp).</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        if (KeyboardState.IsKeyPressed(Keys.H)) PrintHelp();

        // --- Obiectul selectat: tastatura (secțiunea 8) ------------------------------

        float dt = (float)args.Time;
        _orbitAngleDegrees = (_orbitAngleDegrees + 90.0f * dt) % 360.0f;
        SceneObject sel = _scene[_selected];

        const float moveSpeed = 2.0f;   // unități de lume pe secundă
        float multiplier = KeyboardState.IsKeyDown(Keys.LeftShift) ? 3.0f : 1.0f;
        float moveStep = moveSpeed * dt * multiplier;
        if (KeyboardState.IsKeyDown(Keys.Left))  sel.Transform.Position.X -= moveStep;
        if (KeyboardState.IsKeyDown(Keys.Right)) sel.Transform.Position.X += moveStep;
        if (KeyboardState.IsKeyDown(Keys.Down))  sel.Transform.Position.Y -= moveStep;
        if (KeyboardState.IsKeyDown(Keys.Up))    sel.Transform.Position.Y += moveStep;

        float rotationStep = 90.0f * dt * multiplier;
        if (KeyboardState.IsKeyDown(Keys.Q)) sel.Transform.Rotation += rotationStep;
        if (KeyboardState.IsKeyDown(Keys.E)) sel.Transform.Rotation -= rotationStep;

        float scaleFactor = MathF.Pow(1.5f, dt * multiplier);
        if (KeyboardState.IsKeyDown(Keys.Z)) sel.Transform.Scale /= scaleFactor;
        if (KeyboardState.IsKeyDown(Keys.X)) sel.Transform.Scale *= scaleFactor;
        // T2: scalarea prin inmultire pastreaza aceeasi schimbare relativa indiferent de scara curenta.
        sel.Transform.Scale.X = Math.Clamp(sel.Transform.Scale.X, 0.1f, 10.0f);
        sel.Transform.Scale.Y = Math.Clamp(sel.Transform.Scale.Y, 0.1f, 10.0f);

        if (KeyboardState.IsKeyPressed(Keys.Tab)) _selected = (_selected + 1) % _scene.Count;
        if (KeyboardState.IsKeyPressed(Keys.O))   _camera.IsPerspective = !_camera.IsPerspective;
        if (KeyboardState.IsKeyPressed(Keys.F))   _wireframe = !_wireframe;
        if (KeyboardState.IsKeyPressed(Keys.R))   ResetScene();

        if (KeyboardState.IsKeyPressed(Keys.Tab) || KeyboardState.IsKeyPressed(Keys.O) ||
            KeyboardState.IsKeyPressed(Keys.R))
        {
            _titleDirty = true;
        }

        // --- Obiectul selectat: mouse-ul (secțiunea 9) -------------------------------

        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            sel.Transform.Position = ScreenToWorld(MouseState.Position, sel.Transform.Depth);
        }

        if (MouseState.IsButtonPressed(MouseButton.Right))
        {
            _pivot = ScreenToWorld(MouseState.Position);
        }

        if (_pivot.HasValue && KeyboardState.IsKeyDown(Keys.P))
        {
            float pivotRotation = 90.0f * dt * multiplier;
            Vector2 pivot = _pivot.Value;
            // T4: T(-pivot) * R * T(pivot) rotește centrul obiectului în jurul pivotului.
            Matrix4 aroundPivot =
                Matrix4.CreateTranslation(-pivot.X, -pivot.Y, 0.0f)
              * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(pivotRotation))
              * Matrix4.CreateTranslation(pivot.X, pivot.Y, 0.0f);
            Vector4 position = new Vector4(sel.Transform.Position.X, sel.Transform.Position.Y, 0.0f, 1.0f) * aroundPivot;
            sel.Transform.Position = new Vector2(position.X, position.Y);
            sel.Transform.Rotation += pivotRotation;
        }

        // --- Camera: tastatura și rotița (secțiunea 6.2) -----------------------------

        float camSpeed = 3.0f / _camera.Zoom;                    // unități de lume pe secundă
        if (KeyboardState.IsKeyDown(Keys.A)) _camera.Position.X -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.D)) _camera.Position.X += camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.S)) _camera.Position.Y -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.W)) _camera.Position.Y += camSpeed * dt;

        _camera.Zoom *= MathF.Pow(1.1f, MouseState.ScrollDelta.Y);   // un pas al rotiței = ±10 %
        _camera.Zoom = Math.Clamp(_camera.Zoom, 0.1f, 10.0f);

        // --- Titlul ferestrei: obiectul selectat, proiecția, cadre pe secundă --------

        double sampleSeconds = _fpsStopwatch.Elapsed.TotalSeconds;
        if (sampleSeconds >= 1.0)
        {
            _lastFps = _frames / sampleSeconds;
            _frames = 0;
            _fpsStopwatch.Restart();
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            string projection = _camera.IsPerspective ? "perspectivă" : "ortografică";
            Title = $"{_baseTitle} | selectat: {_scene[_selected].Name} | proiecție {projection} | FPS: {_lastFps:F0}";
            _titleDirty = false;
        }
    }

    /// <summary>Apelată la fiecare iterație a buclei: DESENAREA unui cadru.</summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        // 1. Ștergem imaginea veche din back buffer.
        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // 2. Starea de rasterizare și programul shader.
        SetWireframe(_wireframe);
        _shader.Use();

        // 3. View și projection sunt aceleași pentru toate obiectele: le trimitem o singură dată.
        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);

        // 4. Fiecare obiect: matricea lui model, nuanța, apoi desenarea.
        foreach (SceneObject obj in _scene)
        {
            _shader.SetMatrix4("uModel", obj.Transform.ModelMatrix);
            _shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }

        if (_satelliteMesh is not null)
        {
            // T5: orbita este în coordonatele părintelui, deci scalarea lui mărește distanța satelitului.
            Matrix4 localTransform =
                Matrix4.CreateScale(0.3f, 0.3f, 1.0f)
              * Matrix4.CreateTranslation(1.2f, 0.0f, 0.0f)
              * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(_orbitAngleDegrees));
            Matrix4 satelliteModel = localTransform * _scene[_selected].Transform.ModelMatrix;
            _shader.SetMatrix4("uModel", satelliteModel);
            _shader.SetColor("uTint", Color4.White);
            _satelliteMesh.Draw();
        }

        // 5. Afișăm cadrul terminat: back buffer <-> front buffer.
        SwapBuffers();
        _frames++;
    }

    /// <summary>Apelată O SINGURĂ DATĂ, la închiderea ferestrei: eliberăm resursele GPU.</summary>
    protected override void OnUnload()
    {
        foreach (SceneObject obj in _scene)
        {
            obj.Dispose();
        }
        _satelliteMesh?.Dispose();
        _shader.Dispose();

        base.OnUnload();
    }

    /// <summary>Transformă poziția mouse-ului (pixeli, origine stânga-sus) în coordonate de lume.</summary>
    private Vector2 ScreenToWorld(Vector2 mousePixels, float worldDepth = 0.0f)
    {
        float width = Math.Max(ClientSize.X, 1);
        float height = Math.Max(ClientSize.Y, 1);
        // T3: pixelii cresc spre jos, iar NDC spre sus; de aceea y_ndc este 1 - 2*y/H.
        float ndcX = 2.0f * mousePixels.X / width - 1.0f;
        float ndcY = 1.0f - 2.0f * mousePixels.Y / height;

        Matrix4 viewProjection = _camera.ViewMatrix * _camera.ProjectionMatrix;
        Matrix4 inverseViewProjection = Matrix4.Invert(viewProjection);

        if (!_camera.IsPerspective)
        {
            Vector4 world = new Vector4(ndcX, ndcY, 0.0f, 1.0f) * inverseViewProjection;
            if (MathF.Abs(world.W) > float.Epsilon) world /= world.W;
            return new Vector2(world.X, world.Y);
        }

        Vector4 nearHomogeneous = new Vector4(ndcX, ndcY, -1.0f, 1.0f) * inverseViewProjection;
        Vector4 farHomogeneous = new Vector4(ndcX, ndcY, 1.0f, 1.0f) * inverseViewProjection;
        Vector3 nearPoint = nearHomogeneous.Xyz / nearHomogeneous.W;
        Vector3 farPoint = farHomogeneous.Xyz / farHomogeneous.W;
        Vector3 rayDirection = farPoint - nearPoint;

        if (MathF.Abs(rayDirection.Z) <= float.Epsilon)
        {
            return new Vector2(nearPoint.X, nearPoint.Y);
        }

        float distance = (worldDepth - nearPoint.Z) / rayDirection.Z;
        Vector3 worldPoint = nearPoint + rayDirection * distance;
        return new Vector2(worldPoint.X, worldPoint.Y);
    }

    /// <summary>Tasta R: obiectele revin la transformările inițiale, camera la starea inițială.</summary>
    private void ResetScene()
    {
        for (int i = 0; i < _scene.Count; i++)
        {
            _scene[i].Transform = _initialTransforms[i].Clone();
        }
        _camera.Reset();
        _pivot = null;
        _orbitAngleDegrees = 0.0f;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("EGC - LABORATOR 04 - COMENZI");
        Console.WriteLine("H              - afiseaza aceasta lista");
        Console.WriteLine("ESC            - inchide aplicatia");
        Console.WriteLine("TAB            - selecteaza urmatorul obiect; satelitul il urmeaza");
        Console.WriteLine("SAGETI         - deplaseaza obiectul selectat");
        Console.WriteLine("Q / E          - roteste obiectul selectat");
        Console.WriteLine("Z / X          - micsoreaza / mareste obiectul selectat");
        Console.WriteLine("LEFT SHIFT     - miscare, rotatie si scalare de 3 ori mai rapide");
        Console.WriteLine("W / A / S / D  - deplaseaza camera");
        Console.WriteLine("SCROLL         - modifica zoom-ul camerei");
        Console.WriteLine("CLICK STANGA   - muta obiectul selectat sub cursor, pastrand adancimea");
        Console.WriteLine("CLICK DREAPTA  - stabileste pivotul sub cursor");
        Console.WriteLine("P              - cat timp este tinuta, roteste obiectul in jurul pivotului");
        Console.WriteLine("O              - schimba proiectia ortografica / perspectiva");
        Console.WriteLine("F              - schimba desenarea contur / plin");
        Console.WriteLine("R              - reseteaza obiectele, camera, pivotul si satelitul");
        Console.WriteLine("========================================");
    }

    /// <summary>
    /// Contur / plin. Dacă Visual Studio semnalează o eroare la primul parametru,
    /// folosiți aici tipul acceptat de versiunea voastră (vezi laboratorul 2, Problema 3):
    /// TriangleFace.FrontAndBack sau MaterialFace.FrontAndBack.
    /// </summary>
    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }
}
