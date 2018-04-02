using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Tao.OpenGl;
using Geometry;

using TrimeshWrapper;

namespace FameBase
{
    public unsafe class MeshClass
    {
        public MeshClass() { }

        public MeshClass(Mesh m)
        {
            this.mesh = m;
        }

        public Mesh Mesh
        {
            get
            {
                return this.mesh;
            }
        }

        public MyTriMesh2 TriMesh
        {
            get
            {
                return this.triMesh;
            }
        }
        private Mesh mesh;
        public int tabIndex; // list of meshes
        private float[] _material = { 0.62f, 0.74f, 0.85f, 1.0f };
        private float[] _ambient = { 0.2f, 0.2f, 0.2f, 1.0f };
        private float[] _diffuse = { 1.0f, 1.0f, 1.0f, 1.0f };
        private float[] _specular = { 1.0f, 1.0f, 1.0f, 1.0f };
        private float[] _position = { 1.0f, 1.0f, 1.0f, 0.0f };

        /******************** Function ********************/
        private double[] modelViewMat = new double[16];
        private double[] projectionMat = new double[16];
        private int[] viewPort = new int[4];
        private List<int> selectedVertices = new List<int>();
        private List<int> selectedEdges = new List<int>();
        private List<int> selectedFaces = new List<int>();
        private bool unSelect = false;
        private string meshName = "";

        static private void ArrayConvCtoSB(ref sbyte[] to_sbyte, char[] from_char)
        {
            for (int i = 0; i < from_char.Length; i++)
            {
                Array.Resize(ref to_sbyte, to_sbyte.Length + 1);
                to_sbyte[i] = (sbyte)from_char[i];
            }
        }

        public List<int> getSelectedFaces()
        {
            return this.selectedFaces;
        }

        private MyTriMesh2 triMesh;      
        public List<Vector3d> loadTriMesh(string filename, Vector3d eye)
        {
            this.mesh = new Mesh(filename, true);
            sbyte[] fn = new sbyte[0];
            ArrayConvCtoSB(ref fn, filename.ToCharArray());
            List<Vector3d> contourPoints = new List<Vector3d>();
            fixed (sbyte* meshName = fn)
            {
                triMesh = new MyTriMesh2(meshName);
                double[] pos = eye.ToArray();
                double[] contour = new double[2500000];
                int npoints = triMesh.vertextCount();
                fixed (double* eyepos = pos)
                fixed (double* output = contour)
                {
                    int nps = triMesh.get_contour(eyepos, 0, output);
                    for (int i = 0; i < nps; i+=3)
                    {
                        Vector3d v = new Vector3d(contour[i], contour[i + 1], contour[i + 2]);
                        contourPoints.Add(v);
                    }
                }
                double[] curvature = new double[npoints*3];
                fixed (double* curv1 = curvature)
                {
                    triMesh.getCurvature1(curv1);
                }
            }
            return contourPoints;
        }

        public List<Vector3d> computeContour(double[] verts, Vector3d eye, int tag)
        {
            List<Vector3d> contourPoints = new List<Vector3d>();
            double[] eyepos = eye.ToArray();
            double[] contour = new double[30000];
            if (tag >= 3)
                contour = new double[50000];
            int npoints = triMesh.vertextCount();
            fixed (double* verts_ = verts)
            {
                triMesh.set_transformed_Vertices(verts_);
            }
            fixed (double* eyepos_ = eyepos)
            fixed (double* contour_ = contour)
            {
                int nps = 0;
                switch (tag)
                {
                    case 1:
                        nps = triMesh.get_silhouette(eyepos_, 0, contour_);
                        break;
                    case 3:
                        nps = triMesh.get_suggestive_contour(eyepos_, 0, contour_);
                        break;
                    case 4:
                        nps = triMesh.get_apparent_ridges(eyepos_, 0, contour_);
                        break;
                    case 5:
                        nps = triMesh.get_boundary(eyepos_, 0, contour_);
                        break;
                    case 2:
                        nps = triMesh.get_contour(eyepos_, 0, contour_);
                        break;
                }
                
                for (int i = 0; i < nps; i += 3)
                {
                    Vector3d v = new Vector3d(contour[i], contour[i + 1], contour[i + 2]);
                    contourPoints.Add(v);
                }
            }
            return contourPoints;
        }
        
