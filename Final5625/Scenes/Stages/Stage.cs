using System;
using System.Collections.Generic;
using System.Drawing;

using OpenTK;

using Chireiden.Materials;
using Chireiden.Meshes;

namespace Chireiden.Scenes.Stages
{
    public partial class Stage : SceneTreeNode
    {
        int[,] tiles;
        int width;
        int height;

        int tileSideLength;
        int wallHeight;

        List<StageTilesNode> stageElements;

        public Stage(int[,] tiles, int tileSideLength, int wallHeight)
        {
            this.tiles = tiles;
            width = tiles.GetUpperBound(0) + 1;
            height = tiles.GetUpperBound(1) + 1;

            this.tileSideLength = tileSideLength;
            this.wallHeight = wallHeight;

            stageElements = new List<StageTilesNode>();

            setUpStage(tiles, tileSideLength, wallHeight);

            toWorldMatrix = Matrix4.Identity;
            toParentMatrix = Matrix4.Identity;
        }

        public Stage(StageData data)
        {
            this.tiles = data.Tiles;
            this.tileSideLength = data.TileSideLength;
            this.wallHeight = data.WallHeight;
            this.width = data.Width;
            this.height = data.Height;

            stageElements = new List<StageTilesNode>();
            setUpStage(tiles, tileSideLength, wallHeight);

            toWorldMatrix = Matrix4.Identity;
            toParentMatrix = Matrix4.Identity;
        }

        static StageTilesNode stageMaterialOfID(int id, MaterialTileCollection matTiles)
        {
            switch (id)
            {
                default:
                    // Rock texture by http://agf81.deviantart.com/art/Stone-Texture-Seamless-197981741
                    Texture lambertTex = TextureManager.getTexture("data/texture/stone.jpg");
                    Material m = new LambertianMaterial(new Vector4(1, 1, 1, 1), lambertTex, 10, 10);
                    return new StageTilesNode(matTiles.Vertices.ToArray(), matTiles.Faces.ToArray(),
                        matTiles.Normals.ToArray(), matTiles.TexCoords.ToArray(), matTiles.Tangents.ToArray(), m);
            }
        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            // Nothing needs to be done here
        }

        public override void render(Camera camera)
        {
            // Just render each piece of the scene
            foreach (StageTilesNode stageElement in stageElements)
            {
                stageElement.render(camera);
            }
        }

        Point nearestInBounds(Point loc)
        {
            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(loc);
            Point current;
            while ((current = queue.Dequeue()) != null)
            {
                if (tiles[current.X, current.Y] > 0)
                {
                    return current;
                }
                // Enqueue all neighbors
                if (current.X > 0) queue.Enqueue(new Point(current.X - 1, current.Y));
                if (current.X < width - 1) queue.Enqueue(new Point(current.X + 1, current.Y));
                if (current.Y > 0) queue.Enqueue(new Point(current.X, current.Y - 1));
                if (current.Y < height - 1) queue.Enqueue(new Point(current.X, current.Y + 1));
            }
            throw new Exception("No points are in bounds");
        }

        bool tileInBounds(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return false;
            return tiles[x, y] > 0;
        }

        bool tileInBounds(float fx, float fy)
        {
            int x = (int)Math.Floor(fx);
            int y = (int)Math.Floor(fy);
            return tileInBounds(x, y);
        }

        bool tileInBounds(Vector2 v)
        {
            return tileInBounds(v.X, v.Y);
        }

        /// <summary>
        /// Compute the time at which the object, starting from baseLoc and moving
        /// along the vector moveVec once per time unit, entered the tile specified
        /// by the coordinates.
        /// </summary>
        bool computeWhenEntered(Vector2 baseLoc, Vector2 moveVec, int tileX, int tileY, out float time)
        {
            float borderX;
            float borderY;

            if (moveVec.X > 0) borderX = tileX;     // If we're traveling westward, we would be looking for the east wall
            else borderX = tileX + 1;               // Likewise, if we're going eastward, we would first go through the west wall
            if (moveVec.Y > 0) borderY = tileY;     // If we're going northward, we would first hit the south wall
            else borderY = tileY + 1;               // etc.

            float timeX = (borderX - baseLoc.X) / moveVec.X;
            float timeY = (borderY - baseLoc.Y) / moveVec.Y;

            bool xOK = false, yOK = false;

            // First check if, when the object entered the X-coordinate range,
            // it was in the Y-coordinate range as well
            if (moveVec.X != 0 && timeX >= 0)
            {
                Vector2 crossingPoint = baseLoc + (timeX * moveVec);
                if (tileY < crossingPoint.Y && crossingPoint.Y < tileY + 1)
                {
                    xOK = true;
                }
            }
            // Same for Y
            if (moveVec.Y != 0 && timeY >= 0)
            {
                Vector2 crossingPoint = baseLoc + (timeY * moveVec);
                if (tileX < crossingPoint.X && crossingPoint.X < tileX + 1)
                {
                    yOK = true;
                }
            }

            if (xOK && yOK)
            {
                time = Math.Min(timeX, timeY);
                return true;
            }
            else if (xOK)
            {
                time = timeX;
                return true;
            }
            else if (yOK)
            {
                time = timeY;
                return true;
            }
            else
            {
                time = 0;
                return false;
            }
        }

