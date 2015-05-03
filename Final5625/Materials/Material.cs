using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Materials
{
    /// <summary>
    /// A very general interface for materials.
    /// </summary>
    public interface Material
    {
        /// <summary>
        /// Binds all the uniforms and textures associated with this material,
        /// in preparation for shading, binding the first texture it uses to 
        /// the given texture unit. Returns the next texture unit that
        /// can be used after this.
        /// </summary>
        int useMaterialParameters(ShaderProgram program, int startTexUnit);

        /// <summary>
        /// Unbind everything that was bound in useMaterialParameters(),
        /// given the same starting texture unit as there.
        /// </summary>
        void unuseMaterialParameters(ShaderProgram program, int startTexUnit);
    }
}
