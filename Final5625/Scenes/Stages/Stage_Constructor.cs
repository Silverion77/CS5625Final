using System;
using System.Collections.Generic;

using OpenTK;

using Chireiden.Materials;

namespace Chireiden.Scenes.Stages
{
    public partial class Stage : SceneTreeNode
    {
        struct FloorTile
        {
            // The X coordinate of this floor tile
            public int X;
            // The Y coordinate of this floor tile
            public int Y;

            int tileSideLength;

            // The material that this floor should be rendered with
            public int MatID;

            static Vector3[] normals = new Vector3[] {
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 1)
            };

            static Vector4[] tangents = new Vector4[] {
                new Vector4(1, 0, 0, 1),
                new Vector4(1, 0, 0, 1),
                new Vector4(1, 0, 0, 1),
                new Vector4(1, 0, 0, 1)
            };

            public Vector3[] Vertices
            {
                get
                {
                    Vector3[] verts = new Vector3[4];
                    // Make the four corners
                    verts[0] = new Vector3(X * tileSideLength, Y * tileSideLength, 0); // lower left
                    verts[1] = new Vector3((X + 1) * tileSideLength, Y * tileSideLength, 0); // lower right
                    verts[2] = new Vector3(X * tileSideLength, (Y + 1) * tileSideLength, 0); // upper left
                    verts[3] = new Vector3((X + 1) * tileSideLength, (Y + 1) * tileSideLength, 0); // upper right
                    return verts;
                }
            }

            public Vector3[] Normals
            {
                get
                {
                    return normals;
                }
            }

            public Vector4[] Tangents
            {
                get
                {
                    return tangents;
                }
            }

            public Vector2[] TexCoords
            {
                get
                {
                    Vector2[] tex = new Vector2[4];
                    tex[0] = new Vector2(0, 0); // lower left
                    tex[1] = new Vector2(tileSideLength, 0); // lower right
                    tex[2] = new Vector2(0, tileSideLength); // upper left
                    tex[3] = new Vector2(tileSideLength, tileSideLength); // upper right
                    return tex;
                }
            }

            public FloorTile(int x, int y, int matID, int tileSideLength)
            {
                X = x;
                Y = y;
                MatID = matID;
                this.tileSideLength = tileSideLength;
            }

            public int[] Indices
            {
                get
                {
                    return new int[] { 0, 1, 2, 1, 3, 2 };
                }
            }
        }

        struct WallTile
        {
            // Every wall tile lies between an empty tile and a non-empty floor tile.
            // These are the coordinates of the empty tile.
            public int EmptyX;
            public int EmptyY;
            // These are the coordinates of the non-empty tile.
            public int FloorX;
            public int FloorY;

            // The material that this wall should be rendered with
            public int MatID;

            int tileSideLength;
            int wallHeight;

            Vector3 normal;
            Vector3 right;

            public WallTile(int emptyX, int emptyY, int floorX, int floorY, int tileSideLength, int wallHeight, int matID)
            {
                EmptyX = emptyX;
                EmptyY = emptyY;
                FloorX = floorX;
                FloorY = floorY;
                MatID = matID;

                // The normal will point from the empty room to to the floor room
                normal = new Vector3(floorX - emptyX, floorY - emptyY, 0);
                Vector3 up = Utils.UP;
                Vector3.Cross(ref up, ref normal, out right);
                right.Normalize();

                this.tileSideLength = tileSideLength;
                this.wallHeight = wallHeight;
            }

