using System;
using Tao.OpenGl;
using Tao.Platform.Windows;

using Geometry;

namespace Component
{
    public class Camera
    {
        public Vector3d eye;
        public Vector3d target; 
        public Vector3d viewDir; // target - eye
        private double[] projection = new double[16];
        private double[] modelView = new double[16];
        private int[] viewport = new int[4];
        private double[] ballMatrix = new double[16];
        private Matrix4d objectSpaceTransform = Matrix4d.IdentityMatrix();

        public Camera() { }

        public Camera(Vector3d pos, Vector3d center)
        {
            this.eye = pos;
            this.target = center;
            this.viewDir = (center - pos).normalize();
        }

        public void SetBallMatrix(double[] ballMat)
        {
            this.ballMatrix = ballMat;
        }

        public void SetObjectSpaceTransform(Matrix4d T)
        {
            this.objectSpaceTransform = T;
        }

        public Matrix4d GetObjectSpaceTransform()
        {
            return this.objectSpaceTransform;
        }

        public Matrix4d GetBallMat()
        {
            return new Matrix4d(this.ballMatrix);
        }

        public Matrix4d GetModelviewMat()
        {
            return new Matrix4d(this.modelView);
        }

        public Matrix4d GetProjMat()
        {
            return new Matrix4d(this.projection);
        }

        public void SetModelViewMatrix(double[] modelView)
        {
            this.modelView = modelView;
        }

