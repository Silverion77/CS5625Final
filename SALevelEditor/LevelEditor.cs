using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden;
using Chireiden.Scenes.Stages;

namespace SALevelEditor
{
    class LevelEditor
    {
        LevelMap currentLevel;

        public LevelEditor()
        {
            currentLevel = new LevelMap(30, 20);
        }

        public void editSquare(int x, int y, int newValue)
        {
            currentLevel.setTile(x, y, newValue);
        }

        public void render(Camera camera)
        {
            currentLevel.render(camera);
        }

        public Stage constructStage()
        {
            return currentLevel.constructStage();
        }
    }
}
