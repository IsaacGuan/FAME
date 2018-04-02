using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using Tao.OpenGl;
using Geometry;

namespace FameBase
{
    class GLDrawer
    {
        public static Color[] ColorSet = { Color.FromArgb(203, 213, 232), Color.FromArgb(252, 141, 98),
                                         Color.FromArgb(102, 194, 165), Color.FromArgb(231, 138, 195),
                                         Color.FromArgb(166, 216, 84), Color.FromArgb(251, 180, 174),
                                         Color.FromArgb(204, 235, 197), Color.FromArgb(222, 203, 228),
                                         Color.FromArgb(31, 120, 180), Color.FromArgb(251, 154, 153),
                                         Color.FromArgb(227, 26, 28), Color.FromArgb(252, 141, 98),
                                         Color.FromArgb(166, 216, 84), Color.FromArgb(231, 138, 195),
                                         Color.FromArgb(141, 211, 199), Color.FromArgb(255, 255, 179),
                                         Color.FromArgb(251, 128, 114), Color.FromArgb(179, 222, 105),
                                         Color.FromArgb(188, 128, 189), Color.FromArgb(217, 217, 217)};
        public static Color ModelColor = Color.FromArgb(254, 224, 139);
        public static Color GuideLineColor = Color.FromArgb(116, 169, 207);
        public static Color MeshColor = Color.FromArgb(116, 169, 207);
        public static Color BodyNodeColor = Color.FromArgb(43, 140, 190);
        public static Color SelectedBodyNodeColor = Color.FromArgb(215, 25, 28);
        public static Color BodeyBoneColor = Color.FromArgb(64, 64, 64);//Color.FromArgb(244, 165, 130);
        public static Color TranslucentBodyColor = Color.FromArgb(244, 165, 130);//(60, 171, 217, 233);
        public static Color BodyColor = Color.FromArgb(204, 204, 204);
        public static Color SelectionColor = Color.FromArgb(231, 138, 195);
        public static Color ContactColor = Color.FromArgb(240, 59, 32);
        public static Color HightLightContactColor = Color.FromArgb(189, 0, 38);
        public static Color DimMeshColor = Color.FromArgb(222, 235, 247);
        public static Color HighlightBboxColor = Color.FromArgb(50, 255, 255, 255);
        public static Color SelectedBackgroundColor = Color.FromArgb(253, 174, 107);
        public static Color ParentViewBackgroundColor = Color.FromArgb(224, 236, 244);
        public static Color HighlightMeshColor = Color.FromArgb(255, 189, 189, 189);

        public static Color GradientColor_0 = Color.FromArgb(254, 204, 92);
        public static Color GradientColor_1 = Color.FromArgb(189, 0, 38);
        public static Color GradientColor_2 = Color.FromArgb(178, 226, 226);
        public static Color GradientColor_3 = Color.FromArgb(0, 109, 44);
        public static Color GradientColor_4 = Color.FromArgb(251, 180, 185);
        public static Color GradientColor_5 = Color.FromArgb(122, 1, 119);
        public static Color GradientColor_6 = Color.FromArgb(231, 212, 232);
        public static Color GradientColor_7 = Color.FromArgb(118, 42, 131);
        public static Color FunctionalSpaceColor = Color.FromArgb(100, 49, 130, 189);
        public static byte FunctionalSpaceAlpha = 230;

        public static int _NSlices = 40;

        public static Color getColorGradient(double ratio, int npatch)
        {
            Color color = Color.White;
            switch (npatch)
            {
                case 1:
                    color = getColorGradient(ratio, GradientColor_2, GradientColor_3);
                    break;
                case 2:
                    color = getColorGradient(ratio, GradientColor_4, GradientColor_5);
                    break;
                case 3:
                    color = getColorGradient(ratio, GradientColor_6, GradientColor_7);
                    break;
                case 0:
                default:
                    color = getColorGradient(ratio, GradientColor_0, GradientColor_1);
                    break;
            }
            return color;
        }// getColorGradient

        public static Color getColorPatch(int n)
        {
            switch (n)
            {
                case 1:
                    return GradientColor_3;
                case 2:
                    return GradientColor_5;
                case 3:
                    return GradientColor_7;
                case 0:
                default:
                    return GradientColor_1;
            }
        }

