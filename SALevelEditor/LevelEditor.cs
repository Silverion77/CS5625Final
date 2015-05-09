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

        public void makeEdit(float realX, float realY, ObjectType type, int matID)
        {
            int coordX = (int)Math.Floor(realX);
            int coordY = (int)Math.Floor(realY);
            switch (type)
            {
                case ObjectType.Material:
                    currentLevel.setTile(coordX, coordY, matID);
                    break;
                case ObjectType.Okuu:
                    currentLevel.setOkuuPosition(coordX, coordY);
                    break;
                case ObjectType.Finish:
                    currentLevel.setGoalPosition(coordX, coordY);
                    break;
                case ObjectType.ZombieFairy:
                    if (matID == 0)
                        currentLevel.removeZombieFairy(realX, realY);
                    else
                        currentLevel.addZombieFairy(realX, realY);
                    break;
                default:
                    break;
            }
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

        public void exportToFile(string filename)
        {
            currentLevel.exportToFile(filename);
        }

        public void importFromFile(string filename)
        {
            StageData data = StageImporter.importStageFromFile(filename);
            currentLevel = new LevelMap(data);
        }
    }
}
