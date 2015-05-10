using System;
using System.IO;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Meshes;

namespace Chireiden.Materials
{
    public class FireMaterial : Material
    {
        Texture noiseTexture;
        Texture fireTexture;
        
        Vector3 scrollSpeeds = new Vector3(1f, 2f, 4f);

        public FireMaterial()
        {
            noiseTexture = TextureManager.getTexture("data/texture/noise.jpg");
            fireTexture = TextureManager.getTexture("data/texture/fire.jpg");
        }

        public int useMaterialParameters(ShaderProgram program, int startTexUnit)
        {
            program.setUniformFloat3("un_ScrollSpeeds", scrollSpeeds);

            program.bindTexture2D("un_NoiseTexture", startTexUnit, noiseTexture);
            program.bindTexture2D("un_FireTexture", startTexUnit + 1, fireTexture);

            return startTexUnit + 2;
        }

        public void unuseMaterialParameters(ShaderProgram program, int startTexUnit)
        {
            program.unbindTexture2D(startTexUnit);
            program.unbindTexture2D(startTexUnit + 1);
        }
    }
}