            /// <summary>
            /// Return the lower-left corner of this wall, as it would be seen when looking at it from the
            /// adjacent floor tile.
            /// </summary>
            /// <returns></returns>
            Vector3 lowerLeftCorner()
            {
                Vector3 result;
                // If this is a north wall, we want the upper-left corner of the floor tile
                if (EmptyY > FloorY)
                {
                    result = new Vector3(FloorX * tileSideLength, (FloorY + 1) * tileSideLength, 0); // upper left
                }
                // If this is a south wall, we want the bottom-right corner of the floor tile
                else if (EmptyY < FloorY)
                {
                    result = new Vector3((FloorX + 1) * tileSideLength, FloorY * tileSideLength, 0); // lower right
                }
                // If this is an east wall, we want the upper-right corner of the floor tile
                else if (EmptyX > FloorX)
                {
                    result = new Vector3((FloorX + 1) * tileSideLength, (FloorY + 1) * tileSideLength, 0); // upper right
                }
                // If this is a west wall, we want the lower-left corner of the floor tile
                else
                {
                    result = new Vector3(FloorX * tileSideLength, FloorY * tileSideLength, 0); // lower left
                }
                return result;
            }

            public Vector3[] Vertices
            {
                get
                {
                    Vector3[] verts = new Vector3[4];

                    // Start with the lower-left
                    verts[0] = lowerLeftCorner();
                    // To get to the lower-right, add the right vector
                    verts[1] = verts[0] + (right * tileSideLength);
                    // To get to the upper-left, add the up vector to the lower-left
                    verts[2] = verts[0] + (Utils.UP * wallHeight);
                    // To get to the upper-right, add the up vector to the lower-right
                    verts[3] = verts[1] + (Utils.UP * wallHeight);
                    return verts;
                }
            }

            public Vector3[] Normals
            {
                get
                {
                    return new Vector3[] { normal, normal, normal, normal };
                }
            }

            public Vector4[] Tangents
            {
                get
                {
                    Vector4 hand = new Vector4(right, 1);
                    return new Vector4[] { hand, hand, hand, hand };
                }
            }

            public Vector2[] TexCoords
            {
                get
                {
                    return new Vector2[] {
                    new Vector2(0, wallHeight),
                    new Vector2(tileSideLength, wallHeight),
                    new Vector2(0, 0),
                    new Vector2(tileSideLength, 0)
                };
                }
            }

            public int[] Indices
            {
                get
                {
                    return new int[] { 0, 1, 2, 1, 3, 2 };
                }
            }
        }

        class MaterialTileCollection
        {
            public List<Vector3> Vertices;
            public List<Vector3> Normals;
            public List<Vector2> TexCoords;
            public List<Vector4> Tangents;
            public List<int> Faces;

            public MaterialTileCollection()
            {
                Vertices = new List<Vector3>();
                Normals = new List<Vector3>();
                TexCoords = new List<Vector2>();
                Tangents = new List<Vector4>();
                Faces = new List<int>();
            }
        }

