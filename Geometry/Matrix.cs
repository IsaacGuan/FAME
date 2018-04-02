using System;

namespace Geometry
{
    public class Matrix2d
    {
        private const int M = 2, N = 2, length = 4;

        private double[] arr = new double[length];

        public Matrix2d()
        {
            for (int i = 0; i < length; ++i)
            {
                arr[i] = 0;
            }
        }

        public Matrix2d(double[] array, bool rowWise = true)
        {
            if (rowWise)
            {
                for (int i = 0; i < length; ++i)
                {
                    arr[i] = array[i];
                }
            }
            else //col-wise
            {
                for (int i = 0; i < M; ++i)
                {
                    for (int j = 0; j < N; ++j)
                    {
                        arr[i * N + j] = array[j * M + i];
                    }
                }
            }
        }

        public Matrix2d(double[,] array)
        {
            for (int i = 0; i < M; ++i)
            {
                for (int j = 0; j < N; ++j)
                {
                    arr[i * N + j] = array[i, j];
                }
            }
        }

        public Matrix2d(Vector2d v1, Vector2d v2)
        {
            for (int i = 0; i < N; i++)
            {
                this[i, 0] = v1[i];
                this[i, 1] = v2[i];
            }
        }

        public double this[int row, int col]
        {
            get
            {
                if (row >= 0 && row < M && col >= 0 && col < N)
                {
                    return arr[row * N + col];
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                if (row >= 0 && row < M && col >= 0 && col < N)
                {
                    arr[row * N + col] = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        public double this[int index]
        {
            get
            {
                if (index >= 0 && index < length)
                {
                    return arr[index];
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                if (index >= 0 && index < length)
                {
                    arr[index] = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        public Matrix2d(Matrix2d mat)
        {
            for (int i = 0; i < length; ++i)
            {
                arr[i] = mat[i];
            }
        }

        public double[] ToArray()
        {
            return arr;
        }

        // operators
        static public Matrix2d operator +(Matrix2d m1, Matrix2d m2)
        {
            Matrix2d m = new Matrix2d(m1);
            for (int i = 0; i < length; ++i)
            {
                m[i] += m2[i];
            }
            return m;
        }

        static public Matrix2d operator -(Matrix2d m1, Matrix2d m2)
        {
            Matrix2d m = new Matrix2d(m1);
            for (int i = 0; i < length; ++i)
            {
                m[i] -= m2[i];
            }
            return m;
        }

        static public Matrix2d operator *(double factor, Matrix2d m)
        {
            Matrix2d mat = new Matrix2d(m);
            for (int i = 0; i < length; ++i)
            {
                mat[i] *= factor;
            }
            return mat;
        }

        static public Matrix2d operator *(Matrix2d m, double factor)
        {
            Matrix2d mat = new Matrix2d(m);
            for (int i = 0; i < length; ++i)
            {
                mat[i] *= factor;
            }
            return mat;
        }

        public static Vector2d operator *(Matrix2d m, Vector2d v)
        {
            Vector2d vec = new Vector2d();
            vec.x = m[0] * v.x + m[1] * v.y;
            vec.y = m[2] * v.x + m[3] * v.y;
            return vec;
        }

        static public Matrix2d operator *(Matrix2d m1, Matrix2d m2)
        {
            Matrix2d mat = new Matrix2d();
            for (int i = 0; i < M; ++i)
            {
                for (int j = 0; j < N; ++j)
                {
                    for (int k = 0; k < N; ++k)
                    {
                        mat[i, j] += m1[i, k] * m2[k, j];
                    }
                }
            }
            return mat;
        }

        static public Matrix2d operator /(Matrix2d m, double factor)
        {
            Matrix2d mat = new Matrix2d(m);
            if (Math.Abs(factor) < 1e-6)
            {
                throw new DivideByZeroException();
            }
            for (int i = 0; i < length; ++i)
            {
                mat[i] /= factor;
            }
            return mat;
        }

        // numerics
        public static Matrix2d IdentityMatrix()
        {
            Matrix2d mat = new Matrix2d();
            mat[0, 0] = mat[1, 1] = 1.0;
            return mat;
        }

        public Matrix2d Transpose()
        {
            Matrix2d mat = new Matrix2d();
            for (int i = 0; i < M; ++i)
            {
                for (int j = 0; j < N; ++j)
                {
                    mat[j, i] = this[i, j];
                }
            }
            return mat;
        }

        public double Trace()
        {
            return this[0, 0] + this[1, 1];
        }

        public double Determinant()
        {
            return this[0, 0] * this[1, 1] - this[0, 1] * this[1, 0];
        }

        public Matrix2d Inverse()
        {
            Matrix2d mat = new Matrix2d();
            double det = this.Determinant();
            if (det == 0) throw new DivideByZeroException("Determinant equals to 0!");
            mat[0, 0] = this[1, 1];
            mat[0, 1] = -this[0, 1];
            mat[1, 0] = -this[1, 0];
            mat[1, 1] = this[1, 1];
            mat = mat / det;
            return mat;
        }

    }//class-Matrix2d

	public class Matrix3d
	{
		private const int M = 3, N = 3, length = 9;

		private double[] arr = new double[length];

		public Matrix3d()
		{
			for (int i = 0; i < length; ++i)
			{
				arr[i] = 0;
			}
		}

		public Matrix3d(double[] array, bool rowWise = true)
		{
			if (rowWise)
			{
				for (int i = 0; i < length; ++i)
				{
					arr[i] = array[i];
				}
			}
			else //col-wise
			{
				for (int i = 0; i < M; ++i)
				{
					for (int j = 0; j < N; ++j)
					{
						arr[i * N + j] = array[j * M + i];
					}
				}
			}
		}

		public Matrix3d(double[,] array)
		{
			for (int i = 0; i < M; ++i)
			{
				for (int j = 0; j < N; ++j)
				{
					arr[i * N + j] = array[i, j];
				}
			}
		}

        public Matrix3d(Vector3d v1, Vector3d v2, Vector3d v3)
        {
            for (int i = 0; i < N; i++)
            {
                this[i, 0] = v1[i];
                this[i, 1] = v2[i];
                this[i, 2] = v3[i];
            }
        }

		public double this[int row, int col]
		{
			get
			{
				if (row >= 0 && row < M && col >= 0 && col < N)
				{
					return arr[row * N + col];
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}
			}
			set
			{
				if (row >= 0 && row < M && col >= 0 && col < N)
				{
					arr[row * N + col] = value;
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		public double this[int index]
		{
			get
			{
				if (index >= 0 && index < length)
				{
					return arr[index];
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}
			}
			set 
			{
				if (index >= 0 && index < length)
				{
					arr[index] = value;
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		public Matrix3d(Matrix3d mat)
		{
			for (int i = 0; i < length; ++i)
			{
				arr[i] = mat[i];
			}
		}

        public double[] ToArray()
        {
            return arr;
        }

		// operators
		static public Matrix3d operator +(Matrix3d m1, Matrix3d m2)
		{
			Matrix3d m = new Matrix3d(m1);
			for (int i = 0; i < length; ++i)
			{
				m[i] += m2[i];
			}
			return m;
		}

		static public Matrix3d operator -(Matrix3d m1, Matrix3d m2)
		{
			Matrix3d m = new Matrix3d(m1);
			for (int i = 0; i < length; ++i)
			{
				m[i] -= m2[i];
			}
			return m;
		}

		static public Matrix3d operator *(double factor, Matrix3d m)
		{
			Matrix3d mat = new Matrix3d(m);
			for (int i = 0; i < length; ++i)
			{
				mat[i] *= factor;
			}
			return mat;
		}

        static public Matrix3d operator *(Matrix3d m, double factor)
        {
            Matrix3d mat = new Matrix3d(m);
            for (int i = 0; i < length; ++i)
            {
                mat[i] *= factor;
            }
            return mat;
        }

        public static Vector3d operator *(Matrix3d m, Vector3d v)
        {
            Vector3d vec = new Vector3d();
            vec.x = m[0] * v.x + m[1] * v.y + m[2] * v.z;
            vec.y = m[3] * v.x + m[4] * v.y + m[5] * v.z;
            vec.z = m[6] * v.x + m[7] * v.y + m[8] * v.z;
            return vec;
        }

        static public Matrix3d operator *(Matrix3d m1, Matrix3d m2)
        {
            Matrix3d mat = new Matrix3d();
            for (int i = 0; i < M; ++i)
            {
                for (int j = 0; j < N; ++j)
                {
                    for (int k = 0; k < N; ++k)
                    {
                        mat[i, j] += m1[i, k] * m2[k, j];
                    }
                }
            }
            return mat;
        }

		static public Matrix3d operator /(Matrix3d m, double factor)
		{
			Matrix3d mat = new Matrix3d(m);
			if (Math.Abs(factor) < 1e-6) 
			{
				throw new DivideByZeroException();
			}
			for (int i = 0; i < length; ++i)
			{
				mat[i] /= factor;
			}
			return mat;
		}

		// numerics
		public static Matrix3d IdentityMatrix()
		{
			Matrix3d mat = new Matrix3d();
			mat[0, 0] = mat[1, 1] = mat[2, 2] = 1.0;
			return mat;
		}

		public Matrix3d Transpose()
		{
			Matrix3d mat = new Matrix3d();
			for (int i = 0; i < M; ++i)
			{
				for (int j = 0; j < N; ++j)
				{
					mat[j, i] = this[i, j];
				}
			}
			return mat;
		}

		public double Trace()
		{
			return this[0, 0] + this[1, 1] + this[2, 2];
		}

		public double Determinant()
		{
			return this[0, 0] * (this[1, 1] * this[2, 2] - this[1, 2] * this[2, 1])
				- this[0, 1] * (this[1, 0] * this[2, 2] - this[1, 2] * this[2, 0])
				+ this[0, 2] * (this[1, 0] * this[2, 1] - this[1, 1] * this[2, 0]);
		}

		public Matrix3d Inverse()
		{
			Matrix3d mat = new Matrix3d();
			double det = this.Determinant();
			if (det == 0) throw new DivideByZeroException("Determinant equals to 0!");
			mat[0, 0] = this[1, 1] * this[2, 2] - this[1, 2] * this[2, 1];
			mat[0, 1] = this[0, 2] * this[2, 1] - this[0, 1] * this[2, 2];
			mat[0, 2] = this[0, 1] * this[1, 2] - this[0, 2] * this[1, 1];
			mat[1, 0] = this[1, 2] * this[2, 0] - this[1, 0] * this[2, 2];
			mat[1, 1] = this[0, 0] * this[2, 2] - this[0, 2] * this[2, 0];
			mat[1, 2] = this[0, 2] * this[1, 0] - this[0, 0] * this[1, 2];
			mat[2, 0] = this[1, 0] * this[2, 1] - this[1, 1] * this[2, 0];
			mat[2, 1] = this[0, 1] * this[2, 0] - this[0, 0] * this[2, 1];
			mat[2, 2] = this[0, 0] * this[1, 1] - this[0, 1] * this[1, 0];
			mat = mat / det;
			return mat;
		}

	}//class-Matrix3d

	public class Matrix4d
	{
		private const int M = 4, N = 4, length = 16;

		private double[] arr = new double[length];

		public Matrix4d()
		{
			for (int i = 0; i < length; ++i)
			{
				arr[i] = 0;
			}
		}

		public Matrix4d(double[] array, bool rowWise = true)
		{
			if (rowWise)
			{
				for (int i = 0; i < length; ++i)
				{
					arr[i] = array[i];
				}
			}
			else //col-wise
			{
				for (int i = 0; i < M; ++i)
				{
					for (int j = 0; j < N; ++j)
					{
						arr[i * N + j] = array[j * M + i];
					}
				}
			}
		}

		public Matrix4d(double[,] array)
		{
			for (int i = 0; i < M; ++i)
			{
				for (int j = 0; j < N; ++j)
				{
					arr[i * N + j] = array[i, j];
				}
			}
		}


        public Matrix4d(Matrix3d m)
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    this[i, j] = m[i, j];
            this[3, 3] = 1.0;
        }

		public double this[int row, int col]
		{
			get
			{
				if (row >= 0 && row < M && col >= 0 && col < N)
				{
					return arr[row * N + col];
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}
			}
			set
			{
				if (row >= 0 && row < M && col >= 0 && col < N)
				{
					arr[row * N + col] = value;
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		public double this[int index]
		{
			get
			{
				if (index >= 0 && index < length)
				{
					return arr[index];
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}
			}
			set
			{
				if (index >= 0 && index < length)
				{
					arr[index] = value;
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}
			}
		}

		public Matrix4d(Matrix4d mat)
		{
			for (int i = 0; i < length; ++i)
			{
				arr[i] = mat[i];
			}
		}

        public double[] ToArray()
        {
            return arr;
        }

		// operators
		static public Matrix4d operator +(Matrix4d m1, Matrix4d m2)
		{
			Matrix4d m = new Matrix4d(m1);
			for (int i = 0; i < length; ++i)
			{
				m[i] += m2[i];
			}
			return m;
		}

		static public Matrix4d operator -(Matrix4d m1, Matrix4d m2)
		{
			Matrix4d m = new Matrix4d(m1);
			for (int i = 0; i < length; ++i)
			{
				m[i] -= m2[i];
			}
			return m;
		}

		static public Matrix4d operator *(double factor, Matrix4d m)
		{
			Matrix4d mat = new Matrix4d(m);
			for (int i = 0; i < length; ++i)
			{
				mat[i] *= factor;
			}
			return mat;
		}

        static public Matrix4d operator *(Matrix4d m1, Matrix4d m2)
        {
            Matrix4d mat = new Matrix4d();
            for (int i = 0; i < M; ++i)
            {
                for (int j = 0; j < N; ++j)
                {
                    for (int k = 0; k < N; ++k)
                    {
                        mat[i, j] += m1[i, k] * m2[k, j];
                    }
                }
            }
            return mat;
        }

        static public Vector4d operator *(Matrix4d m, Vector4d v)
        {
            Vector4d vec = new Vector4d();
            for (int i = 0; i < M; ++i)
            {
                for (int j = 0; j < N; ++j)
                {
                    vec[i] += m[i, j] * v[j];
                }
            }
            return vec;
        }

		static public Matrix4d operator /(double factor, Matrix4d m)
		{
			Matrix4d mat = new Matrix4d(m);
			if (factor == 0) throw new DivideByZeroException();
			for (int i = 0; i < length; ++i)
			{
				mat[i] /= factor;
			}
			return mat;
		}


		// numerics
		public static Matrix4d IdentityMatrix()
		{
			Matrix4d mat = new Matrix4d();
			mat[0, 0] = mat[1, 1] = mat[2, 2] = mat[3, 3] = 1.0;
			return mat;
		}

		public Matrix4d Transpose()
		{
			Matrix4d mat = new Matrix4d();
			for (int i = 0; i < M; ++i)
			{
				for (int j = 0; j < N; ++j)
				{
					mat[j, i] = this[i, j];
				}
			}
			return mat;
		}

        public Matrix4d Inverse()
        {
            SVD svd = new SVD(this.arr, M, N);
            if (svd.State == false) throw new ArithmeticException();
            return new Matrix4d(svd.Inverse);
        }

		public double Trace()
		{
			return this[0, 0] + this[1, 1] + this[2, 2] + this[3, 3];
		}

		public double Determinant()
		{
			return this[0, 0] * FormMatrix3D(0, 0).Determinant()
				- this[0, 1] * FormMatrix3D(0, 1).Determinant()
				+ this[0, 2] * FormMatrix3D(0, 2).Determinant()
				- this[0, 3] * FormMatrix3D(0, 3).Determinant();
		}

		public Matrix3d FormMatrix3D(int row, int col)
		{
			// remove the element at [row, col]
			// for calculating the determinant
			Matrix3d mat = new Matrix3d();
			int r = 0;
			for (int i = 0; i < M; ++i)
			{
				if(i == row) continue;
				int c = 0;
				for (int j = 0; j < N; ++j)
				{
					if (j == col) continue;
					mat[r, c++] = this[i, j];
				}
				++r;
			}
			return mat;
		}

		// transformation
        public static Matrix4d TranslationMatrix(Vector3d v)
		{
			Matrix4d mat = IdentityMatrix();
			for (int i = 0; i < 3; ++i)
			{
				mat[i, 3] = v[i];
			}
			return mat;
		}

        public static Matrix4d ScalingMatrix(Vector3d v)
		{
			Matrix4d mat = IdentityMatrix();
			for (int i = 0; i < 3; ++i)
			{
				mat[i, i] = v[i];
			}
			return mat;
		}

        public static Matrix4d ScalingMatrix(double sx, double sy, double sz)
        {
            Matrix4d mat = IdentityMatrix();
            mat[0, 0] = sx;
            mat[1, 1] = sy;
            mat[2, 2] = sz;
            mat[3, 3] = 1.0;
            return mat;
        }

		public static Matrix4d RotationMatrix(Vector3d axis, double angle)
		{
			Matrix4d mat = new Matrix4d();

			double cos = Math.Cos(angle);
			double sin = Math.Sin(angle);

			axis.normalize();
			double x = axis[0], y = axis[1], z = axis[2];

			mat[0, 0] = cos + x * x * (1 - cos);
			mat[0, 1] = x * y * (1 - cos) - z * sin;
			mat[0, 2] = x * z * (1 - cos) + y * sin;
			mat[1, 0] = y * x * (1 - cos) + z * sin;
			mat[1, 1] = cos + y * y * (1 - cos);
			mat[1, 2] = y * z * (1 - cos) - x * sin;
			mat[2, 0] = z * x * (1 - cos) - y * sin;
			mat[2, 1] = z * y * (1 - cos) + x * sin;
			mat[2, 2] = cos + z * z * (1 - cos);
			mat[3, 3] = 1;

			return mat;
		}

        public static Matrix4d ReflectionalMatrix(Vector3d plane_normal)
        {
            // create an coordinates sys, with plane_normal as x-axis
            Vector3d x = plane_normal;
            Vector3d y;
            if (x.x == 0 && x.y == 0)
                y = new Vector3d(1, 0, 0);
            else
                y = (new Vector3d(-x.y, x.x, 0)).normalize();
            Vector3d z = x.Cross(y).normalize();
            Matrix3d R = new Matrix3d(x, y, z).Transpose();
            Matrix3d InvR = R.Inverse();
            Matrix4d U = new Matrix4d(R);
            Matrix4d V = new Matrix4d(InvR);
            Matrix4d I = Matrix4d.IdentityMatrix();
            I[0, 0] = -1; // reflect matrix along yz plane
            return V * I * U;
        }

        public static Vector3d GetMirrorSymmetryPoint(Vector3d pt, Vector3d normal, Vector3d center)
        {
            Matrix4d R = ReflectionalMatrix(normal);
            return center + (R * new Vector4d(pt - center, 0)).XYZ();
        }

        public static Vector3d GetMirrorSymmetryVector(Vector3d vec, Vector3d normal)
        {
            Matrix4d R = ReflectionalMatrix(normal);
            return (R * new Vector4d(vec, 0)).XYZ();
        }
	}//class-Matrix4d

	public class MatrixNd
	{
		private int M, N, length;

		private double[] arr;

		public int Row
		{
			get
			{
				return this.M;
			}
		}

		public int Col
		{
			get
			{
				return this.N;
			}
		}

		public MatrixNd(int nr, int nc)
		{
			M = nr;
			N = nc;
			length = M * N;
			arr = new double[length];
			for (int i = 0; i < length; ++i)
			{
				arr[i] = 0;
			}
		}

		public MatrixNd(int nr, int nc, double[] array)
		{
			M = nr;
			N = nc;
			length = M * N;
			arr = new double[length];
			for (int i = 0; i < length; ++i)
			{
				arr[i] = array[i];
			}
		}

		public MatrixNd(MatrixNd mat)
		{
			M = mat.Row;
			N = mat.Col;
			length = M * N;
			arr = new double[length];
			for (int i = 0; i < length; ++i)
			{
				arr[i] = mat.arr[i];
			}
		}

        public MatrixNd(SparseMatrix mat)
        {
            M = mat.NRow;
            N = mat.NCol;
            length = M * N;
            arr = new double[length];
            for (int i = 0; i < M; ++i)
            {
                foreach (Triplet tri in mat.GetRowTriplets(i))
                {
                    int r = tri.row;
                    int c = tri.col;
                    double val = tri.value;
                    arr[r * M + c] = val;
                }
            }
        }

		public double this[int row, int col]
		{
			get
			{
				if (row >= 0 && row < M && col >= 0 && col < N)
					return arr[row * N + col];
				else
					throw new ArgumentOutOfRangeException();
			}
			set
			{
				if (row >= 0 && row < M && col >= 0 && col < N)
					arr[row * N + col] = value;
				else
					throw new ArgumentOutOfRangeException();
			}
		}

		public double this[int index]
		{
			get
			{
				if (index >= 0 && index < length)
					return arr[index];
				else
					throw new ArgumentOutOfRangeException();
			}
			set
			{
				if (index >= 0 && index < length)
					arr[index] = value;
				else
					throw new ArgumentOutOfRangeException();
			}
		}

        public double[] ToArray()
        {
            return arr;
        }

		// operators
		static public MatrixNd operator +(MatrixNd m1, MatrixNd m2)
		{
			if(m1.M != m2.M || m1.N != m2.N)
			{
				return null;
			}
			MatrixNd m = new MatrixNd(m1);
			for (int i = 0; i < m1.length; ++i)
			{
				m[i] += m2[i];
			}
			return m;
		}

		static public MatrixNd operator -(MatrixNd m1, MatrixNd m2)
		{
			if (m1.M != m2.M || m1.N != m2.N)
			{
				return null;
			}
			MatrixNd m = new MatrixNd(m1);
			for (int i = 0; i < m1.length; ++i)
			{
				m[i] -= m2[i];
			}
			return m;
		}

		static public MatrixNd operator *(double factor, MatrixNd m)
		{
			MatrixNd mat = new MatrixNd(m);
			for (int i = 0; i < m.length; ++i)
			{
				mat[i] *= factor;
			}
			return mat;
		}

        static public MatrixNd operator *(MatrixNd m1, MatrixNd m2)
        {
            if (m1.Col != m2.Row)
            {
                return null;
            }
            MatrixNd mat = new MatrixNd(m1.Row, m2.Col);
            for (int i = 0; i < m1.Row; ++i)
            {
                for (int j = 0; j < m2.Col; ++j)
                {
                    for (int k = 0; k < m1.Col; ++k)
                    {
                        mat[i, j] += m1[i, k] * m2[k, j];
                    }
                }
            }
            return mat;
        }

		static public MatrixNd operator /(double factor, MatrixNd m)
		{
			MatrixNd mat = new MatrixNd(m);
			if (factor == 0) throw new DivideByZeroException();
			for (int i = 0; i < m.length; ++i)
			{
				mat[i] /= factor;
			}
			return mat;
		}

		// numerics
		public MatrixNd IdentityMatrix(int r, int c)
		{
			MatrixNd mat = new MatrixNd(r, c);
			for (int i = 0; i < r;++i )
			{
				mat[i, i] = 1.0;
			}
			return mat;
		}

		public MatrixNd Transpose()
		{
			MatrixNd mat = new MatrixNd(N, M);
			for (int i = 0; i < M; ++i)
			{
				for (int j = 0; j < N; ++j)
				{
					mat[j, i] = this[i, j];
				}
			}
			return mat;
		}

		public double Trace()
		{
			double sum = 0.0;
			int n = M < N ? M : N;
			for (int i = 0; i < n; ++i)
			{
				sum += this[i, i];
			}
			return sum;
		}

	}//class-MatrixNd
}