        public void Update()
        {
            Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelView);
            Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projection);
            Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewport);

            //this.modelView = (new Matrix4d(this.ballMatrix)).ToArray();
            this.modelView = (new Matrix4d(this.ballMatrix) * new Matrix4d(this.modelView)).ToArray();
        }

        //public Vector3d Project(double objx, double objy, double objz)
        //{
        //    double[] modelview = this.modelView;
        //    double[] projection = this.projection;
        //    double[] ballmat = this.ballMatrix;

        //    //Transformation vectors
        //    double[] tmpb = new double[4];
        //    //Arcball transform
        //    tmpb[0] = ballmat[0] * objx + ballmat[4] * objy + ballmat[8] * objz + ballmat[12];  //w is always 1
        //    tmpb[1] = ballmat[1] * objx + ballmat[5] * objy + ballmat[9] * objz + ballmat[13];
        //    tmpb[2] = ballmat[2] * objx + ballmat[6] * objy + ballmat[10] * objz + ballmat[14];
        //    tmpb[3] = ballmat[3] * objx + ballmat[7] * objy + ballmat[11] * objz + ballmat[15];

        //    double[] fTempo = new double[8];
        //    //Modelview transform
        //    fTempo[0] = modelview[0] * tmpb[0] + modelview[4] * tmpb[1] + modelview[8] * tmpb[2] + modelview[12] * tmpb[3];  //w is always 1
        //    fTempo[1] = modelview[1] * tmpb[0] + modelview[5] * tmpb[1] + modelview[9] * tmpb[2] + modelview[13] * tmpb[3];
        //    fTempo[2] = modelview[2] * tmpb[0] + modelview[6] * tmpb[1] + modelview[10] * tmpb[2] + modelview[14] * tmpb[3];
        //    fTempo[3] = modelview[3] * tmpb[0] + modelview[7] * tmpb[1] + modelview[11] * tmpb[2] + modelview[15] * tmpb[3];

        //    //Projection transform, the final row of projection matrix is always [0 0 -1 0]
        //    //so we optimize for that.
        //    fTempo[4] = projection[0] * fTempo[0] + projection[4] * fTempo[1] + projection[8] * fTempo[2] + projection[12] * fTempo[3];
        //    fTempo[5] = projection[1] * fTempo[0] + projection[5] * fTempo[1] + projection[9] * fTempo[2] + projection[13] * fTempo[3];
        //    fTempo[6] = projection[2] * fTempo[0] + projection[6] * fTempo[1] + projection[10] * fTempo[2] + projection[14] * fTempo[3];
        //    fTempo[7] = -fTempo[2];
        //    //The result normalizes between -1 and 1
        //    if (fTempo[7] == 0.0)        //The w value
        //        return new Vector3d();
        //    fTempo[7] = 1.0 / fTempo[7];
        //    //Perspective division
        //    fTempo[4] *= fTempo[7]; 
        //    fTempo[5] *= fTempo[7];
        //    fTempo[6] *= fTempo[7];
        //    //Window coordinates
        //    //Map x, y to range 0-1
        //    Vector3d windowCoordinate = new Vector3d();
        //    windowCoordinate[0] = (fTempo[4] * 0.5 + 0.5) * viewport[2] + viewport[0];
        //    windowCoordinate[1] = (fTempo[5] * 0.5 + 0.5) * viewport[3] + viewport[1];
        //    //This is only correct when glDepthRange(0.0, 1.0)
        //    windowCoordinate[2] = (1.0 + fTempo[6]) * 0.5;  //Between 0 and 1

        //    // convert from gl 2d coords to windows coordinates
        //    //windowCoordinate.y = this.wndViewHeight - windowCoordinate.y;

        //    return windowCoordinate;
        //}

        private Vector3d ProjectPointToPlaneIdentity(Vector2d screenpt, Vector3d c, Vector3d nor)
        {
            screenpt.y = this.viewport[3] - screenpt.y;
            // the out parameter r measures how close the intersecting point is to the near plane'
            // 1. get transformed plane normal and center (due to view point change)
            Vector3d s = this.UnProject(screenpt.x, screenpt.y, -1);
            Vector3d t = this.UnProject(screenpt.x, screenpt.y, 1);
            double r = (c - t).Dot(nor) / ((s - t).Dot(nor));
            return r * s + (1 - r) * t;
        }

        private void ProjectPointToPlaneIdentity(Vector2d screenpt, Vector3d c, Vector3d nor, out Vector3d v, out double r)
        {
            screenpt.y = this.viewport[3] - screenpt.y;
            // the out parameter r measures how close the intersecting point is to the near plane'
            // 1. get transformed plane normal and center (due to view point change)
            Vector3d s = this.UnProject(screenpt.x, screenpt.y, -1);
            Vector3d t = this.UnProject(screenpt.x, screenpt.y, 1);
            r = (c - t).Dot(nor) / ((s - t).Dot(nor));
            v = r * s + (1 - r) * t;
        }

        public void ProjectPointToPlane(Vector2d screenpt, Vector3d planecenter, Vector3d planenormal,
            out Vector3d p3, out double r)
        {
            // the out parameter r measures how close the intersecting point is to the near plane'
            // 1. get transformed plane normal and center (due to view point change)
            Vector3d c = this.GetObjSpaceTransformedPoint(planecenter);
            Vector3d nor = this.GetObjSpaceTransformedVector(planenormal);
            double[] ss = new double[3];
            double[] tt = new double[3];
            if (this.UnProject(screenpt.x, screenpt.y, -1, ss) == 0 ||
                this.UnProject(screenpt.x, screenpt.y, 1, tt) == 0)
                p3 = new Vector3d(0, 0, 0);
            Vector3d s = new Vector3d(ss);
            Vector3d t = new Vector3d(tt);
            r = (c - t).Dot(nor) / ((s - t).Dot(nor));
            p3 = r * s + (1 - r) * t;
        }
        public Vector3d ProjectPointToPlane(Vector2d screenpt, Vector3d planecenter, Vector3d planenormal)
        {
            // the out parameter r measures how close the intersecting point is to the near plane'
            // 1. get transformed plane normal and center (due to view point change)
            Vector3d c = this.GetObjSpaceTransformedPoint(planecenter);
            Vector3d nor = this.GetObjSpaceTransformedVector(planenormal);
            double[] ss = new double[3];
            double[] tt = new double[3];
            Vector3d p3;
            if (this.UnProject(screenpt.x, screenpt.y, -1, ss) == 0 ||
                this.UnProject(screenpt.x, screenpt.y, 1, tt) == 0)
                p3 = new Vector3d(0, 0, 0);
            Vector3d s = new Vector3d(ss);
            Vector3d t = new Vector3d(tt);
            double r = (c - t).Dot(nor) / ((s - t).Dot(nor));
            p3 = r * s + (1 - r) * t;
            return p3;
        }

        public int UnProject(double winx, double winy, double winz, double[] objectCoordinate)
        {
            // convert from windows coordinate to opengl coordinate
            //winy = viewport[3] - winy;
            //Transformation matrices
            double[] m = new double[16];
            double[] A = new double[16];
            double[] tmpA = new double[16];
            double[] data_in = new double[4];
            double[] data_out = new double[4];
            //Calculation for inverting a matrix, compute projection x modelview
            //and store in A[16]
            MultiplyMatrices4by4OpenGL_FLOAT(tmpA, this.modelView, this.ballMatrix);
            MultiplyMatrices4by4OpenGL_FLOAT(A, this.projection, tmpA);
            //Now compute the inverse of matrix A
            if (glhInvertMatrixf2(A, m) == 0)
                return 0;
            //Transformation of normalized coordinates between -1 and 1
            data_in[0] = (winx - (double)this.viewport[0]) / (double)this.viewport[2] * 2.0 - 1.0;
            data_in[1] = (winy - (double)this.viewport[1]) / (double)this.viewport[3] * 2.0 - 1.0;
            data_in[2] = 2.0 * winz - 1.0;
            data_in[3] = 1.0;
            //Objects coordinates
            MultiplyMatrixByVector4by4OpenGL_FLOAT(data_out, m, data_in);
            if (data_out[3] == 0.0)
                return 0;
            data_out[3] = 1.0 / data_out[3];
            objectCoordinate[0] = data_out[0] * data_out[3];
            objectCoordinate[1] = data_out[1] * data_out[3];
            objectCoordinate[2] = data_out[2] * data_out[3];
            return 1;
            ////Transformation matrices
            //double[] m = new double[16];
            //double[] A = new double[16];
            //double[] tmpA = new double[16];
            //double[] data_in = new double[4];
            //double[] data_out = new double[4];
            ////Calculation for inverting a matrix, compute projection x modelview
            ////and store in A[16]
            //MultiplyMatrices4by4OpenGL_FLOAT(tmpA, this.modelView, this.ballMatrix);
            ////tmpA = this.modelView;
            //MultiplyMatrices4by4OpenGL_FLOAT(A, this.projection, tmpA);
            ////Now compute the inverse of matrix A
            //if (glhInvertMatrixf2(A, m) == 0)
            //    return 0;
            ////Transformation of normalized coordinates between -1 and 1
            //data_in[0] = (winx - (double)this.viewport[0]) / (double)this.viewport[2] * 2.0 - 1.0;
            //data_in[1] = (winy - (double)this.viewport[1]) / (double)this.viewport[3] * 2.0 - 1.0;
            //data_in[2] = 2.0 * winz - 1.0;
            //data_in[3] = 1.0;
            ////data_in[0] = (winx - (double)this.viewport[0]) / (double)this.viewport[2];
            ////data_in[1] = (winy - (double)this.viewport[1]) / (double)this.viewport[3];
            ////data_in[2] = winz;
            ////data_in[3] = 1.0;
            ////Objects coordinates
            //MultiplyMatrixByVector4by4OpenGL_FLOAT(data_out, m, data_in);
            //if (data_out[3] == 0.0)
            //    return 0;
            //data_out[3] = 1.0 / data_out[3];
            //objectCoordinate[0] = data_out[0] * data_out[3];
            //objectCoordinate[1] = data_out[1] * data_out[3];
            //objectCoordinate[2] = data_out[2] * data_out[3];
            //return 1;
        }

        private static double MAT(double[] m, int r, int c)
        {
            return m[(c) * 4 + (r)];
        }

        private static int glhInvertMatrixf2(double[] m, double[] out_mat)
        {
            double[][] wtmp = new double[4][];
            for (int i = 0; i < 4; ++i) wtmp[i] = new double[8];

            double m0, m1, m2, m3, s;
            double[] r0 = wtmp[0];
            double[] r1 = wtmp[1];
            double[] r2 = wtmp[2];
            double[] r3 = wtmp[3];
            r0[0] = MAT(m, 0, 0); r0[1] = MAT(m, 0, 1);
            r0[2] = MAT(m, 0, 2); r0[3] = MAT(m, 0, 3);
            r0[4] = 1.0; r0[5] = r0[6] = r0[7] = 0.0;
            r1[0] = MAT(m, 1, 0); r1[1] = MAT(m, 1, 1);
            r1[2] = MAT(m, 1, 2); r1[3] = MAT(m, 1, 3);
            r1[5] = 1.0; r1[4] = r1[6] = r1[7] = 0.0;
            r2[0] = MAT(m, 2, 0); r2[1] = MAT(m, 2, 1);
            r2[2] = MAT(m, 2, 2); r2[3] = MAT(m, 2, 3);
            r2[6] = 1.0; r2[4] = r2[5] = r2[7] = 0.0;
            r3[0] = MAT(m, 3, 0); r3[1] = MAT(m, 3, 1);
            r3[2] = MAT(m, 3, 2); r3[3] = MAT(m, 3, 3);
            r3[7] = 1.0; r3[4] = r3[5] = r3[6] = 0.0;
            /* choose pivot - or die */
            if (Math.Abs(r3[0]) > Math.Abs(r2[0]))
                SWAP_ROWS_FLOAT(r3, r2);
            if (Math.Abs(r2[0]) > Math.Abs(r1[0]))
                SWAP_ROWS_FLOAT(r2, r1);
            if (Math.Abs(r1[0]) > Math.Abs(r0[0]))
                SWAP_ROWS_FLOAT(r1, r0);
            if (0.0 == r0[0])
                return 0;
            /* eliminate first variable     */
            m1 = r1[0] / r0[0];
            m2 = r2[0] / r0[0];
            m3 = r3[0] / r0[0];
            s = r0[1];
            r1[1] -= m1 * s;
            r2[1] -= m2 * s;
            r3[1] -= m3 * s;
            s = r0[2];
            r1[2] -= m1 * s;
            r2[2] -= m2 * s;
            r3[2] -= m3 * s;
            s = r0[3];
            r1[3] -= m1 * s;
            r2[3] -= m2 * s;
            r3[3] -= m3 * s;
            s = r0[4];
            if (s != 0.0)
            {
                r1[4] -= m1 * s;
                r2[4] -= m2 * s;
                r3[4] -= m3 * s;
            }
            s = r0[5];
            if (s != 0.0)
            {
                r1[5] -= m1 * s;
                r2[5] -= m2 * s;
                r3[5] -= m3 * s;
            }
            s = r0[6];
            if (s != 0.0)
            {
                r1[6] -= m1 * s;
                r2[6] -= m2 * s;
                r3[6] -= m3 * s;
            }
            s = r0[7];
            if (s != 0.0)
            {
                r1[7] -= m1 * s;
                r2[7] -= m2 * s;
                r3[7] -= m3 * s;
            }
            /* choose pivot - or die */
            if (Math.Abs(r3[1]) > Math.Abs(r2[1]))
                SWAP_ROWS_FLOAT(r3, r2);
            if (Math.Abs(r2[1]) > Math.Abs(r1[1]))
                SWAP_ROWS_FLOAT(r2, r1);
            if (0.0 == r1[1])
                return 0;
            /* eliminate second variable */
            m2 = r2[1] / r1[1];
            m3 = r3[1] / r1[1];
            r2[2] -= m2 * r1[2];
            r3[2] -= m3 * r1[2];
            r2[3] -= m2 * r1[3];
            r3[3] -= m3 * r1[3];
            s = r1[4];
            if (0.0 != s)
            {
                r2[4] -= m2 * s;
                r3[4] -= m3 * s;
            }
            s = r1[5];
            if (0.0 != s)
            {
                r2[5] -= m2 * s;
                r3[5] -= m3 * s;
            }
            s = r1[6];
            if (0.0 != s)
            {
                r2[6] -= m2 * s;
                r3[6] -= m3 * s;
            }
            s = r1[7];
            if (0.0 != s)
            {
                r2[7] -= m2 * s;
                r3[7] -= m3 * s;
            }
            /* choose pivot - or die */
            if (Math.Abs(r3[2]) > Math.Abs(r2[2]))
                SWAP_ROWS_FLOAT(r3, r2);
            if (0.0 == r2[2])
                return 0;
            /* eliminate third variable */
            m3 = r3[2] / r2[2];
            r3[3] -= m3 * r2[3]; r3[4] -= m3 * r2[4];
            r3[5] -= m3 * r2[5]; r3[6] -= m3 * r2[6]; r3[7] -= m3 * r2[7];
            /* last check */
            if (0.0 == r3[3])
                return 0;
            s = 1.0 / r3[3];             /* now back substitute row 3 */
            r3[4] *= s;
            r3[5] *= s;
            r3[6] *= s;
            r3[7] *= s;
            m2 = r2[3];                  /* now back substitute row 2 */
            s = 1.0 / r2[2];
            r2[4] = s * (r2[4] - r3[4] * m2); r2[5] = s * (r2[5] - r3[5] * m2);
            r2[6] = s * (r2[6] - r3[6] * m2); r2[7] = s * (r2[7] - r3[7] * m2);
            m1 = r1[3];
            r1[4] -= r3[4] * m1; r1[5] -= r3[5] * m1;
            r1[6] -= r3[6] * m1; r1[7] -= r3[7] * m1;
            m0 = r0[3];
            r0[4] -= r3[4] * m0; r0[5] -= r3[5] * m0;
            r0[6] -= r3[6] * m0; r0[7] -= r3[7] * m0;
            m1 = r1[2];                  /* now back substitute row 1 */
            s = 1.0 / r1[1];
            r1[4] = s * (r1[4] - r2[4] * m1); r1[5] = s * (r1[5] - r2[5] * m1);
            r1[6] = s * (r1[6] - r2[6] * m1); r1[7] = s * (r1[7] - r2[7] * m1);
            m0 = r0[2];
            r0[4] -= r2[4] * m0; r0[5] -= r2[5] * m0;
            r0[6] -= r2[6] * m0; r0[7] -= r2[7] * m0;
            m0 = r0[1];                  /* now back substitute row 0 */
            s = 1.0 / r0[0];
            r0[4] = s * (r0[4] - r1[4] * m0); r0[5] = s * (r0[5] - r1[5] * m0);
            r0[6] = s * (r0[6] - r1[6] * m0); r0[7] = s * (r0[7] - r1[7] * m0);

            out_mat[0] = r0[4]; out_mat[4] = r0[5]; out_mat[8] = r0[6]; out_mat[12] = r0[7];
            out_mat[1] = r1[4]; out_mat[5] = r1[5]; out_mat[9] = r1[6]; out_mat[13] = r1[7];
            out_mat[2] = r2[4]; out_mat[6] = r2[5]; out_mat[10] = r2[6]; out_mat[14] = r2[7];
            out_mat[3] = r3[4]; out_mat[7] = r3[5]; out_mat[11] = r3[6]; out_mat[15] = r3[7];

            return 1;
        }

        private static void SWAP_ROWS_FLOAT(double[] a, double[] b)
        {
            double[] _tmp = a; (a) = (b); (b) = _tmp;
        }

        private static void MultiplyMatrices4by4OpenGL_FLOAT(double[] result, double[] matrix1, double[] matrix2)
        {
            result[0] = matrix1[0] * matrix2[0] +
                matrix1[4] * matrix2[1] +
                matrix1[8] * matrix2[2] +
                matrix1[12] * matrix2[3];
            result[4] = matrix1[0] * matrix2[4] +
                matrix1[4] * matrix2[5] +
                matrix1[8] * matrix2[6] +
                matrix1[12] * matrix2[7];
            result[8] = matrix1[0] * matrix2[8] +
                matrix1[4] * matrix2[9] +
                matrix1[8] * matrix2[10] +
                matrix1[12] * matrix2[11];
            result[12] = matrix1[0] * matrix2[12] +
                matrix1[4] * matrix2[13] +
                matrix1[8] * matrix2[14] +
                matrix1[12] * matrix2[15];
            result[1] = matrix1[1] * matrix2[0] +
                matrix1[5] * matrix2[1] +
                matrix1[9] * matrix2[2] +
                matrix1[13] * matrix2[3];
            result[5] = matrix1[1] * matrix2[4] +
                matrix1[5] * matrix2[5] +
                matrix1[9] * matrix2[6] +
                matrix1[13] * matrix2[7];
            result[9] = matrix1[1] * matrix2[8] +
                matrix1[5] * matrix2[9] +
                matrix1[9] * matrix2[10] +
                matrix1[13] * matrix2[11];
            result[13] = matrix1[1] * matrix2[12] +
                matrix1[5] * matrix2[13] +
                matrix1[9] * matrix2[14] +
                matrix1[13] * matrix2[15];
            result[2] = matrix1[2] * matrix2[0] +
                matrix1[6] * matrix2[1] +
                matrix1[10] * matrix2[2] +
                matrix1[14] * matrix2[3];
            result[6] = matrix1[2] * matrix2[4] +
                matrix1[6] * matrix2[5] +
                matrix1[10] * matrix2[6] +
                matrix1[14] * matrix2[7];
            result[10] = matrix1[2] * matrix2[8] +
                matrix1[6] * matrix2[9] +
                matrix1[10] * matrix2[10] +
                matrix1[14] * matrix2[11];
            result[14] = matrix1[2] * matrix2[12] +
                matrix1[6] * matrix2[13] +
                matrix1[10] * matrix2[14] +
                matrix1[14] * matrix2[15];
            result[3] = matrix1[3] * matrix2[0] +
                matrix1[7] * matrix2[1] +
                matrix1[11] * matrix2[2] +
                matrix1[15] * matrix2[3];
            result[7] = matrix1[3] * matrix2[4] +
                matrix1[7] * matrix2[5] +
                matrix1[11] * matrix2[6] +
                matrix1[15] * matrix2[7];
            result[11] = matrix1[3] * matrix2[8] +
                matrix1[7] * matrix2[9] +
                matrix1[11] * matrix2[10] +
                matrix1[15] * matrix2[11];
            result[15] = matrix1[3] * matrix2[12] +
                matrix1[7] * matrix2[13] +
                matrix1[11] * matrix2[14] +
                matrix1[15] * matrix2[15];
        }
        private static void MultiplyMatrixByVector4by4OpenGL_FLOAT(double[] resultvector, double[] matrix, double[] pvector)
        {
            resultvector[0] = matrix[0] * pvector[0] + matrix[4] * pvector[1] + matrix[8] * pvector[2] + matrix[12] * pvector[3];
            resultvector[1] = matrix[1] * pvector[0] + matrix[5] * pvector[1] + matrix[9] * pvector[2] + matrix[13] * pvector[3];
            resultvector[2] = matrix[2] * pvector[0] + matrix[6] * pvector[1] + matrix[10] * pvector[2] + matrix[14] * pvector[3];
            resultvector[3] = matrix[3] * pvector[0] + matrix[7] * pvector[1] + matrix[11] * pvector[2] + matrix[15] * pvector[3];
        }

        private Vector3d GetObjSpaceTransformedPoint(Vector3d point)
        {
            return (this.objectSpaceTransform * new Vector4d(point, 1)).ToVector3D();
        }
        private Vector3d GetObjSpaceTransformedVector(Vector3d vector)
        {
            return (this.objectSpaceTransform * new Vector4d(vector, 1)).ToVector3D();
        }

        public Vector3d UnProject(double inX, double inY, double inZ)
        {
            double x, y, z;
            Glu.gluUnProject(inX, inY, inZ, modelView, projection, viewport, out x, out y, out z);
            return new Vector3d(x, y, z);
        }
        public Vector3d UnProject(Vector3d p)
        {
            double x, y, z;
            Glu.gluUnProject(p.x, p.y, p.z, modelView, projection, viewport, out x, out y, out z);
            return new Vector3d(x, y, z);
        }
        public Vector3d UnProject(double[] arr, int index)
        {
            double x, y, z;
            Glu.gluUnProject(arr[index], arr[index + 1], arr[index + 2], modelView, projection, viewport, out x, out y, out z);
            return new Vector3d(x, y, z);
        }
        public Vector3d Project(double inX, double inY, double inZ)
        {
            double x, y, z;
            Glu.gluProject(inX, inY, inZ, modelView, projection, viewport, out x, out y, out z);
            return new Vector3d(x, y, z);
        }
        public Vector3d Project(Vector3d p)
        {
            double x, y, z;
            Glu.gluProject(p.x, p.y, p.z, modelView, projection, viewport, out x, out y, out z);
            return new Vector3d(x, y, z);
        }
        public Vector3d Project(double[] arr, int index)
        {
            double x, y, z;
            Glu.gluProject(arr[index], arr[index + 1], arr[index + 2], modelView, projection, viewport, out x, out y, out z);
            return new Vector3d(x, y, z);
        }
    }
}
