using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Meshes;

namespace Chireiden
{
    public class ShaderProgram
    {
        /// <summary>
        /// The maximum number of point lights that can affect a single shader.
        /// </summary>
        public const int MAX_LIGHTS = 40;

        /// <summary>
        /// The maximum number of bones that we can have in a skeleton.
        /// </summary>
        public const int MAX_BONES = 256;

        /// <summary>
        /// The maximum number of bones that can affect a single vertex.
        /// This is rarely more than 4 (for Okuu, it is exactly 4).
        /// </summary>
        public const int MAX_BONES_PER_VERTEX = 4;

        public int programHandle { get; private set; }

        public ShaderProgram(string vertFile, string fragFile)
        {
            programHandle = createShaderFromFiles(vertFile, fragFile);
            // TODO: get list of uniforms, basically copy CS 5625 framework
        }
        public ShaderProgram(string vertFile, string geomFile, string fragFile)
        {
            programHandle = createShaderFromFiles(vertFile, geomFile, fragFile);
            // TODO: get list of uniforms, basically copy CS 5625 framework
        }
        public int createShaderFromFiles(string vertFile, string fragFile)
        {
            string vertSource, fragSource;
            using (StreamReader sr = new StreamReader(vertFile))
            {
                vertSource = sr.ReadToEnd();
            }
            using (StreamReader sr = new StreamReader(fragFile))
            {
                fragSource = sr.ReadToEnd();
            }
            return createShaderFromSource(vertSource, fragSource);
        }

        public int createShaderFromFiles(string vertFile, string geomFile, string fragFile)
        {
            string vertSource, geomSource, fragSource;
            using (StreamReader sr = new StreamReader(vertFile))
            {
                vertSource = sr.ReadToEnd();
            }
            using (StreamReader sr = new StreamReader(geomFile))
            {
                geomSource = sr.ReadToEnd();
            }
            using (StreamReader sr = new StreamReader(fragFile))
            {
                fragSource = sr.ReadToEnd();
            }
            return createShaderFromSource(vertSource, geomSource, fragSource);
        }

        public int createShaderFromSource(string vertSource, string fragSource)
        {
            // Compile vert and frag shaders
            int vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            int fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(vertexShaderHandle, vertSource);
            GL.ShaderSource(fragmentShaderHandle, fragSource);

            GL.CompileShader(vertexShaderHandle);
            GL.CompileShader(fragmentShaderHandle);

            int vertStatus, fragStatus;

            GL.GetShader(vertexShaderHandle, ShaderParameter.CompileStatus, out vertStatus);
            GL.GetShader(fragmentShaderHandle, ShaderParameter.CompileStatus, out fragStatus);

            if (vertStatus != 1)
            {
                String error =  GL.GetShaderInfoLog(vertexShaderHandle);
                Console.WriteLine("Vertex shader compilation failed: {0}", error);
            }
            if (fragStatus != 1)
            {
                String error = GL.GetShaderInfoLog(fragmentShaderHandle);
                Console.WriteLine("Fragment shader compilation failed: {0}", error);
            }

            // Create program
            int shaderProgramHandle = GL.CreateProgram();

            GL.AttachShader(shaderProgramHandle, vertexShaderHandle);
            GL.AttachShader(shaderProgramHandle, fragmentShaderHandle);

            GL.LinkProgram(shaderProgramHandle);
            int linkStatus;

            GL.GetProgram(shaderProgramHandle, GetProgramParameterName.LinkStatus, out linkStatus);
            if (linkStatus != 1)
            {
                Console.WriteLine("Shader linking compilation failed: {0}", GL.GetProgramInfoLog(shaderProgramHandle));

            }

            Debug.WriteLine(GL.GetProgramInfoLog(shaderProgramHandle));
            return shaderProgramHandle;
        }

        public int createShaderFromSource(string vertSource, string geomSource, string fragSource)
        {
            // Compile vert and frag shaders
            int vertexShaderHandle = GL.CreateShader(ShaderType.VertexShader);
            int geometryShaderHandle = GL.CreateShader(ShaderType.GeometryShader);
            int fragmentShaderHandle = GL.CreateShader(ShaderType.FragmentShader);

            GL.ShaderSource(vertexShaderHandle, vertSource);
            GL.ShaderSource(geometryShaderHandle, geomSource);
            GL.ShaderSource(fragmentShaderHandle, fragSource);

            GL.CompileShader(vertexShaderHandle);
            GL.CompileShader(geometryShaderHandle);
            GL.CompileShader(fragmentShaderHandle);

            int vertStatus, geomStatus, fragStatus;

            GL.GetShader(vertexShaderHandle, ShaderParameter.CompileStatus, out vertStatus);
            GL.GetShader(geometryShaderHandle, ShaderParameter.CompileStatus, out geomStatus);
            GL.GetShader(fragmentShaderHandle, ShaderParameter.CompileStatus, out fragStatus);

            if (vertStatus != 1)
            {
                String error = GL.GetShaderInfoLog(vertexShaderHandle);
                Console.WriteLine("Vertex shader compilation failed: {0}", error);
            }
            if (geomStatus != 1)
            {
                String error = GL.GetShaderInfoLog(geometryShaderHandle);
                Console.WriteLine("Geometry shader compilation failed: {0}", error);
            }
            if (fragStatus != 1)
            {
                String error = GL.GetShaderInfoLog(fragmentShaderHandle);
                Console.WriteLine("Fragment shader compilation failed: {0}", error);
            }

            // Create program
            int shaderProgramHandle = GL.CreateProgram();

            GL.AttachShader(shaderProgramHandle, vertexShaderHandle);
            GL.AttachShader(shaderProgramHandle, geometryShaderHandle);
            GL.AttachShader(shaderProgramHandle, fragmentShaderHandle);

            GL.LinkProgram(shaderProgramHandle);
            int linkStatus;

            GL.GetProgram(shaderProgramHandle, GetProgramParameterName.LinkStatus, out linkStatus);
            if (linkStatus != 1)
            {
                Console.WriteLine("Shader linking compilation failed: {0}", GL.GetProgramInfoLog(shaderProgramHandle));

            }

            Debug.WriteLine(GL.GetProgramInfoLog(shaderProgramHandle));
            return shaderProgramHandle;
        }

