using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden
{
    public class Utils
    {
        /// <summary>
        /// Returns the upper-left 3x3 submatrix of a 4x4 matrix.
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Matrix3 get3x3Part(Matrix4 mat)
        {
            Matrix3 outMat = new Matrix3();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    outMat[i, j] = mat[i, j];
                }
            }
            return outMat;
        }

        /// <summary>
        /// Computes the normal transformation matrix from the modelView matrix.
        /// </summary>
        /// <param name="modelView"></param>
        /// <returns></returns>
        public static Matrix3 normalMatrix(Matrix4 modelView)
        {
            Matrix3 normalMat = get3x3Part(modelView);
            normalMat.Invert();
            normalMat.Transpose();
            return normalMat;
        }

        public static Vector3 FORWARD = -Vector3.UnitY;
        public static Vector3 UP = Vector3.UnitZ;

        /// <summary>
        /// Computes the angle needed to rotate normal vector v1 onto normal vector v2.
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static double xyPlaneRotation(Vector3 v1, Vector3 v2)
        {
            return Math.Atan2(v2.Y, v2.X) - Math.Atan2(v1.Y, v1.X);
        }

        /// <summary>
        /// Computes the world-space rotation in the XY plane corresponding to normal vector dir.
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static double worldRotationOfDir(Vector3 dir)
        {
            return xyPlaneRotation(FORWARD, dir);
        }

        static Random random = new Random();

        public static double randomDouble()
        {
            return random.NextDouble();
        }

        /// <summary>
        /// Returns a random double in the range (lowerBound, upperBound).
        /// </summary>
        /// <param name="lowerBound"></param>
        /// <param name="upperBound"></param>
        /// <returns></returns>
        public static double randomDouble(double lowerBound, double upperBound)
        {
            double d = random.NextDouble();
            double range = upperBound - lowerBound;
            return lowerBound + range * d;
        }
        
        static Vector3[] vertices = new Vector3[] {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 1, 0)
        };

        static int[] faces = new int[] {
            0, 1, 2, 1, 3, 2
        };

        static Vector2[] texCoords = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        static int positionVboHandle = 0;
        static int texCoordVboHandle = 0;
        static int eboHandle = 0;
        static int vaoHandle = 0;

        static void createVBOs()
        {
            // Create the VBO for vertex positions
            positionVboHandle = GL.GenBuffer();
            // Bind the VBO we just created so that we can upload things to it
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            // Upload the actual positions to it
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(vertices.Length * Vector3.SizeInBytes),
                vertices, BufferUsageHint.StaticDraw);

            // Create the VBO for texture coordinates
            texCoordVboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordVboHandle);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer,
                new IntPtr(texCoords.Length * Vector2.SizeInBytes),
                texCoords, BufferUsageHint.StaticDraw);

            // Create the index buffer
            eboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                new IntPtr(sizeof(uint) * faces.Length),
                faces, BufferUsageHint.StaticDraw);

            // Unbind our stuff to clean up
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        static void createVAO()
        {
            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            // We're going to use index 0 in the VAO to refer to the vertex positions.
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

            // Use index 1 to refer to the texture coordinates.
            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, texCoordVboHandle);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);

            // Bind the index buffer so that we know what faces exist.
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

            // Unbind the VAO to clean up.
            GL.BindVertexArray(0);
        }

        static Utils()
        {
            createVBOs();
            createVAO();
        }

        /// <summary>
        /// A preset VAO for rendering a rectangle on screen.
        /// Has the 4 vertices and 6 indices for faces bound, as well as the
        /// appropriate texture coordinates.
        /// Index 0 is the vertex positions, and index 1 is the texture coordinates.
        /// </summary>
        public static int QuadVAO { get { return vaoHandle; } }
        public static int QuadNumFaces { get { return faces.Length; } }
    }
}
