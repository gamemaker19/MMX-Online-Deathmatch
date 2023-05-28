uniform sampler2D texture;
uniform float infectedFactor;

void main()
{
   vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
   if(pixel.a == 0.0)
   {
      gl_FragColor = pixel;
   }
   else
   {
      gl_FragColor = vec4(pixel.r + infectedFactor*(75.0/255.0), pixel.g - infectedFactor*(50.0/255.0), pixel.b + infectedFactor*(125.0/255.0), pixel.a);
   }
}