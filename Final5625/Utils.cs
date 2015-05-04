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
    }
}
