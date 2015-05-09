using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Chireiden;

namespace SALevelEditor
{
    public class ShaderLibrary
    {
        public static ShaderProgram MapShader;
        public static ShaderProgram DisplayTexture;


        public static void LoadShaders()
        {
            MapShader = new ShaderProgram("shaders/rectangle.vert", "shaders/mapGrid.frag");
            DisplayTexture = new ShaderProgram("shaders/worldSpaceRect.vert", "shaders/fsq.frag");
        }
        
    }
}
