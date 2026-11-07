shader_type spatial;
// Inverted-hull cartoon outline: render back faces pushed out along the normal.
render_mode cull_front, unshaded;

uniform vec4 outline_color : source_color = vec4(0.06, 0.03, 0.12, 1.0);
uniform float thickness = 0.035;

void vertex() {
	VERTEX += NORMAL * thickness;
}

void fragment() {
	ALBEDO = outline_color.rgb;
}
