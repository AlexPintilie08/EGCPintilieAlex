#version 330 core

in vec3 vColor;
in vec2 vUV;
in vec3 vLocalPos;
in vec3 vViewNormal;

uniform bool uShowUV;
uniform bool uCities;
uniform float uTime;
uniform float uSeaLevel;
uniform float uTerrainScale;
uniform float uTurbulence;
uniform float uCloudScale;
uniform float uCloudSpeed;
uniform float uCloudOpacity;
uniform float uCityIntensity;
uniform float uIceLatitude;
uniform vec3 uSeed;
uniform vec3 uLightDirection;

out vec4 FragColor;

float hash3(vec3 p)
{
    return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

float valueNoise(vec3 p)
{
    vec3 i = floor(p);
    vec3 f = fract(p);
    vec3 u = f * f * (3.0 - 2.0 * f);

    float n000 = hash3(i + vec3(0.0, 0.0, 0.0));
    float n100 = hash3(i + vec3(1.0, 0.0, 0.0));
    float n010 = hash3(i + vec3(0.0, 1.0, 0.0));
    float n110 = hash3(i + vec3(1.0, 1.0, 0.0));
    float n001 = hash3(i + vec3(0.0, 0.0, 1.0));
    float n101 = hash3(i + vec3(1.0, 0.0, 1.0));
    float n011 = hash3(i + vec3(0.0, 1.0, 1.0));
    float n111 = hash3(i + vec3(1.0, 1.0, 1.0));

    float x00 = mix(n000, n100, u.x);
    float x10 = mix(n010, n110, u.x);
    float x01 = mix(n001, n101, u.x);
    float x11 = mix(n011, n111, u.x);
    float y0 = mix(x00, x10, u.y);
    float y1 = mix(x01, x11, u.y);
    return mix(y0, y1, u.z);
}

float fbm(vec3 p)
{
    float sum = 0.0;
    float amplitude = 0.5;
    float total = 0.0;
    for (int i = 0; i < 5; i++)
    {
        sum += amplitude * valueNoise(p);
        total += amplitude;
        p = p * 2.03 + vec3(17.1, 31.7, 9.2);
        amplitude *= 0.5;
    }
    return sum / total;
}

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 1.0 - vUV.x, 1.0);
        return;
    }

    vec3 dir = normalize(vLocalPos);
    vec3 terrainPoint = dir * uTerrainScale + uSeed;
    float broad = fbm(terrainPoint);
    float detail = fbm(dir * uTerrainScale * 2.5 + uSeed * 1.7);
    float height = clamp(0.72 * broad + 0.28 * detail + (detail - 0.5) * uTurbulence * 0.12, 0.0, 1.0);

    vec3 deepWater = vec3(0.015, 0.065, 0.22);
    vec3 shallowWater = vec3(0.04, 0.43, 0.67);
    vec3 sand = vec3(0.76, 0.66, 0.38);
    vec3 grass = vec3(0.24, 0.52, 0.19);
    vec3 forest = vec3(0.055, 0.30, 0.14);
    vec3 rock = vec3(0.40, 0.35, 0.30);
    vec3 snow = vec3(0.88, 0.93, 0.98);

    vec3 color = mix(deepWater, shallowWater,
        smoothstep(uSeaLevel - 0.20, uSeaLevel - 0.02, height));
    color = mix(color, sand, smoothstep(uSeaLevel - 0.012, uSeaLevel + 0.018, height));
    color = mix(color, grass, smoothstep(uSeaLevel + 0.02, uSeaLevel + 0.085, height));
    color = mix(color, forest, smoothstep(uSeaLevel + 0.08, uSeaLevel + 0.16, height));
    color = mix(color, rock, smoothstep(uSeaLevel + 0.16, uSeaLevel + 0.24, height));
    color = mix(color, snow, smoothstep(uSeaLevel + 0.24, uSeaLevel + 0.31, height));

    float polarNoise = fbm(dir * 8.0 + uSeed * 1.3);
    float iceEdge = uIceLatitude + (polarNoise - 0.5) * 0.12;
    float ice = smoothstep(iceEdge - 0.012, iceEdge + 0.012, abs(dir.y));
    color = mix(color, snow, ice);

    float light = max(dot(normalize(vViewNormal), normalize(uLightDirection)), 0.0);
    color *= (0.22 + 0.78 * light) * vColor;

    float land = smoothstep(uSeaLevel + 0.02, uSeaLevel + 0.06, height);
    float night = 1.0 - smoothstep(-0.08, 0.12, dot(normalize(vViewNormal), normalize(uLightDirection)));
    float cityNoise = fbm(dir * 42.0 + uSeed * 5.0);
    float cities = smoothstep(0.70, 0.84, cityNoise) * land * night;
    color += vec3(1.0, 0.56, 0.18) * cities * (uCities ? uCityIntensity : 0.0);

    vec3 cloudPoint = dir * uCloudScale + uSeed * 2.1 + vec3(uTime * uCloudSpeed, 0.0, 0.0);
    float cloudNoise = fbm(cloudPoint);
    float clouds = smoothstep(0.56, 0.70, cloudNoise) * uCloudOpacity;
    color = mix(color, vec3(0.91, 0.96, 1.0), clouds);

    FragColor = vec4(color, 1.0);
}
