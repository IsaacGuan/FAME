using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Geometry
{
    public class ArcBall
    {
        private double w;
        private double h;
        private double r;
        private Vector3d startPos;
        private Vector3d prevPos;
        private Vector3d currPos;
        private double angle;
        private Vector3d rotAxis;
        private bool isMouseDown = false;

        public enum MotionType
        {
            Pan, Rotate, Scale, NONE
        }
        public MotionType motion = MotionType.NONE;

        public ArcBall()
        { }

        public ArcBall(double w, double h)
        {
            this.setDimension(w, h);
        }

        public void setDimension(double w, double h)
        {
            // for windon resize
            this.w = w + 1;
            this.h = h + 1;
            this.r = w < h ? w : h + 1;
        }

        public void reset()
        {
            this.isMouseDown = false;
            this.motion = MotionType.NONE;
        }

        private Vector3d mapToSphere(double x, double y)
        {
            Vector3d p = new Vector3d(
                -1.0 + x / (this.w * 0.5), 
                -1.0 + y / (this.h * 0.5), 0
                );
            p.y = -p.y; // screen coord, from upper to down, contrary to the world coord
            double len = p.x * p.x + p.y * p.y;
            double r = 1.0;
            double r2 = r * r;
            if (len <= r2)
            {
                p.z = Math.Sqrt(r2 - len);
            }
            else
            {
                p.x *= r / Math.Sqrt(len);
                p.y *= r / Math.Sqrt(len);
            }
            return p;
        }// mapToSphere

        public void mouseDown(int x, int y, MotionType type)
        {
            this.motion = type;
            this.isMouseDown = true;
            this.startPos = mapToSphere(x, y);
            this.currPos = new Vector3d(this.startPos);
            this.prevPos = new Vector3d(this.startPos);
            this.angle = 0;
            this.rotAxis = new Vector3d();
        }

        public void mouseMove(int x, int y)
        {
            if (!isMouseDown) return;
            this.prevPos = new Vector3d(this.currPos);
            this.currPos = mapToSphere(x, y);
            double cosv = this.currPos.Dot(this.startPos)/(this.currPos.Length() * this.startPos.Length());
            angle = Math.Acos(cosv);
            rotAxis = this.currPos.Cross(this.startPos);
            rotAxis.normalize();
        }

        public void mouseUp()
        {
            this.motion = MotionType.NONE;
            this.isMouseDown = false;
        }

        public Matrix4d getTransformMatrix(int perspective)
        {
            Matrix4d m = Matrix4d.IdentityMatrix();

            switch (this.motion)
            {
                case MotionType.NONE:
                    break;
                case MotionType.Pan:
                    {
                        Vector3d d = this.currPos - this.startPos;
                        m = Matrix4d.TranslationMatrix(d);
                        break;
                    }
                case MotionType.Scale:
                    {
                        m[0, 0] = m[1, 1] = m[2, 2] = 1.0 + (this.currPos.x - this.startPos.x);
                        break;
                    }
                case MotionType.Rotate:
                default:
                    {
                        m = this.getRotationMatrix(perspective);
                        break;
                    }
            }
            return m;
        }

        public Matrix4d getRotationMatrixAlongAxis(int axis)
        {
            Matrix4d m = Matrix4d.IdentityMatrix();
            double alpha = Math.Acos(currPos.Dot(startPos));
            if (axis == 0)
            {
                m[1, 1] = Math.Cos(alpha);
                m[1, 2] = Math.Sin(alpha);
                m[2, 1] = -Math.Sin(alpha);
                m[2, 2] = Math.Cos(alpha);
            }
            if (axis == 1)
            {
                m[0, 0] = Math.Cos(alpha);
                m[0, 2] = -Math.Sin(alpha);
                m[2, 0] = Math.Sin(alpha);
                m[2, 2] = Math.Cos(alpha);
            }
            if (axis == 2)
            {
                m[0, 0] = Math.Cos(alpha);
                m[0, 1] = Math.Sin(alpha);
                m[1, 0] = -Math.Sin(alpha);
                m[1, 1] = Math.Cos(alpha);
            }
            return m;
        }// getRotationMatrixAlongAxis

        private Matrix4d getRotationMatrix(int perspective)
        {
			if (perspective == 2)
			{
				Vector3d cpos = currPos;
				Vector3d spos = startPos;
				cpos[1] = 0;
				//cpos.normalize();
				spos[1] = 0;
				//spos.normalize();

				Vector3d vn = cpos.Cross(startPos) / (cpos.Length() * spos.Length());
				Vector4d quaternion = new Vector4d(vn.x, vn.y, vn.z, cpos.Dot(startPos)) / (cpos.Length() * spos.Length());
				quaternion.normalize();

				double x = quaternion.x;
				double y = quaternion.y;
				double z = quaternion.z;
				double w = quaternion.w;

				Matrix4d m = new Matrix4d();
				m[0, 0] = w * w + x * x - y * y - z * z;
				m[0, 1] = 2 * x * y + 2 * w * z;
				m[0, 2] = 2 * x * z - 2 * w * y;
				m[1, 0] = 2 * x * y - 2 * w * z;
				m[1, 1] = w * w - x * x + y * y - z * z;
				m[1, 2] = 2 * y * z + 2 * w * x;
				m[2, 0] = 2 * x * z + 2 * w * y;
				m[2, 1] = 2 * y * z - 2 * w * x;
				m[2, 2] = w * w - x * x - y * y + z * z;
				m[3, 3] = w * w + x * x + y * y + z * z;
				return m;
			}
			else
			{
				Vector3d vn = currPos.Cross(startPos) / (currPos.Length() * startPos.Length());
				Vector4d quaternion = new Vector4d(vn.x, vn.y, vn.z, currPos.Dot(startPos)) / (currPos.Length() * startPos.Length());
				quaternion.normalize();

				double x = quaternion.x;
				double y = quaternion.y;
				double z = quaternion.z;
				double w = quaternion.w;

				Matrix4d m = new Matrix4d();
				m[0, 0] = w * w + x * x - y * y - z * z;
				m[0, 1] = 2 * x * y + 2 * w * z;
				m[0, 2] = 2 * x * z - 2 * w * y;
				m[1, 0] = 2 * x * y - 2 * w * z;
				m[1, 1] = w * w - x * x + y * y - z * z;
				m[1, 2] = 2 * y * z + 2 * w * x;
				m[2, 0] = 2 * x * z + 2 * w * y;
				m[2, 1] = 2 * y * z - 2 * w * x;
				m[2, 2] = w * w - x * x - y * y + z * z;
				m[3, 3] = w * w + x * x + y * y + z * z;
				return m;
			}
        }

    }//Arcball
}
