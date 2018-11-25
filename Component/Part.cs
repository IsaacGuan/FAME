using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;
using System.Diagnostics;
using Geometry;
using Accord.Statistics.Analysis;

namespace Component
{
    // part based modeling
    // a part includes:
    // - bounding box info, e.g., vertices
    // - mesh
    public class Part
    {
        Mesh _mesh = null;
        Prism _boundingbox = null;
        List<Part> _jointParts = new List<Part>();
        List<Joint> _joints = new List<Joint>();
        Vector3d[] axes;

        // Deprecated, since it costs to trace such indices, especially when crossover happens
        int[] _vertexIndexInParentMesh; 
        int[] _faceVIndexInParentMesh;

        public SamplePoints _partSP;
        public string _partName; // original_model_name + "_" + part_name + "_" + #num (only change the number to avoid repetitions)
        public Functionality.Category _orignCategory;

        public List<Prism> _functionalSpacePrims = new List<Prism>();

        public Color _COLOR = Color.LightBlue;

        // Test
        public Color[] _highlightColors = new Color[Functionality._NUM_CATEGORIY];

        public Part(Mesh m)
        {
            _mesh = m;

            this.fitProxy(-1);

            setRandomColorToNodes();
        }

        public Part(Mesh m, Prism p)
        {
            _mesh = m;
            _boundingbox = p;
            _boundingbox.setMaxMinScaleFromMesh(m.MaxCoord, m.MinCoord);
            _boundingbox.initInfo();
            setRandomColorToNodes();
        }

        public Part(Mesh m, int[] vIndex, double[] vPos, int[] fIndex, SamplePoints sp)
        {
            // create a mesh part from a large mesh
            // re-order the index of vertex, face
            Dictionary<int, int> d = new Dictionary<int, int>();
            _vertexIndexInParentMesh = vIndex;
            _faceVIndexInParentMesh = fIndex;
            int k = 0;
            foreach (int idx in vIndex)
            {
                if (!d.ContainsKey(idx))
                {
                    d.Add(idx, k++);
                }
            }
            int[] faceVertexIndex = new int[fIndex.Length * 3];
            for (int i = 0, j= 0; i < fIndex.Length; ++i)
            {
                int fv1 = m.FaceVertexIndex[fIndex[i] * 3];
                int fv2 = m.FaceVertexIndex[fIndex[i] * 3 + 1];
                int fv3 = m.FaceVertexIndex[fIndex[i] * 3 + 2];
                faceVertexIndex[j++] = d[fv1];
                faceVertexIndex[j++] = d[fv2];
                faceVertexIndex[j++] = d[fv3];
            }
            _mesh = new Mesh(vPos, faceVertexIndex);
            setRandomColorToNodes();
            this.fitProxy(-1);
            // build SP
            //this.buildSamplePoints(fIndex, sp);
            _partSP = sp;
        }

        public Part(Mesh m, Prism bbox, bool fit)
        {
            _mesh = m;
            _boundingbox = bbox;
            _COLOR = Color.FromArgb(Common.rand.Next(255), Common.rand.Next(255), Common.rand.Next(255));
            if (fit)
            {
                this.fitProxy(-1);
            }
        }

        public void buildSamplePoints(int[] fIndex, SamplePoints sp)
        {
            List<Vector3d> samplePoints = new List<Vector3d>();
            List<Vector3d> normals = new List<Vector3d>();
            List<int> inPartFaceIndex = new List<int>();
            List<Color> colors = new List<Color>();

            for (int i = 0; i < fIndex.Length; ++i)
            {
                int fid = fIndex[i]; // the face index in the model mesh
                if (!sp._fidxMapSPid.ContainsKey(fid)) {
                    // no sample point on this face
                    continue;
                }
                List<int> spidxs = sp._fidxMapSPid[fid]; // the index of sample/normal points of the given index
                foreach (int spid in spidxs)
                {
                    samplePoints.Add(sp._points[spid]);
                    normals.Add(sp._normals[spid]);
                    inPartFaceIndex.Add(i); // map to the new face index of the part mesh
                    //colors.Add(sp._blendColors[spid]);
                    colors.Add(_COLOR);
                }
            }
            _partSP = new SamplePoints(samplePoints.ToArray(), normals.ToArray(), inPartFaceIndex.ToArray(), 
                colors.ToArray(), fIndex.Length);
        }// extractSamplePoints

        public Object Clone()
        {
            Mesh m = _mesh.Clone() as Mesh;
            Prism prism = _boundingbox.Clone() as Prism;
            Part p = new Part(m, prism);
            p._COLOR = this._COLOR;
            if (_partSP != null)
            {
                p._partSP = _partSP.clone() as SamplePoints;
            }
            p._partName = _partName.Clone() as string;
            p._orignCategory = _orignCategory;
            return p;
        }

        public void setRandomColorToNodes()
        {
            _COLOR = Color.FromArgb(Common.rand.Next(255), Common.rand.Next(255), Common.rand.Next(255));
        }

        public void fitProxy(int option)
        {
            axes = new Vector3d[3];

            //axes[0] = Vector3d.XCoord;
            //axes[1] = Vector3d.YCoord;
            //axes[2] = Vector3d.ZCoord;
            //this.calculateAxisAlignedBbox();
            //return;

            int n = _mesh.VertexCount;
            double[][] vArray = new double[n][];
            for (int i = 0; i < n; ++i)
            {
                vArray[i] = new double[3];
            }
            Vector3d center = new Vector3d();
            for (int i = 0, j = 0; i < n; ++i, j += 3)
            {
                vArray[i][0] = _mesh.VertexPos[j];
                vArray[i][1] = _mesh.VertexPos[j + 1];
                vArray[i][2] = _mesh.VertexPos[j + 2];
                center += new Vector3d(vArray[i][0], vArray[i][1], vArray[i][2]);
            }
            center /= n;

            //axes[0] = Vector3d.XCoord;
            //axes[1] = Vector3d.YCoord;
            //axes[2] = Vector3d.ZCoord;
            //this.calculateAxisAlignedBbox();
            //return;

            PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis(vArray, AnalysisMethod.Center);
            pca.Compute();
            
            if (option == 2 || pca.Components.Count < 3)
            {
                // using axis aligned axes
                axes[0] = Vector3d.XCoord;
                axes[1] = Vector3d.YCoord;
                axes[2] = Vector3d.ZCoord;
                this.calculateAxisAlignedBbox();
            }
            else
            {
                axes[0] = new Vector3d(pca.Components[0].Eigenvector).normalize();
                axes[1] = new Vector3d(pca.Components[1].Eigenvector).normalize();
                axes[2] = new Vector3d(pca.Components[2].Eigenvector).normalize();
                double[,] trans = pca.Transform(_mesh.VertexArray, 3);
                double max_x = double.MinValue;
                double max_y = double.MinValue;
                double max_z = double.MinValue;
                double min_x = double.MaxValue;
                double min_y = double.MaxValue;
                double min_z = double.MaxValue;
                for (int i = 0, j = 0; i < n; ++i, j += 3)
                {
                    // compute the min max x,y,z
                    if (max_x < trans[i, 0]) max_x = trans[i, 0];
                    if (min_x > trans[i, 0]) min_x = trans[i, 0];
                    if (max_y < trans[i, 1]) max_y = trans[i, 1];
                    if (min_y > trans[i, 1]) min_y = trans[i, 1];
                    if (max_z < trans[i, 2]) max_z = trans[i, 2];
                    if (min_z > trans[i, 2]) min_z = trans[i, 2];
                }

                // shift the center point
                double shiftX = (max_x + min_x) / 2;
                double scaleX = (max_x - min_x) / 2;
                double shiftY = (max_y + min_y) / 2;
                double scaleY = (max_y - min_y) / 2;
                double shiftZ = (max_z + min_z) / 2;
                double scaleZ = (max_z - min_z) / 2;

                center += (axes[0] * shiftX + axes[1] * shiftY + axes[2] * shiftZ);
                Vector3d scale = new Vector3d(scaleX, scaleY, scaleZ);

                Prism cuboid = fitCuboid(_mesh, center, scale, axes);
                Prism cylinder = fitCylinder(_mesh, center, scale, axes);
                //_boundingbox = cuboid;
                if (option == -1)
                {
                    if (cuboid.fittingError >= cylinder.fittingError && (cuboid.fittingError - cylinder.fittingError) / Math.Abs(cylinder.fittingError) > 0.2)
                    {
                        _boundingbox = cylinder;
                    }
                    else
                    {
                        _boundingbox = cuboid;
                    }
                }
                else
                {
                    if (option == 0)
                    {
                        _boundingbox = cuboid;
                    }
                    else if (option == 1)
                    {
                        _boundingbox = cylinder;
                    }
                }
            }
        }// calPrincipalAxes

        public static Prism FitProxy(int option, Vector3d[] points)
        {
            if (points == null || points.Length == 0)
            {
                return null;
            }
            Vector3d[] axes = new Vector3d[3];
            int n = points.Length;
            double[,] vArray = new double[n, 3];
            Vector3d center = new Vector3d();
            Vector3d maxCoord = Vector3d.MinCoord;
            Vector3d minCoord = Vector3d.MaxCoord;
            for (int i = 0; i < n; ++i)
            {
                vArray[i, 0] = points[i].x;
                vArray[i, 1] = points[i].y;
                vArray[i, 2] = points[i].z;
                center += new Vector3d(vArray[i, 0], vArray[i, 1], vArray[i, 2]);
                maxCoord = Vector3d.Max(points[i], maxCoord);
                minCoord = Vector3d.Min(points[i], minCoord);
            }
            center /= n;

            PrincipalComponentAnalysis pca = new PrincipalComponentAnalysis(vArray, AnalysisMethod.Center);
            pca.Compute();

            Prism prism = null;

            if (pca.Components.Count < 3)
            {
                // using axis aligned axes
                axes[0] = Vector3d.XCoord;
                axes[1] = Vector3d.YCoord;
                axes[2] = Vector3d.ZCoord;
                prism = new Prism(minCoord, maxCoord);
            }
            else
            {
                axes[0] = new Vector3d(pca.Components[0].Eigenvector).normalize();
                axes[1] = new Vector3d(pca.Components[1].Eigenvector).normalize();
                axes[2] = new Vector3d(pca.Components[2].Eigenvector).normalize();
                double[,] trans = pca.Transform(vArray, 3);
                double max_x = double.MinValue;
                double max_y = double.MinValue;
                double max_z = double.MinValue;
                double min_x = double.MaxValue;
                double min_y = double.MaxValue;
                double min_z = double.MaxValue;
                for (int i = 0, j = 0; i < n; ++i, j += 3)
                {
                    // compute the min max x,y,z
                    if (max_x < trans[i, 0]) max_x = trans[i, 0];
                    if (min_x > trans[i, 0]) min_x = trans[i, 0];
                    if (max_y < trans[i, 1]) max_y = trans[i, 1];
                    if (min_y > trans[i, 1]) min_y = trans[i, 1];
                    if (max_z < trans[i, 2]) max_z = trans[i, 2];
                    if (min_z > trans[i, 2]) min_z = trans[i, 2];
                }

                // shift the center point
                double shiftX = (max_x + min_x) / 2;
                double scaleX = (max_x - min_x) / 2;
                double shiftY = (max_y + min_y) / 2;
                double scaleY = (max_y - min_y) / 2;
                double shiftZ = (max_z + min_z) / 2;
                double scaleZ = (max_z - min_z) / 2;

                center += (axes[0] * shiftX + axes[1] * shiftY + axes[2] * shiftZ);
                Vector3d scale = new Vector3d(scaleX, scaleY, scaleZ);

                Prism cuboid = fitCuboid(points, center, scale, axes);
                Prism cylinder = fitCylinder(points, center, scale, axes);
                //prism = cuboid;
                if (option == -1)
                {
                    if (cuboid.fittingError >= cylinder.fittingError && (cuboid.fittingError - cylinder.fittingError) / Math.Abs(cylinder.fittingError) > 0.2)
                    {
                        prism = cylinder;
                    }
                    else
                    {
                        prism = cuboid;
                    }
                }
                else
                {
                    if (option == 0)
                    {
                        prism = cuboid;
                    }
                    else if (option == 1)
                    {
                        prism = cylinder;
                    }
                }
            }
            prism.computeMaxMin();
            return prism;
        }// FitProxy

