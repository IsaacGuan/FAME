using System;
using System.Drawing;
using System.Collections.Generic;

using Tao.OpenGl;
using Tao.Platform.Windows;

using Component;
using Geometry;

namespace FameBase
{
    public class ModelViewer : SimpleOpenGlControl
    {
        public ModelViewer(Model m, int idx, GLViewer glViewer, int gen)
        {
            this.InitializeComponent();
            this.InitializeContexts();
            _model = m;
            _graph = m._GRAPH;
            _mainView = glViewer;
            _idx = idx;
            _gen = gen;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Name = "ModelViewer";

            this.ResumeLayout(false);
        }

        // data read from GLViewer
        GLViewer _mainView;
        Model _model;
        Graph _graph;
        int _idx = -1;
        Matrix4d _modelViewMat = Matrix4d.IdentityMatrix();
        Vector3d _eye = new Vector3d(0, 0, 1.5);
        float[] back_color = { 1.0f, 1.0f, 1.0f };
        // 0: ancester
        // 1: last parent
        // > 1: children
        private int _gen = -1;
        bool _isSelected = false;

        public void setModelViewMatrix(Matrix4d m)
        {
            _modelViewMat = new Matrix4d(m);
        }

        public List<Part> getParts()
        {
            return _model._PARTS;
        }

        public Model _MODEL
        {
            get
            {
                return _model;
            }
        }

        public Graph _GRAPH
        {
            get
            {
                return _graph;
            }
        }

        public int _GEN
        {
            get
            {
                return _gen;
            }
        }

        public void setBackColor(Color c)
        {
            back_color[0] = (float)c.R / 255;
            back_color[1] = (float)c.G / 255;
            back_color[2] = (float)c.B / 255;
        }

        public bool isSelected()
        {
            return _isSelected;
        }

        public void unSelect()
        {
            _isSelected = false;
        }

        protected override void OnMouseClick(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseClick(e);
            _isSelected = !_isSelected;
            _mainView.userSelectModel(this._model, _isSelected);
            if (_isSelected)
            {
                this.setBackColor(GLDrawer.SelectedBackgroundColor);
                this._model._partForm._RATE = 2.0;
            }
            else
            {
                this.setBackColor(Color.White);
                this._model._partForm._RATE = 0.0;
            }
            this.Refresh();
        }

        protected override void OnMouseDoubleClick(System.Windows.Forms.MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            _mainView.setCurrentModel(_model, _idx);
            _mainView.getModelMainFuncs(_model);
            Program.GetFormMain().updateStats();
        }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            this.MakeCurrent();
            this.clearScene();
            draw();
            this.SwapBuffers();
        }// onPaint

        private void clearScene()
        {
            Gl.glClearColor(back_color[0], back_color[1], back_color[2], 0.5f);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glDisable(Gl.GL_NORMALIZE);

            setDefaultMaterial();
            SetDefaultLight();
        }

