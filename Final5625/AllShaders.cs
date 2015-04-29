using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chireiden
{
    public class Shaders {
        public static ShaderProgram CubeShader = new ShaderProgram("data/Simple_VS.vert", "data/Simple_FS.frag");
    }
}