        public static Prism fitCuboid(Vector3d[] points, Vector3d center, Vector3d scale, Vector3d[] axes)
        {
            if (points == null || points.Length == 0)
            {
                return null;
            }
            List<int> index = new List<int>();
            Vector3d maxCoord = Vector3d.MinCoord;
            Vector3d minCoord = Vector3d.MaxCoord;
            for (int i = 0; i < points.Length; ++i)
            {
                index.Add(i);
                maxCoord = Vector3d.Max(points[i], maxCoord);
                minCoord = Vector3d.Min(points[i], minCoord);
            }
            ConvexHull hull = new ConvexHull(points, index);
            // compute box fitting error
            double boxVolume = scale.x * scale.y * scale.z * 8; // scale.x * 2, etc.
            double boxFitError = 1 - hull.Volume / boxVolume;

            if (axes[2].Dot(axes[0].Cross(axes[1])) < 0)
                axes[2] = (new Vector3d() - axes[2]).normalize();

            Prism proxy = Prism.CreateCuboid(new Vector3d(), new Vector3d(1, 1, 1));

            Matrix4d T = Matrix4d.TranslationMatrix(center);
            Matrix4d S = Matrix4d.ScalingMatrix(scale.x, scale.y, scale.z);

            Matrix3d r = new Matrix3d(axes[0], axes[1], axes[2]);
            Matrix4d R = new Matrix4d(r);
            R[3, 3] = 1.0;

            proxy.setMaxMinScaleFromMesh(maxCoord, minCoord);
            proxy.Transform(T * R * S);
            proxy.coordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy.originCoordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy._originScale = proxy._scale = scale;
            proxy.fittingError = boxFitError;
            proxy.type = Common.PrimType.Cuboid;
            proxy.updateOrigin();
            return proxy;
        }// FitCuboid

        public static Prism fitCylinder(Vector3d[] points, Vector3d center, Vector3d scale, Vector3d[] axes)
        {
            if (points == null || points.Length == 0)
            {
                return null;
            }
            List<int> index = new List<int>();
            Vector3d maxCoord = Vector3d.MinCoord;
            Vector3d minCoord = Vector3d.MaxCoord;
            for (int i = 0; i < points.Length; ++i)
            {
                index.Add(i);
                maxCoord = Vector3d.Max(points[i], maxCoord);
                minCoord = Vector3d.Min(points[i], minCoord);
            }
            ConvexHull hull = new ConvexHull(points, index);
            // compute cylinder fitting error, iterate through each axis
            int whichaxis = 0;
            double minCylFitError = 1e8;
            double idealClyRadius = 0, clyAxisLen = 0;
            for (int ax = 0; ax < 3; ++ax)
            {
                int i = (ax + 1) % 3;
                int j = (ax + 2) % 3;
                double cylRadius = Math.Max(scale[i], scale[j]);

                // volume
                double cylVolume = cylRadius * cylRadius * Math.PI * scale[ax] * 2;
                double cylFitError = 1 - hull.Volume / cylVolume;
                if (minCylFitError > cylFitError)
                {
                    minCylFitError = cylFitError;
                    whichaxis = ax;
                    idealClyRadius = cylRadius;
                    clyAxisLen = scale[ax];
                }
            }
            Prism proxy = Prism.CreateCylinder(20); // upright unit-scale cylinder
            Matrix4d T = Matrix4d.TranslationMatrix(center);
            Matrix4d S = Matrix4d.ScalingMatrix(idealClyRadius, idealClyRadius, clyAxisLen);
            Matrix4d R = Matrix4d.IdentityMatrix();
            Vector3d zaxis = new Vector3d(0, 0, 1);
            Vector3d rot_axis = zaxis.Cross(axes[whichaxis]).normalize();
            if (!double.IsNaN(rot_axis.x))
            {
                double angle = Math.Acos(zaxis.Dot(axes[whichaxis]));
                R = Matrix4d.RotationMatrix(rot_axis, angle);
            }

            proxy.setMaxMinScaleFromMesh(maxCoord, minCoord);
            proxy.Transform(T * R * S);
            proxy.coordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy.originCoordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy._originScale = proxy._scale = scale;
            proxy.fittingError = minCylFitError;
            proxy.type = Common.PrimType.Cylinder;
            proxy.updateOrigin();
            return proxy;
        }// FitCylinder

        public static Prism fitCuboid(Mesh mesh, Vector3d center, Vector3d scale, Vector3d[] axes)
        {
            ConvexHull hull = new ConvexHull(mesh);
            // compute box fitting error
            double boxVolume = scale.x * scale.y * scale.z * 8; // scale.x * 2, etc.
            double boxFitError = 1 - hull.Volume / boxVolume;

            if (axes[2].Dot(axes[0].Cross(axes[1])) < 0)
                axes[2] = (new Vector3d() - axes[2]).normalize();

            Prism proxy = Prism.CreateCuboid(new Vector3d(), new Vector3d(1, 1, 1));

            Matrix4d T = Matrix4d.TranslationMatrix(center);
            Matrix4d S = Matrix4d.ScalingMatrix(scale.x, scale.y, scale.z);

            Matrix3d r = new Matrix3d(axes[0], axes[1], axes[2]);
            Matrix4d R = new Matrix4d(r);
            R[3, 3] = 1.0;

            proxy.setMaxMinScaleFromMesh(mesh.MaxCoord, mesh.MinCoord);
            proxy.Transform(T * R * S);
            proxy.coordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy.originCoordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy._originScale = proxy._scale = scale;
            proxy.fittingError = boxFitError;
            proxy.type = Common.PrimType.Cuboid;
            proxy.updateOrigin();
            return proxy;
        }// FitCuboid

        public static Prism fitCylinder(Mesh mesh, Vector3d center, Vector3d scale, Vector3d[] axes)
        {
            ConvexHull hull = new ConvexHull(mesh);
            // compute cylinder fitting error, iterate through each axis
            int whichaxis = 0;
            double minCylFitError = 1e8;
            double idealClyRadius = 0, clyAxisLen = 0;
            for (int ax = 0; ax < 3; ++ax)
            {
                int i = (ax + 1) % 3;
                int j = (ax + 2) % 3;
                double cylRadius = Math.Max(scale[i], scale[j]);

                // volume
                double cylVolume = cylRadius * cylRadius * Math.PI * scale[ax] * 2;
                double cylFitError = 1 - hull.Volume / cylVolume;
                if (minCylFitError > cylFitError)
                {
                    minCylFitError = cylFitError;
                    whichaxis = ax;
                    idealClyRadius = cylRadius;
                    clyAxisLen = scale[ax];
                }
            }
            Prism proxy = Prism.CreateCylinder(20); // upright unit-scale cylinder
            Matrix4d T = Matrix4d.TranslationMatrix(center);
            Matrix4d S = Matrix4d.ScalingMatrix(idealClyRadius, idealClyRadius, clyAxisLen);
            Matrix4d R = Matrix4d.IdentityMatrix();
            Vector3d zaxis = new Vector3d(0, 0, 1);
            Vector3d rot_axis = zaxis.Cross(axes[whichaxis]).normalize();
            if (!double.IsNaN(rot_axis.x))
            {
                double angle = Math.Acos(zaxis.Dot(axes[whichaxis]));
                R = Matrix4d.RotationMatrix(rot_axis, angle);
            }

            proxy.setMaxMinScaleFromMesh(mesh.MaxCoord, mesh.MinCoord);
            proxy.Transform(T * R * S);
            proxy.coordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy.originCoordSys = new CoordinateSystem(center, axes[0], axes[1], axes[2]);
            proxy._originScale = proxy._scale = scale;
            proxy.fittingError = minCylFitError;
            proxy.type = Common.PrimType.Cylinder;
            proxy.updateOrigin();
            return proxy;
        }// FitCylinder

        public void Transform(Matrix4d T)
        {
            _mesh.Transform(T);
            //_boundingbox.setMaxMinScaleFromMesh(_mesh.MaxCoord, _mesh.MinCoord);
            _boundingbox.Transform(T);
            if (_partSP != null)
            {
                _partSP.Transform(T);
                //for (int i = 0; i < _partSP._points.Length; ++i)
                //{
                //    _partSP._points[i] = Common.transformVector(_partSP._points[i], T);
                //    _partSP._normals[i] = Common.transformVector(_partSP._normals[i], T);
                //}
            }
        }

        public void TransformFromOrigin(Matrix4d T)
        {
            _mesh.TransformFromOrigin(T);
            _boundingbox.TransformFromOrigin(T);
            if (_partSP != null)
            {
                _partSP.TransformFromOrigin(T);
            }
        }

        public void updateOriginPos()
        {
            _mesh.updateOriginPos();
            _boundingbox.updateOrigin();
            if (_partSP != null)
            {
                _partSP.updateOrigin();
            }
        }

        public void calculateAxisAlignedBbox()
        {
            if (_mesh == null)
            {
                return;
            }
            Vector3d maxv = _mesh.MaxCoord;
            Vector3d minv = _mesh.MinCoord;
            _boundingbox = new Prism(minv, maxv);

            _boundingbox.coordSys = new CoordinateSystem((_mesh.MaxCoord + _mesh.MinCoord)/2, axes[0], axes[1], axes[2]);
            _boundingbox.originCoordSys = new CoordinateSystem((_mesh.MaxCoord + _mesh.MinCoord) / 2, axes[0], axes[1], axes[2]);
            _boundingbox._originScale = _boundingbox._scale = _mesh.MaxCoord - _mesh.MinCoord;
        }

        public void addAJoint(Part p, Joint j)
        {
            if (!_jointParts.Contains(p))
            {
                _jointParts.Add(p);
            }
            _joints.Add(j);
        }

        public Mesh _MESH
        {
            get
            {
                return _mesh;
            }
        }

        public Prism _BOUNDINGBOX
        {
            get
            {
                return _boundingbox;
            }
        }

        public List<Joint> _JOINTS
        {
            get
            {
                return _joints;
            }
        }

        public List<Part> _JOINTPARTS
        {
            get
            {
                return _jointParts;
            }
        }

