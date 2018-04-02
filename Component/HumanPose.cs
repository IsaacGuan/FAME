using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Geometry;

namespace Component
{
    public class HumanPose
    {
        public List<BodyNode> _bodyNodes;
        public List<BodyBone> _bodyBones;
        BodyNode _root;

        public HumanPose() { }

        public HumanPose(List<BodyNode> nodes, List<BodyBone> bones)
        {
            _bodyNodes = nodes;
            _bodyBones = bones;
            BuildTree();
        }

        public BodyNode _ROOT
        {
            get
            {
                return _root;
            }
        }

        public void BuildTree()
        {
            if (_root == null)
            {
                return;
            }
            Queue<BodyNode> Q = new Queue<BodyNode>();
            Q.Enqueue(_root);
            List<BodyNode> tagged = new List<BodyNode>();
            tagged.Add(_root);
            while (Q.Count > 0)
            {
                BodyNode node = Q.Dequeue();
                List<BodyBone> adjBones = node.getAdjBones();
                foreach (BodyBone bone in adjBones)
                {
                    BodyNode other = bone._SRC == node ? bone._DST : bone._SRC;
                    if (tagged.Contains(other))
                    {
                        continue;
                    }
                    other._PARENT = node;
                    node.addChildNode(other);
                    Q.Enqueue(other);
                    tagged.Add(other);
                }
            }
        }// BuildTree

