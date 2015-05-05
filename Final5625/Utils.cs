using System;
using OpenTK;

namespace Chireiden
{
    public class Utils
    {
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

        public static Matrix3 normalMatrix(Matrix4 modelView)
        {
            Matrix3 normalMat = get3x3Part(modelView);
            normalMat.Invert();
            normalMat.Transpose();
            return normalMat;
        }

        public static Vector3 FORWARD = -Vector3.UnitY;
        public static Vector3 UP = Vector3.UnitZ;

        public static double xyPlaneRotation(Vector3 v1, Vector3 v2)
        {
            return Math.Atan2(v2.Y, v2.X) - Math.Atan2(v1.Y, v1.X);
        }

        public static double worldRotationOfDir(Vector3 dir)
        {
            return xyPlaneRotation(FORWARD, dir);
        }
    }
}