        public int[] _VERTEXINDEX
        {
            get
            {
                return _vertexIndexInParentMesh;
            }
            set
            {
                _vertexIndexInParentMesh = value;
            }
        }

        public int[] _FACEVERTEXINDEX
        {
            get
            {
                return _faceVIndexInParentMesh;
            }
            set
            {
                _faceVIndexInParentMesh = value;
            }
        }

    }// Part

    public class Model
    {
        List<Part> _parts;
        Mesh _mesh; // the whole mesh
        public Graph _GRAPH;
        public int _index = -1;
        public int nNewNodes = 0;
        
        public FunctionalSpace[] _funcSpaces;

        public string _path = "";
        public string _model_name = ""; // parent1 + parent2 + ... can be very long, so use "gen_#gid_#nid"
        public List<string> _parent_names = new List<string>();
        public List<string> _original_names = new List<string>();
        public PartGroupPair _partGroupPair; // kid hybrids, created from which two part groups

        public SamplePoints _SP;
        public FuncFeatures _funcFeat = null;
        private ConvexHull _hull = null;
        private Vector3d _centerOfMass;
        private Vector3d _centerOfConvexHull;
        private Polygon3D _symPlane;

        public Functionality.Category _CAT = Functionality.Category.None;
        public bool isDuplicated = false;

        public PartFormation _partForm;
        // test 
        public List<Vector3d> pointsTest = new List<Vector3d>();

        public Model()
        {
            _parts = new List<Part>();
        }

        public Model(List<Part> parts)
        {
            _parts = parts;
            this.init();
        }

        public Model(Mesh mesh)
        {
            _mesh = mesh;
            this.init();
        }

        public Model(Mesh mesh, List<Part> parts)
        {
            _mesh = mesh;
            _parts = parts;
        }

        public Model(Mesh mesh, SamplePoints sp, FunctionalSpace[] fss, bool needNormalize)
        {
            _mesh = mesh;
            _SP = sp;
            _funcSpaces = fss;
            if (needNormalize)
            {
                this.swithXYZ();
                this.unifyMeshFuncSpace();
                this.unify();
            }
            this.init();
        }

        private void init()
        {
            if (_mesh == null)
            {
                composeMesh();
            }
            computeCenterOfMass();
            computeConvexHull();
            bool needMerge = _parts == null ? true : _parts.Count == 0;
            this.initializeParts();
            this.unify();
            //if (needMerge)
            //{
            //    this.mergeNearbyParts();
            //}
        }

        public void initializeGraph()
        {
            _GRAPH = new Graph(_parts);
        }

        public void setGraph(Graph g)
        {
            _GRAPH = g;
        }

        public void unify()
        {
            if (_mesh == null)
            {
                this.composeMesh();
            }
            Vector3d maxCoord = _mesh.MaxCoord;
            Vector3d minCoord = _mesh.MinCoord;
            Vector3d scale = maxCoord - minCoord;
            if (_GRAPH != null)
            {
                foreach (Node node in _GRAPH._NODES)
                {
                    maxCoord = Vector3d.Max(maxCoord, node._PART._MESH.MaxCoord);
                    minCoord = Vector3d.Min(minCoord, node._PART._MESH.MinCoord);
                }
                scale = maxCoord - minCoord;
            }
            double maxS = scale.x > scale.y ? scale.x : scale.y;
            maxS = maxS > scale.z ? maxS : scale.z;
            maxS = 1.0 / maxS;
            Vector3d center = (maxCoord + minCoord) / 2;
            Matrix4d T = Matrix4d.TranslationMatrix(new Vector3d());
            Matrix4d S = Matrix4d.ScalingMatrix(new Vector3d(maxS, maxS, maxS));
            Matrix4d Q = T * S * Matrix4d.TranslationMatrix(new Vector3d() - center);
            this.Transform(Q);
            // y == 0
            minCoord = Vector3d.MaxCoord;
            if (_GRAPH != null)
            {
                foreach (Node node in _GRAPH._NODES)
                {
                    minCoord = Vector3d.Min(minCoord, node._PART._MESH.MinCoord);
                }
            }
            else
            {
                minCoord = _mesh.MinCoord;
            }
            Vector3d t = new Vector3d();
            t.y = -minCoord.y;
            T = Matrix4d.TranslationMatrix(t);

            this.Transform(T);
        }// unify

        public void setParentNames(List<Model> models)
        {
            _parent_names = new List<string>();
            _original_names = new List<string>();
            // start with parent names
            // originals names
            foreach (Model m in models)
            {
                _parent_names.Add(m._model_name);
                foreach (string name in m._parent_names) // parents' parents' also become originals
                {
                    if (!_original_names.Contains(name))
                    {
                        _original_names.Add(name);
                    }
                }
                foreach (string name in m._original_names)
                {
                    if (!_original_names.Contains(name))
                    {
                        _original_names.Add(name);
                    }
                }
            }
        }// setParentNames

        public void composeMesh()
        {
            List<double> vertexPos = new List<double>();
            List<int> faceIndex = new List<int>();
            int start_v = 0;
            int start_f = 0;
            List<Vector3d> points = new List<Vector3d>();
            List<Vector3d> normals = new List<Vector3d>();
            List<int> faceIdxs = new List<int>();
            List<Color> colors = new List<Color>();
            foreach (Part part in _parts)
            {
                Mesh mesh = part._MESH;
                part._VERTEXINDEX = new int[mesh.VertexCount];
                part._FACEVERTEXINDEX = new int[mesh.FaceCount];
                // vertex
                for (int i = 0; i < mesh.VertexCount; ++i)
                {
                    Vector3d ipos = mesh.getVertexPos(i);
                    vertexPos.Add(ipos.x);
                    vertexPos.Add(ipos.y);
                    vertexPos.Add(ipos.z);
                    part._VERTEXINDEX[i] = start_v + i;
                }
                // face
                List<int> reFaceIdx = new List<int>();
                for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                {
                    faceIndex.Add(start_v + mesh.FaceVertexIndex[j++]);
                    faceIndex.Add(start_v + mesh.FaceVertexIndex[j++]);
                    faceIndex.Add(start_v + mesh.FaceVertexIndex[j++]);
                    part._FACEVERTEXINDEX[i] = start_f + i;
                }

                SamplePoints sp = part._partSP;
                if (sp == null || sp._points == null || sp._points.Length == 0)
                {
                    //redoSP = false;
                    start_v += mesh.VertexCount;
                    start_f += mesh.FaceCount;  
                    continue;
                }
                for (int i = 0; i < sp._faceIdx.Length; ++i)
                {
                    reFaceIdx.Add(start_f + sp._faceIdx[i]);
                }
                start_v += mesh.VertexCount;
                start_f += mesh.FaceCount;                
                
                points.AddRange(sp._points);
                normals.AddRange(sp._normals);
                faceIdxs.AddRange(reFaceIdx);
                if (sp._blendColors != null)
                {
                    colors.AddRange(sp._blendColors);
                }
            }
            _mesh = new Mesh(vertexPos.ToArray(), faceIndex.ToArray());
            if (points.Count > 0)
            {
                _SP = new SamplePoints(points.ToArray(), normals.ToArray(), faceIdxs.ToArray(), colors.ToArray(), _mesh.FaceCount);
                _SP.updateNormals(_mesh);
            }
        }// composeMesh

        public string graphValidityCheck()
        {
            // graph
            if (this._GRAPH == null)
            {
                return "Model graph is not constructed yet.";
            }
            StringBuilder sb = new StringBuilder();
            // connection
            int nIsolatedNodes = _GRAPH.hasNIsolatedNodes();
            bool hasMainFunction = true;
            foreach (Node node in this._GRAPH._NODES)
            {
                foreach (Functionality.Functions f in node._funcs)
                {
                    if (Functionality.IsMainFunction(f))
                    {
                        hasMainFunction = true;
                    }
                }
            }
            sb.Append("There exisits: " + nIsolatedNodes.ToString() + " isolated nodes.");
            // functions
            if (!hasMainFunction)
            {
                sb.Append("\nLack a main functional node.");
            }
            return sb.ToString();
        }// graphValidityCheck

        public void recomputeSamplePointNormals()
        {
            if (_SP != null)
            {
                _SP.updateNormals(_mesh);
            }
            if (_GRAPH != null)
            {
                _GRAPH.recomputeSPnormals();
            }
        }
        public void checkInSamplePoints(SamplePoints sp)
        {
            if (sp == null || sp._blendColors == null)
            {
                composeMesh();
            }
            else
            {
                _SP = sp;
                _SP.updateNormals(_mesh);
            }
        }

        public void checkInSamplePoints()
        {
            if (_SP != null)
            {
                return;
            }
            _SP = new SamplePoints();
            List<Vector3d> points = new List<Vector3d>();
            List<Vector3d> normals = new List<Vector3d>();
            List<int> faceIdxs = new List<int>();
            foreach (Node node in _GRAPH._NODES)
            {
                SamplePoints sp = node._PART._partSP;
                points.AddRange(sp._points);
                normals.AddRange(sp._normals);
                faceIdxs.AddRange(sp._faceIdx);
            }
            _SP._points = points.ToArray();
            _SP._normals = normals.ToArray();
            _SP._faceIdx = faceIdxs.ToArray();
        }// checkInSamplePoints

        private void computeConvexHull()
        {
            // compute
            _hull = new ConvexHull(_mesh);
            _centerOfConvexHull = _hull._center;            
        }// computeConvexHull

        private void computeModeBox()
        {
            Vector3d minCoord = _mesh.MinCoord;
            Vector3d maxCoord = _mesh.MaxCoord;
            _centerOfMass = (minCoord + maxCoord) / 2;
            Vector3d[] symPlaneVecs = new Vector3d[4];
            // assume upright vec is y-axis, the shape is symmetry along the center plane
            symPlaneVecs[0] = new Vector3d(_centerOfMass.x, minCoord.y, minCoord.z);
            symPlaneVecs[1] = new Vector3d(_centerOfMass.x, maxCoord.y, minCoord.z);
            symPlaneVecs[2] = new Vector3d(_centerOfMass.x, maxCoord.y, maxCoord.z);
            symPlaneVecs[3] = new Vector3d(_centerOfMass.x, minCoord.y, maxCoord.z);
            _symPlane = new Polygon3D(symPlaneVecs);
        }// computeModeBox

