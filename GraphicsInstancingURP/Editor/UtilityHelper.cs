using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Extend.GraphicsInstancing
{
    class UtilityHelper
    {
        public static Matrix4x4[] CalculateSkinMatrix(Transform[] bonePose,
            Matrix4x4[] bindPose,
            Matrix4x4 rootMatrix1stFrame,
            bool haveRootMotion)
        {
            if (bonePose.Length == 0)
                return null;

            Transform root = bonePose[0];
            while (root.parent != null)
            {
                root = root.parent;
            }
            Matrix4x4 rootMat = root.worldToLocalMatrix;

            Matrix4x4[] matrix = new Matrix4x4[bonePose.Length];
            for (int i = 0; i != bonePose.Length; ++i)
            {
                matrix[i] = rootMat * bonePose[i].localToWorldMatrix * bindPose[i];
            }
            return matrix;
        }


        public static void CopyMatrixData(GenerateObjectInfo dst, GenerateObjectInfo src)
        {
            dst.animationTime = src.animationTime;
            dst.boneListIndex = src.boneListIndex;
            dst.frameIndex = src.frameIndex;
            dst.nameCode = src.nameCode;
            dst.stateName = src.stateName;
            dst.worldMatrix = src.worldMatrix;
            dst.boneMatrix = src.boneMatrix;
        }

        public static Color[] Convert2Color(Matrix4x4[] boneMatrix)
        {
            Color[] color = new Color[boneMatrix.Length * 4];
            int index = 0;
            foreach (var obj in boneMatrix)
            {
                color[index++] = obj.GetRow(0);
                color[index++] = obj.GetRow(1);
                color[index++] = obj.GetRow(2);
                color[index++] = obj.GetRow(3);
            }
            return color;
        }

        public static Transform[] MergeBone(SkinnedMeshRenderer[] meshRender, List<Matrix4x4> bindPose)
        {
            UnityEngine.Profiling.Profiler.BeginSample("MergeBone()");
            List<Transform> listTransform = new List<Transform>(150);
            for (int i = 0; i != meshRender.Length; ++i)
            {
                Transform[] bones = meshRender[i].bones;
                Matrix4x4[] checkBindPose = meshRender[i].sharedMesh.bindposes;
                for (int j = 0; j != bones.Length; ++j)
                {
                    Debug.Assert(checkBindPose[j].determinant != 0, "The bind pose can't be 0 matrix.");
                    int index = listTransform.FindIndex(q => q == bones[j]);
                    if (index < 0)
                    {
                        listTransform.Add(bones[j]);
                        if (bindPose != null)
                        {
                            bindPose.Add(checkBindPose[j]);
                        }
                    }
                    else
                    {
                        bindPose[index] = checkBindPose[j];
                    }
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            return listTransform.ToArray();
        }
    }
}