        void setUpStage(int[,] tiles, int tileSideLength, int wallHeight)
        {
            // First, we'll collect the set of non-empty tiles we need.
            List<FloorTile> floors = new List<FloorTile>();
            // We'll also collect the set of walls: a wall exists at the boundary between
            // every non-empty and empty tile pair.
            List<WallTile> walls = new List<WallTile>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (tiles[x, y] > 0)
                    {
                        // Then we know this floor is not empty
                        floors.Add(new FloorTile(x, y, tiles[x, y], tileSideLength));
                        // Now we check all its neighbors to see if they are empty or out of bounds,
                        // indicating the presence of walls

                        // Check north wall: add 1 to y
                        int northY = y + 1;
                        if (northY >= height || tiles[x, northY] == 0)
                        {
                            walls.Add(new WallTile(x, northY, x, y, tileSideLength, wallHeight, tiles[x, y]));
                        }
                        // Check south wall: subtract 1 from y
                        int southY = y - 1;
                        if (southY < 0 || tiles[x, southY] == 0)
                        {
                            walls.Add(new WallTile(x, southY, x, y, tileSideLength, wallHeight, tiles[x, y]));
                        }
                        // Check east wall: add 1 to x
                        int eastX = x + 1;
                        if (eastX >= width || tiles[eastX, y] == 0)
                        {
                            walls.Add(new WallTile(eastX, y, x, y, tileSideLength, wallHeight, tiles[x, y]));
                        }
                        // Check west wall: subtract 1 from x
                        int westX = x - 1;
                        if (westX < 0 || tiles[westX, y] == 0)
                        {
                            walls.Add(new WallTile(westX, y, x, y, tileSideLength, wallHeight, tiles[x, y]));
                        }
                    }
                }
            }

            Vector3[] floorNormals = new Vector3[] {
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, 1)
            };

            Vector3[] floorTangents = new Vector3[] {
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 0)
            };

            // Now all the walls and floors are in the lists; we just need to create the vertices
            // Each wall and floor is just a big square, so 4 vertices per element
            int numVertices = 4 * floors.Count + 4 * walls.Count;
            // 2 triangles per square = 6 indices per square
            int numFaces = 6 * floors.Count + 6 * walls.Count;

            // Let's separate all the floors and walls out by material, so that they can be shaded differently
            Dictionary<int, MaterialTileCollection> geometryByMaterial = new Dictionary<int, MaterialTileCollection>();

            // Process floors
            foreach (FloorTile floor in floors)
            {
                MaterialTileCollection tileData;
                if (!geometryByMaterial.TryGetValue(floor.MatID, out tileData))
                {
                    tileData = new MaterialTileCollection();
                    geometryByMaterial.Add(floor.MatID, tileData);
                }
                int startIndex = tileData.Vertices.Count;
                Vector3[] tileVerts = floor.Vertices;
                Vector3[] tileNormals = floor.Normals;
                Vector2[] tileTex = floor.TexCoords;
                Vector4[] tileTan = floor.Tangents;
                int[] tileFaces = floor.Indices;
                // Copy all vertex data into the collection
                for (int i = 0; i < 4; i++)
                {
                    tileData.Vertices.Add(tileVerts[i]);
                    tileData.Normals.Add(tileNormals[i]);
                    tileData.TexCoords.Add(tileTex[i]);
                    tileData.Tangents.Add(tileTan[i]);
                }
                // Copy all face data, but with the appropriate offset -- the new vertices range
                // from [startIndex, startIndex + 3], so we can just add this to the unshifted indices
                for (int i = 0; i < 6; i++)
                {
                    tileData.Faces.Add(tileFaces[i] + startIndex);
                }
            }

            // Process walls
            foreach (WallTile wall in walls)
            {
                // Same procedure as for floors
                MaterialTileCollection tileData;
                if (!geometryByMaterial.TryGetValue(wall.MatID, out tileData))
                {
                    tileData = new MaterialTileCollection();
                    geometryByMaterial.Add(wall.MatID, tileData);
                }
                int startIndex = tileData.Vertices.Count;

                Vector3[] tileVerts = wall.Vertices;
                Vector3[] tileNormals = wall.Normals;
                Vector2[] tileTex = wall.TexCoords;
                Vector4[] tileTan = wall.Tangents;
                int[] tileFaces = wall.Indices;
                // Copy all vertex data into the collection
                for (int i = 0; i < 4; i++)
                {
                    tileData.Vertices.Add(tileVerts[i]);
                    tileData.Normals.Add(tileNormals[i]);
                    tileData.TexCoords.Add(tileTex[i]);
                    tileData.Tangents.Add(tileTan[i]);
                }
                // Copy all face data, but with the appropriate offset -- the new vertices range
                // from [startIndex, startIndex + 3], so we can just add this to the unshifted indices
                for (int i = 0; i < 6; i++)
                {
                    tileData.Faces.Add(tileFaces[i] + startIndex);
                }
            }

            // Finally, we've gathered all the stuff, so make some nodes and materials out of it

            foreach (int matID in geometryByMaterial.Keys)
            {
                MaterialTileCollection matTiles;
                geometryByMaterial.TryGetValue(matID, out matTiles);

                StageTilesNode stn = stageMaterialOfID(matID, matTiles);
                stageElements.Add(stn);
            }
        }
    }
}
