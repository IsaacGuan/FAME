using System;


namespace Geometry
{
	public class Vector2d
	{
		public double x, y;

		// initialization
		public Vector2d()
		{
			x = 0; 
			y = 0;
		}

		public Vector2d(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		public Vector2d(double[] array)
		{
			if (array.Length < 2) return;
			this.x = array[0];
			this.y = array[1];
		}

        public Vector2d(Vector2d v)
        {
            this.x = v.x;
            this.y = v.y;
        }

		public double this[int index]
		{
			get
			{
				if (index == 0) return x;
				if (index == 1) return y;
				throw new ArgumentOutOfRangeException();
			}
			set
			{
				if (index == 0) x = value;
                else if (index == 1) y = value;
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
			}
		}

		// functions
		public double[] ToArray()
		{
			double[] array = new double[2];
			array[0] = x;
			array[1] = y;
			return array;
		}

		public Vector3d ToVector3D()
		{
			return new Vector3d(x, y, 0);
		}

		public Vector3d ToHomoVector3D()
		{
			return new Vector3d(x, y, 1.0);
		}

		public double Length()
		{
			return Math.Sqrt(x * x + y * y);
		}

		public Vector2d normalize()
		{
			double length = this.Length();
			x /= length;
			y /= length;
            return new Vector2d(x, y);
		}

		public double Dot(Vector2d v)
		{
			return x * v.x + y * v.y;
		}

		public double Cross(Vector2d v)
		{
			return x * v.y - y * v.x;
		}

		// following the guiderlines of implementing operator == and overide Equals, GetHashCode
		// to avoid warning...
		public override bool Equals(Object obj)
		{
			if (obj == null) return false;
			Vector2d v = obj as Vector2d;
			if ((Object)v == null) return false;
			return (x == v.x) && (y == v.y) ? true : false;
		}

		public override int GetHashCode()
		{ 
			return base.GetHashCode();
		}

		// Operators
		static public Vector2d operator +(Vector2d v1, Vector2d v2)
		{
			return new Vector2d(v1.x + v2.x, v1.y + v2.y);
		}

		static public Vector2d operator -(Vector2d v1, Vector2d v2)
		{
			return new Vector2d(v1.x - v2.x, v1.y - v2.y);
		}

		static public Vector2d operator *(double factor, Vector2d v)
		{
			return new Vector2d(v.x * factor, v.y * factor);
		}

        static public Vector2d operator *(Vector2d v, double factor)
        {
            return new Vector2d(v.x * factor, v.y * factor);
        }

		static public Vector2d operator /(Vector2d v, double factor)
		{
			return new Vector2d(v.x / factor, v.y / factor);
		}

		static public bool operator ==(Vector2d v1, Vector2d v2)
		{
			return (v1.x == v2.x) && (v1.y == v2.y) ? true : false;
		}

		static public bool operator !=(Vector2d v1, Vector2d v2)
		{
			return !(v1 == v2);
		}

        static public Vector2d Min(Vector2d v1, Vector2d v2)
        {
            Vector2d v = new Vector2d();
            v.x = v1.x < v2.x ? v1.x : v2.x;
            v.y = v1.y < v2.y ? v1.y : v2.y;
            return v;
        }

        static public Vector2d Max(Vector2d v1, Vector2d v2)
        {
            Vector2d v = new Vector2d();
            v.x = v1.x > v2.x ? v1.x : v2.x;
            v.y = v1.y > v2.y ? v1.y : v2.y;
            return v;
        }

        static public Vector2d MaxCoord()
        {
            return new Vector2d(double.MaxValue, double.MaxValue);
        }

        static public Vector2d MinCoord()
        {
            return new Vector2d(double.MinValue, double.MinValue);
        }

        public Vector2d rotate(double theta)
        {
            double[] rot = {Math.Cos(theta), -Math.Sin(theta), Math.Sin(theta), Math.Cos(theta)};
            Vector2d rotatedVec = new Vector2d();
            rotatedVec.x = rot[0] * this.x + rot[1] * this.y;
            rotatedVec.y = rot[2] * this.x + rot[3] * this.y;
            return rotatedVec;
        }
	}//class-Vector2d

	public class Vector3d
	{
		public double x, y, z;

		// initialization
		public Vector3d()
		{
			x = 0; 
			y = 0; 
			z = 0;
		}

