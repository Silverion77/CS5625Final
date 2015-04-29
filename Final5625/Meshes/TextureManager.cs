using System;
using System.Collections.Generic;

namespace Chireiden.Meshes
{
    /// <summary>
    /// A class to handle loading all the textures from file,
    /// to avoid loading them multiple times.
    /// </summary>
    class TextureManager
    {
        static Dictionary<string, Texture> dict = new Dictionary<string, Texture>();

        public static Texture getTexture(string filename)
        {
            Texture t = null;
            if (dict.TryGetValue(filename, out t))
            {
                return t;
            }
            else
            {
                t = new Texture(filename);
                return t;
            }
        }
    }
}
