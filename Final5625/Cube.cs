using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden
{

    public class Cube
    {
        int positionVboHandle;
        int normalVboHandle;
        int eboHandle;
        int vaoHandle;

        float scale = 1;
        Vector3 translation = new Vector3(0, 0, 0);
        Quaternion rotation = Quaternion.Identity;

        Vector3[] positionVboData = new Vector3[]{
            new Vector3(-1.0f, -1.0f,  1.0f),
            new Vector3( 1.0f, -1.0f,  1.0f),
            new Vector3( 1.0f,  1.0f,  1.0f),
            new Vector3(-1.0f,  1.0f,  1.0f),
            new Vector3(-1.0f, -1.0f, -1.0f),
            new Vector3( 1.0f, -1.0f, -1.0f), 
            new Vector3( 1.0f,  1.0f, -1.0f),
            new Vector3(-1.0f,  1.0f, -1.0f) };

        int[] indicesVboData = new int[]{
             // front face
                0, 1, 2, 2, 3, 0,
                // top face
                3, 2, 6, 6, 7, 3,
                // back face
                7, 6, 5, 5, 4, 7,
                // left face
                4, 0, 3, 3, 7, 4,
                // bottom face
                0, 1, 5, 5, 4, 0,
                // right face
                1, 5, 6, 6, 2, 1, };

        void CreateVBOs()
        {
            // Create the VBO for vertex positions
            positionVboHandle = GL.GenBuffer();
            // Bind the VBO we just created so that we can upload things to it
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            // Upload the actual positions to it
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.StaticDraw);

            // Create the VBO for vertex normals
            normalVboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
                new IntPtr(positionVboData.Length * Vector3.SizeInBytes),
                positionVboData, BufferUsageHint.StaticDraw);

            // Create the index buffer
            eboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
            GL.BufferData(BufferTarget.ElementArrayBuffer,
                new IntPtr(sizeof(uint) * indicesVboData.Length),
                indicesVboData, BufferUsageHint.StaticDraw);

            // Unbind our stuff to clean up
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        }

        void CreateVAOs()
        {
            // GL3 allows us to store the vertex layout in a "vertex array object" (VAO).
            // This means we do not have to re-issue VertexAttribPointer calls
            // every time we try to use a different vertex layout - these calls are
            // stored in the VAO so we simply need to bind the correct VAO.
            vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandle);

            // We're going to use index 0 in the VAO to refer to the vertex positions.
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

            // Use index 1 to refer to the vertex normals.
            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, normalVboHandle);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

            // Bind the index buffer so that we know what faces exist.
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

            // Unbind the VAO to clean up.
            GL.BindVertexArray(0);
        }

        public void update(FrameEventArgs e)
        {
        }

        /// <summary>
        /// Gives the matrix that transforms from this object's local space into its parent's space.
        /// </summary>
        /// <returns></returns>
        public Matrix4 modelMatrix()
        {
            Matrix4 scaleMat = Matrix4.Scale(scale);
            Matrix4 translationMat = Matrix4.CreateTranslation(translation);
            Matrix4 rotationMat = Matrix4.CreateFromQuaternion(rotation);
            return Matrix4.Mult(translationMat, Matrix4.Mult(rotationMat, scaleMat));
        }

        public void render(Matrix4 parentModel, Matrix4 projection)
        {
            int projectionMatrixLocation = Shaders.CubeShader.uniformLocation("projection_matrix");
            int modelviewMatrixLocation = Shaders.CubeShader.uniformLocation("modelview_matrix");

            GL.BindVertexArray(vaoHandle);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
            Shaders.CubeShader.use();

            Matrix4 localModelMat = modelMatrix();
            Matrix4 modelView = Matrix4.Mult(parentModel, localModelMat);

            GL.UniformMatrix4(projectionMatrixLocation, false, ref projection);
            GL.UniformMatrix4(modelviewMatrixLocation, false, ref modelView);

            GL.DrawElements(PrimitiveType.Triangles, indicesVboData.Length,
                DrawElementsType.UnsignedInt, IntPtr.Zero);

            Shaders.CubeShader.unuse();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
    }
}
