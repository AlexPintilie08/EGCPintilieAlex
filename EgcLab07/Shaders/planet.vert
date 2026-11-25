#version 330 core

layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aColor;
layout (location = 2) in vec2 aUV;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vColor;
out vec2 vUV;
out vec3 vLocalPos;
out vec3 vViewPos;
out vec3 vViewNormal;

void main()
{
    mat4 modelView = uView * uModel;
    vec4 viewPos = modelView * vec4(aPosition, 1.0);
    vColor = aColor;
    vUV = aUV;
    vLocalPos = aPosition;
    vViewPos = viewPos.xyz;
    vViewNormal = normalize(transpose(inverse(mat3(modelView))) * aPosition);
    gl_Position = uProjection * viewPos;
}
