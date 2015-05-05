using System;

using OpenTK;

using Chireiden.Meshes.Animations;

namespace Chireiden.Meshes
{
    public interface MeshContainer
    {
        /// <summary>
        /// Gets the named animation clip from the object's animation library.
        /// </summary>
        /// <param name="anim"></param>
        AnimationClip fetchAnimation(string anim);

        /// <summary>
        /// Returns whether or not this container has an animation with the given name.
        /// </summary>
        /// <param name="anim"></param>
        /// <returns></returns>
        bool hasAnimation(string anim);

        /// <summary>
        /// Renders the meshes in this container, with no animation.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="toWorldMatrix"></param>
        void renderMeshes(Camera camera, Matrix4 toWorldMatrix);

        /// <summary>
        /// Renders the meshes in this container, at the specified time of the given animation.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="toWorldMatrix"></param>
        void renderMeshes(Camera camera, Matrix4 toWorldMatrix, AnimationClip clip, double time);
    }
}
