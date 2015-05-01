using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chireiden
{
    public class Shaders {
        public static ShaderProgram CubeShader;
        public static ShaderProgram BlenderShader;
        public static ShaderProgram TonemapShader;

        public static void loadShaders() {
            CubeShader = new ShaderProgram("data/Simple_VS.vert", "data/Simple_FS.frag");
            BlenderShader = new ShaderProgram("shaders/pos_tex_nor_tan.vert", "shaders/blendermaterial.frag");
            TonemapShader = new ShaderProgram("shaders/simple2d.vert", "shaders/tonemap.frag");
        }
    }
}
