uniform sampler2D texture;
uniform vec4 origColor;
uniform vec4 replaceColor;

int areEqual(vec4 f1, vec4 f2) {
	if (distance(f1, f2) < 0.05)
		return 1;
	return 0;
}

void main() {
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
	vec4 pixel2 = vec4(pixel.r, pixel.g, pixel.b, pixel.a);

	if (areEqual(pixel2, origColor) == 1) {
		gl_FragColor = vec4(replaceColor.r, replaceColor.g, replaceColor.b, replaceColor.a);
	} else {
		gl_FragColor = pixel;
	}
}