		public Vector3d(double x, double y, double z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public Vector3d(double[] array)
		{
			if (array.Length < 3) return;
			x = array[0];
			y = array[1];
			z = array[2];
		}

        public Vector3d(double[] array, int idx)
        {
            if (idx <= 0 || array.Length < idx + 3) return;
            // for mesh vertex
            x = array[idx];
            y = array[idx + 1];
            z = array[idx + 2];
        }

        public Vector3d(Vector3d v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
        }

        public Vector3d(Vector2d v, double val)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = val;
        }

		public double this[int index]
		{
			get
			{
				if (index == 0) return x;
				if (index == 1) return y;
				if (index == 2) return z;
				throw new ArgumentOutOfRangeException();
			}
			set
			{
				if (index == 0) x = value;
                else if (index == 1) y = value;
                else if (index == 2) z = value;
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
			}
		}

		// functions
		public double[] ToArray()
		{
			double[] array = new double[3];
			array[0] = x;
			array[1] = y;
			array[2] = z;
			return array;
		}

		public Vector2d ToVector2d()
		{
			return new Vector2d(x, y);
		}

		public Vector2d HomogeneousVector2D()
		{
			if (z == 0) return ToVector2d();
			return new Vector2d(x / z, y / z);
		}

		public double Length()
		{
			return Math.Sqrt(x * x + y * y + z * z);
		}

		public Vector3d normalize()
		{
            double length = this.Length();
			x /= length;
			y /= length;
			z /= length;
            return new Vector3d(x, y, z);
		}

		public void HomogeneousNormalize()
		{
			if (z == 0) return;
			x /= z;
			y /= z;
			z = 1.0;
		}

		public double Dot(Vector3d v)
		{
			return x * v.x + y * v.y + z * v.z;
		}

		public Vector3d Cross(Vector3d v)
		{
			return new Vector3d(
				y * v.z - z * v.y,
				z * v.x - x * v.z,
				x * v.y - y * v.x);
		}

        public bool isValidVector()
        {
            return Common.isValidNumber(x) && Common.isValidNumber(y) && Common.isValidNumber(z);
        }