        public static Color getColorGradient(double ratio, Color c0, Color c1)
        {
            Vector3d v0 = getColorVec(c0);
            Vector3d v1 = getColorVec(c1);
            Vector3d dir = (v1 - v0).normalize();
            Vector3d v = v0 + ratio * dir;
            return getColorRGB(v);
        }// getColorGradient

        public static Vector3d getColorVec(Color c)
        {
            return new Vector3d((double)c.R / 255, (double)c.G / 255, (double)c.B / 255);
        }// getColorVec

        public static Color getColorRGB(Vector3d v)
        {
            return Color.FromArgb((byte)(v.x * 255), (byte)(v.y * 255), (byte)(v.z * 255)); 
        }

        public static Color getColorRGB(byte[] arr)
        {
            return Color.FromArgb(arr[0], arr[1], arr[2]);
        }

        public static byte[] getColorArray(Color c)
        {
            byte[] cv = new byte[3];
            cv[0] = c.R;
            cv[1] = c.G;
            cv[2] = c.B;
            return cv;
        }

        public static byte[] getColorArray(Color c, byte alpha)
        {
            byte[] cv = new byte[4];
            cv[0] = c.R;
            cv[1] = c.G;
            cv[2] = c.B;
            cv[3] = alpha;
            return cv;
        }

        public static Color getRandomColor()
        {
            Random rand = new Random();
            Color c = Color.FromArgb(rand.Next(255), rand.Next(255), rand.Next(255));
            return c;
        }
        public static void drawTriangle(Triangle3D t)
        {
            Gl.glVertex3dv(t.u.ToArray());
            Gl.glVertex3dv(t.v.ToArray());
            Gl.glVertex3dv(t.v.ToArray());
            Gl.glVertex3dv(t.w.ToArray());
            Gl.glVertex3dv(t.w.ToArray());
            Gl.glVertex3dv(t.u.ToArray());
        }

        public static void drawCircle3D(Circle3D e, Color c)
        {
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glLineWidth(1.0f);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < e.points3d.Length; ++i)
            {
                Gl.glVertex3dv(e.points3d[i].ToArray());
                Gl.glVertex3dv(e.points3d[(i + 1) % e.points3d.Length].ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
        }// drawcircle3D

        public static void drawCircle2D(Circle3D e, Color c)
        {
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glLineWidth(1.0f);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < e.points2d.Length; ++i)
            {
                Gl.glVertex3dv(e.points2d[i].ToArray());
                Gl.glVertex3dv(e.points2d[(i + 1) % e.points3d.Length].ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
        }// drawcircle2D

        public static void drawCylinder(Vector3d u, Vector3d v, double r, Color c)
        {
            Glu.GLUquadric quad = Glu.gluNewQuadric();
            double height = (u - v).Length();
            Vector3d dir = (v - u).normalize();
            double angle = Math.Acos(dir.z / dir.Length()) * 180 / Math.PI;

            drawSphere(u, r, c);

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glPushMatrix();
            Gl.glTranslated(u.x, u.y, u.z);
            Gl.glRotated(angle, -dir.y, dir.x, 0);
            Glu.gluCylinder(quad, r, r, height, _NSlices, _NSlices);
            Gl.glPopMatrix();

            drawSphere(v, r, c);

            Glu.gluDeleteQuadric(quad); 
        }// drawCylinder

        public static void drawCylinderTranslucent(Vector3d u, Vector3d v, double r, Color c)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glDisable(Gl.GL_CULL_FACE);

            Glu.GLUquadric quad = Glu.gluNewQuadric();
            double height = (u - v).Length();
            Vector3d dir = (v - u).normalize();
            double angle = Math.Acos(dir.z / dir.Length()) * 180 / Math.PI;

            drawSphere(u, r, c);

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glPushMatrix();
            Gl.glTranslated(u.x, u.y, u.z);
            Gl.glRotated(angle, -dir.y, dir.x, 0);
            Glu.gluCylinder(quad, r, r, height, _NSlices, _NSlices);
            Gl.glPopMatrix();

            drawSphere(v, r, c);

            Glu.gluDeleteQuadric(quad);

            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_CULL_FACE);
        }// drawCylinderTranslucent