        public void savePose(string filename)
        {
            // format:
            // # nodes
            // node name, x, y, z
            // # bones
            // bone name, ith node, jth node, wid, thickness
            if (_bodyNodes == null || _bodyBones == null)
            {
                return;
            }
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.WriteLine(_bodyNodes.Count.ToString() + " body nodes.");
                foreach (BodyNode bn in _bodyNodes)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(bn._NAME);
                    sb.Append(" " + bn._POS.x.ToString());
                    sb.Append(" " + bn._POS.y.ToString());
                    sb.Append(" " + bn._POS.z.ToString());
                    sw.WriteLine(sb.ToString());
                }
                sw.WriteLine(_bodyBones.Count.ToString() + " body bones.");
                foreach (BodyBone bb in _bodyBones)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(bb._NAME);
                    int id1 = _bodyNodes.IndexOf(bb._SRC);
                    int id2 = _bodyNodes.IndexOf(bb._DST);
                    sb.Append(" " + id1.ToString());
                    sb.Append(" " + id2.ToString());
                    sb.Append(" " + bb._WIDTH.ToString());
                    sb.Append(" " + bb._THICKNESS.ToString());
                    sw.WriteLine(sb.ToString());
                }
            }// write
        }// saveHumanPose

        public void loadPose(string filename)
        {
            if (!File.Exists(filename))
            {
                return;
            }
            using (StreamReader sr = new StreamReader(filename))
            {
                char[] separator = { ' ', '\t' };
                string line = sr.ReadLine().Trim();
                string[] strs = line.Split(separator);
                int nnodes = 0;
                try
                {
                    nnodes = Int16.Parse(strs[0]);
                }
                catch (System.FormatException)
                {
                    return;
                }
                _bodyNodes = new List<BodyNode>();
                for (int i = 0; i < nnodes; ++i)
                {
                    line = sr.ReadLine().Trim();
                    strs = line.Split(separator);
                    string name = strs[0];
                    Vector3d pos = new Vector3d(double.Parse(strs[1]), double.Parse(strs[2]), double.Parse(strs[3]));
                    BodyNode bn = new BodyNode(name, pos);
                    _bodyNodes.Add(bn);
                    if (bn._NAME == "body_hip")
                    {
                        bn.setAsRoot();
                        _root = bn;
                    }
                }
                int nbones = 0;
                line = sr.ReadLine().Trim();
                strs = line.Split(separator);
                try
                {
                    nbones = Int16.Parse(strs[0]);
                }
                catch (System.FormatException)
                {
                    return;
                }
                _bodyBones = new List<BodyBone>();
                for (int i = 0; i < nbones; ++i)
                {
                    line = sr.ReadLine().Trim();
                    strs = line.Split(separator);
                    string name = strs[0];
                    int inode = Int16.Parse(strs[1]);
                    int jnode = Int16.Parse(strs[2]);
                    double wid = double.Parse(strs[3]);
                    double thi = double.Parse(strs[4]);
                    BodyBone bb = new BodyBone(_bodyNodes[inode], _bodyNodes[jnode], name, wid, thi, 20);
                    _bodyBones.Add(bb);
                }
                BuildTree();
            }// read
        }// loadHuamPose

        public void Transform(Matrix4d T)
        {
            foreach (BodyNode bn in _bodyNodes)
            {
                bn.Transform(T);
            }
        }// Transform

        public void TransformFromOrigin(Matrix4d T)
        {
            foreach (BodyNode bn in _bodyNodes)
            {
                bn.TransformFromOrigin(T);
            }
        }// Transform

        public void updateOriginPos()
        {
            foreach (BodyNode bn in _bodyNodes)
            {
                bn.updateOriginPos();
            }
        }// updateOriginPos
    }// HumanPose

    public class BodyNode
    {
        // represent by a solid sphere
        string _name;
        Vector3d _originPos;
        Vector3d _pos;
        bool _isRoot = false; // move all nodes together
        double _radius = Common._bodyNodeRadius;
        List<BodyNode> _adjNodes = new List<BodyNode>();
        List<BodyNode> _childrenNodes = new List<BodyNode>();
        List<BodyBone> _adjBones = new List<BodyBone>();
        BodyNode _parentNode;
        public Vector2d _pos2; // for selection

        public BodyNode(string name, Vector3d v)
        {
            _name = name;
            _pos = new Vector3d(v);
            _originPos = new Vector3d(v);
        }

        public Vector3d _POS
        {
            get
            {
                return _pos;
            }
            set
            {
                _pos = value;
            }
        }

        public Vector3d _ORIGIN
        {
            get
            {
                return _originPos;
            }
        }

        public string _NAME
        {
            get
            {
                return _name;
            }
        }

        public double _RADIUS
        {
            get
            {
                return _radius;
            }
        }

        public BodyNode _PARENT
        {
            get
            {
                return _parentNode;
            }
            set
            {
                _parentNode = value;
            }
        }

        public void setAsRoot()
        {
            _isRoot = true;
        }

        public bool isRoot()
        {
            return _isRoot;
        }

        public void addAdjNode(BodyNode node)
        {
            _adjNodes.Add(node);
        }

        public void addChildNode(BodyNode node)
        {
            _childrenNodes.Add(node);
            node._PARENT = this;
        }

        public void addAdjBone(BodyBone bone)
        {
            _adjBones.Add(bone);
        }

        public List<BodyNode> getAdjNodes()
        {
            return _adjNodes;
        }

        public List<BodyNode> getChildrenNodes()
        {
            return _childrenNodes;
        }

        public List<BodyBone> getAdjBones()
        {
            return _adjBones;
        }

        public List<BodyNode> getDescendents()
        {
            List<BodyNode> descendents = new List<BodyNode>();
            if (_childrenNodes != null)
            {
                Queue<BodyNode> Q = new Queue<BodyNode>();
                foreach (BodyNode bn in _childrenNodes)
                {
                    Q.Enqueue(bn);
                }
                while (Q.Count > 0)
                {
                    BodyNode node = Q.Dequeue();
                    if (!descendents.Contains(node))
                    {
                        descendents.Add(node);
                    }
                    if (node._childrenNodes != null)
                    {
                        foreach (BodyNode bn in node._childrenNodes)
                        {
                            Q.Enqueue(bn);
                        }
                    }
                }
            }
            return descendents;
        }// getDescendents

        public void Transform(Matrix4d T)
        {
            _pos = (T * new Vector4d(_pos, 1)).ToVector3D();
        }

        public void TransformFromOrigin(Matrix4d T)
        {
            _pos = (T * new Vector4d(_originPos, 1)).ToVector3D();
        }

        public void TransformOrigin(Matrix4d T)
        {
            _originPos = (T * new Vector4d(_originPos, 1)).ToVector3D();
        }

        public void updateOriginPos()
        {
            _originPos = new Vector3d(_pos);
        }
    }// BodyNode

    public class BodyBone
    {
        // represent by a cylinder + ellipsoid
        BodyNode _src;
        BodyNode _dst;
        string _name;
        double _radius = 0.01; // of the cyclinder
        Ellipsoid _entity;
        double _len = 0.002;
        double _wid = 0.002;
        double _thickness = 0.002;
        int _nslices;
        Vector3d[] _faceVertices;

        public BodyBone(BodyNode s, BodyNode d, string name)
        {
            _src = s;
            _dst = d;
            _name = name;
            _src.addAdjBone(this);
            _dst.addAdjBone(this);
            _entity = new Ellipsoid(_len, _wid, _thickness, _nslices);
            updateEntity();
        }

        public BodyBone(BodyNode s, BodyNode d, string name, double w, double th, int n)
        {
            _src = s;
            _dst = d;
            _name = name;
            _src.addAdjBone(this);
            _dst.addAdjBone(this);
            _len = (_src._POS - _dst._POS).Length();
            _wid = w;
            _thickness = th;
            _nslices = n;
            _entity = new Ellipsoid(_len, _wid, _thickness, _nslices);
            updateEntity();
        }

        public BodyNode _SRC
        {
            get
            {
                return _src;
            }
        }

        public BodyNode _DST
        {
            get
            {
                return _dst;
            }
        }

        public string _NAME
        {
            get
            {
                return _name;
            }
        }

        public double _LENGTH
        {
            get
            {
                return _len;
            }
        }

        public double _WIDTH
        {
            get
            {
                return _wid;
            }
        }

        public double _THICKNESS
        {
            get
            {
                return _thickness;
            }
        }

        public double _RADIUS
        {
            get
            {
                return _radius;
            }
        }

        public Vector3d[] _FACEVERTICES
        {
            get
            {
                return _faceVertices;
            }
        }

        public void updateEntity()
        {
            // body nodes have been _updated
            _entity.create(_src._POS, _dst._POS);
            _faceVertices = _entity.getFaceVertices();
        }
    }// BodyBone
}
