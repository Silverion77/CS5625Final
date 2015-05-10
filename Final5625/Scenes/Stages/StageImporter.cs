using System;
using System.Collections.Generic;
using System.IO;

using OpenTK;

namespace Chireiden.Scenes.Stages
{

    public struct StageData
    {
        public int Width;
        public int Height;
        public int TileSideLength;
        public int WallHeight;
        public int[,] Tiles;
        public Vector2 OkuuPosition;
        public Vector2 GoalPosition;
        public List<Vector2> ZombiePositions;
    }

    public class StageImporter
    {
        public static StageData importStageFromFile(string filename)
        {
            StageData data = new StageData();

            using (StreamReader sr = new StreamReader(filename))
            {
                // Read width
                string[] widthLine = sr.ReadLine().Split(' ');
                int width;
                if (!widthLine[0].Equals("width") || !Int32.TryParse(widthLine[1], out width))
                    throw new FormatException("Error reading width from level file");
                data.Width = width;

                string[] heightLine = sr.ReadLine().Split(' ');
                int height;
                if (!heightLine[0].Equals("height") || !Int32.TryParse(heightLine[1], out height))
                    throw new FormatException("Error reading height from level file");
                data.Height = height;


                string[] tileSideLine = sr.ReadLine().Split(' ');
                int tileSideLength;
                if (!tileSideLine[0].Equals("tileSideLength") || !Int32.TryParse(tileSideLine[1], out tileSideLength))
                    throw new FormatException("Error reading tileSideLength from level file");
                data.TileSideLength = tileSideLength;

                string[] wallHeightLine = sr.ReadLine().Split(' ');
                int wallHeight;
                if (!wallHeightLine[0].Equals("wallHeight") || !Int32.TryParse(wallHeightLine[1], out wallHeight))
                    throw new FormatException("Error reading wallHeight from level file");
                data.WallHeight = wallHeight;

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

                data.Tiles = tiles;

                string[] okuuLine = sr.ReadLine().Split(' ');
                int okuuX, okuuY;
                if (!okuuLine[0].Equals("okuu") || !Int32.TryParse(okuuLine[1], out okuuX) || !Int32.TryParse(okuuLine[2], out okuuY))
                    throw new FormatException("Error reading Okuu's position from level file");

                Vector2 okuuPosCoords = new Vector2(okuuX, okuuY);
                data.OkuuPosition = okuuPosCoords;

                string[] goalLine = sr.ReadLine().Split(' ');
                int goalX, goalY;
                if (!goalLine[0].Equals("goal") || !Int32.TryParse(goalLine[1], out goalX) || !Int32.TryParse(goalLine[2], out goalY))
                    throw new FormatException("Error reading goal position from level file");

                Vector2 goalPosCoords = new Vector2(goalX, goalY);
                data.GoalPosition = goalPosCoords;

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

                data.ZombiePositions = zombieFairyLocs;

                return data;
            }
        }

        public static World makeStageWorld(StageData data, out Stage stage, out UtsuhoReiuji okuu, out List<ZombieFairy> zombies)
        {
            World world = new World();
            stage = new Stage(data);
            world.addChild(stage);

            Vector3 okuuWorldPos = new Vector3(data.TileSideLength * (data.OkuuPosition.X + 0.5f),
                data.TileSideLength * (data.OkuuPosition.X + 0.5f), 0);

            okuu = new UtsuhoReiuji(okuuWorldPos);
            world.addChild(okuu);

            // This is just for debugging, so that we can see
            PointLight light = new PointLight(new Vector3(0, -2f, 4.3f), 2, 20, new Vector3(1, 1, 1));
            okuu.addChild(light);

            okuu.setStage(stage);

            Vector3 goalWorldPos = new Vector3(data.TileSideLength * (data.GoalPosition.X + 0.5f),
                data.TileSideLength * (data.GoalPosition.Y + 0.5f), 0);

            MeshNode goalFlagMesh = new MeshNode(MeshLibrary.GoalFlag, goalWorldPos);
            PointLight goalLight = new PointLight(new Vector3(0, -1f, 6), 2, 20, new Vector3(1, 1, 1));
            PointLight goalLight2 = new PointLight(new Vector3(0, 1f, 6), 2, 20, new Vector3(1, 1, 1));
            ParticleEmitter pe = new ParticleEmitter(new Vector3(0, 0, 8), 100f);

            Vector3 armBegin = new Vector3(-0.96f, 0, 2.97f);
            Vector3 armEnd = new Vector3(-2.44f, 0, 1.96f);

            for (int i = 0; i <= 5; i++)
            {
                MeshNode box = new MeshNode(MeshLibrary.HappySphere, Vector3.Lerp(armBegin, armEnd, i / 5f));
                okuu.addCollisionHitbox(box);
            }

            world.addChild(goalFlagMesh);
            goalFlagMesh.addChild(goalLight);
            goalFlagMesh.addChild(goalLight2);
            goalFlagMesh.addChild(pe);

            world.registerPointLight(light);
            world.registerPointLight(goalLight);
            world.registerPointLight(goalLight2);

            Console.WriteLine("TODO: Handle goal checking");

            zombies = new List<ZombieFairy>();

            foreach (Vector2 loc in data.ZombiePositions)
            {
                ZombieFairy fairy = new ZombieFairy(new Vector3(loc.X * data.TileSideLength, loc.Y * data.TileSideLength, 0));
                world.addChild(fairy);
                fairy.setStage(stage);
                zombies.Add(fairy);
            }

            return world;
        }
    }
}