        public bool worldLocInBounds(Vector3 worldPos)
        {
            float mapX = worldPos.X / tileSideLength;
            float mapY = worldPos.Y / tileSideLength;
            return tileInBounds(mapX, mapY);
        }

        bool computeFloorCrossed(float origZ, float newZ, out float time)
        {
            float zDiff = newZ - origZ;
            if (zDiff == 0 || newZ > 0)
            {
                time = 0;
                return false;
            }
            time = -origZ / zDiff;
            if (time < 0 || time > 1) return false;
            return true;
        }

        /// <summary>
        /// Finds the nearest in-bounds location on the ray from oldPos to pos.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public Vector3 computeCollisionTime(Vector3 oldPos, Vector3 pos)
        {
            Vector2 oldMapCoords = new Vector2(oldPos.X / tileSideLength, oldPos.Y / tileSideLength);
            Vector2 mapCoords = new Vector2(pos.X / tileSideLength, pos.Y / tileSideLength);

            Vector2 mapMoveVec = mapCoords - oldMapCoords;

            int oldIntX = (int)Math.Floor(oldMapCoords.X);
            int oldIntY = (int)Math.Floor(oldMapCoords.Y);
            int intX = (int)Math.Floor(mapCoords.X);
            int intY = (int)Math.Floor(mapCoords.Y);

            int lowerX = Math.Min(intX, oldIntX);
            int upperX = Math.Max(intX, oldIntX);
            int lowerY = Math.Min(intY, oldIntY);
            int upperY = Math.Max(intY, oldIntY);

            float minTime = 100;

            for (int x = lowerX; x <= upperX; x++)
            {
                for (int y = lowerY; y <= upperY; y++)
                {
                    // We're only interested in when they first entered out-of-bounds squares
                    if (tileInBounds(x, y)) continue;
                    float crossingTime;
                    // If we didn't actually enter this square, skip it
                    if (!computeWhenEntered(oldMapCoords, mapMoveVec, x, y, out crossingTime)) continue;
                    minTime = Math.Min(minTime, crossingTime);
                }
            }

            float zTime;
            if (computeFloorCrossed(oldPos.Z, pos.Z, out zTime))
            {
                minTime = Math.Min(minTime, zTime);
            }

            if (minTime < 1)
            {
                Vector3 worldMoveVec = pos - oldPos;
                return oldPos + (minTime * worldMoveVec);
            }
            else return pos;
        }

        /// <summary>
        /// Moves the given position so that it is at least worldSpaceDistance
        /// away from each wall in its room.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="worldSpaceDistance"></param>
        /// <returns></returns>
        public Vector3 repelFromWall(Vector3 pos, float worldSpaceDistance)
        {
            float mapX = pos.X / tileSideLength;
            float mapY = pos.Y / tileSideLength;
            float distance = worldSpaceDistance / tileSideLength;

            int baseX = (int)Math.Floor(mapX);
            int baseY = (int)Math.Floor(mapY);

            float fractX = mapX - baseX;
            float fractY = mapY - baseY;

            if (distance < fractX && fractX < 1 - distance && distance < fractY && fractY < 1 - distance)
                return pos;

            if (fractX < distance && !tileInBounds(mapX - 1, mapY))
            {
                // If we're too close to a wall on the west side
                mapX = baseX + distance;
            }
            else if (fractX > 1 - distance && !tileInBounds(mapX + 1, mapY))
            {
                // If we're too close to a wall on the east side
                mapX = baseX + 1 - distance;
            }
            if (fractY < distance && !tileInBounds(mapX, mapY - 1))
            {
                // If we're too close to a wall on the south side
                mapY = baseY + distance;
            }
            else if (fractY > 1 - distance && !tileInBounds(mapX, mapY + 1))
            {
                // Wall on north side
                mapY = baseY + 1 - distance;
            }

            return new Vector3(mapX * tileSideLength, mapY * tileSideLength, pos.Z);
        }
    }
}
