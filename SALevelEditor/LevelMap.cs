using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden;
using Chireiden.Meshes;
using Chireiden.Scenes.Stages;

namespace SALevelEditor
{
    class LevelMap
    {
        Vector2 dimensions;
        int[,] tiles;
        MapTexture texture;

        public const int tileSideLength = 20;
        public const int wallHeight = 50;

        static Texture radioactive = new Texture("textures/radioactive.png");

        public LevelMap(int width, int height)
        {
            tiles = new int[width, height];
            dimensions = new Vector2(width, height);
            texture = new MapTexture(tiles);
            Console.WriteLine("{0}: [{1}, {2}]; {3}: [{4}, {5}]", 0, tiles.GetLowerBound(0), tiles.GetUpperBound(0), 1, tiles.GetLowerBound(1), tiles.GetUpperBound(1));
            tiles[0, 1] = 1;
        }

        static Random rand = new Random();

        public void setRandomTile()
        {
            for (int i = 0; i < dimensions.X; i++)
            {
                for (int j = 0; j < dimensions.Y; j++)
                {
                    tiles[i, j] = rand.Next(2);
                }
            }
        }

        public void setTile(int x, int y, int newVal)
        {
            if (x >= 0 && x < dimensions.X && y >= 0 && y < dimensions.Y)
                tiles[x, y] = newVal;
        }

        public Stage constructStage()
        {
            Stage s = new Stage(tiles, tileSideLength, wallHeight);
            return s;
        }

        public void render(Camera camera)
        {
            Chireiden.ShaderProgram program = ShaderLibrary.MapShader;
            Matrix4 viewMatrix = camera.getViewMatrix();
            Matrix4 projectionMatrix = camera.getProjectionMatrix();

            texture.setTextureData(tiles);

            // Bind the stuff we need for this object (VAO, index buffer, program)
            GL.BindVertexArray(Utils.QuadVAO);

            program.use();

            program.setUniformMatrix4("projectionMatrix", projectionMatrix);
            program.setUniformMatrix4("viewMatrix", viewMatrix);
            program.setUniformFloat2("dimensions", dimensions);
            program.bindTextureRect("texture", 0, texture);

            GL.DrawElements(PrimitiveType.Triangles, Utils.QuadNumFaces,
                DrawElementsType.UnsignedInt, IntPtr.Zero);

            // Clean up
            program.unbindTextureRect(0);
            program.unuse();
            GL.BindVertexArray(0);
        }
    }
}
