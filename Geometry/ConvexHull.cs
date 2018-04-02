using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Geometry
{
    public unsafe class ConvexHull
    {
        private class HullFace
        {
            public int p1, p2, p3;
            public List<int> associatedPoints = new List<int>(); // associated points
            public Vector3d normal;
            public int furthestIndex = -1;
            public double furthestDistance = 0;
            public bool active = true;

            public HullFace(int p1, int p2, int p3)
            {
                this.p1 = p1;
                this.p2 = p2;
                this.p3 = p3;
            }
            public void AddPoint(int index, double dis)
            {
                associatedPoints.Add(index);
                if (dis > furthestDistance)
                {
                    furthestDistance = dis;
                    furthestIndex = index;
                }
            }
        }

        private class EdgeRecord
        {
            public int p1, p2;

            public EdgeRecord(int p1, int p2)
            {
                this.p1 = p1;
                this.p2 = p2;
            }

            public override bool Equals(object obj)
            {
                if (obj is EdgeRecord)
                {
                    EdgeRecord e = obj as EdgeRecord;
                    return ((this.p1 == e.p1 && this.p2 == e.p2) ||
                        (this.p1 == e.p2 && this.p2 == e.p1));
                }
                return false;
            }
            public override int GetHashCode()
            {
                return p1 + p2;
            }
        }

        // private fields
        private Mesh mesh = null;
        private Vector3d[] p; // array of input point set
        private IEnumerable<int> pointIndex = null; // input point index, not all of the p[] will be used
        private Queue<HullFace> faceQueue = new Queue<HullFace>(1024);
        private HashSet<HullFace> surfaceSet = new HashSet<HullFace>();
        private List<int> hullVertices = new List<int>();
        private double volume = 0;
        private int[] initIndex;
        public Vector3d _center = new Vector3d();

        // public properties
        public double Volume { get { return volume; } }
        public List<int> HullVertices
        {
            get { return hullVertices; }
        }

        // constructors
        public ConvexHull(Mesh mesh)
        {
            // init fields
            this.mesh = mesh;
            int n = mesh.VertexCount;
            int[] indexArray = new int[n];
            this.p = new Vector3d[n];
            this.pointIndex = indexArray;
            for (int i = 0; i < n; i++)
            {
                p[i] = new Vector3d(mesh.VertexPos, i * 3);
                indexArray[i] = i;
            }

            InitHullFaces();
            MainLoop();
            this.volume = ComputeVolume();
            //	Program.OutputText("vol: " + volume,true);
        }

        public ConvexHull(Vector3d[] points, IEnumerable<int> index)
        {
            this.p = points;
            this.pointIndex = index;

            InitHullFaces();
            MainLoop();
            this.volume = ComputeVolume();
        }

        // helper functions
        private int[] FindInitPoints()
        {
            int[] pts = new int[4];

            Vector3d p0 = this.p[0];
            Vector3d p1 = new Vector3d();
            Vector3d p2 = new Vector3d();
            Vector3d p3 = new Vector3d();
            Vector3d p4 = new Vector3d();
            double maxDis = 0;
            foreach (int i in this.pointIndex)
            {
                Vector3d u = this.p[i];
                double dis = (u - p0).Length();
                if (dis > maxDis)
                {
                    maxDis = dis;
                    p1 = u;
                    pts[0] = i;
                }
            }

            maxDis = 0;
            foreach (int i in this.pointIndex)
            {
                Vector3d u = this.p[i];
                double dis = (u - p1).Length();
                if (dis > maxDis)
                {
                    maxDis = dis;
                    p2 = u;
                    pts[1] = i;
                }
            }

            maxDis = 0;
            foreach (int i in this.pointIndex)
            {
                Vector3d u = this.p[i];
                double dis = Math.Abs((u - p1).Cross((p2 - p1).normalize()).Length());
                if (dis > maxDis)
                {
                    maxDis = dis;
                    p3 = u;
                    pts[2] = i;
                }
            }

            maxDis = 0;
            foreach (int i in this.pointIndex)
            {
                Vector3d u = this.p[i];
                double dis = Math.Abs((u - p1).Dot((p2 - p1).Cross(p3 - p1)));
                if (dis > maxDis)
                {
                    maxDis = dis;
                    p4 = u;
                    pts[3] = i;
                }
            }

            return pts;
        }
        private void InitHullFaces()
        {
            // get init points
            int[] initPts = FindInitPoints();
            this.initIndex = initPts;
            int p0 = initPts[0];
            int p1 = initPts[1];
            int p2 = initPts[2];
            int p3 = initPts[3];
            double vol = (p[p1] - p[p0]).Dot((p[p2] - p[p0]).Cross(p[p3] - p[p0]));
            if (vol > 0) { int t = p0; p0 = p1; p1 = t; }
            this.hullVertices.Add(p0);
            this.hullVertices.Add(p1);
            this.hullVertices.Add(p2);
            this.hullVertices.Add(p3);

            // create hull faces
            HullFace f1 = new HullFace(p0, p1, p2); this.faceQueue.Enqueue(f1);
            HullFace f2 = new HullFace(p3, p1, p0); this.faceQueue.Enqueue(f2);
            HullFace f3 = new HullFace(p3, p2, p1); this.faceQueue.Enqueue(f3);
            HullFace f4 = new HullFace(p3, p0, p2); this.faceQueue.Enqueue(f4);
            f1.normal = ((p[p1] - p[p0]).Cross(p[p2] - p[p0])).normalize();
            f2.normal = ((p[p1] - p[p3]).Cross(p[p0] - p[p3])).normalize();
            f3.normal = ((p[p2] - p[p3]).Cross(p[p1] - p[p3])).normalize();
            f4.normal = ((p[p0] - p[p3]).Cross(p[p2] - p[p3])).normalize();
            this.surfaceSet.Add(f1);
            this.surfaceSet.Add(f2);
            this.surfaceSet.Add(f3);
            this.surfaceSet.Add(f4);
            // assoicate vertices outside current hull
            foreach (int i in this.pointIndex)
            {
                if (i == p0 || i == p1 || i == p2 || i == p3) continue;
                double d1 = (p[i] - p[p0]).Dot(f1.normal);
                double d2 = (p[i] - p[p3]).Dot(f2.normal);
                double d3 = (p[i] - p[p3]).Dot(f3.normal);
                double d4 = (p[i] - p[p3]).Dot(f4.normal);
                if (d1 < 0 && d2 < 0 && d3 < 0 && d4 < 0)
                {
                    continue;
                }
                if (d1 > 0) f1.AddPoint(i, d1);
                if (d2 > 0) f2.AddPoint(i, d2);
                if (d3 > 0) { f3.AddPoint(i, d3); }
                if (d4 > 0) { f4.AddPoint(i, d4); }
            }
        }
        private void MainLoop()
        {
            int count = 0;
            while (faceQueue.Count > 0)
            {
                count++;
                HullFace face = faceQueue.Dequeue();

                if (face.active == false) continue;

                if (face.furthestIndex != -1)
                {
                    int p0 = face.furthestIndex;

                    this.hullVertices.Add(p0);

                    // collect visible faces, boundary edges and assoicated vertices
                    List<HullFace> holeFace = new List<HullFace>();
                    HashSet<EdgeRecord> holeBoundary = new HashSet<EdgeRecord>();
                    HashSet<int> assoicatedVertex = new HashSet<int>();
                    foreach (HullFace f in this.surfaceSet)
                    {
                        Vector3d v = this.p[p0] - this.p[f.p1];
                        if (v.Dot(f.normal) > 0)
                        {
                            holeFace.Add(f);

                            int p1 = f.p1;
                            int p2 = f.p2;
                            int p3 = f.p3;
                            EdgeRecord r1 = new EdgeRecord(p1, p2);
                            EdgeRecord r2 = new EdgeRecord(p2, p3);
                            EdgeRecord r3 = new EdgeRecord(p3, p1);
                            if (holeBoundary.Contains(r1)) holeBoundary.Remove(r1); else holeBoundary.Add(r1);
                            if (holeBoundary.Contains(r2)) holeBoundary.Remove(r2); else holeBoundary.Add(r2);
                            if (holeBoundary.Contains(r3)) holeBoundary.Remove(r3); else holeBoundary.Add(r3);

                            foreach (int index in f.associatedPoints)
                                assoicatedVertex.Add(index);
                        }
                    }
                    if (holeFace.Count == 0) throw new Exception();

                    // remove add visible faces
                    foreach (HullFace f in holeFace)
                    {
                        this.surfaceSet.Remove(f);
                        f.active = false;
                    }

                    // add new faces
                    foreach (EdgeRecord edge in holeBoundary)
                    {
                        HullFace newFace = new HullFace(p0, edge.p1, edge.p2);
                        newFace.normal = ((p[edge.p1] - p[p0]).Cross(p[edge.p2] - p[p0])).normalize();
                        this.surfaceSet.Add(newFace);
                        this.faceQueue.Enqueue(newFace);

                        // add assoicated vertices
                        foreach (int index in assoicatedVertex)
                        {
                            if (index == p0) continue;

                            double dis = (p[index] - p[p0]).Dot(newFace.normal);
                            if (dis > 0) newFace.AddPoint(index, dis);
                        }
                    }
                }
            }
            computeCenter();
        }
        private double ComputeVolume()
        {
            double vol = 0;
            foreach (HullFace face in surfaceSet)
            {
                Vector3d v1 = this.p[face.p1];
                Vector3d v2 = this.p[face.p2];
                Vector3d v3 = this.p[face.p3];
                vol += v1.Dot(v2.Cross(v3));
            }
            vol /= 6.0;

            return vol;
        }

        private void computeCenter()
        {
            _center = new Vector3d();
            for (int i = 0; i < hullVertices.Count; ++i)
            {
                Vector3d v = p[hullVertices[i]];
                _center += v;
            }
            _center /= hullVertices.Count;
        }

        #region IMeshDisplay Members

        //public void Display()
        //{
        //    GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_LINE);
        //    GL.glDisable(GL.GL_CULL_FACE);
        //    GL.glColor3d(0.5,0.5,0.5);

        //    Mesh m = this.mesh;

        //    GL.glLineWidth(1);

        //    GL.glBegin(GL.GL_TRIANGLES);
        //    GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
        //    fixed (double* vp = m.VertexPos)
        //    foreach (HullFace face in surfaceSet)
        //    {
        //        GL.glVertex3dv((double*)new IntPtr(vp + face.p1 * 3));
        //        GL.glVertex3dv((double*)new IntPtr(vp + face.p2 * 3));
        //        GL.glVertex3dv((double*)new IntPtr(vp + face.p3 * 3));
        //    }
        //    GL.glDisableClientState(GL.GL_VERTEX_ARRAY);
        //    GL.glEnd();
        //    GL.glEnable(GL.GL_CULL_FACE);
        //}

        public void SetData()
        {

        }

        #endregion
    }
}