        private void computeCenterOfMass()
        {
            _centerOfMass = new Vector3d();
            double totalVolume = 0;
            Vector3d refPoint = (_mesh.MinCoord + _mesh.MaxCoord) / 2;
            for (int i = 0; i < _mesh.FaceCount; ++i)
            {
                int v1 = _mesh.FaceVertexIndex[3 * i];
                int v2 = _mesh.FaceVertexIndex[3 * i + 1];
                int v3 = _mesh.FaceVertexIndex[3 * i + 2];
                Vector3d[] vecs = new Vector3d[4];
                vecs[0] = _mesh.getVertexPos(v1);
                vecs[1] = _mesh.getVertexPos(v2);
                vecs[2] = _mesh.getVertexPos(v3);
                vecs[3] = refPoint;
                //double[] arr = new double[16];
                //int t = 0;
                //Vector3d mass = new Vector3d();
                //for (int j = 0; j < 4; ++j)
                //{
                //    for (int k = 0; k < 3; ++k)
                //    {
                //        arr[t++] = vecs[j][k];
                //    }
                //    mass += vecs[j];
                //}
                //mass /= 4;
                //Matrix4d mat = new Matrix4d(arr);
                //double volume = mat.Determinant() / 6;
                //_centerOfMass += mass * volume;

                Vector3d pos1 = vecs[0];
                Vector3d pos2 = vecs[1];
                Vector3d pos3 = vecs[2];
                double volume = (pos1.x * pos1.y * pos3.z - pos1.x * pos3.y * pos2.z - pos2.x * pos1.y * pos3.z +
                    pos2.x * pos3.y * pos1.z + pos3.x * pos1.y * pos2.z - pos3.x * pos1.y * pos1.z) / 6;
                _centerOfMass += ((pos1 + pos2 + pos3) / 4) * volume;

                totalVolume += volume;
            }
            _centerOfMass /= totalVolume;
            if (_GRAPH != null)
            {
                _GRAPH._centerOfMass = new Vector3d(_centerOfMass);
            }
        }// computeCenterOfMass

        public void computeSamplePointsFeatures()
        {
            if (_SP == null)
            {
                this.checkInSamplePoints();
            }
            if (_symPlane == null)
            {
                computeModeBox();
            }
            int n = _SP._points.Length;
            int dim = Functionality._POINT_FEAT_DIM;
            this._funcFeat._pointFeats = new double[n * dim];
            double maxh = double.MinValue;
            double minh = double.MaxValue;
            double maxd = double.MinValue;
            double mind = double.MaxValue;
            for (int i = 0; i < n; ++i)
            {
                Vector3d v = _SP._points[i];
                Vector3d nor = _SP._normals[i];
                // height - v project to the upright (unit) vector
                double height = v.Dot(Common.uprightVec);
                maxh = maxh > height ? maxh : height;
                minh = minh < height ? minh : height;
                // angle - normal vs. upright vector
                double cosv = nor.Dot(Common.uprightVec);
                cosv = Common.cutoff(cosv, -1, 1);
                double angle = Math.Acos(cosv);
                angle /= Math.PI;
                angle = Common.cutoff(angle, 0, 1.0);
                // dist to center of hull - reflection plane
                double dist = (v - _symPlane.center).Dot(_symPlane.normal);
                dist = Math.Abs(dist);
                maxd = maxd > dist ? maxd : dist;
                mind = mind < dist ? mind : dist;
                _funcFeat._pointFeats[i * dim] = angle;
                _funcFeat._pointFeats[i * dim + 1] = height;
                _funcFeat._pointFeats[i * dim + 2] = dist;
            }
            // normalize
            double diffh = maxh - minh;
            double diffd = maxd - mind;
            for (int i = 0; i < n; ++i)
            {
                double h = _funcFeat._pointFeats[i * dim + 1];
                _funcFeat._pointFeats[i * dim + 1] = (h - minh) / diffh;
                //double d = _funcFeat._pointFeats[i * dim + 2];
                //_funcFeat._pointFeats[i * dim + 2] = (d - mind) / diffd;
                _funcFeat._pointFeats[i * dim + 2] /= maxd;
            }
        }// computeSamplePointsFeatures

        public void findBestSymPlane(Vector3d[] centers, Vector3d[] normals)
        {
            //_symPlane = new Polygon3D(new Vector3d(0, 0, 0), new Vector3d(-1, 0, 0));
            //return;
            if (centers.Length == 1)
            {
                _symPlane = new Polygon3D(centers[0], normals[0].normalize());
                return;
            }
            alglib.kdtree kdt;
            int n = _SP._points.Length;
            double[,] xy = new double[n, 3];
            int[] tags = new int[n];
            for (int i = 0; i < n; ++i)
            {
                xy[i, 0] = _SP._points[i].x;
                xy[i, 1] = _SP._points[i].y;
                xy[i, 2] = _SP._points[i].z;
                tags[i] = i;
            }
            int nx = 3;
            int ny = 0;
            int normtype = 2;
            alglib.kdtreebuildtagged(xy, tags, nx, ny, normtype, out kdt);

            double mind = double.MaxValue;
            int bestPlaneIdx = -1;
            for (int i = 0; i < centers.Length; ++i)
            {
                double sumd = 0;
                for (int j = 0; j < n; ++j)
                {
                    Vector3d pt = _SP._points[j];
                    double prj = 2 * (pt - centers[i]).Dot(normals[i]);
                    Vector3d sympt = pt - prj * normals[i];

                    int res = alglib.kdtreequeryknn(kdt, sympt.ToArray(), 1, true); // true for overlapping points
                    int[] nearestIds = new int[1];
                    alglib.kdtreequeryresultstags(kdt, ref nearestIds);
                    double d = (_SP._points[nearestIds[0]] - sympt).Length();
                    sumd += d;
                }
                if (sumd < mind)
                {
                    mind = sumd;
                    bestPlaneIdx = i;
                }
            }
            _symPlane = new Polygon3D(centers[bestPlaneIdx], normals[bestPlaneIdx].normalize());
        }// findBestSymPlane

        public void computeDistAndAngleToCenterOfConvexHull()
        {
            //if (_hull == null)
            //{
            //    computeConvexHull();
            //}
            computeConvexHull();
            int dim = Functionality._CONVEXHULL_FEAT_DIM;
            double maxdist = double.MinValue;
            double mindist = double.MaxValue;
            _funcFeat._conhullFeats = new double[dim * _SP._points.Length];
            for (int i = 0; i < _SP._points.Length; ++i)
            {
                Vector3d v = _SP._points[i];
                double dist = 0;
                double angle = 0;
                computeDistAndAngleToAnchorNormalized(v, _centerOfConvexHull, out dist, out angle);
                _funcFeat._conhullFeats[i * dim] = dist;
                _funcFeat._conhullFeats[i * dim + 1] = angle;
                maxdist = maxdist > dist ? maxdist : dist;
                mindist = mindist < dist ? mindist : dist;
            }
            double ddist = maxdist - mindist;
            for (int i = 0; i < _funcFeat._conhullFeats.Length; i += dim)
            {
                //_funcFeat._conhullFeats[i] = (_funcFeat._conhullFeats[i] - mindist) / ddist;
                _funcFeat._conhullFeats[i] /= maxdist;
            }
        }// computeDistAndAngleToCenterOfConvexHull

        public void computeDistAndAngleToCenterOfMass()
        {
            //if (!_centerOfMass.isValidVector())
            //{
            //    this.computeCenterOfMass();
            //}
            //this.computeCenterOfMass();
            _centerOfMass = (_mesh.MaxCoord + _mesh.MinCoord) / 2;
            int dim = Functionality._CONVEXHULL_FEAT_DIM;
            double maxdist = double.MinValue;
            double mindist = double.MaxValue;
            _funcFeat._cenOfMassFeats = new double[dim * _SP._points.Length];
            for (int i = 0; i < _SP._points.Length; ++i)
            {
                Vector3d v = _SP._points[i];
                double dist = 0;
                double angle = 0;
                computeDistAndAngleToAnchorNormalized(v, _centerOfMass, out dist, out angle);
                _funcFeat._cenOfMassFeats[i * dim] = dist;
                _funcFeat._cenOfMassFeats[i * dim + 1] = angle;
                maxdist = maxdist > dist ? maxdist : dist;
                mindist = mindist < dist ? mindist : dist;
            }
            double ddist = maxdist - mindist;
            for (int i = 0; i < _funcFeat._cenOfMassFeats.Length; i += dim)
            {
                //_funcFeat._cenOfMassFeats[i] = (_funcFeat._cenOfMassFeats[i] - mindist) / ddist;
                _funcFeat._cenOfMassFeats[i] /= maxdist;
            }
        }// computeDistAndAngleToCenterOfMass

        private void computeDistAndAngleToAnchorNormalized(Vector3d v, Vector3d anchor, out double dist, out double angle)
        {
            Vector3d dir = (v - anchor).normalize();
            dist = (v - anchor).Length();
            double cosv = dir.Dot(Common.uprightVec);
            cosv = Common.cutoff(cosv, -1.0, 1.0);
            angle = Math.Acos(cosv) / Math.PI;
            angle = Common.cutoff(angle, 0.0, 1.0);
        }// computeDistAndAngleToAnchorNormalized


        private void swithXYZ()
        {
            if (_mesh == null)
            {
                return;
            }
            Common.switchXYZ_mesh(_mesh, 2);
            Common.switchXYZ_mesh(_mesh, 2);
            Common.switchXYZ_mesh(_mesh, 2);
            Common.switchXYZ_mesh(_mesh, 1);
            Common.switchXYZ_mesh(_mesh, 2);
            Common.switchXYZ_mesh(_mesh, 2);
            if (_SP != null)
            {
                if (_SP._points != null)
                {
                    Common.switchXYZ_vectors(_SP._points, 2);
                    Common.switchXYZ_vectors(_SP._points, 2);
                    Common.switchXYZ_vectors(_SP._points, 2);
                    Common.switchXYZ_vectors(_SP._points, 1);
                    Common.switchXYZ_vectors(_SP._points, 2);
                    Common.switchXYZ_vectors(_SP._points, 2);
                }
                _SP.updateNormals(_mesh);
            }
            if (_funcSpaces != null)
            {
                foreach (FunctionalSpace fs in _funcSpaces)
                {
                    Common.switchXYZ_mesh(fs._mesh, 2);
                    Common.switchXYZ_mesh(fs._mesh, 2);
                    Common.switchXYZ_mesh(fs._mesh, 2);
                    Common.switchXYZ_mesh(fs._mesh, 1);
                    Common.switchXYZ_mesh(fs._mesh, 2);
                    Common.switchXYZ_mesh(fs._mesh, 2);
                }
            }
        }// swithXYZ

        private void unifyMeshFuncSpace()
        {
            if (_mesh == null)
            {
                return;
            }
            Vector3d maxCoord = _mesh.MaxCoord;
            Vector3d minCoord = _mesh.MinCoord;
            //foreach (FunctionalSpace fs in _SP.funcSpaces)
            //{
            //    maxCoord = Vector3d.Max(maxCoord, fs._mesh.MaxCoord);
            //    minCoord = Vector3d.Min(minCoord, fs._mesh.MinCoord);
            //}
            Vector3d center = (maxCoord + minCoord) / 2;
            Vector3d scale = maxCoord - minCoord;
            double maxS = scale.x > scale.y ? scale.x : scale.y;
            maxS = maxS > scale.z ? maxS : scale.z;
            maxS = 1.0 / maxS;
            //center.y = minCoord.y;
            Matrix4d T = Matrix4d.TranslationMatrix(new Vector3d(0, 0, 0));
            Matrix4d S = Matrix4d.ScalingMatrix(new Vector3d(maxS, maxS, maxS));
            Matrix4d Q = T * S * Matrix4d.TranslationMatrix(new Vector3d() - center);
            this.Transform(Q);
        }// unifyMeshFuncSpace

