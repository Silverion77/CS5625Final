using System;

using OpenTK;

namespace Chireiden.Meshes
{
    public interface MeshContainer
    {
        /// <summary>
        /// Clears the currently playing animation and resets the object to its rest pose.
        /// </summary>
        void clearAnimation();

        /// <summary>
        /// Sets the currently playing animation of this object to be the given one.
        /// </summary>
        /// <param name="anim"></param>
        void setCurrentAnimation(string anim);

        /// <summary>
        /// Advances the current position of the currently playing animation by the given delta, in seconds.
        /// </summary>
        /// <param name="delta"></param>
        void advanceAnimation(double delta);

        /// <summary>
        /// Renders the meshes in this container, at the current time of the currently set animation.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="toWorldMatrix"></param>
        void renderMeshes(Camera camera, Matrix4 toWorldMatrix);
    }
}
