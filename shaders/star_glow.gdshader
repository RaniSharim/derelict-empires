shader_type spatial;
render_mode blend_add, unshaded, cull_disabled, depth_draw_never, shadows_disabled, fog_disabled;

uniform float glow_intensity : hint_range(0.0, 5.0) = 1.2;
uniform float core_radius : hint_range(0.01, 0.3) = 0.14;
uniform float glow_falloff : hint_range(1.0, 50.0) = 32.0;
uniform float pulse_speed : hint_range(0.0, 5.0) = 0.8;
uniform float pulse_amount : hint_range(0.0, 0.5) = 0.1;

varying float v_phase;

void vertex() {
	// Pass per-instance twinkle phase to fragment
	v_phase = INSTANCE_CUSTOM.x * 6.2832; // [0,1] → [0, TAU]

	// Billboard: face camera while preserving instance scale
	float sx = length(MODEL_MATRIX[0].xyz);
	float sy = length(MODEL_MATRIX[1].xyz);
	float sz = length(MODEL_MATRIX[2].xyz);
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
		vec4(INV_VIEW_MATRIX[0].xyz * sx, 0.0),
		vec4(INV_VIEW_MATRIX[1].xyz * sy, 0.0),
		vec4(INV_VIEW_MATRIX[2].xyz * sz, 0.0),
		MODEL_MATRIX[3]
	);
}

void fragment() {
	vec2 uv = UV - vec2(0.5);
	float dist = length(uv);

	// Desynchronized twinkle per star
	float twinkle = 1.0 + pulse_amount * sin(TIME * pulse_speed + v_phase);

	// Bright white-hot core with smooth falloff
	float core = smoothstep(core_radius, 0.0, dist);

	// Soft exponential glow halo
	float glow = exp(-dist * dist * glow_falloff) * glow_intensity * twinkle;

	// Faction color from MultiMesh instance color; core blends toward white
	vec3 faction_color = COLOR.rgb;
	vec3 final_color = mix(faction_color, vec3(1.0), core * 0.6) * (core * 1.5 + glow);

	float alpha = clamp(core + glow * 0.5, 0.0, 1.0);
	if (alpha < 0.005) discard;

	ALBEDO = final_color;
	ALPHA = alpha * COLOR.a;
}
