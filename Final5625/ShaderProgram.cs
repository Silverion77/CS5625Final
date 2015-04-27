using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

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

            Debug.WriteLine(GL.GetShaderInfoLog(vertexShaderHandle));
            Debug.WriteLine(GL.GetShaderInfoLog(fragmentShaderHandle));

            // Create program
            int shaderProgramHandle = GL.CreateProgram();

            GL.AttachShader(shaderProgramHandle, vertexShaderHandle);
            GL.AttachShader(shaderProgramHandle, fragmentShaderHandle);

            GL.LinkProgram(shaderProgramHandle);
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
    }
}