        private void Transform(Matrix4d T)
        {
            if (_mesh != null)
            {
                _mesh.Transform(T);
            }
            if (_SP != null)
            {
                for (int i = 0; i < _SP._points.Length; ++i)
                {
                    Vector3d ori = _SP._points[i];
                    _SP._points[i] = (T * new Vector4d(ori, 1)).ToVector3D();
                    ori = _SP._normals[i];
                    _SP._normals[i] = (T * new Vector4d(ori, 1)).ToVector3D().normalize();
                }
                if (_funcSpaces != null)
                {
                    foreach (FunctionalSpace fs in _funcSpaces)
                    {
                        fs._mesh.Transform(T);
                    }
                }
            }
            if (_GRAPH != null)
            {
                _GRAPH.transformAll(T);
            }
            else if (_parts != null)
            {
                foreach (Part p in _parts)
                {
                    p.Transform(T);
                }
            }
        }// Transform

        public void setPart(Part p, int idx)
        {
            if (idx < 0 || idx > _parts.Count)
            {
                return;
            }
            _parts[idx] = p;
        }

        public void replaceNodes(List<Node> oldNodes, List<Node> newNodes)
        {
            foreach (Node node in oldNodes)
            {
                _parts.Remove(node._PART);
                if (node.symmetry != null)
                {
                    Node sym = node.symmetry;
                    node.symmetry = null;
                    sym.symmetry = null;
                }
            }
            foreach (Node node in newNodes)
            {
                node._PART.updateOriginPos();
                _parts.Add(node._PART);
                if (node.symmetry != null)
                {
                    Node sym = node.symmetry;
                    node.symmetry = null;
                    sym.symmetry = null;
                }
            }        
        }// replaceNodes

        public Object Clone()
        {
            List<Part> parts = new List<Part>();
            foreach (Part p in _parts)
            {
                parts.Add(p.Clone() as Part);
            }
            Model m = new Model(parts);
            Mesh mesh = _mesh.Clone() as Mesh;
            m._MESH = mesh;
            //m._SP = _SP.clone() as SamplePoints;
            //if (_funcSpaces != null)
            //{
            //    FunctionalSpace[] fss = new FunctionalSpace[_funcSpaces.Length];
            //    for (int i = 0; i < _funcSpaces.Length; ++i)
            //    {
            //        fss[i] = _funcSpaces[i].clone() as FunctionalSpace;
            //    }
            //    m._funcSpaces = fss;
            //}
            m._path = this._path.Clone() as string;
            m._model_name = this._model_name.Clone() as string;
            m._GRAPH = _GRAPH.Clone(parts) as Graph;
            for (int i = 0; i < this._GRAPH._NNodes; ++i)
            {
                if (this._GRAPH._NODES[i]._functionalSpaceAgent != null)
                {
                    m._GRAPH._NODES[i]._functionalSpaceAgent = this._GRAPH._NODES[i]._functionalSpaceAgent.Clone() as Prism;
                }
            }
            return m;
        }

        private void calPartRelations()
        {
            if (_parts == null || _parts.Count == 0)
            {
                return;
            }
            for (int i = 0; i < _parts.Count - 1; ++i)
            {
                for (int j = i + 1; j < _parts.Count; ++j)
                {
                    Vector3d jointPoint;
                    double d = calClosestPointBetweenMeshes(_parts[i]._MESH, _parts[j]._MESH, out jointPoint);
                    if (d < Common._thresh)
                    {
                        // has a contact
                        Joint joint = new Joint(_parts[i], _parts[j], jointPoint);
                        _parts[i].addAJoint(_parts[j], joint);
                        _parts[j].addAJoint(_parts[i], joint);
                    }
                }
            }
        }// calPartRelations

        private double calClosestPointBetweenMeshes(Mesh m1, Mesh m2, out Vector3d jointPoint)
        {
            double min_dist = double.MaxValue;
            jointPoint = new Vector3d();
            for (int i = 0; i < m1.VertexCount; ++i)
            {
                Vector3d v1 = new Vector3d(m1.VertexPos, i * 3);
                for (int j = 0; j < m2.VertexCount; ++j)
                {
                    Vector3d v2 = new Vector3d(m2.VertexPos, j * 3);
                    double d = (v1 - v2).Length();
                    if (d < min_dist)
                    {
                        min_dist = d;
                        jointPoint = (v1 + v2) / 2;
                    }
                }
            }
            return min_dist;
        }// calClosestPointBetweenMeshes

        private void initializeParts()
        {
            if (_mesh == null || (_parts != null && _parts.Count > 0))
            {
                return;
            }
            _parts = this.initializePartsFromMesh(_mesh, _SP);
        }// initializeParts

        private List<Part> initializePartsFromMesh(Mesh mesh, SamplePoints meshSP)
        {
            // find point cluster to form initial parts
            int n = mesh.VertexCount;
            bool[] visited = new bool[n];
            List<Part> parts = new List<Part>();
            for (int k = 0; k < n; ++k)
            {
                if (visited[k]) continue;
                Queue<int> q = new Queue<int>();
                List<int> vIndex = new List<int>();
                q.Enqueue(k);
                visited[k] = true;
                // region growing from i
                while (q.Count > 0)
                {
                    int cur = q.Dequeue();
                    vIndex.Add(cur);
                    int[] curRow = mesh._VV[cur];
                    foreach (int j in curRow)
                    {
                        if (visited[j]) continue;
                        q.Enqueue(j);
                        visited[j] = true;
                    }
                }
                // vertex position, face
                double[] vPos = new double[vIndex.Count * 3];
                for (int i = 0, j = 0; i < vIndex.Count; ++i)
                {
                    int idx = vIndex[i] * 3;
                    vPos[j++] = mesh.VertexPos[idx++];
                    vPos[j++] = mesh.VertexPos[idx++];
                    vPos[j++] = mesh.VertexPos[idx++];
                }
                // collect face indices belong to this part, avoiding repeat face id
                HashSet<int> fIndex = new HashSet<int>();
                foreach (int i in vIndex)
                {
                    foreach (int f in mesh._VF[i])
                    {
                        fIndex.Add(f);
                    }
                }
                List<Vector3d> samplePnts = new List<Vector3d>();
                List<Vector3d> samplePntsNormals = new List<Vector3d>();
                List<int> samplePntsFaceIdxs = new List<int>();
                List<Color> samplePntsColors = new List<Color>();
                int[] faceIdxs = fIndex.ToArray();
                List<PatchWeightPerCategory> weightsPerCat = new List<PatchWeightPerCategory>();
                for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
                {
                    weightsPerCat.Add(new PatchWeightPerCategory(Functionality.getCategoryName(i)));
                }
                // find corresponding sample points from each triangle face 
                // each face can have 0, 1,...n sample points
                if (meshSP != null && meshSP._fidxMapSPid != null)
                {
                    List<int> spPerPart = new List<int>();
                    for (int f = 0; f < fIndex.Count; ++f)
                    {
                        if (!meshSP._fidxMapSPid.ContainsKey(faceIdxs[f]))
                        {
                            continue;
                        }
                        // sample points  on face f
                        List<int> spIds = meshSP._fidxMapSPid[faceIdxs[f]];
                        foreach (int spid in spIds)
                        {
                            samplePnts.Add(meshSP._points[spid]);
                            samplePntsNormals.Add(meshSP._normals[spid]);
                            samplePntsFaceIdxs.Add(f);
                            if (meshSP._blendColors != null && meshSP._blendColors.Length > 0)
                            {
                                samplePntsColors.Add(meshSP._blendColors[spid]);
                            }
                        }
                        spPerPart.AddRange(spIds);
                        // collect weights per part
                        int totalSamplePoints = spPerPart.Count;
                        for (int c = 0; c < Functionality._NUM_CATEGORIY; ++c)
                        {
                            weightsPerCat[c]._weights = new double[spPerPart.Count, meshSP._weightsPerCat[c]._nPatches];
                            for (int np = 0; np < totalSamplePoints; ++np)
                            {
                                int idx = spPerPart[np];
                                for (int d = 0; d < meshSP._weightsPerCat[c]._nPatches; ++d)
                                {
                                    weightsPerCat[c]._weights[np, d] = meshSP._weightsPerCat[c]._weights[idx, d];
                                }
                            }
                            weightsPerCat[c]._nPoints = totalSamplePoints;
                            weightsPerCat[c]._nPatches = meshSP._weightsPerCat[c]._nPatches;
                        }
                    }
                }
                // BUG before! preserve the part even there is no sample points
                //if (samplePnts.Count == 0)
                //{
                //    continue;
                //}
                SamplePoints sp = new SamplePoints(samplePnts.ToArray(), samplePntsNormals.ToArray(),
                    samplePntsFaceIdxs.ToArray(), samplePntsColors.ToArray(), faceIdxs.Length);
                sp._weightsPerCat = weightsPerCat;
                Part part = new Part(mesh, vIndex.ToArray(), vPos, faceIdxs, sp);
                parts.Add(part);
            }
            return parts;
        }// initializePartsFromMesh

        public void unGroupParts(List<Part> parts)
        {
            foreach (Part part in parts)
            {
                this.unGroupAPart(part);
            }
        }// unGroupParts

        private void unGroupAPart(Part part)
        {
            List<Part> parts = this.initializePartsFromMesh(part._MESH, part._partSP);
            if (parts.Count == 1)
            {
                // cannot be segmented, then each triangle surface is a part..
                for (int f = 0; f < part._MESH.FaceCount; ++f)
                {
                    int[] faceIdxs = { f };
                    int[] vIndex = new int[3];
                    vIndex[0] = part._MESH.FaceVertexIndex[3 * f];
                    vIndex[1] = part._MESH.FaceVertexIndex[3 * f + 1];
                    vIndex[2] = part._MESH.FaceVertexIndex[3 * f + 2];
                    // vertex position, face
                    double[] vPos = new double[vIndex.Length * 3];
                    for (int i = 0, j = 0; i < vIndex.Length; ++i)
                    {
                        int idx = vIndex[i] * 3;
                        vPos[j++] = part._MESH.VertexPos[idx++];
                        vPos[j++] = part._MESH.VertexPos[idx++];
                        vPos[j++] = part._MESH.VertexPos[idx++];
                    }
                    Part partf = new Part(part._MESH, vIndex, vPos, faceIdxs, null); // add sample points if needed
                    parts.Add(partf);
                }
            }
            _parts.Remove(part);
            _parts.AddRange(parts);
        }// sepearteParts

