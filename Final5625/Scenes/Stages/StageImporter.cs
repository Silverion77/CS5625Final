using System;
using System.Collections.Generic;
using System.IO;

using OpenTK;

namespace Chireiden.Scenes.Stages
{
    class StageImporter
    {
        public static Stage importStageFromFile(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                // Read width
                string[] widthLine = sr.ReadLine().Split(' ');
                int width;
                if (!widthLine[0].Equals("width") || !Int32.TryParse(widthLine[1], out width))
                    throw new FormatException("Error reading width from level file");
                string[] heightLine = sr.ReadLine().Split(' ');
                int height;
                if (!heightLine[0].Equals("height") || !Int32.TryParse(heightLine[1], out height))
                    throw new FormatException("Error reading height from level file");


                string[] tileSideLine = sr.ReadLine().Split(' ');
                int tileSideLength;
                if (!heightLine[0].Equals("tileSideLength") || !Int32.TryParse(heightLine[1], out tileSideLength))
                    throw new FormatException("Error reading tileSideLength from level file");

                string[] wallHeightLine = sr.ReadLine().Split(' ');
                int wallHeight;
                if (!heightLine[0].Equals("wallHeight") || !Int32.TryParse(heightLine[1], out wallHeight))
                    throw new FormatException("Error reading wallHeight from level file");

                int[,] tiles = new int[width, height];

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        string[] tileLine = sr.ReadLine().Split(' ');
                        int matID;
                        if (!tileLine[0].Equals("tile") || !Int32.TryParse(tileLine[1], out matID))
                            throw new FormatException("Error reading tiles from level file");
                        tiles[x, y] = matID;
                    }
                }

                string[] okuuLine = sr.ReadLine().Split(' ');
                int okuuX, okuuY;
                if (!okuuLine[0].Equals("okuu") || !Int32.TryParse(okuuLine[1], out okuuX) || !Int32.TryParse(okuuLine[2], out okuuY))
                    throw new FormatException("Error reading Okuu's position from level file");

                Vector2 okuuPosCoords = new Vector2(okuuX, okuuY);

                string[] goalLine = sr.ReadLine().Split(' ');
                int goalX, goalY;
                if (!goalLine[0].Equals("goal") || !Int32.TryParse(goalLine[1], out goalX) || !Int32.TryParse(goalLine[2], out goalY))
                    throw new FormatException("Error reading goal position from level file");
                
                Vector2 goalPosCoords = new Vector2(goalX, goalY);

                List<Vector2> zombieFairyLocs = new List<Vector2>();
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    string[] pieces = line.Split(' ');
                    if (pieces[0] != "zombie") continue;
                    float zombieX, zombieY;
                    if (!float.TryParse(pieces[1], out zombieX) || !float.TryParse(pieces[2], out zombieY))
                        throw new FormatException("Error reading zombie fairy position from level file");
                    zombieFairyLocs.Add(new Vector2(zombieX, zombieY));
                }

                return new Stage(tiles, tileSideLength, wallHeight);
            }
        }
    }
}
