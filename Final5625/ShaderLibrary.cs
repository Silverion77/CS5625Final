using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chireiden
{
    public class ShaderLibrary {

        public static ShaderProgram SsaoShader;
        public static ShaderProgram CubeShader;
        public static ShaderProgram BlenderShader;
        public static ShaderProgram TonemapShader;
        public static ShaderProgram AnimationShader;
        public static ShaderProgram LogLuminanceShader;
        public static ShaderProgram CopyShader;
        public static ShaderProgram BloomXShader;
        public static ShaderProgram BloomYShader;
        public static ShaderProgram ParticleShader;
        public static ShaderProgram POMShader;
        public static ShaderProgram TextShader;
        public static ShaderProgram LambertianShader;
        public static ShaderProgram FireShader;
        public static ShaderProgram ShadowMapShader;
        public static ShaderProgram ShadowMapViewerShader;

        public static void loadShaders() {
            SsaoShader = new ShaderProgram("shaders/simple.vert", "shaders/ssao.frag");
            CubeShader = new ShaderProgram("data/Simple_VS.vert", "data/Simple_FS.frag");
            BlenderShader = new ShaderProgram("shaders/pos_tex_nor_tan.vert", "shaders/blendermaterial.frag");
            TonemapShader = new ShaderProgram("shaders/simple2d.vert", "shaders/tonemap.frag");
            AnimationShader = new ShaderProgram("shaders/skeletal_mesh.vert", "shaders/blendermaterial.frag");
            LogLuminanceShader = new ShaderProgram("shaders/simple2d.vert", "shaders/logLuminance.frag");
            CopyShader = new ShaderProgram("shaders/simple2d.vert", "shaders/copy.frag");
            BloomXShader = new ShaderProgram("shaders/simple2d.vert", "shaders/bloomX.frag");
            BloomYShader = new ShaderProgram("shaders/simple2d.vert", "shaders/bloomY.frag");
            ParticleShader = new ShaderProgram("shaders/particle.vert", "shaders/particle.geom", "shaders/particle.frag");
            POMShader = new ShaderProgram("shaders/parallaxmapping.vert", "shaders/parallaxmapping.frag");
            TextShader = new ShaderProgram("shaders/on_screen_quad.vert", "shaders/fsq.frag");
            LambertianShader = new ShaderProgram("shaders/pos_tex_nor_tan.vert", "shaders/lambertian.frag");
            FireShader = new ShaderProgram("shaders/pos_tex_nor_tan.vert", "shaders/fireShader.frag");
            ShadowMapShader = new ShaderProgram("shaders/pos_tex_nor_tan.vert", "shaders/shadowmap.frag");
            ShadowMapViewerShader = new ShaderProgram("shaders/simple.vert", "shaders/view_shadowmap.frag");
        }
    }
}
