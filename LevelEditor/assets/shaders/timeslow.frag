uniform sampler2D texture;
uniform float x;
uniform float y;
uniform float t;
uniform float r;

void main() {
	vec2 coords = gl_TexCoord[0].xy;
	vec4 pixel = texture2D(texture, coords);

	if (distance(vec2(coords.x * 1.33, coords.y), vec2(x * 1.33, y)) < r && (fract(t * 7.0) > r)) {
		float yOff = 0.0;
		if (coords.y - y > 0.0) {
			float tFactor = (t * 0.25);
			float yDiff = fract((coords.y - y) - tFactor);
			if ((yDiff > 0.05 && yDiff < 0.1) ||
				(yDiff > 0.4 && yDiff < 0.45) ||
				(yDiff > 0.8 && yDiff < 0.85)) {
				yOff = 0.05;
			}
		} else {
			float tFactor = (t * 0.25);
			float yDiff = fract((coords.y - y) + tFactor);
			if ((yDiff > 0.05 && yDiff < 0.1) ||
				(yDiff > 0.4 && yDiff < 0.45) ||
				(yDiff > 0.8 && yDiff < 0.85)) {
				yOff = 0.05;
			}
		}

		gl_FragColor = texture2D(texture, vec2(coords.x, coords.y + yOff));
	} else {
		gl_FragColor = texture2D(texture, coords);
	}
}