        private void mergeNearbyParts()
        {
            // In some meshes, though a vertex is shared across triangles, however, multiple vertices 
            // may be defined at the same position, causing inaccurate initial part clusters
            alglib.kdtree kdt;
            double[,] xy = new double[_mesh.VertexCount, 3];
            int[] tags = new int[_mesh.VertexCount];
            for (int i = 0, j = 0; i < _mesh.VertexCount; ++i)
            {
                xy[i, 0] = _mesh.VertexPos[j++];
                xy[i, 1] = _mesh.VertexPos[j++];
                xy[i, 2] = _mesh.VertexPos[j++];
                tags[i] = i;
            }
            int nx = 3;
            int ny = 0;
            int normtype = 2;
            alglib.kdtreebuildtagged(xy, tags, nx, ny, normtype, out kdt);
            while (true)
            {
                int[] labels = new int[_mesh.VertexCount];
                int l = 0;
                foreach (Part part in _parts)
                {
                    foreach (int idx in part._VERTEXINDEX)
                    {
                        labels[idx] = l;
                    }
                    ++l;
                }
                // search
                Part pa = null;
                Part pb = null;
                int k = 5;
                if (_mesh.VertexCount < k)
                {
                    return;
                }
                double[,] x = new double[0,0];
                int[] tag = new int[k];
                foreach (Part p1 in _parts)
                {
                    bool found = false;
                    foreach (int v in p1._VERTEXINDEX)
                    {
                        double[] vpos = { _mesh.VertexPos[v * 3], _mesh.VertexPos[v * 3 + 1], _mesh.VertexPos[v * 3 + 2] };
                        int res = alglib.kdtreequeryknn(kdt, vpos, k, true); // true for overlapping points
                        alglib.kdtreequeryresultsx(kdt, ref x);
                        alglib.kdtreequeryresultstags(kdt, ref tag);
                        Vector3d v1 = new Vector3d(vpos[0],vpos[1],vpos[2]);
                        for (int i = 0; i < k; ++i)
                        {
                            Part p2 = _parts[labels[tag[i]]];
                            if (p1 == p2) continue;
                            double d = (v1 - new Vector3d(x[i, 0], x[i, 1], x[i, 2])).Length();
                            if (d < Common._thresh)
                            {
                                pa = p1;
                                pb = p2;
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            break;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
                if (pa != null && pb != null)
                {
                    // group parts
                    List<Part> parts = new List<Part>();
                    parts.Add(pa);
                    parts.Add(pb);
                    groupParts(parts);
                }
                else
                {
                    break; // couldn't find any parts for grouping
                }
            }// while
        }// mergeNearbyParts

        public Part groupParts_use_model_mesh_index(List<Part> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                return null;
            }
            List<int> vIndex = new List<int>();
            List<double> vPos = new List<double>();
            List<int> fIndex = new List<int>();
            foreach (Part part in parts)
            {
                vIndex.AddRange(part._VERTEXINDEX.ToList());
                vPos.AddRange(part._MESH.VertexPos.ToList());
                fIndex.AddRange(part._FACEVERTEXINDEX.ToList());
                _parts.Remove(part);
            }
            SamplePoints sp = groupSamplePoints(parts);
            Part newPart = new Part(_mesh, vIndex.ToArray(), vPos.ToArray(), fIndex.ToArray(), sp);
            newPart._partName = getPartGroupName();
            _parts.Add(newPart);
            return newPart;
        }// group parts

        public Part groupParts(List<Part> parts)
        {
            List<double> vPos = new List<double>();
            List<int> fIndex = new List<int>();
            // merge meshes
            int start = 0;
            foreach (Part part in parts)
            {
                Mesh m = part._MESH;
                vPos.AddRange(m.VertexPos.ToList());
                for (int i = 0, j = 0; i < m.FaceCount; ++i, j += 3)
                {
                    fIndex.Add(start + m.FaceVertexIndex[j]);
                    fIndex.Add(start + m.FaceVertexIndex[j + 1]);
                    fIndex.Add(start + m.FaceVertexIndex[j + 2]);
                }
                start += m.VertexCount;
                _parts.Remove(part);
            }
            // without sample points
            Mesh merged = new Mesh(vPos.ToArray(), fIndex.ToArray());
            Part newPart = new Part(merged);
            newPart._partName = getPartGroupName();
            _parts.Add(newPart);
            composeMesh(); // recompose mesh
            return newPart;
        }// groupParts

        private string getPartGroupName()
        {
            int n = 0;
            string group_name = "group_0";
            bool exisit = true;
            while (exisit)
            {
                exisit = false;
                foreach (Part part in _parts)
                {
                    if (part._partName != null && part._partName.Equals(group_name))
                    {
                        exisit = true;
                        break;
                    }
                }
                if (exisit)
                {
                    ++n;
                    group_name = "group_" + n.ToString();
                }
            }
            return group_name;
        }// getPartGroupName

        public string getPartName()
        {
            int n = 0;
            foreach (Part part in _parts)
            {
                if (part._partName != null && part._partName.StartsWith("part"))
                {
                    ++n;
                }
            }
            return "part_" + n.ToString();
        }// getPartName

        public string avoidRepeatPartName(int index)
        {
            int n = 0;
            string partName = _parts[index]._partName.ToLower();
            for (int i =0; i < index; ++i)
            {
                Part part = _parts[i];
                if (part._partName != null && part._partName.ToLower().StartsWith(partName))
                {
                    ++n;
                }
            }
            if (n > 0)
            {
                partName += "_" + n.ToString();
            }
            return partName;
        }// avoidRepeatPartName

        private SamplePoints groupSamplePoints(List<Part> parts)
        {
            int start = 0;
            List<Vector3d> samplePnts = new List<Vector3d>();
            List<Vector3d> samplePntsNormals = new List<Vector3d>();
            List<int> samplePntsFaceIdxs = new List<int>();
            List<Color> samplePntsColors = new List<Color>();
            List<PatchWeightPerCategory> mergedWeights = new List<PatchWeightPerCategory>();
            for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
            {
                mergedWeights.Add(new PatchWeightPerCategory(Functionality.getCategoryName(i)));
            }
            int totalSamplePoints = 0;
            foreach (Part part in parts)
            {
                if (part._partSP == null)
                {
                    continue;
                }

                if (part._partSP != null && part._partSP._points.Length != 0 && part._partSP._fidxMapSPid != null)
                {
                    samplePnts.AddRange(part._partSP._points);
                    samplePntsNormals.AddRange(part._partSP._normals);
                    samplePntsColors.AddRange(part._partSP._blendColors);
                    for (int f = 0; f < part._partSP._faceIdx.Length; ++f)
                    {
                        int fid = part._partSP._faceIdx[f];
                        samplePntsFaceIdxs.Add(start + fid);
                    }
                    totalSamplePoints += part._partSP._points.Length;
                }
                start += part._MESH.FaceCount;
            }
            if (totalSamplePoints > 0)
            {
                // merge points weights
                for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
                {
                    int iNumPatches = Functionality.getNumberOfFunctionalPatchesPerCategory((Functionality.Category)i);
                    mergedWeights[i]._weights = new double[totalSamplePoints, iNumPatches];
                    mergedWeights[i]._nPatches = iNumPatches;
                    mergedWeights[i]._nPoints = totalSamplePoints;
                    int nsp = 0;
                    foreach (Part part in parts)
                    {
                        if (part._partSP == null)
                        {
                            continue;
                        }
                        for (int p = 0; p < part._partSP._weightsPerCat[i]._nPoints; ++p)
                        {
                            for (int q = 0; q < part._partSP._weightsPerCat[i]._nPatches; ++q)
                            {
                                mergedWeights[i]._weights[nsp, q] = part._partSP._weightsPerCat[i]._weights[p, q];
                            }
                            ++nsp;
                        }
                    }
                }
            }
            SamplePoints sp = new SamplePoints(samplePnts.ToArray(), samplePntsNormals.ToArray(), 
                samplePntsFaceIdxs.ToArray(), samplePntsColors.ToArray(), start + 1);
            sp._weightsPerCat = mergedWeights;
            return sp;
        }// groupSamplePoints

        public void addAPart(Part p)
        {
            _parts.Add(p);
        }

        public void removeAPart(Part p)
        {
            _parts.Remove(p);
        }

        public void deleteParts(List<int> indices)
        {
            indices.Sort();
            // with parts together
            for (int i = indices.Count - 1; i >= 0; --i)
            {
                _parts.RemoveAt(indices[i]);
            }
        }// deleteParts

        private int fitOption(int idx)
        {
            int option = 0;

            if (_GRAPH != null && _GRAPH._functionalityValues != null)
            {
                if (_GRAPH._functionalityValues._parentCategories.Contains(Functionality.Category.Handcart) ||
                    _GRAPH._functionalityValues._parentCategories.Contains(Functionality.Category.Basket))
                {
                    if ((_funcSpaces.Length == 3 && idx == 1) || (_funcSpaces.Length == 2 && idx == 0))
                    {
                        option = 1;
                    }
                }
            }
            return option;
        }// fitOption

        private int[] indexOfFunctionalSpaceConsidered()
        {
            if (_funcSpaces == null)
            {
                return null;
            }
            int n = _funcSpaces.Length;
            bool before = n == 2;
            if (_GRAPH != null && _GRAPH._functionalityValues != null)
            {
                if (!_GRAPH._functionalityValues._parentCategories.Contains(Functionality.Category.Basket))
                {
                    n--;
                    before = false;
                }
            }
            int[] ids = new int[n];
            if (before)
            {
                for (int i = 0; i < n; ++i)
                {
                    ids[i] = i;
                }
            }
            else
            {
                for (int i = 0; i < n; ++i)
                {
                    ids[i] = i + 1;
                }
            }
            return ids;
        }// indexOfFunctionalSpaceConsidered

        public void analyzeFunctionalSpace()
        {
            int[] fsIds = indexOfFunctionalSpaceConsidered();
            if (fsIds == null)
            {
                return;
            }
            List<List<double>> weights = new List<List<double>>();
            for (int i = 0; i < fsIds.Length; ++i)
            {
                List<double> iweights = new List<double>();
                weights.Add(iweights);
            }
            int catId = (int)_GRAPH._functionalityValues._parentCategories[0];
            foreach (Node node in _GRAPH._NODES)
            {
                SamplePoints sp = node._PART._partSP;
                if (sp == null)
                {
                    continue;
                }
                for (int i = 0; i < fsIds.Length; ++i)
                {
                    int fsId = fsIds[i];
                    for (int j = 0; j < sp._weightsPerCat[catId]._nPoints; ++j)
                    {
                        weights[i].Add(sp._weightsPerCat[catId]._weights[j, fsId]);
                    }
                }
            }
            double ratio = 0.7;
            double[] threshes = new double[fsIds.Length];
            for (int i = 0; i < fsIds.Length; ++i)
            {
                weights[i].Sort();
                int count = weights[i].Count;
                int index = (int)(ratio * count);
                threshes[i] = weights[i][index];
            }
            // do not handle the last - ground functional space      
            
            for (int i = 0; i < fsIds.Length; ++i)
            {
                // 1. separate to several meshes
                double[] fs_weights = _funcSpaces[fsIds[i]]._weights.Clone() as double[];
                Array.Sort(fs_weights);
                double thr = fs_weights[(int)(0.9 * fs_weights.Length)];
                List<List<Vector3d>> pointClouds = findIndependentPointClouds(_funcSpaces[fsIds[i]]._mesh, fs_weights, thr);
                //if (i == 0)
                //{
                //    this.pointsTest = pointClouds;
                //}
                // 2. fit proxy
                List<Prism> primitives = new List<Prism>();
                int option = fitOption(fsIds[i]);
                for (int j = 0; j < pointClouds.Count; ++j)
                {
                    Prism prism = Part.FitProxy(option, pointClouds[j].ToArray());
                    primitives.Add(prism);
                }
                // 3. associate with parts
                // 3.1 sort parts by #important points - high weights
                List<Part> parts = new List<Part>();
                List<int> numOfSalientPoints = new List<int>();
                foreach (Part part in _parts)
                {
                    if (part._partSP == null) {
                        continue;
                    }
                    int npoints = part._partSP._points.Length;
                    int nSalientPoints = 0;
                    for (int t = 0; t < npoints; ++t)
                    {
                        if (part._partSP._weightsPerCat[catId]._weights[t, i] > threshes[i])
                        {
                            ++nSalientPoints;
                        }
                    }
                    int insertId = 0;
                    for (int t = 0; t < numOfSalientPoints.Count; ++t)
                    {
                        if (nSalientPoints < numOfSalientPoints[t])
                        {
                            insertId = t + 1;
                            break;
                        }
                    }
                    parts.Insert(insertId, part);
                    numOfSalientPoints.Insert(insertId, nSalientPoints);
                }
                // 3.2 select parts
                for (int j = 0; j < primitives.Count; ++j)
                {
                    Part selected = null;
                    // distance of the point to the plane
                    double mindist_part = double.MaxValue;
                    bool found = false;
                    for (int t = 0; t < parts.Count; ++t)
                    {
                        Mesh tmesh = parts[t]._MESH;
                        double mindist = double.MaxValue;
                        for (int np = 0; np < tmesh.VertexCount; ++np)
                        {
                            Vector3d v = tmesh.getVertexPos(np);
                            foreach (Polygon3D plane in primitives[j]._PLANES)
                            {
                                double d = Common.PointDistToPlane(v, plane.center, plane.normal);
                                if (d < mindist)
                                {
                                    mindist = d;
                                }
                                if (mindist < Common._thresh)
                                {
                                    selected = parts[t]; // the first part (already high rank) that is closeset to the primitive
                                    found = true;
                                    break;
                                }
                            }
                            if (found)
                            {
                                break;
                            }
                        }
                        if (found)
                        {
                            break;
                        }
                        if (mindist < mindist_part)
                        {
                            mindist_part = mindist;
                            selected = parts[t];
                        }
                    }// each part
                    selected._functionalSpacePrims.Add(primitives[j]);
                }// for each primitive
            }
        }// analyzeFunctionalSpace

        private List<Mesh> findIndependentMeshes(Mesh mesh)
        {
            List<Mesh> meshses = new List<Mesh>();
            int n = mesh.VertexCount;
            bool[] visited = new bool[n];
            for (int k = 0; k < n; ++k)
            {
                if (visited[k]) continue;
                Queue<int> q = new Queue<int>();
                List<int> vIndex = new List<int>();
                q.Enqueue(k);
                visited[k] = true;
                // region growing from i
                while (q.Count > 0)
                {
                    int cur = q.Dequeue();
                    vIndex.Add(cur);
                    int[] curRow = mesh._VV[cur];
                    foreach (int j in curRow)
                    {
                        if (visited[j]) continue;
                        q.Enqueue(j);
                        visited[j] = true;
                    }
                }
                if (vIndex.Count < 10)
                {
                    continue;
                }
                // vertex position, face
                Dictionary<int, int> vertexIdMap = new Dictionary<int, int>();
                double[] vPos = new double[vIndex.Count * 3];
                for (int i = 0, j = 0; i < vIndex.Count; ++i)
                {
                    vertexIdMap.Add(vIndex[i], i);
                    int idx = vIndex[i] * 3;
                    vPos[j++] = mesh.VertexPos[idx++];
                    vPos[j++] = mesh.VertexPos[idx++];
                    vPos[j++] = mesh.VertexPos[idx++];
                }
                // collect face indices belong to this part, avoiding repeat face id
                HashSet<int> fIndex = new HashSet<int>();
                List<int> faceVertexIndex = new List<int>();
                foreach (int i in vIndex)
                {
                    foreach (int f in mesh._VF[i])
                    {
                        if (!fIndex.Contains(f))
                        {
                            int[] vertexIds = new int[3];
                            for (int t = 0; t < 3; ++t)
                            {
                                int vid = mesh.FaceVertexIndex[f + t];
                                int newVid = -1;
                                vertexIdMap.TryGetValue(vid, out newVid);
                                vertexIds[t] = newVid;
                                if (newVid == -1)
                                {
                                    vertexIds = null;
                                    break;
                                }
                            }
                            if (vertexIds != null)
                            {
                                for (int t = 0; t < 3; ++t)
                                {
                                    faceVertexIndex.Add(vertexIds[t]);
                                }
                            }
                            fIndex.Add(f);
                        }
                    }
                }// faces
                Mesh m = new Mesh(vPos, faceVertexIndex.ToArray());
                meshses.Add(m);
            }// each 
            return meshses;
        }// findIndependentMeshes

        private List<List<Vector3d>> findIndependentPointClouds(Mesh mesh, double[] weights, double thr)
        {
            List<List<Vector3d>> res = new List<List<Vector3d>>();
            int n = mesh.VertexCount;
            bool[] visited = new bool[n];
            for (int k = 0; k < n; ++k)
            {
                if (visited[k]) continue;
                Queue<int> q = new Queue<int>();
                List<int> vIndex = new List<int>();
                q.Enqueue(k);
                visited[k] = true;
                // region growing from i
                while (q.Count > 0)
                {
                    int cur = q.Dequeue();
                    vIndex.Add(cur);
                    int[] curRow = mesh._VV[cur];
                    foreach (int j in curRow)
                    {
                        if (visited[j]) continue;
                        q.Enqueue(j);
                        visited[j] = true;
                    }
                }
                if (vIndex.Count < 10)
                {
                    continue;
                }
                // vertex position, face
                Dictionary<int, int> vertexIdMap = new Dictionary<int, int>();
                double[] vPos = new double[vIndex.Count * 3];
                
                for (int i = 0, j = 0; i < vIndex.Count; ++i)
                {
                    vertexIdMap.Add(vIndex[i], i);
                    int idx = vIndex[i] * 3;
                    vPos[j++] = mesh.VertexPos[idx++];
                    vPos[j++] = mesh.VertexPos[idx++];
                    vPos[j++] = mesh.VertexPos[idx++];
                }
                // collect face indices belong to this part, avoiding repeat face id
                HashSet<int> fIndex = new HashSet<int>();
                List<int> faceVertexIndex = new List<int>();
                List<Vector3d> points = new List<Vector3d>();
                foreach (int i in vIndex)
                {
                    foreach (int f in mesh._VF[i])
                    {
                        if (weights[f] < thr)
                        {
                            continue;
                        }
                        if (!fIndex.Contains(f))
                        {
                            // add vertex and break
                            Vector3d v = mesh.getVertexPos(i);
                            points.Add(v);
                            // offset
                            Vector3d nor = mesh.getFaceNormal(f);
                            Vector3d voff = v - nor * 0.05;
                            points.Add(voff);
                            fIndex.Add(f);
                            break;
                        }
                    }
                }// faces
                res.Add(points);
            }// each 
            return res;
        }// findIndependentMeshes
        // Global get
        public int _NPARTS
        {
            get
            {
                return _parts == null ? 0 : _parts.Count;
            }
        }

        public List<Part> _PARTS
        {
            get
            {
                return _parts;
            }
        }

        public Mesh _MESH
        {
            get
            {
                return _mesh;
            }
            set
            {
                _mesh = value;
            }
        }
    }// Model

    public class Joint
    {
        Vector3d _point;
        Part _part1;
        Part _part2;
        public Joint() { }

        public Joint(Part p1, Part p2, Vector3d v)
        {
            _part1 = p1;
            _part2 = p2;
            _point = v;
        }
    }// Joint

    public class PartFormation
    {
        List<int> _partIdxs;
        double _rate = 0; // valid: 1, invalid: 0, user select: 2?

        public PartFormation(List<int> partIdxs, double rate)
        {
            _partIdxs = partIdxs;
            _rate = rate;
        }

        public List<int> getPartIdxs()
        {
            return _partIdxs;
        }

        public double _RATE
        {
            get
            {
                return _rate;
            }
            set
            {
                _rate = value;
            }
        }
    }// PartFormation

    public class FunctionalityModel
    {
        int _dim = 0;
        double[] _features;
        string _category;

        public FunctionalityModel(double[] fs, string name)
        {
            _dim = fs.Length;
            _features = fs;
            _category = name;
        }

        public int _DIM
        {
            get
            {
                return _dim;
            }
        }

        public string _CATEGORY
        {
            get
            {
                return _category;
            }
        }

        public double[] _FEATURES
        {
            get
            {
                return _features;
            }
        }
    }// FunctionalityModel

    public class SamplePoints
    {
        public Vector3d[] _points_orgin;
        public Vector3d[] _points;
        public Vector3d[] _normals;
        public int[] _faceIdx; // assoicated with the mesh in a model or part
        public double[,] _weights; // w.r.t. npatches
        public Color[,] _colors;
        public Color[] _blendColors;
        public Dictionary<int, List<int>> _fidxMapSPid;
        private int _totalNfaces = 0;
        public List<PatchWeightPerCategory> _weightsPerCat = null;

        public List<Vector3d> testVisiblePoints = new List<Vector3d>();
        public List<Vector3d> testNormals = new List<Vector3d>();

        // Test
        public Color[][] _highlightedColors;

        public SamplePoints() { }

        public SamplePoints(Vector3d[] points, Vector3d[] normals, int[] faceIdxs,
            Color[] blendColors, int totalNFaces)
        {
            _points = points;
            _points_orgin = points.Clone() as Vector3d[];
            _normals = normals;
            _faceIdx = faceIdxs;
            _totalNfaces = totalNFaces;
            _blendColors = blendColors;
            if (_blendColors == null)
            {
                _blendColors = new Color[_points.Length];
            }
            buildFaceSamplePointsMap(totalNFaces);
        }

        private void buildFaceSamplePointsMap(int totalFaces)
        {
            // a face can have multiple sample points, or no sample point
            _fidxMapSPid = new Dictionary<int,List<int>>();
            for (int i = 0; i < _faceIdx.Length; ++i)
            {
                if (!_fidxMapSPid.ContainsKey(_faceIdx[i]))
                {
                    _fidxMapSPid.Add(_faceIdx[i], new List<int>());
                }
                _fidxMapSPid[_faceIdx[i]].Add(i); // the map between face idx and sample points
            }
        }

        public void reBuildFaceSamplePointsMap(List<Vector3d> points, List<Vector3d> normals, List<int> faceIdxs)
        {
            _points = points.ToArray();
            _points_orgin = _points.Clone() as Vector3d[];
            _normals = normals.ToArray();
            _faceIdx = faceIdxs.ToArray();
            _blendColors = null;
            _fidxMapSPid = new Dictionary<int, List<int>>();
            for (int i = 0; i < _faceIdx.Length; ++i)
            {
                if (!_fidxMapSPid.ContainsKey(_faceIdx[i]))
                {
                    _fidxMapSPid.Add(_faceIdx[i], new List<int>());
                }
                _fidxMapSPid[_faceIdx[i]].Add(i); // the map between face idx and sample points
            }
        }// reBuildFaceSamplePointsMap

        public void Transform(Matrix4d T)
        {
            for(int i =0; i < _points.Length; ++i)
            {
                Vector3d v = new Vector3d(_points[i]);
                _points[i] = Common.transformVector(v, T);
            }
        }

        public void TransformFromOrigin(Matrix4d T)
        {
            for (int i = 0; i < _points.Length; ++i)
            {
                Vector3d v = new Vector3d(_points_orgin[i]);
                _points[i] = Common.transformVector(v, T);
            }
        }

        public void updateOrigin()
        {
            this._points_orgin = _points.Clone() as Vector3d[];
        }

        public void updateNormals(Mesh mesh)
        {
            if (_points == null || _faceIdx == null || mesh == null)
            {
                return;
            }
            int n = _points.Length;
            if (_normals == null)
            {
                _normals = new Vector3d[n];
            }
            for (int i = 0; i < n; ++i)
            {
                int fid = _faceIdx[i];
                _normals[i] = mesh.getFaceNormal(fid);
                //Vector3d A = mesh.getVertexPos(mesh.FaceVertexIndex[fid * 3]);
                //Vector3d B = mesh.getVertexPos(mesh.FaceVertexIndex[fid * 3 + 1]);
                //Vector3d C = mesh.getVertexPos(mesh.FaceVertexIndex[fid * 3 + 2]);
                ////_normals[i] = Common.getBarycentricCoord(A, B, C, _points[i]).normalize();
                //Vector3d p = _points[i];
                //Vector3d nor1 = (p - A).normalize().Cross((B - A).normalize());
                //Vector3d nor2 = (p - B).normalize().Cross((C - B).normalize());
                //Vector3d nor3 = (p - C).normalize().Cross((A - C).normalize());
                //_normals[i] = (nor1 + nor2 + nor3).normalize();
            }
        }// updateNormals

        public Object clone()
        {
            Vector3d[] cpoints = _points.Clone() as Vector3d[];
            Vector3d[] cnormals = _normals.Clone() as Vector3d[];
            int[] cfaceIdx = _faceIdx.Clone() as int[];
            Color[] ccolors = null;
            if (_blendColors != null)
            {
                ccolors = _blendColors.Clone() as Color[];
            }
            SamplePoints sp = new SamplePoints(cpoints, cnormals, cfaceIdx, ccolors, _totalNfaces);
            sp._weightsPerCat = new List<PatchWeightPerCategory>();
            foreach (PatchWeightPerCategory pw in this._weightsPerCat)
            {
                sp._weightsPerCat.Add(pw.Clone() as PatchWeightPerCategory);
            }
            return sp;
        }

        public void setWeigthsPerCat(List<PatchWeightPerCategory> weights)
        {
            _weightsPerCat = weights;
            foreach (PatchWeightPerCategory pw in weights)
            {
                pw._nPoints = pw._weights.GetLength(0);
                pw._nPatches = pw._weights.GetLength(1);
            }
        }// setWeigthsPerCat
    }// SamplePoint

    public class PatchWeightPerCategory
    {
        public string _catName;
        public double[,] _weights;
        public int _nPoints = 0;
        public int _nPatches = 0;
        public PatchWeightPerCategory() { }

        public PatchWeightPerCategory(string name) {
            _catName = name;
        }

        public PatchWeightPerCategory(string name, double[,] weights)
        {
            _catName = name;
            _weights = weights;
            _nPoints = weights.GetLength(0);
            _nPatches = weights.GetLength(1);
        }

        public Object Clone()
        {
            PatchWeightPerCategory pw = new PatchWeightPerCategory(_catName.Clone() as string);
            pw._weights = _weights.Clone() as double[,];
            pw._nPatches = _nPatches;
            pw._nPoints = _nPoints;
            return pw;
        }
    }// PatchWeightPerCategory

    public class FunctionalSpace
    {
        public Mesh _mesh;
        public double[] _weights; // w.r.t. faces

        public FunctionalSpace(Mesh mesh, double[] weights)
        {
            _mesh = mesh;
            _weights = weights;
        }

        public Object clone()
        {
            Mesh cmesh = _mesh.Clone() as Mesh;
            double[] cweights = _weights.Clone() as double[];
            FunctionalSpace fs = new FunctionalSpace(cmesh, cweights);
            return fs;
        }
    }// FunctionalSpace

    public class PartCombination
    {
        List<int> _nodes = new List<int>();
        BigInteger _hash;
        double[] _scores = new double[Functionality._NUM_CATEGORIY];

        public PartCombination(List<int> nodes, BigInteger hash, double[] scores)
        {
            _nodes = nodes;
            _hash = hash;
            _scores = scores;
        }

        public PartCombination(List<int> nodes)
        {
            _nodes = new List<int>(nodes);
            _hash = 0;
            foreach (int i in _nodes)
            {
                _hash += BigInteger.Pow(2, i);
            }
            for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
            {
                _scores[i] = 0;
            }
        }

        public void Add(int node)
        {
            _nodes.Add(node);
            _hash += BigInteger.Pow(2, node);
        }

        public Object Clone()
        {
            List<int> nodes = new List<int>();
            foreach (int node in _nodes)
            {
                nodes.Add(node);
            }
            BigInteger hash = _hash;
            double[] score = _scores.Clone() as double[];
            PartCombination partCombination = new PartCombination(nodes, hash, score);
            return partCombination;
        }

        public List<int> _NODES
        {
            get
            {
                return _nodes;
            }
        }

        public BigInteger _HASH
        {
            get
            {
                return _hash;
            }
        }

        public double[] _SCORES
        {
            get
            {
                return _scores;
            }
            set
            {
                _scores = value;
            }
        }
    }// PartCombination

    public class PartGroup
    {
        int index = -1;
        int _parentShapeIdx = -1;
        List<Node> _nodes = new List<Node>();
        public double[] _featureVector = new double[Functionality._TOTAL_FUNCTONAL_PATCHES];
        public int _gen = 0;
        public bool _isSymmBreak = false;
        public List<List<Node>> _clusters = new List<List<Node>>();

        public List<PartGroup> _compatiblePGs;
        public int pgSet = -1; // a set of part groups with similar geometry

        public PartGroup(List<Node> nodes, int g)
        {
            _nodes = new List<Node>(nodes);
            _gen = g;
            _compatiblePGs = new List<PartGroup>();
            this.seekCluster();
        }

        public PartGroup(List<Node> nodes, double[] featureVectors)
        {
            _nodes = new List<Node>(nodes);
            _featureVector = featureVectors;
            _compatiblePGs = new List<PartGroup>();
            this.seekCluster();
        }

        private void seekCluster()
        {
            // determine the node cluster (connected nodes), viewed as a node tree
            if (_nodes.Count == 0)
            {
                return;
            }
            bool[] added = new bool[_nodes.Count];
            _clusters = new List<List<Node>>();
            for (int i = 0; i < _nodes.Count; ++i)
            {
                if (added[i])
                {
                    continue;
                }
                // search from node i
                List<Node> igroup = new List<Node>();
                Queue<Node> q = new Queue<Node>();
                q.Enqueue(_nodes[i]);
                added[i] = true;
                while (q.Count > 0)
                {
                    Node qn = q.Dequeue();
                    igroup.Add(qn);
                    foreach (Node adj in qn._adjNodes)
                    {
                        int id = _nodes.IndexOf(adj);
                        if (id != -1 && !added[id])
                        {                            
                            q.Enqueue(adj);
                            added[id] = true;
                        }
                    }
                }
                _clusters.Add(igroup);
            }
        }// seekCluster

        public bool containsMainFuncPart()
        {
            foreach(Node node in _nodes)
            {
                foreach (Functionality.Functions f in node._funcs)
                {
                    if (Functionality.IsMainFunction(f))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void computeFeatureVector(List<double> thresholds)
        {
            if (_nodes.Count == 0)
            {
                return;
            }
            int ndim = Functionality._TOTAL_FUNCTONAL_PATCHES;
            double[] means = new double[ndim];
            double[] stds = new double[ndim];
            double[] sums = new double[ndim];
            int[] npoints = new int[ndim];
            foreach (Node node in _nodes)
            {
                // accummulate the weight fields
                SamplePoints sp = node._PART._partSP;
                int d = 0;
                for (int c = 0; c < Functionality._NUM_CATEGORIY; ++c)
                {
                    if (sp == null || sp._weightsPerCat == null || sp._weightsPerCat.Count == 0)
                    {
                        return;
                    }
                    for (int i = 0; i < sp._weightsPerCat[c]._nPatches; ++i)
                    {
                        for (int j = 0; j < sp._weightsPerCat[c]._nPoints; ++j)
                        {
                            double w = sp._weightsPerCat[c]._weights[j, i];
                            if (thresholds == null || w > thresholds[d + i])
                            {
                                sums[d + i] += w;
                                ++npoints[d + i];
                            }
                        }
                    }
                    d += sp._weightsPerCat[c]._nPatches;
                }
            }
            for (int i = 0; i < ndim; ++i)
            {
                if (npoints[i] == 0)
                {
                    means[i] = 0;
                }
                else
                {
                    means[i] = sums[i] / npoints[i];
                }
            }
            foreach (Node node in _nodes)
            {
                SamplePoints sp = node._PART._partSP;
                int d = 0;
                for (int c = 0; c < Functionality._NUM_CATEGORIY; ++c)
                {
                    for (int i = 0; i < sp._weightsPerCat[c]._nPatches; ++i)
                    {
                        for (int j = 0; j < sp._weightsPerCat[c]._nPoints; ++j)
                        {
                            double w = sp._weightsPerCat[c]._weights[j, i];
                            if (thresholds == null || w > thresholds[d + i])
                            {
                                double std = w - means[d + i];
                                std *= std;
                                stds[d + i] += std;
                            }
                        }
                    }
                    d += sp._weightsPerCat[c]._nPatches;
                }
            }
            for (int i = 0; i < ndim; ++i)
            {
                if (npoints[i] == 0)
                {
                    stds[i] = 0;
                }
                else
                {
                    stds[i] /= npoints[i];
                }
            }
            // TEST
            _featureVector = means;
            //_featureVector = stds;
            //_featureVector = sums;
        }// computeFeatureVector

        public int _INDEX
        {
            get
            {
                return index;
            }
            set
            {
                index = value;
            }
        }
        public List<Node> _NODES
        {
            get
            {
                return _nodes;
            }
        }

        public int _ParentModelIndex
        {
            get
            {
                return _parentShapeIdx;
            }
            set
            {
                _parentShapeIdx = value;
            }
        }
    }// PartGroup

    public class PartGroupPair
    {
        public int _p1;
        public int _p2;
        public double _score = 1.0;

        public PartGroupPair(int i, int j, double val)
        {
            _p1 = i;
            _p2 = j;
            _score = val;
        }
    }// PartGroupPair

    public class NodeFunctionalSpaceAgent
    {
        public string _nodeName = "";
        public Prism _functionalSpaceAgent = null;
        public NodeFunctionalSpaceAgent(string name, Prism prism)
        {
            _nodeName = name.ToLower();
            _functionalSpaceAgent = prism;
        }
    }// NodeFunctionalSpaceAgent

    public class TrainedFeaturePerCategory
    {
        public Functionality.Category _cat;
        public int _nPatches = 0;
        public int _npairs = 0;
        public List<double[,]> _unaryF;
        public List<double[,]> _binaryF;
        public double[] weights;

        public TrainedFeaturePerCategory(Functionality.Category c)
        {
            _cat = c;
            _nPatches = Functionality.getNumberOfFunctionalPatchesPerCategory(c);
            _npairs = _nPatches * (_nPatches + 1) / 2;
            _unaryF = new List<double[,]>();
            _binaryF = new List<double[,]>();
        }
    }// TrainedFeaturePerCategory
}
