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
    public partial class LevelMap
    {
        Vector2 dimensions;
        int[,] tiles;
        MapTexture texture;

        public const int tileSideLength = 20;
        public const int wallHeight = 50;

        static Texture radioactive = new Texture("textures/radioactive.png");
        static Texture okuu = new Texture("textures/okuu.png");
        static Texture zombie = new Texture("textures/zombie_fairy.png");
        static Texture finish = new Texture("textures/finish_line.png");

        Vector2 okuuPosition;
        Vector2 goalPosition;
        List<Vector2> zombieFairies;

        public LevelMap(int width, int height)
        {
            tiles = new int[width, height];
            dimensions = new Vector2(width, height);
            texture = new MapTexture(tiles);
            tiles[0, 1] = 1;
            zombieFairies = new List<Vector2>();
            okuuPosition = new Vector2(0, 0);
            goalPosition = new Vector2(1, 1);
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

        public void setOkuuPosition(int x, int y)
        {
            okuuPosition.X = x;
            okuuPosition.Y = y;
        }

        public void setGoalPosition(int x, int y)
        {
            goalPosition.X = x;
            goalPosition.Y = y;
        }

        public void addZombieFairy(float x, float y)
        {
            zombieFairies.Add(new Vector2(x, y));
        }

        public void removeZombieFairy(float x, float y)
        {
            Vector2 loc;
            if (!findNearestZombie(new Vector2(x, y), out loc))
            {
                return;
            }
            zombieFairies.Remove(loc);
        }

        public Stage constructStage()
        {
            Stage s = new Stage(tiles, tileSideLength, wallHeight);
            return s;
        }

        public void render(Camera camera)
        {
            GL.Disable(EnableCap.DepthTest);

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

            drawOkuu(projectionMatrix, viewMatrix);
            drawFinish(projectionMatrix, viewMatrix);
            drawZombies(projectionMatrix, viewMatrix);

            GL.Enable(EnableCap.DepthTest);
        }

        public bool findNearestZombie(Vector2 loc, out Vector2 close)
        {
            float dist = 2;
            Vector2 closest = Vector2.Zero;
            foreach (Vector2 zombieLoc in zombieFairies)
            {
                Vector2 diff = zombieLoc - loc;
                float newDist = diff.Length;
                if (newDist < dist)
                {
                    dist = newDist;
                    closest = zombieLoc;
                }
            }
            if (dist < 0.4f)
            {
                close = closest;
                return true;
            }
            close = Vector2.Zero;
            return false;
        }

        void drawOkuu(Matrix4 proj, Matrix4 view)
        {
            drawIconAtPos(okuuPosition + new Vector2(0.1f, 0.1f), new Vector2(0.8f, 0.8f), okuu, proj, view);
        }

        void drawFinish(Matrix4 proj, Matrix4 view)
        {
            drawIconAtPos(goalPosition + new Vector2(0.1f, 0.1f), new Vector2(0.8f, 0.8f), finish, proj, view);
        }

        void drawZombies(Matrix4 proj, Matrix4 view)
        {
            foreach (Vector2 loc in zombieFairies)
            {
                drawIconAtPos(loc - new Vector2(0.2f, 0.2f), new Vector2(0.4f, 0.4f), zombie, proj, view);
            }
        }

        void drawIconAtPos(Vector2 drawPos, Vector2 drawDims, Texture t, Matrix4 projectionMatrix, Matrix4 viewMatrix)
        {
            // Draw Okuu's position
            GL.BindVertexArray(Utils.QuadVAO);

            Chireiden.ShaderProgram program = ShaderLibrary.DisplayTexture;
            program.use();

            program.setUniformMatrix4("projectionMatrix", projectionMatrix);
            program.setUniformMatrix4("viewMatrix", viewMatrix);

            program.setUniformFloat2("dimensions", drawDims);
            program.setUniformFloat2("position", drawPos);
            program.bindTexture2D("texture", 0, t);

            GL.DrawElements(PrimitiveType.Triangles, Utils.QuadNumFaces,
                DrawElementsType.UnsignedInt, IntPtr.Zero);

            // Clean up
            program.unbindTexture2D(0);
            program.unuse();
            GL.BindVertexArray(0);
        }
    }
}
