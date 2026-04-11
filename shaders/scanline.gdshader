shader_type canvas_item;

void fragment() {
    float line = mod(FRAGCOORD.y, 4.0);
    COLOR = vec4(0.0, 0.0, 0.0, line < 1.0 ? 0.05 : 0.0);
}