        public static void drawSphere(Vector3d o, double r, Color c)
        {
            Gl.glShadeModel(Gl.GL_SMOOTH);
            Glu.GLUquadric quad = Glu.gluNewQuadric();
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glPushMatrix();
            Gl.glTranslated(o.x, o.y, o.z);
            Glu.gluSphere(quad, r / 2, _NSlices, _NSlices);
            Gl.glPopMatrix();
            Glu.gluDeleteQuadric(quad);
        }// drawSphere

        public static void drawEllipseCurve3D(Ellipse3D e)
        {
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < e.points3d.Length; ++i)
            {
                Gl.glVertex3dv(e.points3d[i].ToArray());
                Gl.glVertex3dv(e.points3d[(i + 1) % e.points3d.Length].ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
        }// drawEllipseCurve3D

        public static void drawEllipse3D(Ellipse3D e)
        {
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < e.points3d.Length; ++i)
            {
                Gl.glVertex3dv(e.points3d[i].ToArray());
                Gl.glVertex3dv(e.points3d[(i + 1) % e.points3d.Length].ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
        }// drawEllipse3D

        public static void drawEllipsoidTranslucent(Ellipsoid e, Color c)
        {
            

            Vector3d[] points = e.getFaceVertices();
            Vector3d[] quad = new Vector3d[4];
            for (int i = 0; i < points.Length; i += 4)
            {
                for (int j = 0; j < 4; ++j)
                {
                    quad[j] = points[i + j];
                }
                drawQuad3d(quad, c);
            }
        }// drawEllipsoidTranslucent

        public static void drawEllipsoidSolid(Ellipsoid e, Color c)
        {
            Vector3d[] points = e.getFaceVertices();
            Vector3d[] quad = new Vector3d[4];
            for (int i = 0; i < points.Length; i += 4)
            {
                for (int j = 0; j < 4; ++j)
                {
                    quad[j] = points[i + j];
                }
                drawQuad3d(quad, c);
            }
        }// drawEllipsoidSolid

        public static void drawPoints2d(Vector2d[] points3d, Color c, float pointSize)
        {
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glPointSize(pointSize);
            Gl.glBegin(Gl.GL_POINTS);
            foreach (Vector2d v in points3d)
            {
                Gl.glVertex2dv(v.ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
        }

        public static void drawPoints3d(Vector3d[] points3d, Color c, float pointSize)
        {
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glPointSize(pointSize);
            Gl.glBegin(Gl.GL_POINTS);
            foreach (Vector3d v in points3d)
            {
                Gl.glVertex3dv(v.ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_POINT_SMOOTH);
        }

        public static void drawPlane(Polygon3D plane, Color c)
        {
            if (plane.points3d == null) return;
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_POLYGON);
            foreach (Vector3d p in plane.points3d)
            {
                Gl.glVertex3dv(p.ToArray());
            }
            Gl.glEnd();
        }

        public static void drawPlane2D(Polygon3D plane)
        {
            if (plane.points2d == null) return;
            Gl.glColor3ub(0, 0, 255);
            Gl.glPointSize(4.0f);
            Gl.glBegin(Gl.GL_POINTS);
            foreach (Vector2d p in plane.points2d)
            {
                Gl.glVertex2dv(p.ToArray());
            }
            Gl.glEnd();
        }

        public static void drawLines2D(List<Vector2d> points3d, Color c, float linewidth)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glColor3ub(c.R, c.G, c.B);
            for (int i = 0; i < points3d.Count - 1; ++i)
            {
                Gl.glVertex2dv(points3d[i].ToArray());
                Gl.glVertex2dv(points3d[i + 1].ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);

            Gl.glLineWidth(1.0f);
        }

        public static void drawLines2D(Vector2d v1, Vector2d v2, Color c, float linewidth)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glVertex2dv(v1.ToArray());
            Gl.glVertex2dv(v2.ToArray());
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);

            Gl.glLineWidth(1.0f);
        }

        public static void drawDashedLines2D(Vector2d v1, Vector2d v2, Color c, float linewidth)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glLineStipple(1, 0x00FF);
            Gl.glEnable(Gl.GL_LINE_STIPPLE);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex3dv(v1.ToArray());
            Gl.glVertex3dv(v2.ToArray());
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_LINE_STIPPLE);

            Gl.glLineWidth(1.0f);
        }

