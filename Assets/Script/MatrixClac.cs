using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
// using Vector2= AUCommunication.Data.Vector2;
using UnityEngine;

namespace MatrixClaculator{
    public static class MatrixHelper
    {
        // 定义矩阵相加方法
        public static Matrix4x4 Add(this Matrix4x4 a, Matrix4x4 b)
        {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result[i, j] = a[i, j] + b[i, j];
                }
            }
            return result;
        }

        // 定义矩阵相减方法
        public static Matrix4x4 Sub(this Matrix4x4 a, Matrix4x4 b)
        {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result[i, j] = a[i, j] - b[i, j];
                }
            }
            return result;
        }

        // 定义矩阵乘法方法
        public static Matrix4x4 Multiply(this Matrix4x4 a, Matrix4x4 b)
        {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result[i, j] = a[i, 0] * b[0, j] + a[i, 1] * b[1, j] + a[i, 2] * b[2, j] + a[i, 3] * b[3, j];
                }
            }
            return result;
        }

        // 定义矩阵转置方法
        public static Matrix4x4 Transpose(this Matrix4x4 matrix)
        {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result[j, i] = matrix[i, j];
                }
            }
            return result;
        }

        // 定义矩阵求逆方法
        public static Matrix4x4 Inverse(this Matrix4x4 matrix)
        {
            Matrix4x4 result = Matrix4x4.Inverse(matrix);
            if (result.determinant==0||result.IsUnityNull())
            {
                Debug.LogError("Matrix is singular and cannot be inverted.");
                return Matrix4x4.identity;
            }
            return result;
        }

        // 提取 Vector3 从 Matrix4x4
        public static Vector3 ExtractVector3(this Matrix4x4 matrix)
        {
            return new Vector3(matrix[0, 3], matrix[1, 3], matrix[2, 3]);
        }
    }

    /*状态向量 (state):
        描述系统状态的向量。
        在 Unity 中通常表示为 Vector3 或 Vector2。
        初始化时通常设为零向量 Vector3.zero 或 Vector2.zero。
    状态转移矩阵 (transitionMatrix):
        描述系统状态随时间变化的关系。
        如果状态不随时间变化，则可以初始化为单位矩阵 Matrix4x4.identity。
        可以根据具体应用调整该矩阵来反映系统的动态特性。
    观测矩阵 (observationMatrix):
        将状态向量映射到观测值的矩阵。
        根据观测模型的不同，可以初始化为不同的矩阵。
        例如，如果只观测位置而不观测速度，可以设置为 new Vector4(1, 0, 0, 0)。
    过程噪声协方差矩阵 (processNoiseCovariance):
        描述系统状态转移过程中引入的随机噪声的影响。
        可以根据系统特性和噪声特性进行调整。
        通常初始化为对角矩阵，对角线元素表示对应状态变量的噪声方差。
    观测噪声协方差矩阵 (measurementNoiseCovariance):
        描述观测值中的噪声影响。
        可以根据观测设备的精度进行调整。
        通常初始化为对角矩阵，对角线元素表示对应观测值的噪声方差。
        状态协方差矩阵 (errorCovariancePost):
        描述状态估计的不确定性。
        初始值通常设为较大的对角矩阵，表示初始估计的不确定性较高。
        随着滤波器运行，该矩阵会逐渐收敛到一个稳定的值。*/
    public class KalmanFilter
    {
        private Vector3 state; // 状态向量
        private Matrix4x4 transitionMatrix; // 状态转移矩阵
        private Matrix4x4 observationMatrix; // 观测矩阵
        private Matrix4x4 processNoiseCovariance; // 过程噪声协方差矩阵
        private Matrix4x4 measurementNoiseCovariance; // 观测噪声协方差矩阵
        private Matrix4x4 errorCovariancePost; // 状态协方差矩阵

        public KalmanFilter(int dimensionState, int dimensionMeasurement)
        {
            state = new Vector4(0, 0, 0, 0); // 初始状态向量

            // 初始化状态转移矩阵
            transitionMatrix = InitializeTransitionMatrix(dimensionState);

            // 初始化观测矩阵
            observationMatrix = InitializeObservationMatrix(dimensionState, dimensionMeasurement);

            // 初始化过程噪声协方差矩阵
            processNoiseCovariance = InitializeProcessNoiseCovariance(dimensionState);

            // 初始化观测噪声协方差矩阵
            measurementNoiseCovariance = InitializeMeasurementNoiseCovariance(dimensionMeasurement);

            // 初始化状态协方差矩阵
            errorCovariancePost = InitializeErrorCovariancePost(dimensionState);
        }

        private Matrix4x4 InitializeTransitionMatrix(int dimensionState)
        {
            Matrix4x4 matrix = new Matrix4x4();
            for (int i = 0; i < dimensionState; i++)
            {
                matrix.SetRow(i, new Vector4(0, 0, 0, 0));
            }

            // 示例：简单状态转移矩阵
            matrix.SetRow(0, new Vector4(1, 0, 1, 0)); // x = x + dt * vx
            matrix.SetRow(1, new Vector4(0, 1, 0, 1)); // y = y + dt * vy
            matrix.SetRow(2, new Vector4(0, 0, 1, 0)); // vx = vx
            matrix.SetRow(3, new Vector4(0, 0, 0, 1)); // vy = vy

            return matrix;
        }

        private Matrix4x4 InitializeObservationMatrix(int dimensionState, int dimensionMeasurement)
        {
            Matrix4x4 matrix = new Matrix4x4();
            for (int i = 0; i < dimensionMeasurement; i++)
            {
                for (int j = 0; j < dimensionState; j++)
                {
                    matrix[i, j] = (i == j) ? 1 : 0;
                }
            }

            // 示例：简单观测矩阵
            matrix.SetRow(0, new Vector4(1, 0, 0, 0)); // 观测 x
            matrix.SetRow(1, new Vector4(0, 1, 0, 0)); // 观测 y

            return matrix;
        }

        private Matrix4x4 InitializeProcessNoiseCovariance(int dimensionState)
        {
            Matrix4x4 matrix = new Matrix4x4();
            for (int i = 0; i < dimensionState; i++)
            {
                matrix.SetRow(i, new Vector4(0, 0, 0, 0));
            }

            // 示例：对角矩阵
            matrix.SetRow(0, new Vector4(0.1f, 0, 0, 0));
            matrix.SetRow(1, new Vector4(0, 0.1f, 0, 0));
            matrix.SetRow(2, new Vector4(0, 0, 0.1f, 0));
            matrix.SetRow(3, new Vector4(0, 0, 0, 0.1f));

            return matrix;
        }

        private Matrix4x4 InitializeMeasurementNoiseCovariance(int dimensionMeasurement)
        {
            Matrix4x4 matrix = new Matrix4x4();
            for (int i = 0; i < dimensionMeasurement; i++)
            {
                matrix.SetRow(i, new Vector4(0, 0, 0, 0));
            }

            // 示例：对角矩阵
            matrix.SetRow(0, new Vector4(0.2f, 0, 0, 0));
            matrix.SetRow(1, new Vector4(0, 0.2f, 0, 0));
            matrix.SetRow(2, new Vector4(0, 0, 0.2f, 0));
            matrix.SetRow(3, new Vector4(0, 0, 0, 0.1f));

            return matrix;
        }

        private Matrix4x4 InitializeErrorCovariancePost(int dimensionState)
        {
            Matrix4x4 matrix = new Matrix4x4();
            for (int i = 0; i < dimensionState; i++)
            {
                matrix.SetRow(i, new Vector4(0, 0, 0, 0));
            }

            // 示例：对角矩阵
            matrix.SetRow(0, new Vector4(1, 0, 0, 0));
            matrix.SetRow(1, new Vector4(0, 1, 0, 0));
            matrix.SetRow(2, new Vector4(0, 0, 1, 0));
            matrix.SetRow(3, new Vector4(0, 0, 0, 1));

            return matrix;
        }


        public Vector3 Predict(Vector3 measurement)
        {
            // 预测阶段
            state = transitionMatrix * state;

            // 使用 Multiply() 方法进行矩阵乘法
            Matrix4x4 temp = transitionMatrix * errorCovariancePost * transitionMatrix.transpose;
            errorCovariancePost = temp.Add(errorCovariancePost);


            // 更新阶段
            Matrix4x4 innovationCovariance = (observationMatrix * errorCovariancePost * observationMatrix.transpose).Add( measurementNoiseCovariance);
            Matrix4x4 kalmanGain = errorCovariancePost * observationMatrix.transpose * innovationCovariance.Inverse();
            var tState = observationMatrix * state;
            Vector3 innovation = measurement - (Vector3)tState; 
            var tState2 = kalmanGain * innovation;
            // // 检查创新是否有效
            // if (float.IsNaN(innovation.x) || float.IsNaN(innovation.y) || float.IsNaN(innovation.z))
            // {
            //     Debug.LogWarning("Innovation contains NaN values.");
            //     return measurement;
            // }
            state += (Vector3)tState2;

            // 检查状态是否有效
            // if (float.IsNaN(state.x) || float.IsNaN(state.y) || float.IsNaN(state.z))
            // {
            //     Debug.LogWarning("State contains NaN values.");
            //     return measurement;
            // }
            errorCovariancePost = Matrix4x4.identity.Sub( kalmanGain * observationMatrix) * errorCovariancePost;

            return state;
        }
        public Vector2 Predict( Vector2 measurement)
        {
            // 预测阶段
            state = transitionMatrix * state;

            // 使用 Multiply() 方法进行矩阵乘法
            Matrix4x4 temp = transitionMatrix * errorCovariancePost * transitionMatrix.transpose;
            errorCovariancePost = temp.Add(errorCovariancePost);


            // 更新阶段
            Matrix4x4 innovationCovariance = (observationMatrix * errorCovariancePost * observationMatrix.transpose).Add( measurementNoiseCovariance);
            Matrix4x4 kalmanGain = errorCovariancePost * observationMatrix.transpose * innovationCovariance.Inverse();
            var tState = observationMatrix * state;
            UnityEngine.Vector2 innovation = new UnityEngine.Vector2(measurement.x,measurement.y) - (UnityEngine.Vector2)tState; 
            var tState2 = kalmanGain * innovation;

            // 检查创新是否有效
            if (float.IsNaN(innovation.x) || float.IsNaN(innovation.y))
            {
                Debug.LogWarning("Innovation contains NaN values.");
                return measurement;
            }

            state += (Vector3)tState2;
            // 检查状态是否有效
            if (float.IsNaN(state.x) || float.IsNaN(state.y) || float.IsNaN(state.z))
            {
                Debug.LogWarning("State contains NaN values.");
                return measurement;
            }
            errorCovariancePost = Matrix4x4.identity.Sub( kalmanGain * observationMatrix) * errorCovariancePost;
            // 四舍五入保留四位小数精度
            var roundedX = Mathf.Round(state.x * 10000f) / 10000f;
            var roundedY = Mathf.Round(state.y * 10000f) / 10000f;
            return new Vector2(roundedX,roundedY);
        }
    }
}