        public Vector3d projectToScreen(Vector3d v)
        {
            // project a 3D point to screen coordinate
            Vector3d screen = new Vector3d();
            Glu.gluProject(v.x, v.y, v.z, this.modelViewMat, this.projectionMat, this.viewPort,
                out screen.x, out screen.y, out screen.z);
            screen.y = this.viewPort[3] - screen.y;
            return screen;
        }//projectToScreen

        public Vector3d unProjectToModel(Vector3d v)
        {
            // unproject a screen coordinate to 3d coord
            Vector3d coord = new Vector3d();
            Glu.gluUnProject(v.x, v.y, v.z, this.modelViewMat, this.projectionMat, this.viewPort,
                out coord.x, out coord.y, out coord.z);
            return coord;
        }//unProjectToModel

        private void getViewMatrices()
        {
            // Get modelview, projection and viewport
            Gl.glGetDoublev(Gl.GL_MODELVIEW_MATRIX, modelViewMat);
            Gl.glGetDoublev(Gl.GL_PROJECTION_MATRIX, projectionMat);
            Gl.glGetIntegerv(Gl.GL_VIEWPORT, viewPort);
        }//getViewMatrices

        public void selectMouseDown(int mode, bool isShift, bool isCtrl)
        {
            this.getViewMatrices();
            this.unSelect = isCtrl;
            switch (mode)
            {
                case 1:
                    {
                        if (!isShift && !this.unSelect)
                        {
                            this.selectedVertices = new List<int>();
                        }
                        break;
                    }
                case 2:
                    {
                        if (!isShift && !this.unSelect)
                        {
                            this.selectedEdges = new List<int>();
                        }
                        break;
                    }
                case 3:
                    {
                        if (!isShift && !this.unSelect)
                        {
                            this.selectedFaces = new List<int>();
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        public void selectMouseMove(int mode, Quad2d q)
        {
            switch (mode)
            {
                case 1:
                    {
                        this.selectMeshVertex(q);
                        break;
                    }
                case 2:
                    {
                        this.selectMeshEdges(q);
                        break;
                    }
                case 3:
                    {
                        this.selectMeshFaces(q);
                        break;
                    }
                default:
                    break;
            }
        }

        public void selectMouseUp()
        {
            this.unSelect = false;
        }

        public void selectMeshVertex(Quad2d q)
        {
            double[] vertexPos = this.mesh.VertexPos;
            for (int i = 0, j = 0; i < this.mesh.VertexCount; ++i, j += 3)
            {
                Vector3d screen = this.projectToScreen(new Vector3d(vertexPos[j], vertexPos[j + 1], vertexPos[j + 2]));                
                if (Quad2d.isPointInQuad(new Vector2d(screen.x, screen.y), q))
                {
                    if (!this.unSelect)
                    {
                        if (!this.selectedVertices.Contains(i))
                        {
                            this.selectedVertices.Add(i);
                        }
                    }
                    else
                    {
                        this.selectedVertices.Remove(i);
                    }
                }
            }
        }//selectMeshVertex

        public void selectMeshEdges(Quad2d q)
        {
            HalfEdge[] guideLines = this.mesh.Edges;
            double[] vertexPos = this.mesh.VertexPos;
            for (int i = 0; i < guideLines.Length; ++i)
            {
                int fromIdx = guideLines[i].FromIndex;
                int toIdx = guideLines[i].ToIndex;
                Vector3d vf = new Vector3d(vertexPos[fromIdx * 3], 
                    vertexPos[fromIdx * 3 + 1],
                    vertexPos[fromIdx * 3 + 2]);
                Vector3d vt = new Vector3d(vertexPos[toIdx * 3],
                    vertexPos[toIdx * 3 + 1],
                    vertexPos[toIdx * 3 + 2]);
                Vector3d screenFrom = this.projectToScreen(vf);
                Vector3d screenTo = this.projectToScreen(vt);
                if (Quad2d.isPointInQuad(new Vector2d(screenFrom.x,screenFrom.y), q) ||
                    Quad2d.isPointInQuad(new Vector2d(screenTo.x, screenTo.y), q))
                {
                    if (!this.unSelect)
                    {
                        if (!this.selectedEdges.Contains(i))
                        {
                            this.selectedEdges.Add(i);
                        }
                    }
                    else
                    {
                        this.selectedEdges.Remove(i);
                    }
                }
            }
        }//selectMeshEdges

        public void selectMeshFaces(Quad2d q)
        {
            fixed (double* vertexPos = this.mesh.VertexPos)
            {
                fixed (int* faceIndex = this.mesh.FaceVertexIndex)
                {
                    for (int i = 0, j = 0; i < this.mesh.FaceCount; ++i, j += 3)
                    {
                        Vector3d[] verts = {new Vector3d(vertexPos[faceIndex[j] * 3],
                                               vertexPos[faceIndex[j] * 3 + 1],
                                               vertexPos[faceIndex[j] * 3 + 2]),
                                               new Vector3d(vertexPos[faceIndex[j + 1] * 3],
                                                   vertexPos[faceIndex[j + 1] * 3 + 1],
                                                   vertexPos[faceIndex[j + 1] * 3 + 2]),
                                               new Vector3d(vertexPos[faceIndex[j + 2] * 3],
                                                   vertexPos[faceIndex[j + 2] * 3 + 1],
                                                   vertexPos[faceIndex[j + 2] * 3 + 2])
                               };
                        for (int k = 0; k < 3; ++k)
                        {
                            Vector3d v3 = this.projectToScreen(verts[k]);
                            Vector2d screen = new Vector2d(v3.x, v3.y);
                            if (Quad2d.isPointInQuad(screen, q))
                            {
                                if (!this.unSelect)
                                {
                                    if (!this.selectedFaces.Contains(i))
                                    {
                                        this.selectedFaces.Add(i);
                                    }
                                    break;
                                }
                                else
                                {
                                    this.selectedFaces.Remove(i);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }//selectMeshFaces

        public string _MESHNAME
        {
            get
            {
                return this.meshName;
            }
            set
            {
                this.meshName = value;
            }
        }

        /******************** Render ********************/
        public void renderShaded()
        {
            //Gl.glEnable(Gl.GL_COLOR_MATERIAL);
            //Gl.glColorMaterial(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT_AND_DIFFUSE);
            //Gl.glEnable(Gl.GL_CULL_FACE);
            //Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT_AND_DIFFUSE, _material);
            //Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_AMBIENT, _ambient);
            //Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, _diffuse);
            //Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_SPECULAR, _specular);
            //Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_POSITION, _position);
            //Gl.glEnable(Gl.GL_LIGHT0);
            //Gl.glDepthFunc(Gl.GL_LESS);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glEnable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_NORMALIZE);


            Gl.glColor3ub(GLDrawer.ModelColor.R, GLDrawer.ModelColor.G, GLDrawer.ModelColor.B);

            fixed (double* vp = this.mesh.VertexPos)
            fixed (double* vn = this.mesh.FaceNormal)
            fixed (int* index = this.mesh.FaceVertexIndex)
            {
                Gl.glBegin(Gl.GL_TRIANGLES);
                for (int i = 0, j = 0; i < this.mesh.FaceCount; ++i, j += 3)
                {
                    Gl.glNormal3dv(new IntPtr(vn + j));
                    Gl.glVertex3dv(new IntPtr(vp + index[j] * 3));
                    Gl.glVertex3dv(new IntPtr(vp + index[j + 1] * 3));
                    Gl.glVertex3dv(new IntPtr(vp + index[j + 2] * 3));
                }
                Gl.glEnd();
            }
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glDisable(Gl.GL_NORMALIZE);
            Gl.glDisable(Gl.GL_LIGHTING);
            Gl.glDisable(Gl.GL_LIGHT0);
            Gl.glDisable(Gl.GL_CULL_FACE);
            Gl.glDisable(Gl.GL_COLOR_MATERIAL);
        }

        public void renderWireFrame()
        {
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glColor3ub(GLDrawer.ColorSet[1].R, GLDrawer.ColorSet[1].G, GLDrawer.ColorSet[1].B);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < this.mesh.Edges.Length; ++i)
            {
                int fromIdx = this.mesh.Edges[i].FromIndex;
                int toIdx = this.mesh.Edges[i].ToIndex;
                Gl.glVertex3d(this.mesh.VertexPos[fromIdx * 3], 
                    this.mesh.VertexPos[fromIdx*3+1],
                    this.mesh.VertexPos[fromIdx*3+2]);
                Gl.glVertex3d(this.mesh.VertexPos[toIdx * 3],
                    this.mesh.VertexPos[toIdx * 3 + 1],
                    this.mesh.VertexPos[toIdx * 3 + 2]);
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
        }

        public void renderVertices()
        {
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glColor3ub(GLDrawer.ColorSet[2].R, GLDrawer.ColorSet[2].G, GLDrawer.ColorSet[2].B);
            Gl.glPointSize(2.0f);
            Gl.glBegin(Gl.GL_POINTS);
            for (int i = 0; i < this.mesh.VertexCount; ++i)
            {
                Gl.glVertex3d(this.mesh.VertexPos[i * 3], this.mesh.VertexPos[i * 3 + 1], this.mesh.VertexPos[i * 3 + 2]);
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            //Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
        }

        public void drawSamplePoints()
        {
            if (mesh.samplePoints == null)
            {
                return;
            }
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glColor3ub(GLDrawer.ColorSet[2].R, GLDrawer.ColorSet[2].G, GLDrawer.ColorSet[2].B);
            Gl.glPointSize(4.0f);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glBegin(Gl.GL_POINTS);
            for (int i = 0; i < this.mesh.samplePoints.Length; i += 3)
            {
                Gl.glColor3ub(this.mesh.sampleColors[i], this.mesh.sampleColors[i + 1], this.mesh.sampleColors[i + 2]);
                Gl.glBegin(Gl.GL_POINTS);
                Gl.glVertex3d(this.mesh.samplePoints[i], this.mesh.samplePoints[i + 1], this.mesh.samplePoints[i + 2]);
                Gl.glEnd();
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
        }

        public void renderVertices_color()
        {
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glPointSize(2.0f);
            for (int i = 0; i < this.mesh.VertexCount; ++i)
            {
                Gl.glColor3ub(this.mesh.VertexColor[i * 3], this.mesh.VertexColor[i * 3 + 1], this.mesh.VertexColor[i * 3 + 2]);
                Gl.glBegin(Gl.GL_POINTS);
                Gl.glVertex3d(this.mesh.VertexPos[i * 3], this.mesh.VertexPos[i * 3 + 1], this.mesh.VertexPos[i * 3 + 2]);
                Gl.glEnd();
            }
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            //Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
        }

        public void drawSelectedVertex()
        {
            if (this.selectedVertices == null || this.selectedVertices.Count == 0)
            {
                return;
            }
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glColor3ub(255, 0, 0);
            Gl.glPointSize(3.0f);
            Gl.glBegin(Gl.GL_POINTS);
            for (int j = 0; j < this.selectedVertices.Count; ++j )
            {
                int i = this.selectedVertices[j];
                Gl.glVertex3d(this.mesh.VertexPos[i * 3], this.mesh.VertexPos[i * 3 + 1], this.mesh.VertexPos[i * 3 + 2]);
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_POINT_SMOOTH);
            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
        }

        public void drawSelectedEdges()
        {
            if (this.selectedEdges.Count == 0)
            {
                return;
            }
            Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glColor3ub(255, 0, 0);
            Gl.glBegin(Gl.GL_LINES);
            for (int i = 0; i < this.selectedEdges.Count; ++i)
            {
                int fromIdx = this.mesh.Edges[this.selectedEdges[i]].FromIndex;
                int toIdx = this.mesh.Edges[this.selectedEdges[i]].ToIndex;
                Gl.glVertex3d(this.mesh.VertexPos[fromIdx * 3],
                    this.mesh.VertexPos[fromIdx * 3 + 1],
                    this.mesh.VertexPos[fromIdx * 3 + 2]);
                Gl.glVertex3d(this.mesh.VertexPos[toIdx * 3],
                    this.mesh.VertexPos[toIdx * 3 + 1],
                    this.mesh.VertexPos[toIdx * 3 + 2]);
            }
            Gl.glEnd();
            Gl.glDisable(Gl.GL_LINE_SMOOTH);
            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
        }//drawSelectedEdges

        public void drawSelectedFaces()
        {
            if(this.selectedFaces.Count == 0)
            {
                return;
            }
            Gl.glColor3ub(255, 0, 0);
            fixed (double* vp = this.mesh.VertexPos)
            fixed (int* index = this.mesh.FaceVertexIndex)
            {
                Gl.glBegin(Gl.GL_TRIANGLES);
                for (int i = 0; i < this.selectedFaces.Count; ++i)
                {
                    Gl.glVertex3dv(new IntPtr(vp + index[this.selectedFaces[i] * 3] * 3));
                    Gl.glVertex3dv(new IntPtr(vp + index[this.selectedFaces[i] * 3 + 1] * 3));
                    Gl.glVertex3dv(new IntPtr(vp + index[this.selectedFaces[i] * 3 + 2] * 3));
                }
                Gl.glEnd();
            }
        }
    }
}
