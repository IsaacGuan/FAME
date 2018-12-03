using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;

using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Diagnostics;

using Tao.OpenGl;
using Tao.Platform.Windows;

using Geometry;
using Component;

using System.Runtime.Serialization.Json;
using System.Web.Script.Serialization;
using Accord.Statistics.Distributions.Univariate;

namespace FameBase
{
    public class GLViewer : SimpleOpenGlControl
    {
        /******************** Initialization ********************/
        public GLViewer()
        {
            this.InitializeComponent();
            this.InitializeContexts();

            this.initScene();

            this.initMatlab();
        }

        public void Init()
        {
            this.initializeVariables();
            
            //// glsl shaders
            //this.shader = new Shader(
            //    @"shaders\vertexshader.glsl",
            //    @"shaders\fragmentshader.glsl");
            //this.shader.Link();

            //this.LoadTextures();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Name = "GlViewer";

            this.ResumeLayout(false);
        }

        private void initializeVariables()
        {
            this._meshClasses = new List<MeshClass>();
            this._currModelTransformMatrix = Matrix4d.IdentityMatrix();
            this.arcBall = new ArcBall(this.Width, this.Height);
            this.camera = new Camera();

            _axes = new Vector3d[18] { new Vector3d(-1.2, 0, 0), new Vector3d(1.2, 0, 0),
                                      new Vector3d(1, 0.2, 0), new Vector3d(1.2, 0, 0), 
                                      new Vector3d(1, -0.2, 0), new Vector3d(1.2, 0, 0), 
                                      new Vector3d(0, -1.2, 0), new Vector3d(0, 1.2, 0), 
                                      new Vector3d(-0.2, 1, 0), new Vector3d(0, 1.2, 0),
                                      new Vector3d(0.2, 1, 0), new Vector3d(0, 1.2, 0),
                                      new Vector3d(0, 0, -1.2), new Vector3d(0, 0, 1.2),
                                      new Vector3d(-0.2, 0, 1), new Vector3d(0, 0, 1.2),
                                      new Vector3d(0.2, 0, 1), new Vector3d(0, 0, 1.2)};
            this.startWid = this.Width;
            this.startHeig = this.Height;
            initializeGround();
        }

        private void initializeGround()
        {
            Vector3d[] groundPoints = new Vector3d[4] {
                new Vector3d(-1, 0, -1), new Vector3d(-1, 0, 1),
                new Vector3d(1, 0, 1), new Vector3d(1, 0, -1)};
            _groundPlane = new Polygon3D(groundPoints);
            int n = 10;
            _groundGrids = new Vector3d[(n + 1) * 2 * 2];
            Vector3d xf = groundPoints[0];
            Vector3d xt = groundPoints[1];
            Vector3d yf = groundPoints[0];
            Vector3d yt = groundPoints[3];
            double xstep = (xt - xf).Length() / n;
            Vector3d xdir = (xt - xf).normalize();
            int k = 0;
            for (int i = 0; i <= n; ++i)
            {
                _groundGrids[k++] = xf + xdir * xstep * i;
                _groundGrids[k++] = yt + xdir * xstep * i;
            }
            double ystep = (yt - yf).Length() / n;
            Vector3d ydir = (yt - yf).normalize();
            for (int i = 0; i <= n; ++i)
            {
                _groundGrids[k++] = xf + ydir * ystep * i;
                _groundGrids[k++] = xt + ydir * ystep * i;
            }
        }

        // modes
        public enum UIMode
        {
            // !Do not change the order of the modes --- used in the current program to retrieve the index (Integer)
            Viewing, VertexSelection, EdgeSelection, FaceSelection, BoxSelection, BodyNodeEdit,
            Translate, Scale, Rotate, Contact, PartPick, NONE
        }

        private bool drawVertex = false;
        private bool drawEdge = false;
        private bool isDrawMesh = true;
        private bool isDrawBbox = true;
        private bool isDrawGraph = true;
        private bool isDrawAxes = false;
        private bool isDrawQuad = false;
        private bool isDrawFuncSpace = false;
        public bool isDrawModelSamplePoints = false;
        public bool isDrawPartSamplePoints = false;
        public bool needReSample = false;
        public bool isDrawPartFunctionalSpacePrimitive = false;
        public bool isDrawFunctionalSpaceAgent = false;

        public bool enableDepthTest = true;
        public bool showVanishingLines = true;
        public bool lockView = false;
        public bool showFaceToDraw = true;

        public bool showSharpEdge = false;
        public bool enableHiddencheck = true;
        public bool condition = true;

        private static Vector3d eyePosition3D = new Vector3d(0, 0.5, 1.5);
        private static Vector3d eyePosition2D = new Vector3d(0, 1, 1.5);
        Vector3d eye = new Vector3d(0, 0.5, 1.5);
        private float[] _material = { 0.62f, 0.74f, 0.85f, 1.0f };
        private float[] _ambient = { 0.2f, 0.2f, 0.2f, 1.0f };
        private float[] _diffuse = { 1.0f, 1.0f, 1.0f, 1.0f };
        private float[] _specular = { 1.0f, 1.0f, 1.0f, 1.0f };
        private float[] _position = { 1.0f, 1.0f, 1.0f, 0.0f };

        /******************** Variables ********************/
        private UIMode currUIMode = UIMode.Viewing;
        private Matrix4d _currModelTransformMatrix = Matrix4d.IdentityMatrix();
        private Matrix4d _modelTransformMatrix = Matrix4d.IdentityMatrix();
        private Matrix4d _fixedModelView = Matrix4d.IdentityMatrix();
        private Matrix4d scaleMat = Matrix4d.IdentityMatrix();
        private Matrix4d transMat = Matrix4d.IdentityMatrix();
        private Matrix4d rotMat = Matrix4d.IdentityMatrix();
        private ArcBall arcBall = new ArcBall();
        private Vector2d mouseDownPos;
        private Vector2d prevMousePos;
        private Vector2d currMousePos;
        private bool isMouseDown = false;
        private List<MeshClass> _meshClasses = new List<MeshClass>();
        private MeshClass currMeshClass;
        private Quad2d highlightQuad;
        private Camera camera;
        private Shader shader;
        public static uint pencilTextureId, crayonTextureId, inkTextureId, waterColorTextureId, charcoalTextureId, brushTextureId;

        private List<Model> _crossOverBasket = new List<Model>();
        private int _selectedModelIndex = -1;

        public string foldername = "";
        private Vector3d objectCenter = new Vector3d();

        private enum Depthtype
        {
            opacity, hidden, OpenGLDepthTest, none, rayTracing // test 
        }

        public bool showVanishingRay1 = true;
        public bool showVanishingRay2 = true;
        public bool showVanishingPoints = true;
        public bool showBoxVanishingLine = true;
        public bool showGuideLineVanishingLine = true;
        private List<int> boxShowSequence = new List<int>();

        public bool zoonIn = false;
        private int meshIdx = 0;
        private int _fsIdx = 0;
        private int _pairPG1 = 0;
        private int _pairPG2 = 1;
        public int _nPairsPG = 0;
        private int _categoryId = -1;
        List<Part> _pgPairVisualization;
        private List<int> _inputSetCats;
        List<double> _inputSetThreshholds;
        Dictionary<string, Vector3d> _functionalPartScales = new Dictionary<string, Vector3d>();
        List<NodeFunctionalSpaceAgent> _nodeFSAs = new List<NodeFunctionalSpaceAgent>();

        /******************** Vars ********************/
        Model _currModel;
        List<Model> _ancesterModels = new List<Model>();
        List<Part> _selectedParts = new List<Part>();
        List<Node> _selectedNodes = new List<Node>();
        List<ModelViewer> _ancesterModelViewers = new List<ModelViewer>();
        HumanPose _currHumanPose;
        List<HumanPose> _humanposes = new List<HumanPose>();
        BodyNode _selectedNode;
        public bool _unitifyMesh = true;
        bool _showEditAxes = false;
        public bool isDrawGround = false;
        private Vector3d[] _axes;
        private Contact[] _editAxes;
        private Polygon3D _groundPlane;
        int _hightlightAxis = -1;
        ArcBall _editArcBall;
        bool _isRightClick = false;
        bool _isDrawTranslucentHumanPose = true;
        List<Node> _userSelectedNodes = new List<Node>();
        List<Part> _userSelectedParts = new List<Part>();

        private Vector3d[] _groundGrids;
        Edge _selectedEdge = null;
        Contact _selectedContact = null;
        private ReplaceablePair[,] _replaceablePairs = null;
        private int _currGenId = 0;
        private int _mutateOrCross = -1;
        private int _modelViewIndex = -1;
        private int _modelIndex = 0; // only serves as a model index for all selected models including inputs
        private bool _showContactPoint = false;

        List<ModelViewer> _partViewers = new List<ModelViewer>();
        List<ModelViewer> _currGenModelViewers = new List<ModelViewer>();
        List<Model> _userSelectedModels = new List<Model>();
        List<Model> _modelLibrary = new List<Model>();
        List<FunctionalityModel> _functionalityModels = new List<FunctionalityModel>();
        private bool _isPreRun = false;

        private SparseMatrix _validityMatrixPG;
        private Dictionary<int, List<int>> _curGenPGmemory = new Dictionary<int, List<int>>();
        private Dictionary<int, List<int>> _pairGroupMemory = new Dictionary<int, List<int>>();
        private Dictionary<int, Model> _modelIndexMap = new Dictionary<int, Model>();
        // part groups in the first generation are kept unchanged
        // they will be cloned in the evolution, so will be used for new crossover
        List<List<PartGroup>> _partGroups = new List<List<PartGroup>>();
        private int _numOfEmptyGroupUsed = 0;
        private int _maxUseEmptyGroup = 0;
        private int _nValidSymBreakUsed = 0;

        List<PartGroup> _partGroupLibrary;

        List<TrainedFeaturePerCategory> _trainingFeaturesPerCategory;

        // record the part combinations to avoid repetition
        Dictionary<string, int> partNameToInteger;
        List<PartFormation>[] partCombinationMemory;

        private MLApp.MLApp matlab = new MLApp.MLApp();

        /******************** Functions ********************/

        public UIMode CurrentUIMode
        {
            get
            {
                return this.currUIMode;
            }
            set
            {
                this.currUIMode = value;
            }
        }

        private void LoadTextures()					// load textures for canvas and brush
        {
            this.CreateTexture(@"data\pencil.png", out GLViewer.pencilTextureId);
            this.CreateTexture(@"data\crayon.png", out GLViewer.crayonTextureId);
            this.CreateTexture(@"data\ink.jpg", out GLViewer.inkTextureId);
            this.CreateTexture(@"data\watercolor.png", out GLViewer.waterColorTextureId);
            this.CreateTexture(@"data\charcoal.jpg", out GLViewer.charcoalTextureId);
            this.CreateGaussianTexture(32);
        }

        private void CreateTexture(string imagefile, out uint textureid)
        {
            Bitmap image = new Bitmap(imagefile);

            // to gl texture
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            //	image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            System.Drawing.Imaging.BitmapData bitmapdata = image.LockBits(rect,
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Gl.glGenTextures(1, out textureid);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, textureid);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_R, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, 4, image.Width, image.Height, 0, Gl.GL_BGRA,
                Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);
        }

        public byte[] CreateGaussianTexture(int size)
        {
            int w = size * 2, size2 = size * size;
            Bitmap bitmap = new Bitmap(w, w);
            byte[] alphas = new byte[w * w * 4];
            for (int i = 0; i < w; ++i)
            {
                int dx = i - size;
                for (int j = 0; j < w; ++j)
                {
                    int J = j * w + i;

                    int dy = j - size;
                    double dist2 = (dx * dx + dy * dy);

                    byte alpha = 0;
                    if (dist2 <= size2)	// -- not necessary actually, similar effects
                    {
                        // set gaussian values for the alphas
                        // modify the denominator to get different over-paiting effects
                        double gau_val = Math.Exp(-dist2 / (2 * size2 / 2)) / Math.E / 2;
                        alpha = Math.Min((byte)255, (byte)((gau_val) * 255));
                        //	alpha = 100; // Math.Min((byte)255, (byte)((gau_val) * 255));
                    }

                    byte beta = (byte)(255 - alpha);
                    alphas[J * 4] = (byte)(beta);
                    alphas[J * 4 + 1] = (byte)(beta);
                    alphas[J * 4 + 2] = (byte)(beta);
                    alphas[J * 4 + 3] = (byte)(alpha);

                    bitmap.SetPixel(i, j, System.Drawing.Color.FromArgb(alpha, beta, beta, beta));
                }
            }
            bitmap.Save(@"data\output.png");

            // create gl texture
            uint[] txtid = new uint[1];
            // -- create texture --
            Gl.glGenTextures(1, txtid);				// Create The Texture
            GLViewer.brushTextureId = txtid[0];

            // to gl texture
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            //	image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            System.Drawing.Imaging.BitmapData bitmapdata = bitmap.LockBits(rect,
                System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, txtid[0]);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_S, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_T, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_WRAP_R, Gl.GL_CLAMP_TO_EDGE);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, 4, bitmap.Width, bitmap.Height, 0, Gl.GL_RGBA,
                Gl.GL_UNSIGNED_BYTE, bitmapdata.Scan0);

            return alphas;
        }

        public void clearContext()
        {
            this.currMeshClass = null;
            _currModel = null;
            _ancesterModels.Clear();
            _ancesterModelViewers.Clear();
            _currHumanPose = null;
            this._meshClasses.Clear();
            _humanposes.Clear();
        }

        private void clearHighlights()
        {
            _selectedNode = null;
            _hightlightAxis = -1;
            _selectedEdge = null;
            _selectedContact = null;
            _selectedParts.Clear();
            _selectedNodes.Clear();
            _userSelectedNodes.Clear();
            _userSelectedParts.Clear();
            _selected_cat = Functionality.Category.None; // reset the current category
        }// clearHighlights

        /******************** Load & Save ********************/
        public void loadMesh(string filename)
        {
            this.clearContext();
            Mesh m = new Mesh(filename, _unitifyMesh);
            MeshClass mc = new MeshClass(m);
            this._meshClasses.Add(mc);
            this.currMeshClass = mc;
            _currModel = new Model(m);
            _ancesterModels = new List<Model>();
            _ancesterModels.Add(_currModel);
            Program.GetFormMain().outputSystemStatus("Mesh is unified and a segmented model is created.");
        }// loadMesh

        public void importOneMesh(string filename)
        {
            // if import multiple meshes, do not unify each mesh
            Mesh m = new Mesh(filename, false);
            MeshClass mc = new MeshClass(m);
            this._meshClasses.Add(mc);
            this.currMeshClass = mc;
            if (_currModel != null) // insert parts
            {
                _currModel.addAPart(new Part(m));
            }
            else
            {
                _currModel = new Model(m);
            }
        }// importOneMesh

        public void importMesh(string[] filenames, bool multiple)
        {
            // if import multiple meshes, do not unify each mesh
            if (filenames == null)
            {
                return;
            }
            if (filenames.Length == 1)
            {
                this.loadMesh(filenames[0]);
            }
            List<Part> parts = new List<Part>();
            foreach (string filename in filenames)
            {
                Mesh m = new Mesh(filename, false);
                Part part = new Part(m);
                parts.Add(part);
            }
            _currModel = new Model(parts);
        }// importMesh

        public string getStats()
        {
            if (_currModel == null)
            {
                return "";
            }
            Program.GetFormMain().writePostAnalysisInfo(this.getFunctionalityValuesString(_currModel, false));
            StringBuilder sb = new StringBuilder();

            sb.Append(_currModel._model_name + "\n");
            //if (_currModel._GRAPH != null && _currModel._GRAPH._functionalityValues != null &&_currModel._GRAPH._functionalityValues._parentCategories.Count > 0)
            //{
            //    string catStr = "";
            //    foreach (Functionality.Category cat in _currModel._GRAPH._functionalityValues._parentCategories)
            //    {
            //        catStr += cat + " ";
            //    }
            //    catStr += "\n";
            //    sb.Append(catStr);
            //}

            sb.Append("#part:   ");
            sb.Append(_currModel._NPARTS.ToString());

            if (_currModel._MESH != null)
            {
                sb.Append("\n#vertex:   ");
                sb.Append(_currModel._MESH.VertexCount.ToString());
                sb.Append("\n#edge:     ");
                sb.Append(_currModel._MESH.EdgeCount.ToString());
                sb.Append("\n#face:    ");
                sb.Append(_currModel._MESH.FaceCount.ToString());
            }
            sb.Append("\n#selected parts: ");
            sb.Append(_selectedParts.Count.ToString());
            sb.Append("\n#human poses: ");
            sb.Append(_humanposes.Count.ToString());
            if (_currModel._GRAPH != null)
            {
                sb.Append("\n#nodes: ");
                sb.Append(_currModel._GRAPH._NNodes.ToString());
                sb.Append("\n#edges: ");
                sb.Append(_currModel._GRAPH._NEdges.ToString());
            }
            if (_currModel._SP != null && _currModel._SP._points != null)
            {
                sb.Append("\n#sample pointss: " + _currModel._SP._points.Length);
            }
            sb.Append("\n#iter: " + _currGenId.ToString() + " ");
            string mc = _mutateOrCross == -1 ? "" : (_mutateOrCross == 0 ? "mutate" : (_mutateOrCross == 1 ? "crossover" : "growth"));
            sb.Append(mc);
            return sb.ToString();
        }// getStats

        public void loadTriMesh(string filename)
        {
            //MessageBox.Show("Trimesh is not activated in this version.");
            //return;

            this.clearContext();
            MeshClass mc = new MeshClass();
            this._meshClasses.Add(mc);
            this.currMeshClass = mc;
            this.Refresh();
        }// loadTriMesh

        public void saveObj(Mesh mesh, string filename, Color c)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }
            if (mesh == null)
            { 
                // save a mesh for the current model
                if (_currModel._NPARTS > 0)
                {
                    this.saveModelObj(_currModel, filename);
                    return;
                }
                mesh = _currModel._MESH;
            }
            string model_name = filename.Substring(filename.LastIndexOf('\\') + 1);
            model_name = model_name.Substring(0, model_name.LastIndexOf('.'));
            string mtl_name = filename.Substring(0, filename.LastIndexOf('.')) + ".mtl";
            using (StreamWriter sw = new StreamWriter(filename))
            {
                // vertex
                sw.WriteLine("usemtl " + model_name);
                string s = "";
                for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                {
                    s = "v";
                    s += " " + mesh.VertexPos[j++].ToString();
                    s += " " + mesh.VertexPos[j++].ToString();
                    s += " " + mesh.VertexPos[j++].ToString();
                    sw.WriteLine(s);
                }
                // face
                for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                {
                    s = "f";
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    sw.WriteLine(s);
                }
            }
            using (StreamWriter sw = new StreamWriter(mtl_name))
            {
                sw.WriteLine("newmtl " + model_name);
                sw.Write("Ka ");
                sw.WriteLine(colorToString(c, false));
                sw.Write("Kd ");
                sw.WriteLine(colorToString(c, false));
                sw.Write("Ks ");
                sw.WriteLine(colorToString(c, false));
                sw.Write("ke ");
                sw.WriteLine(colorToString(c, false));
            }
        }// saveObj

        public void saveFunctionalSpace(FunctionalSpace fs, string model_name, string foldername, int fid)
        {
            if (fs._mesh == null)
            {
                return;
            }
            string mesh_name = foldername + model_name + "_fs_" + fid.ToString() + ".obj";
            string w_name = foldername + model_name + "_fs_" + fid.ToString() + ".weight";
            using (StreamWriter sw = new StreamWriter(mesh_name))
            {
                // vertex
                string s = "";
                for (int i = 0, j = 0; i < fs._mesh.VertexCount; ++i)
                {
                    s = "v";
                    s += " " + fs._mesh.VertexPos[j++].ToString();
                    s += " " + fs._mesh.VertexPos[j++].ToString();
                    s += " " + fs._mesh.VertexPos[j++].ToString();
                    sw.WriteLine(s);
                }
                // face
                for (int i = 0, j = 0; i < fs._mesh.FaceCount; ++i)
                {
                    s = "f";
                    s += " " + (fs._mesh.FaceVertexIndex[j++] + 1).ToString();
                    s += " " + (fs._mesh.FaceVertexIndex[j++] + 1).ToString();
                    s += " " + (fs._mesh.FaceVertexIndex[j++] + 1).ToString();
                    sw.WriteLine(s);
                }
            }
            using (StreamWriter sw = new StreamWriter(w_name))
            {
                string s = "";
                for (int i = 0, j = 0; i < fs._mesh.FaceCount; ++i)
                {
                    s = fs._mesh.FaceColor[j++].ToString() + " ";
                    s += fs._mesh.FaceColor[j++].ToString() + " ";
                    s += fs._mesh.FaceColor[j++].ToString() + " ";
                    s += fs._mesh.FaceColor[j++].ToString() + " ";
                    s += fs._weights[i].ToString();
                    sw.WriteLine(s);
                }
            }
        }// saveFunctionalSpace

        public void saveModelOff(Model model, string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }
            Mesh mesh = model._MESH;
            if (mesh == null)
            {
                saveModelFromPartsOff(filename);
                return;
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine("OFF");
                sw.WriteLine(mesh.VertexCount.ToString() + " " + mesh.FaceCount.ToString() + " " + mesh.EdgeCount.ToString());
                // vertex
                string s = "";
                for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                {
                    s = mesh.VertexPos[j++].ToString() + " " 
                        + mesh.VertexPos[j++].ToString() + " " 
                        + mesh.VertexPos[j++].ToString();
                    sw.WriteLine(s);
                }
                // face
                for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                {
                    s = "3";
                    s += " " + (mesh.FaceVertexIndex[j++]).ToString();
                    s += " " + (mesh.FaceVertexIndex[j++] ).ToString();
                    s += " " + (mesh.FaceVertexIndex[j++]).ToString();
                    sw.WriteLine(s);
                }
            }
        }// saveOffFile


        public void saveModelMesh_StyleSimilarityUse(Model model, string filename)
        {
            Mesh mesh = model._MESH;
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine("OFF");
                sw.WriteLine(mesh.VertexCount.ToString() + " " + mesh.FaceCount.ToString() + " " + mesh.EdgeCount.ToString());
                // vertex
                string s = "";
                for (int i = 0, j = 0; i < mesh.VertexCount; ++i, j+= 3)
                {
                    s = mesh.VertexPos[j].ToString() + " "
                        + mesh.VertexPos[j + 1].ToString() + " "
                        + mesh.VertexPos[j + 2].ToString() + " ";
                    s += mesh.VertexNormal[j].ToString() + " "
                        + mesh.VertexNormal[j + 1].ToString() + " "
                        + mesh.VertexNormal[j + 2].ToString() + " ";
                    sw.WriteLine(s);
                }
                // face
                for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                {
                    s = "3";
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                    sw.WriteLine(s);
                }
            }
        }// saveOffFile

        private void saveModelFromPartsOff(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine("OFF");
                int vertexCount = 0;
                int faceCount = 0;
                foreach (Part p in _currModel._PARTS)
                {
                    Mesh mesh = p._MESH;
                    vertexCount += mesh.VertexCount;
                    faceCount += mesh.FaceCount;
                }
                sw.WriteLine(vertexCount.ToString() + " " + faceCount.ToString() + "  0");
                
                foreach (Part p in _currModel._PARTS)
                {
                    Mesh mesh = p._MESH;
                    // vertex
                    string s = "";
                    for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                    {
                        s = mesh.VertexPos[j++].ToString() + " "
                        + mesh.VertexPos[j++].ToString() + " "
                        + mesh.VertexPos[j++].ToString();
                        sw.WriteLine(s);
                    }
                }
                int start = 0;
                foreach (Part p in _currModel._PARTS)
                {
                    Mesh mesh = p._MESH;
                    // face
                    string s = "";
                    for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                    {
                        s = "3";
                        s += " " + (start + mesh.FaceVertexIndex[j++] + 1).ToString();
                        s += " " + (start + mesh.FaceVertexIndex[j++] + 1).ToString();
                        s += " " + (start + mesh.FaceVertexIndex[j++] + 1).ToString();
                        sw.WriteLine(s);
                    }
                    start += mesh.VertexCount;
                }
            }
        }// saveModelFromPartsOff

        private string colorToString(Color c, bool space)
        {
            double r = (double)c.R / 255.0;
            double g = (double)c.G / 255.0;
            double b = (double)c.B / 255.0;
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0:0.###}", r) + " ");
            sb.Append(string.Format("{0:0.###}", g) + " ");
            sb.Append(string.Format("{0:0.###}", b));
            if (space)
            {
                sb.Append(" ");
            }
            return sb.ToString();
        }// colorToString

        private void saveModelObj(Model model, string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                int start = 0;
                foreach (Part p in model._PARTS)
                {
                    Mesh mesh = p._MESH;

                    // vertex
                    string s = "";
                    for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                    {
                        s = "v";
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        sw.WriteLine(s);
                    }
                }
                foreach (Part p in model._PARTS)
                {
                    Mesh mesh = p._MESH;
                    // face
                    string s = "";
                    for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                    {
                        s = "f";
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        sw.WriteLine(s);
                    }
                    start += mesh.VertexCount;
                }
            }
        }// saveModelObj

        public void saveMergedObj(string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            if (this._meshClasses == null)
            {
                return;
            }

            using (StreamWriter sw = new StreamWriter(filename))
            {
                int start = 0;
                foreach (MeshClass mc in this._meshClasses)
                {
                    Mesh mesh = mc.Mesh;

                    // vertex
                    string s = "";
                    for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                    {
                        s = "v";
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        sw.WriteLine(s);
                    }
                    // face
                    for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                    {
                        s = "f";
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        sw.WriteLine(s);
                    }
                    start += mesh.VertexCount;
                }
            }
        }// saveMergedObj

        public void saveMeshForModel(Model model, string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            if (model == null || model._GRAPH == null)
            {
                return;
            }

            using (StreamWriter sw = new StreamWriter(filename))
            {
                int start = 0;
                foreach (Node node in model._GRAPH._NODES)
                {
                    Mesh mesh = node._PART._MESH;

                    // vertex
                    string s = "";
                    for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                    {
                        s = "v";
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        sw.WriteLine(s);
                    }
                    // face
                    for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                    {
                        s = "f";
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        sw.WriteLine(s);
                    }
                    start += mesh.VertexCount;
                }
            }
        }// saveMeshForModel

        public void saveMeshForModel(List<Node> nodes, string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }

            using (StreamWriter sw = new StreamWriter(filename))
            {
                int start = 0;
                foreach (Node node in nodes)
                {
                    Mesh mesh = node._PART._MESH;

                    // vertex
                    string s = "";
                    for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                    {
                        s = "v";
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        sw.WriteLine(s);
                    }
                    // face
                    for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                    {
                        s = "f";
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                        sw.WriteLine(s);
                    }
                    start += mesh.VertexCount;
                }
            }
        }// saveMeshForModel

        public void switchXYZ(int mode)
        {
            if (_currModel != null)
            {
                Common.switchXYZ_mesh(_currModel._MESH, mode);
                if (_currModel._SP != null)
                {
                    Common.switchXYZ_vectors(_currModel._SP._points, mode);
                    _currModel._SP.updateNormals(_currModel._MESH);
                }
                if (_currModel._funcSpaces != null)
                {
                    foreach (FunctionalSpace fs in _currModel._funcSpaces)
                    {
                        Common.switchXYZ_mesh(fs._mesh, mode);
                    }
                }
            }

            foreach (Part p in _selectedParts)
            {
                Common.switchXYZ_mesh(p._MESH, mode);
                if (p._partSP != null)
                {
                    Common.switchXYZ_vectors(p._partSP._points, mode);
                    p._partSP.updateNormals(_currModel._MESH);
                }
                p.fitProxy(-1);
                p.updateOriginPos();
            }
            this.Refresh();
        }// switchXYZ

        private Functionality.Category _selected_cat = Functionality.Category.None;
        private string model_filename = "";

        public void setCurrentModelCategoryAndSave(string cat)
        {
            _selected_cat = Functionality.getCategory(cat);
            _currModel._CAT = _selected_cat;
            this.saveAPartBasedModel(_currModel, model_filename, true);
        }// setCurrentModelCategoryAndSave

        public void saveTheCurrentModel(string filename, bool isOriginal)
        {
            // call from UI
            if (_currModel._CAT == Functionality.Category.None)
            {
                if (_selected_cat == Functionality.Category.None)
                {
                    model_filename = filename;
                    Program.GetFormMain().showCategorySelection();
                    return;
                }
                else
                {
                    _currModel._CAT = _selected_cat;
                }
            }
            this.saveAPartBasedModel(_currModel, filename, isOriginal);
        }// saveTheCurrentModel

        public void saveAPartBasedModel(Model model, string filename, bool isOriginalModel)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            if (model == null)
            {
                return;
            }
            // Data fromat:
            // n Parts
            // Part #i:
            // bbox vertices
            // mesh file loc
            // m edges (for graph)
            // id1, id2
            // ..., ...
            // comments start with "%"
            string meshDir = filename.Substring(0, filename.LastIndexOf('.')) + "\\";
            int loc = filename.LastIndexOf('\\');
            string modelName = filename.Substring(loc + 1, filename.LastIndexOf('.') - loc - 1);
            model._model_name = modelName;
            model._path = filename.Substring(0, filename.LastIndexOf('\\') + 1);
            if (!Directory.Exists(meshDir))
            {
                Directory.CreateDirectory(meshDir);
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                //if (model._GRAPH != null)
                //{
                //    model.unify();
                //}
                sw.Write("%parents: ");
                foreach (string name in model._parent_names)
                {
                    sw.Write(name + " ");
                }
                sw.WriteLine();
                sw.Write("%original: ");
                foreach (string name in model._original_names)
                {
                    sw.Write(name + " ");
                }
                sw.WriteLine();
                sw.WriteLine("%Category name: " + Functionality.getCategoryName((int)model._CAT));
                sw.WriteLine(model._NPARTS.ToString() + " parts");
                for (int i = 0; i < model._NPARTS; ++i)
                {
                    if (model._PARTS[i]._partName == null)
                    {
                        // It happens when parts get grouped, to avoid using the same part name as other parts.
                        model._PARTS[i]._partName = model.getPartName();
                    }
                    string partName = model._PARTS[i]._partName;
                    partName = model.avoidRepeatPartName(i);
                    sw.WriteLine("% Part #" + i.ToString() + " " + partName);
                    // bounding box
                    Part ipart = model._PARTS[i];
                    foreach (Vector3d v in ipart._BOUNDINGBOX._POINTS3D)
                    {
                        sw.Write(vector3dToString(v, " ", " "));
                    }
                    sw.WriteLine();
                    // principal axes
                    sw.Write(vector3dToString(ipart._BOUNDINGBOX.coordSys.x, " ", " "));
                    sw.Write(vector3dToString(ipart._BOUNDINGBOX.coordSys.y, " ", " "));
                    sw.WriteLine(vector3dToString(ipart._BOUNDINGBOX.coordSys.z, " ", ""));
                    // save mesh
                    string meshName = partName + ".obj";
                    this.saveObj(ipart._MESH, meshDir + meshName, ipart._COLOR);
                    sw.WriteLine("\\" + modelName + "\\" + meshName);
                }
                if (model._GRAPH != null)
                {
                    string graphName = filename.Substring(0, filename.LastIndexOf('.')) + ".graph";
                    saveAGraph(model._GRAPH, graphName);
                    string pgName = filename.Substring(0, filename.LastIndexOf('.')) + ".pg";
                    savePartGroupsOfAModelGraph(model._GRAPH._partGroups, pgName);
                }
                saveModelInfo(model, meshDir, modelName, isOriginalModel);
            }
        }// saveAPartBasedModel
        
        private void saveModelInfo(Model model, string foldername, string model_name, bool isOriginalModel)
        {
            if (model == null)
            {
                return;
            }
            // save mesh
            if (model._MESH == null)
            {
                model.composeMesh();
            }
            string meshName = foldername + model_name + ".obj";
            //this.saveObj(model._MESH, meshName, GLDrawer.MeshColor);
            this.saveModelObj(model, meshName);
            // save .off file & .pts file for shape2pose feature computation
            string shape2poseDataFolder = model._path + "shape2pose\\" + model._model_name + "\\";
            string offname = shape2poseDataFolder + model._model_name + ".off";
            string meshfileName = shape2poseDataFolder + model._model_name + ".mesh";            
            this.saveModelOff(model, offname);
            this.saveModelMesh_StyleSimilarityUse(model, meshfileName);
            string ptsname = shape2poseDataFolder + model._model_name + ".pts";
            this.saveModelSamplePoints(model, ptsname);

            // save mesh sample points & normals & faceindex
            if (model._SP != null)
            {
                string modelSPname = foldername + model_name + ".sp";
                this.saveSamplePointsInfo(model._SP, modelSPname);
                string spColorname = foldername + model_name + ".color";
                this.saveSamplePointsColor(model._SP._blendColors, spColorname);
                string weightsPerCatName = foldername + "points_weights_per_cat\\" + model_name + "\\" + model_name + ".pw";
                this.saveSamplePointWeightsPerCategory(model._SP._weightsPerCat, weightsPerCatName);
            }
            if (isOriginalModel)
            {
                if (model._funcSpaces != null)
                {
                    int fsId = 1;
                    foreach (FunctionalSpace fs in model._funcSpaces)
                    {
                        string fsName = model._model_name + "_" + fsId.ToString() + ".obj";
                        this.saveFunctionalSpace(fs, model_name, foldername, fsId++);
                    }
                }
            }
            for (int i = 0; i < model._NPARTS; ++i)
            {
                Part ipart = model._PARTS[i];
                if (ipart._partSP == null || ipart._partSP._points == null || ipart._partSP._points.Length == 0 || ipart._partSP._normals == null)
                {
                    continue;
                }
                string partSPname = foldername + ipart._partName + ".sp";
                this.saveSamplePointsInfo(ipart._partSP, partSPname);
                string spColorname = foldername + ipart._partName + ".color";
                this.saveSamplePointsColor(ipart._partSP._blendColors, spColorname);
                string partWeightsPerCatName = foldername + "points_weights_per_cat\\" + ipart._partName + "\\" + ipart._partName + ".pw";
                this.saveSamplePointWeightsPerCategory(ipart._partSP._weightsPerCat, partWeightsPerCatName);
                // part mesh index info
                if (isOriginalModel)
                {
                    // Note - this is not necessary for new shapes
                    string partMeshIndexName = foldername + ipart._partName + ".mi";
                    this.savePartMeshIndexInfo(ipart, partMeshIndexName);
                }
            }
        }// saveModelInfo

        public void saveSamplePointWeightsPerCategory(List<PatchWeightPerCategory> weights, string filename)
        {
            if (weights == null)
            {
                return;
            }
            string wfolder = Path.GetDirectoryName(filename);
            if (!Directory.Exists(wfolder))
            {
                Directory.CreateDirectory(wfolder);
            }
            // save point weights per cat
            string name = filename.Substring(0, filename.LastIndexOf('.'));
            string ext = filename.Substring(filename.LastIndexOf('.'));
            foreach (PatchWeightPerCategory pw in weights)
            {
                string catName = name + "_" + pw._catName + ext;
                string folder = Path.GetDirectoryName(catName);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                using (StreamWriter sw = new StreamWriter(catName))
                {
                    sw.WriteLine(pw._nPoints.ToString() + " " + pw._nPatches.ToString());
                    for (int i = 0; i < pw._nPoints; ++i)
                    {
                        StringBuilder sb = new StringBuilder();
                        for (int j = 0; j < pw._nPatches; ++j)
                        {
                            sb.Append(pw._weights[i, j].ToString());
                            sb.Append(" ");
                        }
                        sw.WriteLine(sb.ToString());
                    }
                }
            }
        }// saveSamplePointWeightsPerCategory

        public List<PatchWeightPerCategory> loadSamplePointWeightsPerCategory(string folder)
        {
            List<PatchWeightPerCategory> weights = new List<PatchWeightPerCategory>();
            if (!Directory.Exists(folder))
            {
                return weights;
            }
            string[] filenames = Directory.GetFiles(folder, "*.pw");
            for (int f = 0; f < filenames.Length; ++f)
            {
                string catName = filenames[f].Substring(filenames[f].LastIndexOf('_') + 1);
                catName = catName.Substring(0, catName.LastIndexOf('.'));
                PatchWeightPerCategory pw = new PatchWeightPerCategory(catName);
                using (StreamReader sr = new StreamReader(filenames[f]))
                {
                    char[] separator = { ' ', '\t' };
                    string s = sr.ReadLine().Trim();
                    string[] strs = s.Split(separator);
                    pw._nPoints = int.Parse(strs[0]);
                    pw._nPatches = int.Parse(strs[1]);
                    pw._weights = new double[pw._nPoints, pw._nPatches];
                    for (int i = 0; i < pw._nPoints; ++i)
                    {
                        s = sr.ReadLine().Trim();
                        strs = s.Split(separator);
                        if (strs.Length != pw._nPatches)
                        {
                            MessageBox.Show("Data format error: Unmatched patch number. " 
                                + filenames[f].Substring(filenames[f].LastIndexOf('\\')));
                            return new List<PatchWeightPerCategory>(); // return empty
                        }
                        for (int j = 0; j < pw._nPatches; ++j)
                        {
                            pw._weights[i, j] = double.Parse(strs[j]);
                        }
                    }
                }
                weights.Add(pw);
            }
            return weights;
        }// loadSamplePointWeightsPerCategory

        public void saveValidityMatrix(string filename)
        {
            if (_validityMatrixPG == null)
            {
                return;
            }
            //string folder = Interface.MODLES_PATH + "ValidityMatrix\\";
            //string[] files = Directory.GetFiles(folder, "*.vdm");
            //List<string> fileStrs = new List<string>(files);
            //int id = 0;
            //string filename = "validityMatrix_" + id.ToString();
            //while(fileStrs.Contains(filename))
            //{
            //    id++;
            //    filename = "validityMatrix_" + id.ToString();
            //}
            using (StreamWriter sw = new StreamWriter(filename))
            {
                // models name
                StringBuilder sb = new StringBuilder();
                foreach(Model m in _ancesterModels)
                {
                    sb.Append(m._model_name);
                    sb.Append(" ");
                }
                sw.WriteLine(sb.ToString());
                sw.WriteLine(_partGroups.Count.ToString());
                sw.WriteLine(_validityMatrixPG.NTriplets.ToString());
                // matrix
                for(int i = 0; i < _validityMatrixPG.NTriplets; ++i)
                {
                    Triplet triplet = _validityMatrixPG.GetTriplet(i);
                    sb = new StringBuilder();
                    sb.Append(triplet.row.ToString());
                    sb.Append(" ");
                    sb.Append(triplet.col.ToString());
                    sb.Append(" ");
                    sb.Append(triplet.value.ToString());
                    sw.WriteLine(sb.ToString());
                }
            }
        }// saveValidityMatrix

        public void saveTimingInfo(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                double secs = avgTimePerValidOffspring / validOffspringNumber;
                sw.WriteLine("Total valid offspring: " + validOffspringNumber.ToString());
                sw.WriteLine("Longest Time to run a valid crossover: " + longestTimePerValidOffspring.ToString() + " senconds.");
                sw.WriteLine("Average Time to run a valid crossover: " + secs.ToString() + " senconds.");
            }
        }// saveTimingInfo

        public void loadValidityMatrix(string filename)
        {
            if(!File.Exists(filename))
            {
                return;
            }
            if (_ancesterModels.Count == 0)
            {
                MessageBox.Show("Load an input set first.");
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t' };
                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separator);
                // model name
                if (strs.Length != _ancesterModels.Count)
                {
                    MessageBox.Show("Matrix does not match the input set.");
                    return;
                }
                List<string> modelNames = new List<string>(strs);
                foreach(Model m in _ancesterModels)
                {
                    if (!modelNames.Contains(m._model_name))
                    {
                        MessageBox.Show("Matrix does not match the input set.");
                        return;
                    }
                }
                s = sr.ReadLine().Trim();
                strs = s.Split(separator);
                int npartgroups = int.Parse(strs[0]);
                if (_partGroups.Count != npartgroups)
                {
                    // should use a more consistent way to store the part groups !
                    MessageBox.Show("Matrix does not match the input set.");
                    return;
                }
                s = sr.ReadLine().Trim();
                strs = s.Split(separator);
                int nTriplets = int.Parse(strs[0]);
                _validityMatrixPG = new SparseMatrix(npartgroups, npartgroups);
                for (int i = 0; i < nTriplets; ++i)
                {
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    int row = int.Parse(strs[0]);
                    int col = int.Parse(strs[1]);
                    double val = double.Parse(strs[2]);
                    Triplet triplet = new Triplet(row, col, val);
                    _validityMatrixPG.AddTriplet(triplet);
                }
            }
        }// loadValidityMatrix

        private void initMatlab()
        {
            string exeStr = "cd " + Interface.MATLAB_PATH;
            this.matlab.Execute(exeStr);
            this.matlab.Execute("allModels = loadAllRegressionModels()");
        }

        public void savePointFeature()
        {
            if (_currModel == null || _currModel._GRAPH == null)
            {
                return;
            }
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Object matlabOutput = null;
            this.matlab.Feval("clearData", 0, out matlabOutput);

            this.runFunctionalityTest(_currModel);
            //this.runFunctionalityTestWithPatchCombination(_currModel, 0);

            long secs = stopWatch.ElapsedMilliseconds / 1000;
            Program.writeToConsole("Time to compute features: " + secs.ToString() + " senconds.");
        }

        private void reSamplingForANewShape(Model model)
        {
            string shape2poseDataFolder = model._path + "shape2pose\\" + model._model_name + "\\";
            if (!Directory.Exists(shape2poseDataFolder))
            {
                Directory.CreateDirectory(shape2poseDataFolder);
            }
            model.composeMesh();
            string meshName = shape2poseDataFolder + model._model_name + ".mesh";
            this.saveModelMesh_StyleSimilarityUse(model, meshName);
            string exeFolder = @"..\..\external\";
            string exePath = Path.GetFullPath(exeFolder);
            string samplingCmd = exePath + "StyleSimilarity.exe ";
            string samplingCmdPara = "-sample " + meshName + " -numSamples 2000 -visibilityChecking 1";

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = samplingCmd;
            startInfo.Arguments = samplingCmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // reset sample points
            string ptsname = shape2poseDataFolder + model._model_name + ".pts";
            model._SP = this.loadSamplePoints(ptsname, model._MESH.FaceCount);

            this.recomputeSamplePointsFaceIndex(model._SP, model._MESH);

            string folder = model._path + model._model_name + "\\";
            string modelSPname = folder + model._model_name + ".sp";
            this.saveSamplePointsInfo(model._SP, modelSPname);
            string spColorname = folder + model._model_name + ".color";
            model._SP._blendColors = new Color[model._SP._points.Length];
            this.saveSamplePointsColor(model._SP._blendColors, spColorname);
            this.currMeshClass = new MeshClass(model._MESH);
            foreach (Part p in model._PARTS)
            {
                p.buildSamplePoints(p._FACEVERTEXINDEX, model._SP);
                string partSPname = folder + p._partName + ".sp";
                this.saveSamplePointsInfo(p._partSP, partSPname);
            }
        }// reSamplingForANewShape

        public bool computeShape2PoseAndIconFeatures(Model model)
        {
            string path = model._path;
            string model_name = model._model_name;
            string shape2poseDataFolder = path + "shape2pose\\" + model_name + "\\";
            if (!Directory.Exists(shape2poseDataFolder))
            {
                Directory.CreateDirectory(shape2poseDataFolder);
            }
            string exeFolder = @"..\..\external\";
            string exePath = Path.GetFullPath(exeFolder);

            string shape2poseMeshFile = shape2poseDataFolder + model_name + ".off";
            string shape2poseSampleFile = shape2poseDataFolder + model_name + ".pts";


            string msh2plnCmd = exePath + "msh2pln.exe ";
            string prstOutputFile1 = shape2poseDataFolder + model_name + "_msh.planes.txt";
            string prstOutputFile2 = shape2poseDataFolder + model_name + "_msh.arff";
            string prstCmdPara = shape2poseMeshFile + " " + prstOutputFile1 +
                              " -v -input_points " + shape2poseSampleFile + " -output_point_properties " + prstOutputFile2 +
                              " -in_plane_vector 0 1 0 -min_value 0.9 -min_weight 128";
            //Process.Start(msh2plnCmd, prstCmdPara);

            // WaitForExit(); block the program from responding

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = msh2plnCmd;
            startInfo.Arguments = prstCmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // metric
            string metricOutputFile = shape2poseDataFolder + model_name + ".metric";
            string metricCmd = exePath + "Metric.exe ";
            string metricCmdPara = "-mesh " + shape2poseMeshFile + " -pnts " + shape2poseSampleFile +
                            " -dist geodGraph -writeDist " + metricOutputFile;
            //Process.Start(metricCmd, metricCmdPara).WaitForExit();

            process = new Process();
            startInfo = new ProcessStartInfo();
            startInfo.FileName = metricCmd;
            startInfo.Arguments = metricCmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            string computelocalfeatureCmd = exePath + "ComputeLocalFeatures.exe ";
            // oriented geodesic PCA
            string ogPCAOutputFile = shape2poseDataFolder + model_name + "_og.arff";
            string ogPCACmdPara = shape2poseMeshFile + " -points " + shape2poseSampleFile +
                               " -radius 0.1 -outfile " + ogPCAOutputFile + " -feat OrientedGeodesicPCA -densePoints " + shape2poseSampleFile +
                               " -metricFile " + metricOutputFile + " -randseed -1";
            //Process.Start(computelocalfeatureCmd, ogPCACmdPara).WaitForExit();

            process = new Process();
            startInfo = new ProcessStartInfo();
            startInfo.FileName = computelocalfeatureCmd;
            startInfo.Arguments = ogPCACmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // abs curv
            string absCurvOutputFile = shape2poseDataFolder + model_name + "_absCurv.arff";
            string absCurvCmdPara = shape2poseMeshFile + " -points " + shape2poseSampleFile +
                " -radius -1 -outfile " + absCurvOutputFile + " -feat AbsCurv -densePoints " + shape2poseSampleFile +
                " -metricFile " + metricOutputFile + " -randseed -1";
            //Process.Start(computelocalfeatureCmd, absCurvCmdPara).WaitForExit();

            process = new Process();
            startInfo = new ProcessStartInfo();
            startInfo.FileName = computelocalfeatureCmd;
            startInfo.Arguments = absCurvCmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // abs curv geodesic
            string absCurvGeoAvgOutputFile = shape2poseDataFolder + model_name + "_absCga.arff";
            string absCurvGeoAvgCmdPara = shape2poseMeshFile + " -points " + shape2poseSampleFile +
                " -radius 0.1 -outfile " + absCurvGeoAvgOutputFile + " -feat AbsCurvGeodesicAvg -densePoints " + shape2poseSampleFile +
                " -metricFile " + metricOutputFile + " -randseed -1";
            //Process.Start(computelocalfeatureCmd, absCurvGeoAvgCmdPara);

            process = new Process();
            startInfo = new ProcessStartInfo();
            startInfo.FileName = computelocalfeatureCmd;
            startInfo.Arguments = absCurvGeoAvgCmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // min & max curvature K1 K2
            string k1k2OutputFile = shape2poseDataFolder + model_name + "_K1K2.curvature";
            string k1k2Cmd = exePath + "StyleSimilarity.exe ";
            string k1k2CmdPara = k1k2OutputFile;
            process = new Process();
            startInfo = new ProcessStartInfo();
            startInfo.FileName = k1k2Cmd;
            startInfo.Arguments = "-curvature " + shape2poseSampleFile;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // load features
            int nSamplePoints = model._SP._points.Length;

            model._funcFeat = new FuncFeatures();
            model._funcFeat._pcaFeats = loadShape2Pose_OrientedGeodesicPCAFeatures(ogPCAOutputFile);
            // load sym plane
            Vector3d[] centers;
            Vector3d[] normals;
            this.loadShape2Pose_SymPlane(prstOutputFile1, out centers, out normals);
            if (centers == null || normals == null || centers.Length == 0)
            {
                centers = new Vector3d[1];
                centers[0] = new Vector3d(0, 0.5, 0);
                normals = new Vector3d[1];
                normals[0] = new Vector3d(1, 0, 0);
            }
            model.findBestSymPlane(centers, normals);
            model.computeSamplePointsFeatures();
            
            double[] absCurv = this.loadShape2Pose_nDimFeatures(absCurvOutputFile, 1);
            double[] absCurvGeo = this.loadShape2Pose_nDimFeatures(absCurvGeoAvgOutputFile, 1);

            double[] k1k2Curv = this.loadShape2Pose_nDimFeatures(k1k2OutputFile, 2);

            if (absCurv == null || absCurvGeo == null || k1k2Curv == null)
            {
                return false;
            }

            int dim = Functionality._CURV_FEAT_DIM;
            model._funcFeat._curvFeats = new double[dim * nSamplePoints];
            for (int i = 0; i < nSamplePoints; ++i)
            {
                model._funcFeat._curvFeats[i * dim] = absCurv[i];
                model._funcFeat._curvFeats[i * dim + 1] = absCurvGeo[i];
                model._funcFeat._curvFeats[i * dim + 2] = k1k2Curv[i * 2];
                model._funcFeat._curvFeats[i * dim + 3] = k1k2Curv[i * 2 + 1];
            }
            
            model._funcFeat._rayFeats = model._MESH.computeRayDist(model._SP._points, model._SP._normals, out model._funcFeat._visibliePoint);
            model.computeDistAndAngleToCenterOfConvexHull();
            model.computeDistAndAngleToCenterOfMass();

            // tes 
            model._SP.testVisiblePoints = new List<Vector3d>();
            model._SP.testNormals = new List<Vector3d>();
            //model._SP.updateNormals(model._MESH);
            for (int i = 0; i < model._SP._points.Length; ++i)
            {
                Vector3d v = model._SP._points[i];
                if (model._funcFeat._visibliePoint[i])
                {
                    model._SP.testVisiblePoints.Add(v);
                    Vector3d vshift = v + model._SP._normals[i] * 0.05;
                    model._SP.testNormals.Add(v);
                    model._SP.testNormals.Add(vshift);
                }
            }
            return true;
        }// computeShape2PoseAndIconFeatures


        private int[] kMeansClustering(double[][] simMat, int numClusters)
        {
            Console.WriteLine("\nSetting numClusters to " + numClusters);

            int[] clustering = KMeansDemo.Cluster(simMat, numClusters); 

            Console.WriteLine("\nK-means clustering complete\n");

            Console.WriteLine("Final clustering in internal form:\n");
            KMeansDemo.ShowVector(clustering, true);

            Console.WriteLine("Raw data by cluster:\n");
            KMeansDemo.ShowClustered(simMat, clustering, numClusters, 1);

            Console.WriteLine("\nEnd k-means clustering demo\n");
            //Console.ReadLine();

            return clustering;
        }// clustering

        public void LFD_test()
        {
            this.LFD(_ancesterModels);
        }

        private List<Model> LFD(List<Model> models)
        {
            // compare the similarity between shapes to select a set of diverse shapes
            string exe_path = @"..\..\external\LFD\";
            string alighmentCmd = "3DAlignment.exe";
            string lfdCmd = "GroundTruth.exe"; // Note in the .exe file, the number of model is fixed to 4
            string prstCmdPara = "";
            string listFile = exe_path + "list.txt";
            string prefix = @"Models\";
            string model_path = exe_path + prefix;

            if (!Directory.Exists(model_path))
            {
                Directory.CreateDirectory(model_path);
            }

            if (!File.Exists(alighmentCmd))
            {
                return null;
            }

            // 1. write .obj file name to "list.txt"
            if (models == null || models.Count < 2)
            {
                MessageBox.Show("Not enough models for comparison.");
                return null;
            }
            List<string> model_list = new List<string>();
            Dictionary<string, int> modelMap = new Dictionary<string, int>();
            int n = 0;
            using (StreamWriter sw = new StreamWriter(listFile))
            {
                foreach (Model model in models)
                {
                    string name = prefix + model._model_name;
                    modelMap.Add(name, n++);
                    sw.WriteLine(name);
                    model_list.Add(name);
                    string fullfilename = model_path + model._model_name + ".obj";
                    this.saveModelObj(model, fullfilename);
                }
            }

            // for "ground_trutch.exe"
            string result_dir = exe_path + "Results\\";
            string compare_file = exe_path + "compare.txt";
            if (!Directory.Exists(result_dir))
            {
                Directory.CreateDirectory(result_dir);
            }
            StreamWriter sw_compare = new StreamWriter(compare_file);
            foreach (string cur in model_list) {
                sw_compare.WriteLine(cur);
                StreamWriter sw_cur = new StreamWriter(exe_path + cur + ".txt");
                sw_cur.WriteLine(cur);
                sw_cur.Close();
            }
            sw_compare.Close();
            using (StreamReader sr = new StreamReader(listFile))
            {
                while (sr.Peek() > 0)
                {
                    string s = sr.ReadLine().Trim();
                    string res_dir = result_dir + s;
                    if (!Directory.Exists(res_dir))
                    {
                        Directory.CreateDirectory(res_dir);
                    }
                }
            }

            // 2. call "3DAlignment.exe" for computing features

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = exe_path;
            startInfo.FileName = alighmentCmd;
            startInfo.Arguments = prstCmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // 2. read model names from "list.txt" to compute the similarity of pairs of models using "GroundTruth.exe"
            prstCmdPara = models.Count.ToString();

            process = new Process();
            startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = exe_path;
            startInfo.FileName = lfdCmd;
            startInfo.Arguments = prstCmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // read similarity
            int count = models.Count;
            double[][] sims = new double[count][];
            for (int i = 0; i < count; ++i) {
                sims[i] = new double[count];
            }
            string simDir = result_dir + "Models\\";
            for (int i = 0; i < count; ++i)
            {
                string simFile = simDir + models[i]._model_name + "_sim.txt";
                using (StreamReader sr = new StreamReader(simFile))
                {
                    char[] separator = { ' ' };
                    while (sr.Peek() > 0)
                    {
                        string s = sr.ReadLine().Trim();
                        string[] arr = s.Split(separator);
                        string next = arr[0].Substring(arr[0].LastIndexOf('\\') + 1);
                        int nextid = -1;
                        modelMap.TryGetValue(next, out nextid);
                        if (nextid >= 0)
                        {
                            sims[i][nextid] = double.Parse(arr[1]);
                        }
                    }
                }
            }
            int minCluster = _ancesterModels.Count;
            int nCluster = minCluster; // Math.Max(minCluster, count / 10);
            nCluster = Math.Min(nCluster, sims.Length);
            int[] clusters = kMeansClustering(sims, nCluster);
            // in each cluster, record the index of the corresponding models
            List<int>[] clusterIds = new List<int>[nCluster];
            for (int i = 0; i < nCluster; ++i)
            {
                clusterIds[i] = new List<int>();
            }
            for (int i = 0; i < clusters.Length; ++i)
            {
                clusterIds[clusters[i]].Add(i);
            }
            List<Model> representatives = new List<Model>();
            int numInCluster = 2;
            for (int i = 0; i < nCluster; ++i)
            {
                Random rand = new Random();
                int m = Math.Min(numInCluster, clusterIds[i].Count);
                if (m == clusterIds[i].Count)
                {
                    for (int j = 0; j < clusterIds[i].Count; ++j)
                    {
                        representatives.Add(models[clusterIds[i][j]]);
                    }
                }
                else
                {
                    HashSet<int> ids = new HashSet<int>();
                    for (int j = 0; j < m; ++j)
                    {
                        int selected = rand.Next(clusterIds[i].Count);
                        while (ids.Contains(selected))
                        {
                            selected = rand.Next(clusterIds[i].Count);
                        }
                        representatives.Add(models[clusterIds[i][selected]]);
                        ids.Add(selected);
                    }
                }
            }
            return representatives;
        }// LFD

        double[,] partSimMat;
        Dictionary<string, int> partNameMap;
        private void computePartSimExternally(List<Model> models)
        {
            // compare the similarity between shapes to select a set of diverse shapes
            string exe_path = @"..\..\external\LFD\";
            string alighmentCmd = "3DAlignment.exe";
            string lfdCmd = "GroundTruth.exe"; // Note in the .exe file, the number of model is fixed to 4
            string prstCmdPara = "";
            string listFile = exe_path + "list.txt";
            string prefix = @"Parts\";
            string part_path = exe_path + prefix;

            if (!Directory.Exists(part_path))
            {
                Directory.CreateDirectory(part_path);
            }

            if (!File.Exists(alighmentCmd))
            {
                return;
            }

            // 1. write .obj file name to "list.txt"
            // save each part group
            List<Part> parts = new List<Part>();
            foreach (Model m in models)
            {
                foreach (Part p in m._PARTS)
                {
                    parts.Add(p);
                }
            }
            // collect meshes
            int n = 0;
            partNameMap = new Dictionary<string, int>();
            foreach (Part p in parts)
            {
                string filename = part_path + "part_" + n.ToString() + ".obj";
                partNameMap.Add(p._partName, n);
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    Mesh mesh = p._MESH;

                    // vertex
                    string s = "";
                    for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                    {
                        s = "v";
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        s += " " + mesh.VertexPos[j++].ToString();
                        sw.WriteLine(s);
                    }
                    // face
                    for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                    {
                        s = "f";
                        s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                        s += " " + (mesh.FaceVertexIndex[j++] + 1).ToString();
                        sw.WriteLine(s);
                    }
                }
                ++n;
            }

            List<string> part_list = new List<string>();
            using (StreamWriter sw = new StreamWriter(listFile))
            {
                int id = 0;
                foreach (Part p in parts)
                {
                    string name = prefix + "part_" + id.ToString();
                    sw.WriteLine(name);
                    part_list.Add(name);
                    ++id;
                }
            }

            // for "ground_trutch.exe"
            string result_dir = exe_path + "Results\\";
            string compare_file = exe_path + "compare.txt";
            if (!Directory.Exists(result_dir))
            {
                Directory.CreateDirectory(result_dir);
            }
            StreamWriter sw_compare = new StreamWriter(compare_file);
            foreach (string cur in part_list)
            {
                sw_compare.WriteLine(cur);
                StreamWriter sw_cur = new StreamWriter(exe_path + cur + ".txt");
                sw_cur.WriteLine(cur);
                sw_cur.Close();
            }
            sw_compare.Close();
            using (StreamReader sr = new StreamReader(listFile))
            {
                while (sr.Peek() > 0)
                {
                    string s = sr.ReadLine().Trim();
                    string res_dir = result_dir + s;
                    if (!Directory.Exists(res_dir))
                    {
                        Directory.CreateDirectory(res_dir);
                    }
                }
            }

            // 2. call "3DAlignment.exe" for computing features

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = exe_path;
            startInfo.FileName = alighmentCmd;
            startInfo.Arguments = prstCmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // 2. read model names from "list.txt" to compute the similarity of pairs of models using "GroundTruth.exe"

            process = new Process();
            startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = exe_path;
            startInfo.FileName = lfdCmd;
            startInfo.Arguments = parts.Count.ToString(); // prstCmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // read similarity
            int count = parts.Count;
            double[][] sims = new double[count][];
            for (int i = 0; i < count; ++i)
            {
                sims[i] = new double[count];
            }
            string simDir = result_dir + "Parts\\";
            double maxVal = 0;
            for (int i = 0; i < count; ++i)
            {
                string simFile = simDir + "part_" + i.ToString() + "_sim.txt";
                using (StreamReader sr = new StreamReader(simFile))
                {
                    char[] separator = { ' ' };
                    while (sr.Peek() > 0)
                    {
                        string s = sr.ReadLine().Trim();
                        string[] arr = s.Split(separator);
                        string next = arr[0].Substring(arr[0].LastIndexOf('_') + 1);
                        int nextid = Int32.Parse(next);
                        sims[i][nextid] = double.Parse(arr[1]);
                        maxVal = Math.Max(maxVal, sims[i][nextid]);
                    }
                }
            }
            partSimMat = new double[count, count];
            for (int i = 0; i < count; ++i)
            {
                double[] cur = sims[i];
                int[] keys = new int[count];
                Array.Sort(cur, keys);
                for (int j = 0; j < count; ++j)
                {
                    partSimMat[i, j] = (double)sims[i][j] / maxVal;
                }
            }
        } // compute part similarity 

        private void computePGSimExternally(List<Model> models)
        {
            // compare the similarity between shapes to select a set of diverse shapes
            string exe_path = @"..\..\external\LFD\";
            string alighmentCmd = "3DAlignment.exe";
            string lfdCmd = "GroundTruth.exe"; // Note in the .exe file, the number of model is fixed to 4
            string prstCmdPara = "";
            string listFile = exe_path + "list.txt";
            string prefix = @"PartGroups\";
            string pg_path = exe_path + prefix;

            if (!Directory.Exists(pg_path))
            {
                Directory.CreateDirectory(pg_path);
            }

            if (!File.Exists(alighmentCmd))
            {
                return;
            }

            // 1. write .obj file name to "list.txt"
            // save each part group
            List<PartGroup> pgs = new List<PartGroup>();
            foreach (Model m in models)
            {
                foreach (PartGroup pg in m._GRAPH._partGroups)
                {
                    if (pg._NODES.Count > 0)
                    {
                        pgs.Add(pg);
                    }
                }
            }
            // collect meshes
            int n = 0;
            pgSimMatrixMap = new Dictionary<PartGroup, int>();
            foreach (PartGroup pg in pgs)
            {
                string filename = pg_path + "pg_" + n.ToString() + ".obj";
                pgSimMatrixMap.Add(pg, n);
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    int start = 0;
                    foreach (Node node in pg._NODES)
                    {
                        Mesh mesh = node._PART._MESH;

                        // vertex
                        string s = "";
                        for (int i = 0, j = 0; i < mesh.VertexCount; ++i)
                        {
                            s = "v";
                            s += " " + mesh.VertexPos[j++].ToString();
                            s += " " + mesh.VertexPos[j++].ToString();
                            s += " " + mesh.VertexPos[j++].ToString();
                            sw.WriteLine(s);
                        }
                        // face
                        for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                        {
                            s = "f";
                            s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                            s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                            s += " " + (mesh.FaceVertexIndex[j++] + 1 + start).ToString();
                            sw.WriteLine(s);
                        }
                        start += mesh.VertexCount;
                    }
                }
                ++n;
            }

            List<string> pg_list = new List<string>();
            using (StreamWriter sw = new StreamWriter(listFile))
            {
                int id = 0;
                foreach (PartGroup pg in pgs)
                {
                    string name = prefix + "pg_" + id.ToString();
                    sw.WriteLine(name);
                    pg_list.Add(name);
                    ++id;
                }
            }

            // for "ground_trutch.exe"
            string result_dir = exe_path + "Results\\";
            string compare_file = exe_path + "compare.txt";
            if (!Directory.Exists(result_dir))
            {
                Directory.CreateDirectory(result_dir);
            }
            StreamWriter sw_compare = new StreamWriter(compare_file);
            foreach (string cur in pg_list)
            {
                sw_compare.WriteLine(cur);
                StreamWriter sw_cur = new StreamWriter(exe_path + cur + ".txt");
                sw_cur.WriteLine(cur);
                sw_cur.Close();
            }
            sw_compare.Close();
            using (StreamReader sr = new StreamReader(listFile))
            {
                while (sr.Peek() > 0)
                {
                    string s = sr.ReadLine().Trim();
                    string res_dir = result_dir + s;
                    if (!Directory.Exists(res_dir))
                    {
                        Directory.CreateDirectory(res_dir);
                    }
                }
            }

            // 2. call "3DAlignment.exe" for computing features

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = exe_path;
            startInfo.FileName = alighmentCmd;
            startInfo.Arguments = prstCmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // 2. read model names from "list.txt" to compute the similarity of pairs of models using "GroundTruth.exe"

            process = new Process();
            startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = exe_path;
            startInfo.FileName = lfdCmd;
            startInfo.Arguments = pgs.Count.ToString(); // prstCmdPara;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // read similarity
            int count = pgs.Count;
            double[][] sims = new double[count][];
            for (int i = 0; i < count; ++i)
            {
                sims[i] = new double[count];
            }
            string simDir = result_dir + "PartGroups\\";
            for (int i = 0; i < count; ++i)
            {
                string simFile = simDir + "pg_" + i.ToString() + "_sim.txt";
                using (StreamReader sr = new StreamReader(simFile))
                {
                    char[] separator = { ' ' };
                    while (sr.Peek() > 0)
                    {
                        string s = sr.ReadLine().Trim();
                        string[] arr = s.Split(separator);
                        string next = arr[0].Substring(arr[0].LastIndexOf('_') + 1);
                        int nextid = Int32.Parse(next);
                        sims[i][nextid] = double.Parse(arr[1]);
                    }
                }
            }
            pgSimMatrix = new double[count, count];
            for (int i = 0; i < count; ++i)
            {
                double maxVal = 0;
                for (int j = 0; j < count; ++j)
                {
                    maxVal = Math.Max(maxVal, sims[i][j]);
                }
                double[] cur = sims[i];
                int[] keys = new int[count];
                Array.Sort(cur, keys);
                for (int j = 0; j < count; ++j)
                {
                    pgSimMatrix[i, j] = (double)sims[i][j] / maxVal;
                }
            }
        } // computePGsimilarityExternally

        private void computePartGroupSimilarity(List<Model> models)
        {
            List<PartGroup> pgs = new List<PartGroup>();
            foreach (Model m in models)
            {
                foreach (PartGroup pg in m._GRAPH._partGroups)
                {
                    if (pg._NODES.Count > 0)
                    {
                        pgs.Add(pg);
                    }
                }
            }
            // collect meshes
            int n = 0;
            pgSimMatrixMap = new Dictionary<PartGroup, int>();
            foreach (PartGroup pg in pgs)
            {
                pgSimMatrixMap.Add(pg, n);
            }
            int count = pgs.Count;
            pgSimMatrix = new double[count, count];
            for (int i = 0; i < count - 1; ++i)
            {
                List<int> pg_i_ids = new List<int>();
                foreach (Node node in pgs[i]._NODES)
                {
                    int idx = -1;
                    partNameMap.TryGetValue(node._PART._partName, out idx);
                    if (idx != -1)
                    {
                        pg_i_ids.Add(idx);
                    }
                }
                for (int j = i + 1; j < count; ++j)
                {
                    int n_toCompare = 0;
                    double diff = 0;
                    foreach (Node node in pgs[j]._NODES)
                    {
                        int idx = -1;
                        partNameMap.TryGetValue(node._PART._partName, out idx);
                        if (idx != -1)
                        {
                            double minDiff = double.MaxValue;
                            foreach (int i_idx in pg_i_ids)
                            {
                                minDiff = Math.Min(minDiff, partSimMat[i_idx, idx]);
                            }
                            diff += minDiff;
                            ++n_toCompare;
                        }
                    }
                    diff /= n_toCompare;
                    pgSimMatrix[i, j] = diff;
                    pgSimMatrix[j, i] = diff;
                }
            }
        }// computePartGroupSimilarity

        public void computePGsimilarity()
        {
            //computePGSimExternally();
            // save pg set info
            //foreach (Model m in _ancesterModels) {
            //    string filename = m._path + m._model_name + ".pg";
            //    savePartGroupsOfAModelGraph(m._GRAPH._partGroups, filename);
            //}
        }// computePGsimilarity

        private double[] loadShape2Pose_OrientedGeodesicPCAFeatures(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t', ',' };
                List<double> pcas = new List<double>();
                while (sr.Peek() > 0)
                {
                    string s = sr.ReadLine().Trim();
                    if (s[0] == '@')
                    {
                        continue;
                    }
                    string[] strs = s.Split(separators);                   
                    if (strs.Length != 5)
                    {
                        MessageBox.Show("Wrong data format - Oriented Geodesic PCA.");
                        return null;
                    }
                    for (int i = 0; i < strs.Length; ++i)
                    {
                        pcas.Add(double.Parse(strs[i]));
                    }
                }
                return pcas.ToArray();
            }
        }// loadShape2Pose_OrientedGeodesicPCAFeatures

        private double[] loadShape2Pose_nDimFeatures(string filename, int dim)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };
                List<double> feats = new List<double>();
                while (sr.Peek() > 0)
                {
                    string s = sr.ReadLine().Trim();
                    if (s[0] == '@')
                    {
                        continue;
                    }
                    string[] strs = s.Split(separators);
                    if (strs.Length < dim)
                    {
                        MessageBox.Show("Wrong data format - ." + Path.GetFileName(filename));
                        return null;
                    }
                    if (strs[0].Contains("NAN"))
                    {
                        feats.Add(0);
                    }
                    else
                    {
                        feats.Add(double.Parse(strs[0]));
                    }
                    if (dim == 2)
                    {
                        if (strs[1].Contains("NAN"))
                        {
                            feats.Add(0);
                        }
                        else
                        {
                            feats.Add(double.Parse(strs[1]));
                        }
                    }
                }
                return feats.ToArray();
            }
        }// loadShape2Pose_nDimFeatures


        private void loadShape2Pose_SymPlane(string filename, out Vector3d[] centers, out Vector3d[] normals)
        {
            centers = null;
            normals = null;
            if (!File.Exists(filename))
            {
                return;
            }
            List<Vector3d> cs = new List<Vector3d>();
            List<Vector3d> ns = new List<Vector3d>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t', ',' };
                while (sr.Peek() > 0)
                {
                    string s = sr.ReadLine().Trim();
                    string[] strs = s.Split(separators);
                    if (strs.Length < 6)
                    {
                        continue;
                    }
                    double[] vecs = new double[3];
                    int i = -1;
                    int j = 0;
                    while (++i < strs.Length)
                    {
                        if (strs[i] == "")
                        {
                            continue;
                        }
                        if (j == 3)
                        {
                            break;
                        }
                        vecs[j++] = double.Parse(strs[i]);
                    }
                    Vector3d center = new Vector3d(vecs);
                    j = 0;
                    --i;
                    while (++i < strs.Length)
                    {
                        if (strs[i] == "")
                        {
                            continue;
                        }
                        if (j == 3)
                        {
                            break;
                        }
                        vecs[j++] = double.Parse(strs[i]);
                    }
                    Vector3d normal = new Vector3d(vecs);
                    cs.Add(center);
                    ns.Add(normal);
                }
                centers = cs.ToArray();
                normals = ns.ToArray();
            }
        }// loadShape2Pose_SymPlane

        private void saveModelSamplePoints(Model model, string filename)
        {
            if (model == null || model._SP == null)
            {
                return;
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                SamplePoints sp = model._SP;
                for (int j = 0; j < sp._points.Length; ++j)
                {
                    Vector3d vpos = sp._points[j];
                    sw.Write(vector3dToString(vpos, " ", " "));
                    Vector3d vnor = sp._normals[j];
                    sw.Write(vector3dToString(vnor, " ", " "));
                    int fidx = sp._faceIdx[j];
                    sw.WriteLine(fidx.ToString());
                }
            }
        }// saveModelSamplePoints

        private void saveModelSamplePointsFromParts(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                int start = 0;
                foreach (Node node in _currModel._GRAPH._NODES)
                {
                    SamplePoints sp = node._PART._partSP;
                    for (int j = 0; j < sp._points.Length; ++j)
                    {
                        Vector3d vpos = sp._points[j];
                        sw.Write(vector3dToString(vpos, " ", " "));
                        Vector3d vnor = sp._normals[j];
                        sw.Write(vector3dToString(vnor, " ", " "));
                        int fidx = start + sp._faceIdx[j];
                        sw.WriteLine(fidx.ToString());
                    }
                    start += node._PART._MESH.FaceCount;
                }
            }
        }// saveModelSamplePoints

        private void saveSamplePointsInfo(SamplePoints sp, string filename)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                //sw.WriteLine(sp._points.Length.ToString());
                for (int j = 0; j < sp._points.Length; ++j)
                {
                    Vector3d vpos = sp._points[j];
                    sw.Write(vector3dToString(vpos, " ", " "));
                    Vector3d vnor = sp._normals[j];
                    sw.Write(vector3dToString(vnor, " ", " "));
                    sw.WriteLine(sp._faceIdx[j].ToString());
                }
            }
        }// saveSamplePointsInfo

        private void saveSamplePointsColor(Color[] colors, string filename)
        {
            if (colors == null || colors.Length == 0)
            {
                return;
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int j = 0; j < colors.Length; ++j)
                {
                    Color c = colors[j];
                    sw.WriteLine(c.R.ToString() + " " + c.G.ToString() + " " + c.B.ToString());
                }
            }
        }// saveSamplePointsColor

        private void savePartMeshIndexInfo(Part part, string filename)
        {
            if (part._VERTEXINDEX == null || part._FACEVERTEXINDEX == null)
            {
                //MessageBox.Show("The part lack index info from the model mesh.");
                return;
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine(part._VERTEXINDEX.Length.ToString());
                for (int i = 0; i < part._VERTEXINDEX.Length; ++i)
                {
                    sw.WriteLine(part._VERTEXINDEX[i].ToString());
                }
                sw.WriteLine(part._FACEVERTEXINDEX.Length.ToString());
                for (int i = 0; i < part._FACEVERTEXINDEX.Length; ++i)
                {
                    sw.WriteLine(part._FACEVERTEXINDEX[i].ToString());
                }
            }
        }// savePartMeshIndexInfo

        private void loadPartMeshIndexInfo(string filename, out int[] vertexIndex, out int[] faceVertexIndex)
        {
            vertexIndex = null;
            faceVertexIndex = null;
            if (!File.Exists(filename))
            {
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };

                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separators);
                int nv = int.Parse(strs[0]);
                vertexIndex = new int[nv];
                for (int i = 0; i < nv; ++i)
                {
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separators);
                    vertexIndex[i] = int.Parse(strs[0]);
                }
                s = sr.ReadLine().Trim();
                strs = s.Split(separators);
                int nf = int.Parse(strs[0]);
                faceVertexIndex = new int[nf];
                for (int i = 0; i < nf; ++i)
                {
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separators);
                    faceVertexIndex[i] = int.Parse(strs[0]);
                }
            }
        }// loadPartMeshIndexInfo

        private string vector3dToString(Vector3d v, string sep, string tail)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0:0.###}", v.x) + sep +
                            string.Format("{0:0.###}", v.y) + sep +
                            string.Format("{0:0.###}", v.z));
            sb.Append(tail);
            return sb.ToString();
        }// vector3dToString

        private string double2String(double val)
        {
            return string.Format("{0:0.###}", val);
        }

        private string formatOutputStr(double val)
        {
            return string.Format("{0:0.######}", val); 
        }

        private Model loadOnePartBasedModel(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                string model_name = filename.Substring(filename.LastIndexOf('\\') + 1);
                model_name = model_name.Substring(0, model_name.LastIndexOf('.'));
                string cat_name = "";
                char[] separator = { ' ', '\t' };
                // parent & orignal name
                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separator);
                List<string> parent_names = new List<string>();
                List<string> original_names = new List<string>();
                if (s[0] == '%')
                {
                    for (int i = 1; i < strs.Length; ++i)
                    {
                        parent_names.Add(strs[i]);
                    }
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    for (int i = 1; i < strs.Length; ++i)
                    {
                        original_names.Add(strs[i]);
                    }
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    if (s[0] == '%')
                    {
                        cat_name = strs[strs.Length - 1];
                        s = sr.ReadLine().Trim();
                        strs = s.Split(separator);
                    }
                }
                // n parts
                int n = 0;
                try
                {
                    n = Int16.Parse(strs[0]);
                }
                catch (System.FormatException)
                {
                    MessageBox.Show("Wrong data format - need to know #n parts.");
                    return null;
                }
                List<Part> parts = new List<Part>();
                string folder = filename.Substring(0, filename.LastIndexOf('\\'));
                string modelName = filename.Substring(filename.LastIndexOf('\\') + 1);
                modelName = modelName.Substring(0, modelName.LastIndexOf('.'));
                string partfolder = filename.Substring(0, filename.LastIndexOf('.'));
                // load mesh
                string meshName = partfolder + "\\" + modelName + ".obj";
                Mesh modelMesh = null;
                if (File.Exists(meshName))
                {
                    modelMesh = new Mesh(meshName, false);
                }
                // mesh sample points
                string modelSPname = partfolder + "\\" + modelName + ".sp";
                SamplePoints sp = this.loadSamplePoints(modelSPname, modelMesh == null ? 0 : modelMesh.FaceCount);
                if (sp != null && sp._blendColors == null && sp._points!= null)
                {
                    sp._blendColors = new Color[sp._points.Length];
                }
                // functional space
                int fid = 1;
                List<FunctionalSpace> fss = new List<FunctionalSpace>();
                while (true)
                {
                    string fsName = partfolder + "\\" + modelName + "_fs_" + fid.ToString() + ".obj";
                    string fsInfoName = partfolder + "\\" + modelName + "_fs_" + fid.ToString() + ".weight";
                    if (!File.Exists(fsName))
                    {
                        break;
                    }
                    FunctionalSpace fs = this.loadFunctionalSpaceInfo(fsName, fsInfoName);
                    fss.Add(fs);
                    ++fid;
                }
                string modelNameLower = model_name.ToLower();
                for (int i = 0; i < n; ++i)
                {
                    // read a part
                    // bbox vertices:
                    s = sr.ReadLine().Trim(); // description of #i part
                    while (s.Length > 0 && s[0] == '%')
                    {
                        s = sr.ReadLine().Trim();
                    }
                    strs = s.Split(separator);
                    int nVertices = strs.Length / 3;
                    Vector3d[] pnts = new Vector3d[nVertices];
                    for (int j = 0, k = 0; j < nVertices; ++j)
                    {
                        pnts[j] = new Vector3d(double.Parse(strs[k++]), double.Parse(strs[k++]), double.Parse(strs[k++]));
                    }
                    Prism prim = new Prism(pnts);
                    // coord system
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    bool hasPrism = false;
                    if (strs.Length == 9)
                    {
                        hasPrism = true;
                        pnts = new Vector3d[3];
                        for (int j = 0, k = 0; j < 3; ++j)
                        {
                            pnts[j] = new Vector3d(double.Parse(strs[k++]), double.Parse(strs[k++]), double.Parse(strs[k++]));
                        }
                        prim.coordSys = new CoordinateSystem(prim.CENTER, pnts[0], pnts[1], pnts[2]);
                        s = sr.ReadLine().Trim();
                    }
                    // mesh loc:                    
                    while (s.Length > 0 && s[0] == '%')
                    {
                        s = sr.ReadLine().Trim();
                    }
                    string meshFile = folder + s;
                    if (!File.Exists(meshFile))
                    {
                        MessageBox.Show("Mesh does not exist at #" + i.ToString() + ".");
                        return null;
                    }
                    Mesh mesh = new Mesh(meshFile, false);
                    //Part part = new Part(mesh);
                    Part part = hasPrism ? new Part(mesh, prim) : new Part(mesh);
                    part._partName = s.Substring(0, s.LastIndexOf('.'));
                    part._partName = part._partName.Substring(part._partName.LastIndexOf('\\') + 1);
                    string partSPname = partfolder + "\\" + part._partName + ".sp";
                    part._partSP = loadSamplePoints(partSPname, mesh.FaceCount);
                    // part mesh index
                    string partMeshIndexInfoName = partfolder + "\\" + part._partName + ".mi";
                    int[] vertexIndex;
                    int[] faceVertexIndex;
                    this.loadPartMeshIndexInfo(partMeshIndexInfoName, out vertexIndex, out faceVertexIndex);
                    part._VERTEXINDEX = vertexIndex;
                    part._FACEVERTEXINDEX = faceVertexIndex;

                    // refine part name
                    string ppartName = part._partName.ToLower();
                    if (!model_name.StartsWith("gen") && !ppartName.StartsWith(modelNameLower))
                    {
                        // only for the parent shapes
                        part._partName = model_name + "_" + part._partName;
                    }
                    char[] sepChar = { '_' };
                    string[] names = part._partName.Split(sepChar);
                    part._orignCategory = Functionality.getCategory(names[0]);
                    parts.Add(part);
                }
                // NOTE!! the old version including unify, e.g., _SP will be shifted
                Model model = new Model(modelMesh, parts);
                //model.checkInSamplePoints(sp);
                model._funcSpaces = fss.ToArray();
                model._path = filename.Substring(0, filename.LastIndexOf('\\') + 1);
                model._model_name = model_name;
                model._parent_names = parent_names;
                model._original_names = original_names;
                model._CAT = Functionality.getCategory(cat_name);
                model._SP = sp;
                if (model._SP == null)
                {
                    // used for calculating contact point
                    this.reSamplingForANewShape(model);
                }
                model.unify();
                return model;
            }
        }// loadOnePartBasedModel

        public Model loadAPartBasedModelAgent(string filename, bool initializePG)
        {
            if (!File.Exists(filename))
            {
                MessageBox.Show("File does not exist!");
                return null;
            }
            this.foldername = Path.GetDirectoryName(filename);
            this.clearHighlights();
            Model model = this.loadOnePartBasedModel(filename);
            if (model == null)
            {
                return null;
            }
            // try to load the assoicated graph
            string graphName = filename.Substring(0, filename.LastIndexOf('.')) + ".graph";
            if (!File.Exists(graphName))
            {
                model.initializeGraph();
            }
            else
            {
                LoadAGraph(model, graphName, false);
            }
            // try to load the associated part groups
            string pgName = filename.Substring(0, filename.LastIndexOf('.')) + ".pg";
            if (!File.Exists(pgName) && initializePG)
            {
                if (model._GRAPH != null)
                {
                    model._GRAPH.initializePartGroups();
                }
            }
            else
            {
                LoadPartGroupsOfAModelGraph(model._GRAPH, pgName, model._model_name);
            }

            //model.composeMesh();
            //model._MESH.testNormals = new List<Vector3d>();
            //for (int i = 0; i < model._MESH.FaceCount; ++i)
            //{
            //    int vid = model._MESH.FaceVertexIndex[3 * i];
            //    Vector3d v = model._MESH.getVertexPos(vid);
            //    Vector3d nor = model._MESH.getFaceNormal(i);
            //    model._MESH.testNormals.Add(v);
            //    model._MESH.testNormals.Add(v + nor * 0.05);
            //}
            return model;
        }// loadAPartBasedModel

        public void loadAPartBasedModel(string filename)
        {
            this.clearContext();
            this.clearHighlights();
            _currModel = this.loadAPartBasedModelAgent(filename, true);
            this.setUIMode(0);
            // model check
            if (_currModel != null)
            {
                Program.GetFormMain().outputSystemStatus(_currModel._model_name + " loaded.");
                String s = _currModel.graphValidityCheck();
                Program.GetFormMain().outputSystemStatus(s);
                this.getModelMainFuncs(_currModel);
            }
            this.Refresh();
        }// loadAPartBasedModel

        public void importPartBasedModel(string[] filenames)
        {
            if (filenames == null || filenames.Length == 0)
            {
                MessageBox.Show("No model loaded!");
                return;
            }
            this.clearHighlights();
            if (_currModel == null)
            {
                _currModel = new Model();
            }
            foreach (string file in filenames)
            {
                Model m = loadOnePartBasedModel(file);
                foreach (Part p in m._PARTS)
                {
                    _currModel.addAPart(p);
                }
            }
            // rebuild the graph
            _currModel.initializeGraph();
            this.Refresh();
        }// importPartBasedModel

        public List<ModelViewer> loadPartBasedModels(string segfolder)
        {
            if (!Directory.Exists(segfolder))
            {
                MessageBox.Show("Directory does not exist!");
                return null;
            }
            this.foldername = segfolder;
            this.clearContext();
            this.clearHighlights();
            string[] files = Directory.GetFiles(segfolder, "*.pam");
            int idx = 0;
            Program.writeToConsole("Loading all " + files.Length.ToString() + " models...");
            _ancesterModels = new List<Model>();
            // when load a new set, reset the dictionary 
            _modelIndex = 0;
            foreach (string file in files)
            {
                string modelName = file.Substring(file.LastIndexOf('\\') + 1);
                modelName = modelName.Substring(0, modelName.LastIndexOf('.'));
                Program.writeToConsole("Loading Model info #" + (idx+1).ToString() + " " + modelName + "...");
                Model m = loadOnePartBasedModel(file);
                if (m != null)
                {
                    _ancesterModels.Add(m);
                    // load graph
                    string graphName = file.Substring(0, file.LastIndexOf('.')) + ".graph";
                    LoadAGraph(m, graphName, false);
                    if (m._GRAPH == null)
                    {
                        MessageBox.Show("Cannot find the GRAPH Info of model: " + modelName);
                        return _ancesterModelViewers;
                    }
                    // load part groups
                    string pgName = file.Substring(0, file.LastIndexOf('.')) + ".pg";
                    LoadPartGroupsOfAModelGraph(m._GRAPH, pgName, m._model_name);
                    if (m._GRAPH._partGroups == null)
                    {
                        MessageBox.Show("Cannot find the PART GROUP Info of model: " + modelName);
                        return _ancesterModelViewers;
                    }

                    m._index = idx;
                    foreach (PartGroup pg in m._GRAPH._partGroups)
                    {
                        pg._ParentModelIndex = idx;
                    }

                    ModelViewer modelViewer = new ModelViewer(m, idx++, this, 0); // ancester
                    _ancesterModelViewers.Add(modelViewer);
                    ++_modelIndex;
                }
            }
            // map part names to int
            int id = 0;
            this.partNameToInteger = new Dictionary<string, int>();
            foreach (Model m in _ancesterModels)
            {
                foreach (Part p in m._PARTS)
                {
                    this.partNameToInteger.Add(p._partName, id++);
                }
            }
            // the number of a subset could be from 1 - n
            // a subset is a set of parts from the original input set
            this.partCombinationMemory = new List<PartFormation>[partNameToInteger.Count];
            foreach (Model m in _ancesterModels)
            {
                m._partForm = this.tryCreateANewPartFormation(m._PARTS, 1.0);
            }
            // set the default model as the last one
            if (_ancesterModelViewers.Count > 0)
            {
                this.setCurrentModel(_ancesterModelViewers[_ancesterModelViewers.Count - 1]._MODEL, _ancesterModelViewers.Count - 1);
            }
            this.readModelModelViewMatrix(foldername + "\\view.mat");

            this.setUIMode(0);

            // pre-process models
            //this.preProcessInputSet(_ancesterModels);
            this.calculatePartGroupCompatibility(_ancesterModels);

            return _ancesterModelViewers;
        }// loadPartBasedModels

        public void batchLoadPartGroupScores(string segfolder)
        {
            if (!Directory.Exists(segfolder))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            this.decideWhichToDraw(true, false, false, true, false, false);
            this.foldername = segfolder;
            this.setUIMode(0);
            string[] files = Directory.GetFiles(segfolder, "*.pam");
            int idx = 0;
            Program.writeToConsole("Loading all " + files.Length.ToString() + " models...");
            char[] separator = { '_' };
            int n = _validityMatrixPG.NRow;
            _validityMatrixPG = new SparseMatrix(n, n);
            // record the top N ranked shapes
            int topN = 20;
            string[] topVailityModels = new string[topN];
            double[] topNValidityValues = new double[topN];
            double[] topNNoveltyValues = new double[topN];
            string[] topNoveltyModels = new string[topN];
            foreach (string file in files)
            {
                string model_name = file.Substring(file.LastIndexOf('\\') + 1);
                model_name = model_name.Substring(0, model_name.LastIndexOf('.'));
                Program.writeToConsole("Loading Model info @" + (idx + 1).ToString() + " " + model_name + "...");
                if (file.Contains("invalid") || file.Contains("obstructed"))
                {
                    continue;
                }
                string graphName = file.Substring(0, file.LastIndexOf('.')) + ".graph";
                double[] vals = this.readOnlyFuncValues(graphName);
                double[] probs = new double[Functionality._NUM_CATEGORIY];
                for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
                {
                    double[] probArr = this.getProbabilityForACat(i, vals[i]);
                    probs[i] = probArr[2];
                }
                string[] strs = model_name.Split(separator);
                int pg1 = int.Parse(strs[5]);
                int pg2 = int.Parse(strs[6]);
                Model parent1 = _ancesterModels[_partGroups[pg1][0]._ParentModelIndex];
                Model parent2 = _ancesterModels[_partGroups[pg2][0]._ParentModelIndex];
                int cid1 = (int)parent1._GRAPH._functionalityValues._parentCategories[0];
                int cid2 = (int)parent2._GRAPH._functionalityValues._parentCategories[0];
                double maxValidity = Math.Max(probs[cid1], probs[cid2]);
                List<int> parentCatIds = new List<int>();
                parentCatIds.Add(cid1);
                parentCatIds.Add(cid2);
                double novelty = this.getNoveltyValue(probs, parentCatIds);
                if (maxValidity > 0.5)
                {
                    double val = maxValidity;// * novelty;
                    _validityMatrixPG.AddTriplet(pg1, pg2, val);
                }
                insertAVal(topVailityModels, topNValidityValues, maxValidity, model_name);
                insertAVal(topNoveltyModels, topNNoveltyValues, novelty * maxValidity, model_name);
            }
            string validityMatrixFolder = Interface.MODLES_PATH + "ValidityMatrix\\";
            string validityMatrixFileName = validityMatrixFolder + "Set_1_dec_gen_" + _currGenId.ToString() + ".vdm";
            this.saveValidityMatrix(validityMatrixFileName);

            string today = DateTime.Today.ToString("MMdd");
            validityMatrixFileName = validityMatrixFolder + "Set_1_dec_" + today + ".vdm";
            this.saveValidityMatrix(validityMatrixFileName);

            // save the models
            string topValidityFolder = segfolder.Substring(0, segfolder.LastIndexOf('\\') + 1) + "topValidity\\";
            string topNoveltyFolder = segfolder.Substring(0, segfolder.LastIndexOf('\\') + 1) + "topNovelty\\";
            if (!Directory.Exists(topValidityFolder))
            {
                Directory.CreateDirectory(topValidityFolder);
            }
            if (!Directory.Exists(topNoveltyFolder))
            {
                Directory.CreateDirectory(topNoveltyFolder);
            }
            for (int i = 0; i < topN; ++i)
            {
                int rank = topN - i;
                string filename = topValidityFolder + "Rank_" + rank.ToString() + "_" + topVailityModels[i] + ".png";
                this.loadAPartBasedModel(segfolder + "\\" + topVailityModels[i] + ".pam");
                _currModel._GRAPH._functionalityValues._validityVal = topNValidityValues[i];
                Program.GetFormMain().writePostAnalysisInfo(this.getFunctionalityValuesString(_currModel, false));
                this.captureScreen(filename);

                string noveltyFilename = topNoveltyFolder + "Rank_" + rank.ToString() + "_" + topNoveltyModels[i] + ".png";
                this.loadAPartBasedModel(segfolder + "\\" + topNoveltyModels[i] + ".pam");
                _currModel._GRAPH._functionalityValues._noveltyVal = topNNoveltyValues[i];
                Program.GetFormMain().writePostAnalysisInfo(this.getFunctionalityValuesString(_currModel, false));
                this.captureScreen(noveltyFilename);
            }
        }// loadPartBasedModels

        private void insertAVal(string[] modelNames, double[] values, double val, string modelName)
        {
            int i = 0;
            for (; i < values.Length; ++ i)
            {
                if (val < values[i])
                {
                    break;
                }
            }
            if (i <= 0)
            {
                return;
            }
            for (int j = 0; j < i - 1; ++j)
            {
                values[j] = values[j + 1];
                modelNames[j] = modelNames[j + 1];
            }
            values[i - 1] = val;
            modelNames[i - 1] = modelName;
        }// insertAVal

        private double[] readOnlyFuncValues(string filename)
        {
            double[] res = new double[Functionality._NUM_CATEGORIY];
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t' };
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine().Trim();
                    if (!s.StartsWith("Backpack"))
                    {
                        continue;
                    }
                    string[] strs = s.Split(separator);
                    int catId = (int)Functionality.getCategory(strs[0]);
                    res[catId] = double.Parse(strs[1]);
                    for (int i = 1; i < Functionality._NUM_CATEGORIY; ++i)
                    {
                        s = sr.ReadLine().Trim();
                        strs = s.Split(separator);
                        catId = (int)Functionality.getCategory(strs[0]);
                        if (catId == (int)Functionality.Category.None)
                        {
                            break;
                        }
                        if (catId < Functionality._NUM_CATEGORIY )
                        { 
                            double val = 0;
                            double.TryParse(strs[1], out val);
                            res[catId] = val;
                        }
                    }
                }
            }
            return res;
        }// readOnlyFuncValues

        public void batchLoadTest(string segfolder)
        {
            if (!Directory.Exists(segfolder))
            {
                MessageBox.Show("Directory does not exist!");
                return;
            }
            this.foldername = segfolder;
            this.clearContext();
            this.clearHighlights();
            this.readModelModelViewMatrix(foldername + "\\view.mat");
            this.decideWhichToDraw(true, false, false, true, false, false);
            this.setUIMode(0);
            string[] files = Directory.GetFiles(segfolder, "*.pam");
            int idx = 0;
            Program.writeToConsole("Loading all " + files.Length.ToString() + " models...");
            foreach (string file in files)
            {
                Program.writeToConsole("Loading Model info @" + (idx + 1).ToString() + "...");
                Model m = loadOnePartBasedModel(file);
                if (m != null)
                {
                    string graphName = file.Substring(0, file.LastIndexOf('.')) + ".graph";
                    LoadAGraph(m, graphName, false);
                    string pgName = file.Substring(0, file.LastIndexOf('.')) + ".pg";
                    LoadPartGroupsOfAModelGraph(m._GRAPH, pgName, m._model_name);
                    if (m._GRAPH != null && m._GRAPH._partGroups != null)
                    {
                        m._GRAPH._partGroups.Add(new PartGroup(new List<Node>(), 0));
                        foreach (PartGroup pg in m._GRAPH._partGroups)
                        {
                            pg._ParentModelIndex = _modelIndex;
                        }
                    }
                    m.unify();
                    m.composeMesh();
                    m._index = idx;
                    this.setCurrentModel(m, idx);
                    this.runProabilityTest();
                    ++_modelIndex;
                }
            }

        }// loadPartBasedModels

        private void preProcessInputSet(List<Model> models)
        {
            // Given n input shapes, do:
            if (models.Count == 0)
            {
                return;
            }
            _inputSetCats = new List<int>();
            _inputSetThreshholds = new List<double>();
            _functionalPartScales = new Dictionary<string, Vector3d>();
            _nodeFSAs = new List<NodeFunctionalSpaceAgent>();
            // 1. load all part groups
            // 2. normalize all weights per category
            int ndim = Functionality._TOTAL_FUNCTONAL_PATCHES;
            double[] minw = new double[ndim];
            double[] maxw = new double[ndim];
            for (int i = 0; i < ndim; ++i)
            {
                minw[i] = double.MaxValue;
                maxw[i] = double.MinValue;
            }
            foreach (Model model in models)
            {
                int patchId = 0;
                for (int n = 0; n < model._GRAPH._NNodes; ++n)
                {
                    Node node = model._GRAPH._NODES[n];
                    SamplePoints sp = node._PART._partSP;
                    if (sp != null)
                    {
                        node._PART._partSP._highlightedColors = new Color[Functionality._NUM_CATEGORIY][];
                        for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
                        {
                            node._PART._partSP._highlightedColors[i] = new Color[node._PART._partSP._points.Length];
                            for (int j = 0; j < node._PART._partSP._points.Length; ++j)
                            {
                                node._PART._partSP._highlightedColors[i][j] = Color.LightGray;
                            }
                        }
                    }
                    if (node._functionalSpaceAgent != null)
                    {
                        NodeFunctionalSpaceAgent nfsa = new NodeFunctionalSpaceAgent(node._PART._partName, node._functionalSpaceAgent);
                        _nodeFSAs.Add(nfsa);
                    }
                }
                for (int c = 0; c < Functionality._NUM_CATEGORIY; ++c)
                {
                    int nPatches = Functionality.getNumberOfFunctionalPatchesPerCategory((Functionality.Category)c);
                    List<List<double>> weights = new List<List<double>>();
                    for (int j = 0; j < nPatches; ++j)
                    {
                        weights.Add(new List<double>());
                    }
                    int totalPoints = 0;
                    for (int n = 0; n < model._GRAPH._NNodes; ++n)
                    {
                        Node node = model._GRAPH._NODES[n];
                        SamplePoints sp = node._PART._partSP;
                        if (sp == null)
                        {
                            continue;
                        }
                        PatchWeightPerCategory pw = sp._weightsPerCat[c];
                        for (int i = 0; i < sp._points.Length; ++i)
                        {
                            for (int j = 0; j < nPatches; ++j)
                            {
                                weights[j].Add(pw._weights[i, j]);
                            }
                        }
                        totalPoints += sp._points.Length;
                    }
                    double[] threshes = new double[nPatches];
                    double[] lowest = new double[nPatches];
                    int topNs = (int)(0.7 * totalPoints);
                    for (int j = 0; j < nPatches; ++j)
                    {
                        weights[j].Sort();
                        if (weights[j].Count > 0)
                        {
                            threshes[j] = weights[j][topNs];
                        }
                    }
                    for (int n = 0; n < model._GRAPH._NNodes; ++n)
                    {
                        Node node = model._GRAPH._NODES[n];
                        SamplePoints sp = node._PART._partSP;
                        if (sp == null)
                        {
                            continue;
                        }
                        PatchWeightPerCategory pw = sp._weightsPerCat[c];
                        int nPoints = pw._weights.GetLength(0);
                        int[] numSalientPoints = new int[nPatches];
                        double maxWeight = double.MinValue;
                        double minWeight = double.MaxValue;
                        for (int j = 0; j < nPatches; ++j)
                        {
                            for (int i = 0; i < nPoints; ++i)
                            {
                                maxWeight = Math.Max(maxWeight, pw._weights[i, j]);
                                minWeight = Math.Min(minWeight, pw._weights[i, j]);
                            }
                        }
                        for (int j = 0; j < nPatches; ++j)
                        {
                            for (int i = 0; i < nPoints; ++i)
                            {
                                // set highlight colors
                                double ratio = (pw._weights[i, j] - minWeight) / (maxWeight - minWeight);
                                if (ratio > 0.5)
                                {
                                    ++numSalientPoints[j];
                                }
                                if (ratio < 0.1)
                                {
                                    continue;
                                }
                                Color color = GLDrawer.getColorGradient(ratio, j);
                                byte[] color_array = GLDrawer.getColorArray(color, 255);
                                node._PART._partSP._highlightedColors[c][i] = GLDrawer.getColorRGB(color_array);
                            }
                            if (numSalientPoints[j] > nPoints * 0.2)
                            {
                                node._isFunctionalPatch[patchId + j] = true;
                            }
                            else
                            {
                                node._isFunctionalPatch[patchId + j] = false;
                            }
                        }
                        int pIdx = 0;
                        int maxNum = 0;
                        for (int j = 0; j < nPatches; ++j)
                        {
                            if (numSalientPoints[j] > maxNum)
                            {
                                maxNum = numSalientPoints[j];
                                pIdx = j;
                            }
                        }
                        node._isFunctionalPatch[patchId + pIdx] = true;
                        node._PART._highlightColors[c] = GLDrawer.getColorPatch(pIdx);
                    }// node
                    patchId += nPatches;
                }// category
            }// model
            foreach (Model model in models)
            {
                if (model._GRAPH == null || model._GRAPH._partGroups.Count == 0 || model._GRAPH._functionalityValues == null)
                {
                    //MessageBox.Show("Model #" + model._model_name + " lack catgory info or part groups.");
                    continue;
                }
                foreach (Functionality.Category cat in model._GRAPH._functionalityValues._parentCategories)
                {
                    int cid = (int)cat;
                    if (!_inputSetCats.Contains(cid) && Functionality.isKnownCategory(cid))
                    {
                        _inputSetCats.Add(cid);
                    }
                }
                // correct name conflict
                bool needSave = false;
                for (int i = 0; i < model._GRAPH._NODES.Count - 1; ++i)
                {
                    string i_part_name = model._GRAPH._NODES[i]._PART._partName;
                    int id = 1;
                    for (int j = i + 1; j < model._GRAPH._NODES.Count; ++j)
                    {
                        string j_part_name = model._GRAPH._NODES[j]._PART._partName;
                        if (j_part_name.Equals(i_part_name))
                        {
                            model._GRAPH._NODES[j]._PART._partName = j_part_name + "_" + id.ToString();
                            ++id;
                            needSave = true;
                        }
                    }
                }
                if (needSave)
                {
                    this.saveAPartBasedModel(model, model._path + model._model_name + ".pam", true);
                }

                foreach (Node node in model._GRAPH._NODES)
                {
                    if (node._funcs.Contains(Functionality.Functions.PLACEMENT))
                    {
                        string part_name = node._PART._partName;
                        node.calRatios();
                        _functionalPartScales.Add(part_name, node._ratios);
                    }
                    SamplePoints sp = node._PART._partSP;
                    if (sp == null)
                    {
                        continue;
                    }
                    int d = 0;
                    for (int c = 0; c < Functionality._NUM_CATEGORIY; ++c)
                    {
                        for (int i = 0; i < sp._weightsPerCat[c]._nPatches; ++i)
                        {
                            for (int j = 0; j < sp._weightsPerCat[c]._nPoints; ++j)
                            {
                                minw[d + i] = Math.Min(minw[d + i], sp._weightsPerCat[c]._weights[j, i]);
                                maxw[d + i] = Math.Max(maxw[d + i], sp._weightsPerCat[c]._weights[j, i]);
                            }
                        }
                        d += sp._weightsPerCat[c]._nPatches;
                    }
                }
            }
            // normalize
            double[] diffw = new double[ndim];
            for (int i = 0; i < ndim; ++i)
            {
                diffw[i] = maxw[i] - minw[i];
                _inputSetThreshholds.Add(minw[i] + diffw[i] / 2);
            }
            //foreach (Model model in models)
            //{
            //    foreach (Node node in model._GRAPH._NODES)
            //    {
            //        SamplePoints sp = node._PART._partSP;
            //        int d = 0;
            //        for (int c = 0; c < Functionality._NUM_CATEGORIY; ++c)
            //        {
            //            for (int i = 0; i < sp._weightsPerCat[c]._nPatches; ++i)
            //            {
            //                for (int j = 0; j < sp._weightsPerCat[c]._nPoints; ++j)
            //                {
            //                    double old = sp._weightsPerCat[c]._weights[j, i];
            //                    sp._weightsPerCat[c]._weights[j, i] = (old - minw[d + i]) / diffw[d + i];
            //                }
            //            }
            //            d += sp._weightsPerCat[c]._nPatches;
            //        }
            //    }
            //}
            
            // 2. analyze the feature vector for each part group
            int nPGs = 0;
            _partGroups = new List<List<PartGroup>>();
            foreach(Model m in models)
            {
                if (m._GRAPH._partGroups.Count == 0)
                {
                    m._GRAPH.initializePartGroups();
                }
                nPGs += m._GRAPH._partGroups.Count;
                foreach (PartGroup pg in m._GRAPH._partGroups)
                {
                    // recompute after having the thresholds
                    pg.computeFeatureVector(_inputSetThreshholds);
                    pg._ParentModelIndex = m._index;
                    // at the beginning, a part group only maps to itself
                    // after a few generations, it will map to its cloned part groups
                    List<PartGroup> pgs = new List<PartGroup>();
                    pgs.Add(pg);
                    _partGroups.Add(pgs);
                }
            }
            int nps = _partGroups.Count;
            _nPairsPG = nps * (nps - 1) / 2 - nps;
            _validityMatrixPG = new SparseMatrix(nPGs, nPGs);

            //List<double> sorted = new List<double>();
            //_validityMatrixPG = new SparseMatrix(nPGs, nPGs);
            //for (int i = 0; i < nPGs - 1; ++i)
            //{
            //    //_validityMatrixPG.AddTriplet(i, i, 3.0);
            //    for (int j = i + 1; j < nPGs; ++j)
            //    {
            //        if (_partGroups[i][0]._ParentModelIndex == _partGroups[j][0]._ParentModelIndex)
            //        {
            //            continue;
            //        }                        
            //        double simdist = compareTwoPartGroups(_partGroups[i][0], _partGroups[j][0]);
            //        _validityMatrixPG.AddTriplet(i, j, simdist);
            //        _validityMatrixPG.AddTriplet(j, i, simdist);
            //        sorted.Add(simdist);
            //        StringBuilder sb = new StringBuilder();
            //        sb.Append("Two part groups: \n");
            //        sb.Append(this.getPartGroupNames(_partGroups[i][0]));
            //        sb.Append("\n");
            //        sb.Append(this.getPartGroupNames(_partGroups[j][0]));
            //        sb.Append("\n");
            //        sb.Append("Similarity value: " + simdist.ToString());
            //        Program.writeToConsole(sb.ToString());
            //    }
            //}
            //sorted.Sort();

            // 3. load knowledge base - binary features
            this.loadTrainedInfo();

            // 4. set up the current generation

            _modelLibrary = new List<Model>(models);
            _currGenId = 1;
            for (int i = 0; i < _modelLibrary.Count; ++i)
            {
                if (!_modelIndexMap.ContainsKey(i))
                {
                    _modelIndexMap.Add(i, _modelLibrary[i]);
                    _modelViewIndex++;
                }
            }
            _userSelectedModels = new List<Model>(models);

            // 5. compute binary feature for known category models
            //foreach (Model m in models)
            //{
            //    m._GRAPH._functionalityValues._funScores = this.evaluateFeaturesOfAModel(m);
            //}
            // TEST
            //this.rankOffspringByICONfeatures(models);
        }// preProcessInputSet

        private void loadAllCategoryScores()
        {
            string scoreFolder = Interface.MODLES_PATH + "categoryScores\\";
            string[] files = Directory.GetFiles(scoreFolder, "*.score");
            int nShapes = files.Length;
            int[] index = new int[Functionality._NUM_CATEGORIY];
            string catName = Functionality.getCategoryName(0);
            int catId = 0;
            double[,] scores = new double[nShapes, Functionality._NUM_CATEGORIY];
            for (int i = 0; i < nShapes; ++i)
            {
                string filename = files[i];
                double[] score = this.loadAScoreFile(filename);
                if (!filename.Contains(catName))
                {
                    ++catId;
                    index[catId] = i;
                    catName = Functionality.getCategoryName(catId);
                }
                for (int j = 0; j < score.Length; ++j)
                {
                    scores[i, j] = score[j];
                }
            }
            this.analyzeBetaDistribution(scores, index);
        }// loadAllCategoryScores

        BetaDistribution[] bd_inClass = new BetaDistribution[Functionality._NUM_CATEGORIY];
        BetaDistribution[] bd_outClass = new BetaDistribution[Functionality._NUM_CATEGORIY];

        private double correctZeroProb(double prob)
        {
            if (prob == 0)
            {
                return 0.01;
            }
            return prob;
        }
        private void analyzeBetaDistribution(double[,] scores, int[] startIds)
        {
            int nShapes = scores.GetLength(0);
            bd_inClass = new BetaDistribution[Functionality._NUM_CATEGORIY];
            bd_outClass = new BetaDistribution[Functionality._NUM_CATEGORIY];
            double thr = 0.05;
            for (int c = 0; c < Functionality._NUM_CATEGORIY; ++c)
            {
                string catName = Functionality.getCategoryName(c);
                List<double> inClass = new List<double>();
                List<double> outClass = new List<double>();
                int start = startIds[c];
                int end = (c == Functionality._NUM_CATEGORIY - 1 ? nShapes : startIds[c + 1]);
                double maxOut = double.MinValue;
                double minIn = double.MaxValue;
                double shrink = 0.98;
                if (c == 1 || c == 9)
                {
                    shrink = 0.96;
                }
                for (int i = 0; i < start; ++i)
                {
                    double val = scores[i, c] * shrink;
                    outClass.Add(val);
                    maxOut = maxOut > val ? maxOut : val;
                }
                for (int i = start; i < end; ++i)
                {
                    double val = scores[i, c] * shrink;
                    inClass.Add(val);
                    minIn = minIn < val ? minIn : val;
                }
                for (int i = end; i < nShapes; ++i)
                {
                    double val = scores[i, c] * shrink;
                    outClass.Add(val);
                    maxOut = maxOut > val ? maxOut : val;
                }
                // optim
                double ratio = 1;
                if (maxOut - minIn >= thr)
                {
                    ratio = (minIn + thr) / maxOut;
                }
                //for (int i = 0; i < outClass.Count; ++i)
                //{
                //    outClass[i] = outClass[i] * ratio;
                //}
                bd_inClass[c] = new BetaDistribution(0, 1);
                bd_inClass[c].Fit(inClass.ToArray());
                bd_outClass[c] = new BetaDistribution(0, 1);
                bd_outClass[c].Fit(outClass.ToArray());
            }
        }// analyzeDistribution

        private double[] loadAScoreFile(string filename)
        {
            if (filename == null || !File.Exists(filename))
            {
                return null;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t', ',' };
                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separator);
                List<double> res = new List<double>();
                for (int i = 0; i < strs.Length; ++i)
                {
                    double val;
                    if (double.TryParse(strs[i], out val))
                    {
                        res.Add(val);
                    }
                }
                return res.ToArray();
            }
        }// loadAScoreFile

        private void loadTrainedInfo()
        {
            this.loadTrainedFeautres();
            this.loadAllCategoryScores();
            // update inpnt cats
            if (_inputSetCats == null || _inputSetCats.Count == 0)
            {
                _inputSetCats = new List<int>();
                int[] excluded = { 0, 2, 7, 8, 10, 11, 12, 13, 14 };
                List<int> excludedList = new List<int>(excluded);
                for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
                {
                    if (!excludedList.Contains(i) && Functionality.isKnownCategory(i))
                    {
                        _inputSetCats.Add(i);
                    }
                }
            }
        }
        private void loadTrainedFeautres()
        {
            string featureFolder = Interface.MODLES_PATH + "patchFeature\\";
            string weightFolder = Interface.MODLES_PATH + "featureWeight\\";
            _trainingFeaturesPerCategory = new List<TrainedFeaturePerCategory>();
            for (int c = 0; c < Functionality._NUM_CATEGORIY; ++c)
            {
                string catName = Functionality.getCategoryName(c);
                string folder = featureFolder + catName + "\\";
                if (!Directory.Exists(folder))
                {
                    MessageBox.Show("Missing binary feature folder: " + catName);
                    return;
                }
                TrainedFeaturePerCategory tf = new TrainedFeaturePerCategory((Functionality.Category)c);   
                // unary
                for (int i = 0; i < tf._nPatches; ++i)
                {
                    string filename = folder + catName + "_funcpatch_" + i.ToString() +"_unary_feature_gt.csv";
                    if (!File.Exists(filename))
                    {
                        MessageBox.Show("Missing binary feature data: " + catName);
                        return;
                    }
                    double[,] res = this.loadTrainedFeaturePerCategory(filename, Functionality._NUM_UNARY_FEATURE);
                    tf._unaryF.Add(res);
                }
                // binary
                for (int i = 0; i < tf._nPatches; ++i)
                {
                    for (int j = i; j < tf._nPatches; ++j)
                    {
                        string filename = folder + catName + "_pair_" + i.ToString() + "_" + j.ToString() + "_binary_feature_gt.csv";
                        if (!File.Exists(filename))
                        {
                            MessageBox.Show("Missing binary feature data: " + catName);
                            return;
                        }
                        double[,] res = this.loadTrainedFeaturePerCategory(filename, Functionality._NUM_BINARY_FEATURE);
                        tf._binaryF.Add(res);
                    }
                }
                // feature weights
                string featureFileName = weightFolder + catName + "_weight_optimal.csv";
                tf.weights = loadFeatureWeights(featureFileName);
                _trainingFeaturesPerCategory.Add(tf);
            }
        }// loadTrainedFeautres

        private double[] loadFeatureWeights(string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t', ',' };
                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separator);
                double[] res = new double[strs.Length];
                for (int j = 0; j < strs.Length; ++j)
                {
                    res[j] = double.Parse(strs[j]);
                }
                return res;
            }
        }// loadFeatureWeights

        private double[,] loadTrainedFeaturePerCategory(string filename, int ndim)
        {
            double[,] res = null;
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t', ',' };
                for (int i = 0; i < ndim; ++i)
                {
                    string s = sr.ReadLine().Trim();
                    string[] strs = s.Split(separator);
                    if (i == 0)
                    {
                        res = new double[ndim, strs.Length];
                    }
                    for (int j = 0; j < strs.Length; ++j)
                    {
                        res[i, j] = double.Parse(strs[j]);
                    }
                }
            }
            return res;
        }// loadTrainedFeaturePerCategory

        private double compareTwoPartGroups(PartGroup pg1, PartGroup pg2)
        {
            double simdist = 0;
            double max = 3;
            //int n = pg1._featureVector.Length;
            //for (int i = 0; i < n; ++i)
            //{
            //    double d = pg1._featureVector[i] - pg2._featureVector[i];
            //    simdist += d * d;
            //}
            //simdist /= n;

            int ndim = Functionality._TOTAL_FUNCTONAL_PATCHES;
            int d = 0;
            for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
            {
                int np = Functionality.getNumberOfFunctionalPatchesPerCategory((Functionality.Category)i);
                if (_inputSetCats.Contains(i))
                {
                    for (int j = 0; j < np; ++j)
                    {
                        double dist = pg1._featureVector[j + d] - pg2._featureVector[j + d];
                        simdist += Math.Abs(dist);
                    }
                }
                d += np;
            }
            if (pg1._NODES.Count == 0 || pg2._NODES.Count == 0)
            {
                simdist = max;
            }
            return max - simdist;
        }// compareTwoPartGroups

        public string loadAShapeNetModel(string foldername)
        {
            if (!Directory.Exists(foldername))
            {
                return "";
            }
            // load points
            string points_folder = foldername + "\\points";
            string expert_lable_folder = foldername + "\\expert_verified\\points_label\\";
            string obj_folder = foldername + "\\objs\\";
            string[] points_files = Directory.GetFiles(points_folder);
            int idx = 0;
            int max_idx = 1;
            string points_name = foldername.Substring(foldername.LastIndexOf('\\') + 1);
            this._meshClasses = new List<MeshClass>();
            foreach (String file in points_files)
            {
                if (!file.EndsWith(".pts"))
                {
                    continue;
                }
                // find if there is a .seg file (labeling)
                string mesh_name = file.Substring(file.LastIndexOf('\\') + 1);
                mesh_name = mesh_name.Substring(0, mesh_name.LastIndexOf('.'));
                string seg_file = mesh_name + ".seg";
                seg_file = expert_lable_folder + seg_file;
                string obj_file = obj_folder + mesh_name + "\\" + "model.obj";
                if (!File.Exists(seg_file))
                {
                    continue;
                }
                //Mesh m = loadPointCloud(file, seg_file);
                if (!File.Exists(obj_file))
                {
                    continue;
                }
                Mesh m = loadPointCloud(file, obj_file, seg_file);
                if (m != null)
                {
                    this.currMeshClass = new MeshClass(m);
                    this.currMeshClass._MESHNAME = mesh_name;
                    this._meshClasses.Add(this.currMeshClass);
                    ++idx;
                }
                if (idx >= max_idx)
                {
                    break;
                }
            }
            if (this._meshClasses.Count > 0)
            {
                this.currMeshClass = this._meshClasses[0];
            }
            // only points
            this.setRenderOption(1);
            this.meshIdx = -1;
            string str = nextMeshClass();
            return str;
        }// loadAShapeNetModel

        private Mesh loadPointCloud(string points_file, string seg_file)
        {
            if (!File.Exists(points_file) || !File.Exists(seg_file))
            {
                return null;
            }
            char[] separator = { ' ', '\t' };
            List<double> vertices = new List<double>();
            List<byte> colors = new List<byte>();
            using (StreamReader sr = new StreamReader(points_file))
            {
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separator);
                    if (strs.Length >= 3)
                    {
                        for (int i = 0; i < 3; ++i)
                        {
                            vertices.Add(double.Parse(strs[i]));
                        }
                    }
                }
            }
            using (StreamReader sr = new StreamReader(seg_file))
            {
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separator);
                    if (strs.Length > 0)
                    {
                        int label = int.Parse(strs[0]);
                        Color c = GLDrawer.ColorSet[label];
                        colors.Add(c.R);
                        colors.Add(c.G);
                        colors.Add(c.B);
                    }
                }
                Mesh m = new Mesh(vertices.ToArray(), colors.ToArray());
                return m;
            }
        }// loadPointCloud

        private Mesh loadPointCloud(string points_file, string obj_file, string seg_file)
        {
            if (!File.Exists(obj_file) || !File.Exists(seg_file))
            {
                return null;
            }
            char[] separator = { ' ', '\t' };
            List<double> vertices = new List<double>();
            List<byte> colors = new List<byte>();
            using (StreamReader sr = new StreamReader(points_file))
            {
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separator);
                    if (strs.Length >= 3)
                    {
                        for (int i = 0; i < 3; ++i)
                        {
                            vertices.Add(double.Parse(strs[i]));
                        }
                    }
                }
            }
            Mesh m = new Mesh(obj_file, false);
            using (StreamReader sr = new StreamReader(seg_file))
            {
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separator);
                    if (strs.Length > 0)
                    {
                        int label = int.Parse(strs[0]);
                        Color c = GLDrawer.ColorSet[label];
                        colors.Add(c.R);
                        colors.Add(c.G);
                        colors.Add(c.B);
                    }
                }
            }
            if (m.VertexCount != vertices.Count / 3)
            {
                return null;
            }
            return m;
        }// loadPointCloud

        public void saveReplaceablePairs()
        {
            if (_crossOverBasket.Count < 2)
            {
                return;
            }
            Model model_i = _crossOverBasket[_crossOverBasket.Count - 2];
            Model model_j = _crossOverBasket[_crossOverBasket.Count - 1];
            Graph graph_i = model_i._GRAPH;
            Graph graph_j = model_j._GRAPH;
            if (graph_i == null || graph_j == null || graph_i.selectedNodePairs.Count != graph_j.selectedNodePairs.Count)
            {
                return;
            }
            string filename = model_i._path + model_i._model_name + "_" + model_j._model_name + ".corr";
            using (StreamWriter sw = new StreamWriter(filename))
            {
                int n = graph_i.selectedNodePairs.Count;
                sw.WriteLine(n.ToString());
                for (int i = 0; i < n; ++i)
                {
                    for (int j = 0; j < graph_i.selectedNodePairs[i].Count; ++j)
                    {
                        sw.Write(graph_i.selectedNodePairs[i][j]._INDEX.ToString() + " ");
                    }
                    sw.WriteLine();
                    for (int j = 0; j < graph_j.selectedNodePairs[i].Count; ++j)
                    {
                        sw.Write(graph_j.selectedNodePairs[i][j]._INDEX.ToString() + " ");
                    }
                    sw.WriteLine();
                }
            }
        }// saveLoadReplaceablePairs

        private void tryLoadReplaceablePairs()
        {
            if (_ancesterModelViewers.Count == 0)
            {
                return;
            }
            int n = _ancesterModelViewers.Count;
            _replaceablePairs = new ReplaceablePair[n, n];
            for (int i = 0; i < n - 1; ++i)
            {
                Model model_i = _ancesterModelViewers[i]._MODEL;
                Graph graph_i = _ancesterModelViewers[i]._GRAPH;
                for (int j = i + 1; j < n; ++j)
                {
                    Model model_j = _ancesterModelViewers[j]._MODEL;
                    Graph graph_j = _ancesterModelViewers[j]._GRAPH;
                    string filename = model_i._path + model_i._model_name + "_" + model_j._model_name + ".corr";
                    List<List<int>> pairs_i = new List<List<int>>();
                    List<List<int>> pairs_j = new List<List<int>>();
                    loadReplaceablePair(filename, out pairs_i, out pairs_j);
                    _replaceablePairs[i, j] = new ReplaceablePair(graph_i, graph_j, pairs_i, pairs_j);
                }
            }
        }// tryLoadReplacePairs

        private void loadReplaceablePair(string filename, out List<List<int>> pairs_1, out List<List<int>> pairs_2)
        {
            pairs_1 = new List<List<int>>();
            pairs_2 = new List<List<int>>();
            if (!File.Exists(filename))
            {
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t' };
                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separator);
                int npairs = int.Parse(strs[0]);
                for (int i = 0; i < npairs; ++i)
                {
                    List<int> p1 = new List<int>();
                    List<int> p2 = new List<int>();
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    for (int j = 0; j < strs.Length; ++j)
                    {
                        p1.Add(int.Parse(strs[j]));
                    }
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    for (int j = 0; j < strs.Length; ++j)
                    {
                        p2.Add(int.Parse(strs[j]));
                    }
                    pairs_1.Add(p1);
                    pairs_2.Add(p2);
                }
            }
        }// loadReplaceablePair

        public void LoadPartGroupsOfAModelGraph(Graph graph, string filename, string model_name)
        {
            if (graph == null || !File.Exists(filename))
            {
                return;
            }
            graph._partGroups = new List<PartGroup>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t' };
                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separator);
                int nPGs = int.Parse(strs[0]);
                for (int i = 0; i < nPGs; ++i)
                {
                    s = sr.ReadLine().Trim();
                    // if pg category is stored
                    int cat = -1;
                    if (s.StartsWith("PGset: "))
                    {
                        cat = int.Parse(s.Substring(7));
                        s = sr.ReadLine().Trim();
                    }
                    strs = s.Split(separator);
                    List<Node> nodes = new List<Node>();
                    if (strs[0] != "") // empty
                    {
                        for (int j = 0; j < strs.Length; ++j)
                        {
                            int idx = int.Parse(strs[j]);
                            nodes.Add(graph._NODES[idx]);
                        }
                    }
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    int n = int.Parse(strs[0]);
                    double[] features = new double[n];
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    if (strs.Length < n)
                    {
                        MessageBox.Show("Feature vector data error.");
                        break;
                    }
                    for (int j = 0; j < n; ++j)
                    {
                        features[j] = double.Parse(strs[j]);
                    }
                    PartGroup pg = new PartGroup(nodes, features);
                    pg.pgSet = cat;
                    graph._partGroups.Add(pg);
                }
            }
            //if (graph._partGroups.Count < 2)
            //{
            //    graph.initializePartGroups(model_name);
            //}
        }// LoadPartGroupsOfAModelGraph

        public void savePartGroupsOfAModelGraph(List<PartGroup> pgs, string filename)
        {
            if (pgs == null || pgs.Count == 0)
            {
                return;
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine(pgs.Count.ToString() + " part groups.");
                foreach (PartGroup pg in pgs)
                {
                    StringBuilder sb = new StringBuilder();
                    sw.WriteLine("PGset: " + pg.pgSet);
                    foreach (Node node in pg._NODES)
                    {
                        sb.Append(node._INDEX.ToString());
                        sb.Append(" ");
                    }
                    sw.WriteLine(sb.ToString());
                    int n = pg._featureVector.Length;
                    sw.WriteLine(n.ToString() + " dim of features.");
                    sb = new StringBuilder();
                    for (int i = 0; i < n; ++i)
                    {
                        sb.Append(pg._featureVector[i].ToString());
                        sb.Append(" ");
                    }
                    sw.WriteLine(sb.ToString());
                }
            }
        }// savePartGroupsOfAModelGraph

        public void LoadAGraph(Model m, string filename, bool unify)
        {
            if (m == null || !File.Exists(filename))
            {
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t' };
                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separator);
                int nNodes = int.Parse(strs[0]);
                if (nNodes != m._NPARTS)
                {
                    MessageBox.Show("Unmatched graph nodes and mesh parts.");
                    return;
                }
                Graph g = new Graph();
                List<int> symGroups = new List<int>();
                bool hasGroundTouching = false;
                for (int i = 0; i < nNodes; ++i)
                {
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    int j = int.Parse(strs[0]);
                    int k = int.Parse(strs[1]);
                    Node node = new Node(m._PARTS[i], j);
                    node._isGroundTouching = k == 1 ? true : false;
                    if (node._isGroundTouching)
                    {
                        hasGroundTouching = true;
                        node.addFunctionality(Functionality.Functions.GROUND_TOUCHING);
                    }
                    if (strs.Length > 4)
                    {
                        Color c = Color.FromArgb(int.Parse(strs[2]), int.Parse(strs[3]), int.Parse(strs[4]));
                        node._PART._COLOR = c;
                    }
                    if (strs.Length > 5)
                    {
                        int sym = int.Parse(strs[5]);
                        if (sym > i) // sym != -1
                        {
                            symGroups.Add(i);
                            symGroups.Add(sym);
                        }
                    }
                    if (strs.Length > 6)
                    {
                        for (int f = 6; f < strs.Length; ++f)
                        {
                            Functionality.Functions func = getFunctionalityFromString(strs[f]);
                            node.addFunctionality(func);
                        }
                    }
                    g.addANode(node);
                }
                // add symmetry
                for (int i = 0; i < symGroups.Count; i += 2)
                {
                    g.markSymmtry(g._NODES[symGroups[i]], g._NODES[symGroups[i + 1]]);
                }
                if (!hasGroundTouching)
                {
                    g.markGroundTouchingNodes();
                }
                s = sr.ReadLine().Trim();
                strs = s.Split(separator);
                int nEdges = int.Parse(strs[0]);
                for (int i = 0; i < nEdges; ++i)
                {
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    int j = int.Parse(strs[0]);
                    int k = int.Parse(strs[1]);
                    if (strs.Length > 4)
                    {
                        int t = 2;
                        List<Contact> contacts = new List<Contact>();
                        while (t + 2 < strs.Length)
                        {
                            Vector3d v = new Vector3d(double.Parse(strs[t++]), double.Parse(strs[t++]), double.Parse(strs[t++]));
                            Contact c = new Contact(v);
                            contacts.Add(c);
                        }
                        g.addAnEdge(g._NODES[j], g._NODES[k], contacts);
                    }
                    else
                    {
                        g.addAnEdge(g._NODES[j], g._NODES[k]);
                    }
                }
                g._functionalityValues = new FunctionalityFeatures();
                List<Functionality.Category> cats = new List<Functionality.Category>();
                if (sr.Peek() > -1)
                {
                    // functionality
                    for (int c = 0; c < Functionality._NUM_CATEGORIY; ++c)
                    {
                        if (sr.Peek() == -1)
                        {
                            break;
                        }
                        s = sr.ReadLine().Trim();
                        strs = s.Split(separator);
                        if (strs.Length < 2) // old version, less categories
                        {
                            break;
                        }
                        int cid = (int)Functionality.getCategory(strs[0]);
                        g._functionalityValues._funScores[cid] = double.Parse(strs[1]);
                        cats.Add(Functionality.getCategory(strs[0]));
                    }
                    if (sr.Peek() > -1)
                    {
                        s = sr.ReadLine().Trim();
                        strs = s.Split(separator);
                        for (int c = 0; c < strs.Length; ++c)
                        {
                            g._functionalityValues._parentCategories.Add(Functionality.getCategory(strs[c]));
                        }
                    }
                }

                if (unify)
                {
                    g.unify();
                    m.composeMesh();
                }
                g.init();

                string fsaName = filename.Substring(0, filename.LastIndexOf('.')) + ".fsa";
                if (File.Exists(fsaName))
                {
                    this.loadFunctionalSpaceAgent(g, fsaName);
                }
                if (!m._model_name.StartsWith("gen"))
                {
                    char[] sepChar = { '_' };
                    string[] names = m._model_name.Split(sepChar);
                    g._functionalityValues._parentCategories = new List<Functionality.Category>();
                    g._functionalityValues._parentCategories.Add(Functionality.getCategory(names[0]));
                    // save the fsa to store new info                    
                    if (!File.Exists(fsaName))
                    {
                        g.fitNodeFunctionalSpaceAgent();
                        this.saveFunctionalSpaceAgent(g, fsaName); // always save the original fs
                    }
                }
                else
                {
                    List<Functionality.Category> parentCats = new List<Functionality.Category>();
                    foreach (Part part in m._PARTS)
                    {
                        if (!parentCats.Contains(part._orignCategory))
                        {
                            parentCats.Add(part._orignCategory);
                        }
                    }
                    g._functionalityValues._parentCategories = parentCats;
                }
                m.setGraph(g);
                //this.calculateProbability(m);
                foreach (Node node in m._GRAPH._NODES)
                {
                    node._PART._MESH.afterUpdatePos();
                }
            }
        }// LoadAGraph

        public void saveAGraph(Graph g, string filename)
        {
            if (g == null)
            {
                return;
            }
            // node:
            // idx, isGroundTouching, Color, Sym (-1: no sym, idx)
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine(g._NNodes.ToString() + " nodes.");
                for (int i = 0; i < g._NNodes; ++i)
                {
                    Node iNode = g._NODES[i];
                    sw.Write(iNode._INDEX.ToString() + " ");
                    int isGround = iNode._isGroundTouching ? 1 : 0;
                    sw.Write(isGround.ToString() + " ");
                    // color
                    sw.Write(iNode._PART._COLOR.R.ToString() + " " + iNode._PART._COLOR.G.ToString() + " " + iNode._PART._COLOR.B.ToString() + " ");
                    // sym
                    int symIdx = -1;
                    if (iNode.symmetry != null)
                    {
                        symIdx = iNode.symmetry._INDEX;
                    }
                    sw.Write(symIdx.ToString());
                    // functionality
                    if (iNode._funcs != null)
                    {
                        foreach (Functionality.Functions func in iNode._funcs)
                        {
                            sw.Write(" " + func.ToString());
                        }
                    }
                    sw.WriteLine();
                }
                sw.WriteLine(g._EDGES.Count.ToString() + " edges.");
                foreach (Edge e in g._EDGES)
                {
                    sw.Write(e._start._INDEX.ToString() + " " + e._end._INDEX.ToString() + " ");
                    foreach (Contact pnt in e._contacts)
                    {
                        sw.Write(this.vector3dToString(pnt._pos3d, " ", " "));
                    }
                    sw.WriteLine();
                }
                if (g._functionalityValues != null)
                {
                    for (int i = 0; i < g._functionalityValues._cats.Length; ++i)
                    {
                        sw.WriteLine(g._functionalityValues._cats[i] + " " + g._functionalityValues._funScores[i].ToString());
                    }
                    // parent categories
                    StringBuilder sb = new StringBuilder();
                    foreach (Functionality.Category pc in g._functionalityValues._parentCategories)
                    {
                        sb.Append(Functionality.getCategoryName((int)pc));
                        sb.Append(" ");
                    }
                    sw.WriteLine(sb.ToString());
                }
            }
        }// saveAGraph

        public void saveFunctionalSpaceAgent(Graph g, string filename)
        {
            if (g == null)
            {
                return;
            }
            int nFSA = 0;
            foreach (Node node in g._NODES)
            {
                if (node._functionalSpaceAgent != null)
                {
                    ++nFSA;
                }
            }
            // idx, isGroundTouching, Color, Sym (-1: no sym, idx)
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine(nFSA.ToString() + " functional spaces.");
                for (int i = 0; i < g._NNodes; ++i)
                {
                    Node iNode = g._NODES[i];
                    if (iNode._functionalSpaceAgent == null)
                    {
                        continue;
                    }
                    sw.WriteLine(iNode._INDEX.ToString() + " ");
                    // prism
                    foreach (Vector3d v in iNode._functionalSpaceAgent._POINTS3D)
                    {
                        sw.Write(vector3dToString(v, " ", " "));
                    }
                    sw.WriteLine();
                }
            }
        }// saveFunctionalSpaceAgent

        public void loadFunctionalSpaceAgent(Graph g, string filename)
        {
            if (g == null)
            {
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t' };
                string s = sr.ReadLine().Trim();
                string[] strs = s.Split(separator);
                int nFSA = int.Parse(strs[0]);
                for (int i = 0; i < nFSA; ++i)
                {
                    // read a part
                    // bbox vertices:
                    s = sr.ReadLine().Trim(); // description of #i part
                    strs = s.Split(separator);
                    int nodeIdx = int.Parse(strs[0]);
                    // prism vertices
                    s = sr.ReadLine().Trim();
                    strs = s.Split(separator);
                    int nVertices = strs.Length / 3;
                    Vector3d[] pnts = new Vector3d[nVertices];
                    for (int j = 0, k = 0; j < nVertices; ++j)
                    {
                        pnts[j] = new Vector3d(double.Parse(strs[k++]), double.Parse(strs[k++]), double.Parse(strs[k++]));
                    }
                    Prism prim = new Prism(pnts);
                    g._NODES[nodeIdx]._functionalSpaceAgent = prim;
                }
            }
        }// loadFunctionalSpaceAgent

        /******************** End - Load & Save ********************/

        public string nextModel()
        {
            if (_ancesterModels.Count == 0)
            {
                return "0/0";
            }
            this.meshIdx = (this.meshIdx + 1) % this._ancesterModels.Count;
            this._currModel = this._ancesterModels[this.meshIdx];
            this.Refresh();
            string str = (this.meshIdx + 1).ToString() + "\\" + this._ancesterModels.Count.ToString() + ": ";
            str += this._currModel._model_name;
            return str;
        }

        public string prevModel()
        {
            if (_ancesterModels.Count == 0)
            {
                return "0/0";
            }
            this.meshIdx = (this.meshIdx - 1 + this._ancesterModels.Count) % this._ancesterModels.Count;
            this._currModel = this._ancesterModels[this.meshIdx];
            this.Refresh();
            string str = (this.meshIdx + 1).ToString() + "\\" + this._ancesterModels.Count.ToString() + ": ";
            str += this._currModel._model_name;
            return str;
        }

        public string[] collectSimValuesOfPartGroups()
        {
            if (_pairPG2 + 1 >= _partGroups.Count)
            {
                _pairPG1++;
                _pairPG2 = _pairPG1;
            }
            ++_pairPG2;
            if (_pairPG1 >= _partGroups.Count)
            {
                MessageBox.Show("Start over.");
                _pairPG1 = 0;
                _pairPG2 = 1;
            }
            string[] strs = new string[2];
            double dist = this.setCurrPairOfPartGroups();
            strs[0] = "pg_" + _pairPG1.ToString() + "_" + _pairPG2.ToString();
            strs[1] = dist.ToString();
            return strs;
        }

        public string nextFunctionalSpace()
        {
            //// Functional Patch / parts
            //if (_currModel == null)
            //{
            //    return "";
            //}
            //_categoryId = (_categoryId + 1) % Functionality._NUM_CATEGORIY;
            //this.Refresh();
            //string str = Functionality.getCategoryName(_categoryId);
            //return str;

            // Functional Space
            if (_currModel == null || _currModel._funcSpaces == null)
            {
                return "0/0";
            }
            _fsIdx = (_fsIdx + 1) % _currModel._funcSpaces.Length;
            this.Refresh();
            string str = (_fsIdx + 1).ToString() + "//" + _currModel._funcSpaces.Length.ToString();
            return str;
        }
        
        public string prevFunctionalSpace()
        {
            // Functional Patch / parts
            //if (_currModel == null)
            //{
            //    return "";
            //}
            //_categoryId = (_categoryId - 1 + Functionality._NUM_CATEGORIY) % Functionality._NUM_CATEGORIY;
            //this.Refresh();
            //string str = Functionality.getCategoryName(_categoryId);
            //return str;

            // Functional Space
            if (_currModel == null || _currModel._funcSpaces == null)
            {
                return "0/0";
            }
            _fsIdx = (_fsIdx - 1 + _currModel._funcSpaces.Length) % _currModel._funcSpaces.Length;
            this.Refresh();
            string str = (_fsIdx + 1).ToString() + "//" + _currModel._funcSpaces.Length.ToString();
            return str;
        }

        public string nextMeshClass()
        {
            if (this._meshClasses.Count == 0)
            {
                return "0/0";
            }
            this.meshIdx = (this.meshIdx + 1) % this._meshClasses.Count;
            this.currMeshClass = this._meshClasses[this.meshIdx];
            this.Refresh();
            string str = (this.meshIdx + 1).ToString() + "\\" + this._meshClasses.Count.ToString() + ": ";
            str += this.currMeshClass._MESHNAME;
            return str;
        }

        private double setCurrPairOfPartGroups()
        {
            PartGroup pg1 = _partGroups[_pairPG1][0];
            PartGroup pg2 = _partGroups[_pairPG2][0];
            double sim = this.compareTwoPartGroups(pg1, pg2);
            _pgPairVisualization = new List<Part>();
            Matrix4d T = Matrix4d.TranslationMatrix(new Vector3d(-0.5, 0, 0));
            foreach (Node node in pg1._NODES)
            {
                Part p = node._PART.Clone() as Part;
                p.Transform(T);
                _pgPairVisualization.Add(p);
            }
            T = Matrix4d.TranslationMatrix(new Vector3d(0.5, 0, 0));
            foreach (Node node in pg2._NODES)
            {
                Part p = node._PART.Clone() as Part;
                p.Transform(T);
                _pgPairVisualization.Add(p);
            }
            _currModel = null;
            this.Refresh();
            return sim;
        }// setCurrPairOfPartGroups

        public string prevMeshClass()
        {
            if (this._meshClasses.Count == 0)
            {
                return "0/0";
            }
            this.meshIdx = (this.meshIdx - 1 + this._meshClasses.Count) % this._meshClasses.Count;
            this.currMeshClass = this._meshClasses[this.meshIdx];
            this.Refresh();
            string str = (this.meshIdx + 1).ToString() + "\\" + this._meshClasses.Count.ToString() + ": ";
            str += this.currMeshClass._MESHNAME;
            return str;
        }

        public void refit_by_cylinder()
        {
            if (_selectedParts.Count == 0)
            {
                return;
            }
            foreach (Part p in _selectedParts)
            {
                p.fitProxy(1);
            }
            this.Refresh();
        }// refit_by_cylinder

        public void refit_by_cuboid()
        {
            if (_selectedParts.Count == 0)
            {
                return;
            }
            foreach (Part p in _selectedParts)
            {
                p.fitProxy(0);
            }
        }// refit_by_cuboid

        public void refit_by_axis_aligned_cuboid()
        {
            if (_selectedParts.Count == 0)
            {
                return;
            }
            foreach (Part p in _selectedParts)
            {
                p.fitProxy(2);
            }
        }// refit_by_axis-aligned-cuboid

        private bool hasInValidContact(Graph g)
        {
            if (g == null) return false;
            foreach (Edge e in g._EDGES)
            {
                foreach (Contact c in e._contacts)
                {
                    if (double.IsNaN(c._pos3d.x) || double.IsNaN(c._pos3d.y) || double.IsNaN(c._pos3d.z))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void refreshModelViewers()
        {
            // view the same as the main view
            foreach (ModelViewer mv in _ancesterModelViewers)
            {
                mv.Refresh();
            }
            foreach (ModelViewer mv in _currGenModelViewers)
            {
                mv.Refresh();
            }
            foreach (ModelViewer mv in _partViewers)
            {
                mv.Refresh();
            }
        }// refreshModelViewers

        public void setCurrentModel(Model m, int idx)
        {
            _currModel = m;
            _selectedParts.Clear();
            _selectedModelIndex = idx;
            _crossOverBasket.Remove(m);
            m._GRAPH.selectedNodePairs.Clear();
            if (this._isPreRun)
            {
                Program.GetFormMain().writePostAnalysisInfo(getFunctionalityValuesString(m, false));
            }
            this.cal2D();
            this.Refresh();
        }

        public void getModelMainFuncs(Model m)
        {
            if (m == null || m._GRAPH == null)
            {
                return;
            }
            Program.GetFormMain().clearCheckBoxes();
            _currUserFunctions = m._GRAPH.collectMainFunctions();
            foreach (Functionality.Functions f in _currUserFunctions)
            {
                Program.GetFormMain().setCheckBox(Functionality.getFunctionString(f));
            }
            _prevUserFunctions = new List<Functionality.Functions>(_currUserFunctions);
        }


        public void userSelectModel(Model m, bool addOrReomove)
        {
            // from user selction
            if (addOrReomove)
            {
                _userSelectedModels.Add(m);
            }
            else
            {
                _userSelectedModels.Remove(m);
            }
        }// userSelectModel

        private string getFunctionalityValuesString(Model m, bool needRanks)
        {
            if (m == null || m._GRAPH == null || m._GRAPH._functionalityValues == null
                || m._GRAPH._functionalityValues._cats == null || _inputSetCats == null || _inputSetCats.Contains((int)Functionality.Category.None))
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            int n = _currGenModelViewers.Count;
            n = _ancesterModelViewers.Count > n ? _ancesterModelViewers.Count : n;
            for (int i = 0; i <_inputSetCats.Count; ++i)
            {
                int cid = _inputSetCats[i];
                sb.Append(Functionality.getCategoryName(cid));
                sb.Append(" validity: ");
                sb.Append(this.double2String(m._GRAPH._functionalityValues._funScores[cid]));
                sb.Append(" P_1: ");
                sb.Append(this.double2String(m._GRAPH._functionalityValues._inClassProbs[cid]));
                sb.Append(" P_2: ");
                sb.Append(this.double2String(m._GRAPH._functionalityValues._outClassProbs[cid]));
                sb.Append(" P_1_2: ");
                sb.Append(this.double2String(m._GRAPH._functionalityValues._classProbs[cid]));
                if (needRanks && _ranksByCategory != null)
                {
                    sb.Append(" Rank: ");
                    int idx = -1;
                    _currentModelIndexMap.TryGetValue(m._index, out idx);
                    sb.Append(_ranksByCategory[idx, i].ToString());
                    sb.Append("/");
                    sb.Append(n.ToString());
                }
                sb.Append("\n");
            }
            sb.Append("max validity of parent categories: ");
            sb.Append(this.double2String(m._GRAPH._functionalityValues._validityVal));
            sb.Append("\n");
            sb.Append("novelty (multiple functionality): ");
            sb.Append(this.double2String(m._GRAPH._functionalityValues._noveltyVal));
            sb.Append("\n");
            sb.Append("validity * novelty: ");
            sb.Append(this.double2String(m._GRAPH._functionalityValues._noveltyVal * m._GRAPH._functionalityValues._validityVal));
            return sb.ToString();
        }// getFunctionalityValuesString

        private void cal2D()
        {
            // otherwise when glViewe is initialized, it will run this function from MouseUp()
            //if (this.currSegmentClass == null) return;

            // reset the current 3d transformation again to check in the camera info, projection/modelview
            Gl.glViewport(0, 0, this.Width, this.Height);
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            double aspect = (double)this.Width / this.Height;
            if (this.nPointPerspective == 3)
            {
                Glu.gluPerspective(90, aspect, 0.1, 1000);
            }
            else
            {
                Glu.gluPerspective(45, aspect, 0.1, 1000);
            }
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();

            Glu.gluLookAt(this.eye.x, this.eye.y, this.eye.z, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);

            Matrix4d transformMatrix = this.arcBall.getTransformMatrix(this.nPointPerspective);
            Matrix4d m = transformMatrix * this._currModelTransformMatrix;

            m = Matrix4d.TranslationMatrix(this.objectCenter) * m * Matrix4d.TranslationMatrix(
                new Vector3d() - this.objectCenter);

            this.calculatePoint2DInfo();

            //Gl.glMatrixMode(Gl.GL_MODELVIEW);
            //Gl.glPushMatrix();
            //Gl.glMultMatrixd(m.Transpose().ToArray());

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();
        }//cal2D

        private void calculatePoint2DInfo()
        {
            this.updateCamera();
            if (_currHumanPose != null)
            {
                foreach (BodyNode bn in _currHumanPose._bodyNodes)
                {
                    Vector2d v2 = this.camera.Project(bn._POS).ToVector2d();
                    bn._pos2 = new Vector2d(v2.x, this.Height - v2.y);
                }
            }
            if (this._currModel == null || this._currModel._PARTS == null)
                return;
            Vector2d max_coord = Vector2d.MinCoord();
            Vector2d min_coord = Vector2d.MaxCoord();
            foreach (Part p in this._currModel._PARTS)
            {
                Prism box = p._BOUNDINGBOX;
                for (int i = 0; i < box._POINTS3D.Length; ++i)
                {
                    p._BOUNDINGBOX._POINTS2D[i] = getVec2D(box._POINTS3D[i]);
                }
            }
            if (_currModel._GRAPH != null)
            {
                foreach (Edge e in _currModel._GRAPH._EDGES)
                {
                    foreach (Contact pnt in e._contacts)
                    {
                        Vector2d v2 = this.camera.Project(pnt._pos3d).ToVector2d();
                        pnt._pos2d = new Vector2d(v2.x, this.Height - v2.y);
                    }
                }
            }
            if (_editAxes != null)
            {
                foreach (Contact p in _editAxes)
                {
                    p._pos2d = getVec2D(p._pos3d);
                }
            }
        }// calculatePoint2DInfo

        private Vector2d getVec2D(Vector3d v3)
        {
            Vector2d v2 = this.camera.Project(v3).ToVector2d();
            Vector2d v = new Vector2d(v2.x, this.Height - v2.y);
            return v;
        }// getVec2D

        public void writeModelViewMatrix(string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < 4; ++i)
                {
                    for (int j = 0; j < 4; ++j)
                    {
                        sw.Write(this._currModelTransformMatrix[i, j].ToString() + " ");
                    }
                }
            }
        }

        public void readModelModelViewMatrix(string filename)
        {
            if (!File.Exists(filename))
            {
                Program.GetFormMain().outputSystemStatus("Default view matrix does not exist.");
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ' };
                string s = sr.ReadLine().Trim();
                s.Trim();
                string[] strs = s.Split(separator);
                double[] arr = new double[strs.Length];
                for (int i = 0; i < arr.Length; ++i)
                {
                    if (strs[i] == "") continue;
                    arr[i] = double.Parse(strs[i]);
                }
                this._currModelTransformMatrix = new Matrix4d(arr);
                this._fixedModelView = new Matrix4d(arr);
                this.Refresh();
            }
        }

        public void captureInputSets()
        {
            if (_currGenModelViewers.Count == 0 && _ancesterModelViewers.Count == 0)
            {
                return;
            }
            for (int i = 0; i < _currGenModelViewers.Count; ++i)
            {
                this.setCurrentModel(_currGenModelViewers[i]._MODEL, i);
                this.captureScreen(i);
            }
        }

        public void captureScreen(int idx)
        {
            Size newSize = new System.Drawing.Size(this.Width, this.Height);
            var bmp = new Bitmap(newSize.Width, newSize.Height);
            var gfx = Graphics.FromImage(bmp);
            gfx.CopyFromScreen((int)(this.Location.X), (int)(this.Location.Y) + 90,
                0, 0, newSize, CopyPixelOperation.SourceCopy);
            string imageFolder = foldername + "\\screenCapture";

            if (!Directory.Exists(imageFolder))
            {
                Directory.CreateDirectory(imageFolder);
            }
            string name = imageFolder + "\\model_" + idx.ToString() + ".png";
            bmp.Save(name, System.Drawing.Imaging.ImageFormat.Png);
        }

        public void captureScreen(string filename)
        {
            Size newSize = new System.Drawing.Size(this.Width, this.Height);
            using (var bmp = new Bitmap(newSize.Width, newSize.Height))
            {
                using (var gfx = Graphics.FromImage(bmp))
                {
                    gfx.CopyFromScreen((int)(this.Location.X), (int)(this.Location.Y) + 90,
                        0, 0, newSize, CopyPixelOperation.SourceCopy);
                    //var copyTosolveGDIError = new Bitmap(bmp);
                    //copyTosolveGDIError.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
                    bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        private Vector3d getCurPos(Vector3d v)
        {
            return (this._currModelTransformMatrix * new Vector4d(v, 1)).ToVector3D();
        }

        private Vector3d getOriPos(Vector3d v)
        {
            return (this._modelTransformMatrix.Inverse() * new Vector4d(v, 1)).ToVector3D();
        }

        public void renderToImage(string filename)
        {
            //uint FramerbufferName = 0;
            //Gl.glGenFramebuffersEXT(1, out FramerbufferName);
            //Gl.glBindFramebufferEXT(Gl.GL_FRAMEBUFFER_EXT, FramerbufferName);
            this.Draw3D();
            int w = this.Width, h = this.Height;
            Bitmap bmp = new Bitmap(w, h);
            Rectangle rect = new Rectangle(0, 0, w, h);
            System.Drawing.Imaging.BitmapData data =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            //System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            Gl.glReadPixels(0, 0, w, h, Gl.GL_BGR, Gl.GL_UNSIGNED_BYTE, data.Scan0);
            bmp.UnlockBits(data);
            bmp.RotateFlip(RotateFlipType.Rotate180FlipX);
            bmp.Save(filename, System.Drawing.Imaging.ImageFormat.Png);
        }

        private void writeALine(StreamWriter sw, Vector2d u, Vector2d v, float width, Color color)
        {
            sw.WriteLine("newpath");
            double x = u.x;
            double y = u.y;
            sw.Write(x.ToString() + " ");
            sw.Write(y.ToString() + " ");
            sw.WriteLine("moveto ");
            x = v.x;
            y = v.y;
            sw.Write(x.ToString() + " ");
            sw.Write(y.ToString() + " ");
            sw.WriteLine("lineto ");
            //sw.WriteLine("gsave");
            sw.WriteLine(width.ToString() + " " + "setlinewidth");
            float[] c = { (float)color.R / 255, (float)color.G / 255, (float)color.B / 255 };
            sw.WriteLine(c[0].ToString() + " " +
                c[1].ToString() + " " +
                c[2].ToString() + " setrgbcolor");
            sw.WriteLine("stroke");
        }

        private void writeATriangle(StreamWriter sw, Vector2d u, Vector2d v, Vector2d w, float width, Color color)
        {
            sw.WriteLine("newpath");
            double x = u.x;
            double y = u.y;
            sw.Write(x.ToString() + " ");
            sw.Write(y.ToString() + " ");
            sw.WriteLine("moveto ");
            x = v.x;
            y = v.y;
            sw.Write(x.ToString() + " ");
            sw.Write(y.ToString() + " ");
            sw.WriteLine("lineto ");
            sw.Write(w.x.ToString() + " ");
            sw.Write(w.y.ToString() + " ");
            sw.WriteLine("lineto ");
            sw.WriteLine("closepath");
            sw.WriteLine("gsave");

            float[] c = { (float)color.R / 255, (float)color.G / 255, (float)color.B / 255 };
            sw.WriteLine("grestore");
            sw.WriteLine(width.ToString() + " " + "setlinewidth");
            sw.WriteLine(c[0].ToString() + " " +
                c[1].ToString() + " " +
                c[2].ToString() + " setrgbcolor");
            sw.WriteLine("stroke");
        }

        private void writeACircle(StreamWriter sw, Vector2d center, float radius, Color color, float width)
        {
            float[] c = { (float)color.R / 255, (float)(float)color.G / 255, (float)(float)color.B / 255 };
            sw.WriteLine(width.ToString() + " " + "setlinewidth");
            sw.Write(center.x.ToString() + " ");
            sw.Write(center.y.ToString() + " ");
            sw.Write(radius.ToString());
            sw.WriteLine(" 0 360 arc closepath");
            sw.WriteLine(c[0].ToString() + " " +
                        c[1].ToString() + " " +
                        c[2].ToString() + " setrgbcolor fill");
            sw.WriteLine("stroke");
        }

        /*****************Functions-aware evolution*************************/

        public void markSymmetry()
        {
            if (_selectedNodes.Count != 2)
            {
                return;
            }
            _currModel._GRAPH.markSymmtry(_selectedNodes[0], _selectedNodes[1]);
        }// markSymmetry

        public void markFunctionPart(int i)
        {
            Functionality.Functions func = getFunctionalityFromIndex(i);

            foreach (Node node in _selectedNodes)
            {
                node.addFunctionality(func);
            }

        }// markFunctionPart

        public void removeFunction()
        {
            foreach (Node node in _selectedNodes)
            {
                node._funcs.Clear();
                node._isGroundTouching = false;
            }
        }

        public void nameParts(string s)
        {
            foreach (Node node in _selectedNodes)
            {
                int n = this.numOfPartNameStartsWith(s);
                node._PART._partName = s + "_" + n.ToString();
            }
        }// nameParts

        private int numOfPartNameStartsWith(string s)
        {
            if (_currModel == null || _currModel._PARTS == null)
            {
                return 0;
            }
            int n = 0;
            foreach (Part part in _currModel._PARTS)
            {
                if (part._partName != null && part._partName.StartsWith(s))
                {
                    ++n;
                }
            }
            return n;
        }// numOfPartNameStartsWith

        private Functionality.Functions getFunctionalityFromIndex(int i)
        {
            switch (i)
            {
                case 0:
                    return Functionality.Functions.GROUND_TOUCHING;
                case 1:
                    return Functionality.Functions.HUMAN_BACK;
                case 2:
                    return Functionality.Functions.SITTING;
                case 3:
                    return Functionality.Functions.HAND_HOLD;
                case 4:
                    return Functionality.Functions.PLACEMENT;
                case 5:
                    return Functionality.Functions.SUPPORT;
                case 6:
                    return Functionality.Functions.HANG;
                case 7:
                    return Functionality.Functions.STORAGE;
                case 8:
                    return Functionality.Functions.ROLLING;
                case 9:
                    return Functionality.Functions.ROCKING;
                default:
                    return Functionality.Functions.NONE;
            }
        }// getFunctionalityFromIndex

        private Functionality.Functions getFunctionalityFromString(string s)
        {
            switch (s.ToUpper())
            {
                case "HUMAN_BACK":
                    return Functionality.Functions.HUMAN_BACK;
                case "SITTING":
                    return Functionality.Functions.SITTING;
                case "HAND_HOLD":
                    return Functionality.Functions.HAND_HOLD;
                case "PLACEMENT":
                    return Functionality.Functions.PLACEMENT;
                case "SUPPORT":
                    return Functionality.Functions.SUPPORT;
                case "STORAGE":
                    return Functionality.Functions.STORAGE;
                case "HANG":
                    return Functionality.Functions.HANG;
                case "ROLLING":
                    return Functionality.Functions.ROLLING;
                case "ROCKING":
                    return Functionality.Functions.ROCKING;
                case "GROUND_TOUCHING":
                    return Functionality.Functions.GROUND_TOUCHING;
                default:
                    return Functionality.Functions.NONE;
            }
        }// getFunctionalityFromIndex

        public void switchParts(Graph g1, Graph g2, List<Node> nodes1, List<Node> nodes2)
        {
            List<Edge> edgesToConnect_1 = g1.getOutgoingEdges(nodes1);
            List<Edge> edgesToConnect_2 = g2.getOutgoingEdges(nodes2);
            List<Vector3d> sources = collectPoints(edgesToConnect_1);
            List<Vector3d> targets = collectPoints(edgesToConnect_2);

            if (sources.Count == targets.Count && sources.Count == 2)
            {

            }
        }// switchParts

        public void collectSnapshotsFromFolder(string folder)
        {
            string snapshot_folder = folder + "\\snapshots";

            this.readModelModelViewMatrix(folder + "\\view.mat");

            // for capturing screen
            this.reloadView();

            this.decideWhichToDraw(true, false, false, true, false, false);

            dfs_files(folder, snapshot_folder);
        }// collectSnapshotsFromFolder

        public void decideWhichToDraw(bool isDrawMesh, bool isBbox, bool isGraph, bool isGround, bool isFunctionalSpace, bool isDrawSamplePoints)
        {
            this.isDrawMesh = isDrawMesh;
            this.isDrawBbox = isBbox;
            this.isDrawGraph = isGraph;
            this.isDrawGround = isGround;
            this.isDrawFuncSpace = isFunctionalSpace;
            this.isDrawModelSamplePoints = isDrawSamplePoints;
            this.updateCheckBox();
        }

        private void updateCheckBox()
        {
            Program.GetFormMain().setCheckBox_drawMesh(this.isDrawMesh);
            Program.GetFormMain().setCheckBox_drawBbox(this.isDrawBbox);
            Program.GetFormMain().setCheckBox_drawGraph(this.isDrawGraph);
            Program.GetFormMain().setCheckBox_drawGround(this.isDrawGround);
            Program.GetFormMain().setCheckBox_drawFunctionalSpace(this.isDrawFuncSpace);
            Program.GetFormMain().setCheckBox_drawSamplePoints(this.isDrawModelSamplePoints);
        }

        private void dfs_files(string folder, string snap_folder)
        {
            string[] files = Directory.GetFiles(folder);
            if (!Directory.Exists(snap_folder))
            {
                Directory.CreateDirectory(snap_folder);
            }
            foreach (string file in files)
            {
                if (!file.EndsWith("pam"))
                {
                    continue;
                }
                Model m = loadOnePartBasedModel(file);
                if (m != null)
                {
                    string graphName = file.Substring(0, file.LastIndexOf('.')) + ".graph";
                    LoadAGraph(m, graphName, false);
                    this.setCurrentModel(m, -1);
                    Program.GetFormMain().updateStats();
                    this.captureScreen(snap_folder + "\\" + m._model_name + ".png");
                }
            }
            string[] folders = Directory.GetDirectories(folder);
            if (folders.Length == 0)
            {
                return;
            }
            foreach (string subfolder in folders)
            {
                string foldername = subfolder.Substring(subfolder.LastIndexOf('\\'));
                dfs_files(subfolder, snap_folder + foldername);
            }
        }// dfs_files

        public int getParentModelNum()
        {
            return _ancesterModelViewers.Count;
        }

        private void convertFunctionalityDescription(Vector3d[] points, double[] weights)
        {
            if (_currModel == null || _currModel._GRAPH == null || points == null || weights == null || points.Length != weights.Length)
            {
                return;
            }
            // TO BE UPDATED AFTER GET THE DATA
            int[] labels = new int[weights.Length];
            foreach (Node node in _currModel._GRAPH._NODES)
            {
                // get a stats of each point
                Mesh m = node._PART._MESH;
                Vector3d[] vecs = m.VertexVectorArray;
                Dictionary<int, int> dict = new Dictionary<int, int>();
                for (int i = 0; i < vecs.Length; ++i)
                {
                    double mind = double.MaxValue;
                    int plabel = -1;
                    for (int j = 0; j < points.Length; ++j)
                    {
                        double d = (vecs[i] - points[j]).Length();
                        if (d < mind)
                        {
                            mind = d;
                            plabel = labels[j];
                        }
                    }
                    int val = 0;
                    if (dict.TryGetValue(plabel, out val))
                    {
                        val++;
                    }
                    else
                    {
                        val = 1;
                    }
                    dict.Add(plabel, val);
                }// for -vertex
                // label by the majority
                Dictionary<int, int>.Enumerator iter = dict.GetEnumerator();
                int part_label = -1;
                int maxnum = 0;
                while (iter.MoveNext())
                {
                    int num = iter.Current.Value;
                    if (num > maxnum)
                    {
                        maxnum = num;
                        part_label = iter.Current.Key;
                    }
                }
                node.addFunctionality(this.getFunctionalityFromIndex(part_label));
            }// for-part
        }// convertFunctionalityDescription

        // save folders
        string userFolder;
        string mutateFolder;
        string crossoverFolder;
        string growthFolder;
        string funcFolder;
        string imageFolder_m;
        string imageFolder_c;
        string imageFolder_g;
        int _userIndex = 1;

        public int registerANewUser()
        {
            string user_root = this.foldername + "\\Users";
            _userIndex = 1;
            userFolder = user_root + "\\User_" + _userIndex.ToString();
            while (Directory.Exists(userFolder))
            {
                // create a new folder for the new user
                ++_userIndex;
                userFolder = user_root + "\\User_" + _userIndex.ToString();
            }
            Directory.CreateDirectory(userFolder);

            mutateFolder = userFolder + "\\models\\mutate\\";
            crossoverFolder = userFolder + "\\models\\crossover\\";
            growthFolder = userFolder + "\\models\\growth\\";
            funcFolder = userFolder + "\\models\\funcTest\\";

            createDirectory(mutateFolder);
            createDirectory(crossoverFolder);
            createDirectory(growthFolder);

            imageFolder_m = userFolder + "\\screenCapture\\mutate\\";
            imageFolder_c = userFolder + "\\screenCapture\\crossover\\";
            imageFolder_g = userFolder + "\\screenCapture\\growth\\";
            string invalidImagefolder = imageFolder_c + "invald\\";

            createDirectory(imageFolder_m);
            createDirectory(imageFolder_c);
            createDirectory(imageFolder_g);
            createDirectory(invalidImagefolder);

            return _userIndex;
        }// registerANewUser

        private void createDirectory(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        private void saveUserSelections(int gen)
        {
            string dir = userFolder + "\\selections_" + _userReCompute.ToString() + "\\";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string selectionTxt = dir + "gen_" + gen.ToString() + ".txt";
            using (StreamWriter sw = new StreamWriter(selectionTxt))
            {
                sw.WriteLine("User " + _userIndex.ToString());
                sw.WriteLine("Generation " + gen.ToString());
                foreach (Model model in _userSelectedModels)
                {
                    sw.WriteLine(model._path + model._model_name);

                    string meshName = dir + model._model_name + ".obj";
                    this.saveModelObj(model, meshName);
                }
            }
        }// saveUserSelections

        int _userReCompute = 1;
        List<Model> _userSelectedModelsBeforeRecompute = new List<Model>();
        private void expandValidityMatrix()
        {
            // read user selections
            //string dir = userFolder + "\\selections_" + _userReCompute.ToString() + "\\";
            //string[] files = Directory.GetFiles(dir, "*.txt");
            //
            //for (int i = 1; i < files.Length; ++i)
            //{
            //    List<Model> models = this.selectedModels(files[i]);
            //    userSelectedModels.AddRange(models);
            //}
            _ancesterModels.AddRange(_userSelectedModelsBeforeRecompute);
            // only use mix part groups
            foreach (Model model in _userSelectedModelsBeforeRecompute)
            {
                model._GRAPH.initializePartGroups();
                // check
                for (int i = 0; i < model._GRAPH._partGroups.Count; ++i)
                {
                    bool sameParent = true;
                    PartGroup pg = model._GRAPH._partGroups[i];
                    if (pg._NODES.Count == 0)
                    {
                        continue;
                    }
                    Functionality.Category cat = pg._NODES[0]._PART._orignCategory;
                    for (int j = 1; j < pg._NODES.Count; ++j)
                    {
                        if (pg._NODES[j]._PART._orignCategory != cat)
                        {
                            sameParent = false;
                            break;
                        }
                    }
                    if (sameParent)
                    {
                        model._GRAPH._partGroups.RemoveAt(i);
                        --i;
                    }
                }
                foreach (PartGroup pg in model._GRAPH._partGroups)
                {
                    List<PartGroup> pgs = new List<PartGroup>();
                    pg._ParentModelIndex = model._index;
                    pgs.Add(pg);
                    _partGroups.Add(pgs);
                }
            }
            int nPGs = _partGroups.Count;
            SparseMatrix tmp = new SparseMatrix(_validityMatrixPG);
            int oldPGs = tmp.NRow;
            _validityMatrixPG = new SparseMatrix(nPGs, nPGs);
            foreach (Triplet trip in tmp.GetTriplets())
            {
                _validityMatrixPG.AddTriplet(trip.row, trip.col, trip.value);
            }
            // compute 
            int pairId = tmp.NTriplets;
            Random rand = new Random();
            this._isPreRun = true;
            string imageFolder = imageFolder_c + "recompute_" + _currGenId.ToString() + "\\";
            for (int i = 0; i < nPGs; ++i)
            {
                for (int j = oldPGs; j < nPGs; ++j)
                {
                    if (_partGroups[i][0]._ParentModelIndex == _partGroups[j][0]._ParentModelIndex)
                    {
                        continue;
                    }
                    if (!_partGroups[j][0].containsMainFuncPart())
                    {
                        continue;
                    }
                    //List<Model> ijs;
                    //runACrossover(i, j, _currGenId, new Random(), imageFolder, pairId++, out ijs);
                    _validityMatrixPG.AddTriplet(i, j, 0.5 + rand.NextDouble() / 2);
                    _validityMatrixPG.AddTriplet(j, i, 0.5 + rand.NextDouble() / 2);
                }
            }
            
            for (int i = oldPGs; i < nPGs; ++i)
            {
                for (int j = i + 1; j < nPGs; ++j)
                {
                    if (_partGroups[i][0]._ParentModelIndex == _partGroups[j][0]._ParentModelIndex)
                    {
                        continue;
                    }
                    if (!_partGroups[j][0].containsMainFuncPart())
                    {
                        continue;
                    }
                    //List<Model> ijs;
                    //runACrossover(i, j, _currGenId, new Random(), imageFolder_c, pairId++, out ijs);
                    _validityMatrixPG.AddTriplet(i, j, 0.5 + rand.NextDouble() / 2);
                    _validityMatrixPG.AddTriplet(j, i, 0.5 + rand.NextDouble() / 2);
                }
            }
            string validityMatrixFolder = Interface.MODLES_PATH + "ValidityMatrix\\";
            string validityMatrixFileName = validityMatrixFolder + "User_" + _userIndex.ToString() +
            "_Recompute_" + _userReCompute.ToString() + ".vdm";
            this.saveValidityMatrix(validityMatrixFileName);
            ++_userReCompute;
            this.loadValidityMatrix(validityMatrixFileName);
            this._isPreRun = false;
            ++_currGenId; // to view at diff model folders
            _userSelectedModelsBeforeRecompute = new List<Model>();
        }// expandValidityMatrix

        private List<Model> selectedModels(string filename)
        {
            List<Model> models = new List<Model>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t' };
                sr.ReadLine();
                sr.ReadLine();
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine().Trim();
                    string model_name = s + ".pam";
                    Model m = this.loadAPartBasedModelAgent(model_name, false);
                    models.Add(m);
                }
            }
            return models;
        }// selectedModels

        double avgTimePerValidOffspring = 0;
        int validOffspringNumber = 0;
        double longestTimePerValidOffspring = 0;
        public List<ModelViewer> autoGenerate()
        {
            if (!Directory.Exists(userFolder))
            {
                this.registerANewUser();
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            this.decideWhichToDraw(true, false, false, true, false, false);
            // for capturing screen            
            this.reloadView();

            string validityMatrixFolder = Interface.MODLES_PATH + "ValidityMatrix\\";
            string validityMatrixFileName = validityMatrixFolder + "User_" + _userIndex.ToString() +
                "_gen_" + _currGenId.ToString() + ".vdm";

            // set user selection
            string selectedModelPath = userFolder + "\\selected\\";
            if (!Directory.Exists(selectedModelPath))
            {
                Directory.CreateDirectory(selectedModelPath);
            }
            for (int i = 0; i < _currGenModelViewers.Count; ++i) 
            {
                ModelViewer mv = _currGenModelViewers[i];
                int p1 = mv._MODEL._partGroupPair._p1;
                int p2 = mv._MODEL._partGroupPair._p2;
                // fixed bug
                Triplet trip = _validityMatrixPG.GetTriplet(p1, p2);
                if (trip == null)
                {
                    continue; // already removed in the loop
                }
                double prob = _validityMatrixPG.GetTriplet(p1, p2).value;
                double update_prob = 0; // user did not select
                bool increase = _userSelectedModels.Contains(mv._MODEL);
                if (increase)
                {
                    update_prob = Math.Min(1.0, prob * 1.5);
                    //mv._MODEL._GRAPH.initializePartGroups();
                    foreach (PartGroup pg in mv._MODEL._GRAPH._partGroups)
                    {
                        pg._ParentModelIndex = mv._MODEL._index;
                        _partGroups[p1].Add(pg);
                    }
                    _modelIndexMap.Add(mv._MODEL._index, mv._MODEL);
                    _userSelectedModelsBeforeRecompute.Add(mv._MODEL);
                }
                if (update_prob == 0)
                {
                    _validityMatrixPG.RemoveATriplet(p1, p2);
                }
                else
                {
                    _validityMatrixPG.AddTriplet(p1, p2, update_prob);
                    // put in a selected folder                
                    this.saveAPartBasedModel(mv._MODEL, selectedModelPath + mv._MODEL._model_name + ".pam", false);
                }
                //this.updateSimilarGroups(p1, p2, increase);
            }
            // after user selction
            this.saveValidityMatrix(validityMatrixFileName);
            // add user selected models
            if (_currGenId > 0)
            {
                this.saveUserSelections(_currGenId);
            }
            // parent shapes at the current generation
            //List<Model> parents = new List<Model>(_userSelectedModels);           
            if (_currGenId % Functionality._NUM_INTER_BEFORE_RERUN == 0)
            {
                // expand the matrix
                this.expandValidityMatrix();
            }
            // always include the ancient models
            List<Model> parents = new List<Model>(_ancesterModels);            

            // run 
            avgTimePerValidOffspring = 0;
            validOffspringNumber = 0;
            int maxIter = 1;
            int start = 0;
            _userSelectedModels = new List<Model>();
            _currGenModelViewers.Clear();
            _curGenPGmemory = new Dictionary<int, List<int>>();
            _maxUseEmptyGroup = parents.Count;
            List<Model> curGeneration = new List<Model>();

            // pre-process
            this._isPreRun = true;
            curGeneration = preRun(imageFolder_c);
            string today = DateTime.Today.ToString("MMdd");
            validityMatrixFileName = validityMatrixFolder + "Set_Teaser_1_" + today + ".vdm";
            this.saveValidityMatrix(validityMatrixFileName);
            string timingFilename = validityMatrixFolder + "Set_Teaser_1_" + today + ".time";
            return _currGenModelViewers;


            for (int i = 0; i < maxIter; ++i)
            {
                Random rand = new Random();
                //_mutateOrCross = runMutateOrCrossover(rand);
                _mutateOrCross = 1;
                // always use the given parent shapes, if the auto evolution is ran more than 1 iteration
                // do not add the kid shapes to the parent set
                List<Model> cur_par = new List<Model>(parents);
                List<Model> cur_kids = new List<Model>();
                string runstr = "Run ";
                switch (_mutateOrCross)
                {
                    case 0:
                        // mutate
                        runstr += "Mutate @iteration " + i.ToString();
                        Program.GetFormMain().writeToConsole(runstr);
                        cur_kids = runMutate(cur_par, _currGenId, imageFolder_m, start);
                        break;
                    case 2:
                        runstr += "Growth @iteration " + i.ToString();
                        Program.GetFormMain().writeToConsole(runstr);
                        cur_kids = runGrowth(cur_par, _currGenId, rand, imageFolder_g, start);
                        break;
                    case 1:
                    default:
                        // crossover
                        runstr += "Crossover @iteration " + i.ToString();
                        Program.GetFormMain().writeToConsole(runstr);
                        cur_kids = runAGenerationOfCrossover(_currGenId, rand, imageFolder_c);
                        break;
                }
                curGeneration.AddRange(cur_kids);
                ++_currGenId;
            }// for each iteration
            // rank
            //_currentModelIndexMap = new Dictionary<int, int>();
            //for (int i = 0; i < curGeneration.Count; ++i)
            //{
            //    _currentModelIndexMap.Add(curGeneration[i]._index, i);
            //}
            //List<ModelViewer> sorted = this.rankByHighestCategoryValue(curGeneration, 3);
            int nModels = Math.Min(Functionality._MAX_USE_PRESENT_NUMBER, curGeneration.Count);
            ////List<ModelViewer> sorted = new List<ModelViewer>();
            ////for (int i = 0; i < nModels; ++i )
            ////{
            ////    Model imodel = curGeneration[i];
            ////    sorted.Add(new ModelViewer(imodel, imodel._index, this, _currGenId));
            ////}
            _currGenModelViewers = new List<ModelViewer>();
            for (int j = 0; j < nModels; ++j)
            {
                //_currGenModelViewers.Add(sorted[j]);
                Model imodel = curGeneration[j];
                _currGenModelViewers.Add(new ModelViewer(imodel, imodel._index, this, _currGenId));
            }
            //_userSelectedModels = new List<Model>(prev_parents); // for user selection

            long secs = stopWatch.ElapsedMilliseconds / 1000;
            Program.writeToConsole("Time: " + _currGenId.ToString() + " iteration, " + _ancesterModelViewers.Count.ToString()
            + " orginal models, takes " + secs.ToString() + " senconds.");

            avgTimePerValidOffspring /= validOffspringNumber;
            Program.writeToConsole("Average time to produce a valid offspring is (including filtering invalid ones):" + avgTimePerValidOffspring.ToString()); 

            return _currGenModelViewers;
        }// autoGenerate

        public List<ModelViewer> runEvolution()
        {
            // Register a user ID
            if (!Directory.Exists(userFolder))
            {
                this.registerANewUser();
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            this.decideWhichToDraw(true, false, false, true, false, false);
            //this.decideWhichToDraw(false, true, false, true, false, false);
            this._showContactPoint = true;
            // for capturing screen            
            this.reloadView();

            string validityMatrixFolder = Interface.MODLES_PATH + "ValidityMatrix\\";
            if (!Directory.Exists(validityMatrixFolder))
            {
                Directory.CreateDirectory(validityMatrixFolder);
            }
            string validityMatrixFileName = validityMatrixFolder + "User_" + _userIndex.ToString() +
                "_gen_" + _currGenId.ToString() + ".vdm";

            // set user selection
            string selectedModelPath = userFolder + "\\selected\\";
            if (!Directory.Exists(selectedModelPath))
            {
                Directory.CreateDirectory(selectedModelPath);
            }

            // build the validity matrix, check which two part groups are replaceable
            for (int i = 0; i < _currGenModelViewers.Count; ++i)
            {
                ModelViewer mv = _currGenModelViewers[i];
                int p1 = mv._MODEL._partGroupPair._p1;
                int p2 = mv._MODEL._partGroupPair._p2;
                // fixed bug
                Triplet trip = _validityMatrixPG.GetTriplet(p1, p2);
                if (trip == null)
                {
                    continue; // already removed in the loop
                }
                double prob = _validityMatrixPG.GetTriplet(p1, p2).value;
                double update_prob = 0; // user did not select
                bool increase = _userSelectedModels.Contains(mv._MODEL);
                if (increase)
                {
                    update_prob = Math.Min(1.0, prob * 1.5);
                    //mv._MODEL._GRAPH.initializePartGroups();
                    foreach (PartGroup pg in mv._MODEL._GRAPH._partGroups)
                    {
                        pg._ParentModelIndex = mv._MODEL._index;
                        _partGroups[p1].Add(pg);
                    }
                    _modelIndexMap.Add(mv._MODEL._index, mv._MODEL);
                    _userSelectedModelsBeforeRecompute.Add(mv._MODEL);
                }
                if (update_prob == 0)
                {
                    _validityMatrixPG.RemoveATriplet(p1, p2);
                }
                else
                {
                    _validityMatrixPG.AddTriplet(p1, p2, update_prob);
                    // put in a selected folder                
                    this.saveAPartBasedModel(mv._MODEL, selectedModelPath + mv._MODEL._model_name + ".pam", false);
                }
            }

            // after user selction
            this.saveValidityMatrix(validityMatrixFileName);
            // add user selected models
            if (_currGenId > 0)
            {
                this.saveUserSelections(_currGenId);
            }
            // parent shapes at the current generation
            //List<Model> parents = new List<Model>(_userSelectedModels);           
            if (_currGenId % Functionality._NUM_INTER_BEFORE_RERUN == 0)
            {
                // expand the matrix
                this.expandValidityMatrix();
            }
            // always include the ancient models
            List<Model> parents = new List<Model>(_ancesterModels);

            // run 
            avgTimePerValidOffspring = 0;
            validOffspringNumber = 0;
            int maxIter = 1;
            _userSelectedModels = new List<Model>();
            _currGenModelViewers.Clear();
            _curGenPGmemory = new Dictionary<int, List<int>>();
            _maxUseEmptyGroup = parents.Count;
            List<Model> curGeneration = new List<Model>();

            // pre-process
            this._isPreRun = true;
            string today = DateTime.Today.ToString("MMdd");
            validityMatrixFileName = validityMatrixFolder + "Set_Teaser_1_" + today + ".vdm";
            this.saveValidityMatrix(validityMatrixFileName);
            string timingFilename = validityMatrixFolder + "Set_Teaser_1_" + today + ".time";

            for (int i = 0; i < maxIter; ++i)
            {
                Random rand = new Random();
                string runstr = "Run ";
                runstr += "Crossover @iteration " + i.ToString();
                Program.GetFormMain().writeToConsole(runstr);
                List<Model> cur_kids = preRunTest(imageFolder_c);
                curGeneration.AddRange(cur_kids);
                ++_currGenId;
            }// for each iteration
            int nModels = Math.Min(Functionality._MAX_USE_PRESENT_NUMBER, curGeneration.Count);
            _currGenModelViewers = new List<ModelViewer>();
            for (int j = 0; j < nModels; ++j)
            {
                //_currGenModelViewers.Add(sorted[j]);
                Model imodel = curGeneration[j];
                _currGenModelViewers.Add(new ModelViewer(imodel, imodel._index, this, _currGenId));
            }
            //_userSelectedModels = new List<Model>(prev_parents); // for user selection

            long secs = stopWatch.ElapsedMilliseconds / 1000;
            Program.writeToConsole("Time: " + _currGenId.ToString() + " iteration, " + _ancesterModelViewers.Count.ToString()
            + " orginal models, takes " + secs.ToString() + " senconds.");

            avgTimePerValidOffspring /= validOffspringNumber;
            Program.writeToConsole("Average time to produce a valid offspring is (including filtering invalid ones):" + avgTimePerValidOffspring.ToString());

            return _currGenModelViewers;
        }// runEvolution

        private List<Model> preRunTest(string imageFolder)
        {
            List<Model> res = new List<Model>();
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            res = new List<Model>();
            int nPGs = _partGroupLibrary.Count;
            if (nPGs == 0)
            {
                return res;
            }
            int pairId = 0;
            List<Triplet> invalid = new List<Triplet>();
            for (int t = 0; t < _validityMatrixPG.NTriplets; ++t)
            {
                // the matrix is not symmetric and (i, j) (j, i) could both exist
                int i = _validityMatrixPG.GetTriplet(t).row;
                int j = _validityMatrixPG.GetTriplet(t).col;
                List<Model> ijs;
                //i = 11;
                //j = 4;
                if (!runACrossoverTest(i, j, 1, new Random(), imageFolder, pairId++, out ijs))
                {
                    invalid.Add(_validityMatrixPG.GetTriplet(t));
                }
                res.AddRange(ijs);
            }

            double secs = avgTimePerValidOffspring / validOffspringNumber;
            StringBuilder sb = new StringBuilder();
            sb.Append("Total valid offspring: " + validOffspringNumber.ToString() + "\n");
            sb.Append("Average Time to run a valid crossover: " + secs.ToString() + " senconds.");
            Program.writeToConsole(sb.ToString());
            return res;
        }// preRunTest

        private bool runACrossoverTest(int p1, int p2, int gen, Random rand, string imageFolder, int idx, out List<Model> res)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            res = new List<Model>();
            int nPGs = _partGroupLibrary.Count;
            if (nPGs == 0)
            {
                return false;
            }
            // select two part groups randomly

            Triplet triplet = _validityMatrixPG.GetTriplet(p1, p2);
            int nTriplets = _validityMatrixPG.NTriplets;
            // select parent shapes
            Program.writeToConsole("Crossover: \n");
            Model model1 = _ancesterModels[_partGroupLibrary[p1]._ParentModelIndex];
            Model model2 = _ancesterModels[_partGroupLibrary[p2]._ParentModelIndex];
            // _updated partgroups

            Stopwatch stopWatch_cross = new Stopwatch();
            stopWatch_cross.Start();

            List<Model> results = this.performOneCrossover(model1, model2, gen, idx, p1, p2);

            long secs = stopWatch_cross.ElapsedMilliseconds / 1000;
            Program.writeToConsole("Time to run crossover: " + secs.ToString() + " seconds.");

            int id = -1;
            foreach (Model m in results)
            {
                ++id;
                if (id > 0)
                {
                    break;
                }

                // screenshot
                m.unify();
                m.composeMesh();
                this.setCurrentModel(m, -1);

                if (!m._GRAPH.isValid())
                {
                    m._model_name += "_invalid";
                    Program.GetFormMain().updateStats();
                    this.captureScreen(imageFolder + "invald\\" + m._model_name + ".png");
                    saveAPartBasedModel(m, m._path + m._model_name + ".pam", false);
                    continue;
                }

                Program.GetFormMain().updateStats();
                // valid graph
                int doRestore = rand.Next(1);
                //if (doRestore == 1)
                //{
                    // restore the node randomely
                    //this.tryRestoreFunctionalNodes(m);
                //}
                res.Add(m);
                // save at diff folder
                string splitFolder = imageFolder;
                this.captureScreen(splitFolder + m._model_name + ".png");
                //this.captureScreen(imageFolder + m._model_name + ".png");
                saveAPartBasedModel(m, m._path + m._model_name + ".pam", false);
                m._index = _modelIndex;
                ++_modelIndex;

            }
            secs = stopWatch.ElapsedMilliseconds / 1000;
            Program.writeToConsole("Time to run a crossover: " + secs.ToString() + " senconds.");
            avgTimePerValidOffspring += secs;
            validOffspringNumber += res.Count;
            longestTimePerValidOffspring = longestTimePerValidOffspring > secs ? longestTimePerValidOffspring : secs;

            return true;
        }// runACrossover - part groups

        public List<Model> performOneCrossover(Model m1, Model m2, int gen, int idx, int p1, int p2)
        {
            // replace p1 by p2 in m1
            if (m1 == null || m2 == null)
            {
                return null;
            }
            this.setSelectedNodes(m1, _partGroupLibrary[p1]);
            this.setSelectedNodes(m2, _partGroupLibrary[p2]);
            List<Model> res = new List<Model>();
            Model newModel = m1.Clone() as Model;
            // m1 starts name
            string name = m1._model_name;
            int slashId = name.IndexOf('-');
            if (slashId == -1)
            {
                slashId = name.Length;
            }
            string originalModelName = name.Substring(0, slashId);
            newModel._path = crossoverFolder + "gen_" + gen.ToString() + "\\";
            if (!Directory.Exists(newModel._path))
            {
                Directory.CreateDirectory(newModel._path);
            }
            List<Node> nodes1 = new List<Node>();
            List<Node> nodes2 = new List<Node>(m2._GRAPH.selectedNodes); // unchanged
            List<Node> updatedNodes2 = new List<Node>();
            List<Model> parents = new List<Model>(); // to set parent names
            parents.Add(m1);
            parents.Add(m2);
            foreach (Node node in m1._GRAPH.selectedNodes)
            {
                nodes1.Add(newModel._GRAPH._NODES[node._INDEX]);
            }
            PartGroup pg2 = _partGroupLibrary[p2];
            // 1. deletion
            if (m1._GRAPH.selectedNodes.Count > 0 && m2._GRAPH.selectedNodes.Count == 0)
            {
                newModel = this.deletionOperation(m1, m1._CAT);
                newModel._model_name = originalModelName + "-gen_" + gen.ToString() + "_num_" + idx.ToString() + "_pg_" + p1.ToString() + "_" + p2.ToString() + "_del";
            }
            else if (m1._GRAPH.selectedNodes.Count == 0 && m2._GRAPH.selectedNodes.Count > 0)
            {
                // 2. insertion
                newModel._model_name = originalModelName + "-gen_" + gen.ToString() + "_num_" + idx.ToString() + "_pg_" + p1.ToString() + "_" + p2.ToString() + "_growth";
                // add by cluster
                foreach (List<Node> cluster in pg2._clusters)
                {
                    List<Node> newNodes = this.insertionOperation(newModel, m2, cluster, false);
                    updatedNodes2.AddRange(newNodes);
                }
                newModel.replaceNodes(nodes1, updatedNodes2);
            }
            else
            {
                // 3. replace
                // using model names will be too long, exceed the maximum length of file name
                newModel._model_name = originalModelName + "-gen_" + gen.ToString() + "_num_" + idx.ToString() + "_pg_" + p1.ToString() + "_" + p2.ToString();
                List<Node> reduced = reduceRepeatedNodes(nodes1, nodes2);
                this.setSelectedNodes(m2, reduced);
                // switch
                updatedNodes2 = this.replaceNodes(newModel._GRAPH, m2._GRAPH, nodes1, reduced);
                newModel.replaceNodes(nodes1, updatedNodes2);
                // topology
                newModel._GRAPH.replaceNodes(nodes1, updatedNodes2);    
            }
            newModel.nNewNodes = updatedNodes2.Count;
            newModel.setParentNames(parents);
            res.Add(newModel);
            return res;
        }// crossover

        private List<Node> reduceRepeatedNodes(List<Node> nodes1, List<Node> nodes2)
        {
            // if #nodes2 contain only main functional parts, and its m --> 1
            List<Functionality.Functions> func2 = Functionality.getNodesFunctionalitiesIncludeNone(nodes2);
            int nMainNodes1 = containsNMainNodes(nodes1);
            int nMainNodes2 = containsNMainNodes(nodes2);
            if (func2.Count == 1 && Functionality.IsMainFunction(func2[0]) && nMainNodes2 > nMainNodes1)
            {
                List<Node> nodes = new List<Node>();
                // randomly select one
                Random rand = new Random();
                int id = rand.Next(nodes2.Count);
                nodes.Add(nodes2[id]);
                return nodes;
            }
            return new List<Node>(nodes2);
        }// reduceRepeatedNodes

        private int containsNMainNodes(List<Node> nodes)
        {
            int n = 0;
            foreach (Node node in nodes)
            {
                if (Functionality.ContainsMainFunction(node._funcs))
                {
                    ++n;
                }
            }
            return n;
        }

        private Model deletionOperation(Model m, Functionality.Category cat)
        {
            // can delete if the selected nodes do not remove necessary functions
            Graph g = m._GRAPH;
            List<Functionality.Functions> funcs = new List<Functionality.Functions>();
            foreach (Node node in g._NODES)
            {
                if (!g.selectedNodes.Contains(node))
                {
                    foreach (Functionality.Functions f in node._funcs)
                    {
                        if (!funcs.Contains(f))
                        {
                            funcs.Add(f);
                        }
                    }
                }
            }
            // 
            List<Functionality.Functions> necessaryFuncs = Functionality.getFunctionalityFromCategory(cat);
            var miss = necessaryFuncs.Except(funcs); // Linq
            if (miss.Count() > 0)
            {
                return null;
            }
            Model newModel = m.Clone() as Model;
            List<Node> toRemove = new List<Node>();
            foreach (Node node in m._GRAPH.selectedNodes)
            {
                toRemove.Add(newModel._GRAPH._NODES[node._INDEX]);
            }
            newModel.replaceNodes(toRemove, new List<Node>());
            return newModel;
        }// deletionOperation

        private List<Node> insertionOperation(Model growModel, Model m2, List<Node> insertNodes2, bool front)
        {
            // insert #insertNodes to the growth model #m1
            List<Node> insertNodes = cloneNodesAndRelations(insertNodes2);
            Graph g1 = growModel._GRAPH;
            Graph g2 = m2._GRAPH;
            List<Edge> edgesToConnect = g2.getOutgoingEdges(insertNodes);
            // check what kinds of nodes the part group connect to
            List<Node> nodesToConnect = g2.getOutConnectedNodes(insertNodes);
            List<Functionality.Functions> functions = Functionality.getNodesFunctionalities(nodesToConnect);
            List<Node> candidteNodes = new List<Node>();
            int nConns = 0; // use it as importance of nodes
            Node toConnect = null;
            Node mainNode = null;
            // look for nodes with same functions in g1
            foreach (Node node in g1._NODES)
            {
                foreach (Functionality.Functions f in node._funcs)
                {
                    if (Functionality.IsMainFunction(f))
                    {
                        if (mainNode == null || node._PART._BOUNDINGBOX.CENTER.y > mainNode._PART._BOUNDINGBOX.CENTER.y)
                        {
                             mainNode = node;
                        }
                    }
                    if (functions.Contains(f))
                    {
                        if (!candidteNodes.Contains(node))
                        {
                            candidteNodes.Add(node);
                        }
                        if (node._edges.Count > nConns || (node._edges.Count == nConns && Functionality.IsMainFunction(f)))
                        { 
                            nConns = node._edges.Count;
                            toConnect = node;
                            //break;
                        }
                    }
                }
            }
            //if (toConnect == null)
            //{
            //    // use the main functional node
            //    // find similar struture from its source model
            //    toConnect = mainNode;
            //}
            if (mainNode != null) {
                toConnect = mainNode;
            }
            if (toConnect == null)
            {
                return null;
            }

            Vector3d maxCoordInConn = Vector3d.MinCoord;
            Vector3d minCoordInConn = Vector3d.MaxCoord;
            foreach (Node node in nodesToConnect)
            {
                maxCoordInConn = Vector3d.Max(maxCoordInConn, node._PART._BOUNDINGBOX.MaxCoord);
                minCoordInConn = Vector3d.Min(maxCoordInConn, node._PART._BOUNDINGBOX.MinCoord);
            }
            Vector3d center = new Vector3d();
            foreach (Node node in insertNodes)
            {
                center += node._PART._BOUNDINGBOX.CENTER;
            }
            center /= insertNodes.Count;
            bool isUpper = center.y > maxCoordInConn.y;
            bool isLower = center.y < minCoordInConn.y;
            bool isGround = this.hasGroundTouching(insertNodes);
            if (isGround)
            {
                isUpper = false;
                isLower = true;
            }
            bool isEmbed = !isUpper && !isLower;

            List<Vector3d> targets = new List<Vector3d>();
            List<Vector3d> sourcePnts = collectPoints(edgesToConnect);
            List<Vector3d> sources = new List<Vector3d>();
            Vector3d center1 = new Vector3d();
            Vector3d vmin = toConnect._PART._BOUNDINGBOX.MinCoord;
            Vector3d vmax = toConnect._PART._BOUNDINGBOX.MaxCoord;
            // add nodes and inner edges
            foreach (Node node in insertNodes)
            {
                g1.addANode(node);
            }
            List<Edge> innerEdges = Graph.GetInnerEdges(insertNodes);
            foreach (Edge e in innerEdges)
            {
                g1.addAnEdge(e._start, e._end, e._contacts);
            }
            int ncontacts = 0;
            foreach (Edge e in edgesToConnect)
            {
                ncontacts += e._contacts.Count;
            }
            Vector3d vcenter = new Vector3d(vmin);
            if (front)
            {
                vcenter.z = vmin.z + (vmax.z - vmin.z) * 0.9;
            }

            //
            Vector3d maxCoordInSrc = Vector3d.MinCoord;
            Vector3d minCoordInSrc = Vector3d.MaxCoord;
            foreach (Edge e in edgesToConnect)
            {
                Node toInsert = insertNodes.Contains(e._start) ? e._start : e._end;
                Node nodeInContact = e._start == toInsert ? e._end : e._start;
                // local xyz
                Vector3d v1 = nodeInContact._PART._BOUNDINGBOX.MinCoord;
                Vector3d v2 = nodeInContact._PART._BOUNDINGBOX.MaxCoord;
                maxCoordInSrc = Vector3d.Max(maxCoordInSrc, v2);
                minCoordInSrc = Vector3d.Min(minCoordInSrc, v1);
            }
            foreach (Edge e in edgesToConnect)
            {
                Node toInsert = insertNodes.Contains(e._start) ? e._start : e._end;
                Node nodeInContact = e._start == toInsert ? e._end : e._start;
                List<Contact> newContacts = new List<Contact>();
                foreach (Contact c in e._contacts)
                {
                    Vector3d v = c._pos3d;
                    center1 += v;
                    // try to find correspondence in target
                    Vector3d s1 = (v - minCoordInSrc) / (maxCoordInSrc - minCoordInSrc);
                    //s1 = new Vector3d(1 - s1[0], 1-s1[1], 1 - s1[2]);
                    //if (ncontacts == 2)
                    //{
                    //    s1.y = 0;
                    //}
                    Vector3d s2 = vcenter + s1 * (vmax - vmin);
                    sources.Add(v);
                    targets.Add(s2);
                    newContacts.Add(new Contact(s2));
                }
                g1.addAnEdge(toInsert, toConnect, newContacts);
                // remove old out edges
                toInsert.deleteAnEdge(e);
            }
            center1 /= sourcePnts.Count;

            if (isGround)
            {
                targets.Add(g1.getGroundTouchingNodesCenter());
                sources.Add(g2.getGroundTouchingNodesCenter());
            }

            Vector3d center2 = new Vector3d();
            Vector3d maxv = Vector3d.MinCoord;
            Vector3d minv = Vector3d.MaxCoord;
            foreach (Node node in insertNodes)
            {
                center2 += node._PART._BOUNDINGBOX.CENTER;
                maxv = Vector3d.Max(maxv, node._PART._BOUNDINGBOX.MaxCoord);
                minv = Vector3d.Min(minv, node._PART._BOUNDINGBOX.MinCoord);
            }
            center2 = (maxv + minv) / 2;

            Vector3d scale2 = new Vector3d(1, 1, 1);

            Matrix4d S, T, Q;
            getTransformation(sources, targets, out S, out T, out Q, scale2, false, center1, center2, false, -1, isGround);
            this.deformNodesAndEdges(insertNodes, Q);

            if (isGround)
            {
                g1.resetUpdateStatus();
                adjustGroundTouching(insertNodes);
            }
            g1.resetUpdateStatus();
            return insertNodes;
        }// insertionOperation

        private Vector3d[] getScales(List<Node> nodes)
        {
            Vector3d[] res = new Vector3d[3];
            Vector3d center = new Vector3d();
            Vector3d maxv_t = Vector3d.MinCoord;
            Vector3d minv_t = Vector3d.MaxCoord;
            foreach (Node node in nodes)
            {
                center += node._PART._BOUNDINGBOX.CENTER;
                maxv_t = Vector3d.Max(maxv_t, node._PART._BOUNDINGBOX.MaxCoord);
                minv_t = Vector3d.Min(minv_t, node._PART._BOUNDINGBOX.MinCoord);
            }
            //center /= nodes.Count;
            center = (maxv_t + minv_t) / 2;
            res[0] = center;
            res[1] = maxv_t;
            res[2] = minv_t;
            return res;
        }

        private List<Node> replaceNodes(Graph g1, Graph g2, List<Node> nodes1, List<Node> nodes2)
        {
            // replace nodes1 by nodes2 in g1
            List<Node> updateNodes2 = cloneNodesAndRelations(nodes2);

            List<Edge> edgesToConnect_1 = g1.getOutgoingEdges(nodes1);
            List<Edge> edgesToConnect_2 = g2.getOutgoingEdges(nodes2);
            List<Vector3d> targets = collectPoints(edgesToConnect_1);
            List<Vector3d> sources = collectPoints(edgesToConnect_2);

            List<Node> ground1 = this.getGroundTouchingNode(nodes1);
            List<Node> ground2 = this.getGroundTouchingNode(nodes2);
            if (ground1.Count == 0 && ground2.Count > 0)
            {
                foreach (Node gn in ground2)
                {
                    Vector3d[] groundPoints = this.getGroundTouchingPoints(gn, ground2.Count == 2 ? 2 : 1);
                    if (groundPoints != null)
                    {
                        for (int i = 0; i < groundPoints.Length; ++i)
                        {
                            sources.Add(groundPoints[i]);
                        }
                    }
                }
            }

            Vector3d[] scaleVecs_1 = this.getScales(nodes1);
            Vector3d[] scaleVecs_2 = this.getScales(nodes2);

            Vector3d center1 = scaleVecs_1[0];
            Vector3d maxv_t = scaleVecs_1[1];
            Vector3d minv_t = scaleVecs_1[2];

           
            Vector3d center2 = scaleVecs_2[0];
            Vector3d maxv_s = scaleVecs_2[1];
            Vector3d minv_s = scaleVecs_2[2];


            double[] scale1 = { 1.0, 1.0, 1.0 };
            if (nodes1.Count > 0)
            {
                scale1[0] = (maxv_t.x - minv_t.x) / (maxv_s.x - minv_s.x);
                scale1[1] = (maxv_t.y - minv_t.y) / (maxv_s.y - minv_s.y);
                scale1[2] = (maxv_t.z - minv_t.z) / (maxv_s.z - minv_s.z);
            }

            double[] scale2 = { 1.0, 1.0, 1.0 };
            if (nodes2.Count > 0)
            {
                scale2[0] = (maxv_s.x - minv_s.x) / (maxv_t.x - minv_t.x);
                scale2[1] = (maxv_s.y - minv_s.y) / (maxv_t.y - minv_t.y);
                scale2[2] = (maxv_s.z - minv_s.z) / (maxv_t.z - minv_t.z);
            }

            int axis = this.hasCylinderNode(nodes2);
            Vector3d boxScale = new Vector3d(scale1[0], scale1[1], scale1[2]);

            // try to scale and translate the new part group to the target place
            // and estimate the contact mapping from there
            Matrix4d simS = Matrix4d.ScalingMatrix(boxScale);
            Matrix4d simQ = Matrix4d.TranslationMatrix(center1) * simS * Matrix4d.TranslationMatrix(new Vector3d() - center2);
            List<Vector3d> simPoints = new List<Vector3d>();
            for (int i = 0; i < sources.Count; ++i)
            {
                Vector3d simV = (simQ * new Vector4d(sources[i], 1)).ToVector3D();
                simPoints.Add(simV);
            }

            Matrix4d S = Matrix4d.ScalingMatrix(1, 1, 1);
            Matrix4d T = Matrix4d.IdentityMatrix();
            Matrix4d Q = Matrix4d.IdentityMatrix();
            // sort corresponding points
            int nps = targets.Count;
            bool swapped = false;
            List<Vector3d> left = targets;
            List<Vector3d> right = simPoints;
            if (sources.Count < nps)
            {
                nps = sources.Count;
                swapped = true;
                left = simPoints;
                right = targets;
            }
            List<Vector3d> src = new List<Vector3d>();
            List<Vector3d> trt = new List<Vector3d>();
            bool[] visited = new bool[right.Count];

            for (int i = 0; i < left.Count; ++i)
            {
                Vector3d leftv = left[i];
                src.Add(swapped ? sources[i] : leftv); // left use sim points
                int t = -1;
                double mind = double.MaxValue;
                for (int j = 0; j < right.Count; ++j)
                {
                    if (visited[j]) continue;
                    double d = (leftv - right[j]).Length();
                    if (d < mind)
                    {
                        mind = d;
                        t = j;
                    }
                }
                trt.Add(swapped ? right[t] : sources[t]); // right use sim points
                visited[t] = true;
            }

            if (swapped)
            {
                targets = trt;
                sources = src;
            }
            else
            {
                targets = src;
                sources = trt;
            }

            bool useScale = targets.Count == 0 || sources.Count == 0 || left.Count >= right.Count * 2 || right.Count >= left.Count * 2;

            useScale = updateNodes2.Count == 1 && !Functionality.ContainsMainFunction(updateNodes2[0]._funcs);

            if (sources.Count == 0)
            {
                useScale = true;
                boxScale = new Vector3d(1, 1, 1);
            }

            if (targets.Count < 1)
            {
                targets.Add(center1);
                sources.Add(center2);
            }
            bool useGround = ground1.Count > 0 && ground2.Count > 0;
            if (useGround)
            {
                //targets.Add(this.getGroundTouchingNodesCenter(nodes1));
                //sources.Add(this.getGroundTouchingNodesCenter(nodes2));
                targets.Add(new Vector3d(center1.x, 0, center1.z));
                sources.Add(new Vector3d(center2.x, 0, center2.z));
            }
            bool userCenter = false;
            double storageScale = -1;

            if (crossoverFolder.Contains("set_1"))
            {
                useScale = nodes1.Count == 1 || nodes2.Count == 1 || left.Count >= right.Count * 2 || right.Count >= left.Count * 2;
            }
            if (this.containsFunc(updateNodes2, Functionality.Functions.STORAGE))
            {
                useScale = true;
                boxScale[1] = 1.0;
            }
            // when replacing a storage part by a placement part, the volume is not necessary
            if (this.containsFunc(nodes1, Functionality.Functions.STORAGE)
                && updateNodes2.Count == 1 && updateNodes2[0]._funcs.Contains(Functionality.Functions.PLACEMENT))
            {
                storageScale = 1.0;
            }

            if (nodes1.Count > 0 && nodes2.Count > 0)
            {
                getTransformation(sources, targets, out S, out T, out Q, boxScale, useScale, center1, center2, userCenter, storageScale, useGround);
                this.deformNodesAndEdges(updateNodes2, Q);
                g1.resetUpdateStatus();
                this.restoreCyclinderNodes(updateNodes2, S);
            }


            if (ground1 != null)
            {
                g1.resetUpdateStatus();
                //adjustGroundTouching(updateNodes2);
            }
            g1.resetUpdateStatus();

            return updateNodes2;
        }// replaceNodes

        private List<Node> replaceNodes(Graph g1, Graph g2, List<Vector3d> targets, List<Node> nodes2)
        {
            // replace nodes1 by nodes2 in g1
            List<Node> updateNodes2 = cloneNodesAndRelations(nodes2);

            List<Edge> edgesToConnect_2 = g2.getOutgoingEdges(nodes2);
            List<Vector3d> sources = collectPoints(edgesToConnect_2);

            Vector3d maxv_s = Vector3d.MinCoord;
            Vector3d minv_s = Vector3d.MaxCoord;
            Vector3d maxv_t = Vector3d.MinCoord;
            Vector3d minv_t = Vector3d.MaxCoord;

            Vector3d center1 = new Vector3d();

            Vector3d center2 = new Vector3d();
            foreach (Node node in nodes2)
            {
                center2 += node._PART._BOUNDINGBOX.CENTER;
                maxv_s = Vector3d.Max(maxv_s, node._PART._BOUNDINGBOX.MaxCoord);
                minv_s = Vector3d.Min(minv_s, node._PART._BOUNDINGBOX.MinCoord);
            }
            center2 /= nodes2.Count;
            center1.y = center2.y;

            double[] scale1 = { 1.0, 1.0, 1.0 };

            double[] scale2 = { 1.0, 1.0, 1.0 };
            if (nodes2.Count > 0)
            {
                scale2[0] = (maxv_s.x - minv_s.x) / (maxv_t.x - minv_t.x);
                scale2[1] = (maxv_s.y - minv_s.y) / (maxv_t.y - minv_t.y);
                scale2[2] = (maxv_s.z - minv_s.z) / (maxv_t.z - minv_t.z);
            }

            int axis = this.hasCylinderNode(nodes2);
            Vector3d boxScale = new Vector3d(scale1[0], scale1[1], scale1[2]);

            // try to scale and translate the new part group to the target place
            // and estimate the contact mapping from there
            Matrix4d simS = Matrix4d.ScalingMatrix(boxScale);
            Matrix4d simQ = Matrix4d.TranslationMatrix(center1) * simS * Matrix4d.TranslationMatrix(new Vector3d() - center2);
            List<Vector3d> simPoints = new List<Vector3d>();
            for (int i = 0; i < sources.Count; ++i)
            {
                Vector3d simV = (simQ * new Vector4d(sources[i], 1)).ToVector3D();
                simPoints.Add(simV);
            }

            Matrix4d S, T, Q;
            // sort corresponding points
            int nps = targets.Count;
            bool swapped = false;
            List<Vector3d> left = targets;
            List<Vector3d> right = simPoints;
            if (sources.Count < nps)
            {
                nps = sources.Count;
                swapped = true;
                left = simPoints;
                right = targets;
            }
            List<Vector3d> src = new List<Vector3d>();
            List<Vector3d> trt = new List<Vector3d>();
            bool[] visited = new bool[right.Count];

            for (int i = 0; i < left.Count; ++i)
            {
                Vector3d leftv = left[i];
                src.Add(swapped ? sources[i] : leftv); // left use sim points
                int t = -1;
                double mind = double.MaxValue;
                for (int j = 0; j < right.Count; ++j)
                {
                    if (visited[j]) continue;
                    double d = (leftv - right[j]).Length();
                    if (d < mind)
                    {
                        mind = d;
                        t = j;
                    }
                }
                trt.Add(swapped ? right[t] : sources[t]); // right use sim points
                visited[t] = true;
            }

            if (swapped)
            {
                targets = trt;
                sources = src;
            }
            else
            {
                targets = src;
                sources = trt;
            }

            bool useScale = targets.Count == 0 || sources.Count == 0 || left.Count >= right.Count * 2 || right.Count >= left.Count * 2;
            useScale = updateNodes2.Count == 1 && !Functionality.ContainsMainFunction(updateNodes2[0]._funcs);

            if (targets.Count < 1)
            {
                targets.Add(center1);
                sources.Add(center2);
            }

            bool userCenter = false;
            double storageScale = -1;

            if (this.containsFunc(updateNodes2, Functionality.Functions.STORAGE))
            {
                useScale = true;
                boxScale[1] = 1.0;
            }

            if (nodes2.Count > 0)
            {
                getTransformation(sources, targets, out S, out T, out Q, boxScale, useScale, center1, center2, 
                    userCenter, storageScale, false);
                this.deformNodesAndEdges(updateNodes2, Q);
                g1.resetUpdateStatus();
                this.restoreCyclinderNodes(updateNodes2, S);
            }

            g1.resetUpdateStatus();

            return updateNodes2;
        }// replaceNodes


        private bool containsFunc(List<Node> nodes, Functionality.Functions f)
        {
            foreach (Node node in nodes)
            {
                if (node._funcs.Contains(f))
                {
                    return true;
                }
            }
            return false;
        }// containsStorageFuncs

        public Vector3d getGroundTouchingNodesCenter(List<Node> nodes)
        {
            Vector3d center = new Vector3d();
            int n = 0;
            foreach (Node node in nodes)
            {
                if (node._isGroundTouching)
                {
                    center += node._PART._BOUNDINGBOX.CENTER;
                    ++n;
                }
            }
            center /= n;
            center.y = 0;
            return center;
        }// getGroundTouchingNode

        private void restoreCyclinderNodes(List<Node> nodes, Matrix4d S)
        {
            // restore nodes if cyclinder
            Vector3d qscale = new Vector3d(S[0, 0], S[1, 1], S[2, 2]);
            Vector3d[] scales = new Vector3d[3];
            int[] nums = new int[3];
            List<Node> toUpdate = new List<Node>();
            List<int> axes = new List<int>();
            for (int i = 0; i < 3; ++i)
            {
                scales[i] = new Vector3d();
            }
            foreach (Node node in nodes)
            {
                if (node._PART._BOUNDINGBOX.type == Common.PrimType.Cylinder)
                {
                    int rot_axis = this.getAxisAlignedAxis(node._PART._BOUNDINGBOX.rot_axis);
                    Vector3d rescale = rescaleForCylinder(rot_axis, qscale);
                    scales[rot_axis] += rescale;
                    nums[rot_axis]++;
                    toUpdate.Add(node);
                    axes.Add(rot_axis);
                    
                }
            }
            for (int i = 0; i < 3; ++i)
            {
                scales[i] /= nums[i];
                scales[i] = scales[i] / qscale;
                scales[i][i] = 1.0;
            }
            for (int i = 0; i < toUpdate.Count; ++i)
            {
                Vector3d center = toUpdate[i]._PART._BOUNDINGBOX.CENTER;
                Matrix4d rT = Matrix4d.TranslationMatrix(center) * Matrix4d.ScalingMatrix(scales[axes[i]]) * Matrix4d.TranslationMatrix(new Vector3d() - center);
                toUpdate[i].Transform(rT);
                // all contacts
                foreach (Edge e in toUpdate[i]._edges)
                {
                    if (e._contactUpdated)
                    {
                        continue;
                    }
                    e.TransformContact(rT);
                }
            }
        }// restoreCyclinderNodes

        private Vector3d rescaleForCylinder( int axis, Vector3d scale)
        {
            // reverse the scale back
            Vector3d rescale = new Vector3d(scale);
            double newScale = 1.0;
            if (axis == 0)
            {
                newScale = Math.Min(scale.y, scale.z); // (scale.y + scale.z) / 2;
                rescale.y = newScale;
                rescale.z = newScale;
            }
            else if (axis == 1)
            {
                newScale = Math.Min(scale.x, scale.z); // (scale.x + scale.z) / 2;
                rescale.x = newScale;
                rescale.z = newScale;
            }
            else if (axis == 2)
            {
                newScale = Math.Min(scale.x, scale.y); // (scale.x + scale.y) / 2;
                rescale.x = newScale;
                rescale.y = newScale;
            }
            return rescale;
        }// rescaleForCylinder

        private void calculatePartGroupCompatibility(List<Model> models)
        {
            if (models == null || models.Count == 0)
            {
                return;
            }
            // evaluate pairs of part groups
            _partGroupLibrary = new List<PartGroup>();
            _functionalPartScales = new Dictionary<string, Vector3d>();
            // list all part groups and index
            int id = 0;
            int mid = 0;
            foreach (Model m in models)
            {
                // add an empty
                PartGroup empty = new PartGroup(new List<Node>(), 0);
                empty._INDEX = id++;
                empty._ParentModelIndex = mid;
                _partGroupLibrary.Add(empty);
                foreach (PartGroup pg in m._GRAPH._partGroups)
                {
                    pg._INDEX = id++;
                    _partGroupLibrary.Add(pg);
                }
                foreach (Node node in m._GRAPH._NODES)
                {
                    string part_name = node._PART._partName;
                    node.calRatios();
                    _functionalPartScales.Add(part_name, node._ratios);
                }
                ++mid;
            }
            int n = _partGroupLibrary.Count;
            _validityMatrixPG = new SparseMatrix(n, n);
            for (int i = 0; i < n - 1; ++i)
            {
                Model m1 = _ancesterModels[_partGroupLibrary[i]._ParentModelIndex];
                for (int j = i + 1; j < n; ++j)
                {
                    Model m2 = _ancesterModels[_partGroupLibrary[j]._ParentModelIndex];
                    double[] comp = Functionality.GetPartGroupCompatibility(m1, m2, _partGroupLibrary[i], _partGroupLibrary[j]);
                    if (comp[0] > 0 && comp[1] > 0)
                    {
                        _validityMatrixPG.AddTriplet(i, j, comp[0]);
                        _validityMatrixPG.AddTriplet(j, i, comp[1]);
                    } else if (comp[0] > 0)
                    {
                        _validityMatrixPG.AddTriplet(i, j, comp[0]);
                    } else if (comp[1] > 0)
                    {
                        _validityMatrixPG.AddTriplet(j, i, comp[1]);
                    }
                }
            }
        }// calculatePartGroupCompatibility

        private List<Vector3d> sortInOrder(List<Vector3d> points)
        {
            // *roughly* from lower left to upper right
            List<Vector3d> sorted = new List<Vector3d>();
            Vector3d vmin = Vector3d.MaxCoord;
            Vector3d vmax = Vector3d.MinCoord;
            foreach (Vector3d pnt in points)
            {
                vmin = Vector3d.Min(vmin, pnt);
                vmax = Vector3d.Max(vmax, pnt);
            }
            bool[] added = new bool[points.Count];
            double mind = double.MaxValue;
            int id = -1;
            for (int i = 0; i < points.Count; ++i)
            {
                double dis = (vmin - points[i]).Length();
                if (dis < mind)
                {
                    mind = dis;
                    id = i;
                }
            }
            added[id] = true;
            sorted.Add(points[id]);
            while (sorted.Count < points.Count)
            {
                mind = double.MaxValue;
                id = -1;
                Vector3d cur = sorted[sorted.Count - 1];
                for (int i = 0; i < points.Count; ++i)
                {
                    if (added[i]) continue;
                    double dis = (cur - points[i]).Length();
                    if (dis < mind)
                    {
                        mind = dis;
                        id = i;
                    }
                }
                added[id] = true;
                sorted.Add(points[id]);
            }
            return sorted;
        }// sortInOrder

        private void updateSimilarGroups(int p1, int p2, bool increase)
        {
            // after user choice, update similar pairs by func
            PartGroup pg1 = _partGroups[p1][0]; // the parts are the same, just with different parent shapes
            PartGroup pg2 = _partGroups[p2][0];
            List<Functionality.Functions> funcs1 = this.getFunctionalityOfAPartGroup(pg1);
            List<Functionality.Functions> funcs2 = this.getFunctionalityOfAPartGroup(pg2);
            int n = _partGroups.Count;
            for (int i = 0; i < n - 1; ++i)
            {
                PartGroup ipg = _partGroups[i][0];
                List<Functionality.Functions> ifuncs = this.getFunctionalityOfAPartGroup(ipg);
                for (int j = i + 1; j < n; ++j)
                {
                    if (i == p1 && j == p2)
                    {
                        continue;
                    }
                    PartGroup jpg = _partGroups[j][0];
                    List<Functionality.Functions> jfuncs = this.getFunctionalityOfAPartGroup(jpg);
                    if ((this.containsSameFunctionalities(ifuncs, funcs1) && this.containsSameFunctionalities(jfuncs, funcs2))
                        || (this.containsSameFunctionalities(ifuncs, funcs2) && this.containsSameFunctionalities(jfuncs, funcs1)))
                    {
                        // update
                        Triplet trip = _validityMatrixPG.GetTriplet(i, j);
                        if (trip != null)
                        {
                            double val = trip.value;
                            double update_val = 0;
                            if (increase)
                            {
                                update_val = Math.Min(1.0, val * 1.5);
                            }
                            _validityMatrixPG.AddTriplet(i, j, update_val);
                        }
                    }
                }
            }
        }// updateSimilarGroups

        private List<Functionality.Functions> getFunctionalityOfAPartGroup(PartGroup pg)
        {
            List<Functionality.Functions> funcs = new List<Functionality.Functions>();
            foreach (Node node in pg._NODES)
            {
                foreach (Functionality.Functions func in node._funcs)
                {
                    if (!funcs.Contains(func))
                    {
                        funcs.Add(func);
                    }
                }
            }
            return funcs;
        }// getFunctionalityOfAPartGroup

        private bool containsSameFunctionalities(List<Functionality.Functions> funcs1, List<Functionality.Functions> funcs2)
        {
            if (funcs1 == null || funcs2 == null||funcs1.Count != funcs2.Count)
            {
                return false;
            }
            foreach (Functionality.Functions func in funcs1)
            {
                if (!funcs2.Contains(func))
                {
                    return false;
                }
            }
            return true;
        }// containsSameFunctionalities

        public List<ModelViewer> sortEvolutionResults()
        {
            if (_currGenModelViewers.Count == 0)
            {
                return _currGenModelViewers;
            }
            Random rand = new Random();
            int m = _currGenModelViewers.Count;
            int n = _inputSetCats.Count;
            List<Model> models = new List<Model>();
            double[,] scores = new double[m, n];
            for (int i = 0; i < m; ++i )
            {
                Model model = _currGenModelViewers[i]._MODEL;
                models.Add(model);
                double[] ss = runFunctionalityTest(model);
                //// TEST
                //double[] ss = new double[Functionality._NUM_CATEGORIY];
                //for (int k = 0; k < ss.Length; ++k)
                //{
                //    ss[k] = rand.NextDouble();
                //}
                // only use input set
                int j = 0;
                foreach (int c in _inputSetCats)
                {
                    scores[i, j++] = ss[c];
                }
            }
            //List<int> sortedIndex = this.rankOffspringByICONfeatures(models);
            // sort 
            int[,] rankMat = new int[m, n];
            List<double> ranks = new List<double>(); // for sorting
            List<double> sums = new List<double>(); 
            for (int i = 0; i < m; ++i)
            {
                ranks.Add(0);
                sums.Add(0);
            }
            for (int j = 0; j < n; ++j)
            {
                Dictionary<double, int> sortIndex = new Dictionary<double, int>();
                List<double> simVals = new List<double>();
                for (int i = 0; i < m; ++i)
                {
                    sortIndex.Add(scores[i, j], i);
                    simVals.Add(scores[i, j]);
                    sums[i] += scores[i, j];
                }
                simVals.Sort((a, b) => b.CompareTo(a));
                for (int i = 0; i < m; ++i)
                {
                    if (!sortIndex.TryGetValue(simVals[i], out rankMat[i, j]))
                    {
                        MessageBox.Show("Miss data when sorting the similarity.");
                    }
                    ranks[i] += rankMat[i, j]; // has repeat values
                }
            }
            Dictionary<double, int> rankDict = new Dictionary<double, int>();
            for (int i = 0; i < m; ++i)
            {
                //rankDict.Add(ranks[i], i);
                rankDict.Add(sums[i], i);
            }
            //ranks.Sort((a, b) => b.CompareTo(a));
            sums.Sort((a, b) => b.CompareTo(a));
            List<ModelViewer> sorted = new List<ModelViewer>(_currGenModelViewers);
            _currGenModelViewers = new List<ModelViewer>();
            for (int i = 0; i < m; ++i)
            {
                int cur = -1;
                //rankDict.TryGetValue(ranks[i], out cur);
                rankDict.TryGetValue(sums[i], out cur);
                _currGenModelViewers.Add(sorted[cur]);
            }
            return _currGenModelViewers;
        }// sortEvolutionResults

        private List<Model> preRun(string imageFolder)
        {
            List<Model> res = new List<Model>();
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            res = new List<Model>();
            int nPGs = _partGroups.Count;
            if (nPGs == 0)
            {
                return res;
            }
            int pairId = 0; //412, i: 15, j: 24
            for (int i = 0; i < nPGs - 1; ++i)
            {
                //if (!_partGroups[i][0].containsMainFuncPart())
                //{
                //    continue;
                //}
                for (int j = i + 1; j < nPGs; ++j)
                {
                    if (_partGroups[i][0]._ParentModelIndex == _partGroups[j][0]._ParentModelIndex)
                    {
                        continue;
                    }
                    //this.reloadView();
                    //if (!_partGroups[j][0].containsMainFuncPart())
                    //{
                    //    continue;
                    //}
                    List<Model> ijs;
                    runACrossover(i, j, 1, new Random(), imageFolder, pairId++, out ijs);
                    Random rand = new Random();
                    _validityMatrixPG.AddTriplet(i, j, 0.5 + rand.NextDouble() / 2);
                    _validityMatrixPG.AddTriplet(j, i, 0.5 + rand.NextDouble() / 2);
                }
            }
            double secs = avgTimePerValidOffspring / validOffspringNumber;
            StringBuilder sb = new StringBuilder();
            sb.Append("Total valid offspring: " + validOffspringNumber.ToString() + "\n");
            sb.Append("Average Time to run a valid crossover: " + secs.ToString() + " senconds.");
            Program.writeToConsole(sb.ToString());
            return res;
        }// preRun

        private List<Model> runAGenerationOfCrossover(int gen, Random rand, string imageFolder)
        {
            List<Model> crossed = new List<Model>();
            while (crossed.Count < Functionality._MAX_GEN_HYBRID_NUMBER)
            {
                List<Model> res = new List<Model>();
                if (!this.runACrossover(-1, -1, gen, rand, imageFolder, _modelViewIndex + crossed.Count, out res))
                {
                    // couldn't find any more
                    MessageBox.Show("Do not have any good choices.");
                    break;
                }
                crossed.AddRange(res);
            }
            //this.runACrossover(-1, -1, gen, rand, imageFolder, _modelViewIndex + crossed.Count, out crossed);
            return crossed;
        }// runAGenerationOfCrossover

        private bool runACrossover(int p1, int p2, int gen, Random rand, string imageFolder, int idx, out List<Model> res)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            res = new List<Model>();
            int nPGs = _partGroups.Count;
            if (nPGs == 0)
            {
                return false;
            }
            // select two part groups randomly
            
            Triplet triplet = null;
            int ntry = 0;
            bool selectHighProb = true; // _currGenId % 2 == 0 ? false : true;
            int nTriplets = _validityMatrixPG.NTriplets;

            if (p1 == -1 && p2 == -1)
            {
                bool selected = false;
                while (!selected && ntry < Functionality._MAX_TRY_TIMES)
                {
                    int triId = rand.Next(nTriplets);
                    triplet = _validityMatrixPG.GetTriplet(triId);
                    if (isAGoodSelectionOfPartGroupPair(triplet, selectHighProb))
                    {
                        selected = true;
                        p1 = triplet.row;
                        p2 = triplet.col;
                        Program.writeToConsole("Similarity of the selected part group: " + triplet.value.ToString());
                        break;
                    }
                    ++ntry;
                }
                if (!selected)
                {
                    return false;
                }
            }
            triplet = _validityMatrixPG.GetTriplet(p1, p2);
            // select parent shapes
            Program.writeToConsole("Crossover: \n");
            Model model1 = this.selectAPartGroupAndParentModel(p1, rand);
            Model model2 = this.selectAPartGroupAndParentModel(p2, rand);
            // _updated partgroups
            PartGroup pg1;
            PartGroup pg2;
            Stopwatch stopWatch_cross = new Stopwatch();
            stopWatch_cross.Start();
            List<Model> results = this.crossOverOp(model1, model2, gen, idx, p1, p2, out pg1, out pg2);
            long secs = stopWatch_cross.ElapsedMilliseconds / 1000;
            Program.writeToConsole("Time to run crossover: " + secs.ToString() + " senconds.");

            //if (!_isPreRun && results.Count == 2)
            //{
            //    // use only one
            //    results.RemoveAt(1);
            //}
            int id = -1;
            foreach (Model m in results)
            {
                ++id;
                Program.writeToConsole("");
                Stopwatch stopWatch_eval = new Stopwatch();
                stopWatch_eval.Start();

                bool hasAnyFunctionalPart = m._GRAPH.hasAnyNonObstructedFunctionalPart(-1);
                //if (!hasAnyFunctionalPart)
                //{
                //    // screenshot
                //    this.isDrawFunctionalSpaceAgent = true;
                //    this.setCurrentModel(m, -1);
                //    Program.GetFormMain().updateStats();
                //    this.captureScreen(imageFolder + "invald\\" + m._model_name + "_obstructed.png");
                //    saveAPartBasedModel(m, m._path + m._model_name + "_obstructed.pam", false);
                //    if (id == 0)
                //    {
                //        _validityMatrixPG.RemoveATriplet(p1, p2);
                //    }
                //    else
                //    {
                //        _validityMatrixPG.RemoveATriplet(p2, p1);
                //    }
                //    this.isDrawFunctionalSpaceAgent = false;
                //    continue;
                //}

                if (!m._GRAPH.isValid())
                {
                    // screenshot
                    this.setCurrentModel(m, -1);
                    Program.GetFormMain().updateStats();
                    this.captureScreen(imageFolder + "invald\\" + m._model_name + "_invalid.png");
                    saveAPartBasedModel(m, m._path + m._model_name + "_invalid.pam", false);
                    if (id == 0)
                    {
                        _validityMatrixPG.RemoveATriplet(p1, p2);
                    }
                    else
                    {
                        _validityMatrixPG.RemoveATriplet(p2, p1);
                    }
                    continue;
                }

                // valid graph
                this.tryRestoreFunctionalNodes(m);
                m.unify();
                m.composeMesh();
                // record the post analysis feature - REPEAT the last statement, REMOVED after testing
                StringBuilder sb = new StringBuilder();
                // add to the model
                List<Functionality.Category> cats = new List<Functionality.Category>();
                List<double> scores = new List<double>();
                List<double> probs1 = new List<double>();
                List<double> probs2 = new List<double>();
                List<double> probs12 = new List<double>();
                //double[,] point_features = this.computePointFeatures(m);
                m._GRAPH._functionalityValues = new FunctionalityFeatures();
                m._GRAPH._functionalityValues.addParentCategories(model1._GRAPH._functionalityValues._parentCategories);
                m._GRAPH._functionalityValues.addParentCategories(model2._GRAPH._functionalityValues._parentCategories);

                if (this._isPreRun)
                {
                    //double[,] vals = this.partialMatching(m, false);
                    double[,] vals = new double[Functionality._NUM_CATEGORIY, 4];

                    for (int j = 0; j < Functionality._NUM_CATEGORIY; ++j)
                    {
                        cats.Add((Functionality.Category)j);
                        scores.Add(vals[j, 0]);
                        probs1.Add(vals[j, 1]);
                        probs2.Add(vals[j, 2]);
                        probs12.Add(vals[j, 3]);
                    }
                    m._GRAPH._functionalityValues._cats = cats.ToArray();
                    m._GRAPH._functionalityValues._funScores = scores.ToArray();
                    m._GRAPH._functionalityValues._inClassProbs = probs1.ToArray();
                    m._GRAPH._functionalityValues._outClassProbs = probs2.ToArray();
                    m._GRAPH._functionalityValues._classProbs = probs12.ToArray();

                    for (int j = 0; j < _inputSetCats.Count; ++j)
                    {
                        int cid = _inputSetCats[j];
                        sb.Append(Functionality.getCategoryName(cid));
                        sb.Append(" Score: ");
                        sb.Append(scores[cid].ToString());
                        sb.Append(" P_1: ");
                        sb.Append(this.double2String(m._GRAPH._functionalityValues._inClassProbs[cid]));
                        sb.Append(" P_2: ");
                        sb.Append(this.double2String(m._GRAPH._functionalityValues._outClassProbs[cid]));
                        sb.Append(" P_1_2: ");
                        sb.Append(this.double2String(m._GRAPH._functionalityValues._classProbs[cid]));
                        sb.Append("\n");
                    }

                    int cid1 = (int)model1._GRAPH._functionalityValues._parentCategories[0];
                    int cid2 = (int)model2._GRAPH._functionalityValues._parentCategories[0];
                    double maxValidity = 0;
                    if (Functionality.isKnownCategory(cid1) && Functionality.isKnownCategory(cid2))
                    {
                        Math.Max(m._GRAPH._functionalityValues._classProbs[cid1], m._GRAPH._functionalityValues._classProbs[cid2]);
                    }
                    if (id == 0)
                    {
                        _validityMatrixPG.AddTriplet(p1, p2, maxValidity);
                    }else
                    {
                        _validityMatrixPG.AddTriplet(p2, p1, maxValidity);
                    }
                    if (_partGroups[p1][0]._isSymmBreak || _partGroups[p2][0]._isSymmBreak)
                    {
                        ++_nValidSymBreakUsed;
                    }
                }
                else
                {
                    if (triplet != null)
                    {
                        sb.Append("Valid probability: " + triplet.value.ToString());
                    }
                }
                Program.GetFormMain().writePostAnalysisInfo(sb.ToString());

                //this.saveSamplePointsRequiredInfo(m);
                secs = stopWatch_cross.ElapsedMilliseconds / 1000;
                Program.writeToConsole("Time to eval an offspring: " + secs.ToString() + " senconds.");
                // screenshot
                this.setCurrentModel(m, -1);
                Program.GetFormMain().updateStats();
                res.Add(m);
                // save at diff folder
                string splitFolder = imageFolder;
                if (this._isPreRun && m._GRAPH._functionalityValues != null)
                {
                    int novelty = this.getNoveltyValue(m);
                    switch (novelty)
                    {
                        case 1:
                            splitFolder = imageFolder + "\\multi_high\\";
                            break;
                        case 2:
                            splitFolder = imageFolder + "\\high_prob\\";
                            break;
                        case 3:
                            splitFolder = imageFolder + "\\medium_prob\\";
                            break;
                        case 4:
                            splitFolder = imageFolder + "\\low_prob\\";
                            break;
                        default:
                            break;
                    }
                }
                if (!Directory.Exists(splitFolder))
                {
                    Directory.CreateDirectory(splitFolder);
                }
                this.captureScreen(splitFolder + m._model_name + ".png");
                //this.captureScreen(imageFolder + m._model_name + ".png");
                saveAPartBasedModel(m, m._path + m._model_name + ".pam", false);
                m._index = _modelIndex;
                ++_modelIndex;
                double val = 0;
                if (triplet != null)
                {
                    val = triplet.value;
                }
                if (id == 0)
                {
                    m._partGroupPair = new PartGroupPair(p1, p2, val);
                    m._GRAPH._partGroups.Add(pg1);
                }
                else
                {
                    m._partGroupPair = new PartGroupPair(p2, p1, val); // --> p2, p1
                    m._GRAPH._partGroups.Add(pg2);
                }
                
            }
            secs = stopWatch.ElapsedMilliseconds / 1000;
            Program.writeToConsole("Time to run a crossover: " + secs.ToString() + " senconds.");
            avgTimePerValidOffspring += secs;
            validOffspringNumber += res.Count;
            longestTimePerValidOffspring = longestTimePerValidOffspring > secs ? longestTimePerValidOffspring : secs;

            //if (res.Count == 2)
            //{
            //    // select the BEST of the two
            //    double[] scores = new double[res.Count];
            //    int maxId = -1;
            //    double maxScore = 1;
            //    for (int i = 0; i < res.Count; ++i)
            //    {
            //        for (int j = 0; j < res[i]._GRAPH._functionalityValues._parentCategories.Count; ++j)
            //        {
            //            int catId = (int)res[i]._GRAPH._functionalityValues._parentCategories[j];
            //            scores[i] *= res[i]._GRAPH._functionalityValues._classProbs[catId];
            //        }
            //        if (scores[i] > maxScore)
            //        {
            //            maxScore = scores[i];
            //            maxId = i;
            //        }
            //    }
            //    //Model removed = res[maxId];
            //    //// screenshot
            //    //this.setCurrentModel(removed, -1);
            //    //Program.GetFormMain().updateStats();
            //    //this.captureScreen(imageFolder + removed._model_name + "_bad_one.png");
            //}
            return true;
        }// runACrossover - part groups
  
        public void test()
        {
            if (_currModel!= null && _currModel._GRAPH!= null)
            {
                bool hasFuncPart = _currModel._GRAPH.hasAnyNonObstructedFunctionalPart(-1);
                //_currModel._GRAPH.isPhysicalValid();
                this.tryRestoreFunctionalNodes(_currModel);
            }
        }
        private void tryRestoreFunctionalNodes(Model m)
        {
            foreach(Node node in m._GRAPH._NODES)
            {
                if (node._funcs.Contains(Functionality.Functions.PLACEMENT))
                {
                    this.tryRestoreFunctionalNodeArea(m, node);
                }
                else if (node._funcs.Contains(Functionality.Functions.STORAGE))
                {
                    this.tryRestoreFunctionalNodeVolume(m, node);
                }
            }
        }// tryRestoreFunctionalNodes

        private void tryRestoreFunctionalNodeArea(Model m, Node node)
        {
        }

        private bool tryRestoreFunctionalNodeVolume(Model m, Node node)
        {
            //Random rand = new Random();
            //int randnum = rand.Next(1);
            //if (randnum == 0)
            //{
            //    return false;
            //}
            string node_name = node._PART._partName;
            if (!_functionalPartScales.ContainsKey(node_name))
            {
                return false;
            }
            Vector3d originalRatio;
            if (!_functionalPartScales.TryGetValue(node_name, out originalRatio))
            {
                return false;
            }
            node.calRatios();
            Vector3d scale = new Vector3d(1, 1, 1);
            double ry = node._ratios[1] / originalRatio[1];
            double rz = node._ratios[2] / originalRatio[2];
            double thr1 = 3.0;
            double thr2 = 1.0/thr1;
            bool needReScale = false;
            if (ry >= thr1 && rz >= thr1)
            {
                scale[0] *= Math.Min(ry, rz);
                needReScale = true;
            }
            if (ry <= thr2)
            {
                scale[1] = originalRatio[1] / node._ratios[1];
                needReScale = true;
            }
            if (rz <= thr2)
            {
                scale[2] = originalRatio[2] / node._ratios[2];
                needReScale = true;
            }
            if (needReScale)
            {
                Vector3d center = node._PART._BOUNDINGBOX.CENTER;
                List<Node> nodes = new List<Node>();
                nodes.Add(node);
                // involved nodes
                Matrix4d T = Matrix4d.TranslationMatrix(center) * Matrix4d.ScalingMatrix(scale) 
                    * Matrix4d.TranslationMatrix(new Vector3d() - center);
                this.deformNodesAndEdges(nodes, T);
                
                Program.GetFormMain().outputSystemStatus("Re-scale a functional part of model: " + m._model_name);
            }
            return needReScale;
        }// tryRestoreAFunctionalNode

        private bool isAGoodSelectionOfPartGroupPair(Triplet triplet, bool highProb)
        {
            if(triplet == null)
            {
                return false;
            }
            int i = triplet.row;
            int j = triplet.col;
            // update global dictionary
            List<int> pairs;
            if (_pairGroupMemory.TryGetValue(i, out pairs))
            {
                if (!pairs.Contains(j))
                {
                    _pairGroupMemory[i].Add(j);
                }
            }
            else
            {
                pairs = new List<int>();
                pairs.Add(j);
                _pairGroupMemory.Add(i, pairs);
            }
            // update current dictionary
            if (_curGenPGmemory.TryGetValue(i, out pairs))
            {
                if (!pairs.Contains(j))
                {
                    _curGenPGmemory[i].Add(j);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                pairs = new List<int>();
                pairs.Add(j);
                _curGenPGmemory.Add(i, pairs);
            }

            double val = _validityMatrixPG.GetTriplet(i, j).value;
            if (highProb && val == 0)
            {
                // user dislike
                return false;
            }
            //if ((highProb && val < _highProbabilityThresh) || (!highProb && val > _highProbabilityThresh))
            //{
            //    return false;
            //}
            if (_partGroups[i][0]._NODES.Count == 0 || _partGroups[j][0]._NODES.Count == 0)
            {
                if (_numOfEmptyGroupUsed >= _maxUseEmptyGroup)
                {
                    return false;
                }
                ++_numOfEmptyGroupUsed;
            }
            return true;
        }// isAGoodSelectionOfPartGroupPair

        private void setSelectedNodes(Model m, PartGroup pg)
        {
            m._GRAPH.selectedNodes = new List<Node>();
            foreach (Node node in pg._NODES)
            {
                if (!m._GRAPH._NODES.Contains(node))
                {
                    MessageBox.Show("Something goes wrong with the part group: " + m._model_name);
                    return;
                }
                m._GRAPH.selectedNodes.Add(node);
            }
        }// setSelectedNodes

        private void setSelectedNodes(Model m, List<Node> nodes)
        {
            m._GRAPH.selectedNodes.Clear();
            foreach (Node node in nodes)
            {
                if (!m._GRAPH._NODES.Contains(node))
                {
                    MessageBox.Show("Something goes wrong with the part group: " + m._model_name);
                    return;
                }
                m._GRAPH.selectedNodes.Add(node);
            }
        }// setSelectedNodes

        private bool runFunctionalityTestWithPatchCombination(Model model, int gen)
        {
            //runFunctionalityTest(model);

            if (funcFolder == null)
            {
                funcFolder = System.IO.Path.GetFullPath(@"..\..\data_sets\patch_data\models\funcTest\" + model._model_name + "\\");
            } 

            //List<string> patchFileNames = this.useSelectedSubsetPatchesForPrediction(model);
            // combination of patches
            //List<SamplePoints> patches = new List<SamplePoints>();
            string weightFolder = Interface.WEIGHT_PATH + model._model_name;
            List<int> possibleN = new List<int>();
            List<List<int>> patchSPindices = new List<List<int>>();
            Dictionary<int, List<Functionality.Category>> maps = new Dictionary<int, List<Functionality.Category>>();
            foreach (Functionality.Category cat in model._GRAPH._functionalityValues._cats)
            {
                //List<SamplePoints> sps = this.getPatchesFromCategory(cat, model, weightFolder);
                //patches.AddRange(sps);
                List<List<int>> sps = this.getPatchesFromCategory(cat, model, weightFolder);
                patchSPindices.AddRange(sps);
                int pn = Functionality.getNumberOfFunctionalPatchesPerCategory(cat);
                if (!possibleN.Contains(pn))
                {
                    possibleN.Add(pn);
                    List<Functionality.Category> catIdx = new List<Functionality.Category>();
                    catIdx.Add(cat);
                    maps.Add(pn, catIdx);
                }else{
                    maps[pn].Add(cat);
                }
            }

            int totalPatches = patchSPindices.Count;
            int comId = 0;
            int nCats = Enum.GetNames(typeof(Functionality.Category)).Length;
            double[] highestScores = new double[nCats];
            for (int i = 0; i < nCats; ++i)
            {
                highestScores[i] = 0;
            }
            double max_score_all = 0;
            Dictionary<Functionality.Category, SamplePoints> dict = new Dictionary<Functionality.Category, SamplePoints>();
            for (int i = 0; i < possibleN.Count; ++i)
            {
                List<List<int>> com = new List<List<int>>();
                List<int> curCats = new List<int>();
                if (possibleN[i] == 1)
                {
                    for (int j = 0; j < totalPatches; ++j)
                    {
                        List<int> single = new List<int>();
                        single.Add(j + 1);
                        com.Add(single);
                    }
                }
                else if (possibleN[i] == 2)
                {
                    com = combine(totalPatches, 2);
                }
                else if (possibleN[i] == 3)
                {
                    com = combine(totalPatches, 3);
                }
                // this is for checking different categories with the same number of patches, 
                // so we can check them in the same time
                int nCatCheck = maps[possibleN[i]].Count;
                SamplePoints[] bestSP = new SamplePoints[nCatCheck];
                for (int c = 0; c < com.Count; ++c)
                {
                    List<List<int>> curr = new List<List<int>>();
                    for (int j = 0; j < com[c].Count; ++j)
                    {
                        curr.Add(patchSPindices[com[c][j] - 1]);
                    }
                    Model model_com = model.Clone() as Model;
                    model_com._path = funcFolder + "gen_" + gen.ToString() + "\\";
                    model_com._model_name += "_com_" + comId.ToString();
                    SamplePoints sp = this.mergePatchesSP(curr, model);
                    model_com._SP = sp;
                    double[] scores = runFunctionalityTest(model_com);
                    this.saveAPartBasedModel(model_com, model_com._path + model_com._model_name + ".pam", false);
                    ++comId;
                    if (scores == null)
                    {
                        continue;
                    }
                    for (int j = 0; j < nCatCheck; ++j)
                    {
                        int catId = (int)maps[possibleN[i]][j];
                        if (scores[catId] > highestScores[catId])
                        {
                            highestScores[catId] = scores[catId];
                            dict[maps[possibleN[i]][j]] = sp;
                            if (scores[catId] > max_score_all)
                            {
                                max_score_all = scores[catId];
                            }
                        }
                    }
                }// each combination
            }
            foreach (Functionality.Category cat in model._GRAPH._functionalityValues._cats)
            {
                Model best_patches = model.Clone() as Model;
                best_patches._path = funcFolder + "gen_" + gen.ToString() + "\\";
                best_patches._model_name += "_best_" + cat;
                best_patches._SP = dict[cat];
                this.saveAPartBasedModel(best_patches, best_patches._path + best_patches._model_name + ".pam", false);
            }
            return max_score_all > 0.9;
        }// runFunctionalityTestWithPatchCombination

        private void saveSamplePointsRequiredInfo(Model model)
        {
            string shape2poseDataFolder = model._path + "shape2pose\\" + model._model_name + "\\";
            if (!Directory.Exists(shape2poseDataFolder))
            {
                Directory.CreateDirectory(shape2poseDataFolder);
            }

            string offname = shape2poseDataFolder + model._model_name + ".off";
            string meshName = shape2poseDataFolder + model._model_name + ".mesh";
            string ptsname = shape2poseDataFolder + model._model_name + ".pts";

            this.saveModelOff(model, offname);
            this.saveModelMesh_StyleSimilarityUse(model, meshName);

            if (this.needReSample || model._SP == null || model._SP._points == null || model._SP._points.Length == 0)
            {
                this.reSamplingForANewShape(model);
            }
            model._SP.updateNormals(model._MESH);
            this.saveModelSamplePoints(model, ptsname);
            //if (!File.Exists(ptsname))
            //{
            //    this.saveModelSamplePoints(model, ptsname);
            //}
        }// saveSamplePointsRequiredInfo

        private double[] runFunctionalityTest(Model model)
        {
            string shape2poseDataFolder = model._path + "shape2pose\\" + model._model_name + "\\";
            model.recomputeSamplePointNormals();
            this.saveSamplePointsRequiredInfo(model);

            bool isSuccess = this.computeShape2PoseAndIconFeatures(model);

            if (!isSuccess)
            {
                return null;
            }

            this.writeModelSampleFeatureFilesForPrediction(model);

            Object matlabOutput = null;
            this.matlab.Feval("clearData", 0, out matlabOutput);
            // MATLAB array
            //matlab.Execute("a = [1 2 3 4 5 6 7 8]");
            matlabOutput = null;
            this.matlab.Feval("getSingleModelFunctionalityScore", 1, out matlabOutput, model._model_name);
            Object[] res = matlabOutput as Object[];
            double[,] results = res[0] as double[,];
            // save the scores
            string scoreFileName = shape2poseDataFolder + model._model_name + ".score";
            double[] scores = new double[results.GetLength(1)];
            for (int i = 0; i < results.GetLength(1); ++i)
            {
                scores[i] = results[0, i];
            }
            this.saveScoreFile(scoreFileName, scores);

            return scores;
        }// runFunctionalityTest

        private SamplePoints mergePatchesSP(List<List<int>> patches, Model model)
        {
            int npoints = model._SP._points.Length;
            bool[] added = new bool[npoints];
            for (int i = 0; i < patches.Count; ++i)
            {
                for (int j = 0; j < patches[i].Count; ++j)
                {
                    added[patches[i][j]] = true;
                }
            }
            List<Vector3d> points = new List<Vector3d>();
            List<Vector3d> normals = new List<Vector3d>();
            List<int> faceIdxs = new List<int>();
            for (int i = 0; i < npoints; ++i)
            {
                if (!added[i])
                {
                    continue;
                }
                points.Add(model._SP._points[i]);
                normals.Add(model._SP._normals[i]);
                faceIdxs.Add(model._SP._faceIdx[i]);
            }
            SamplePoints sp = new SamplePoints(points.ToArray(), normals.ToArray(), faceIdxs.ToArray(), null, model._MESH.FaceCount);
            return sp;
        }// mergeSamplePoints

        public List<List<int>> combine(int n, int k)
        {
            List<List<int>> result = new List<List<int>>();

            if (n <= 0 || n < k)
                return result;

            List<int> item = new List<int>();
            dfs(n, k, 1, item, result); // because it need to begin from 1
            return result;
        }

        private void dfs(int n, int k, int start, List<int> item, List<List<int>> res)
        {
            if (item.Count == k)
            {
                res.Add(new List<int>(item));
                return;
            }

            for (int i = start; i <= n; i++)
            {
                item.Add(i);
                dfs(n, k, i + 1, item, res);
                item.RemoveAt(item.Count - 1);
            }
        }

        private List<List<int>> getPatchesFromCategory(Functionality.Category cat, Model model, string weightFolder)
        {
            int nPatches = Functionality.getNumberOfFunctionalPatchesPerCategory(cat);
            double thr_ratio = 0.5;
            string wight_name_filter = model._model_name + "_predict_" + cat;
            string[] weightFiles = Directory.GetFiles(weightFolder, "*.csv");
            int fid = 0;
            List<string> patchWeightFiles = new List<string>();
            while (fid < weightFiles.Length)
            {
                string weight_name = Path.GetFileName(weightFiles[fid]);
                if (weight_name.StartsWith(wight_name_filter))
                {
                    // locate the weight files
                    while (weight_name.StartsWith(wight_name_filter))
                    {
                        patchWeightFiles.Add(weightFiles[fid++]);
                        if (fid >= weightFiles.Length)
                        {
                            break;
                        }
                        weight_name = Path.GetFileName(weightFiles[fid]);
                    }
                    break;
                }
                ++fid;
            }
            // load weights
            //List<SamplePoints> samplePoints = new List<SamplePoints>();
            List<List<int>> samplePointsIndices = new List<List<int>>();
            int patchId = 0;
            int nPoints = model._SP._points.Length;
            double[,] weights = new double[nPoints, nPatches];
            foreach (string wfile in patchWeightFiles)
            {
                double minw;
                double maxw;
                double[] curr_weights = loadPatchWeight(wfile, out minw, out maxw);
                double wdiff = maxw - minw;
                for (int i = 0; i < curr_weights.Length; ++i)
                {
                    double normalized = (curr_weights[i] - minw) / wdiff;
                    weights[i, patchId] = normalized;                    
                }
                ++patchId;
            }
            for (int i = 0; i < nPatches; ++i)
            {
                //List<Vector3d> points = new List<Vector3d>();
                //List<Vector3d> normals = new List<Vector3d>();
                //List<int> faceIdxs = new List<int>();
                //List<Color> colors = new List<Color>();
                List<int> indices = new List<int>();
                for (int j = 0; j < nPoints; ++j)
                {
                    if (weights[j, i] < thr_ratio)
                    {
                        continue;
                    }
                    bool isMax = true;
                    for (int k = 0; k < nPatches; ++k)
                    {
                        if (k != i && weights[j, k] > weights[j, i])
                        {
                            isMax = false;
                            break;
                        }
                    }
                    if (isMax)
                    {
                        indices.Add(j);
                    }
                    //points.Add(model._SP._points[i]);
                    //normals.Add(model._SP._normals[i]);
                    //faceIdxs.Add(model._SP._faceIdx[i]);
                    //Color color = GLDrawer.getColorGradient(weights[j, i], patchId);
                    //colors.Add(color);
                }
                //SamplePoints sp = new SamplePoints(points.ToArray(), normals.ToArray(), faceIdxs.ToArray(), colors.ToArray(), model._MESH.FaceCount);
                //samplePoints.Add(sp);
                samplePointsIndices.Add(indices);
            }
            return samplePointsIndices;
            //return samplePoints;
        }// getPatchesFromCategory

        private void writeFileNamesForPredictToMatlabFolder(List<string> strs)
        {
            string filename = Interface.MATLAB_PATH + "\\shapeFileNames.txt";
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < strs.Count; ++i)
                {
                    sw.WriteLine(strs[i]);
                }
            }
        }// writeFileNamesForPredictToMatlabFolder

        private void saveScoreFile(string filename, double[] scores)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                for (int i = 0; i < scores.Length; ++i)
                {
                    sw.WriteLine(Functionality.getCategoryName(i) + " " + scores[i].ToString());
                }
                string tops = Functionality.getTopPredictedCategories(scores);
                sw.WriteLine(tops);
                Program.writeToConsole(tops);
            }
        }// saveScoreFile

        private List<string> useSelectedSubsetPatchesForPrediction(Model model)
        {
            if (model == null)
            {
                return null;
            }
            List<string> patchFileNamesForPredictions = new List<string>();
            // select those patches with certain functioanlity for a category
            foreach (Functionality.Category cat in model._GRAPH._functionalityValues._cats)
            {
                List<Functionality.Functions> funcs = Functionality.getFunctionalityFromCategory(cat);
                List<Node> subPatches = new List<Node>();
                foreach (Node node in model._GRAPH._NODES)
                {
                    List<Functionality.Functions> funcs_node = node._funcs;
                    foreach (Functionality.Functions f in funcs_node)
                    {
                        if (funcs.Contains(f))
                        {
                            subPatches.Add(node);
                            break;
                        }
                    }
                }
                // save 
                string subsetStr = "_subPatches_" + cat;
                string patchFileName = model._model_name + subsetStr;
                patchFileNamesForPredictions.Add(patchFileName);
                //writeSampleFeatureFilesForPrediction(subPatches, model, subsetStr);

                // test the whole shape
                subPatches = model._GRAPH._NODES;
                subsetStr = "_wholeShape_" + cat;
                patchFileName = model._model_name + subsetStr;
                patchFileNamesForPredictions.Add(patchFileName);
                //writeSampleFeatureFilesForPrediction(subPatches, model, subsetStr);
            }
            return patchFileNamesForPredictions;
        }// useSelectedSubsetPatchesForPrediction

        private double[,] writeModelSampleFeatureFilesForPrediction(Model model)
        {
            string mesh_file = model._path + model._model_name + "\\" + model._model_name + ".obj";
            if (!File.Exists(mesh_file))
            {
                this.saveObj(model._MESH, mesh_file, GLDrawer.MeshColor);
            }

            string copyFolder = Interface.MESH_PATH + model._model_name + "\\";
            if (!Directory.Exists(copyFolder))
            {
                Directory.CreateDirectory(copyFolder);
            }
            File.Copy(mesh_file, copyFolder + model._model_name + ".obj", true);

            //string folder = Interface.MATLAB_INPUT_PATH + model._model_name + "\\";
            string folder = Interface.MATLAB_INPUT_PATH;
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string possionFilename = model._model_name + ".poisson";
            string pois_file = folder + possionFilename;
            using (StreamWriter sw = new StreamWriter(pois_file))
            {
                SamplePoints sp = model._SP;
                for (int j = 0; j < sp._points.Length; ++j)
                {
                    Vector3d vpos = sp._points[j];
                    sw.Write(vector3dToString(vpos, " ", " "));
                    Vector3d vnor = sp._normals[j];
                    sw.Write(vector3dToString(vnor, " ", " "));
                    int fidx = sp._faceIdx[j];
                    sw.WriteLine(fidx.ToString());
                }
            }
            copyFolder = Interface.POINT_SAMPLE_PATH + model._model_name + "\\";
            if (!Directory.Exists(copyFolder))
            {
                Directory.CreateDirectory(copyFolder);
            }
            File.Copy(pois_file, copyFolder + possionFilename, true);

            string featureFileName = model._model_name + "_point_feature.csv";
            string feat_file = folder + featureFileName;
            double[,] point_features = new double[model._SP._points.Length, Functionality._POINT_FEATURE_DIM];
            using (StreamWriter sw = new StreamWriter(feat_file))
            {
                int n = model._SP._points.Length;
                for (int i = 0; i < n; ++i)
                {
                    StringBuilder sb = new StringBuilder();
                    int d = Functionality._POINT_FEAT_DIM;//3
                    int dimId = 0;
                    for (int j = 0; j < d; ++j)
                    {
                        sb.Append(this.formatOutputStr(Common.correct(model._funcFeat._pointFeats[i * d + j])));
                        sb.Append(",");
                        point_features[i, dimId++] = model._funcFeat._pointFeats[i * d + j];
                    }
                    d = Functionality._CURV_FEAT_DIM; //4
                    for (int j = 0; j < d; ++j)
                    {
                        sb.Append(this.formatOutputStr(Common.correct(model._funcFeat._curvFeats[i * d + j])));
                        sb.Append(",");
                        point_features[i, dimId++] = model._funcFeat._curvFeats[i * d + j];
                    }
                    d = Functionality._PCA_FEAT_DIM;//5
                    for (int j = 0; j < d; ++j)
                    {
                        sb.Append(this.formatOutputStr(Common.correct(model._funcFeat._pcaFeats[i * d + j])));
                        sb.Append(",");
                        point_features[i, dimId++] = model._funcFeat._pcaFeats[i * d + j];
                    }
                    d = Functionality._RAY_FEAT_DIM;//2
                    for (int j = 0; j < d; ++j)
                    {
                        sb.Append(this.formatOutputStr(Common.correct(model._funcFeat._rayFeats[i * d + j])));
                        sb.Append(",");
                        point_features[i, dimId++] = model._funcFeat._rayFeats[i * d + j];
                    }
                    d = Functionality._CONVEXHULL_FEAT_DIM;//2
                    for (int j = 0; j < d; ++j)
                    {
                        sb.Append(this.formatOutputStr(Common.correct(model._funcFeat._conhullFeats[i * d + j])));
                        sb.Append(",");
                        point_features[i, dimId++] = model._funcFeat._conhullFeats[i * d + j];
                    }
                    for (int j = 0; j < d; ++j)
                    {
                        sb.Append(this.formatOutputStr(Common.correct(model._funcFeat._cenOfMassFeats[i * d + j])));
                        if (j < d - 1)
                        {
                            sb.Append(",");
                        }
                        point_features[i, dimId++] = model._funcFeat._cenOfMassFeats[i * d + j];
                    }
                    sw.WriteLine(sb.ToString());
                }
            }
            copyFolder = Interface.POINT_FEATURE_PATH + model._model_name + "\\";
            if (!Directory.Exists(copyFolder))
            {
                Directory.CreateDirectory(copyFolder);
            }
            File.Copy(feat_file, copyFolder + featureFileName, true);
            return point_features;
        }// writeModelSampleFeatureFilesForPrediction

        private void writeSamplePoints(Model model)
        {
            string folder = Interface.MATLAB_INPUT_PATH;
            string possionFilename = model._model_name + ".poisson";
            string pois_file = folder + possionFilename;
            using (StreamWriter sw = new StreamWriter(pois_file))
            {
                SamplePoints sp = model._SP;
                for (int j = 0; j < sp._points.Length; ++j)
                {
                    Vector3d vpos = sp._points[j];
                    sw.Write(vector3dToString(vpos, " ", " "));
                    Vector3d vnor = sp._normals[j];
                    sw.Write(vector3dToString(vnor, " ", " "));
                    int fidx = sp._faceIdx[j];
                    sw.WriteLine(fidx.ToString());
                }
            }// writeSamplePoints
        }

        private List<Model> runGrowth(List<Model> models, int gen, Random rand, string imageFolder, int start)
        {
            List<Model> growth = new List<Model>();
            if (models.Count < 2)
            {
                return growth;
            }
            string path = growthFolder + "gen_" + gen.ToString() + "\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            for (int i = start; i < models.Count; ++i)
            {
                Model m1 = models[i];
                // jump those have already been visited
                int j = start + 1;
                if (i > start)
                {
                    j = 0;
                }
                int idx = 0;
                int randomj = rand.Next(models.Count - j);
                int maxw = 5;
                int w = 0;
                while (randomj == i && w < maxw)
                {
                    randomj = rand.Next(models.Count - j);
                    ++w;
                }
                j = randomj;
                if (j == i)
                {
                    continue;
                }
                //for (; j < models.Count; ++j)
                //{
                    if (i == j)
                    {
                        continue;
                    }
                    Model m2 = models[j];
                    //Model model = addPlacement(m1, m2._GRAPH, path, idx, rand, gen);
                    Model model = growNewFunctionality(m1, m2, path, idx, rand);
                    if (model != null && model._GRAPH != null && model._GRAPH.isValid())
                    {
                        model.composeMesh();
                        model._GRAPH.reset();
                        model._GRAPH.recomputeSPnormals();
                        growth.Add(model);
                        // screenshot
                        this.setCurrentModel(model, -1);
                        Program.GetFormMain().updateStats();
                        this.captureScreen(imageFolder + model._model_name + ".png");
                        saveAPartBasedModel(model, model._path + model._model_name + ".pam", false);
                        ++idx;
                    }
                //}
            }
            return growth;
        }// runGrowth

        private Model growNewFunctionality(Model m1, Model m2, string path, int idx, Random rand)
        {
            // find the new func in m2 and add to m1
            List<Functionality.Functions> funcs1 = m1._GRAPH.getGraphFuncs();
            List<Functionality.Functions> funcs2 = m2._GRAPH.getGraphFuncs();
            List<Functionality.Functions> cands = new List<Functionality.Functions>();
            foreach (Functionality.Functions f2 in funcs2)
            {
                if (!funcs1.Contains(f2))
                {
                    cands.Add(f2);
                }
            }
            if (cands.Count == 0) {
                return null;
            }
            int i = rand.Next(cands.Count);
            Functionality.Functions addf = cands[i];
            Model m1_clone = m1.Clone() as Model;
            m1_clone._path = path;
            m1_clone._model_name = m1._model_name + "_grow_" + idx.ToString();
            Graph g1_clone = m1_clone._GRAPH;
            List<Node> nodes = m2._GRAPH.getNodesByUniqueFunctionality(addf);
            if (nodes.Count == 0)
            {
                return null;
            }
            List<Edge> outEdges = m2._GRAPH.getOutgoingEdges(nodes);
            List<Node> clone_nodes = new List<Node>();
            List<Edge> clone_edges = new List<Edge>();
            m2._GRAPH.cloneSubgraph(nodes, out clone_nodes, out clone_edges);
            foreach (Node node in clone_nodes)
            {
                m1_clone.addAPart(node._PART);
                g1_clone.addANode(node);
            }
            foreach (Edge e in clone_edges)
            {
                g1_clone.addAnEdge(e._start, e._end, e._contacts);
            }
            // node to attach
            Node attach = g1_clone.getNodeToAttach();
            if (attach == null)
            {
                return null;
            }
            Vector3d sourcePos = new Vector3d();
            int ne = 0;
            foreach (Edge e in outEdges)
            {
                List<Contact> clone_contacts = new List<Contact>();
                foreach (Contact c in e._contacts)
                {
                    sourcePos += c._pos3d;
                    ++ne;
                    clone_contacts.Add(new Contact(new Vector3d(c._pos3d)));
                }
                Node out_node =  nodes.Contains(e._start) ? e._start : e._end;
                Node cnode = clone_nodes[nodes.IndexOf(out_node)];
                g1_clone.addAnEdge(cnode, attach, clone_contacts);
            }
            sourcePos /= ne;
            Vector3d targetPos;
            if (addf == Functionality.Functions.PLACEMENT || addf == Functionality.Functions.SITTING)
            {
                targetPos = new Vector3d(
                attach._PART._BOUNDINGBOX.CENTER.x,
                attach._PART._BOUNDINGBOX.MaxCoord.y,
                attach._PART._BOUNDINGBOX.CENTER.z);
            }
            else if (addf == Functionality.Functions.SUPPORT)
            {
                targetPos = new Vector3d(
                attach._PART._BOUNDINGBOX.CENTER.x,
                attach._PART._BOUNDINGBOX.MinCoord.y,
                attach._PART._BOUNDINGBOX.CENTER.z);
            }
            else
            {
                targetPos = new Vector3d(
                attach._PART._BOUNDINGBOX.CENTER.x,
                attach._PART._BOUNDINGBOX.MaxCoord.y,
                attach._PART._BOUNDINGBOX.MinCoord.z);
            }

            Matrix4d T = Matrix4d.TranslationMatrix(targetPos - sourcePos);
            foreach (Node cnode in clone_nodes)
            {
                deformANodeAndEdges(cnode, T);
            }
            return m1_clone;
        }// growNewFunctionality

        private Model addPlacement(Model m1, Graph g2, string path, int idx, Random rand, int gen)
        {
            // get sth from g2 that can be added to g1
            Graph g1 = m1._GRAPH;
            Node place_g1 = null;
            foreach (Node node in g1._NODES)
            {
                if (node._funcs.Contains(Functionality.Functions.PLACEMENT))
                {
                    place_g1 = node;
                    break;
                }
            }
            if (place_g1 == null)
            {
                return null;
            }
            int option = rand.Next(2);
            if (m1._model_name.Contains("table"))
            {
                option = 0;
            }
            if (gen > 1)
            {
                option = 1;
            }
            Node nodeToAdd = getAddNode(m1._GRAPH, g2, option);
            if (nodeToAdd == null)
            {
                return null;
            }
            // along X-axis
            double hx = (place_g1._PART._BOUNDINGBOX.MaxCoord.x - place_g1._PART._BOUNDINGBOX.MinCoord.x) / 2;
            double xscale = hx / (nodeToAdd._PART._BOUNDINGBOX.MaxCoord.x - nodeToAdd._PART._BOUNDINGBOX.MinCoord.x);
            double y = (nodeToAdd._PART._BOUNDINGBOX.MaxCoord.y - nodeToAdd._PART._BOUNDINGBOX.MinCoord.y);
            double z = (nodeToAdd._PART._BOUNDINGBOX.MaxCoord.z - nodeToAdd._PART._BOUNDINGBOX.MinCoord.z);
            double yscale = 1.0;
            double zscale = (place_g1._PART._BOUNDINGBOX.MaxCoord.z - place_g1._PART._BOUNDINGBOX.MinCoord.z) / z;
            Vector3d center = new Vector3d(place_g1._PART._BOUNDINGBOX.MaxCoord.x - hx / 2,
                place_g1._PART._BOUNDINGBOX.MaxCoord.y + y / 2,
                place_g1._PART._BOUNDINGBOX.CENTER.z);

            Matrix4d S = Matrix4d.ScalingMatrix(xscale, yscale, zscale);
            if (option == 1)
            {
                center = new Vector3d(place_g1._PART._BOUNDINGBOX.CENTER.x,
                place_g1._PART._BOUNDINGBOX.MaxCoord.y + y / 2,
                place_g1._PART._BOUNDINGBOX.MinCoord.z + z / 2);
                S = Matrix4d.ScalingMatrix(xscale * 2, 1.0, 1.0);
            }
            Matrix4d Q = Matrix4d.TranslationMatrix(center) * S * Matrix4d.TranslationMatrix(new Vector3d() - nodeToAdd._PART._BOUNDINGBOX.CENTER);
            Node nodeToAdd_clone = nodeToAdd.Clone() as Node;
            nodeToAdd_clone._INDEX = m1._GRAPH._NNodes;
            int node_idx = m1._GRAPH._NODES.IndexOf(place_g1);
            Model m1_clone = m1.Clone() as Model;
            m1_clone._path = path;
            m1_clone._model_name = m1._model_name + "_grow_" + idx.ToString();
            Graph g1_clone = m1_clone._GRAPH;
            m1_clone.addAPart(nodeToAdd_clone._PART);
            g1_clone.addANode(nodeToAdd_clone);
            List<Contact> clone_contacts = new List<Contact>();
            Vector3d[] contact_points = new Vector3d[4];
            contact_points[0] = new Vector3d(nodeToAdd._PART._BOUNDINGBOX.MinCoord.x, nodeToAdd._PART._BOUNDINGBOX.MinCoord.y, nodeToAdd._PART._BOUNDINGBOX.MinCoord.z);
            contact_points[1] = new Vector3d(nodeToAdd._PART._BOUNDINGBOX.MaxCoord.x, nodeToAdd._PART._BOUNDINGBOX.MinCoord.y, nodeToAdd._PART._BOUNDINGBOX.MinCoord.z);
            contact_points[2] = new Vector3d(nodeToAdd._PART._BOUNDINGBOX.MaxCoord.x, nodeToAdd._PART._BOUNDINGBOX.MinCoord.y, nodeToAdd._PART._BOUNDINGBOX.MaxCoord.z);
            contact_points[3] = new Vector3d(nodeToAdd._PART._BOUNDINGBOX.MinCoord.x, nodeToAdd._PART._BOUNDINGBOX.MinCoord.y, nodeToAdd._PART._BOUNDINGBOX.MaxCoord.z);

            for (int i = 0; i < 4; ++i)
            {
                clone_contacts.Add(new Contact(contact_points[i]));
            }
            g1_clone.addAnEdge(g1_clone._NODES[node_idx], nodeToAdd_clone, clone_contacts);
            deformANodeAndEdges(nodeToAdd_clone, Q);
            return m1_clone;
        }// addPlacement

        private Node getAddNode(Graph g1, Graph g2, int option)
        {
            List<Contact> contacts = new List<Contact>();
            int maxSupport = 0;
            Node nodeToAdd = null;
            if (option == 1)
            {
                // add a new functionality
                List<Functionality.Functions> funcs1 = g1.getGraphFuncs();
                List<Functionality.Functions> funcs2 = g2.getGraphFuncs();
                foreach (Functionality.Functions f in funcs1)
                {
                    funcs2.Remove(f);
                }
                if (funcs2.Count > 0)
                {
                    Random rand = new Random();
                    int fidx = rand.Next(funcs2.Count);
                    Functionality.Functions func = funcs2[fidx];
                    foreach (Node node in g2._NODES)
                    {
                        if (node._funcs.Contains(func))
                        {
                            if (nodeToAdd != null)
                            {
                                nodeToAdd = null; // only support one node for now
                                break;
                            }
                            nodeToAdd = node;
                            //break;
                        }
                    }
                }
            }
            else
            {
                foreach (Node node in g2._NODES)
                {
                    int ns = 0;
                    List<Contact> cnts = new List<Contact>();
                    for (int i = 0; i < node._edges.Count; ++i)
                    {
                        Node adj = node._edges[i]._start == node ? node._edges[i]._end : node._edges[i]._start;
                        if (adj._funcs.Contains(Functionality.Functions.SUPPORT))
                        //&& adj._PART._BOUNDINGBOX.MaxCoord.y < node._PART._BOUNDINGBOX.MaxCoord.y)
                        {
                            ++ns;
                            cnts.AddRange(node._edges[i]._contacts);
                        }
                    }
                    if (ns > maxSupport)
                    {
                        maxSupport = ns;
                        nodeToAdd = node;
                        contacts = cnts;
                    }
                }
            }
            return nodeToAdd;
        }// getAddNode

        private List<Model> runMutate(List<Model> models, int gen, string imageFolder, int start)
        {
            List<Model> mutated = new List<Model>();
            if (models.Count == 0)
            {
                return mutated;
            }
            Random rand = new Random();
            string path = mutateFolder + "gen_" + gen.ToString() + "\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            for (int i = start; i < models.Count; ++i)
            {
                // select a node
                Model iModel = models[i];
                int j = rand.Next(iModel._GRAPH._NNodes);
                Model model = iModel.Clone() as Model;
                model._path = path.Clone() as string;
                model._model_name = iModel._model_name + "_g_" + gen.ToString();
                //model._model_name = "gen_" + gen.ToString() + "_" + i.ToString();
                Node updateNode = model._GRAPH._NODES[j];
                if (gen > 1)
                {
                    updateNode = model._GRAPH._NODES[j];
                }
                // mutate
                if (hasInValidContact(model._GRAPH))
                {
                    break;
                }
                mutateANode(updateNode, rand);
                deformPropagation(model._GRAPH, updateNode);
                model._GRAPH.resetUpdateStatus();
                model._GRAPH._functionalityValues = iModel._GRAPH._functionalityValues.clone() as FunctionalityFeatures;
                if (model._GRAPH.isValid())
                {
                    model.unify();
                    model.composeMesh();
                    mutated.Add(model);
                    // screenshot
                    this.setCurrentModel(model, -1);
                    Program.GetFormMain().updateStats();
                    this.captureScreen(imageFolder + model._model_name + ".png");
                    saveAPartBasedModel(model, model._path + model._model_name + ".pam", false);
                }
            }
            return mutated;
        }// runMutate

        private void mutateANode(Node node, Random rand)
        {
            double s1 = 1.0;// 0.5; // min
            double s2 = 2.0; // max
            double scale = s1 + rand.NextDouble() * (s2 - s1);
            Vector3d scale_vec = new Vector3d(1, 1, 1);
            Matrix4d R = Matrix4d.IdentityMatrix();
            int axis = rand.Next(3);
            scale_vec[axis] = scale;
            Vector3d ori_axis = new Vector3d();
            ori_axis[axis]=1;
            if (node._PART._BOUNDINGBOX.type == Common.PrimType.Cylinder)
            {
                //Vector3d rot_axis = node._PART._BOUNDINGBOX.rot_axis;
                //R = Matrix4d.RotationMatrix(rot_axis, Math.Acos(ori_axis.Dot(rot_axis)));
                //scale_vec = scale * rot_axis;

            }
            Matrix4d S = Matrix4d.ScalingMatrix(scale_vec);
            Vector3d center = node._pos;
            Matrix4d Q = R * S;
            Q = Matrix4d.TranslationMatrix(center) * Q * Matrix4d.TranslationMatrix(new Vector3d() - center);
            if (node._isGroundTouching)
            {
                Node cNode = node.Clone() as Node;
                deformANodeAndEdges(cNode, Q);
                Vector3d trans = new Vector3d();
                trans.y = -cNode._PART._BOUNDINGBOX.MinCoord.y;
                Matrix4d T = Matrix4d.TranslationMatrix(trans);
                Q = T * Q;
            }
            deformANodeAndEdges(node, Q);
            deformSymmetryNode(node);
        }// mutateANode

        private string getPartGroupNames(PartGroup pg)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Node node in pg._NODES)
            {
                sb.Append(node._PART._partName);
                sb.Append(" ");
            }
            return sb.ToString();
        }// getPartGroupNames

        private Model selectAPartGroupAndParentModel(int p, Random rand)
        {
            int np = _partGroups[p].Count;
            int pgIdx = rand.Next(np);
            pgIdx = 0;
            int parentIdx = _partGroups[p][pgIdx]._ParentModelIndex;
            // do not use the models created at this generation
            while (_partGroups[p][pgIdx]._gen == _currGenId)
            {
                np = pgIdx;
                pgIdx = rand.Next(np);
                parentIdx = _partGroups[p][pgIdx]._ParentModelIndex;
            }
            Model model = null;
            _modelIndexMap.TryGetValue(parentIdx, out model);
            this.setSelectedNodes(model, _partGroups[p][pgIdx]);
            // info
            StringBuilder sb = new StringBuilder();
            sb.Append(model._model_name + " - part groups:\n");
            sb.Append(this.getPartGroupNames(_partGroups[p][pgIdx]));
            Program.writeToConsole(sb.ToString());

            return model;
        }// selectAPartGroupAndParentModel

        private List<Model> postAnalysis(List<Model> models)
        {
            List<int> sortIndx = this.rankOffspringByICONfeatures(models);
            _currGenModelViewers = new List<ModelViewer>();
            List<Model> sorted = new List<Model>();
            for (int i = 0; i < models.Count; ++i)
            {
                sorted.Add(models[sortIndx[i]]);
            }
            return sorted;
        }

        public List<ModelViewer> postAnalysis()
        {
            if (_currGenModelViewers.Count == 0)
            {
                return _currGenModelViewers;
            }
            List<Model> models = new List<Model>();
            foreach (ModelViewer mv in _currGenModelViewers)
            {
                models.Add(mv._MODEL);
            }
            List<int> sortIndx = this.rankOffspringByICONfeatures(models);
            List<ModelViewer> sorted = new List<ModelViewer>(_currGenModelViewers);
            _currGenModelViewers = new List<ModelViewer>();
            for (int i = 0; i < models.Count; ++i)
            {
                _currGenModelViewers.Add(sorted[sortIndx[i]]);
            }
            return _currGenModelViewers;
        }// postAnalysis

        int[,] _ranksByCategory;
        Dictionary<int, int> _currentModelIndexMap;
        public List<ModelViewer> rankByHighestCategoryValue(List<Model> models, int topN)
        {
            if (models == null || models.Count == 0)
            {
                models = new List<Model>();

                foreach (ModelViewer mv in _ancesterModelViewers)
                {
                    models.Add(mv._MODEL);
                }
                if (models.Count == 0)
                {
                    return _currGenModelViewers;
                }
                _currentModelIndexMap = new Dictionary<int, int>();
                for (int i = 0; i < models.Count; ++i)
                {
                    _currentModelIndexMap.Add(i, i);
                }
            }
            int n = models.Count;
            double[] topNHighest = new double[n];
            int[] indices = new int[n];
            //int[] catsIds = { 1, 3, 5, 6, 13 };
            int[] catsIds = new int[_inputSetCats.Count];
            for (int i = 0; i < _inputSetCats.Count; ++i)
            {
                catsIds[i] = _inputSetCats[i];
            }
            // 1. rank by category
            _ranksByCategory = new int[n, _inputSetCats.Count];
            for (int j = 0; j < _inputSetCats.Count; ++j)
            {
                double[] vals = new double[n];
                int[] ids = new int[n];
                int cid = _inputSetCats[j];
                for (int i = 0; i < n; ++i)
                {
                    ids[i] = i;
                    vals[i] = models[i]._GRAPH._functionalityValues._funScores[cid];
                }
                Array.Sort(vals, ids);
                for (int i = 0; i < n; ++i)
                {
                    _ranksByCategory[ids[i], j] = n - i;
                }
            }
            for (int i = 0; i < n; ++i)
            {
                indices[i] = i;
                List<double> vals = new List<double>(models[i]._GRAPH._functionalityValues._inClassProbs);
                List<double> selectedCatVals = new List<double>();
                for (int j = 0; j < catsIds.Length; ++j)
                {
                    selectedCatVals.Add(vals[catsIds[j]]);
                }
                selectedCatVals.Sort();
                for (int j = 1; j <= topN; ++j)
                {
                    topNHighest[i] += selectedCatVals[selectedCatVals.Count - j];
                }
            }
            Array.Sort(topNHighest, indices);
            List<ModelViewer> sorted = new List<ModelViewer>();
            //int nTopN = 15;
            //int mintopN = 1;
            for (int i = n - 1; i >= 0; --i)
            {
                //// if the model is not in the top-5 of any category, ignore it
                //int nTop = 0;
                //for (int j = 0; j < catsIds.Length; ++j)
                //{
                //    if (_ranksByCategory[indices[i], catsIds[j]] <= nTopN)
                //    {
                //        ++nTop;
                //    }
                //}
                //if (nTop < mintopN)
                //{
                //    continue;
                //}
                Model imodel = models[indices[i]];
                sorted.Add(new ModelViewer(imodel, imodel._index, this, _currGenId));
            }
            _currGenModelViewers = new List<ModelViewer>(sorted);
            return sorted;
        }// rankByHighestCategoryValue

        public void runProabilityTest()
        {
            //this.reloadView();
            predictFunctionalPatches();
            if (_currModel._GRAPH._functionalityValues == null || _currModel._GRAPH._functionalityValues._funScores[0] == 0)
            {
                return;
            }
            string imageFolder = System.IO.Path.GetFullPath(@"..\..\data_sets\patch_data\models\Users\User_1\screenCapture\test_split\");
            string splitFolder = imageFolder;
            if (_currModel._GRAPH._functionalityValues != null)
            {
                int nHighProb = 0;
                int nMedium = 0;
                int nParentProb = 0;
                for(int j = 0; j < _inputSetCats.Count; ++j)
                {
                    int i = _inputSetCats[j];
                    double prob = _currModel._GRAPH._functionalityValues._classProbs[i];
                    if (prob > 0.9)
                    {
                        ++nHighProb;
                    }
                    if (_currModel._GRAPH._functionalityValues._parentCategories.Contains((Functionality.Category)i))
                    {
                        if (prob > 0.9)
                        {
                            ++nParentProb;
                        } else if (prob > 0.5)
                        {
                            ++nMedium;
                        }
                    }
                }
                if (nParentProb > 0)
                {
                    if (nHighProb > 1)
                    {
                        splitFolder = imageFolder + "\\multi_high\\";
                    }
                    else
                    {
                        splitFolder = imageFolder + "\\high_prob\\";
                    }
                } else if (nMedium > 0)
                {
                    splitFolder = imageFolder + "\\medium_prob\\";
                } else
                {
                    splitFolder = imageFolder + "\\low_prob\\";
                }
            }
            if (!Directory.Exists(splitFolder))
            {
                Directory.CreateDirectory(splitFolder);
            }
            this.captureScreen(splitFolder + _currModel._model_name + ".png");
        }
        public void predictFunctionalPatches()
        {
            if (_currModel == null)
            {
                return;
            }
            if (_trainingFeaturesPerCategory == null)
            {
                this.loadTrainedInfo();
            }
            //foreach (Node node in _currModel._GRAPH._NODES)
            //{
            //    for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
            //    {
            //        node._PART._highlightColors[i] = Color.FromArgb(255, 255, 255, 0);
            //    }
            //}
            double[] scores;
            Model mc = null;
            this._isPreRun = true;
            if (_selectedNodes.Count > 0 || _currModel._GRAPH._functionalityValues == null || _currModel._GRAPH._functionalityValues._funScores[0] == 0)
            {
                //return;
                //_currModel._GRAPH._functionalityValues = new FunctionalityFeatures();
                _currModel.composeMesh();
                double[,] pointFeatures;
                mc = this.composeASubMatch(_currModel, out pointFeatures);
                scores = this.runFunctionalityTest(mc);
            }
            else
            {
                mc = _currModel;
                //mc.composeMesh();
                if (_currModel._GRAPH._functionalityValues != null && _currModel._GRAPH._functionalityValues._funScores[0] > 0)
                {
                    scores = _currModel._GRAPH._functionalityValues._funScores;
                }
                else
                {
                    scores = this.runFunctionalityTest(mc);
                }
            }
            if (mc._GRAPH._functionalityValues == null)
            {
                mc._GRAPH._functionalityValues = new FunctionalityFeatures();
            }
            mc._GRAPH._functionalityValues._validityVal = 0;
            for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
            {
                int cid = i;// _inputSetCats[i];
                double[] probs = this.getProbabilityForACat(i, scores[i]);
                mc._GRAPH._functionalityValues._funScores[cid] = scores[i];
                mc._GRAPH._functionalityValues._inClassProbs[cid] = probs[0];
                mc._GRAPH._functionalityValues._outClassProbs[cid] = probs[1];
                mc._GRAPH._functionalityValues._classProbs[cid] = probs[2];
                if (probs[2] > mc._GRAPH._functionalityValues._validityVal)
                {
                    mc._GRAPH._functionalityValues._validityVal = probs[2];
                }
            }
            // save the scores
            if (!_currModel._model_name.StartsWith("gen"))
            {
                string scoreFolder = Interface.MODLES_PATH + "fameScore\\";
                if (!Directory.Exists(scoreFolder))
                {
                    Directory.CreateDirectory(scoreFolder);
                }
                string scoreFileName = scoreFolder + _currModel._model_name + ".score";
                this.saveScoreFile(scoreFileName, _currModel._GRAPH._functionalityValues._funScores);
            }
            this.getNoveltyValue(_currModel);
            Program.GetFormMain().writePostAnalysisInfo(this.getFunctionalityValuesString(mc, false));
            string graphName = _currModel._path + _currModel._model_name + ".graph";
            this.saveAGraph(_currModel._GRAPH, graphName);
        }// predictFunctionalPatches

        public void partialMatchingForAnInputModel()
        {
            if (_currModel == null)
            {
                return;
            }
            if (_trainingFeaturesPerCategory == null)
            {
                this.loadTrainedInfo();
            }
            this._isPreRun = true;
            double[,] vals = this.partialMatching(_currModel, true);
            
            for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
            {
                int cid = i; // _inputSetCats[i];
                _currModel._GRAPH._functionalityValues._funScores[cid] = vals[i, 0];
                _currModel._GRAPH._functionalityValues._inClassProbs[cid] = vals[i, 1];
                _currModel._GRAPH._functionalityValues._outClassProbs[cid] = vals[i, 2];
                _currModel._GRAPH._functionalityValues._classProbs[cid] = vals[i, 3];
            }
            this.getNoveltyValue(_currModel);
            this.saveAGraph(_currModel._GRAPH, _currModel._path + _currModel._model_name + ".graph");
            Program.GetFormMain().writePostAnalysisInfo(this.getFunctionalityValuesString(_currModel, false));
        }// partialMatchingForAnInputModel

        private void calculateProbability(Model m)
        {
            if (m._GRAPH == null || m._GRAPH._functionalityValues == null)
            {
                return;
            }
            if (_trainingFeaturesPerCategory == null)
            {
                this.loadTrainedInfo();
            }
            Graph g = m._GRAPH;
            for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
            {
                int cid = i;
                double[] probs = this.getProbabilityForACat(cid, g._functionalityValues._funScores[cid]);
                g._functionalityValues._inClassProbs[cid] = probs[0];
                g._functionalityValues._outClassProbs[cid] = probs[1];
                g._functionalityValues._classProbs[cid] = probs[2];
            }
            g._functionalityValues._validityVal = 0;
            foreach (Functionality.Category cat in g._functionalityValues._parentCategories)
            {
                int cid = (int)cat;
                if (cid < Functionality._NUM_CATEGORIY && g._functionalityValues._funScores[cid] > g._functionalityValues._validityVal)
                {
                    g._functionalityValues._validityVal = g._functionalityValues._funScores[cid];
                }
            }
            this.getNoveltyValue(m);
        }// calculateProbability

        private List<int> rankOffspringByICONfeatures(List<Model> models)
        {
            int m = models.Count;
            int n = _inputSetCats.Count;
            double[,] distCats = new double[models.Count, n];
            for (int i = 0; i < m; ++i)
            {
                Model im = models[i];
                double[,] point_features = this.computePointFeatures(im);
                StringBuilder sb = new StringBuilder();
                _currFuncScores = null;
                for (int j = 0; j < n; ++j)
                {
                    List<PartGroup> patches;
                    bool[] useNodes = new bool[im._GRAPH._NNodes];
                    for (int t = 0;t< im._GRAPH._NNodes; ++t)
                    {
                        useNodes[t] = true;
                    }
                    double val = this.computeICONfeaturePerCategory(im, _inputSetCats[j], point_features, useNodes, out patches);
                    distCats[i, j] = val;

                    sb.Append(Functionality.getCategoryName(_inputSetCats[j]));
                    sb.Append(" ");
                    sb.Append(distCats[i, j].ToString());
                    sb.Append("\n");
                }
                Program.GetFormMain().writePostAnalysisInfo(sb.ToString());
            }
            // normalize -- cannot use it simply, as it should be normalized on all hybrids
            // for now, we  can only apply it to the current generation
            double[,] simCats = new double[m, n];
            List<double> sums = new List<double>();
            for (int i = 0; i < m; ++i)
            {
                sums.Add(0);
            }
            for (int j = 0; j < n; ++j)
            {
                double min = double.MaxValue;
                double max = double.MinValue;
                for (int i = 0; i < m; ++i)
                {
                    min = min < distCats[i, j] ? min : distCats[i, j];
                    max = max > distCats[i, j] ? max : distCats[i, j];
                }
                double diff = max - min;
                for (int i = 0; i < m; ++i)
                {
                    if (diff == 0)
                    {
                        simCats[i, j] = 0;
                    }
                    else
                    {
                        simCats[i, j] = 1 - (distCats[i, j] - min) / diff;
                    }
                    sums[i] += simCats[i, j];
                }
            }
            // repeat sum
            for (int i = 0; i < m - 1; ++i)
            {
                int iden = 1;
                for (int j = i + 1; j < m; ++j)
                {
                    if (sums[j] == sums[i])
                    {
                        sums[j] += Common._thresh * iden;
                        ++iden;
                    }
                }
            }
            // sort 
            Dictionary<double, int> rankDict = new Dictionary<double, int>();
            for (int i = 0; i < m; ++i)
            {
                rankDict.Add(sums[i], i);
            }
            sums.Sort((a, b) => b.CompareTo(a));
            List<int> sorted = new List<int>();
            for (int i = 0; i < m; ++i)
            {
                int cur = -1;
                rankDict.TryGetValue(sums[i], out cur);
                sorted.Add(cur);
            }
            return sorted;
        }// rankOffspringByICONfeatures

        private double[,] computePointFeatures(Model m)
        {
            int nSamplePoints = 0;
            foreach (Node node in m._GRAPH._NODES)
            {
                if (node._PART._partSP != null)
                {
                    nSamplePoints += node._PART._partSP._points.Length;
                }
            }
            if (m._SP == null || m._SP._points.Length != nSamplePoints)
            {
                m.composeMesh();
            }
            this.saveSamplePointsRequiredInfo(m);
            bool isSuccess = this.computeShape2PoseAndIconFeatures(m);
            //double[,] point_features = this.writeModelSampleFeatureFilesForPrediction(m);
            // n * 18 put all features together
            double[,] point_features = new double[nSamplePoints, Functionality._POINT_FEATURE_DIM];
            for (int i = 0; i < nSamplePoints; ++i)
            {
                int dimId = 0;
                int d = Functionality._POINT_FEAT_DIM;
                for (int j = 0; j < d; ++j)
                {
                    point_features[i, dimId++] = m._funcFeat._pointFeats[i * d + j];
                }
                d = Functionality._CURV_FEAT_DIM;
                for (int j = 0; j < d; ++j)
                {
                    point_features[i, dimId++] = m._funcFeat._curvFeats[i * d + j];
                }
                d = Functionality._PCA_FEAT_DIM;
                for (int j = 0; j < d; ++j)
                {
                    point_features[i, dimId++] = m._funcFeat._pcaFeats[i * d + j];
                }
                d = Functionality._RAY_FEAT_DIM;
                for (int j = 0; j < d; ++j)
                {
                    point_features[i, dimId++] = m._funcFeat._rayFeats[i * d + j];
                }
                d = Functionality._CONVEXHULL_FEAT_DIM;
                for (int j = 0; j < d; ++j)
                {
                    point_features[i, dimId++] = m._funcFeat._conhullFeats[i * d + j];
                }
                for (int j = 0; j < d; ++j)
                {
                    point_features[i, dimId++] = m._funcFeat._cenOfMassFeats[i * d + j];
                }
            }
            return point_features;
        }

        private double[] getProbabilityForACat(int catId, double score)
        {
            double[] probs = new double[3];
            // use probability
            double cdf1 = bd_inClass[catId].DistributionFunction(score);
            double cdf2 = bd_outClass[catId].DistributionFunction(score);
            probs[0] = cdf1;
            probs[1] = cdf2;
            probs[2] = probs[0] * probs[1];
            for (int i = 0; i < 3; ++i)
            {
                probs[i] = this.correctZeroProb(probs[i]);
            }
            return probs;
        }// getProbabilityForACat

        private int getNoveltyValue(Model m)
        {
            // 1: multi_high
            // 2: high
            // 3. medium
            // 4. low
            if (m._GRAPH._functionalityValues == null)
            {
                return 0;
            }
            int nHighProb = 0;
            int nMedium = 0;
            int nParentProb = 0;
            for (int j = 0; j < _inputSetCats.Count; ++j)
            {
                int cid = _inputSetCats[j];
                if (cid >= Functionality._NUM_CATEGORIY)
                {
                    continue;
                }
                double prob = m._GRAPH._functionalityValues._classProbs[cid];
                if (prob > 0.9)
                {
                    ++nHighProb;
                }
                if (m._GRAPH._functionalityValues._parentCategories.Contains((Functionality.Category)cid))
                {
                    if (prob > 0.9)
                    {
                        ++nParentProb;
                    }
                    else if (prob > 0.5)
                    {
                        ++nMedium;
                    }
                }
            }
            double novelty = 0;
            for (int j = 0; j < _inputSetCats.Count; ++j)
            {
                int cid = _inputSetCats[j];
                double prob = m._GRAPH._functionalityValues._classProbs[cid];
                novelty += prob * prob;
            }
            novelty /= _inputSetCats.Count;
            m._GRAPH._functionalityValues._noveltyVal = Functionality._NOVELTY_MINIMUM + Functionality._NOVELTY_MINIMUM / _inputSetCats.Count * nHighProb;
            if (nParentProb > 0)
            {
                if (nHighProb > 1)
                {
                    return 1;
                }
                else
                {
                    return 2;
                }
            }
            else if (nMedium > 0)
            {
                return 3;
            }
            else
            {
                return 4;
            }
        }// getNoveltyValue

        private double getNoveltyValue(double[] probs, List<int> parentCatIds)
        {
            int nHighProb = 0;
            int nMedium = 0;
            int nParentProb = 0;
            double novelVal = 0;
            int nhigh = 0;
            for (int j = 0; j < _inputSetCats.Count; ++j)
                //for (int j = 0; j < Functionality._NUM_CATEGORIY; ++j)
            {
                int cid =  _inputSetCats[j];
                double prob = probs[cid];
                if (prob > 0.8)
                {
                    nhigh++;
                    novelVal += prob;
                }
                if (prob > 0.9)
                {
                    ++nHighProb;
                }
                if (parentCatIds.Contains(cid))
                {
                    if (prob > 0.9)
                    {
                        ++nParentProb;
                    }
                    else if (prob > 0.5)
                    {
                        ++nMedium;
                    }
                }
            }
            double novelty = Functionality._NOVELTY_MINIMUM + Functionality._NOVELTY_MINIMUM / _inputSetCats.Count * nhigh;

            //novelty = 0;
            //for (int j = 0; j < _inputSetCats.Count; ++j)
            //{
            //    int cid = _inputSetCats[j];
            //    double prob = probs[cid];
            //    //novelty += prob * prob;
            //    novelty += prob;
            //}
            //novelty /= _inputSetCats.Count;
            //if (nHighProb == 0)
            //{
            //    novelVal = 0;
            //}
            //else
            //{
            //    novelVal /= nhigh;
            //    Console.WriteLine("# high prob > 0.8: " + nhigh.ToString());
            //}
            //novelty = novelVal;
            return novelty;
        }// getNoveltyValue
        private double fromCDFtoProbability(BetaDistribution bd, double cdf)
        {
            double mag = 100;
            double upper = Math.Ceiling(cdf * mag);
            double lower = Math.Floor(cdf * mag);
            upper /= mag;
            lower /= mag;
            double cdf_u = bd.DistributionFunction(upper);
            double cdf_l = bd.DistributionFunction(lower);
            return cdf_u - cdf_l;
        }// fromCDFtoProbability

        private Model composeASubMatch(Model m, out double[,] pointFeatures)
        {
            // once compute the point features, it will be used for all categories
            pointFeatures = null;
            Graph g = m._GRAPH;
            int nNodes = m._GRAPH._NNodes;
            bool[] useNodes = new bool[nNodes];
            List<int> indices = new List<int>();
            useNodes = new bool[nNodes];
            for (int i = 0; i < nNodes; ++i)
            {
                useNodes[i] = true;
            }

            if (_selectedNodes.Count > 0)
            {
                for (int i = 0; i < nNodes; ++i)
                {
                    if (!_selectedNodes.Contains(m._GRAPH._NODES[i]))
                    {
                        indices.Add(i);
                        useNodes[i] = false;
                    }
                }
            } else
            {
                return m;
            }
            Model mc = m.Clone() as Model;
            // delete nodes
            mc._GRAPH.deleteNodes(indices);
            mc._model_name += "_test";
            mc.deleteParts(indices);
            mc.unify();
            mc.composeMesh();
            this.saveAPartBasedModel(mc, mc._path + mc._model_name + ".pam", true);
            //pointFeatures = this.computePointFeatures(mc);
            return mc;
        }// composeASubMatch

        private double[] seperatePartialMatching(Model m, int catIdx, double[,] pointsFeatures)
        {
            double[] res = new double[4]; // score + 3 prob
            List<PartGroup> patches;
            Graph g = m._GRAPH;
            int nNodes = g._NNodes;
            bool[] useNodes = new bool[nNodes];
            double[] probs;
            double score;
            // already selected nodes, see #composeASubMatch
            for (int i = 0; i < nNodes; ++i)
            {
                useNodes[i] = true;
            }
            score = this.computeICONfeaturePerCategory(m, catIdx, pointsFeatures, useNodes, out patches);
            probs = this.getProbabilityForACat(catIdx, score);
            res[0] = score;
            res[1] = probs[0];
            res[2] = probs[1];
            res[3] = probs[2];
            return res;
        }// seperatePartialMatching

        int pcid = 0;
        List<PartCombination> visitedCombinations = new List<PartCombination>();
        string toPrint = "";
        private double[,] partialMatching(Model m, bool doPartialMatching)
        {
            double[,] res = new double[Functionality._NUM_CATEGORIY, 4]; // score + 3 prob
            //bool[] useNodes;
            List<int> allIndices = new List<int>();
            for (int i = 0; i < m._GRAPH._NNodes; ++i)
            {
                allIndices.Add(i);
            }
            //List<int> indices = new List<int>();
            //List<List<int>> excludedNodeIndices = new List<List<int>>();
            //excludedNodeIndices.Add(indices);
            // all
            //useNodes = new bool[nNodes];
            //for (int i = 0; i < nNodes; ++i)
            //{
            //    useNodes[i] = true;
            //}
            //pointsFeatures = this.computePointFeatures(m);
            double[] scores = this.runFunctionalityTest(m);
            double[] probs;
            for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
            {
                probs = this.getProbabilityForACat(i, scores[i]);
                res[i, 0] = scores[i];
                res[i, 1] = probs[0];
                res[i, 2] = probs[1];
                res[i, 3] = probs[2];
            }
            if (!doPartialMatching)
            {
                return res;
            }
            //var subsets = SubsetExtensions.SubSets(allIndices, 2);
            pcid = 0;
            visitedCombinations = new List<PartCombination>() { };
            // considering parent categories
            foreach (Functionality.Category c in m._GRAPH._functionalityValues._parentCategories)
            {
                if (c == Functionality.Category.None)
                {
                    continue;
                }
                int cid = (int)c;
                if (res[cid, 1] >= 0.9)
                {
                    continue;
                }
                //foreach (var subset in subsets)
                //{
                //    PartCombination startCombination = new PartCombination(subset.ToList());
                //    res = beamSearch(2, startCombination, cid, m, res);
                //}
                PartCombination startCombination = new PartCombination(allIndices);
                res = beamSearch(2, startCombination, cid, m, res);
            }// per cat
            string save_path = m._path + "\\" + m._model_name + "_probs[0].txt";
            System.IO.File.WriteAllText(save_path, toPrint);
            //foreach (var subset in allIndices.PowerSets())
            //{
            //    if (subset.ToList().Count == m._PARTS.Count || subset.ToList().Count == 0)
            //    {
            //        continue;
            //    }
            //    Model mc = m.Clone() as Model;
            //    mc._GRAPH.deleteNodes(subset.ToList());
            //    if (!mc._GRAPH.isValid())
            //    {
            //        continue;
            //    }
            //    mc._model_name += "_" + id.ToString();
            //    mc.deleteParts(subset.ToList());
            //    mc.unify();
            //    mc.composeMesh();
            //    this.saveAPartBasedModel(mc, mc._path + mc._model_name + ".pam", true);
            //    scores = this.runFunctionalityTest(mc);
            //    // considering parent categories
            //    foreach (Functionality.Category c in m._GRAPH._functionalityValues._parentCategories)
            //    {
            //        if (c == Functionality.Category.None)
            //        {
            //            continue;
            //        }
            //        int cid = (int)c;
            //        if (res[cid, 1] >= 0.9)
            //        {
            //            continue;
            //        }
            //        probs = this.getProbabilityForACat(cid, scores[cid]);
            //        if (probs[0] > res[cid, 1])
            //        {
            //            res[cid, 0] = scores[cid];
            //            res[cid, 1] = probs[0];
            //            res[cid, 2] = probs[1];
            //            res[cid, 3] = probs[2];
            //        }
            //    }// per cat
            //    id++;
            //}
            // considering parent categories
            //foreach (Functionality.Category c in m._GRAPH._functionalityValues._parentCategories)
            //{
            //    if (c == Functionality.Category.None)
            //    {
            //        continue;
            //    }
            //    int cid = (int)c;
            //    if (res[cid, 1] >= 0.9)
            //    {
            //        continue;
            //    }
            //    for (int i = 0; i < nNodes; ++i)
            //    {
            //        if (g._NODES[i]._PART._orignCategory == c)
            //        {
            //            useNodes[i] = true;
            //        } else
            //        {
            //            indices.Add(i);
            //        }
            //    }
            //    if (indices.Count == m._PARTS.Count || indices.Count == 0)
            //    {
            //        continue;
            //    }
            //    Model mc = m.Clone() as Model;
            //    // delete nodes
            //    mc._GRAPH.deleteNodes(indices);
            //    mc._model_name += "_" + id.ToString();
            //    mc.deleteParts(indices);
            //    mc.unify();
            //    mc.composeMesh();
            //    this.saveAPartBasedModel(mc, mc._path + mc._model_name + ".pam", true);
            //    // check functional space
            //    // but not easy to check, since some other parts may serve the functionality
            //    // e.g., chair seat is replaced by shelves, we don't know which fs to evaluate
            //    bool hasFunctionalSpace = mc._GRAPH.hasAnyNonObstructedFunctionalPart(-1);
            //    if (!hasFunctionalSpace)
            //    {
            //        res[cid, 0] = 0;
            //        res[cid, 1] = 0;
            //        res[cid, 2] = 0;
            //        res[cid, 3] = 0;
            //        continue;
            //    }
            //    scores = this.runFunctionalityTest(mc);
            //    probs = this.getProbabilityForACat(cid, scores[cid]);
            //    if (probs[0] > res[cid, 1])
            //    {
            //        res[cid, 0] = scores[cid];
            //        res[cid, 1] = probs[0];
            //        res[cid, 2] = probs[1];
            //        res[cid, 3] = probs[2];
            //    }
            //}// per cat
            return res;
        }// partialMatching

        private double[,] beamSearch(int beamSize, PartCombination startCombination, int cid, Model m, double[,] res)
        {
            double[] scores;
            double[] probs;
            List<int> allIndices = new List<int>();
            for (int i = 0; i < m._GRAPH._NNodes; ++i)
            {
                allIndices.Add(i);
            }
            List<PartCombination> beam = new List<PartCombination>();
            List<BigInteger> hash = new List<BigInteger>();
            beam.Add(startCombination);
            hash.Add(startCombination._HASH);
            while (beam.Count > 0)
            {
                List<PartCombination> set = new List<PartCombination>();
                foreach (PartCombination combination in beam)
                {
                    foreach (int j in combination._NODES)
                    {
                        PartCombination successorCombination = combination.Clone() as PartCombination;
                        successorCombination.Remove(j);
                        if (hash.Contains(successorCombination._HASH))
                        {
                            continue;
                        }
                        if (visitedCombinations.Select(x => x._HASH).ToList().Contains(successorCombination._HASH))
                        {
                            successorCombination = visitedCombinations.Find(x => x._HASH == successorCombination._HASH);
                            scores = successorCombination._SCORES;
                        }
                        else
                        {
                            Model mc = m.Clone() as Model;
                            mc._GRAPH.deleteNodes(allIndices.Except(successorCombination._NODES).ToList());
                            if (!mc._GRAPH.isValid())
                            {
                                continue;
                            }
                            mc._model_name += "_" + pcid.ToString();
                            mc.deleteParts(allIndices.Except(successorCombination._NODES).ToList());
                            mc.unify();
                            mc.composeMesh();
                            this.saveAPartBasedModel(mc, mc._path + mc._model_name + ".pam", true);
                            scores = this.runFunctionalityTest(mc);
                            successorCombination._SCORES = scores;
                            successorCombination._path = mc._path;
                            successorCombination._model_name = mc._model_name;
                            visitedCombinations.Add(successorCombination);
                            pcid++;
                        }
                        probs = this.getProbabilityForACat(cid, scores[cid]);
                        set.Add(successorCombination);
                        hash.Add(successorCombination._HASH);
                        toPrint += pcid + "\n" + Functionality.getCategoryName(cid) + ": "
                                + res[cid, 0] + ", " + res[cid, 1] + ", " + res[cid, 2] + ", " + res[cid, 3] + "\n";
                        toPrint += "\n";
                        string scoreDir = successorCombination._path + "\\" + successorCombination._model_name + "\\" + successorCombination._model_name + "_score.txt";
                        string str = Functionality.getCategoryName(cid) + " " + probs[2];
                        System.IO.File.WriteAllText(scoreDir, str);
                        if (probs[0] > res[cid, 1])
                        {
                            res[cid, 0] = scores[cid];
                            res[cid, 1] = probs[0];
                            res[cid, 2] = probs[1];
                            res[cid, 3] = probs[2];
                        }
                        if (res[cid, 1] >= 0.9)
                        {
                            return res;
                        }
                    }
                }
                beam.Clear();
                while (set.Count > 0 && beam.Count < beamSize)
                {
                    set = set.OrderByDescending(x => x._SCORES[cid]).ToList();
                    beam.Add(set[0]);
                    set.RemoveAt(0);
                }
            }
            return res;
        }// beamSearch

        double[] _currFuncScores = null;
        private double computeICONfeaturePerCategory(Model m, int catIdx, double[,] pointsFeatures, bool[] useNodes, out List<PartGroup> patches)
        {
            patches = new List<PartGroup>();
            if (_currFuncScores == null)
            {
                _currFuncScores = this.runFunctionalityTest(m);
            }
            return _currFuncScores[catIdx];

            Graph g = m._GRAPH;
            if (g == null)
            {
                return 1;
            }
            Functionality.Category cat = (Functionality.Category)catIdx;
            int[] patchIdxs = Functionality.getCategoryPatchIndicesInFeatureVector(cat);
            int nPatches = patchIdxs.Length;
            TrainedFeaturePerCategory trainedFeaturePerCat = _trainingFeaturesPerCategory[catIdx];
            // 1. estimate the functional patches - already pre-processed
            //      1.1 for each node, check the number of points that have a high value for each functional patch
            //      1.2 classify it to a patch that has the most number of such salient points            
            for (int i = 0; i < nPatches; ++i)
            {
                patches.Add(new PartGroup(new List<Node>(), _currGenId));
            }
            int pointInModelIndex = 0;
            int nUsePoints = 0;
            // normalize weights
            double[] sumw = new double[nPatches];
            List<List<double>> weights = new List<List<double>>();
            for (int i = 0; i < nPatches; ++i)
            {
                weights.Add(new List<double>());
            }
            Vector3d maxPos = Vector3d.MinCoord;
            Vector3d minPos = Vector3d.MaxCoord;
            List<int> addedPointIndex = new List<int>();
            for (int n = 0; n < g._NNodes;++n)
            {
                Node node = g._NODES[n];
                if(node._PART._partSP == null)
                {
                    continue;
                }
                PatchWeightPerCategory pw = node._PART._partSP._weightsPerCat[catIdx];
                foreach (Vector3d v in node._PART._partSP._points)
                {
                    maxPos = Vector3d.Max(maxPos, v);
                    minPos = Vector3d.Min(minPos, v);
                }
                if (useNodes[n])
                {
                    for (int i = 0; i < node._PART._partSP._points.Length; ++i)
                    {
                        int inModelIndex = pointInModelIndex + i;
                        //if (!m._funcFeat._visibliePoint[inModelIndex])
                        //{
                        //    continue;
                        //}
                        for (int j = 0; j < nPatches; ++j)
                        {
                            weights[j].Add(pw._weights[i, j]);
                            sumw[j] += pw._weights[i, j];
                        }
                        addedPointIndex.Add(inModelIndex);
                        ++nUsePoints;
                    }
                }
                pointInModelIndex += node._PART._partSP._points.Length;
            }// add nodes / points for evaluation
            int nFs = pointsFeatures.GetLength(1);
            double[,] point_features = new double[nUsePoints, nFs];
            // weight matrix of the subset of the model w.r.t. the given category
            for (int i = 0; i < nUsePoints; ++i)
            {
                for (int j = 0; j < nPatches; ++j)
                {
                    weights[j][i] /= sumw[j];
                }
                for (int j = 0; j < nFs; ++j)
                {
                    point_features[i, j] = pointsFeatures[addedPointIndex[i], j];
                }
            }
            // evaulate subset of parts that can serve the functional patches
            // 2. compute unary and binary features
            double[] res = new double[17]; // 15 for unary and 2 for binary
            // 2.1. calculate unary feature
            // predefined feature dimensions
            int nUnaryFeatureDim = 15;
            int[] unaryFeatDims = new int[nUnaryFeatureDim];
            double[] featureBinWidth = new double[nUnaryFeatureDim];
            int nUnaryDim = 0;
            int defaultBinNum = 10;
            for (int i = 0; i < 12; ++i)
            {
                unaryFeatDims[i] = defaultBinNum;
                nUnaryDim += unaryFeatDims[i];
                featureBinWidth[i] = 0.1;
            }
            for (int i = 12; i < 15; ++i)
            {
                unaryFeatDims[i] = defaultBinNum * defaultBinNum;
                nUnaryDim += unaryFeatDims[i];
                featureBinWidth[i] = 0.1;
            }
            featureBinWidth[12] = 0.05;
            for (int pid = 0; pid < nPatches; ++pid)
            {
                int colIdx = 0;
                int offset = 0;
                double[] unaryFeatureBins = new double[nUnaryDim];
                for (int i = 0; i < unaryFeatDims.Length; ++i)
                {
                    if (unaryFeatDims[i] == defaultBinNum)
                    {
                        for (int j = 0; j < nUsePoints; ++j)
                        {
                            double idd = Math.Floor(point_features[j, colIdx] / featureBinWidth[i]);
                            int idx = (int)Common.cutoff(idd, 0, defaultBinNum - 1);
                            idx += offset;
                            unaryFeatureBins[idx] += weights[pid][j] * 1.0;
                        }
                        ++colIdx;
                    }
                    else
                    {
                        for (int j = 0; j < nUsePoints; ++j)
                        {
                            double pf1 = point_features[j, colIdx];
                            double pf2 = point_features[j, colIdx + 1];
                            double idd1 = Math.Floor(pf1 / featureBinWidth[i]);
                            double idd2 = Math.Floor(pf2 / featureBinWidth[i]);
                            int idx1 = (int)Common.cutoff(idd1, 0, defaultBinNum - 1);
                            int idx2 = (int)Common.cutoff(idd2, 0, defaultBinNum - 1);
                            int idx = idx1 + idx2 * defaultBinNum;
                            idx += offset;
                            unaryFeatureBins[idx] += weights[pid][j];
                        }
                        colIdx += 2;
                    }
                     offset += unaryFeatDims[i];
                }
                // calculate distance for all the 15 dims (bins)
                int start = 0;
                for (int j = 0; j < unaryFeatDims.Length; ++j)
                {
                    double dist = this.getDistanceToLearnedFeature(trainedFeaturePerCat._unaryF[pid],
                        start, start + unaryFeatDims[j], unaryFeatureBins);
                    res[j] += dist / nPatches;
                    start += unaryFeatDims[j];
                }
            }// each patch
            // 2.2. calculate binary feature between each two patches
            int[] dims = { 10, 100 };
            double angleStep1 = 0.5 / dims[0];
            int nBin = (int)Math.Sqrt(dims[1]);
            double distStep = (maxPos - minPos).Length() / nBin;
            double angleStep2 = 0.5 / nBin;

            int np = nPatches * nPatches;
            int pairIdx = 0;

            for (int p = 0; p < nPatches; ++p)
            {
                List<Vector3d> points1 = new List<Vector3d>();
                List<Vector3d> normals1 = new List<Vector3d>();
                List<double> weights1 = weights[p]; // w.r.t. patch p

                for (int q = p; q < nPatches; ++q)
                {
                    List<Vector3d> points2 = new List<Vector3d>();
                    List<Vector3d> normals2 = new List<Vector3d>();
                    List<double> weights2 = weights[q]; // w.r.t. patch q
                    // histogram
                    // * weight at each entry
                    double[] binaryFeatureBins = new double[Functionality._NUM_BINARY_FEATURE];
                    for (int i = 0; i < points1.Count; ++i)
                    {
                        for (int j = 0; j < points2.Count; ++j)
                        {
                            // 2.1 orientation
                            Vector3d vi = normals1[i].normalize();
                            Vector3d vj = normals2[j].normalize();
                            double cosv = Common.cutoff(vi.Dot(vj), 0, 1); // when a value is large than 1, e.g., 1.000001, Acos() gives NaN
                            double angle1 = Math.Acos(cosv) / Math.PI;
                            angle1 = Common.cutoff(angle1, 0, 1);
                            int binId1 = (int)Common.cutoff(angle1 / angleStep1, 0, dims[0] - 1);
                            binaryFeatureBins[binId1] += weights1[i] * weights2[j];
                            // 2.2
                            // point distance
                            double distBin = (points1[i] - points2[j]).Length() / distStep;
                            int dBinIdx = (int)Common.cutoff(distBin, 0, nBin - 1);
                            // line segment angle
                            Vector3d dir = (points1[i] - points2[j]).normalize();
                            double angle2 = Math.Acos(Common.cutoff(dir[1], 0, 1)); // .dot(new vector3d(0,1,0) == y - axis upright vector
                            angle2 /= Math.PI;
                            int aBinIdx = (int)Common.cutoff(angle2 / angleStep2, 1, nBin);
                            int binId2 = dBinIdx + (aBinIdx - 1) * nBin;
                            binId2 = (int)Common.cutoff(binId2, 0, dims[1] - 1);
                            binaryFeatureBins[binId2] += weights1[i] * weights2[j];
                        }
                    }
                    // 3. calculate the distance to the given categories
                    // not as filtering, maybe sort models by the functionality distance
                    double[,] trainedBinaryFeature = trainedFeaturePerCat._binaryF[pairIdx];
                    double d1 = this.getDistanceToLearnedFeature(trainedBinaryFeature, 0, dims[0], binaryFeatureBins);
                    double d2 = this.getDistanceToLearnedFeature(trainedBinaryFeature, dims[0], dims[1], binaryFeatureBins);
                    res[15] += d1;
                    res[16] += d2;
                    if (p != q)
                    {
                        res[15] += d1;
                        res[16] += d2;
                    }
                    ++pairIdx;
                }
            }// each two patch    
            res[15] /= np;
            res[16] /= np;
            // sum up
            double sum = 0;
            for (int t = 0; t < res.Length; ++t)
            {
                sum += trainedFeaturePerCat.weights[t] * res[t];
            }
            //sum /= res.Length;
            return 1- sum;
        }// computeICONfeaturePerCategory

        private double getDistanceToLearnedFeature(double[,] trainedFeatures, int s, int e, double[] feature)
        {
            int k = 5;
            double[] neighs = new double[k];
            for (int j = 0; j < k; ++j)
            {
                neighs[j] = double.MaxValue;
            }
            int nShapes = trainedFeatures.GetLength(1);
            double[] ds = new double[nShapes];
            for (int j = 0; j < nShapes; ++j) // number of train shapes
            {
                for (int i = s; i < e; ++i)
                {
                    double dist = (trainedFeatures[i, j] - feature[i]);
                    ds[j] += dist * dist;
                }
            }
            for (int j = 0; j < nShapes; ++j)
            {
                for (int t = 0; t < k; ++t)
                {
                    if (ds[j] < neighs[t])
                    {
                        for (int q = k - 1; q > t; --q)
                        {
                            neighs[q] = neighs[q - 1];
                        }
                        neighs[t] = ds[j];
                        break;
                    }
                }
            }// compare to find the min 5 neighs to calculate the average distance
            double avg = 0;
            for (int j = 0; j < k; ++j)
            {
                avg += neighs[j];
            }
            avg /= k;
            return avg;
        }// getDistanceToLearnedFeature

        private List<Model> runCrossover(List<Model> models, int gen, Random rand, string imageFolder, int start)
        {
            List<Model> crossed = new List<Model>();
            if (models.Count < 2)
            {
                return crossed;
            }
            int m_idx = 0;
            for (int i = 0; i < models.Count - 1; ++i)
            {
                Model m1 = models[i];
                // jump those have already been visited
                int j = Math.Max(i + 1, start + 1);
                for (; j < models.Count; ++j)
                {
                    Model m2 = models[j];
                    // select parts for crossover
                    bool isValid = this.selectNodesForCrossover(m1._GRAPH, m2._GRAPH, rand);
                    if (!isValid)
                    {
                        continue;
                    }
                    // include all inner connected nodes
                    List<Node> nodes1 = m1._GRAPH.getNodePropagation(m1._GRAPH.selectedNodes);
                    List<Node> nodes2 = m2._GRAPH.getNodePropagation(m2._GRAPH.selectedNodes);
                    if (nodes1.Count < m1._GRAPH._NNodes)
                    {
                        m1._GRAPH.selectedNodes = nodes1;
                    }
                    if (nodes2.Count < m2._GRAPH._NNodes)
                    {
                        m2._GRAPH.selectedNodes = nodes2;
                    }
                    PartGroup pg1, pg2;
                    List<Model> results = this.crossOverOp(m1, m2, gen, m_idx, -1, -1, out pg1, out pg2);
                    m_idx += 2;
                    foreach (Model m in results)
                    {
                        if (m._GRAPH.isValid())
                        {
                            // unify first, transform poitns, and then compose
                            m.unify();
                            m.composeMesh();
                            crossed.Add(m);
                            //if (crossed.Count > 15) { return crossed; }
                            // screenshot
                            this.setCurrentModel(m, -1);
                            Program.GetFormMain().updateStats();
                            this.captureScreen(imageFolder + m._model_name + ".png");
                            saveAPartBasedModel(m, m._path + m._model_name + ".pam", false);
                        }
                    }
                }
            }
            return crossed;
        }// runCrossover

        private bool selectNodesForCrossover(Graph g1, Graph g2, Random rand)
        {
            // select functionality, one or more
            List<Functionality.Functions> funcs = new List<Functionality.Functions>();
            int max_func_search = Functionality._NUM_FUNCTIONALITY;
            int search = 0;
            g1.selectedNodes.Clear();
            g2.selectedNodes.Clear();
            int option = rand.Next(4);
            if (option < 2)
            {
                // select functionality
                while (search < max_func_search && !this.isValidSelection(g1, g2))
                {
                    // only switch 1 functionality at one time
                    funcs = this.selectFunctionality(rand, 1);
                    funcs.Clear();
                    funcs.Add(Functionality.Functions.SUPPORT);
                    if (option == 0)
                    {
                        g1.selectedNodes = g1.getNodesAndDependentsByFunctionality(funcs);
                        g2.selectedNodes = g2.getNodesAndDependentsByFunctionality(funcs);
                    }
                    else
                    {
                        g1.selectedNodes = g1.getNodesByUniqueFunctionality(funcs[0]);
                        g2.selectedNodes = g2.getNodesByUniqueFunctionality(funcs[0]);
                    }
                    ++search;
                }
            }
            bool isValid = this.isValidSelection(g1, g2);
            if (option > 1 || !isValid)
            {
                g1.selectedNodes.Clear();
                g2.selectedNodes.Clear();
                // symmetry
                List<int> visitedFuncs = new List<int>();
                int nDiffFuncs = Enum.GetNames(typeof(Functionality.Functions)).Length;
                while (!this.isValidSelection(g1, g2) && visitedFuncs.Count < nDiffFuncs)
                {
                    int nf = rand.Next(nDiffFuncs);
                    while (visitedFuncs.Contains(nf))
                    {
                        nf = rand.Next(nDiffFuncs);
                    }
                    visitedFuncs.Add(nf);
                    Functionality.Functions func = this.getFunctionalityFromIndex(nf);
                    g1.selectedNodes = g1.selectSymmetryFuncNodes(func);
                    g2.selectedNodes = g2.selectSymmetryFuncNodes(func);
                }
                if (option > 1)
                {
                    // break symmetry
                    List<Node> nodes1 = new List<Node>();
                    List<Node> nodes2 = new List<Node>();
                    foreach (Node node in g1.selectedNodes)
                    {
                        if (!nodes1.Contains(node) && !nodes1.Contains(node.symmetry))
                        {
                            nodes1.Add(node);
                        }
                    }
                    foreach (Node node in g2.selectedNodes)
                    {
                        if (!nodes2.Contains(node) && !nodes2.Contains(node.symmetry))
                        {
                            nodes2.Add(node);
                        }
                    }
                    g1.selectedNodes = nodes1;
                    g2.selectedNodes = nodes2;
                }
            }
            return this.isValidSelection(g1, g2);
        }//selectNodesForCrossover

        private bool isValidSelection(Graph g1, Graph g2)
        {
            if (g1.selectedNodes.Count == g1._NNodes && g2.selectedNodes.Count == g2._NNodes)
            {
                return false;
            }
            if (g1.selectedNodes.Count == 0 || g2.selectedNodes.Count == 0)
            {
                return false;
            }
            // same functionality parts in g1.selected vs. g2.unselected
            List<Node> g1_unselected = new List<Node>(g1._NODES);
            foreach (Node node in g1.selectedNodes)
            {
                g1_unselected.Remove(node);
            }
            List<Node> g2_unselected = new List<Node>(g2._NODES);
            foreach (Node node in g2.selectedNodes)
            {
                g2_unselected.Remove(node);
            }
            if (g1_unselected.Count == 0 || g2_unselected.Count == 0)
            {
                return false;
            }

            //List<Functionality.Functions> g1_selected_funcs = getAllFuncs(g1.selectedNodes);
            //List<Functionality.Functions> g2_selected_funcs = getAllFuncs(g2.selectedNodes);
            //List<Functionality.Functions> g1_unselected_funcs = getAllFuncs(g1_unselected);
            //List<Functionality.Functions> g2_unselected_funcs = getAllFuncs(g2_unselected);

            //if (this.hasfunctionIntersection(g1_selected_funcs, g2_unselected_funcs) ||
            //    this.hasfunctionIntersection(g1_unselected_funcs, g2_selected_funcs))
            //{
            //    return false;
            //}            
            return true;
        }// isValidSelection

        private List<Functionality.Functions> getAllFuncs(List<Node> nodes)
        {
            List<Functionality.Functions> funcs = new List<Functionality.Functions>();
            foreach (Node node in nodes)
            {
                foreach (Functionality.Functions f in node._funcs)
                {
                    if (!funcs.Contains(f))
                    {
                        funcs.Add(f);
                    }
                }
            }
            return funcs;
        }// getAllFuncs

        private bool hasfunctionIntersection(List<Functionality.Functions> funcs1, List<Functionality.Functions> funcs2)
        {
            foreach (Functionality.Functions f1 in funcs1)
            {
                if (funcs2.Contains(f1))
                {
                    return true;
                }
            }
            return false;
        }// hasIntersection

        public List<Model> crossOverOp(Model m1, Model m2, int gen, int idx, int p1, int p2, out PartGroup pg1, out PartGroup pg2)
        {
            List<Model> crossModels = new List<Model>();
            pg1 = null;
            pg2 = null;
            if (m1 == null || m2 == null)
            {
                return crossModels;
            }

            List<Node> nodes1 = new List<Node>();
            List<Node> nodes2 = new List<Node>();
            //findOneToOneMatchingNodes(g1, g2, out nodes1, out nodes2);

            Model newM1 = m1.Clone() as Model;
            Model newM2 = m2.Clone() as Model;
            newM1._path = crossoverFolder + "gen_" + gen.ToString() + "\\";
            newM2._path = crossoverFolder + "gen_" + gen.ToString() + "\\";
            if (!Directory.Exists(newM1._path))
            {
                Directory.CreateDirectory(newM1._path);
            }
            if (!Directory.Exists(newM2._path))
            {
                Directory.CreateDirectory(newM2._path);
            }
            // using model names will be too long, exceed the maximum length of file name
            //newM1._model_name = m1._model_name + "_c_" + m2._model_name;
            //newM2._model_name = m2._model_name + "_c_" + m1._model_name;
            if (p1 != -1 && p2 != -1)
            {
                newM1._model_name = "gen_" + gen.ToString() + "_num_" + idx.ToString() + "_pg_" + p1.ToString() + "_" + p2.ToString();
                newM2._model_name = "gen_" + gen.ToString() + "_num_" + idx.ToString() + "_pg_" + p2.ToString() + "_" + p1.ToString();
            }
            else
            {
                newM1._model_name = "gen_" + gen.ToString() + "_" + idx.ToString();
                newM2._model_name = "gen_" + gen.ToString() + "_" + (idx + 1).ToString();
            }
            foreach (Node node in m1._GRAPH.selectedNodes)
            {
                nodes1.Add(newM1._GRAPH._NODES[node._INDEX]);
            }
            foreach (Node node in m2._GRAPH.selectedNodes)
            {
                nodes2.Add(newM2._GRAPH._NODES[node._INDEX]);
            }

            if (nodes1 == null || nodes2 == null)
            {
                return null;
            }

            List<Node> updatedNodes1;
            List<Node> updatedNodes2;
            List<Model> parents = new List<Model>(); // to set parent names
            parents.Add(m1);
            parents.Add(m2);
            // switch
            switchNodes(newM1._GRAPH, newM2._GRAPH, nodes1, nodes2, out updatedNodes1, out updatedNodes2);
            newM1.replaceNodes(nodes1, updatedNodes2);
            newM1.nNewNodes = updatedNodes2.Count;
            pg1 = new PartGroup(updatedNodes2, gen);
            newM1.setParentNames(parents);
            crossModels.Add(newM1);

            // if insert, we do not know where to insert
            newM2.replaceNodes(nodes2, updatedNodes1);
            pg2 = new PartGroup(updatedNodes1, gen);
            newM2.nNewNodes = updatedNodes1.Count;
            newM2.setParentNames(parents);
            crossModels.Add(newM2);

            return crossModels;
        }// crossover

        private int getAxisAlignedAxis(Vector3d v)
        {
            int axis = -1;
            double maxv = double.MinValue;
            for (int i = 0; i < 3; ++i)
            {
                if (Math.Abs(v[i]) > maxv)
                {
                    maxv = Math.Abs(v[i]);
                    axis = i;
                }
            }
            return axis;
        }//getAxisAlignedAxis

        private int hasCylinderNode(List<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                if (node._PART._BOUNDINGBOX.type == Common.PrimType.Cylinder)
                {
                    return this.getAxisAlignedAxis(node._PART._BOUNDINGBOX.rot_axis);
                }
            }
            return -1;
        }// hasCylinderNode

        private double[] updateScalesForCylinder(double[] scales, int axis)
        {
            double max_scale = double.MinValue;
            for (int i = 0; i < scales.Length; ++i)
            {
                max_scale = max_scale > scales[i] ? max_scale : scales[i];
            }
            for (int i = 0; i < scales.Length; ++i)
            {
                if (axis == i)
                {
                    scales[i] = max_scale;
                }
                else
                {
                    scales[i] = 1.0;
                }
            }
            return scales;
        }// updateScalesForCylinder

        private void switchNodes(Graph g1, Graph g2, List<Node> nodes1, List<Node> nodes2,
            out List<Node> updateNodes1, out List<Node> updateNodes2)
        {
            updateNodes1 = cloneNodesAndRelations(nodes1);
            updateNodes2 = cloneNodesAndRelations(nodes2);

            List<Edge> edgesToConnect_1 = g1.getOutgoingEdges(nodes1);
            List<Edge> edgesToConnect_2 = g2.getOutgoingEdges(nodes2);
            List<Vector3d> sources = collectPoints(edgesToConnect_1);
            List<Vector3d> targets = collectPoints(edgesToConnect_2);

            Vector3d center1 = new Vector3d();
            Vector3d maxv_s = Vector3d.MinCoord;
            Vector3d minv_s = Vector3d.MaxCoord;
            Vector3d maxv_t = Vector3d.MinCoord;
            Vector3d minv_t = Vector3d.MaxCoord;

            foreach (Node node in nodes1)
            {
                center1 += node._PART._BOUNDINGBOX.CENTER;
                maxv_s = Vector3d.Max(maxv_s, node._PART._BOUNDINGBOX.MaxCoord);
                minv_s = Vector3d.Min(minv_s, node._PART._BOUNDINGBOX.MinCoord);
            }
            //center1 /= nodes1.Count;
            center1 = (maxv_s + minv_s) / 2;

            Vector3d center2 = new Vector3d();
            foreach (Node node in nodes2)
            {
                center2 += node._PART._BOUNDINGBOX.CENTER;
                maxv_t = Vector3d.Max(maxv_t, node._PART._BOUNDINGBOX.MaxCoord);
                minv_t = Vector3d.Min(minv_t, node._PART._BOUNDINGBOX.MinCoord);
            }
            //center2 /= nodes2.Count;
            center2 = (maxv_t + minv_t) / 2;

            double[] scale1 = { 1.0, 1.0, 1.0 };
            if (nodes1.Count > 0)
            {
                scale1[0] = (maxv_t.x - minv_t.x) / (maxv_s.x - minv_s.x);
                scale1[1] = (maxv_t.y - minv_t.y) / (maxv_s.y - minv_s.y);
                scale1[2] = (maxv_t.z - minv_t.z) / (maxv_s.z - minv_s.z);
            }

            int axis = this.hasCylinderNode(nodes1);
            if (axis != -1)
            {
                scale1 = this.updateScalesForCylinder(scale1, axis);
            }
            Vector3d boxScale_1 = new Vector3d(scale1[0], scale1[1], scale1[2]);
            double[] scale2 = { 1.0, 1.0, 1.0 };
            if (nodes2.Count > 0)
            {
                scale2[0] = (maxv_s.x - minv_s.x) / (maxv_t.x - minv_t.x);
                scale2[1] = (maxv_s.y - minv_s.y) / (maxv_t.y - minv_t.y);
                scale2[2] = (maxv_s.z - minv_s.z) / (maxv_t.z - minv_t.z);
            }
            axis = this.hasCylinderNode(nodes2);
            if (axis != -1)
            {
                scale2 = this.updateScalesForCylinder(scale2, axis);
            }
            Vector3d boxScale_2 = new Vector3d(scale2[0], scale2[1], scale2[2]);

            Matrix4d S, T, Q;

            // sort corresponding points
            int nps = sources.Count;
            bool startWithSrc = true;
            List<Vector3d> left = sources;
            List<Vector3d> right = targets;
            if (targets.Count < nps)
            {
                nps = targets.Count;
                startWithSrc = false;
                left = targets;
                right = sources;
            }
            List<Vector3d> src = new List<Vector3d>();
            List<Vector3d> trt = new List<Vector3d>();
            bool[] visited = new bool[right.Count];
            foreach (Vector3d v in left)
            {
                src.Add(v);
                int j = -1;
                double mind = double.MaxValue;
                for (int i = 0; i < right.Count; ++i)
                {
                    if (visited[i]) continue;
                    double d = (v - right[i]).Length();
                    if (d < mind)
                    {
                        mind = d;
                        j = i;
                    }
                }
                trt.Add(right[j]);
                visited[j] = true;
            }
            if (startWithSrc)
            {
                sources = src;
                targets = trt;
            }
            else
            {
                sources = trt;
                targets = src;
            }


            if (sources.Count <= 1)
            {
                sources.Add(center1);
                targets.Add(center2);
            }

            Node ground1 = hasGroundTouchingNode(nodes1);
            Node ground2 = hasGroundTouchingNode(nodes2);
            bool useGround = ground1 != null && ground2 != null;
            if (useGround)
            {
                sources.Add(g1.getGroundTouchingNodesCenter());
                targets.Add(g2.getGroundTouchingNodesCenter());
                //sources.Add(new Vector3d(sources[0].x, 0, sources[0].z));
                //targets.Add(new Vector3d(targets[0].x, 0, targets[0].z));
            }
            bool userCenter = nodes1.Count == 1 || nodes2.Count == 1;
            if (nodes1.Count > 0 && nodes2.Count > 0)
            {
                getTransformation(sources, targets, out S, out T, out Q, boxScale_1, true, center2, center1, userCenter, -1, useGround);
                this.deformNodesAndEdges(updateNodes1, Q);
            }
            
            if (ground2 != null)
            {
                g1.resetUpdateStatus();
                adjustGroundTouching(updateNodes1);
            }
            g1.resetUpdateStatus();

            if (nodes2.Count > 0 && nodes1.Count > 0)
            {
                getTransformation(targets, sources, out S, out T, out Q, boxScale_2, true, center1, center2, userCenter, -1, useGround);
                this.deformNodesAndEdges(updateNodes2, Q);
            }
            
            if (ground1 != null)
            {
                g2.resetUpdateStatus();
                adjustGroundTouching(updateNodes2);
            }
            g2.resetUpdateStatus();
        }// switchNodes

        private void deformNodesAndEdges(List<Node> nodes, Matrix4d T)
        {
            foreach (Node node in nodes)
            {
                if(node._updated)
                {
                    continue;
                }
                node.Transform(T);
                node._updated = true;
            }
            List<Edge> inner_edges = Graph.GetAllEdges(nodes);
            foreach (Edge e in inner_edges)
            {
                if (e._contactUpdated)
                {
                    continue;
                }
                e.TransformContact(T);
            }
        }// deformNodesAndEdges

        private void adjustGroundTouching(List<Node> nodes)
        {
            double miny = double.MaxValue;
            foreach (Node node in nodes)
            {
                double y = node._PART._BOUNDINGBOX.MinCoord.y;
                miny = miny < y ? miny : y;
            }
            Vector3d trans = new Vector3d(0, -miny, 0);
            Matrix4d T = Matrix4d.TranslationMatrix(trans);

            this.deformNodesAndEdges(nodes, T);

            // in case the nodes are not marked as ground touching
            foreach (Node node in nodes)
            {
                double ydist = node._PART._MESH.MinCoord.y;
                if (Math.Abs(ydist) < Common._thresh)
                {
                    node._isGroundTouching = true;
                    node.addFunctionality(Functionality.Functions.GROUND_TOUCHING);
                }
            }
        }// adjustGroundTouching

        private List<Node> cloneNodesAndRelations(List<Node> nodes)
        {
            List<Node> clone_nodes = new List<Node>();
            foreach (Node node in nodes)
            {
                Node cloned = node.Clone() as Node;
                clone_nodes.Add(cloned);
            }
            // edges
            for (int i = 0; i < nodes.Count; ++i)
            {
                Node node = nodes[i];
                foreach (Edge e in node._edges)
                {
                    Node other = e._start == node ? e._end : e._start;
                    int j = nodes.IndexOf(other);
                    Node adjNode = other;
                    if (j != -1)
                    {
                        adjNode = clone_nodes[j];
                    }
                    if (adjNode == other || j > i)
                    {
                        List<Contact> contacts = new List<Contact>();
                        foreach (Contact c in e._contacts)
                        {
                            contacts.Add(new Contact(c._pos3d));
                        }
                        Edge clone_edge = new Edge(clone_nodes[i], adjNode, contacts);
                        clone_nodes[i].addAnEdge(clone_edge);
                        if (adjNode != other)
                        {
                            adjNode.addAnEdge(clone_edge);
                        }
                    }
                }
            }
            return clone_nodes;
        }// cloneNodesAndRelations

        private int runMutateOrCrossover(Random rand)
        {
            int n = 5;
            int r = rand.Next(n);
            if (r >= 3)
            {
                r = 2;
            }
            return r;
        }// runMutateOrCrossover

        private List<Functionality.Functions> selectFunctionality(Random rand, int maxNfunc)
        {
            int n = Functionality._NUM_FUNCTIONALITY;
            int r = rand.Next(n) + 1;
            r = Math.Min(r, maxNfunc);
            List<Functionality.Functions> funcs = new List<Functionality.Functions>();
            for (int i = 0; i < r; ++i)
            {
                int j = rand.Next(n);
                if (j >= n)
                {
                    j = 2;
                }
                Functionality.Functions f = getFunctionalityFromIndex(j);
                if (!funcs.Contains(f))
                {
                    funcs.Add(f);
                }
            }
            return funcs;
        }// selectAFunctionality

        private List<Node> selectSubsetNodes(List<Node> nodes, int n)
        {
            List<Node> selected = new List<Node>();
            List<Node> sym_nodes = new List<Node>();
            Random rand = new Random();
            foreach (Node node in nodes)
            {
                if (node.symmetry != null && !selected.Contains(node) && !selected.Contains(node.symmetry))
                {
                    selected.Add(node);
                    selected.Add(node.symmetry);
                }
            }
            if (n < 2)
            {
                // take symmetry
                int nsym = sym_nodes.Count / 2;
                for (int i = 0; i < nsym; i += 2)
                {
                    int s = rand.Next(2);
                    if (s == 0)
                    {
                        selected.Add(sym_nodes[i]);
                        selected.Add(sym_nodes[i + 1]);
                    }
                }
                if (selected.Count == 0 && sym_nodes.Count != 0)
                {
                    int j = rand.Next(nsym);
                    selected.Add(sym_nodes[j * 2]);
                    selected.Add(sym_nodes[j * 2 + 1]);
                }
            }
            else if (n < 4)
            {
                //// node propagation --- all inner nodes that only connect to the #nodes#
                //selected = Graph.GetNodePropagation(nodes);
                // break symmetry
                if (sym_nodes.Count == 2)
                {
                    int s = rand.Next(2);
                    selected.Add(sym_nodes[s]);
                }
            }
            else
            {
                selected = nodes;
            }
            return selected;
        }// selectSubsetNodes

        private bool hasGroundTouching(List<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                if (node._isGroundTouching)
                {
                    return true;
                }
            }
            return false;
        }// hasGroundTouching

        private void selectReplaceableNodesPair(Graph g1, Graph g2, out List<List<Node>> nodeChoices1, out List<List<Node>> nodeChoices2)
        {
            nodeChoices1 = new List<List<Node>>();
            nodeChoices2 = new List<List<Node>>();
            // ground touching
            List<Node> nodes1 = g1.getGroundTouchingNodes();
            List<Node> nodes2 = g2.getGroundTouchingNodes();
            nodeChoices1.Add(nodes1);
            nodeChoices2.Add(nodes2);

            // Key nodes
            List<List<Node>> splitNodes1 = g1.splitAlongKeyNode();
            List<List<Node>> splitNodes2 = g2.splitAlongKeyNode();
            if (splitNodes1.Count < splitNodes2.Count)
            {
                if (hasGroundTouching(splitNodes2[1]))
                {
                    splitNodes2.RemoveAt(2);
                }
                else
                {
                    splitNodes2.RemoveAt(1);
                }
            }
            else if (splitNodes1.Count > splitNodes2.Count)
            {
                if (hasGroundTouching(splitNodes1[1]))
                {
                    splitNodes1.RemoveAt(2);
                }
                else
                {
                    splitNodes1.RemoveAt(1);
                }
            }
            else
            {
                nodeChoices1.AddRange(splitNodes1);
                nodeChoices2.AddRange(splitNodes2);
            }
            // symmetry nodes
            List<List<Node>> symPairs1 = g1.getSymmetryPairs();
            List<List<Node>> symPairs2 = g2.getSymmetryPairs();
            List<int> outEdgeNum1 = new List<int>();
            List<int> outEdgeNum2 = new List<int>();
            foreach (List<Node> nodes in symPairs1)
            {
                outEdgeNum1.Add(g1.getOutgoingEdges(nodes).Count);
            }
            foreach (List<Node> nodes in symPairs2)
            {
                outEdgeNum2.Add(g2.getOutgoingEdges(nodes).Count);
            }
            for (int i = 0; i < symPairs1.Count; ++i)
            {
                bool isGround1 = symPairs1[i][0]._isGroundTouching;
                for (int j = 0; j < symPairs2.Count; ++j)
                {
                    bool isGround2 = symPairs2[j][0]._isGroundTouching;
                    if ((isGround1 && isGround2) || (!isGround1 && !isGround2 && outEdgeNum1[i] == outEdgeNum2[j]))
                    {
                        nodeChoices1.Add(symPairs1[i]);
                        nodeChoices2.Add(symPairs2[j]);
                    }
                }
            }
        }// selectReplaceableNodesPair

        public void setSelectedNodes()
        {
            if (_currModel == null)
            {
                return;
            }
            _currModel._GRAPH.selectedNodes = new List<Node>();
            foreach (Node node in _currModel._GRAPH._NODES)
            {
                if (_selectedParts.Contains(node._PART))
                {
                    _currModel._GRAPH.selectedNodes.Add(node);
                }
            }
            if (!_crossOverBasket.Contains(_currModel))
            {
                _crossOverBasket.Add(_currModel);
            }
            _currModel._GRAPH.selectedNodePairs.Add(_currModel._GRAPH.selectedNodes);
            Program.writeToConsole(_currModel._GRAPH.selectedNodes.Count.ToString() + " nodes in Graph #" + _selectedModelIndex.ToString() + " are selcted.");
        }// setSelectedNodes

        private void deformANodeAndEdges(Node node, Matrix4d T)
        {
            node.Transform(T);
            node._updated = true;
            foreach (Edge e in node._edges)
            {
                if (!e._contactUpdated)
                {
                    e.TransformContact(T);
                }
            }
        }// deformANodeAndEdges

        private bool deformSymmetryNode(Node node)
        {
            if (node.symmetry == null || node.symmetry._updated)
            {
                return false;
            }

            Node other = node.symmetry;
            Symmetry symm = node.symm;
            // get scales
            Vector3d s2 = node._PART._BOUNDINGBOX._scale;
            Vector3d s1 = other._PART._BOUNDINGBOX._scale;

            Vector3d cc = Matrix4d.GetMirrorSymmetryPoint(node._pos, symm._axis, symm._center);
            Matrix4d T1 = Matrix4d.TranslationMatrix(new Vector3d() - other._pos);
            Matrix4d T2 = Matrix4d.TranslationMatrix(cc);
            Matrix4d S = Matrix4d.ScalingMatrix(s2.x / s1.x, s2.y / s1.y, s2.z / s1.z);

            Matrix4d Q = T2 * S * T1;

            deformANodeAndEdges(other, Q);

            return true;
        }// deformSymmetryNode

        private void deformPropagation(Graph graph, Node edited)
        {
            Node activeNode = edited;
            activeNode._updated = true;
            int time = 0;
            while (activeNode != null)
            {
                int maxNumUpdatedContacts = -1;
                activeNode = null;
                foreach (Node node in graph._NODES)
                {
                    if (node._updated && !node.isAllNeighborsUpdated())
                    {
                        int nUpdatedContacts = 0;
                        foreach (Edge e in node._edges)
                        {
                            if (e._contactUpdated)
                            {
                                ++nUpdatedContacts;
                            }
                        }
                        if (nUpdatedContacts > maxNumUpdatedContacts)
                        {
                            maxNumUpdatedContacts = nUpdatedContacts;
                            activeNode = node;
                        }
                    }
                }// foreach
                if (activeNode != null)
                {
                    // deform all neighbors
                    activeNode._allNeigborUpdated = true;
                    // deoform from the most _updated one
                    Node toUpdate = activeNode;
                    while (toUpdate != null)
                    {
                        int nMatxUpdatedContacts = -1;
                        toUpdate = null;
                        foreach (Node node in activeNode._adjNodes)
                        {
                            int nUpdatedContacts = 0;
                            if (node._updated)
                            {
                                continue;
                            }
                            foreach (Edge e in node._edges)
                            {
                                if (e._contactUpdated)
                                {
                                    ++nUpdatedContacts;
                                }
                            }
                            if (nUpdatedContacts > nMatxUpdatedContacts)
                            {
                                nMatxUpdatedContacts = nUpdatedContacts;
                                toUpdate = node;
                            }
                        }
                        if (toUpdate != null)
                        {
                            deformNode(toUpdate);
                            deformSymmetryNode(toUpdate);
                            if (hasInValidContact(graph))
                            {
                                ++time;
                            }
                        }
                    }
                }
            }// while
        }// deformPropagation        

        private void deformNode(Node node)
        {
            List<Vector3d> sources = new List<Vector3d>();
            List<Vector3d> targets = new List<Vector3d>();
            foreach (Edge e in node._edges)
            {
                if (e._contactUpdated)
                {
                    sources.AddRange(e.getOriginContactPoints());
                    targets.AddRange(e.getContactPoints());
                }
            }
            bool useGround = false;
            if (sources.Count > 0 && node._isGroundTouching)
            {
                sources.Add(new Vector3d(sources[0].x, 0, sources[0].z));
                targets.Add(new Vector3d(targets[0].x, 0, targets[0].z));
                useGround = true;
            }
            Matrix4d T, S, Q;
            getTransformation(sources, targets, out S, out T, out Q, new Vector3d(1,1,1), false, null, null, false, -1, useGround);
            node.Transform(Q);
            node._updated = true;
            foreach (Edge e in node._edges)
            {
                if (e._contactUpdated)
                {
                    continue;
                }
                e.TransformContact(Q);
            }
        }// deformNode

        private Node hasGroundTouchingNode(List<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                if (node._isGroundTouching)
                {
                    return node;
                }
            }
            return null;
        }// hasGroundTouchingNode

        private List<Node> getGroundTouchingNode(List<Node> nodes)
        {
            List<Node> grounds = new List<Node>();
            foreach (Node node in nodes)
            {
                if (node._isGroundTouching)
                {
                    grounds.Add(node);
                }
            }
            return grounds;
        }// getGroundTouchingNode

        private List<Vector3d> collectPoints(List<Edge> edges)
        {
            List<Vector3d> points = new List<Vector3d>();
            foreach (Edge e in edges)
            {
                points.AddRange(e.getContactPoints());
            }
            // if two points are too close, merge them
            for (int i = 0; i < points.Count - 1; ++i)
            {
                Vector3d vi = points[i];
                for (int j = i + 1; j < points.Count; ++j)
                {
                    Vector3d vj = points[j];
                    double d = (vi - vj).Length();
                    if (d < 0.02)
                    {
                        points.RemoveAt(j);
                        --j;
                    }
                }
            }
            return points;
        }// collectPoints

        private bool isNaNMat(Matrix4d m)
        {
            for (int i = 0; i < 16; ++i)
            {
                if (double.IsNaN(m[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public void getTransformation(List<Vector3d> srcpts, List<Vector3d> tarpts, 
            out Matrix4d S, out Matrix4d T, out Matrix4d Q, 
            Vector3d boxScale, bool useScale, 
            Vector3d tc, Vector3d sc, bool useCenter, 
            double storageScale, //y-axis to preserve the volume
            bool isLastGroundPoint)
        {
            // when estimating the scaling, if the last point is the ground point, the calculation can go wrong, see below
            int n = srcpts.Count;
            if (n == 1)
            {
                double ss = 1;
                Vector3d trans = tarpts[0] - srcpts[0];
                S = Matrix4d.ScalingMatrix(ss, ss, ss);
                if (useScale && boxScale.isValidVector())
                {
                    S = Matrix4d.ScalingMatrix(boxScale);
                }
                T = Matrix4d.TranslationMatrix(trans);
                Q = Matrix4d.TranslationMatrix(tarpts[0]) * S * Matrix4d.TranslationMatrix(new Vector3d() - srcpts[0]);
                if (useCenter)
                {
                    T = Matrix4d.TranslationMatrix(tc - sc);
                    Q = Matrix4d.TranslationMatrix(tc) * S * Matrix4d.TranslationMatrix(new Vector3d() - sc);
                }
                
                if (isNaNMat(Q))
                {
                    Q = Matrix4d.IdentityMatrix();
                }
            }
            else if (n == 2)
            {
                Vector3d c1 = (srcpts[0] + srcpts[1]) / 2;
                Vector3d c2 = (tarpts[0] + tarpts[1]) / 2;
                Vector3d v1 = srcpts[1] - srcpts[0];
                Vector3d v2 = tarpts[1] - tarpts[0];
                if (v1.Dot(v2) < 0) v1 = new Vector3d() - v1;
                double ss = v2.Length() / v1.Length();
                if (double.IsNaN(ss))
                {
                    ss = 1.0;
                }
                S = Matrix4d.ScalingMatrix(ss, ss, ss);

                if (boxScale.isValidVector() && useScale)
                {
                    S = Matrix4d.ScalingMatrix(boxScale);
                }

                Matrix4d R = Matrix4d.IdentityMatrix();
                double cos = v1.normalize().Dot(v2.normalize());
                if (cos < Math.Cos(1.0 / 18 * Math.PI))
                {
                    Vector3d axis = v1.Cross(v2).normalize();
                    double theta = Math.Acos(cos);
                    R = Matrix4d.RotationMatrix(axis, theta);
                    if (isNaNMat(R))
                    {
                        R = Matrix4d.IdentityMatrix();
                    }
                }
                T = Matrix4d.TranslationMatrix(c2 - c1);
                Q = Matrix4d.TranslationMatrix(c2) * R * S * Matrix4d.TranslationMatrix(new Vector3d() - c1);
                if (useCenter)
                {
                    T = Matrix4d.TranslationMatrix(tc - sc);
                    Q = Matrix4d.TranslationMatrix(tc) * S * Matrix4d.TranslationMatrix(new Vector3d() - sc);
                }                
                if (isNaNMat(Q))
                {
                    Q = Matrix4d.IdentityMatrix();
                }
            }
            else
            {
                Vector3d t1 = new Vector3d();
                Vector3d t2 = new Vector3d();
                foreach (Vector3d tt in srcpts)
                    t1 += tt;
                foreach (Vector3d tt in tarpts)
                    t2 += tt;
                t1 /= srcpts.Count;
                t2 /= tarpts.Count;

                Vector3d trans = t2 - t1;
                T = Matrix4d.TranslationMatrix(trans);

                // find the scales
                int k = srcpts.Count;
                double sx = 0, sy = 0, sz = 0;
                if (isLastGroundPoint)
                {
                    k--;
                }
                // if the last point is added ground point, remove it from scaling calculation to avoid error
                // e.g., the distance from this point to the center is 0 in two axes, meaning the two scaling axes will contribute 0
                double maxy = double.MinValue;
                double miny = double.MaxValue;
                for (int i = 0; i < k; ++i)
                {
                    Vector3d p1 = srcpts[i] - t1;
                    Vector3d p2 = tarpts[i] - t2;
                    sx += p2.x / p1.x;
                    sy += p2.y / p1.y;
                    sz += p2.z / p1.z;
                    maxy = Math.Max(maxy, srcpts[i].y);
                    miny = Math.Min(miny, srcpts[i].y);
                }
                sx /= k;
                sy /= k;
                sz /= k;


                    sx = sx < 0 ? boxScale.x : sx;
                    sy = sy < 0 ? boxScale.y : sy;
                    sz = sz < 0 ? boxScale.z : sz;

                if (isInValidScale(sx))
                {
                    sx = 1.0;
                }
                if (isInValidScale(sy))
                {
                    sy = 1.0;
                }
                if (isInValidScale(sz))
                {
                    sz = 1.0;
                }

                if (storageScale != -1)
                {
                    sy = storageScale;
                }

                Vector3d scale = new Vector3d(sx, sy, sz);
                scale = adjustScale(scale);
                if (maxy - miny < 0.05) // contact points on the a plane, preserve z-scale
                {
                    scale[1] = (scale[0] + scale[2]) / 2;
                }
                //if (double.IsNaN(scale.x) || double.IsNaN(trans.x)) throw new Exception();

                S = Matrix4d.ScalingMatrix(scale.x, scale.y, scale.z);

                if (useScale && boxScale.isValidVector())
                {
                    S = Matrix4d.ScalingMatrix(boxScale);
                }
                Q = Matrix4d.TranslationMatrix(t2) * S * Matrix4d.TranslationMatrix(new Vector3d() - t1);
                if (useCenter)
                {
                    T = Matrix4d.TranslationMatrix(tc - sc);
                    Q = Matrix4d.TranslationMatrix(tc) * S * Matrix4d.TranslationMatrix(new Vector3d() - sc);
                }
                if (isNaNMat(Q))
                {
                    Q = Matrix4d.IdentityMatrix();
                }
            }
        }// getTransformation

        private bool isInValidScale(double s)
        {
            return double.IsNaN(s) || double.IsInfinity(s) || s < Common._thresh;
        }// isInValidScale

        private Vector3d adjustScale(Vector3d scale)
        {
            for (int i = 0; i < 3; ++i)
            {
                if (scale[i] > Common._max_scale)
                {
                    scale[i] = Common._max_scale / 2;
                }

                if (scale[i] < Common._min_scale)
                {
                    scale[i] = Common._min_scale * 2;
                }
            }
            return scale;
        }// adjustScale

        private bool hasInvalidVec(Vector3d[] vecs)
        {
            foreach (Vector3d v in vecs)
            {
                if (!v.isValidVector())
                {
                    return true;
                }
            }
            return false;
        }// hasInvalidVec

        /*****************end - Functions-aware evolution*************************/


        //########## set modes ##########//
        public void setTabIndex(int i)
        {
            this.currMeshClass.tabIndex = i;
        }

        public void setUIMode(int i)
        {
            switch (i)
            {
                case 1:
                    this.currUIMode = UIMode.VertexSelection;
                    break;
                case 2:
                    this.currUIMode = UIMode.EdgeSelection;
                    break;
                case 3:
                    this.currUIMode = UIMode.FaceSelection;
                    break;
                case 4:
                    this.currUIMode = UIMode.BoxSelection;
                    break;
                case 6:
                    this.currUIMode = UIMode.Translate;
                    break;
                case 7:
                    this.currUIMode = UIMode.Scale;
                    break;
                case 8:
                    this.currUIMode = UIMode.Rotate;
                    break;
                case 9:
                    this.currUIMode = UIMode.Contact;
                    clearHighlights();
                    break;
                case 0:
                default:
                    this.currUIMode = UIMode.Viewing;
                    break;
            }
            if (i >= 6 && i <= 9)
            {
                this.calEditAxesLoc();
                _showEditAxes = true;
                this.Refresh();
            }
        }// setUIMode

        public void setRenderOption(int i)
        {
            switch (i)
            {
                case 1:
                    this.drawVertex = !this.drawVertex;
                    break;
                case 2:
                    this.drawEdge = !this.drawEdge;
                    break;
                case 4:
                    this.isDrawBbox = !this.isDrawBbox;
                    break;
                case 5:
                    this.isDrawGraph = !this.isDrawGraph;
                    break;
                case 6:
                    this.isDrawFuncSpace = !this.isDrawFuncSpace;
                    break;
                case 3:
                default:
                    this.isDrawMesh = !this.isDrawMesh;
                    break;
            }
            this.Refresh();
        }//setRenderOption

        public void setShowHumanPoseOption(bool isTranlucent)
        {
            _isDrawTranslucentHumanPose = isTranlucent;
            this.Refresh();
        }

        public void setShowAxesOption(bool isShow)
        {
            this.isDrawAxes = isShow;
            this.Refresh();
        }

        public void setRandomColorToNodes()
        {
            if (_currModel != null && _currModel._GRAPH != null)
            {
                foreach (Node node in _currModel._GRAPH._NODES)
                {
                    node._PART.setRandomColorToNodes();
                }
            }
        }// setRandomColorToNodes

        private void calEditAxesLoc()
        {
            Vector3d center = new Vector3d();
            double ad = 0;
            if (_selectedParts.Count > 0)
            {
                foreach (Part p in _selectedParts)
                {
                    center += p._BOUNDINGBOX.CENTER;
                    double d = (p._BOUNDINGBOX.MaxCoord - p._BOUNDINGBOX.MinCoord).Length();
                    ad = ad > d ? ad : d;
                }
                center /= _selectedParts.Count;
            }
            else if (_currHumanPose != null)
            {
                center = _currHumanPose._ROOT._POS;
            }
            else if (_selectedEdge != null && _selectedContact != null)
            {
                center = _selectedContact._pos3d;
            }
            ad /= 2;
            if (ad == 0)
            {
                ad = 0.2;
            }
            double arrow_d = ad / 6;
            _editAxes = new Contact[18];
            for (int i = 0; i < _editAxes.Length; ++i)
            {
                _editAxes[i] = new Contact(new Vector3d());
            }
            _editAxes[0]._pos3d = center - ad * Vector3d.XCoord;
            _editAxes[1]._pos3d = center + ad * Vector3d.XCoord;
            _editAxes[2]._pos3d = _editAxes[1]._pos3d - arrow_d * Vector3d.XCoord + arrow_d * Vector3d.YCoord;
            _editAxes[3]._pos3d = new Vector3d(_editAxes[1]._pos3d);
            _editAxes[4]._pos3d = _editAxes[1]._pos3d - arrow_d * Vector3d.XCoord - arrow_d * Vector3d.YCoord;
            _editAxes[5]._pos3d = new Vector3d(_editAxes[1]._pos3d);

            _editAxes[6]._pos3d = center - ad * Vector3d.YCoord;
            _editAxes[7]._pos3d = center + ad * Vector3d.YCoord;
            _editAxes[8]._pos3d = _editAxes[7]._pos3d - arrow_d * Vector3d.YCoord + arrow_d * Vector3d.XCoord;
            _editAxes[9]._pos3d = new Vector3d(_editAxes[7]._pos3d);
            _editAxes[10]._pos3d = _editAxes[7]._pos3d - arrow_d * Vector3d.YCoord - arrow_d * Vector3d.XCoord;
            _editAxes[11]._pos3d = new Vector3d(_editAxes[7]._pos3d);

            _editAxes[12]._pos3d = center - ad * Vector3d.ZCoord;
            _editAxes[13]._pos3d = center + ad * Vector3d.ZCoord;
            _editAxes[14]._pos3d = _editAxes[13]._pos3d - arrow_d * Vector3d.ZCoord + arrow_d * Vector3d.XCoord;
            _editAxes[15]._pos3d = new Vector3d(_editAxes[13]._pos3d);
            _editAxes[16]._pos3d = _editAxes[13]._pos3d - arrow_d * Vector3d.ZCoord - arrow_d * Vector3d.XCoord;
            _editAxes[17]._pos3d = new Vector3d(_editAxes[13]._pos3d);
        }// calEditAxesLoc

        public void resetView()
        {
            this.arcBall.reset();
            if (this.nPointPerspective == 2)
            {
                this.eye = new Vector3d(eyePosition2D);
            }
            else
            {
                this.eye = new Vector3d(eyePosition3D);
            }
            this._currModelTransformMatrix = Matrix4d.IdentityMatrix();
            this._modelTransformMatrix = Matrix4d.IdentityMatrix();
            this.rotMat = Matrix4d.IdentityMatrix();
            this.scaleMat = Matrix4d.IdentityMatrix();
            this.transMat = Matrix4d.IdentityMatrix();
            this.cal2D();
            this.Refresh();
        }

        public void reloadView()
        {
            this.arcBall.reset();
            if (this.nPointPerspective == 2)
            {
                this.eye = new Vector3d(eyePosition2D);
            }
            else
            {
                this.eye = new Vector3d(eyePosition3D);
            }
            this._currModelTransformMatrix = new Matrix4d(this._fixedModelView);
            this._modelTransformMatrix = Matrix4d.IdentityMatrix();
            this.cal2D();
            this.Refresh();
        }

        public void reloadView2d()
        {
            this.arcBall.reset();
            this.eye = new Vector3d(eyePosition2D);
            double[] arr = { 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, -1, 0, 0, 0, 0, 1 };
            // camera model
            //double[] arr = { -1, 0, 0, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, 1 };
            this._currModelTransformMatrix = new Matrix4d(arr);
            this._modelTransformMatrix = Matrix4d.IdentityMatrix();
            this.cal2D();
            this.Refresh();
        }

        private void updateCamera()
        {
            if (this.camera == null) return;
            Matrix4d m = this._currModelTransformMatrix;
            double[] ballmat = m.Transpose().ToArray();	// matrix applied with arcball
            this.camera.SetBallMatrix(ballmat);
            this.camera.Update();
        }

        //########## Mouse ##########//
        private void viewMouseDown(MouseEventArgs e)
        {
            //if (this.currMeshClass == null) return;
            this.arcBall = new ArcBall(this.Width, this.Height);
            //this._currModelTransformMatrix = Matrix4d.IdentityMatrix();
            switch (e.Button)
            {
                case System.Windows.Forms.MouseButtons.Middle:
                    {
                        this.arcBall.mouseDown(e.X, e.Y, ArcBall.MotionType.Pan);
                        break;
                    }
                case System.Windows.Forms.MouseButtons.Right:
                    {
                        this.arcBall.mouseDown(e.X, e.Y, ArcBall.MotionType.Scale);
                        break;
                    }
                case System.Windows.Forms.MouseButtons.Left:
                default:
                    {
                        this.arcBall.mouseDown(e.X, e.Y, ArcBall.MotionType.Rotate);
                        break;
                    }
            }
            this._showEditAxes = false;
            this.clearHighlights();
        }// viewMouseDown

        private void viewMouseMove(int x, int y)
        {
            if (!this.isMouseDown) return;
            this.arcBall.mouseMove(x, y);
        }// viewMouseMove

        private int nPointPerspective = 3;

        private void viewMouseUp()
        {
            this._currModelTransformMatrix = this.arcBall.getTransformMatrix(this.nPointPerspective) * this._currModelTransformMatrix;
            if (this.arcBall.motion == ArcBall.MotionType.Pan)
            {
                this.transMat = this.arcBall.getTransformMatrix(this.nPointPerspective) * this.transMat;
            }
            else if (this.arcBall.motion == ArcBall.MotionType.Rotate)
            {
                this.rotMat = this.arcBall.getTransformMatrix(this.nPointPerspective) * this.rotMat;
            }
            else
            {
                this.scaleMat = this.arcBall.getTransformMatrix(this.nPointPerspective) * this.scaleMat;
            }
            this.arcBall.mouseUp();
            //this._modelTransformMatrix = this.transMat * this.rotMat * this.scaleMat;

            this._modelTransformMatrix = this._currModelTransformMatrix.Transpose();
        }// viewMouseUp

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            this.mouseDownPos = new Vector2d(e.X, e.Y);
            this.currMousePos = new Vector2d(e.X, e.Y);
            this.isMouseDown = true;
            this.highlightQuad = null;
            _isRightClick = e.Button == System.Windows.Forms.MouseButtons.Right;

            if (this.ContextMenuStrip != null)
            {
                this.ContextMenuStrip = null; // set hide() is not working, only when set it to null
            }

            switch (this.currUIMode)
            {
                case UIMode.VertexSelection:
                case UIMode.EdgeSelection:
                case UIMode.FaceSelection:
                    {
                        if (this.currMeshClass != null)
                        {
                            Matrix4d m = this.arcBall.getTransformMatrix(this.nPointPerspective) * this._currModelTransformMatrix;
                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPushMatrix();
                            Gl.glMultMatrixd(m.Transpose().ToArray());

                            this.currMeshClass.selectMouseDown((int)this.currUIMode,
                                Control.ModifierKeys == Keys.Shift,
                                Control.ModifierKeys == Keys.Control);

                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPopMatrix();

                            this.isDrawQuad = true;
                        }
                        break;
                    }
                case UIMode.BoxSelection:
                    {
                        if (this._currModel != null)
                        {
                            Matrix4d m = this.arcBall.getTransformMatrix(this.nPointPerspective) * this._currModelTransformMatrix;
                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPushMatrix();
                            Gl.glMultMatrixd(m.Transpose().ToArray());

                            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                            {
                                this.ContextMenuStrip = Program.GetFormMain().getRightButtonMenu();
                                this.ContextMenuStrip.Show();
                            }
                            else
                            {
                                this.selectMouseDown(Control.ModifierKeys == Keys.Shift,
                                    Control.ModifierKeys == Keys.Control);
                            }
                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPopMatrix();

                            this.isDrawQuad = true;
                        }
                        break;
                    }
                case UIMode.BodyNodeEdit:
                    {
                        this.cal2D();
                    }
                    break;
                case UIMode.PartPick:
                    {
                        if (this._currModel != null)
                        {
                            Matrix4d m = this.arcBall.getTransformMatrix(this.nPointPerspective) * this._currModelTransformMatrix;
                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPushMatrix();
                            Gl.glMultMatrixd(m.Transpose().ToArray());

                            this.selectMouseMove((int)this.currUIMode, null, false);

                            Gl.glMatrixMode(Gl.GL_MODELVIEW);
                            Gl.glPopMatrix();

                            this.isDrawQuad = true;
                        }
                        break;
                    }
                case UIMode.Translate:
                case UIMode.Contact:
                    {
                        this.editMouseDown(1, this.mouseDownPos);
                    }
                    break;
                case UIMode.Scale:
                    {
                        this.editMouseDown(2, this.mouseDownPos);
                    }
                    break;
                case UIMode.Rotate:
                    {
                        this.editMouseDown(3, this.mouseDownPos);
                    }
                    break;
                case UIMode.Viewing:
                default:
                    {
                        this.viewMouseDown(e);
                        break;
                    }
            }
            this.Refresh();
        }// OnMouseDown

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            this.prevMousePos = this.currMousePos;
            this.currMousePos = new Vector2d(e.X, e.Y);

            switch (this.currUIMode)
            {
                case UIMode.VertexSelection:
                case UIMode.EdgeSelection:
                case UIMode.FaceSelection:
                    {
                        if (this.currMeshClass != null && this.isMouseDown)
                        {
                            this.highlightQuad = new Quad2d(this.mouseDownPos, this.currMousePos);
                            this.currMeshClass.selectMouseMove((int)this.currUIMode, this.highlightQuad);
                            this.isDrawQuad = true;
                            this.Refresh();
                        }
                        break;
                    }
                case UIMode.BoxSelection:
                    {
                        if (this._currModel != null && this.isMouseDown)
                        {
                            this.highlightQuad = new Quad2d(this.mouseDownPos, this.currMousePos);
                            //this.selectMouseMove((int)this.currUIMode, this.highlightQuad,
                            //    Control.ModifierKeys == Keys.Control);
                            this.isDrawQuad = true;
                            this.Refresh();
                        }
                        break;
                    }
                case UIMode.PartPick:
                    {
                        if (this._currModel != null)
                        {
                            this.selectPartByUser(currMousePos);
                            this.Refresh();
                        }
                        break;
                    }
                case UIMode.BodyNodeEdit:
                    {
                        if (this.isMouseDown)
                        {
                            this.EditBodyNode(this.currMousePos);
                        }
                        else
                        {
                            this.SelectBodyNode(this.currMousePos);
                        }
                    }
                    this.Refresh();
                    break;
                case UIMode.Translate:
                case UIMode.Scale:
                case UIMode.Rotate:
                    {
                        if (_currModel != null)
                        {
                            if (this.isMouseDown)
                            {
                                this.transformSelections(this.currMousePos);
                            }
                            else
                            {
                                this.selectAxisWhileMouseMoving(this.currMousePos);
                            }
                            this.Refresh();
                        }
                    }
                    break;
                case UIMode.Contact:
                    {
                        if (_currModel != null && _currModel._GRAPH != null)
                        {
                            if (this.isMouseDown)
                            {
                                this.moveContactPoint(this.currMousePos);
                            }
                            else
                            {
                                this.selectContactPoint(currMousePos);
                                this.selectAxisWhileMouseMoving(this.currMousePos);
                            }
                            this.Refresh();
                        }
                    }
                    break;
                case UIMode.Viewing:
                    //default:
                    {
                        if (!this.lockView)
                        {
                            this.viewMouseMove(e.X, e.Y);
                            this.Refresh();
                            this.refreshModelViewers();
                        }
                    }
                    break;
            }
        }// OnMouseMove

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            this.prevMousePos = this.currMousePos;
            this.currMousePos = new Vector2d(e.X, e.Y);
            this.isMouseDown = false;

            switch (this.currUIMode)
            {
                case UIMode.VertexSelection:
                case UIMode.EdgeSelection:
                case UIMode.FaceSelection:
                    {
                        this.isDrawQuad = false;
                        if (this.currMeshClass != null)
                        {
                            this.currMeshClass.selectMouseUp();
                            // test
                            List<int> ids = this.currMeshClass.getSelectedFaces();
                            if (_currModel != null && _currModel._SP != null)
                            {
                                _currModel.pointsTest = new List<Vector3d>();

                                foreach (int fid in ids)
                                {
                                    if (!_currModel._SP._fidxMapSPid.ContainsKey(fid))
                                    {
                                        // no sample point on this face
                                        continue;
                                    }
                                    List<int> spidxs = _currModel._SP._fidxMapSPid[fid];
                                    foreach (int spid in spidxs)
                                    {
                                        _currModel.pointsTest.Add(_currModel._SP._points[spid]);
                                    }
                                }
                            }
                        }
                        //this.Refresh();
                        break;
                    }
                case UIMode.BoxSelection:
                    {
                        this.isDrawQuad = false;
                        if (this._currModel != null && e.Button != System.Windows.Forms.MouseButtons.Right)
                        {
                            this.selectMouseUp(this.highlightQuad,
                                Control.ModifierKeys == Keys.Shift,
                                Control.ModifierKeys == Keys.Control);
                        }
                        break;
                    }
                case UIMode.BodyNodeEdit:
                case UIMode.Translate:
                case UIMode.Scale:
                case UIMode.Rotate:
                    {
                        this.editMouseUp();
                        this.Refresh();
                    }
                    break;
                case UIMode.Contact:
                    {
                        //_selectedEdge = null;
                        this.moveContactUp();
                        this.Refresh();
                    }
                    break;
                case UIMode.Viewing:
                default:
                    {
                        this.viewMouseUp();
                        this.refreshModelViewers();
                        break;
                    }
            }
            this.Refresh();
        }// OnMouseUp

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // to get the correct 2d info of the points3d
            //this.Refresh();
            this.cal2D();
            this.Refresh();
        }

        public void selectMouseDown(bool isShift, bool isCtrl)
        {
            switch (this.currUIMode)
            {
                case UIMode.PartPick:
                    {
                        break;
                    }
                default:
                    break;
            }
        }

        public void selectMouseMove(int mode, Quad2d q, bool isCtrl)
        {
        }

        public void selectMouseUp(Quad2d q, bool isShift, bool isCtrl)
        {
            switch (this.currUIMode)
            {
                case UIMode.BoxSelection:
                    {
                        if (!isShift && !isCtrl)
                        {
                            _selectedParts = new List<Part>();
                        }
                        this.selectBbox(q, isCtrl);
                        Program.GetFormMain().updateStats();
                        break;
                    }
                default:
                    break;
            }
            this.isDrawQuad = false;
        }

        public void acceptKeyData(KeyEventArgs e)
        {
            SendKeys.Send(e.KeyData.ToString());
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                    return true;
                case Keys.Shift | Keys.Right:
                case Keys.Shift | Keys.Left:
                case Keys.Shift | Keys.Up:
                case Keys.Shift | Keys.Down:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control == true && e.KeyCode == Keys.C)
            {
                this.clearContext();
                this.Refresh();
                return;
            }
            switch (e.KeyData)
            {
                case System.Windows.Forms.Keys.V:
                    {
                        this.currUIMode = UIMode.Viewing;
                        break;
                    }
                case Keys.R:
                    {
                        this.reloadView();
                        break;
                    }
                case Keys.I:
                    {
                        this.resetView(); // Identity
                        break;
                    }
                case Keys.B:
                    {
                        this.currUIMode = UIMode.BodyNodeEdit;
                        this.cal2D();
                        break;
                    }
                case Keys.S:
                    {
                        this.currUIMode = UIMode.BoxSelection;
                        break;
                    }
                case Keys.C:
                    {
                        _showContactPoint = !_showContactPoint;
                        if (this._showContactPoint)
                        {
                            this.setUIMode(9); // contact
                        }
                        else
                        {
                            this.setUIMode(0);
                        }
                        break;
                    }
                case Keys.P:
                    {
                        this.currUIMode = UIMode.PartPick;
                        break;
                    }
                case Keys.Space:
                    {
                        this.lockView = !this.lockView;
                        break;
                    }
                case Keys.PageDown:
                case Keys.Right:
                    {
                        if (!e.Shift)
                        {
                        }
                        break;
                    }
                case Keys.PageUp:
                case Keys.Left:
                    {
                        break;
                    }
                case Keys.Delete:
                    {
                        this.deleteParts();
                        break;
                    }
                default:
                    break;
            }
            this.Refresh();
        }// OnKeyDown        

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
        }//OnMouseWheel

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            this.MakeCurrent();

            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

            this.Draw3D();
            this.Draw2D();

            this.SwapBuffers();

        }// onPaint

        //########## Functions ##########//
        private List<Functionality.Functions> _prevUserFunctions = new List<Functionality.Functions>();
        private List<Functionality.Functions> _currUserFunctions = new List<Functionality.Functions>();
        public void editFunctions(string fs, bool selected)
        {
            Functionality.Functions fs_func = Functionality.getFunction(fs);
            if (!selected && _currUserFunctions.Contains(fs_func))
            {
                this.removeAFunction(fs_func);
                _currUserFunctions.Remove(fs_func);
            }
            if (selected && !_currUserFunctions.Contains(fs_func))
            {
                this.addAFunction(fs_func);
                _currUserFunctions.Add(fs_func);
            }
        }// editFunctions

        private void checkInUserSelectedFunctions()
        {
            List<string> res = Program.GetFormMain().getUserSelectedFunctions();
            _currUserFunctions.Clear();
            foreach (string s in res)
            {
                Functionality.Functions f = Functionality.getFunction(s);
                _currUserFunctions.Add(f);
            }
        }// checkInUserSelectedFunctions

        private void addAFunction(Functionality.Functions func)
        {
        }// addAFunction

        private void removeAFunction(Functionality.Functions func)
        {
        }// removeAFunction

        private PartFormation tryCreateANewPartFormation(List<Part> parts, double rate)
        {
            // if there exist a subset, do not store and do not perform this operation either
            if (this.partNameToInteger == null || this.partCombinationMemory == null)
            {
                return null;
            }
            List<int> cur = new List<int>();
            foreach (Part p in parts)
            {
                int id = -1;
                if (this.partNameToInteger.TryGetValue(p._partName, out id))
                {
                    cur.Add(id);
                }
                else
                {
                    MessageBox.Show("Part name map error: " + p._partName);
                    return null;
                }
            }
            cur.Sort();
            PartFormation partForm = new PartFormation(cur, rate);
            int n = parts.Count;
            if (n >= this.partCombinationMemory.Length)
            {
                return null;
            }
            List<PartFormation> iSet = this.partCombinationMemory[n];
            if (iSet == null)
            {
                this.partCombinationMemory[n] = new List<PartFormation>();
                iSet = this.partCombinationMemory[n];
            }
            else
            {
                foreach (PartFormation pf in iSet)
                {
                    List<int> idxs = pf.getPartIdxs();
                    var sub = cur.Except(idxs);
                    if (sub.Count() == 0)
                    {
                        // exist
                        return null;
                    }
                }
            }
            this.partCombinationMemory[n].Add(partForm);
            return partForm;
        }// tryCreateANewPartFormation

        bool useSimFilter = false;
        Dictionary<PartGroup, int> pgSimMatrixMap;
        double[,] pgSimMatrix;

        public void autoRunTest()
        {
            // compute part similarity
            //computePartSimExternally(_ancesterModels);

            int maxIter = 1;
            //crossedPairNames = new HashSet<string>();
            //autoRunTestWithOrWithoutFilter(true, maxIter);
            //registerANewUser();
            //_currGenId = 1;
            crossedPairNames = new HashSet<string>();
            autoRunTestWithOrWithoutFilter(false, maxIter);
            crossedPairNames = new HashSet<string>();
        }

        private void autoRunTestWithOrWithoutFilter(bool withFilter, int maxIter)
        {
            useSimFilter = withFilter;
            int iter = 1;
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            int nTotalModels = 0;
            while (iter <= maxIter)
            {
                Console.WriteLine("\nIteration " + iter + ": ");
                runByUserSelection();
                if (_currGenModelViewers == null || _currGenModelViewers.Count == 0)
                {
                    break;
                }
                nTotalModels += _currGenModelViewers.Count;
                // choose all models as source
                foreach (ModelViewer mv in _currGenModelViewers)
                {
                    mv.selectCurrent();
                }
                ++iter;
            }
            long secs = stopWatch.ElapsedMilliseconds / 1000;
            stopWatch.Stop();
            // stats
            string statsName = userFolder + "\\stats_" + (withFilter ? "withFilter" : "withoutFilter") + ".txt";
            using (StreamWriter sw = new StreamWriter(statsName))
            {
                sw.WriteLine("#Iteration: " + iter.ToString());
                sw.WriteLine("#Time: " + secs.ToString() + " seconds.");
                sw.WriteLine("#Total models: " + nTotalModels.ToString());
            }
        }// autoRunTestWithOrWithoutFilter

        HashSet<string> crossedPairNames = new HashSet<string>();
        int nTotalCandidate = 0;
        public List<ModelViewer> runByUserSelection()
        {
            // evolve the current model
            if (_currModel == null || _ancesterModels.Count == 0)
            {
                return null;
            }
            if (!Directory.Exists(userFolder))
            {
                this.registerANewUser();
            }
            this.checkInUserSelectedFunctions();

            this._showContactPoint = true;
            this.decideWhichToDraw(true, false, false, true, false, false);
            List<Model> targetMoels = new List<Model>(_userSelectedModels);
            List<Model> sourceModels = new List<Model>();
            foreach (Model m in _ancesterModels)
            {
                if (!targetMoels.Contains(m))
                {
                    sourceModels.Add(m);
                }
            }
            // more iterations
            _userSelectedModels.Clear();
            foreach (ModelViewer mv in _currGenModelViewers)
            {
                if (!mv.isSelected())
                {
                    continue;
                }
                Model m = mv._MODEL;
                m._GRAPH.initializePartGroups();
                sourceModels.Add(m);
                _userSelectedModels.Add(m);
            }

            // store user selections
            this.saveUserSelections(_currGenId);
            ++_currGenId;

            // target functions
            this.isUserTargeted = targetMoels.Count == 1;
            if (targetMoels.Count == 0)
            {
                targetMoels = sourceModels;
            }
            List<Model> candidates = new List<Model>();
            if (useSimFilter)
            {
                computePartGroupSimilarity(targetMoels);
                //computePGSimExternally(targetMoels);
            }

            this.loadTrainedInfo();

            Dictionary<string, int[]> nameMap = new Dictionary<string, int[]>();
            foreach (Model m1 in targetMoels)
            {
                int[] idx = new int[1];
                string originalName = getOriginalModelName(m1._model_name);
                if (!nameMap.ContainsKey(originalName))
                {
                    nameMap.Add(originalName, idx);
                }
                nameMap.TryGetValue(originalName, out idx);
                List<Model> otherModels = new List<Model>(sourceModels);
                foreach (Model m in targetMoels)
                {
                    if (m1 != m && !otherModels.Contains(m))
                    {
                        otherModels.Add(m);
                    }
                }
                foreach (Model m2 in otherModels)
                {
                    if (m1 == m2)
                    {
                        continue;
                    }
                    string c1 = m1._model_name + m2._model_name;
                    string c2 = m2._model_name + m1._model_name;
                    if (crossedPairNames.Contains(c1))// || crossedPairNames.Contains(c2))
                    {
                        continue;
                    }
                    crossedPairNames.Add(c1);
                    if (isUserTargeted)
                    {
                        m2._GRAPH._partGroups.Add(new PartGroup(m2._GRAPH._NODES, 0));
                    }
                    List<Model> res = this.tryCrossOverTwoModelsWithFunctionalConstraints(m1, m2, idx);
                    foreach (Model m in res)
                    {
                        double[,] scores_probs = partialMatching(m, true);
                        double[,] categories_cores_probs = new double[Functionality._NUM_CATEGORIY, 5];
                        Console.WriteLine(m._model_name + " partial matching scores and probs: ");
                        for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
                        {
                            categories_cores_probs[i, 0] = i;
                            categories_cores_probs[i, 1] = scores_probs[i, 0];
                            categories_cores_probs[i, 2] = scores_probs[i, 1];
                            categories_cores_probs[i, 3] = scores_probs[i, 2];
                            categories_cores_probs[i, 4] = scores_probs[i, 3];
                        }
                        categories_cores_probs = categories_cores_probs.OrderByDescending(x => x[4]);
                        for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
                        {
                            Console.WriteLine(i+1 + ". " + Functionality.getCategoryName((int)categories_cores_probs[i, 0]) + ": "
                                + categories_cores_probs[i, 1] + ", "
                                + categories_cores_probs[i, 2] + ", "
                                + categories_cores_probs[i, 3] + ", "
                                + categories_cores_probs[i, 4]);
                        }
                        m._CAT = Functionality.getCategory(Functionality.getCategoryName((int)categories_cores_probs[0, 0]));
                        string scoreDir = m._path + "\\" + m._model_name + "\\" + m._model_name + ".score";
                        saveScoreFile(scoreDir, Enumerable.Range(0, scores_probs.GetLength(0)).Select(x => scores_probs[x, 3]).ToArray());
                    }
                    candidates.AddRange(res);
                    Console.WriteLine("The number of candidates: " + candidates.Count.ToString());
                    int pgid = m2._GRAPH._partGroups.Count - 1;
                    if (isUserTargeted)
                    {
                        m2._GRAPH._partGroups.RemoveAt(pgid);
                    }
                }
            }
            foreach (ModelViewer mv in _currGenModelViewers)
            {
                mv.unSelect();
                _userSelectedModels.Remove(mv._MODEL);
            }
            // keep distinct models
            List<Model> selected = null;
            if (_currGenId > 1)
            {
                selected = LFD(candidates);
                //selected = candidates;
            }
            if (selected == null || selected.Count == 0)
            {
                selected = candidates;
            }
            _currGenModelViewers.Clear();
            for (int i = 0; i < selected.Count; ++i)
            {
                Model imodel = selected[i];
                _currGenModelViewers.Add(new ModelViewer(imodel, imodel._index, this, _currGenId));
            }
            return _currGenModelViewers;
        }// runByUserSelection

        private bool isUserTargeted = false;
        // a set of partGroup: pgs that have similar partial geometries
        // whenever replacing a pair of part groups, check if a similar pair has already been used
        private Dictionary<int, List<int>> crossOverDict;
        private List<Model> tryCrossOverTwoModelsWithFunctionalConstraints(Model m1, Model m2, int[] idx)
        {
            List<Model> res = new List<Model>();
            // user selected functions
            var sub = _currUserFunctions.Except(m1._GRAPH.collectAllDistinceFunctions());
            List<Functionality.Functions> lackedFuncs = sub.ToList();
            // degenerate to part replacement
            if (lackedFuncs.Count() == 0)
            {
                // as long as the original functions are preserved
                List<PartGroup> pgs_1 = m1._GRAPH._partGroups;
                List<PartGroup> pgs_2 = m2._GRAPH._partGroups;
                for (int i = 0; i < pgs_1.Count; ++i)
                {
                    List<Functionality.Functions> funcs1 = Functionality.getNodesFunctionalities(pgs_1[i]._NODES);
                    int id1 = -1;
                    if (pgSimMatrixMap != null)
                    {
                        pgSimMatrixMap.TryGetValue(pgs_1[i], out id1);
                    }
                    for (int j = 0; j < pgs_2.Count; ++j)
                    {
                        if (useSimFilter && pgSimMatrixMap != null)
                        {
                            int id2 = -1;
                            double thr = 0.99;
                            pgSimMatrixMap.TryGetValue(pgs_2[j], out id2);
                            if (id1 != -1 && id2 != -1 && pgSimMatrix[id1, id2] > thr && pgSimMatrix[id2, id1] > thr)
                            {
                                continue;
                            }
                        }
                        List<Functionality.Functions> funcs2 = Functionality.getNodesFunctionalities(pgs_2[j]._NODES);
                        //if (m1._model_name.StartsWith("swing") && funcs1.Count == 0 && funcs2.Count > 0 && funcs2.Contains(Functionality.Functions.HAND_HOLD))
                        if (funcs1.Count == 0 && funcs2.Count > 0 && _currGenId < 3 &&
                            (Functionality.ContainsMainFunction(funcs2) || Functionality.ContainsSecondaryFunction(funcs2)))
                            
                        {
                            // special case for comparison to TVCG paper
                            Model mCloned = m1.Clone() as Model;
                            mCloned._path = crossoverFolder + "gen_" +_currGenId.ToString() + "\\";
                            if (!Directory.Exists(mCloned._path))
                            {
                                Directory.CreateDirectory(mCloned._path);
                            }
                            mCloned._model_name = m1._model_name + "-gen_" + _currGenId.ToString() + "_num_" + idx[0].ToString();
                            List<Node> newNodes = insertionOperation(mCloned, m2, pgs_2[j]._NODES, funcs2.Contains(Functionality.Functions.HAND_HOLD));
                            if (newNodes == null)
                            {
                                continue;
                            }
                            mCloned.replaceNodes(new List<Node>(), newNodes);
                            // topology
                            //mCloned._GRAPH.replaceNodes(new List<Node>(), newNodes);
                            PartFormation pf = this.tryCreateANewPartFormation(mCloned._PARTS, 0);
                            if (pf != null)
                            {
                                if (this.processAnOffspringModel(mCloned))
                                {
                                    pf._RATE = 1.0;
                                    mCloned._partForm = pf;
                                    mCloned._GRAPH._functionalityValues = new FunctionalityFeatures();
                                    mCloned._GRAPH._functionalityValues.addParentCategories(new List<Functionality.Category> { m1._CAT, m2._CAT });
                                    mCloned._GRAPH._functionalityValues.addParentCategories(m1._GRAPH._functionalityValues._parentCategories);
                                    mCloned._GRAPH._functionalityValues.addParentCategories(m2._GRAPH._functionalityValues._parentCategories);
                                    res.Add(mCloned);
                                    ++idx[0];
                                }
                            }
                            continue;
                        }
                        if (!Functionality.hasCompatibleFunctions(funcs1, funcs2) 
                            || Functionality.isTrivialReplace(pgs_1[i]._NODES, pgs_2[j]._NODES))
                        {
                            continue;
                        }
                        if (pgs_2[j]._NODES.Count == m2._GRAPH._NODES.Count && 
                            !funcs1.Contains(Functionality.Functions.GROUND_TOUCHING))
                        {
                            continue;
                        }
                        Model m1_cloned = this.crossOverTwoModelsWithFunctionalConstraints(m1, m2, pgs_1[i]._NODES, pgs_2[j]._NODES, idx[0]);
                        ++idx[0];
                        if (m1_cloned != null)
                        {
                            m1_cloned._GRAPH._functionalityValues = new FunctionalityFeatures();
                            m1_cloned._GRAPH._functionalityValues.addParentCategories(new List<Functionality.Category> { m1._CAT, m2._CAT });
                            m1_cloned._GRAPH._functionalityValues.addParentCategories(m1._GRAPH._functionalityValues._parentCategories);
                            m1_cloned._GRAPH._functionalityValues.addParentCategories(m2._GRAPH._functionalityValues._parentCategories);
                            res.Add(m1_cloned);
                        }
                    }
                }
                return res;
            }

            List<Functionality.Functions> oriFuncs = m1._GRAPH.collectMainFunctions();
            // 1. add funs
            List<Model> prevLevels = new List<Model>();
            prevLevels.Add(m1);
            //List<Node> mainNodes = m1._GRAPH.getMainNodes();
            int nlevel = 0;
            foreach (Functionality.Functions f in lackedFuncs)
            {
                PartGroup[] pgs = this.findAPairPartGroupsWithFunctionalConstraints(m1._GRAPH, m2._GRAPH, f);
                if (pgs == null)
                {
                    ++nlevel;
                    continue;
                }
                List<Model> currLevels = new List<Model>();
                foreach (Model m in prevLevels)
                {
                    Model mCloned = m.Clone() as Model;
                    mCloned._path = crossoverFolder + "gen_" +_currGenId.ToString() + "\\";
                    if (!Directory.Exists(mCloned._path))
                    {
                        Directory.CreateDirectory(mCloned._path);
                    }
                    mCloned._model_name = m1._model_name + "-gen_" + _currGenId.ToString() + "_num_" + idx[0].ToString();
                    bool useReplace = pgs[0]._NODES.Count > 0 && nlevel == 0;
                    List<Node> supportNodes = mCloned._GRAPH.getNodesByFunctionality(Functionality.Functions.SUPPORT);
                    if ((Functionality.isRollableFunction(f) || f == Functionality.Functions.PLACEMENT) && supportNodes.Count == 0)
                    {
                        useReplace = false;
                    }
                    if (useReplace)
                    {
                        List<Node> updateNodes1 = new List<Node>();
                        foreach (Node node in pgs[0]._NODES)
                        {
                            updateNodes1.Add(mCloned._GRAPH._NODES[node._INDEX]);
                        }
                        List<Node> updateNodes2 = this.replaceNodes(mCloned._GRAPH, m2._GRAPH, updateNodes1, pgs[1]._NODES);
                        mCloned.replaceNodes(updateNodes1, updateNodes2);
                        mCloned._GRAPH.replaceNodes(updateNodes1, updateNodes2);
                        mCloned.nNewNodes = updateNodes2.Count;
                    }
                    else
                    {
                        List<Node> groundNodes = mCloned._GRAPH.getNodesByFunctionality(Functionality.Functions.GROUND_TOUCHING);
                        List<Node> groundNodesDep = mCloned._GRAPH.getNodesAndDependentsByFunctionality(Functionality.Functions.GROUND_TOUCHING);
                        List<Node> toReplace = pgs[1]._NODES;
                        if (Functionality.isRollableFunction(f))
                        {
                            List<Vector3d> targets = new List<Vector3d>();
                            // find the ground touching points,
                            // the box center is not working, e.g., two crossed legs have the same centers
                            foreach (Node gn in groundNodes)
                            {
                                Vector3d[] groundPoints = this.getGroundTouchingPoints(gn, groundNodes.Count == 2 ? 2 : 1);
                                if (groundPoints == null)
                                {
                                    continue;
                                }
                                for (int i = 0; i < groundPoints.Length; ++i)
                                {
                                    targets.Add(groundPoints[i]);
                                }
                            }
                            if (targets.Count == 0)
                            {
                                continue;
                            }
                            List<Node> updateNodes2 = this.replaceNodes(mCloned._GRAPH, m2._GRAPH, targets, toReplace);
                            mCloned.replaceNodes(new List<Node>(), updateNodes2);
                            // topology
                            mCloned._GRAPH.addSubGraph(groundNodesDep, updateNodes2);
                            mCloned.nNewNodes = updateNodes2.Count;
                            foreach (Node node in groundNodes)
                            {
                                node._funcs.Remove(Functionality.Functions.GROUND_TOUCHING);
                                node.addFunctionality(Functionality.Functions.SUPPORT);
                            }
                        }// rolling
                        else if (f == Functionality.Functions.PLACEMENT)
                        {
                            if (supportNodes.Count == 0)
                            {
                                supportNodes = groundNodes;
                            }
                            this.insertPlacement(mCloned, supportNodes, toReplace);
                        }
                    }
                    PartFormation pf = this.tryCreateANewPartFormation(mCloned._PARTS, 0);
                    if (pf != null)
                    {
                        bool isValid = this.processAnOffspringModel(mCloned);
                        pf._RATE = 1.0;
                        if (isValid)
                        {
                            mCloned._partForm = pf;
                            mCloned._GRAPH._functionalityValues = new FunctionalityFeatures();
                            mCloned._GRAPH._functionalityValues.addParentCategories(new List<Functionality.Category> { m1._CAT, m2._CAT });
                            mCloned._GRAPH._functionalityValues.addParentCategories(m1._GRAPH._functionalityValues._parentCategories);
                            mCloned._GRAPH._functionalityValues.addParentCategories(m2._GRAPH._functionalityValues._parentCategories);
                            res.Add(mCloned);
                            currLevels.Add(mCloned);
                        }
                    }
                    ++idx[0];
                }
                prevLevels.Clear();
                prevLevels.Add(m1);
                prevLevels.AddRange(currLevels);
                currLevels.Clear();
                ++nlevel;
            }
            return res;
        }// tryCrossOverTwoModelsWithFunctionalConstraints

        private Vector3d[] getGroundTouchingPoints(Node node, int n)
        {
            if (!node._isGroundTouching || n <= 0)
            {
                return null;
            }
            Vector3d[] res = new Vector3d[n];
            List<Vector3d> pnts = new List<Vector3d>();
            List<Vector3d> points = node._PART._MESH.VertexVectorArray.ToList();
            if (node._PART._partSP != null && node._PART._partSP._points != null)
                //&& node._PART._partSP._points.Length > points.Count)
            {
                points.AddRange(node._PART._partSP._points);
                //points = node._PART._partSP._points.ToList();
            }
            Vector3d center = new Vector3d();
            Vector3d minCoord = Vector3d.MaxCoord;
            Vector3d maxCoord = Vector3d.MinCoord;
            foreach (Vector3d v in points)
            {
                if (Math.Abs(v.y) < Common._magic_thresh)
                {
                    pnts.Add(v);
                    center += v;
                    minCoord = Vector3d.Min(minCoord, v);
                    maxCoord = Vector3d.Max(maxCoord, v);
                }
            }
            if (pnts.Count == 0)
            {
                return null;
            }
            center /= pnts.Count;
            center.y = 0;
            if (n == 1)
            {
                res[0] = center;
            }
            else if (n == 2)
            {
                res[0] = new Vector3d(center.x, 0, minCoord.z);
                res[1] = new Vector3d(center.x, 0, maxCoord.z);
            }
            return res;
        }// getGroundTouchingPoints

        private void insertPlacement(Model m, List<Node> supportNodes, List<Node> toReplace)
        {
            // check how many planks can be inserted
            double height = 0;
            double start = 0;
            Vector3d center = new Vector3d();
            foreach (Node gn in supportNodes)
            {
                double h = gn._PART._BOUNDINGBOX.MaxCoord.y - gn._PART._BOUNDINGBOX.MinCoord.y;
                height = h > height ? h : height;
                center += gn._PART._BOUNDINGBOX.CENTER;
                start = gn._PART._BOUNDINGBOX.MinCoord.y > start ? gn._PART._BOUNDINGBOX.MinCoord.y : start;
            }
            center /= supportNodes.Count;
            int nplacement = (int)(height / Common._min_shelf_interval);
            nplacement = nplacement > toReplace.Count ? toReplace.Count : nplacement;
            double hinterv = height / (nplacement + 1);
            List<Node> left = new List<Node>();
            List<Node> right = new List<Node>();
            double x1 = double.MaxValue;
            double x2 = double.MinValue;
            double z1 = double.MaxValue;
            double z2 = double.MinValue;
            foreach (Node node in supportNodes)
            {
                Vector3d c = node._PART._BOUNDINGBOX.CENTER;
                if (c.x < center.x)
                {
                    left.Add(node);
                    //x1 = x1 < c.x ? x1 : c.x;
                    x1 = x1 < node._PART._MESH.MaxCoord.x ? x1 : node._PART._MESH.MaxCoord.x;
                }
                else
                {
                    right.Add(node);
                    //x2 = x2 > c.x ? x2 : c.x;
                    x2 = x2 > node._PART._MESH.MinCoord.x ? x2 : node._PART._MESH.MinCoord.x;
                }
                z1 = node._PART._BOUNDINGBOX.MinCoord.z < z1 ? node._PART._BOUNDINGBOX.MinCoord.z : z1;
                z2 = node._PART._BOUNDINGBOX.MaxCoord.z > z2 ? node._PART._BOUNDINGBOX.MaxCoord.z : z2;
            }
            List<Node> insertion = new List<Node>();
            for (int i = 0; i < nplacement; ++i)
            {
                double hpos = start + (i + 1) * hinterv;
                Node toInsert = toReplace[i].Clone() as Node;
                Vector3d newScale = new Vector3d(x2 - x1,
                    Math.Min(toInsert._PART._BOUNDINGBOX.MaxCoord.y - toInsert._PART._BOUNDINGBOX.MinCoord.y, Common._min_shelf_interval/2),
                    z2 - z1);
                Vector3d scale = newScale / (toInsert._PART._BOUNDINGBOX.MaxCoord - toInsert._PART._BOUNDINGBOX.MinCoord);
                Vector3d newCenter = new Vector3d((x1 + x2) / 2, hpos, (z1 + z2) / 2);
                Matrix4d S = Matrix4d.ScalingMatrix(scale);
                Matrix4d Q = Matrix4d.TranslationMatrix(newCenter) * S * Matrix4d.TranslationMatrix(new Vector3d() - toInsert._PART._BOUNDINGBOX.CENTER);
                toInsert.Transform(Q);
                insertion.Add(toInsert);
                if (left.Count == 1)
                {
                    // one edge, two contacts
                    List<Contact> contacts = new List<Contact>();
                    contacts.Add(new Contact(new Vector3d(x1, hpos, z1)));
                    contacts.Add(new Contact(new Vector3d(x1, hpos, z2)));
                    m._GRAPH.addAnEdge(left[0], toInsert, contacts);
                }
                else
                {
                    foreach (Node ln in left)
                    {
                        Vector3d contact = new Vector3d(x1, hpos, ln._PART._BOUNDINGBOX.CENTER.z);
                        m._GRAPH.addAnEdge(ln, toInsert, contact);
                    }
                }
                if (right.Count == 1)
                {
                    // one edge, two contacts
                    List<Contact> contacts = new List<Contact>();
                    contacts.Add(new Contact(new Vector3d(x2, hpos, z1)));
                    contacts.Add(new Contact(new Vector3d(x2, hpos, z2)));
                    m._GRAPH.addAnEdge(right[0], toInsert, contacts);
                }
                else
                {
                    foreach (Node rn in right)
                    {
                        Vector3d contact = new Vector3d(x2, hpos, rn._PART._BOUNDINGBOX.CENTER.z);
                        m._GRAPH.addAnEdge(rn, toInsert, contact);
                    }
                }
            }
            m.replaceNodes(new List<Node>(), insertion);
            foreach (Node node in insertion)
            {
                m._GRAPH.addANode(node);
            }
            m._GRAPH.adjustContacts();
        }// insertPlacement

        private string getOriginalModelName(string name)
        {
            int slashId = name.IndexOf('-');
            if (slashId == -1)
            {
                slashId = name.Length;
            }
            string originalModelName = name.Substring(0, slashId);
            return originalModelName;
        }

        private Model crossOverTwoModelsWithFunctionalConstraints(Model m1, Model m2, List<Node> nodes1, List<Node> nodes2, int idx)
        {
            // check if such combination already exists
            List<Part> parts = new List<Part>();
            foreach (Node node in m1._GRAPH._NODES)
            {
                if (!nodes1.Contains(node))
                {
                    parts.Add(node._PART);
                }
            }
            foreach (Node node in nodes2)
            {
                parts.Add(node._PART);
            }
            PartFormation pf = this.tryCreateANewPartFormation(parts, 0);
            if (pf == null)
            {
                return null;
            }

            this.setSelectedNodes(m1, nodes1);
            this.setSelectedNodes(m2, nodes2);
            Model newModel = m1.Clone() as Model;
            // m1 starts name
            string name = m1._model_name;
            string originalModelName = getOriginalModelName(name);
            newModel._path = crossoverFolder + "gen_" + _currGenId.ToString() + "\\";
            if (!Directory.Exists(newModel._path))
            {
                Directory.CreateDirectory(newModel._path);
            }
            List<Node> updateNodes1 = new List<Node>();
            List<Node> updatedNodes2 = new List<Node>();
            List<Model> parents = new List<Model>(); // to set parent names
            parents.Add(m1);
            parents.Add(m2);
            newModel.setParentNames(parents);
            foreach (Node node in nodes1)
            {
                updateNodes1.Add(newModel._GRAPH._NODES[node._INDEX]);
            }
            newModel._model_name = originalModelName + "-gen_" + _currGenId.ToString() + "_num_" + idx.ToString();
            List<Node> reduced = reduceRepeatedNodes(new List<Node>(updateNodes1), new List<Node>(nodes2));
            this.setSelectedNodes(m2, reduced);
            // replace
            if (nodes2.Count == m2._GRAPH._NNodes)
            {
                updatedNodes2 = this.replaceWithAFullGraph(newModel._GRAPH, m2._GRAPH, updateNodes1, reduced);
            }
            else
            {
                updatedNodes2 = this.replaceNodes(newModel._GRAPH, m2._GRAPH, updateNodes1, reduced);
            }
            newModel.replaceNodes(updateNodes1, updatedNodes2);
            // topology
            newModel._GRAPH.replaceNodes(updateNodes1, updatedNodes2);
            newModel.nNewNodes = updatedNodes2.Count;

            if (this.processAnOffspringModel(newModel))
            {
                pf._RATE = 1.0;
                newModel._partForm = pf;
                return newModel;
            }
            else
            {
                return null;
            }
        }// crossOverTwoModelsWithFunctionalConstraints

        private List<Node> replaceWithAFullGraph(Graph g1, Graph g2, List<Node> nodes1, List<Node> nodes2)
        {
            // replace nodes1 by nodes2 in g1
            List<Node> updateNodes2 = cloneNodesAndRelations(nodes2);

            List<Edge> edgesToConnect_1 = g1.getOutgoingEdges(nodes1);
            List<Vector3d> targets = collectPoints(edgesToConnect_1);
            Vector3d center_t = new Vector3d();
            Vector3d mint = Vector3d.MaxCoord;
            Vector3d maxt = Vector3d.MinCoord;
            foreach (Vector3d v in targets)
            {
                center_t += v;
                mint = Vector3d.Min(mint, v);
                maxt = Vector3d.Max(maxt, v);
            }
            center_t /= targets.Count;

            List<Node> ground1 = this.getGroundTouchingNode(nodes1);
            List<Node> ground2 = this.getGroundTouchingNode(nodes2);

            Vector3d[] scaleVecs_1 = this.getScales(nodes1);
            Vector3d[] scaleVecs_2 = this.getScales(nodes2);

            Vector3d center1 = scaleVecs_1[0];
            Vector3d maxv_t = scaleVecs_1[1];
            Vector3d minv_t = scaleVecs_1[2];


            Vector3d center2 = scaleVecs_2[0];
            Vector3d maxv_s = scaleVecs_2[1];
            Vector3d minv_s = scaleVecs_2[2];

            double[] scale1 = { 1.0, 1.0, 1.0 };
            if (nodes1.Count > 0)
            {
                scale1[0] = (maxv_t.x - minv_t.x) / (maxv_s.x - minv_s.x);
                scale1[1] = (maxv_t.y - minv_t.y) / (maxv_s.y - minv_s.y);
                scale1[2] = (maxv_t.z - minv_t.z) / (maxv_s.z - minv_s.z);
            }

            Matrix4d T = Matrix4d.IdentityMatrix();
            Matrix4d S = Matrix4d.IdentityMatrix();
            Matrix4d Q = Matrix4d.IdentityMatrix();

            // case 1:
            // if nodes1 contain all ground nodes
            List<Node> ground_g1 = this.getGroundTouchingNode(g1._NODES);
            if (ground_g1.Count == ground1.Count)
            {
                S = Matrix4d.ScalingMatrix((maxt.x - mint.x) / (maxv_s.x - minv_s.x), scale1[1], scale1[2]);
                center1.x = (maxt.x + mint.x) / 2;
                Q = Matrix4d.TranslationMatrix(center1) * S * Matrix4d.TranslationMatrix(new Vector3d() - center2);
                //Vector3d[] simPoints = new Vector3d[2];
                //simPoints[0] = (S * new Vector4d(minv_s, 1)).ToVector3D();
                //simPoints[1] = (S * new Vector4d(maxv_s, 1)).ToVector3D();
                //Q = Matrix4d.TranslationMatrix(center1) * S * 
                //    Matrix4d.TranslationMatrix(new Vector3d() - (simPoints[0] + simPoints[1]) / 2);
            }
            else
            {
                if (center1.x > center_t.x && center1.z > center_t.z) {
                    // middle
                    Vector3d tv = new Vector3d((mint.x + maxt.x) / 2, 0, (maxt.z + mint.z) / 2);
                    Q = Matrix4d.TranslationMatrix(tv) * Matrix4d.ScalingMatrix(scale1[2], 1, scale1[2]) *
                        Matrix4d.TranslationMatrix(new Vector3d() - new Vector3d((minv_s.x + maxv_s.x) / 2, 0, (minv_s.z + maxv_s.z) / 2));
                }
                else if (center1.x > center_t.x )
                {
                    // right
                    Vector3d tv = new Vector3d(mint.x, 0, (maxt.z + mint.z) / 2);
                    Q = Matrix4d.TranslationMatrix(tv) * Matrix4d.ScalingMatrix(scale1[2], 1, scale1[2]) *
                        Matrix4d.TranslationMatrix(new Vector3d() - new Vector3d(minv_s.x, 0, (minv_s.z + maxv_s.z) / 2));
                }
                else
                {
                    Vector3d tv = new Vector3d(maxt.x, 0, (maxt.z + mint.z) / 2);
                    Q = Matrix4d.TranslationMatrix(tv) * Matrix4d.ScalingMatrix(scale1[2], 1, scale1[2]) *
                        Matrix4d.TranslationMatrix(new Vector3d() - new Vector3d(maxv_s.x, 0, (minv_s.z + maxv_s.z) / 2));

                }
            }
            this.deformNodesAndEdges(updateNodes2, Q);
            g1.resetUpdateStatus();
            this.restoreCyclinderNodes(updateNodes2, S);

            return updateNodes2;
        }// replaceWithAFullGraph

        private bool processAnOffspringModel(Model m)
        {
            // screenshot
            m.unify();
            m.composeMesh();

            if (!m._GRAPH.isValid())
            {
                m._model_name += "_invalid";
                //this.setCurrentModel(m, -1);
                //Program.GetFormMain().updateStats();
                //this.captureScreen(imageFolder_c + "invald\\" + m._model_name + ".png");
                //saveAPartBasedModel(m, m._path + m._model_name + ".pam", false);
                return false;
            }
            this.setCurrentModel(m, -1);
            Program.GetFormMain().updateStats();
            // valid graph
            this.captureScreen(imageFolder_c + m._model_name + ".png");
            saveAPartBasedModel(m, m._path + m._model_name + ".pam", false);
            m._index = _modelIndex;
            ++_modelIndex;
            return true;
        }// processAnOffspringModel

        private PartGroup[] findAPairPartGroupsWithFunctionalConstraints(Graph g1, Graph g2, Functionality.Functions f)
        {
            PartGroup[] res = null;
            List<Functionality.Functions> allFuncs = g1.collectAllDistinceFunctions();
            foreach (PartGroup pg1 in g1._partGroups)
            {
                List<Node> nodes = new List<Node>(g1._NODES);
                foreach (Node node in pg1._NODES)
                {
                    nodes.Remove(node);
                }
                List<Functionality.Functions> afterRemovePg1 = Functionality.getNodesFunctionalities(nodes);
                List<Functionality.Functions> funcs1 = Functionality.getNodesFunctionalities(pg1._NODES);
                foreach (PartGroup pg2 in g2._partGroups)
                {
                    List<Functionality.Functions> funcs2 = Functionality.getNodesFunctionalities(pg2._NODES);
                    List<Functionality.Functions> afterUpdateG1 = new List<Functionality.Functions>(afterRemovePg1);
                    foreach (Functionality.Functions func in funcs2)
                    {
                        if (!afterUpdateG1.Contains(func))
                        {
                            afterUpdateG1.Add(func);
                        }
                    }
                    var sub = allFuncs.Except(afterUpdateG1);
                    if (sub.Count() > 0)
                    {
                        continue;
                    }
                    if (funcs2.Contains(f))
                    {
                        if (res == null || res[0]._NODES.Count == 0 || pg1._NODES.Count < res[0]._NODES.Count)
                        {
                            res = new PartGroup[2];
                            res[0] = pg1;
                            res[1] = pg2;
                        }
                    }
                }
            }
            return res;
        }// findAPairPartGroupsWithFunctionalConstraints

        public void deformFunctionPart(double s, int axis, bool duplicate)
        {
            if (_currModel == null || _currModel._GRAPH == null)
            {
                return;
            }
            if (duplicate && _currModel.isDuplicated)
            {
                return;
            }
            // find the main node
            Node mainNode = this.findMainNodeToScale(_currModel._GRAPH);
            if (mainNode == null)
            {
                return;
            }
            Vector3d center = mainNode._PART._BOUNDINGBOX.CENTER;
            Vector3d scaleVec = new Vector3d(1, 1, 1);
            Vector3d newCenter = new Vector3d(center);
            if (axis == 0)
            {
                scaleVec[axis] = s;
            }
            else if (axis == 1)
            {
                newCenter.y += s;
            }
            if (duplicate)
            {
                // first scale it back
                scaleVec[0] = 0.5;
            }
            Matrix4d S = Matrix4d.ScalingMatrix(scaleVec);      
            Matrix4d Q = Matrix4d.TranslationMatrix(newCenter) * S * Matrix4d.TranslationMatrix(new Vector3d() - center);
            if (mainNode._isGroundTouching)
            {
                Node cNode = mainNode.Clone() as Node;
                deformANodeAndEdges(cNode, Q);
                Vector3d trans = new Vector3d();
                trans.y = -cNode._PART._BOUNDINGBOX.MinCoord.y;
                Matrix4d T = Matrix4d.TranslationMatrix(trans);
                Q = T * Q;
            }
            deformANodeAndEdges(mainNode, Q);
            deformSymmetryNode(mainNode);
            deformPropagation(_currModel._GRAPH, mainNode);
            _currModel._GRAPH.resetUpdateStatus();
            if (duplicate)
            {
                foreach (Node node in _currModel._GRAPH._NODES)
                {
                    node.updateOriginPos();
                }
                duplicateFunctionalNode(_currModel, mainNode);
            }
            this.Refresh();
        }// deformFunctionPart

        private void duplicateFunctionalNode(Model m, Node mainNode)
        {
            // 2X scaling
            // start with the case that a main node is supported by left and right support
            Graph g = m._GRAPH;
            List<Node> toDuplicate = new List<Node>();
            toDuplicate.Add(mainNode);
            List<Functionality.Functions> funcs = mainNode._funcs;
            double x1 = double.MaxValue;
            double x2 = double.MinValue;
            foreach (Node node in g._NODES)
            {
                if (node._funcs.Count != funcs.Count || node == mainNode)
                {
                    continue;
                }
                var sub = node._funcs.Except(funcs);
                if (sub.Count() > 0){
                    continue;      
                }
                toDuplicate.Add(node);
            }
            
            // support nodes
            foreach (Node node in mainNode._adjNodes)
            {
                if (node._funcs.Contains(Functionality.Functions.GROUND_TOUCHING)
                    && node._PART._BOUNDINGBOX.CENTER.x > 0)
                {
                    toDuplicate.Add(node);
                }
            }
            foreach (Node node in toDuplicate)
            {
                x1 = x1 < node._PART._BOUNDINGBOX.MinCoord.x ? x1 : node._PART._BOUNDINGBOX.MinCoord.x;
                x2 = x2 > node._PART._BOUNDINGBOX.MaxCoord.x ? x2 : node._PART._BOUNDINGBOX.MaxCoord.x;
            }
            // region growing dependent nodes
            List<Node> dependentNodes = g.bfs_regionGrowingDependentNodes(toDuplicate);
            toDuplicate.AddRange(dependentNodes);
            // duplicate
            double move = mainNode._PART._BOUNDINGBOX.MaxCoord.x - mainNode._PART._BOUNDINGBOX.MinCoord.x;
            move = x2 - x1;
            Matrix4d shift = Matrix4d.TranslationMatrix(new Vector3d(move, 0, 0));
            foreach (Node node in toDuplicate)
            {
                Node cloned = node.Clone() as Node;
                cloned.TransformFromOrigin(shift);
                cloned.updateOriginPos();
                g.addANode(cloned);
                m.addAPart(cloned._PART);
            }
            m.isDuplicated = true;
        }// duplicateFunctionalNode

        private Node findMainNodeToScale(Graph g)
        {
            if (g == null || g._NODES.Count == 0)
            {
                return null;
            }
            Node mainNode = null;
            foreach (Node node in g._NODES)
            {
                if (!Functionality.ContainsMainFunction(node._funcs))
                {
                    continue;
                }
                if (node._funcs.Count == 1)
                {
                    return node;
                }
                mainNode = node;
            }
            return mainNode;
        }// findMainNodeToScale

        //########## end - Functions ##########//


        //######### Part-based #########//
        public void selectBbox(Quad2d q, bool isCtrl)
        {
            // cannot use GRAPH, as it maybe used for data preprocessing, i.e., grouping, i dont need graph here
            if (this._currModel == null || q == null) return;
            this.cal2D();
            _selectedNodes = new List<Node>();
            foreach (Part p in _currModel._PARTS)
            {
                if (p._BOUNDINGBOX == null) continue;
                if (!isCtrl && _selectedParts.Contains(p))
                {
                    continue;
                }
                foreach (Vector2d v in p._BOUNDINGBOX._POINTS2D)
                {
                    Vector2d v2 = new Vector2d(v);
                    //v2.y = this.Height - v2.y;
                    if (Quad2d.isPointInQuad(v2, q))
                    {
                        if (isCtrl)
                        {
                            _selectedParts.Remove(p);
                            break;
                        }
                        else
                        {
                            _selectedParts.Add(p);
                        }
                        break;
                    }
                }
            }
            if (_currModel._GRAPH != null)
            {
                foreach (Node node in _currModel._GRAPH._NODES)
                {
                    if (_selectedParts.Contains(node._PART))
                    {
                        _selectedNodes.Add(node);
                    }
                }
            }
        }//selectBbox

        public void selectPartByUser(Vector2d mousePos)
        {
            // cannot use GRAPH, as it maybe used for data preprocessing, i.e., grouping, i dont need graph here
            if (this._currModel == null || _currModel._GRAPH == null)
            {
                return;
            }
            this.cal2D();
            Node pointedNode = null;
            foreach (Node node in _currModel._GRAPH._NODES)
            {
                if (node._PART._BOUNDINGBOX == null) continue;
                Vector2d minCoord = Vector2d.MaxCoord();
                Vector2d maxCoord = Vector2d.MinCoord();
                foreach (Vector2d v in node._PART._BOUNDINGBOX._POINTS2D)
                {
                    minCoord = Vector2d.Min(minCoord, v);
                    maxCoord = Vector2d.Max(maxCoord, v);
                }
                if (Quad2d.isPointInQuad(mousePos, new Quad2d(minCoord,maxCoord))) {
                    pointedNode = node;
                    break;
                }
            }
            _userSelectedParts.Clear();
            _userSelectedNodes.Clear();
            if (pointedNode == null)
            {
                return;
            }
            // find all nodes with the same funcs
            List<Functionality.Functions> funcs = pointedNode._funcs;
            foreach (Node node in _currModel._GRAPH._NODES)
            {
                List<Functionality.Functions> funcs_2 = node._funcs;
                if(funcs_2.Count != funcs.Count) {
                    continue;
                }
                var comp = funcs_2.Except(funcs);
                if (comp.Count() > 0)
                {
                    continue;
                }
                _userSelectedParts.Add(node._PART);
                _userSelectedNodes.Add(node);
            }
        }//selectBbox

        public void selectContactPoint(Vector2d mousePos)
        {
            if (this._currModel == null || _currModel._GRAPH == null) return;
            this.cal2D();
            double mind = double.MaxValue;
            _selectedEdge = null;
            _selectedContact = null;
            Edge nearestEdge = null;
            Contact nearestContact = null;
            foreach (Edge e in this._currModel._GRAPH._EDGES)
            {
                foreach (Contact pnt in e._contacts)
                {
                    Vector2d v2 = pnt._pos2d;
                    double dis = (v2 - mousePos).Length();
                    if (dis < mind)
                    {
                        mind = dis;
                        nearestEdge = e;
                        nearestContact = pnt;
                    }
                }
            }

            if (mind < Common._thresh2d)
            {
                _selectedEdge = nearestEdge;
                _selectedContact = nearestContact;
            }
            if (_selectedEdge != null)
            {
                this.calEditAxesLoc();
            }
        }//selectContactPoint

        public void setMeshColor(Color c)
        {
            foreach (Part p in _selectedParts)
            {
                p._COLOR = c;
            }
            this.Refresh();
        }

        public void groupParts()
        {
            if (_currModel == null)
            {
                return;
            }
            Part newPart = _currModel.groupParts(_selectedParts);
            _selectedParts.Clear();
            _selectedParts.Add(newPart);
            //_currModel.initializeGraph();
            this.cal2D();
            this.Refresh();
        }// groupParts

        public void unGroupParts()
        {
            if (_currModel == null)
            {
                return;
            }
            _currModel.unGroupParts(_selectedParts);
            _selectedParts.Clear();
            _currModel._GRAPH = null;
            this.cal2D();
            this.Refresh();
        }// unGroupParts

        public void createAPartGroup()
        {
            if (_currModel == null || _selectedNodes.Count == 0)
            {
                return;
            }
            _currModel._GRAPH.addAPartGroup(_selectedNodes);
        }// createAPartGroup

        public void clearPartGroups()
        {
            _currModel._GRAPH._partGroups.Clear();
        }

        public ModelViewer addSelectedPartsToBasket()
        {
            if (_selectedParts == null || _selectedParts.Count == 0)
            {
                return null;
            }
            List<Part> cloneParts = new List<Part>();
            foreach (Part p in _selectedParts)
            {
                Part np = p.Clone() as Part;
                cloneParts.Add(np);
            }
            Model m = new Model(cloneParts);
            ModelViewer mv = new ModelViewer(m, -1, this, 1);
            _partViewers.Add(mv);
            return mv;
        }// addSelectedPartsToBasket

        private void editMouseDown(int mode, Vector2d mousePos)
        {
            _editArcBall = new ArcBall(this.Width, this.Height);
            switch (mode)
            {
                case 1: // Translate
                    _editArcBall.mouseDown((int)mousePos.x, (int)mousePos.y, ArcBall.MotionType.Pan);
                    break;
                case 2: // Scaling
                    _editArcBall.mouseDown((int)mousePos.x, (int)mousePos.y, ArcBall.MotionType.Scale);
                    break;
                case 3: // Rotate
                    _editArcBall.mouseDown((int)mousePos.x, (int)mousePos.y, ArcBall.MotionType.Rotate);
                    break;
            }
        }// editMouseDown

        private Matrix4d editMouseMove(int x, int y)
        {
            if (!this.isMouseDown) return Matrix4d.IdentityMatrix();
            _editArcBall.mouseMove(x, y);
            Matrix4d T = _editArcBall.getTransformMatrix(3);
            return T;
        }// editMouseMove

        private void editMouseUp()
        {
            _hightlightAxis = -1;
            foreach (Part p in _selectedParts)
            {
                p.updateOriginPos();
            }
            if (_currHumanPose != null)
            {
                _currHumanPose.updateOriginPos();
                this.updateBodyBones();
            }
            if (_editArcBall != null)
            {
                _editArcBall.mouseUp();
            }
            this.cal2D();
        }// editMouseUp

        private void transformSelections(Vector2d mousePos)
        {
            if (_selectedParts.Count == 0 && _currHumanPose == null)
            {
                return;
            }
            Matrix4d T = editMouseMove((int)mousePos.x, (int)mousePos.y);
            // use a fixed axis
            switch (this.currUIMode)
            {
                case UIMode.Translate:
                    {
                        if (_hightlightAxis == 2)
                        {
                            T[2, 3] = T[0, 3];
                        }
                        for (int i = 0; i < 3; ++i)
                        {
                            if (_hightlightAxis != -1 && i != _hightlightAxis)
                            {
                                T[i, 3] = 0;
                            }
                        }
                        break;
                    }
                case UIMode.Scale:
                    {
                        if (!_isRightClick) // right click == uniform scale
                        {
                            for (int i = 0; i < 3; ++i)
                            {
                                if (_hightlightAxis != -1 && i != _hightlightAxis)
                                {
                                    T[i, i] = 1;
                                }
                            }
                        }
                        break;
                    }
                case UIMode.Rotate:
                    {
                        if (_hightlightAxis != -1)
                        {
                            T = _editArcBall.getRotationMatrixAlongAxis(_hightlightAxis);
                        }
                        break;
                    }
                default:
                    {
                        T = Matrix4d.IdentityMatrix();
                        break;
                    }
            }
            // original center
            Vector3d ori = new Vector3d();
            if (_selectedParts.Count > 0)
            {
                ori = getCenter(_selectedParts);
                foreach (Part p in _selectedParts)
                {
                    p.TransformFromOrigin(T);
                }
            }
            else if (_currHumanPose != null) // NOTE!! else relation
            {
                ori = _currHumanPose._ROOT._ORIGIN;
                _currHumanPose.TransformFromOrigin(T);
            }

            if (this.currUIMode != UIMode.Translate)
            {
                Vector3d after = new Vector3d();
                if (_selectedParts.Count > 0)
                {
                    after = getCenter(_selectedParts);
                    Matrix4d TtoCenter = Matrix4d.TranslationMatrix(ori - after);
                    foreach (Part p in _selectedParts)
                    {
                        p.Transform(TtoCenter);
                    }
                }
                else if (_currHumanPose != null)
                {
                    after = _currHumanPose._ROOT._POS;
                    Matrix4d TtoCenter = Matrix4d.TranslationMatrix(ori - after);
                    if (_currHumanPose != null)
                    {
                        foreach (BodyNode bn in _currHumanPose._bodyNodes)
                        {
                            bn.Transform(TtoCenter);
                        }
                    }
                }
            }
        }// transformSelections

        private void moveContactPoint(Vector2d mousePos)
        {
            if (_selectedEdge == null || _selectedContact == null)
            {
                return;
            }
            Matrix4d T = editMouseMove((int)mousePos.x, (int)mousePos.y);
            if (_hightlightAxis == 2)
            {
                T[2, 3] = T[0, 3];
            }
            for (int i = 0; i < 3; ++i)
            {
                if (_hightlightAxis != -1 && i != _hightlightAxis)
                {
                    T[i, 3] = 0;
                }
            }
            _selectedContact.TransformFromOrigin(T);
        }// moveContactPoint

        private void moveContactUp()
        {
            if (_currModel != null && _currModel._GRAPH != null)
            {
                foreach (Edge e in _currModel._GRAPH._EDGES)
                {
                    foreach (Contact c in e._contacts)
                    {
                        c.updateOrigin();
                    }
                }
            }
        }// moveContactUp

        private Vector3d getCenter(List<Part> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                return new Vector3d();
            }
            Vector3d center = new Vector3d();
            foreach (Part p in parts)
            {
                center += p._BOUNDINGBOX.CENTER;
            }
            center /= parts.Count;
            return center;
        }// parts

        public void deleteParts()
        {
            if (_currModel == null || _currModel._GRAPH == null)
            {
                return;
            }
            _currModel._GRAPH.deleteNodes(_selectedNodes);
            foreach (Part p in _selectedParts)
            {
                _currModel.removeAPart(p);
            }
            _selectedParts.Clear();
            this.Refresh();
        }// deleteParts

        public void duplicateParts()
        {
            int n = _selectedParts.Count;
            Matrix4d shift = Matrix4d.TranslationMatrix(new Vector3d(0.2, 0, 0));
            for (int i = 0; i < n; ++i)
            {
                Part p = _selectedParts[i];
                Part pclone = p.Clone() as Part;
                pclone.TransformFromOrigin(shift);
                _currModel.addAPart(pclone);
                _selectedParts.Add(pclone);
            }
            this.Refresh();
        }// deleteParts

        public void composeSelectedParts()
        {
            if (_partViewers == null || _partViewers.Count == 0)
            {
                return;
            }
            _selectedParts.Clear();
            List<Part> parts = new List<Part>();
            foreach (ModelViewer mv in _partViewers)
            {
                List<Part> mv_parts = mv.getParts();
                foreach (Part p in mv_parts)
                {
                    Part pclone = p.Clone() as Part;
                    parts.Add(pclone);
                }
            }
            _currModel = new Model(parts);
            this.cal2D();
            this.Refresh();
        }// composeSelectedParts

        private void selectAxisWhileMouseMoving(Vector2d mousePos)
        {
            this.updateCamera();
            this.cal2D();
            Vector2d s = _editAxes[0]._pos2d;
            Vector2d e = _editAxes[1]._pos2d;
            Line2d xline = new Line2d(s, e);
            double xd = Polygon2D.PointDistToLine(mousePos, xline);

            s = _editAxes[6]._pos2d;
            e = _editAxes[7]._pos2d;
            Line2d yline = new Line2d(s, e);
            double yd = Polygon2D.PointDistToLine(mousePos, yline);

            s = _editAxes[12]._pos2d;
            e = _editAxes[13]._pos2d;
            Line2d zline = new Line2d(s, e);
            double zd = Polygon2D.PointDistToLine(mousePos, zline);

            _hightlightAxis = 0;
            if (yd < xd && yd < zd)
            {
                _hightlightAxis = 1;
            }
            if (zd < xd && zd < yd)
            {
                _hightlightAxis = 2;
            }
        }// selectAxisWhileMouseMoving

        private Matrix4d calTranslation(Vector2d prev, Vector2d curr)
        {
            Vector3d moveDir = new Vector3d();
            moveDir[_hightlightAxis] = 1;

            // distance
            Vector3d u = this.camera.ProjectPointToPlane(prev, _groundPlane.center, _groundPlane.normal);
            Vector3d v = this.camera.ProjectPointToPlane(curr, _groundPlane.center, _groundPlane.normal);
            Vector3d move = (v - u).Length() * moveDir;

            return Matrix4d.TranslationMatrix(move);
        }// calTranslation

        public void addAnEdge()
        {
            if (_currModel == null || _currModel._GRAPH == null || _selectedParts.Count != 2)
            {
                return;
            }
            int i = _currModel._PARTS.IndexOf(_selectedParts[0]);
            int j = _currModel._PARTS.IndexOf(_selectedParts[1]);
            if (i != -1 && j != -1)
            {
                Edge e = _currModel._GRAPH.isEdgeExist(_currModel._GRAPH._NODES[i], _currModel._GRAPH._NODES[j]);
                if (e == null)
                {
                    _currModel._GRAPH.addAnEdge(_currModel._GRAPH._NODES[i], _currModel._GRAPH._NODES[j]);
                }
                else
                {
                    int ncontact = e._contacts.Count;
                    string s = "Already has " + ncontact.ToString() + " contacts, add a contact anyway?";
                    if (MessageBox.Show(s, "Edit Edge", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Vector3d v = e._contacts[0]._pos3d + new Vector3d(0.05, 0, 0);
                        e._contacts.Add(new Contact(v));
                    }
                }
            }
        }// addAnEdge

        public void deleteAnEdge()
        {
            if (_selectedParts.Count != 2)
            {
                return;
            }
            int i = _currModel._PARTS.IndexOf(_selectedParts[0]);
            int j = _currModel._PARTS.IndexOf(_selectedParts[1]);
            if (i != -1 && j != -1)
            {
                _currModel._GRAPH.deleteAnEdge(_currModel._GRAPH._NODES[i], _currModel._GRAPH._NODES[j]);
            }
        }// deleteAnEdge

        public void loadHuamPose(string filename)
        {
            _currHumanPose = this.loadAHumanPose(filename);
            _humanposes = new List<HumanPose>();
            _humanposes.Add(_currHumanPose);
            // test
            //Matrix4d T = Matrix4d.TranslationMatrix(new Vector3d(0, -0.2, -0.4));
            //foreach (BodyNode bn in _currHumanPose._bodyNodes)
            //{
            //    bn.TransformOrigin(T);
            //    bn.Transform(T);
            //}
            //foreach (BodyBone bb in _currHumanPose._bodyBones)
            //{
            //    bb.updateEntity();
            //}
        }// loadHuamPose

        public void importHumanPose(string filename)
        {
            HumanPose hp = this.loadAHumanPose(filename);
            _humanposes.Add(hp);
            this.Refresh();
        }

        private HumanPose loadAHumanPose(string filename)
        {
            HumanPose hp = new HumanPose();
            hp.loadPose(filename);
            return hp;
        }

        public void saveHumanPose(string name)
        {
            if (_currHumanPose != null)
            {
                _currHumanPose.savePose(name);
            }
        }// saveHumanPose

        private void SelectBodyNode(Vector2d mousePos)
        {
            if (_currHumanPose == null)
            {
                return;
            }
            _selectedNode = null;
            foreach (BodyNode bn in _currHumanPose._bodyNodes)
            {
                double d = (bn._pos2 - mousePos).Length();
                if (d < Common._thresh2d)
                {
                    _selectedNode = bn;
                    break;
                }
            }
        }// SelectBodyNode

        private void EditBodyNode(Vector2d mousePos)
        {
            if (_selectedNode == null || _selectedNode._PARENT == null)
            {
                return;
            }
            this.updateCamera();
            Vector3d originPos = _selectedNode._ORIGIN;
            Vector3d projPos = this.camera.Project(originPos);
            Vector3d p1 = this.camera.UnProject(new Vector3d(mousePos.x, this.Height - mousePos.y, projPos.z));
            Vector3d p2 = this.camera.UnProject(new Vector3d(mousePos.x, this.Height - mousePos.y, projPos.z + 0.5));
            Vector3d dir = (p2 - p1).normalize();

            Vector3d u = p1 - originPos;
            Vector3d q = p1 - u.Dot(dir) * dir;				// the point on the plane parallel to screen
            Vector3d o = _selectedNode._PARENT._POS;
            double length = (originPos - o).Length();
            Vector3d t = o + length * (q - o).normalize();			// the target point
            Matrix4d T = Matrix4d.TranslationMatrix(t - originPos);

            DeformBodyNode(_selectedNode, T);
            DeformBodyNodePropagation(_selectedNode, T);
        }// EditBodyNode

        private void DeformBodyNode(BodyNode node, Matrix4d T)
        {
            if (node == null) return;
            node.TransformFromOrigin(T);
        }// DeformBodyNode

        private void DeformBodyNodePropagation(BodyNode node, Matrix4d T)
        {
            if (node == null) return;
            List<BodyNode> children = node.getDescendents();
            foreach (BodyNode bn in children)
            {
                bn.TransformFromOrigin(T);
            }
        }// DeformBodyNodePropagation

        private void updateBodyBones()
        {
            if (_currHumanPose == null) return;
            foreach (BodyBone bn in _currHumanPose._bodyBones)
            {
                bn.updateEntity();
            }
        }// DeformBodyNodePropagation

        //######### end-Part-based #########//

        //######### Data & feature from ICON2 paper #########//
        public void loadPatchInfo_ori(string foldername)
        {
            this.readModelModelViewMatrix(foldername + "\\view.mat");
            this.reloadView();

            this.foldername = foldername;
            string modelFolder = foldername + "\\meshes\\";
            string sampleFolder = foldername + "\\samples\\";
            string weightFolder = foldername + "\\weights\\";
            string funcSpaceFolder = foldername + "\\funcSpace\\";

            string saveModelFolder = foldername + "\\autoModels\\";
            string imageFolder = foldername + "\\snapshots\\";

            if (!Directory.Exists(sampleFolder) || !Directory.Exists(weightFolder) ||
                !Directory.Exists(modelFolder) || !Directory.Exists(funcSpaceFolder))
            {
                MessageBox.Show("Lack data folder.");
                return;
            }

            string[] modelFiles = Directory.GetFiles(modelFolder, "*.obj");
            string[] sampleFiles = Directory.GetFiles(sampleFolder, "*.poisson");
            string[] weightFiles = Directory.GetFiles(weightFolder, "*.csv");
            string[] funspaceFiles = Directory.GetFiles(funcSpaceFolder, "*.fs");

            _ancesterModels = new List<Model>();

            int nfile = 0;
            // re-normalize all meshes w.r.t func space
            // make sure meshes, sample points, func space in the same scale
            foreach (string modelstr in modelFiles)
            {
                // model
                string model_name = Path.GetFileName(modelstr);
                string pure_name = model_name.Substring(0, model_name.LastIndexOf('.'));
                string iNum = pure_name.Substring(pure_name.LastIndexOf('_') + 1);
                int iInfo = int.Parse(iNum);
                if (iInfo != 1)
                {
                    continue;
                }
                model_name = model_name.Substring(0, model_name.LastIndexOf('_'));
                Mesh mesh = new Mesh(modelstr, false);
                if (mesh.isOverSize())
                {
                    // too large mesh
                    continue;
                }
                // category name
                string category = model_name.Substring(0, model_name.IndexOf('_'));

                // category specific view
                this.readModelModelViewMatrix(foldername + "\\" + category + ".mat");
                this.reloadView();

                // sample points
                string sample_name = sampleFolder + model_name + ".poisson";
                SamplePoints sp = loadSamplePoints(sample_name, mesh.FaceCount);
                FunctionalSpace[] fss = null;
                
                // in case the order of files are not the same in diff folders
                string model_name_filter = model_name + "_";
                List<PatchWeightPerCategory> patchWeights = new List<PatchWeightPerCategory>();
                for (int nc = 0; nc < Functionality._NUM_CATEGORIY; ++nc)
                {
                    // weights
                    List<string> cur_wfiles = new List<string>();
                    string cat_name = Functionality.getCategoryName(nc);
                    int fid = 0;
                    string model_wight_name_filter = model_name_filter + "predict_" + cat_name + "_";
                    model_wight_name_filter = model_wight_name_filter.ToLower();
                    while (fid < weightFiles.Length)
                    {
                        string weight_name = Path.GetFileName(weightFiles[fid]).ToLower();
                        if (weight_name.StartsWith(model_wight_name_filter))
                        {
                            // locate the weight files
                            while (weight_name.StartsWith(model_wight_name_filter))
                            {
                                cur_wfiles.Add(weightFiles[fid++]);
                                if (fid >= weightFiles.Length)
                                {
                                    break;
                                }
                                weight_name = Path.GetFileName(weightFiles[fid]).ToLower();
                            }
                            break;
                        }
                        ++fid;
                    }
                    // load patch weights
                    int npatch = 0;
                    int nFaceFromSP = sp._faceIdx.Length;
                    // multiple weights file w.r.t. patches
                    double[,] weights_patches = new double[nFaceFromSP, cur_wfiles.Count];
                    Color[,] colors_patches = new Color[nFaceFromSP, cur_wfiles.Count];
                    List<List<double>> weights_per_cat = new List<List<double>>();
                    if (cat_name.Equals(category))
                    {
                        sp._blendColors = new Color[nFaceFromSP];
                        for (int c = 0; c < nFaceFromSP; ++c)
                        {
                            sp._blendColors[c] = Color.LightGray;
                        }
                    }
                    Program.GetFormMain().writeToConsole("Category :" + cat_name);
                    foreach (string wfile in cur_wfiles)
                    {
                        double minw;
                        double maxw;
                        double[] weights = loadPatchWeight(wfile, out minw, out maxw);
                        // TEST INFO
                        Program.GetFormMain().writeToConsole("Minimum weight is: " + minw.ToString());
                        Program.GetFormMain().writeToConsole("Maximum weight is: " + maxw.ToString());
                        double sumw = 0;
                        foreach (double w in weights)
                        {
                            sumw += w;
                        }
                        Program.GetFormMain().writeToConsole("Sum of weights is: " + sumw.ToString());

                        weights_per_cat.Add(new List<double>(weights));
                        if (weights == null || weights.Length != nFaceFromSP)
                        {
                            MessageBox.Show("Weight file does not match sample file: " + Path.GetFileName(wfile));
                            continue;
                        }

                        double wdiff = maxw - minw;
                        for (int i = 0; i < weights.Length; ++i)
                        {
                            weights_patches[i, npatch] = weights[i];
                        }
                        if (cat_name.Equals(category))
                        {
                            for (int i = 0; i < weights.Length; ++i)
                            {
                                double ratio = (weights[i] - minw) / wdiff;
                                if (ratio < 0.1)
                                {
                                    continue;
                                }
                                Color color = GLDrawer.getColorGradient(ratio, npatch);
                                //mesh.setVertexColor(GLDrawer.getColorArray(color), i);
                                byte[] color_array = GLDrawer.getColorArray(color, 255);
                                mesh.setFaceColor(color_array, sp._faceIdx[i]);
                                colors_patches[i, npatch] = GLDrawer.getColorRGB(color_array);
                                sp._blendColors[i] = colors_patches[i, npatch];
                            }
                        }
                        ++npatch;
                    }// walk through each weight per functional patch
                    patchWeights.Add(new PatchWeightPerCategory(cat_name, weights_patches));
                    // weights & colors
                    if (cat_name.Equals(category))
                    {
                        sp._weights = weights_patches;
                        sp._colors = colors_patches;
                        // functional space
                        model_name_filter = model_name_filter.ToLower();
                        fid = 0;
                        List<string> fspaceFiles = new List<string>();
                        while (fid < funspaceFiles.Length)
                        {
                            string func_name = Path.GetFileName(funspaceFiles[fid]).ToLower();
                            if (func_name.StartsWith(model_name_filter))
                            {
                                // locate the weight files
                                while (func_name.StartsWith(model_name_filter))
                                {
                                    fspaceFiles.Add(funspaceFiles[fid++]);
                                    if (fid >= funspaceFiles.Length)
                                    {
                                        break;
                                    }
                                    func_name = Path.GetFileName(funspaceFiles[fid]).ToLower();
                                }
                                break;
                            }
                            ++fid;
                        }
                        if (fspaceFiles.Count != npatch)
                        {
                            //MessageBox.Show("#Functional space file does not match weight file.");
                            continue;
                        }
                        fss = new FunctionalSpace[npatch];
                        int nfs = 0;
                        bool noFS = false;
                        foreach (String fsfile in fspaceFiles)
                        {
                            FunctionalSpace fs = loadFunctionSpace(fsfile, nfs);
                            if (fs == null || fs._mesh == null || fs._mesh.isOverSize())
                            {
                                //MessageBox.Show("Functional space file error: " + Path.GetFileName(fsfile));
                                //return;
                                noFS = true;
                                break;
                            }
                            fss[nfs++] = fs;
                        }
                        if (noFS)
                        {
                            continue;
                        }
                    }// record for create the model
                }// each category
                // create the model
                // test
                if (fss != null && fss.Length > 0 && fss[0] != null)
                {
                    Program.writeToConsole("Load " + model_name);
                    Program.writeToConsole("Mesh:");
                    Program.writeToConsole("Max vertex: " + this.vector3dToString(mesh.MaxCoord, ", ", ""));
                    Program.writeToConsole("Min vertex: " + this.vector3dToString(mesh.MinCoord, ", ", ""));

                    sp._weightsPerCat = patchWeights;
                    Model model = new Model(mesh, sp, fss, true);
                    model._model_name = model_name;
                    //_ancesterModels.Add(model);

                    model.initializeGraph();
                    model.composeMesh();
                    
                    // Screenshots
                    _currModel = model;
                    string modelFileName = saveModelFolder + model_name + ".pam";
                    this.saveAPartBasedModel(model, modelFileName, true);
                    this.decideWhichToDraw(true, false, false, true, false, false);
                    this.Refresh();
                    this.decideWhichToDraw(true, false, false, false, true, false);

                    for (int i = 0; i < _currModel._funcSpaces.Length; ++i)
                    {
                        _fsIdx = i;
                        this.Refresh();
                        this.captureScreen(imageFolder + model_name + "_fs_" + (i + 1).ToString() + ".png");

                        // test
                        Program.writeToConsole("Functional space #" + (i + 1).ToString());
                        Program.writeToConsole("Max vertex: " + this.vector3dToString(model._funcSpaces[i]._mesh.MaxCoord, ", ", ""));
                        Program.writeToConsole("Min vertex: " + this.vector3dToString(model._funcSpaces[i]._mesh.MinCoord, ", ", ""));
                    }
                    // cal score and save
                    //this.predictFunctionalPatches();
                }
                ++nfile;
            }
            if (_ancesterModels.Count > 0)
            {
                _currModel = _ancesterModels[0];
            }
            _fsIdx = 0;
            this.Refresh();
        }// loadPatchInfo_ori

        public void loadPatchInfo_ori_draw_sample_points_only(string foldername)
        {
            this.readModelModelViewMatrix(foldername + "\\view.mat");
            this.reloadView();

            this.foldername = foldername;
            string modelFolder = foldername + "\\meshes\\";
            string sampleFolder = foldername + "\\samples\\";
            string weightFolder = foldername + "\\weights\\";
            string funcSpaceFolder = foldername + "\\funcSpace\\";

            string saveModelFolder = foldername + "\\autoModels\\";
            string imageFolder = foldername + "\\snapshots\\";

            if (!Directory.Exists(sampleFolder) || !Directory.Exists(weightFolder) ||
                !Directory.Exists(modelFolder) || !Directory.Exists(funcSpaceFolder))
            {
                MessageBox.Show("Lack data folder.");
                return;
            }

            string[] modelFiles = Directory.GetFiles(modelFolder, "*.obj");
            string[] sampleFiles = Directory.GetFiles(sampleFolder, "*.poisson");
            string[] weightFiles = Directory.GetFiles(weightFolder, "*.csv");
            string[] funspaceFiles = Directory.GetFiles(funcSpaceFolder, "*.fs");

            
            // re-normalize all meshes w.r.t func space
            // make sure meshes, sample points, func space in the same scale
            foreach (string modelstr in modelFiles)
            {
                // model
                string model_name = Path.GetFileName(modelstr);
                string pure_name = model_name.Substring(0, model_name.LastIndexOf('.'));
                string iNum = pure_name.Substring(pure_name.LastIndexOf('_') + 1);
                int iInfo = int.Parse(iNum);
                if (iInfo != 1)
                {
                    continue;
                }
                model_name = model_name.Substring(0, model_name.LastIndexOf('_'));
                Mesh mesh = new Mesh(modelstr, false);
                if (mesh.isOverSize())
                {
                    // too large mesh
                    continue;
                }
                // category name
                string category = model_name.Substring(0, model_name.IndexOf('_'));

                // category specific view
                this.readModelModelViewMatrix(foldername + "\\" + category + ".mat");
                this.reloadView();

                // sample points
                string sample_name = sampleFolder + model_name + ".poisson";
                SamplePoints sp = loadSamplePoints(sample_name, mesh.FaceCount);

                Model model = new Model(mesh, sp, null, true);
                model._model_name = model_name;
                _currModel = model;
                // weights                
                string model_name_filter = model_name + "_";
                for (int nc = 0; nc < Functionality._NUM_CATEGORIY; ++nc)
                {
                    List<string> cur_wfiles = new List<string>();
                    string nCategory = Functionality.getCategoryName(nc);
                    //if (nCategory != "TVBench")
                    //{
                    //    continue;
                    //}
                    string model_wight_name_filter = model_name_filter + "predict_" + nCategory + "_";
                    // in case the order of files are not the same in diff folders
                    int fid = 0;
                    while (fid < weightFiles.Length)
                    {
                        string weight_name = Path.GetFileName(weightFiles[fid]);
                        if (weight_name.StartsWith(model_wight_name_filter))
                        {
                            // locate the weight files
                            while (weight_name.StartsWith(model_wight_name_filter))
                            {
                                cur_wfiles.Add(weightFiles[fid++]);
                                if (fid >= weightFiles.Length)
                                {
                                    break;
                                }
                                weight_name = Path.GetFileName(weightFiles[fid]);
                            }
                            break;
                        }
                        ++fid;
                    }
                    // load patch weights
                    int npatch = 0;
                    int nFaceFromSP = sp._faceIdx.Length;
                    // multiple weights file w.r.t. patches
                    double[,] weights_patches = new double[cur_wfiles.Count, nFaceFromSP];
                    Color[,] colors_patches = new Color[cur_wfiles.Count, nFaceFromSP];
                    sp._blendColors = new Color[nFaceFromSP];
                    for (int c = 0; c < nFaceFromSP; ++c)
                    {
                        sp._blendColors[c] = Color.LightGray;
                    }
                    foreach (string wfile in cur_wfiles)
                    {
                        double minw;
                        double maxw;
                        double[] weights = loadPatchWeight(wfile, out minw, out maxw);
                        if (weights == null || weights.Length != nFaceFromSP)
                        {
                            MessageBox.Show("Weight file does not match sample file: " + Path.GetFileName(wfile));
                            continue;
                        }
                        double wdiff = maxw - minw;
                        for (int i = 0; i < weights.Length; ++i)
                        {
                            weights_patches[npatch, i] = weights[i];
                            double ratio = (weights[i] - minw) / wdiff;
                            if (ratio < 0.1)
                            {
                                continue;
                            }
                            Color color = GLDrawer.getColorGradient(ratio, npatch);
                            //mesh.setVertexColor(GLDrawer.getColorArray(color), i);
                            byte[] color_array = GLDrawer.getColorArray(color, 255);
                            mesh.setFaceColor(color_array, sp._faceIdx[i]);
                            colors_patches[npatch, i] = GLDrawer.getColorRGB(color_array);
                            sp._blendColors[i] = colors_patches[npatch, i];
                        }
                        ++npatch;
                    }
                    // weights & colors
                    sp._weights = weights_patches;
                    sp._colors = colors_patches;

                    // Screenshots              
                    _currModel.checkInSamplePoints(sp);
                    if (nc == 0)
                    {
                        this.decideWhichToDraw(true, false, false, true, false, false);
                        this.Refresh();
                        this.captureScreen(imageFolder + model_name + ".png");
                    }
                    this.decideWhichToDraw(false, false, false, false, true, true);
                    this.Refresh();
                    this.captureScreen(imageFolder + model_name + "_" + nCategory + "_functionalPatches.png");
                }
            }
            this.Refresh();
        }// loadPatchInfo_ori_draw_sample_points_only

        private List<string> loadCategory(string filename)
        {
            List<string> cats = new List<string>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    cats.Add(s);
                }
            }
            return cats;
        }// readCategory

        public void loadPatchInfo_opt(string foldername)
        {
            this.foldername = foldername;
            string modelFolder = foldername + "\\meshes\\";
            string sampleFolder = foldername + "\\samples\\";
            string weightFolder = foldername + "\\weights\\";
            string funcSpaceFolder = foldername + "\\funcSpace\\";

            if (!Directory.Exists(sampleFolder) || !Directory.Exists(weightFolder) ||
                !Directory.Exists(modelFolder) || !Directory.Exists(funcSpaceFolder))
            {
                MessageBox.Show("Lack data folder.");
                return;
            }

            List<string> cats = this.loadCategory(modelFolder + "category.txt");

            string[] subFolders = Directory.GetDirectories(modelFolder);
            _ancesterModels = new List<Model>();
            int nfile = 0;
            foreach (string subfolder in subFolders)
            {
                string subfolder_name = subfolder.Substring(subfolder.LastIndexOf('\\') + 1);
                
                string[] modelFiles = Directory.GetFiles(modelFolder + subfolder_name, "*.obj");
                string[] sampleFiles = Directory.GetFiles(sampleFolder + subfolder_name, "*.poisson");
                string[] weightFiles = Directory.GetFiles(weightFolder + subfolder_name, "*.csv");

                // re-normalize all meshes w.r.t func space
                // make sure meshes, sample points, func space in the same scale
                foreach (string modelstr in modelFiles)
                {
                    string model_name = Path.GetFileName(modelstr);
                    model_name = model_name.Substring(0, model_name.LastIndexOf('.'));
                    // model
                    Mesh mesh = new Mesh(modelstr, false);
                    // category name
                    //string category = model_name.Substring(model_name.LastIndexOf('_') + 1);
                    //category = "Chair";
                    string category = cats[nfile];
                    // sample points
                    string sample_name = sampleFolder + subfolder_name + "\\" +  model_name + ".poisson";
                    SamplePoints sp = loadSamplePoints(sample_name, mesh.FaceCount);
                    // weights
                    List<string> cur_wfiles = new List<string>();
                    // in case the order of files are not the same in diff folders
                    int fid = 0;
                    string model_name_filter = model_name + "_";
                    string model_wight_name_filter = model_name_filter + "predict_" + category + "_";
                    while (fid < weightFiles.Length)
                    {
                        string weight_name = Path.GetFileName(weightFiles[fid]);
                        if (weight_name.StartsWith(model_wight_name_filter))
                        {
                            // locate the weight files
                            while (weight_name.StartsWith(model_wight_name_filter))
                            {
                                cur_wfiles.Add(weightFiles[fid++]);
                                if (fid >= weightFiles.Length)
                                {
                                    break;
                                }
                                weight_name = Path.GetFileName(weightFiles[fid]);
                            }
                            break;
                        }
                        ++fid;
                    }
                    // load patch weights
                    int npatch = 0;
                    int nFaceFromSP = sp._faceIdx.Length;
                    // multiple weights file w.r.t. patches
                    double[,] weights_patches = new double[cur_wfiles.Count, nFaceFromSP];
                    Color[,] colors_patches = new Color[cur_wfiles.Count, nFaceFromSP];
                    sp._blendColors = new Color[nFaceFromSP];
                    for (int c = 0; c < nFaceFromSP; ++c)
                    {
                        sp._blendColors[c] = Color.LightGray;
                    }
                    foreach (string wfile in cur_wfiles)
                    {
                        double minw;
                        double maxw;
                        double[] weights = loadPatchWeight(wfile, out minw, out maxw);
                        if (weights == null || weights.Length != nFaceFromSP)
                        {
                            MessageBox.Show("Weight file does not match sample file: " + Path.GetFileName(wfile));
                            continue;
                        }
                        double wdiff = maxw - minw;
                        for (int i = 0; i < weights.Length; ++i)
                        {
                            weights_patches[npatch, i] = weights[i];
                            double ratio = (weights[i] - minw) / wdiff;
                            if (ratio < 0.1)
                            {
                                continue;
                            }
                            Color color = GLDrawer.getColorGradient(ratio, npatch);
                            //mesh.setVertexColor(GLDrawer.getColorArray(color), i);
                            byte[] color_array = GLDrawer.getColorArray(color);
                            mesh.setFaceColor(color_array, sp._faceIdx[i]);
                            colors_patches[npatch, i] = GLDrawer.getColorRGB(color_array);
                            sp._blendColors[i] = colors_patches[npatch, i];
                        }
                        ++npatch;
                    }
                    // weights & colors
                    sp._weights = weights_patches;
                    sp._colors = colors_patches;
                    //// functional space
                    //fid = 0;
                    //List<string> fspaceFiles = new List<string>();
                    //while (fid < funspaceFiles.Length)
                    //{
                    //    string func_name = Path.GetFileName(funspaceFiles[fid]);
                    //    if (func_name.StartsWith(model_name_filter))
                    //    {
                    //        // locate the weight files
                    //        while (func_name.StartsWith(model_name_filter))
                    //        {
                    //            fspaceFiles.Add(funspaceFiles[fid++]);
                    //            if (fid >= funspaceFiles.Length)
                    //            {
                    //                break;
                    //            }
                    //            func_name = Path.GetFileName(funspaceFiles[fid]);
                    //        }
                    //        break;
                    //    }
                    //    ++fid;
                    //}
                    //if (fspaceFiles.Count != npatch)
                    //{
                    //    MessageBox.Show("#Functional space file does not match weight file.");
                    //    return;
                    //}
                    //FunctionalSpace[] fss = new FunctionalSpace[npatch];
                    //int nfs = 0;
                    //foreach (String fsfile in fspaceFiles)
                    //{
                    //    FunctionalSpace fs = loadFunctionSpace(fsfile);
                    //    if (fs == null)
                    //    {
                    //        MessageBox.Show("Functional space file error: " + Path.GetFileName(fsfile));
                    //        return;
                    //    }
                    //    fss[nfs++] = fs;
                    //}

                    FunctionalSpace[] fss = null;
                    Model model = new Model(mesh, sp, fss, false);
                    model._model_name = model_name;
                    _ancesterModels.Add(model);

                    ++nfile;
                    //if (nfile > 2)
                    //{
                    //    // TEST
                    //    break;
                    //}
                }
            }
            if (_ancesterModels.Count > 0)
            {
                _currModel = _ancesterModels[0];
            }
            this.Refresh();
        }// loadPatchInfo

        private FunctionalSpace loadFunctionalSpaceInfo(string meshName, string infoName)
        {
            Mesh mesh = new Mesh(meshName, false);
            List<double> weights = new List<double>();
            using (StreamReader sr = new StreamReader(infoName))
            {
                char[] separators = { ' ', '\\', '\t' };
                int faceId = 0;
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine().Trim();
                    string[] strs = s.Split(separators);
                    byte[] color = new byte[4];
                    for (int i = 0; i < 4; ++i)
                    {
                        color[i] = byte.Parse(strs[i]);
                    }
                    color[3] = GLDrawer.FunctionalSpaceAlpha;
                    mesh.setFaceColor(color, faceId++);
                    weights.Add(double.Parse(strs[4]));
                }
            }
            FunctionalSpace fs = new FunctionalSpace(mesh, weights.ToArray());
            return fs;
        }// loadFunctionalSpaceInfo

        private FunctionalSpace loadFunctionSpace(string filename, int fid)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            // load mesh
            List<double> weights = new List<double>();
            double minw = double.MaxValue;
            double maxw = double.MinValue;
            // weights
            List<double> vposs = new List<double>();
            List<int> faceIds = new List<int>();
            // test
            Vector3d minCoord = Vector3d.MaxCoord;
            Vector3d maxCoord = Vector3d.MinCoord;
            using (StreamReader sr = new StreamReader(filename))
            {
                // read only weights
                char[] separators = { ' ', '\\', '\t' };
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine().Trim();
                    string[] strs = s.Split(separators);

                    if (strs[0] == "v")
                    {
                        Vector3d vec = new Vector3d();
                        for (int i = 1; i < 4; ++i)
                        {
                            vposs.Add(double.Parse(strs[i]));
                            vec[i - 1] = double.Parse(strs[i]);
                        }
                        minCoord = Vector3d.Min(minCoord, vec);
                        maxCoord = Vector3d.Max(maxCoord, vec);
                    }
                    if (strs[0] == "f")
                    {
                        for (int i = 1; i < 4; ++i)
                        {
                            faceIds.Add(int.Parse(strs[i]));
                        }
                        double w = double.Parse(strs[4]);
                        weights.Add(w);
                        minw = minw < w ? minw : w;
                        maxw = maxw > w ? maxw : w;
                        if (strs.Length < 5)
                        {
                            MessageBox.Show("Functional space file error: " + Path.GetFileName(filename));
                            continue;
                        }
                    }
                }
                Mesh mesh = new Mesh(vposs.ToArray(), faceIds.ToArray());
                double diffw = maxw-minw;
                for (int i = 0; i < weights.Count; ++i)
                {
                    double ratio = (weights[i] - minw) / diffw;
                    ratio = Math.Min(ratio * 2, 1);
                    Color cg = GLDrawer.getColorGradient(ratio, fid);
                    mesh.setFaceColor(GLDrawer.getColorArray(cg, GLDrawer.FunctionalSpaceAlpha), i);
                }
                FunctionalSpace fs = new FunctionalSpace(mesh, weights.ToArray());
                return fs;
            }
        }// loadFunctionSpace

        public void loadFunctionalSpaceFiles(string[] filenames)
        {
            if (filenames == null || filenames.Length == 0 || _currModel == null || _currModel._SP == null)
            {
                return;
            }
            _currModel._funcSpaces = new FunctionalSpace[filenames.Length];
            for (int f = 0; f < filenames.Length; ++f)
            {
                string wname = filenames[f].Substring(0, filenames[f].LastIndexOf('.')) + ".weight";
                _currModel._funcSpaces[f] = this.loadFunctionalSpaceInfo(filenames[f], wname);
            }
        }// loadFunctionalSpaceFiles

        public void loadFunctionalPatchesWeight(string[] filenames)
        {
            if (filenames == null || filenames.Length == 0 || _currModel == null || _currModel._SP == null)
            {
                return;
            }
            double minw;
            double maxw;
            int npatches = filenames.Length;
            int npoints = _currModel._SP._points.Length;
            // multiple weights file w.r.t. patches
            double[,] weights_patches = new double[npatches, npoints];
            Color[,] colors_patches = new Color[npatches, npoints];
            _currModel._SP._blendColors = new Color[npoints];
            byte[] def_col = new byte[4]{255,255,255,GLDrawer.FunctionalSpaceColor.A};
            for (int i = 0; i < npoints; ++i)
            {
                _currModel._SP._blendColors[i] = Color.WhiteSmoke;
            }
            for (int w = 0; w < filenames.Length; ++w)
            {
                double[] weights = loadPatchWeight(filenames[w], out minw, out maxw);
                if (weights == null || weights.Length != npoints)
                {
                    MessageBox.Show("Weight file does not match sample file.");
                    continue;
                }
                double wdiff = maxw-minw;
                for (int i = 0; i < weights.Length; ++i)
                {
                    weights_patches[w, i] = weights[i];
                    double ratio = (weights[i] - minw) / wdiff;
                    if (ratio < 0.1)
                    {
                        continue;
                    }
                    Color color = GLDrawer.getColorGradient(ratio, w);
                    //mesh.setVertexColor(GLDrawer.getColorArray(color), i);
                    byte[] color_array = GLDrawer.getColorArray(color, 255);
                    _currModel._MESH.setFaceColor(color_array,  _currModel._SP._faceIdx[i]);
                    colors_patches[w, i] = GLDrawer.getColorRGB(color_array);
                    _currModel._SP._blendColors[i] = colors_patches[w, i];
                }
            }
            _currModel._SP._weights = weights_patches;
            _currModel._SP._colors = colors_patches;
        }// loadFunctionalPatchesWeight

        private SamplePoints loadSamplePoints(string filename, int totalNFaces)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            List<int> faceIndex = new List<int>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };
                List<Vector3d> points = new List<Vector3d>();
                List<Vector3d> normals = new List<Vector3d>();
                int nline = 0;
                while (sr.Peek() > -1)
                {
                    ++nline;
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separators);
                    if (strs.Length < 7)
                    {
                        MessageBox.Show("Wrong format at line " + nline.ToString());
                        return null;
                    }
                    // pos
                    points.Add(new Vector3d(double.Parse(strs[0]),
                        double.Parse(strs[1]),
                        double.Parse(strs[2])));
                    // normal
                    normals.Add(new Vector3d(double.Parse(strs[3]),
                        double.Parse(strs[4]),
                        double.Parse(strs[5])));
                    int fidx = int.Parse(strs[6]);
                    faceIndex.Add(fidx);
                }
                // colors
                string colorName = filename.Substring(0, filename.LastIndexOf("."));
                colorName += ".color";
                Color[] colors = loadSamplePointsColors(colorName);
                SamplePoints sp = new SamplePoints(points.ToArray(), normals.ToArray(),
                    faceIndex.ToArray(), colors, totalNFaces);
                // try to load point weights
                string name = filename.Substring(filename.LastIndexOf('\\'));
                name = name.Substring(0, name.LastIndexOf('.'));
                string folder = filename.Substring(0, filename.LastIndexOf('\\') + 1) + "points_weights_per_cat" + name + "\\";
                List<PatchWeightPerCategory> pws = this.loadSamplePointWeightsPerCategory(folder);
                sp._weightsPerCat = pws;
                return sp;
            }
        }// loadSamplePoints

        private void recomputeSamplePointsFaceIndex(SamplePoints sp, Mesh mesh)
        {
            if (sp == null || sp._points == null)
            {
                return;
            }
            List<Vector3d> points = new List<Vector3d>();
            List<Vector3d> normals = new List<Vector3d>();
            List<int> faceIdxs = new List<int>();
            double pi2 = 2 * Math.PI;
            for (int i = 0; i < sp._points.Length; ++i)
            {
                Vector3d p = sp._points[i];
                double mindToPoint = double.MaxValue;
                int tmp = sp._faceIdx[i];
                sp._faceIdx[i] = -1;
                for (int f = 0; f < mesh.FaceCount; ++f)
                {
                    Vector3d[] vs = new Vector3d[3];
                    for (int j = 0; j < 3; ++j)
                    {
                        int vid = mesh.FaceVertexIndex[f * 3 + j];
                        vs[j] = mesh.getVertexPos(vid);
                        double d = (p - vs[j]).Length();
                        if (d < mindToPoint)
                        {
                            mindToPoint = d;
                        }
                    }
                    // sum of angles
                    double angle = 0;
                    for (int j = 0; j < 3; ++j)
                    {
                        Vector3d v1 = (vs[j] - p).normalize();
                        Vector3d v2 = (vs[(j + 1) % 3] - p).normalize();
                        double ag = Math.Abs(Math.Acos(v1.Dot(v2)));
                        angle += ag;
                    }
                    if (Math.Abs(angle - pi2) < Common._thresh)
                    {
                        sp._faceIdx[i] = f;
                        double dToPlane = Common.PointDistToPlane(p, mesh.getFaceCenter(f), mesh.getFaceNormal(f));
                        if (dToPlane < Common._thresh)
                        {
                            sp._faceIdx[i] = f;
                        }
                    }
                }// each face
                if (sp._faceIdx[i] != -1)
                {
                    // could not find the tri face, remove it
                    points.Add(sp._points[i]);
                    normals.Add(sp._normals[i]);
                    faceIdxs.Add(sp._faceIdx[i]);
                }
            }// each point
            sp.reBuildFaceSamplePointsMap(points, normals, faceIdxs);
        }// recomputeSamplePointsFaceIndex

        private double[] loadPatchWeight(string filename, out double minw, out double maxw)
        {
            minw = double.MaxValue;
            maxw = double.MinValue;
            if (!File.Exists(filename))
            {
                return null;
            }
            List<double> weights = new List<double>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };
                int nline = 0;
                while (sr.Peek() > -1)
                {
                    ++nline;
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separators);
                    if (strs.Length == 0)
                    {
                        MessageBox.Show("Wrong format at line " + nline.ToString());
                        return null;
                    }
                    double w = double.Parse(strs[0]);
                    weights.Add(w);
                    minw = minw < w ? minw : w;
                    maxw = maxw > w ? maxw : w;
                }
            }
            return weights.ToArray();
        }// loadPatchWeight

        private Color[] loadSamplePointsColors(string filename)
        {
            if (!File.Exists(filename))
            {
                //MessageBox.Show("Sample point color file does not exist.");
                return null;
            }
            List<Color> colors = new List<Color>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separators);
                    if (strs.Length < 3)
                    {
                        MessageBox.Show("Wrong color file");
                        return null;
                    }
                    Color c = Color.FromArgb(byte.Parse(strs[0]), byte.Parse(strs[1]), byte.Parse(strs[2]));
                    if (c.R == 255 && c.G == 255 && c.B == 255)
                    {
                        c = Color.LightGray; // GLDrawer.MeshColor;
                    }
                    colors.Add(c);
                }
                return colors.ToArray();
            }
        }// loadSamplePointsColors

        public void loadPointWeight_0(string filename)
        {
            if (_currModel == null)
            {
                MessageBox.Show("Please load a model first.");
                return;
            }
            // load the point cloud with weights indicating the functionality patch
            // since it is not a segmented model, and most of the models are hard to segment
            // there is no need to take it as an original input and perform segmentation.
            // Unify the mesh, and compare the vertex to vertex distances between the point cloud with a segmented version
            List<Vector3d> points = new List<Vector3d>();
            List<double> weights = new List<double>();
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separators = { ' ', '\\', '\t' };
                string s = sr.ReadLine();
                int nline = 0;
                while (sr.Peek() > -1)
                {
                    ++nline;
                    string[] strs = s.Split(separators);
                    if (strs.Length < 4)
                    {
                        MessageBox.Show("Wrong format at line " + nline.ToString());
                        return;
                    }
                    Vector3d v = new Vector3d();
                    for (int i = 0; i < 3; ++i)
                    {
                        v[i] = double.Parse(strs[i]);
                    }
                    points.Add(v);
                    weights.Add(double.Parse(strs[3]));
                }
                convertFunctionalityDescription(points.ToArray(), weights.ToArray());
            }
        }// loadPointWeight

        public void loadFunctionalityModelsFromIcon(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }
            char[] separators = { ' ', '\\', '\t' };
            _functionalityModels = new List<FunctionalityModel>();
            using (StreamReader sr = new StreamReader(filename))
            {
                while (sr.Peek() > -1)
                {
                    string s = sr.ReadLine();
                    string[] strs = s.Split(separators);
                    string name = strs[0]; // category name
                    double[] fs = new double[strs.Length - 1];
                    for (int i = 1; i < strs.Length; ++i)
                    {
                        fs[i - 1] = double.Parse(strs[i]);
                    }
                    FunctionalityModel fm = new FunctionalityModel(fs, name);
                    if (fm != null)
                    {
                        _functionalityModels.Add(fm);
                    }
                }
            }
        }// loadFunctionalityModelsFromIcon

        public void loadFunctionalityModelsFromIcon2(string foldername)
        {
            string[] filenames = Directory.GetFiles(foldername);
            _functionalityModels = new List<FunctionalityModel>();
            foreach (string filename in filenames)
            {
                FunctionalityModel fm = loadOneFunctionalityModel(filename);
                if (fm != null)
                {
                    _functionalityModels.Add(fm);
                }
            }
        }// loadFunctionalityModelsFromIcon2

        private FunctionalityModel loadOneFunctionalityModel(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                string nameAndExt = Path.GetFileName(filename);
                string name = nameAndExt.Substring(0, nameAndExt.LastIndexOf('.'));
                char[] separators = { ' ', '\\', '\t' };
                string s = sr.ReadLine();
                string[] strs = s.Split(separators);
                double[] fs = new double[strs.Length];
                for (int i = 0; i < strs.Length; ++i)
                {
                    fs[i] = double.Parse(strs[i]);
                }
                FunctionalityModel fm = new FunctionalityModel(fs, name);
                return fm;
            }
        }// loadOneFunctionalityModel

        //######### END - Data & feature from ICON2 paper #########//

        private void setViewMatrix()
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

            //Gl.glPushMatrix();

            Glu.gluLookAt(this.eye.x, this.eye.y, this.eye.z, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0);

            Matrix4d transformMatrix = this.arcBall.getTransformMatrix(this.nPointPerspective);
            Matrix4d m = transformMatrix * this._currModelTransformMatrix;

            m = Matrix4d.TranslationMatrix(this.objectCenter) * m * Matrix4d.TranslationMatrix(
                new Vector3d() - this.objectCenter);

            foreach (ModelViewer mv in _ancesterModelViewers)
            {
                mv.setModelViewMatrix(m);
            }
            foreach (ModelViewer mv in _partViewers)
            {
                mv.setModelViewMatrix(m);
            }
            foreach (ModelViewer mv in _currGenModelViewers)
            {
                mv.setModelViewMatrix(m);
            }

            Gl.glMatrixMode(Gl.GL_MODELVIEW);

            Gl.glPushMatrix();
            Gl.glMultMatrixd(m.Transpose().ToArray());
        }

        private int startWid = 0, startHeig = 0;

        private void initScene()
        {
            Gl.glClearColor(1.0f, 1.0f, 1.0f, 0.0f);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

            SetDefaultMaterial();

            Gl.glLoadIdentity();
            SetDefaultLight();
        }

        private void DrawLight()
        {
            for (int i = 0; i < lightPositions.Count; ++i)
            {
                Vector3d pos3 = new Vector3d(lightPositions[i][0],
                    lightPositions[i][1],
                    lightPositions[i][2]);
                Vector3d pos2 = this.camera.Project(pos3.x, pos3.y, pos3.z);
                GLDrawer.DrawCircle2(new Vector2d(pos2.x, pos2.y), Color.Yellow, 0.2f);
            }
        }

        private void Draw2D()
        {
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();
            Glu.gluOrtho2D(0, this.Width, this.Height, 0);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPushMatrix();
            Gl.glLoadIdentity();


            if (this.isDrawQuad && this.highlightQuad != null)
            {
                GLDrawer.drawQuadTranslucent2d(this.highlightQuad, GLDrawer.SelectionColor);
            }

            //this.DrawHighlight2D();

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glPopMatrix();
        }

        private void Draw3D()
        {
            this.setViewMatrix();

            /***** Draw *****/

            if (this.isDrawAxes)
            {
                this.drawAxes(_axes, 3.0f);
            }

            if (this.isDrawGround)
            {
                drawGround();
            }

            //Gl.glEnable(Gl.GL_POLYGON_OFFSET_FILL);

            if (this.enableDepthTest)
            {
                Gl.glEnable(Gl.GL_DEPTH_TEST);
            }

            // Draw all meshes
            if (_currModel != null)
            {
                this.drawModel();
                if (_currModel._GRAPH != null && this.isDrawGraph)
                {
                    this.drawGraph(_currModel._GRAPH);
                }
            }

            this.drawCurrentMesh();

            this.drawImportMeshes();

            this.drawHumanPose();

            this.DrawHighlight3D();

            if (this.enableDepthTest)
            {
                Gl.glDisable(Gl.GL_DEPTH_TEST);
            }

            Gl.glDisable(Gl.GL_POLYGON_OFFSET_FILL);
            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glPopMatrix();

        }// Draw3D   

        private void drawGround()
        {
            //GLDrawer.drawPlane(_groundPlane, Color.LightGray);
            // draw grids
            if (_groundGrids != null)
            {
                for (int i = 0; i < _groundGrids.Length; i += 2)
                {
                    GLDrawer.drawLines3D(_groundGrids[i], _groundGrids[i + 1], Color.Gray, 2.0f);
                }
            }
        }// isDrawGround

        private void drawGraph(Graph g)
        {
            foreach (Node node in g._NODES)
            {
                GLDrawer.drawSphere(node._pos, 0.05, node._PART._COLOR);
            }
            foreach (Edge e in g._EDGES)
            {
                GLDrawer.drawLines3D(e._start._pos, e._end._pos, Color.Gray, 2.0f);
            }
        }// drawGraph

        private void drawCurrentMesh()
        {
            if (this.currMeshClass == null || _currModel != null)
            {
                return;
            }
            if (this.currMeshClass != null)
            {
                if (this.isDrawMesh)
                {
                    GLDrawer.drawMeshFace(currMeshClass.Mesh, Color.White, false);
                    //GLDrawer.drawMeshFace(currMeshClass.Mesh, GLDrawer.MeshColor, false);
                    //GLDrawer.drawMeshFace(currMeshClass.Mesh);
                }
                if (this.drawEdge)
                {
                    currMeshClass.renderWireFrame();
                }
                if (this.drawVertex)
                {
                    if (currMeshClass.Mesh.VertexColor != null && currMeshClass.Mesh.VertexColor.Length > 0)
                    {
                        currMeshClass.renderVertices_color();
                    }
                    else
                    {
                        currMeshClass.renderVertices();
                    }
                }
                currMeshClass.drawSamplePoints();
                currMeshClass.drawSelectedVertex();
                currMeshClass.drawSelectedEdges();
                if (_currModel != null && _currModel.pointsTest.Count == 0)
                {
                    currMeshClass.drawSelectedFaces();
                }
            }
        }// drawCurrentMesh

        private void drawImportMeshes()
        {
            if (_currModel != null || _meshClasses.Count < 2) // the current mesh is already drawn if there is only one
            {
                return;
            }
            int i = 0;
            foreach (MeshClass mc in this._meshClasses)
            {
                Color ic = GLDrawer.ColorSet[i];
                Color c = ic;
                if (i > 0)
                {
                    c = Color.FromArgb(50, 0, 0, 255);
                    GLDrawer.drawMeshFace(mc.Mesh, c);
                }
                else
                {
                    GLDrawer.drawMeshFace(mc.Mesh, c, false);
                }
                //GLDrawer.drawMeshFace(mc.Mesh, c, false);
                ++i;
            }
        }

        private void drawModel()
        {
            if (_currModel == null)
            {
                return;
            }
            // draw mesh
            if (_currModel._MESH != null && _currModel._NPARTS == 0)
            {
                GLDrawer.drawMeshFace(_currModel._MESH, GLDrawer.MeshColor, false);
            }
            //if (_currModel.pointsTest.Count > 0)
            //{
            //    GLDrawer.drawPoints(_currModel.pointsTest.ToArray(), Color.Purple, 6.0f);
            //}
            //return;
            // draw parts
            if (_currModel._PARTS != null)
            {
                drawParts(_currModel._PARTS);
            }
            if (this.isDrawModelSamplePoints && _currModel._SP != null && _currModel._SP._points != null)
            {
                GLDrawer.drawPoints(_currModel._SP._points, _currModel._SP._blendColors, 10.0f);
                //GLDrawer.drawPoints(_currModel._SP.testVisiblePoints.ToArray(), _currModel._SP._blendColors, 10.0f);
                // draw normal
                GLDrawer.drawLines3D(_currModel._SP.testNormals, Color.Blue, 2.0f);
            }
            if (this.drawVertex)
            {
                GLDrawer.drawLines3D(_currModel._MESH.testNormals, Color.Blue, 2.0f);
            }
            // draw functional space
            if (this.isDrawFuncSpace && _currModel._SP != null && _currModel._funcSpaces != null && _fsIdx < _currModel._funcSpaces.Length)
            {
                FunctionalSpace fs = _currModel._funcSpaces[_fsIdx];
                GLDrawer.drawMeshFace(fs._mesh);
            }

            if (this.isDrawFunctionalSpaceAgent && _currModel._GRAPH != null)
            {
                foreach(Node node in _currModel._GRAPH._NODES)
                {
                    if (node._functionalSpaceAgent != null)
                    {
                        GLDrawer.drawBoundingboxPlanes(node._functionalSpaceAgent, GLDrawer.BodyColor);
                    }
                }
            }
        }// drawModel

        private void drawParts(List<Part> parts)
        {
            foreach (Part part in parts)
            {
                if (_selectedParts.Contains(part) || _userSelectedParts.Contains(part))
                {
                    continue;
                }
                bool isSelected = _selectedEdge != null && (_selectedEdge._start._PART == part || _selectedEdge._end._PART == part);
                if (this.isDrawMesh)
                {
                    if (isSelected)
                    {
                        GLDrawer.drawMeshFace(part._MESH, GLDrawer.HighlightBboxColor, false);
                    }
                    else if (this.isDrawPartSamplePoints)
                    {
                        GLDrawer.drawMeshFace(part._MESH, GLDrawer.MeshColor, false);
                    } 
                    else if (_categoryId == -1) {
                        GLDrawer.drawMeshFace(part._MESH, part._COLOR, false);
                    }
                    else
                    {
                        GLDrawer.drawMeshFace(part._MESH, part._highlightColors[_categoryId], false);
                    }
                    //GLDrawer.drawMeshFace(part._MESH, GLDrawer.MeshColor, false);
                }
                if (this.drawEdge)
                {
                    GLDrawer.drawMeshEdge(part._MESH, GLDrawer.ColorSet[1]);
                }
                if (this.drawVertex)
                {
                    GLDrawer.drawMeshVertices(part._MESH);
                    //if (part._MESH.VertexColor != null && part._MESH.VertexColor.Length > 0)
                    //{
                    //    GLDrawer.drawMeshVertices_color(part._MESH);
                    //}
                    //else
                    //{
                    //    GLDrawer.drawMeshVertices(part._MESH);
                    //}
                }
                if (this.isDrawBbox)
                {
                    if (isSelected)
                    {
                        GLDrawer.drawBoundingboxPlanes(part._BOUNDINGBOX, GLDrawer.HighlightBboxColor);
                    }
                    else
                    {
                        GLDrawer.drawBoundingboxPlanes(part._BOUNDINGBOX, part._COLOR);
                    }
                    if (part._BOUNDINGBOX.type == Common.PrimType.Cuboid)
                    {
                        GLDrawer.drawBoundingboxEdges(part._BOUNDINGBOX, part._COLOR);
                    }
                }
                if (this.isDrawPartFunctionalSpacePrimitive && part._functionalSpacePrims.Count > 0)
                {
                    foreach (Prism fs in part._functionalSpacePrims)
                    {
                        GLDrawer.drawBoundingboxPlanes(fs, part._COLOR);
                    }
                }
            }// each part
            if (this.isDrawPartSamplePoints)
            {
                foreach (Part part in parts)
                {
                    if (part._partSP != null && part._partSP._points != null)
                    {
                        if (_selectedParts != null && _selectedParts.Contains(part))
                        {
                            GLDrawer.drawPoints(part._partSP._points, Color.Red, 12.0f);
                        }
                        else
                        {
                            GLDrawer.drawPoints(part._partSP._points, part._COLOR, 12.0f);
                        }
                    }
                }
            }
        }//drawParts

        private void drawHumanPose()
        {
            if (_humanposes.Count == 0)
            {
                return;
            }
            Gl.glPushAttrib(Gl.GL_COLOR_BUFFER_BIT);
            int iMultiSample = 0;
            int iNumSamples = 0;
            Gl.glGetIntegerv(Gl.GL_SAMPLE_BUFFERS, out iMultiSample);
            Gl.glGetIntegerv(Gl.GL_SAMPLES, out iNumSamples);

            Gl.glEnable(Gl.GL_DEPTH_TEST);
            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
            Gl.glEnable(Gl.GL_POLYGON_SMOOTH);
            Gl.glHint(Gl.GL_POLYGON_SMOOTH_HINT, Gl.GL_NICEST);
            Gl.glHint(Gl.GL_LINE_SMOOTH_HINT, Gl.GL_NICEST);

            if (iNumSamples == 0 && _isDrawTranslucentHumanPose)
            {
                Gl.glEnable(Gl.GL_BLEND);
                Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
                Gl.glShadeModel(Gl.GL_SMOOTH);
                Gl.glDepthMask(Gl.GL_FALSE);
            }
            else
            {
                Gl.glEnable(Gl.GL_MULTISAMPLE);
                Gl.glHint(Gl.GL_MULTISAMPLE_FILTER_HINT_NV, Gl.GL_NICEST);
                Gl.glEnable(Gl.GL_SAMPLE_ALPHA_TO_ONE);
            }
            foreach (HumanPose hp in _humanposes)
            {
                foreach (BodyBone bb in hp._bodyBones)
                {
                    if (_isDrawTranslucentHumanPose)
                    {
                        GLDrawer.drawCylinderTranslucent(bb._SRC._POS, bb._DST._POS, Common._bodyNodeRadius / 2, GLDrawer.BodeyBoneColor);
                        for (int i = 0; i < bb._FACEVERTICES.Length; i += 4)
                        {
                            GLDrawer.drawQuadTranslucent3d(bb._FACEVERTICES[i], bb._FACEVERTICES[i + 1],
                                bb._FACEVERTICES[i + 2], bb._FACEVERTICES[i + 3], GLDrawer.TranslucentBodyColor);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < bb._FACEVERTICES.Length; i += 4)
                        {
                            GLDrawer.drawQuadSolid3d(bb._FACEVERTICES[i], bb._FACEVERTICES[i + 1],
                                bb._FACEVERTICES[i + 2], bb._FACEVERTICES[i + 3], GLDrawer.BodyColor);
                        }
                    }
                }
                foreach (BodyNode bn in hp._bodyNodes)
                {
                    if (bn != _selectedNode)
                    {
                        GLDrawer.drawSphere(bn._POS, bn._RADIUS, GLDrawer.BodyNodeColor);
                    }
                }
            }
            if (iNumSamples == 0 && _isDrawTranslucentHumanPose)
            {
                Gl.glDisable(Gl.GL_BLEND);
                Gl.glDisable(Gl.GL_POLYGON_SMOOTH);
                Gl.glDepthMask(Gl.GL_TRUE);
            }
            else
            {
                Gl.glDisable(Gl.GL_MULTISAMPLE);
            }
            Gl.glDisable(Gl.GL_DEPTH_TEST);
            Gl.glPopAttrib();
        }// drawHumanPose

        private void DrawHighlight2D()
        {
        }

        private void DrawHighlight3D()
        {
            if (this._selectedParts != null)
            {
                foreach (Part part in _selectedParts)
                {
                    if (this.isDrawMesh)
                    {
                        GLDrawer.drawMeshFace(part._MESH, GLDrawer.SelectionColor, false);
                    }
                    GLDrawer.drawBoundingboxPlanes(part._BOUNDINGBOX, GLDrawer.SelectionColor);
                    GLDrawer.drawBoundingboxEdges(part._BOUNDINGBOX, GLDrawer.SelectionColor);
                }
            }
            if (_selectedNode != null)
            {
                GLDrawer.drawSphere(_selectedNode._POS, _selectedNode._RADIUS, GLDrawer.SelectedBodyNodeColor);
            }

            if (_showEditAxes)
            {
                this.drawAxes(_editAxes, 4.0f);
            }

            if (_showContactPoint)
            {
                if (_currModel != null && _currModel._GRAPH != null)
                {
                    foreach (Edge e in _currModel._GRAPH._EDGES)
                    {
                        foreach (Contact c in e._contacts)
                        {
                            if (e == _selectedEdge && c == _selectedContact)
                            {
                                GLDrawer.drawSphere(c._pos3d, Common._hightlightContactPointsize, GLDrawer.HightLightContactColor);
                            }
                            else
                            {
                                GLDrawer.drawSphere(c._pos3d, Common._contactPointsize, GLDrawer.ContactColor);
                            }
                        }
                    }
                }
            }

            if (_selectedEdge != null)
            {
                // hightlight the nodes
                GLDrawer.drawBoundingboxPlanes(_selectedEdge._start._PART._BOUNDINGBOX, GLDrawer.HighlightBboxColor);
                GLDrawer.drawBoundingboxPlanes(_selectedEdge._end._PART._BOUNDINGBOX, GLDrawer.HighlightBboxColor);
            }
            if (_pgPairVisualization != null)
            {
                foreach (Part part in _pgPairVisualization)
                {
                    GLDrawer.drawMeshFace(part._MESH, part._COLOR, false);
                }
            }

            if (_categoryId != -1)
            {

            }

            foreach(Part part in _userSelectedParts)
            {                
                GLDrawer.drawMeshFace(part._MESH, GLDrawer.HighlightMeshColor, false);
            }
        }// DrawHighlight3D

        private void drawAxes(Vector3d[] axes, float wid)
        {
            // draw axes with arrows
            for (int i = 0; i < 6; i += 2)
            {
                GLDrawer.drawLines3D(axes[i], axes[i + 1], _hightlightAxis == 0 ? Color.Yellow : Color.Red, wid);
            }

            for (int i = 6; i < 12; i += 2)
            {
                GLDrawer.drawLines3D(axes[i], axes[i + 1], _hightlightAxis == 1 ? Color.Yellow : Color.Green, wid);
            }

            for (int i = 12; i < 18; i += 2)
            {
                GLDrawer.drawLines3D(axes[i], axes[i + 1], _hightlightAxis == 2 ? Color.Yellow : Color.Blue, wid);
            }
        }// drawAxes

        private void drawAxes(Contact[] axes, float wid)
        {
            // draw axes with arrows
            for (int i = 0; i < 6; i += 2)
            {
                GLDrawer.drawLines3D(axes[i]._pos3d, axes[i + 1]._pos3d, _hightlightAxis == 0 ? Color.Yellow : Color.Red, wid);
            }

            for (int i = 6; i < 12; i += 2)
            {
                GLDrawer.drawLines3D(axes[i]._pos3d, axes[i + 1]._pos3d, _hightlightAxis == 1 ? Color.Yellow : Color.Green, wid);
            }

            for (int i = 12; i < 18; i += 2)
            {
                GLDrawer.drawLines3D(axes[i]._pos3d, axes[i + 1]._pos3d, _hightlightAxis == 2 ? Color.Yellow : Color.Blue, wid);
            }
        }// drawAxes

        // Lights & Materials
        public static float[] matAmbient = { 0.1f, 0.1f, 0.1f, 1.0f };
        public static float[] matDiffuse = { 0.7f, 0.7f, 0.5f, 1.0f };
        public static float[] matSpecular = { 1.0f, 1.0f, 1.0f, 1.0f };
        public static float[] shine = { 120.0f };

        private static void SetDefaultLight()
        {
            float[] pos1 = new float[4] { 0.1f, 0.1f, -0.02f, 0.0f };
            float[] pos2 = new float[4] { -0.1f, 0.1f, -0.02f, 0.0f };
            float[] pos3 = new float[4] { 0.0f, 0.0f, 0.1f, 0.0f };
            float[] col1 = new float[4] { 0.7f, 0.7f, 0.7f, 1.0f };
            float[] col2 = new float[4] { 0.8f, 0.7f, 0.7f, 1.0f };
            float[] col3 = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };


            Gl.glEnable(Gl.GL_LIGHT0);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_POSITION, pos1);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, col1);
            Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_SPECULAR, col1);


            Gl.glEnable(Gl.GL_LIGHT1);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_POSITION, pos2);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_DIFFUSE, col2);
            Gl.glLightfv(Gl.GL_LIGHT1, Gl.GL_SPECULAR, col2);


            Gl.glEnable(Gl.GL_LIGHT2);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_POSITION, pos3);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_DIFFUSE, col3);
            Gl.glLightfv(Gl.GL_LIGHT2, Gl.GL_SPECULAR, col3);
        }

        public void AddLight(Vector3d pos, Color col)
        {
            int lightID = lightPositions.Count + 16387;
            float[] posA = new float[4] { (float)pos.x, (float)pos.y, (float)pos.z, 0.0f };
            lightPositions.Add(posA);
            float[] colA = new float[4] { col.R / 255.0f, col.G / 255.0f, col.B / 255.0f, 1.0f };
            lightcolors.Add(colA);
            lightIDs.Add(lightID);
        }
        private static void SetDefaultMaterial()
        {
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_AMBIENT, matAmbient);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_DIFFUSE, matDiffuse);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SPECULAR, matSpecular);
            Gl.glMaterialfv(Gl.GL_FRONT_AND_BACK, Gl.GL_SHININESS, shine);
        }

        public static List<float[]> lightPositions = new List<float[]>();
        public static List<float[]> lightcolors = new List<float[]>();
        public static List<int> lightIDs = new List<int>();
        private static void SetAdditionalLight()
        {
            if (lightPositions.Count == 0)
            {
                return;
            }
            for (int i = 0; i < lightPositions.Count; ++i)
            {
                Gl.glEnable(lightIDs[i]);
                Gl.glLightfv(lightIDs[i], Gl.GL_POSITION, lightPositions[i]);
                Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, lightcolors[i]);
                Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_SPECULAR, lightcolors[i]);
            }
        }
    }// GLViewer
}// namespace