		// following the guiderlines of implementing operator == and overide Equals, GetHashCode
		// to avoid warning...
		public override bool Equals(Object obj)
		{
			if (obj == null) return false;
			Vector3d v = obj as Vector3d;
			if ((Object)v == null) return false;
			return (x == v.x) && (y == v.y) && (z == v.z) ? true : false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Operators
		static public Vector3d operator +(Vector3d v1, Vector3d v2)
		{
			return new Vector3d(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
		}

		static public Vector3d operator -(Vector3d v1, Vector3d v2)
		{
			return new Vector3d(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
		}

		static public Vector3d operator *(double factor, Vector3d v)
		{
			return new Vector3d(v.x * factor, v.y * factor, v.z * factor);
		}

        static public Vector3d operator *(Vector3d v, double factor)
        {
            return new Vector3d(v.x * factor, v.y * factor, v.z * factor);
        }

        static public Vector3d operator *(Vector3d v, Vector3d f)
        {
            return new Vector3d(v.x * f.x, v.y * f.y, v.z * f.z);
        }

		static public Vector3d operator /(Vector3d v, double factor)
		{
			return new Vector3d(v.x / factor, v.y / factor, v.z / factor);
		}

        static public Vector3d operator /(Vector3d v, Vector3d d)
        {
            return new Vector3d(v.x / d.x, v.y / d.y, v.z / d.z);
        }

		static public bool operator ==(Vector3d v1, Vector3d v2)
		{
			return (v1.x == v2.x) && (v1.y == v2.y) && (v1.z == v2.z) ? true : false;
		}

		static public bool operator !=(Vector3d v1, Vector3d v2)
		{
			return !(v1 == v2);
		}

        static public Vector3d Min(Vector3d v1, Vector3d v2)
        {
            Vector3d v = new Vector3d();
            v.x = v1.x < v2.x ? v1.x : v2.x;
            v.y = v1.y < v2.y ? v1.y : v2.y;
            v.z = v1.z < v2.z ? v1.z : v2.z;
            return v;
        }

        static public Vector3d Max(Vector3d v1, Vector3d v2)
        {
            Vector3d v = new Vector3d();
            v.x = v1.x > v2.x ? v1.x : v2.x;
            v.y = v1.y > v2.y ? v1.y : v2.y;
            v.z = v1.z > v2.z ? v1.z : v2.z;
            return v;
        }

        static public Vector3d MaxCoord = new Vector3d(double.MaxValue, double.MaxValue, double.MaxValue);

        static public Vector3d MinCoord = new Vector3d(double.MinValue, double.MinValue, double.MinValue);

        static public Vector3d XCoord = new Vector3d(1, 0, 0);

        static public Vector3d YCoord = new Vector3d(0, 1, 0);

        static public Vector3d ZCoord = new Vector3d(0, 0, 1);
    }//class-Vector3d

	public class Vector4d
	{ 
		public double x, y, z, w;

		// initialization
		public Vector4d()
		{
			x = 0; y = 0; z = 0;
		}

		public Vector4d(double x, double y, double z, double w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		public Vector4d(double[] array)
		{
			if (array.Length < 4) return;
			x = array[0];
			y = array[1];
			z = array[2];
			w = array[3];
		}

        public Vector4d(Vector4d v)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
            this.w = v.w;
        }

        public Vector4d(Vector3d v, double val)
        {
            this.x = v.x;
            this.y = v.y;
            this.z = v.z;
            this.w = val;
        }

		public double this[int index]
		{
			get
			{
				if (index == 0) return x;
				if (index == 1) return y;
				if (index == 2) return z;
				if (index == 3) return w;
				throw new ArgumentOutOfRangeException();
			}
			set
			{
				if (index == 0) x = value;
				else if (index == 1) y = value;
				else if (index == 2) z = value;
                else if (index == 3) w = value;
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
			}
		}

		// functions
		public double[] ToArray()
		{
			double[] array = new double[4];
			array[0] = x;
			array[1] = y;
			array[2] = z;
			array[3] = w;
			return array;
		}

		public Vector3d ToVector3D()
		{
			return new Vector3d(x, y, z);				
		}

		public Vector3d ToHomogeneousVector()
		{
			if (w == 0) return ToVector3D();
			return new Vector3d(x / w, y / w, z / w);
		}

        public Vector3d XYZ()
        {
            return new Vector3d(x, y, z);
        }

		public double Length()
		{
			return Math.Sqrt(x * x + y * y + z * z + w * w);
		}

        public Vector4d normalize()
		{
			double length = this.Length();
			x /= length;
			y /= length;
			z /= length;
			z /= length;
            return new Vector4d(x, y, z, w);
		}


		public void HomogeneousNormalize()
		{
			if (w == 0) return;
			x /= w;
			y /= w;
			z /= w;
			w = 1.0;
		}

		public double Dot(Vector4d v)
		{
			return x * v.x + y * v.y + z * v.z + z * v.w;
		}

		public override bool Equals(Object obj)
		{
			if (obj == null) return false;
			Vector4d v = obj as Vector4d;
			if ((Object)v == null) return false;
			return (x == v.x) && (y == v.y) && (z == v.z) && (w == v.w) ? true : false;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Operators
		static public Vector4d operator +(Vector4d v1, Vector4d v2)
		{
			return new Vector4d(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z, v1.w + v2.w);
		}

		static public Vector4d operator -(Vector4d v1, Vector4d v2)
		{
			return new Vector4d(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z, v1.w - v2.w);
		}

		static public Vector4d operator *(double factor, Vector4d v)
		{
			return new Vector4d(v.x * factor, v.y * factor, v.z * factor, v.w * factor);
		}

        static public Vector4d operator *(Vector4d v, double factor)
        {
            return new Vector4d(v.x * factor, v.y * factor, v.z * factor, v.w * factor);
        }

		static public Vector4d operator /(Vector4d v, double factor)
		{
			return new Vector4d(v.x / factor, v.y / factor, v.z / factor, v.w / factor);
		}

		static public bool operator ==(Vector4d v1, Vector4d v2)
		{
			return (v1.x == v2.x) && (v1.y == v2.y) 
				&& (v1.z == v2.z) && (v1.w == v2.w)? true : false;
		}

		static public bool operator !=(Vector4d v1, Vector4d v2)
		{
			return !(v1 == v2);
		}

        static public Vector4d Min(Vector4d v1, Vector4d v2)
        {
            Vector4d v = new Vector4d();
            v.x = v1.x < v2.x ? v1.x : v2.x;
            v.y = v1.y < v2.y ? v1.y : v2.y;
            v.z = v1.z < v2.z ? v1.z : v2.z;
            v.w = v1.w < v2.w ? v1.w : v2.w;
            return v;
        }

        static public Vector4d Max(Vector4d v1, Vector4d v2)
        {
            Vector4d v = new Vector4d();
            v.x = v1.x > v2.x ? v1.x : v2.x;
            v.y = v1.y > v2.y ? v1.y : v2.y;
            v.z = v1.z > v2.z ? v1.z : v2.z;
            v.w = v1.w > v2.w ? v1.w : v2.w;
            return v;
        }

	}//class-Vector4d

	public class VectorNd
	{
		public double[] val;
		int dim = 0;
		// initialization
		public VectorNd(int n)
		{
			val = new double[n];
			dim = n;
		}

		public VectorNd(double[] array)
		{
			if (array == null) return;
			dim = array.Length;
			this.val = new double[dim];
			for (int i = 0; i < dim; ++i)
			{
				this.val[i] = array[i];
			}
		}

		public double this[int index]
		{
			get
			{
				if(val != null && index < dim)
				{
					return this.val[index];
				}
				throw new ArgumentOutOfRangeException();
			}
			set
			{
				if(val!= null)
				{
					this.val[index] = value;
				}
				throw new ArgumentOutOfRangeException();
			}
		}

		// functions
		public double[] ToArray()
		{
			if(val == null)
			{
				return null;
			}
			double[] array = new double[dim];
			for (int i = 0; i < dim;++i)
			{
				array[i] = val[i];
			}
			return array;
		}

		public double Length()
		{
			double sum = 0.0;
			for (int i = 0; i < dim; ++i)
			{
				sum += val[i];
			}
			return Math.Sqrt(sum);
		}

		public void normalize()
		{
			double length = this.Length();
			for (int i = 0; i < dim; ++i)
			{
				val[i] /= length;
			}
		}

		public double Dot(VectorNd v)
		{
			if(v.dim != this.dim)
			{
				return 0;
			}
			double sum = 0.0;
			for (int i = 0; i < dim; ++i)
			{
				sum += val[i] * v[i];
			}
			return sum;
		}
		
		public override bool Equals(Object obj)
		{
			if (obj == null) return false;
			VectorNd v = obj as VectorNd;
			if ((Object)v == null) return false;
			bool isEqual = true;
			for (int i = 0; i < dim; ++i)
			{
				if (Math.Abs(val[i] - v[i]) < 1e-6)
				{
					isEqual = false;
					break;
				}
			}
			return isEqual;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Operators
		static public VectorNd operator +(VectorNd v1, VectorNd v2)
		{
			if(v1.dim!=v2.dim)
			{
				return null;
			}
			VectorNd v = new VectorNd(v1.dim);
			for (int i = 0; i < v.dim; ++i)
			{
				v[i] = v1[i] + v2[i];
			}
			return v;
		}

		static public VectorNd operator -(VectorNd v1, VectorNd v2)
		{
			if (v1.dim != v2.dim)
			{
				return null;
			}
			VectorNd v = new VectorNd(v1.dim);
			for (int i = 0; i < v.dim; ++i)
			{
				v[i] = v1[i] - v2[i];
			}
			return v;
		}

		static public VectorNd operator *(double factor, VectorNd v)
		{
			VectorNd vn = new VectorNd(v.dim);
			for (int i = 0; i < vn.dim; ++i)
			{
				vn[i] = v[i] * factor;
			}
			return vn;
		}

		static public VectorNd operator /(VectorNd v, double factor)
		{
			if(Math.Abs(factor) < 1e-6)
			{
				throw new DivideByZeroException();
			}
			VectorNd vn = new VectorNd(v.dim);
			for (int i = 0; i < vn.dim; ++i)
			{
				vn[i] = v[i] / factor;
			}
			return vn;
		}

		static public bool operator ==(VectorNd v1, VectorNd v2)
		{
			bool isEqual = true;
			for (int i = 0; i < v1.dim; ++i)
			{
				if (Math.Abs(v1[i] - v2[i]) < 1e-6)
				{
					isEqual = false;
					break;
				}
			}
			return isEqual;
		}

		static public bool operator !=(VectorNd v1, VectorNd v2)
		{
			return !(v1 == v2);
		}
	}//class-VectorNd
}
