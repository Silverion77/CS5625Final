using System;
using System.Collections.Generic;

using OpenTK;

using Chireiden.Meshes;

namespace Chireiden.Materials
{
    class LambertianMaterial : Material
    {
        Vector4 color;
        Texture tex;
        Vector2 texScale;

        public LambertianMaterial(Vector4 col, Texture t, float scaleFactorX, float scaleFactorY)
        {
            color = col;
            tex = t;
            texScale = new Vector2(scaleFactorX, scaleFactorY);
        }

        public int useMaterialParameters(ShaderProgram program, int startTexUnit)
        {
            program.setUniformFloat4("mat_diffuseColor", color);
            program.setUniformFloat2("texScale", texScale);
            if (tex != null)
            {
                program.setUniformBool("mat_hasTexture", true);
                program.bindTexture2D("texture", startTexUnit, tex);
                return startTexUnit + 1;
            }
            else
            {
                program.setUniformBool("mat_hasTexture", false);
                return startTexUnit;
            }
        }

        public void unuseMaterialParameters(ShaderProgram program, int startTexUnit)
        {
            program.unbindTexture2D(startTexUnit);
        }
    }
}
