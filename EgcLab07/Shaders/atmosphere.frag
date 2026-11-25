#version 330 core

in vec3 vColor;
in vec2 vUV;
in vec3 vViewPos;
in vec3 vViewNormal;

uniform bool uShowUV;
uniform vec3 uAtmosphereColor;
uniform float uAtmosphereStrength;
uniform float uAtmosphereAlpha;

out vec4 FragColor;

void main()
{
    if (uShowUV)
    {
        FragColor = vec4(vUV, 1.0 - vUV.x, 0.35);
        return;
    }

    vec3 normal = normalize(vViewNormal);
    vec3 viewDirection = normalize(-vViewPos);
    float rim = pow(1.0 - max(dot(normal, viewDirection), 0.0), 2.2);
    float alpha = clamp(rim * uAtmosphereStrength * uAtmosphereAlpha, 0.0, 0.85);
    FragColor = vec4(uAtmosphereColor * vColor, alpha);
}