        private static void SetDefaultLight()
        {
            float[] col1 = new float[4] { 0.7f, 0.7f, 0.7f, 1.0f };
            float[] col2 = new float[4] { 0.8f, 0.7f, 0.7f, 1.0f };
            float[] col3 = new float[4] { 0, 0, 0, 1 };

            float[] pos_1 = { 10, 0, 0 };
            float[] pos_2 = { 0, 10, 0 };
            float[] pos_3 = { 0, 0, 10 };
            float[] pos_4 = { -10, 0, 0 };
            float[] pos_5 = { 0, -10, 0 };
            float[] pos_6 = { 0, 0, -10 };

            float[] intensity = { 0.5f, 0.5f, 0.5f };
            //Gl.glLightModeli(Gl.GL_LIGHT_MODEL_TWO_SIDE, Gl.GL_TRUE);
            Gl.glEnable(Gl.GL_LIGHT0);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_POSITION, pos_1);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_INTENSITY, intensity);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_SPECULAR, col1);

            Gl.glEnable(Gl.GL_LIGHT1);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_POSITION, pos_2);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_INTENSITY, intensity);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_SPECULAR, col1);

            Gl.glEnable(Gl.GL_LIGHT2);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_POSITION, pos_3);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_SPECULAR, col1);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_INTENSITY, intensity);

            Gl.glEnable(Gl.GL_LIGHT4);
            Gl.glLightfv(Gl.GL_LIGHT4, Gl.GL_POSITION, pos_5);
            Gl.glLightfv(Gl.GL_LIGHT4, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT4, Gl.GL_SPECULAR, col1);
            Gl.glLightfv(Gl.GL_LIGHT4, Gl.GL_INTENSITY, intensity);

            Gl.glEnable(Gl.GL_LIGHT5);
            Gl.glLightfv(Gl.GL_LIGHT5, Gl.GL_POSITION, pos_6);
            Gl.glLightfv(Gl.GL_LIGHT5, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT5, Gl.GL_SPECULAR, col1);
            Gl.glLightfv(Gl.GL_LIGHT5, Gl.GL_INTENSITY, intensity);
        }
        private void setDefaultMaterial()
        {
            float[] mat_a = new float[4] { 0.1f, 0.1f, 0.1f, 1.0f };
            float[] mat_d = { 0.7f, 0.7f, 0.5f, 1.0f };
            float[] mat_s = { 1.0f, 1.0f, 1.0f, 1.0f };
            float[] shine = { 120.0f };

            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT, mat_a);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_DIFFUSE, mat_d);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SPECULAR, mat_s);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, shine);

        }

        private void draw()
        {
            int w = this.Width;
            int h = this.Height;
            if (h == 0)
            {
                h = 1;
            }

            Gl.glViewport(0, 0, w, h);

            double aspect = (double)w / h;

            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();

            Glu.gluPerspective(90, aspect, 0.1, 1000);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();

            Glu.gluLookAt(_eye.x, _eye.y, _eye.z, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);

            Gl.glPushMatrix();
            Gl.glMultMatrixd(_modelViewMat.Transpose().ToArray());

            drawParts();

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
        }

        private void drawParts()
        {
            if (_model == null || _model._PARTS == null)
            {
                return;
            }

            foreach (Part part in _model._PARTS)
            {
                GLDrawer.drawMeshFace(part._MESH, part._COLOR, false);
                //this.drawBoundingbox(part._BOUNDINGBOX, part._COLOR);
            }
        }//drawParts

        private void drawMeshFace(Mesh m, Color c)
        {
            if (m == null) return;

            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            Gl.glDisable(Gl.GL_CULL_FACE);

            Gl.glShadeModel(Gl.GL_SMOOTH);

            float[] mat_a = new float[4] { c.R / 255.0f, c.G / 255.0f, c.B / 255.0f, 1.0f };

            float[] ka = { 0.1f, 0.05f, 0.0f, 1.0f };
            float[] kd = { .9f, .6f, .2f, 1.0f };
            float[] ks = { 0, 0, 0, 0 };
            float[] shine = { 1.0f };
            Gl.glColorMaterial(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT_AND_DIFFUSE);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT, mat_a);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_DIFFUSE, mat_a);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SPECULAR, ks);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, shine);

            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);

            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_NORMALIZE);

            Gl.glColor3ub(GLDrawer.ModelColor.R, GLDrawer.ModelColor.G, GLDrawer.ModelColor.B);
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
                Color fc = Color.FromArgb(m.FaceColor[i * 4 + 3], m.FaceColor[i * 4], m.FaceColor[i * 4 + 1], m.FaceColor[i * 4 + 2]);
                Gl.glColor4ub(fc.R, fc.G, fc.B, fc.A);
                Gl.glBegin(Gl.GL_TRIANGLES);
                Vector3d centroid = (v1 + v2 + v3) / 3;
                Vector3d normal = new Vector3d(m.FaceNormal[i * 3], m.FaceNormal[i * 3 + 1], m.FaceNormal[i * 3 + 2]);
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

            Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glDepthMask(Gl.GL_TRUE);

            Gl.glDisable(Gl.GL_NORMALIZE);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glDisable(Gl.GL_LIGHT0);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glDisable(Gl.GL_COLOR_MATERIAL);
        }

        private void drawBoundingbox(Prism box, Color c)
        {
            if (box == null || box._PLANES == null) return;
            for (int i = 0; i < box._PLANES.Length; ++i)
            {
                this.drawQuadTranslucent3d(box._PLANES[i], c);
            }
        }// drawBoundingbox

        private void drawQuad3d(Polygon3D q, Color c)
        {
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
        }

        private void drawQuadTranslucent3d(Polygon3D q, Color c)
        {
            Gl.glDisable(Gl.GL_LIGHTING);

            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);

            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
            // face
            Gl.glColor4ub(c.R, c.G, c.B, 100);
            Gl.glBegin(Gl.GL_POLYGON);
            for (int i = 0; i < 4; ++i)
            {
                Gl.glVertex3dv(q.points3d[i].ToArray());
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
            Gl.glEnable(Gl.GL_CULL_FACE);
            Gl.glEnable(Gl.GL_LIGHTING);
        }
    }// ModelViewer
}
