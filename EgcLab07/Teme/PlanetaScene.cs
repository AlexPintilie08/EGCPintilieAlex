// Varianta C: planeta procedurala
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class PlanetaScene : Scene
{
    private const string NewPlanet = "genereaza alta planeta";
    private const string ToggleClouds = "arata / ascunde norii";
    private const string ToggleCities = "arata / ascunde luminile oraselor";
    private const string ToggleAtmosphere = "arata / ascunde atmosfera";

    private Material _planetMaterial = null!;
    private Material _atmosphereMaterial = null!;
    private SceneObject _planet = null!;
    private Tween _seedTween = null!;
    private Tween _cloudTween = null!;
    private Tween _atmosphereTween = null!;
    private readonly Random _random = new();
    private bool _cloudsOn = true;
    private bool _citiesOn = true;
    private bool _atmosphereOn = true;

    public override string Name => "planeta procedurala";

    public override string Status =>
        $"mare {_planetMaterial.Uniforms.Get<float>("uSeaLevel"):0.00}; " +
        $"relief {_planetMaterial.Uniforms.Get<float>("uTerrainScale"):0.0}; " +
        $"nori {(_cloudsOn ? "activi" : "opriti")}; " +
        $"orase {(_citiesOn ? "aprinse" : "stinse")}; " +
        $"atmosfera {(_atmosphereOn ? "activa" : "oprita")}; " +
        $"afisare {(ShowUV ? "UV" : "relief")}";

    public override IEnumerable<string> ActionNames =>
        new[] { NewPlanet, ToggleClouds, ToggleCities, ToggleAtmosphere };

    protected override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 3.0f, 9.0f);
        CameraTarget = new Vector3(0.0f, 1.8f, 0.0f);

        input.Bind(NewPlanet, Keys.G);
        input.Bind(ToggleClouds, Keys.Space);
        input.Bind(ToggleCities, Keys.K);
        input.Bind(ToggleAtmosphere, Keys.O);

        Mesh sphere = AddMesh(Shapes.ShadedSphere(1.0f, 96, 48));

        _planetMaterial = AddMaterial("planeta", "planet.vert", "planet.frag")
            .Param("uSeaLevel", 0.36f, 0.58f, 0.47f)
            .Param("uTerrainScale", 1.0f, 6.0f, 3.2f)
            .Param("uTurbulence", 0.0f, 1.5f, 0.9f)
            .Param("uCloudScale", 2.0f, 10.0f, 5.8f)
            .Param("uCloudSpeed", 0.0f, 0.3f, 0.06f)
            .Param("uCityIntensity", 0.0f, 2.0f, 1.1f);

        Vector3 seed = new(11.2f, 4.7f, 8.3f);
        _seedTween = new Tween(seed, seed, 2.5f, Easing.EaseInOut);
        _cloudTween = Tween.Float(0.82f, 0.82f, 0.7f, Easing.EaseInOut);
        _atmosphereTween = Tween.Float(1.0f, 1.0f, 0.7f, Easing.EaseInOut);

        _planetMaterial.Uniforms.Set("uSeed", seed);
        _planetMaterial.Uniforms.Set("uIceLatitude", 0.77f);
        _planetMaterial.Uniforms.Set("uCloudOpacity", _cloudTween.Scalar);
        _planetMaterial.Uniforms.Set("uCities", _citiesOn);
        _planetMaterial.Uniforms.Set("uLightDirection", new Vector3(-0.45f, 0.35f, 0.82f).Normalized());

        _atmosphereMaterial = AddMaterial("atmosfera", "planet.vert", "atmosphere.frag")
            .Param("uAtmosphereStrength", 0.0f, 0.9f, 0.48f);
        _atmosphereMaterial.Transparent = true;
        _atmosphereMaterial.Uniforms.Set("uAtmosphereAlpha", _atmosphereTween.Scalar);
        _atmosphereMaterial.Uniforms.Set("uAtmosphereColor", new Vector3(0.24f, 0.55f, 1.0f));

        _planet = AddObject("suprafata planetei", sphere, _planetMaterial);
        _planet.Transform.Position = new Vector3(0.0f, 1.8f, 0.0f);
        _planet.Transform.Scale = new Vector3(2.0f);
        _planet.Spin = new Vector3(0.0f, 7.0f, 0.0f);

        SceneObject atmosphere = AddObject("atmosfera", sphere, _atmosphereMaterial);
        atmosphere.Transform.Position = _planet.Transform.Position;
        atmosphere.Transform.Scale = new Vector3(2.08f);
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);

        if (input.WasPressed(NewPlanet))
        {
            Vector3 seed = new(_random.NextSingle() * 40.0f,
                               _random.NextSingle() * 40.0f,
                               _random.NextSingle() * 40.0f);
            _seedTween.GoTo(seed, 2.5f);
            ConsolePanel.PrintMessage("se transforma planeta; samanta noua in tranzitie");
        }
        if (input.WasPressed(ToggleClouds))
        {
            _cloudsOn = !_cloudsOn;
            _cloudTween.GoTo(_cloudsOn ? 0.82f : 0.0f, 0.7f);
        }
        if (input.WasPressed(ToggleCities)) _citiesOn = !_citiesOn;
        if (input.WasPressed(ToggleAtmosphere))
        {
            _atmosphereOn = !_atmosphereOn;
            _atmosphereTween.GoTo(_atmosphereOn ? 1.0f : 0.0f, 0.7f);
        }

        _seedTween.Update(dt);
        _cloudTween.Update(dt);
        _atmosphereTween.Update(dt);

        _planetMaterial.Uniforms.Set("uSeed", _seedTween.Value);
        _planetMaterial.Uniforms.Set("uCloudOpacity", _cloudTween.Scalar);
        _planetMaterial.Uniforms.Set("uCities", _citiesOn);
        _atmosphereMaterial.Uniforms.Set("uAtmosphereAlpha", _atmosphereTween.Scalar);
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        Vector3 seed = _seedTween.Value;
        yield return $"Nivelul marii : {_planetMaterial.Uniforms.Get<float>("uSeaLevel"):0.000}";
        yield return $"Samanta curenta: ({seed.X:0.00}, {seed.Y:0.00}, {seed.Z:0.00})";
        yield return $"Scara reliefului: {_planetMaterial.Uniforms.Get<float>("uTerrainScale"):0.00}; turbulenta {_planetMaterial.Uniforms.Get<float>("uTurbulence"):0.00}";
        yield return $"Norii           : scara {_planetMaterial.Uniforms.Get<float>("uCloudScale"):0.00}; viteza {_planetMaterial.Uniforms.Get<float>("uCloudSpeed"):0.00}";
        yield return $"Nori           : {(_cloudsOn ? "afisati" : "ascunsi")} (opacitate {_cloudTween.Scalar:0.00})";
        yield return $"Orase          : {(_citiesOn ? "luminate" : "stinse")}";
        yield return $"Atmosfera      : {(_atmosphereOn ? "activa" : "oprita")} ({_atmosphereTween.Scalar:0.00})";
        yield return $"Mod afisare    : {(ShowUV ? "coordonate UV" : "relief procedural")} (U)";
        yield return "Comenzi        : G planeta noua; Space nori; K orase; O atmosfera";
    }
}
