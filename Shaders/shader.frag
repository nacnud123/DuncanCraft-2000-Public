#version 330 core
            
in vec3 FragColor;
in vec2 TexCoord;
out vec4 color;
            
uniform sampler2D textureAtlas;
            
void main()
{
    // Sample the texture
    vec4 texColor = texture(textureAtlas, TexCoord);
    
    // Alpha test - discard fully transparent pixels
    if (texColor.a < 0.1)
        discard;
                
    // Mix texture with base color
    vec3 baseColor = texColor.rgb * FragColor;
                
    color = vec4(baseColor, texColor.a);
}