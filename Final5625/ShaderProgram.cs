﻿using System;
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
        public int programHandle { get; private set; }

        public ShaderProgram(string vertFile, string fragFile)
        {
            programHandle = createShaderFromFiles(vertFile, fragFile);
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
                Console.WriteLine("Vertex shader compilation failed: {0}", GL.GetShaderInfoLog(vertexShaderHandle));
            }
            if (fragStatus != 1)
            {
                Console.WriteLine("Fragment shader compilation failed: {0}", GL.GetShaderInfoLog(fragmentShaderHandle));
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