        public void use()
        {
            GL.UseProgram(programHandle);
        }

        public void unuse()
        {
            GL.UseProgram(0);
        }

        public int uniformLocation(string uniform)
        {
            return GL.GetUniformLocation(programHandle, uniform);
        }

        public void setUniformMatrix4(string name, Matrix4 mat)
        {
            int unifLoc = uniformLocation(name);
            GL.UniformMatrix4(unifLoc, false, ref mat);
        }

        public void setUniformInt1(string name, int unif)
        {
            int unifLoc = uniformLocation(name);
            GL.Uniform1(unifLoc, unif);
        }

        public void setUniformFloat1(string name, float unif)
        {
            int unifLoc = uniformLocation(name);
            GL.Uniform1(unifLoc, unif);
        }

        public void setUniformFloat2(string name, Vector2 unif)
        {
            int unifLoc = uniformLocation(name);
            GL.Uniform2(unifLoc, unif);
        }

        public void setUniformFloat3(string name, Vector3 unif)
        {
            int unifLoc = uniformLocation(name);
            GL.Uniform3(unifLoc, unif);
        }

        public void setUniformFloat4(string name, Vector4 unif)
        {
            int unifLoc = uniformLocation(name);
            GL.Uniform4(unifLoc, unif);
        }

        public void setUniformFloatArray(string name, float[] unif)
        {
            int unifLoc = uniformLocation(name);
            GL.Uniform1(unifLoc, unif.Length, unif);
        }

        unsafe public void setUniformMat4Array(string name, Matrix4[] unif)
        {
            int unifLoc = uniformLocation(name);
            // See below
            fixed (float* p = &unif[0].Row0.X)
            {
                GL.UniformMatrix4(unifLoc, unif.Length, false, p);
            }
        }

        unsafe public void setUniformVec3Array(string name, Vector3[] unif)
        {
            int unifLoc = uniformLocation(name);
            // Pointer hacks necessary because for some reason OpenTK doesn't
            // let you pass a Vector3 array directly
            fixed (float* p = &unif[0].X)
            {
                GL.Uniform3(unifLoc, unif.Length, p);
            }
        }

        public void setUniformBool(string name, bool unif)
        {
            int unifLoc = uniformLocation(name);
            if (unif)
                GL.Uniform1(unifLoc, 1);
            else
                GL.Uniform1(unifLoc, 0);
        }

        /// <summary>
        /// Binds the given texture to the uniform name, using the given texture unit.
        /// </summary>
        /// <param name="name">The name of the uniform that we're binding to.</param>
        /// <param name="textureUnit">Can think of this as a unique identifier for each texture.
        /// Meaning, every texture we bind needs to have a different one.</param>
        /// <param name="tex">The texture.</param>
        public void bindTexture2D(string name, int textureUnit, Texture tex)
        {
            // Code written with the assistance of http://www.opentk.com/node/2559
            TextureUnit actualUnit = TextureUnit.Texture0 + textureUnit;
            int unifLoc = uniformLocation(name);
            int textureID = tex.getTextureID();
            GL.ActiveTexture(actualUnit);
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.Uniform1(unifLoc, textureUnit);
        }

        /// <summary>
        /// Binds the texture with the given id to the uniform name, using the given texture unit.
        /// </summary>
        /// <param name="name">The name of the uniform that we're binding to.</param>
        /// <param name="textureUnit">Can think of this as a unique identifier for each texture.
        /// Meaning, every texture we bind needs to have a different one.</param>
        /// <param name="textureID">The id of the texture texture.</param>
        public void bindTexture2D(string name, int textureUnit, uint textureID)
        {
            // Code written with the assistance of http://www.opentk.com/node/2559
            TextureUnit actualUnit = TextureUnit.Texture0 + textureUnit;
            int unifLoc = uniformLocation(name);
            GL.ActiveTexture(actualUnit);
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.Uniform1(unifLoc, textureUnit);
        }

        /// <summary>
        /// Unbinds the texture at the given texture unit.
        /// </summary>
        /// <param name="textureUnit"></param>
        public void unbindTexture2D(int textureUnit)
        {
            TextureUnit actualUnit = TextureUnit.Texture0 + textureUnit;
            GL.ActiveTexture(actualUnit);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