        public static void drawLines3D(List<Vector3d> points3d, Color c, float linewidth)
        {

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glHint(Gl.GL_POINT_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            foreach (Vector3d p in points3d)
            {
                Gl.glVertex3dv(p.ToArray());
            }
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);

            Gl.glLineWidth(1.0f);
        }

        public static void drawLines3D(Vector3d v1, Vector3d v2, Color c, float linewidth)
        {
            Gl.glDisable(Gl.GL_LIGHTING);

            Gl.glLineWidth(linewidth);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex3dv(v1.ToArray());
            Gl.glVertex3dv(v2.ToArray());
            Gl.glEnd();
        }

        public static void drawDashedLines3D(Vector3d v1, Vector3d v2, Color c, float linewidth)
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glLineWidth(linewidth);
            Gl.glLineStipple(1, 0x00FF);
            Gl.glEnable(Gl.GL_LINE_STIPPLE);
            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glVertex3dv(v1.ToArray());
            Gl.glVertex3dv(v2.ToArray());
            Gl.glEnd();

            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_LINE_STIPPLE);

            Gl.glLineWidth(1.0f);
        }

        public static void drawQuadTranslucent2d(Quad2d q, Color c)
        {
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glDisable(Gl.GL_LIGHTING);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
            Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glColor4ub(c.R, c.G, c.B, 100);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex2dv(q.points3d[i].ToArray());
            }
            Gl.glEnd();

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glLineWidth(2.0f);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex2dv(q.points3d[i].ToArray());
                Gl.glVertex2dv(q.points3d[(i + 1) % 4].ToArray());
            }
            Gl.glEnd();
            Gl.glEnable(Gl.GL_CULL_FACE);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
        }

        public static void drawQuad3d(Polygon3D q, Color c)
        {
            Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
            Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            // face
            Gl.glColor4ub(c.R, c.G, c.B, c.A);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex3dv(q.points3d[i].ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
        }

        public static void drawQuad3d(Vector3d[] pos, Color c)
        {
            Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
            Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            Gl.glColor4ub(c.R, c.G, c.B, c.A);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < pos.Length; ++i)
            {
                Gl.glVertex3dv(pos[i].ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
        }

        public static void drawQuadSolid3d(Vector3d v1, Vector3d v2, Vector3d v3, Vector3d v4, Color c)
        {
            Gl.glDisable(Gl.GL_LIGHTING);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            Gl.glDisable(Gl.GL_CULL_FACE);

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_TRIANGLES);

            Gl.glVertex3d(v1.x, v1.y, v1.z);
            Gl.glVertex3d(v2.x, v2.y, v2.z);
            Gl.glVertex3d(v3.x, v3.y, v3.z);

            Gl.glVertex3d(v3.x, v3.y, v3.z);
            Gl.glVertex3d(v1.x, v1.y, v1.z);
            Gl.glVertex3d(v4.x, v4.y, v4.z);

            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glEnable(Gl.GL_CULL_FACE);
        }// drawQuadTranslucent3d

        public static void drawQuadTranslucent3d(Vector3d v1, Vector3d v2, Vector3d v3, Vector3d v4, Color c)
        {
            Gl.glDisable(Gl.GL_LIGHTING);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            Gl.glDisable(Gl.GL_CULL_FACE);

            Gl.glColor4ub(c.R, c.G, c.B, 100);
            Gl.glBegin(Gl.GL_TRIANGLES);

            Gl.glVertex3d(v1.x, v1.y, v1.z);
            Gl.glVertex3d(v2.x, v2.y, v2.z);
            Gl.glVertex3d(v3.x, v3.y, v3.z);

            Gl.glVertex3d(v3.x, v3.y, v3.z);
            Gl.glVertex3d(v1.x, v1.y, v1.z);
            Gl.glVertex3d(v4.x, v4.y, v4.z);

            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glEnable(Gl.GL_CULL_FACE);
        }// drawQuadTranslucent3d

        public static void drawQuadTranslucent3d(Polygon3D q, Color c)
        {
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glDisable(Gl.GL_CULL_FACE);
            // face
            if (c.A == 255)
            {
                Gl.glColor4ub(c.R, c.G, c.B, 100);
            }
            else
            {
                Gl.glColor4ub(c.R, c.G, c.B, c.A);
            }
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < q.points3d.Length; ++i)
            {
                Gl.glVertex3dv(q.points3d[i].ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glEnable(Gl.GL_CULL_FACE);
        }

        public static void drawQuadEdge3d(Polygon3D q, Color c)
        {
            for (int i = 0; i < 4; ++i)
            {
                drawLines3D(q.points3d[i], q.points3d[(i + 1) % q.points3d.Length], c, 1.5f);
            }
        }

        public static void DrawCircle2(Vector2d p, Color c, float radius)
        {
            Gl.glEnable(Gl.GL_BLEND);
            //	Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            Gl.glColor4ub(c.R, c.G, c.B, 50);

            int nsample = 50;
            double delta = Math.PI * 2 / nsample;

            Gl.glLineWidth(1.0f);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < nsample; ++i)
            {
                double theta1 = i * delta;
                double x1 = p.x + radius * Math.Cos(theta1), y1 = p.y + radius * Math.Sin(theta1);
                double theta2 = (i + 1) * delta;
                double x2 = p.x + radius * Math.Cos(theta2), y2 = p.y + radius * Math.Sin(theta2);
                Gl.glVertex2d(x1, y1);
                Gl.glVertex2d(x2, y2);
            }
            Gl.glEnd();
            Gl.glLineWidth(1.0f);

            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < nsample; ++i)
            {
                double theta1 = i * delta;
                double x1 = p.x + radius * Math.Cos(theta1), y1 = p.y + radius * Math.Sin(theta1);
                Gl.glVertex2d(x1, y1);
            }
            Gl.glEnd();

            //	Gl.glDisable(Gl.GL_BLEND);
        }

        public static void drawMeshFace(Mesh m, Color c)
        {
            if (m == null) return;

            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glDisable(Gl.GL_CULL_FACE);

            Gl.glColor4ub(c.R, c.G, c.B, c.A);
            for (int i = 0, j = 0; i < m.FaceCount; ++i, j += 3)
            {
                int vidx1 = m.FaceVertexIndex[j];
                int vidx2 = m.FaceVertexIndex[j + 1];
                int vidx3 = m.FaceVertexIndex[j + 2];
                Vector3d v1 = new Vector3d(
                    m.VertexPos[vidx1 * 3], m.VertexPos[vidx1 * 3 + 1], m.VertexPos[vidx1 * 3 + 2]);
                Vector3d v2 = new Vector3d(
                    m.VertexPos[vidx2 * 3], m.VertexPos[vidx2 * 3 + 1], m.VertexPos[vidx2 * 3 + 2]);
                Vector3d v3 = new Vector3d(
                    m.VertexPos[vidx3 * 3], m.VertexPos[vidx3 * 3 + 1], m.VertexPos[vidx3 * 3 + 2]);
                Gl.glBegin(Gl.GL_TRIANGLES);
                Gl.glVertex3d(v1.x, v1.y, v1.z);
                Gl.glVertex3d(v2.x, v2.y, v2.z);
                Gl.glVertex3d(v3.x, v3.y, v3.z);
                Gl.glEnd();
            }            

            Gl.glEnable(Gl.GL_LIGHTING);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glEnable(Gl.GL_CULL_FACE);
        }

        public static void drawMeshFace(Mesh m)
        {
            if (m == null) return;

            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glEnable(Gl.GL_DEPTH_TEST);

            for (int i = 0, j = 0; i < m.FaceCount; ++i, j += 3)
            {
                int vidx1 = m.FaceVertexIndex[j];
                int vidx2 = m.FaceVertexIndex[j + 1];
                int vidx3 = m.FaceVertexIndex[j + 2];
                Vector3d v1 = new Vector3d(
                    m.VertexPos[vidx1 * 3], m.VertexPos[vidx1 * 3 + 1], m.VertexPos[vidx1 * 3 + 2]);
                Vector3d v2 = new Vector3d(
                    m.VertexPos[vidx2 * 3], m.VertexPos[vidx2 * 3 + 1], m.VertexPos[vidx2 * 3 + 2]);
                Vector3d v3 = new Vector3d(
                    m.VertexPos[vidx3 * 3], m.VertexPos[vidx3 * 3 + 1], m.VertexPos[vidx3 * 3 + 2]);
                Gl.glColor4ub(m.FaceColor[i * 4], m.FaceColor[i * 4 + 1], m.FaceColor[i * 4 + 2], m.FaceColor[i * 4 + 3]);
                Gl.glBegin(Gl.GL_TRIANGLES);
                Gl.glVertex3d(v1.x, v1.y, v1.z);
                Gl.glVertex3d(v2.x, v2.y, v2.z);
                Gl.glVertex3d(v3.x, v3.y, v3.z);
                Gl.glEnd();
            }
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_LIGHTING);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glEnable(Gl.GL_CULL_FACE);
        }

        public static void drawMeshFace(Mesh m, Color c, bool useMeshColor)
        {
            if (m == null) return;

            //drawMeshEdge(m, c);

            Gl.glPushAttrib(Gl.GL_COLOR_BUFFER_BIT);
            int iMultiSample = 0;
            int iNumSamples = 0;
            Gl.glGetIntegerv(Gl.GL_SAMPLE_BUFFERS, out iMultiSample);
            Gl.glGetIntegerv(Gl.GL_SAMPLES, out iNumSamples);
            if (iNumSamples == 0)
            {
                Gl.glEnable(Gl.GL_DEPTH_TEST);
                Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);

                //Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
                Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);

                Gl.glDisable(Gl.GL_CULL_FACE);

                Gl.glEnable(Gl.GL_BLEND);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glShadeModel(Gl.GL_SMOOTH);
            }
            else
            {
                Gl.glEnable(Gl.GL_MULTISAMPLE);
                Gl.glHint(Gl.GL_MULTISAMPLE_FILTER_HINT_NV, Gl.GL_NICEST);
                Gl.glEnable(Gl.GL_SAMPLE_ALPHA_TO_ONE);
            }

            Gl.glEnable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_NORMALIZE);

            float[] mat = new float[4] { c.R / 255f, c.G / 255f, c.B / 255f, 1.0f };
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT, mat);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_DIFFUSE, mat);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SPECULAR, mat);

            if (useMeshColor)
            {
                for (int i = 0, j = 0; i < m.FaceCount; ++i, j += 3)
                {
                    int vidx1 = m.FaceVertexIndex[j];
                    int vidx2 = m.FaceVertexIndex[j + 1];
                    int vidx3 = m.FaceVertexIndex[j + 2];
                    Vector3d v1 = new Vector3d(
                        m.VertexPos[vidx1 * 3], m.VertexPos[vidx1 * 3 + 1], m.VertexPos[vidx1 * 3 + 2]);
                    Vector3d v2 = new Vector3d(
                        m.VertexPos[vidx2 * 3], m.VertexPos[vidx2 * 3 + 1], m.VertexPos[vidx2 * 3 + 2]);
                    Vector3d v3 = new Vector3d(
                        m.VertexPos[vidx3 * 3], m.VertexPos[vidx3 * 3 + 1], m.VertexPos[vidx3 * 3 + 2]);
                    Color fc = Color.FromArgb(m.FaceColor[i * 3], m.FaceColor[i * 3 + 1], m.FaceColor[i * 3 + 2]);
                    Gl.glColor4ub(fc.R, fc.G, fc.B, fc.A);
                    Gl.glBegin(Gl.GL_TRIANGLES);
                    //Vector3d centroid = (v1 + v2 + v3) / 3;
                    Vector3d normal = new Vector3d(m.FaceNormal[i * 3], m.FaceNormal[i * 3 + 1], m.FaceNormal[i * 3 + 2]);
                    //if ((centroid - new Vector3d(0, 0, 1.5)).Dot(normal) > 0)
                    //{
                    //    normal *= -1.0;
                    //}
                    //normal *= -1;
                    Gl.glNormal3dv(normal.ToArray());
                    Vector3d n1 = new Vector3d(m.VertexNormal[vidx1 * 3], m.VertexNormal[vidx1 * 3 + 1], m.VertexNormal[vidx1 * 3 + 2]);
                    Gl.glNormal3dv(n1.ToArray());
                    Gl.glVertex3d(v1.x, v1.y, v1.z);
                    Vector3d n2 = new Vector3d(m.VertexNormal[vidx2 * 3], m.VertexNormal[vidx2 * 3 + 1], m.VertexNormal[vidx2 * 3 + 2]);
                    Gl.glNormal3dv(n2.ToArray());
                    Gl.glVertex3d(v2.x, v2.y, v2.z);
                    Vector3d n3 = new Vector3d(m.VertexNormal[vidx3 * 3], m.VertexNormal[vidx3 * 3 + 1], m.VertexNormal[vidx3 * 3 + 2]);
                    Gl.glNormal3dv(n3.ToArray());
                    Gl.glVertex3d(v3.x, v3.y, v3.z);
                    Gl.glEnd();
                }
            }
            else
            {
                Gl.glColor3ub(c.R, c.G, c.B);
                Gl.glBegin(Gl.GL_TRIANGLES);
                for (int i = 0, j = 0; i < m.FaceCount; ++i, j += 3)
                {
                    int vidx1 = m.FaceVertexIndex[j];
                    int vidx2 = m.FaceVertexIndex[j + 1];
                    int vidx3 = m.FaceVertexIndex[j + 2];
                    Vector3d v1 = new Vector3d(
                        m.VertexPos[vidx1 * 3], m.VertexPos[vidx1 * 3 + 1], m.VertexPos[vidx1 * 3 + 2]);
                    Vector3d v2 = new Vector3d(
                        m.VertexPos[vidx2 * 3], m.VertexPos[vidx2 * 3 + 1], m.VertexPos[vidx2 * 3 + 2]);
                    Vector3d v3 = new Vector3d(
                        m.VertexPos[vidx3 * 3], m.VertexPos[vidx3 * 3 + 1], m.VertexPos[vidx3 * 3 + 2]);
                    Vector3d n2 = new Vector3d(m.VertexNormal[vidx2 * 3], m.VertexNormal[vidx2 * 3 + 1], m.VertexNormal[vidx2 * 3 + 2]);
                    Vector3d n1 = new Vector3d(m.VertexNormal[vidx1 * 3], m.VertexNormal[vidx1 * 3 + 1], m.VertexNormal[vidx1 * 3 + 2]);
                    Vector3d n3 = new Vector3d(m.VertexNormal[vidx3 * 3], m.VertexNormal[vidx3 * 3 + 1], m.VertexNormal[vidx3 * 3 + 2]);
                    Vector3d normal = new Vector3d(m.FaceNormal[i * 3], m.FaceNormal[i * 3 + 1], m.FaceNormal[i * 3 + 2]);
                    Vector3d centroid = (v1 + v2 + v3) / 3;
                    if ((centroid - new Vector3d(0, 0, 1.5)).Dot(normal) < 0)
                    {
                        normal *= -1.0;
                    }

                   
                    //Gl.glNormal3dv(n1.ToArray());
                    Gl.glNormal3dv(normal.ToArray());
                    Gl.glVertex3d(v1.x, v1.y, v1.z);
                    //Gl.glNormal3dv(n2.ToArray());
                    Gl.glNormal3dv(normal.ToArray());
                    Gl.glVertex3d(v2.x, v2.y, v2.z);                   
                    //Gl.glNormal3dv(n3.ToArray());
                    Gl.glNormal3dv(normal.ToArray());
                    Gl.glVertex3d(v3.x, v3.y, v3.z);
                }
                Gl.glEnd();
            }

            if (iNumSamples == 0)
            {
                Gl.glDisable(Gl.GL_BLEND);
                //Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
                //Gl.glDepthMask(Gl.GL_TRUE);
                Gl.glDisable(Gl.GL_DEPTH_TEST);
                //Gl.glEnable(Gl.GL_CULL_FACE);
            }
            else
            {
                Gl.glDisable(Gl.GL_MULTISAMPLE);
            }

            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glDisable(Gl.GL_NORMALIZE);
            Gl.glPopAttrib();
        }

        public static void drawMeshVertices(Mesh m)
        {
            if (m == null) return;
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glColor3ub(GLDrawer.ColorSet[2].R, GLDrawer.ColorSet[2].G, GLDrawer.ColorSet[2].B);
            Gl.glPointSize(4.0f);
            Gl.glBegin(Gl.GL_POINTS);
            for (int i = 0; i < m.VertexCount; ++i)
            {
                Gl.glVertex3d(m.VertexPos[i * 3], m.VertexPos[i * 3 + 1], m.VertexPos[i * 3 + 2]);
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
        }// drawMeshVertices

        public static void drawMeshVertices_color(Mesh m)
        {
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glPointSize(4.0f);
            Gl.glBegin(Gl.GL_POINTS);
            for (int i = 0; i < m.VertexCount; ++i)
            {
                Gl.glColor3ub(m.VertexColor[i * 3], m.VertexColor[i * 3 + 1], m.VertexColor[i * 3 + 2]);
                Gl.glVertex3d(m.VertexPos[i * 3], m.VertexPos[i * 3 + 1], m.VertexPos[i * 3 + 2]);
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
        }// drawMeshVertices_color

        public static void drawPoints(Vector3d[] points, Color c, float pointSize)
        {
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glPointSize(pointSize);
            Gl.glBegin(Gl.GL_POINTS);
            for (int i = 0; i < points.Length; ++i)
            {
                Gl.glVertex3d(points[i].x, points[i].y, points[i].z);
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
        }// drawMeshVertices_color

        public static void drawPoints(Vector3d[] points, Color[] colors, float pointSize)
        {
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);

            Gl.glPointSize(pointSize);
            Gl.glBegin(Gl.GL_POINTS);
            for (int i = 0; i < points.Length; ++i)
            {
                if (colors != null && colors.Length > 0)
                {
                    Color c = colors[i];
                    Gl.glColor3ub(c.R, c.G, c.B);
                }
                else
                {
                    Gl.glColor3ub(155, 155, 155);
                }
                Gl.glVertex3d(points[i].x, points[i].y, points[i].z);
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
            Gl.glDisable(Gl.GL_DEPTH_TEST);
        }// drawMeshVertices_color


        public static void drawMeshEdge(Mesh m, Color c)
        {
            if (m == null) return;
            Gl.glShadeModel(Gl.GL_SMOOTH);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_DONT_CARE);

            Gl.glColor3ub(c.R, c.G, c.B);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < m.Edges.Length; ++i)
            {
                int fromIdx = m.Edges[i].FromIndex;
                int toIdx = m.Edges[i].ToIndex;
                Gl.glVertex3d(m.VertexPos[fromIdx * 3],
                    m.VertexPos[fromIdx * 3 + 1],
                    m.VertexPos[fromIdx * 3 + 2]);
                Gl.glVertex3d(m.VertexPos[toIdx * 3],
                    m.VertexPos[toIdx * 3 + 1],
                    m.VertexPos[toIdx * 3 + 2]);
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);
        }

        public static void drawBoundingboxWithEdges(Prism box, Color planeColor, Color lineColor)
        {
            if (box == null) return;
            if (box._PLANES != null)
            {
                for (int i = 0; i < box._PLANES.Length; ++i)
                {
                    drawQuad3d(box._PLANES[i], planeColor);
                    // lines
                    for (int j = 0; j < 4; ++j)
                    {
                        drawLines3D(box._PLANES[i].points3d[j], box._PLANES[i].points3d[(j + 1) % 4], lineColor, 2.0f);
                    }
                }
            }
        }// drawBoundingboxWithEdges

        public static void drawBoundingboxPlanes(Prism box, Color c)
        {
            if (box == null || box._PLANES == null) return;
            for (int i = 0; i < box._PLANES.Length; ++i)
            {
                drawQuadTranslucent3d(box._PLANES[i], c);
            }
        }// drawBoundingboxPlanes

        public static void drawBoundingboxEdges(Prism box, Color c)
        {
            if (box == null) return;
            if (box._PLANES != null)
            {
                for (int i = 0; i < box._PLANES.Length; ++i)
                {
                    // lines
                    for (int j = 0; j < 4; ++j)
                    {
                        drawLines3D(box._PLANES[i].points3d[j], box._PLANES[i].points3d[(j + 1) % 4], c, 2.0f);
                    }
                }
            }
        }// drawBoundingboxWithEdges

        public static void drawBoundingboxWithoutBlend(Prism box, Color c)
        {
            if (box == null) return;
            for (int i = 0; i < box._PLANES.Length; ++i)
            {
                // face
                Gl.glDisable(Gl.GL_BLEND);
                Gl.glColor4ub(c.R, c.G, c.B, c.A);
                Gl.glBegin(Gl.GL_POLYGON);
                for (int j = 0; j < 4; ++j)
                {
                    Gl.glVertex3dv(box._PLANES[i].points3d[j].ToArray());
                }
                Gl.glEnd();
            }
        }// drawBoundingboxPlanes
    }// GLDrawer
}// namespace
