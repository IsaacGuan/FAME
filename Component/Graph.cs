using System;
using System.Collections.Generic;

using Geometry;

namespace Component
{
    /* topological information of a model */
    public class Graph
    {
        List<Node> _nodes = new List<Node>();
        List<Edge> _edges = new List<Edge>();
        public int _NEdges = 0;
        public FunctionalityFeatures _functionalityValues = null;
        public List<PartGroup> _partGroups = new List<PartGroup>();
        public List<Node> selectedNodes = new List<Node>();

        public Vector3d _centerOfMass;

        double _maxNodeBboxScale; // max scale of a box
        double _minNodeBboxScale; // min scale of a box
        double _maxAdjNodesDist; // max distance between two nodes
        
        private List<Functionality.Functions> _origin_funcs = new List<Functionality.Functions>();

        // test
        public List<List<Node>> selectedNodePairs = new List<List<Node>>();

        public Graph() { }

        public Graph(List<Part> parts)
        {
            _nodes = new List<Node>();
            for (int i = 0; i < parts.Count; ++i)
            {
                _nodes.Add(new Node(parts[i], i));
            }
            markGroundTouchingNodes();
            buildGraph();
        }

        public void init()
        {
            analyzeOriginFeatures();
            //computeFeatures();
        }// init

        public Mesh composeMesh()
        {
            List<double> vertexPos = new List<double>();
            List<int> faceIndex = new List<int>();
            int start_v = 0;
            int start_f = 0;
            foreach (Node node in _nodes)
            {
                Mesh mesh = node._PART._MESH;
                node._PART._VERTEXINDEX = new int[mesh.VertexCount];
                node._PART._FACEVERTEXINDEX = new int[mesh.FaceCount];

                // vertex
                for (int i = 0; i < mesh.VertexCount; ++i)
                {
                    Vector3d ipos = mesh.getVertexPos(i);
                    vertexPos.Add(ipos.x);
                    vertexPos.Add(ipos.y);
                    vertexPos.Add(ipos.z);
                    node._PART._VERTEXINDEX[i] = start_v + i;
                }
                start_v += mesh.VertexCount;
                // face
                for (int i = 0, j = 0; i < mesh.FaceCount; ++i)
                {
                    faceIndex.Add(mesh.FaceVertexIndex[j++]);
                    faceIndex.Add(mesh.FaceVertexIndex[j++]);
                    faceIndex.Add(mesh.FaceVertexIndex[j++]);
                    node._PART._FACEVERTEXINDEX[i] = start_f + i;
                }
                start_f += mesh.VertexCount;
            }
            return new Mesh(vertexPos.ToArray(), faceIndex.ToArray());
        }// composeMesh       

        public List<Functionality.Functions> getGraphFuncs()
        {
            List<Functionality.Functions> funcs = new List<Functionality.Functions>();
            foreach (Node node in _nodes)
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
        }// getGraphFuncs

        public Object Clone(List<Part> parts)
        {
            if (_nodes.Count != parts.Count)
            {
                return null;
            }
            Graph cloned = new Graph();
            for (int i = 0; i < _NNodes; ++i)
            {
                Node cn = _nodes[i].Clone(parts[i]) as Node;
                cloned.addANode(cn);
            }
            for (int i = 0; i < _nodes.Count; ++i)
            {
                if (_nodes[i].symmetry != null)
                {
                    int idx = _nodes[i].symmetry._INDEX;
                    if (idx > i)
                    {
                        cloned.markSymmtry(cloned._nodes[i], cloned._nodes[idx]);
                    }
                }
            }
            foreach (Edge e in _edges)
            {
                int i = e._start._INDEX;
                int j = e._end._INDEX;
                List<Contact> contacts = new List<Contact>();
                foreach (Contact pnt in e._contacts)
                {
                    contacts.Add(new Contact(pnt._pos3d));
                }
                Edge ec = new Edge(cloned._nodes[i], cloned._nodes[j], contacts);
                cloned.addEdge(ec);
            }
            cloned._NEdges = cloned._edges.Count;
            cloned._maxAdjNodesDist = _maxAdjNodesDist;
            cloned._minNodeBboxScale = _minNodeBboxScale;
            cloned._maxNodeBboxScale = _maxNodeBboxScale;
            cloned._origin_funcs = new List<Functionality.Functions>(_origin_funcs);
            return cloned;
        }// clone

        public void analyzeOriginFeatures()
        {
            //double[] vals = calScale();
            //_maxAdjNodesDist = vals[0];
            //_minNodeBboxScale = vals[1];
            //_maxNodeBboxScale = vals[2];
            _origin_funcs = this.getGraphFuncs();
        }// analyzeOriginFeatures

        private double[] calScale()
        {
            double[] vals = new double[3];
            double maxd = double.MinValue;
            foreach (Edge e in _edges)
            {
                Node n1 = e._start;
                Node n2 = e._end;
                Vector3d contact;
                double dist = getDistBetweenMeshes(n1._PART._MESH, n2._PART._MESH, out contact);
                if (dist > maxd)
                {
                    maxd = dist;
                }
            }
            double minScale = double.MaxValue;
            double maxScale = double.MinValue;
            foreach (Node node in _nodes)
            {
                node._PART._BOUNDINGBOX.setMaxMinScaleFromMesh(node._PART._MESH.MaxCoord, node._PART._MESH.MinCoord);
                for (int i = 0; i < 3; ++i)
                {
                    if (minScale > node._PART._BOUNDINGBOX._scale[i])
                    {
                        minScale = node._PART._BOUNDINGBOX._scale[i];
                    }
                    if (maxScale < node._PART._BOUNDINGBOX._scale[i])
                    {
                        maxScale = node._PART._BOUNDINGBOX._scale[i];
                    }
                }
            }
            vals[0] = maxd;
            vals[1] = minScale;
            vals[2] = maxScale;
            return vals;
        }// calScale

        public List<Node> getNodesByUniqueFunctionality(Functionality.Functions func)
        {
            List<Node> nodes = new List<Node>();
            foreach (Node node in _nodes)
            {
                if (node._funcs.Count == 1 && !nodes.Contains(node) && node._funcs.Contains(func))
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }// getNodesByUniqueFunctionality

        public List<Node> getNodesByFunctionality(Functionality.Functions func)
        {
            List<Node> nodes = new List<Node>();
            foreach (Node node in _nodes)
            {
                if (!nodes.Contains(node) && node._funcs.Contains(func))
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }// getNodesByFunctionality

        public List<Node> getNodesAndDependentsByFunctionality(Functionality.Functions func)
        {
            List<Node> nodes = new List<Node>();
            foreach (Node node in _nodes)
            {
                if (!nodes.Contains(node) && node._funcs.Contains(func))
                {
                    nodes.Add(node);
                }
            }
            nodes = this.bfs_regionGrowingNonFunctionanlNodes(nodes);
            return nodes;
        }// getNodesAndDependentsByFunctionality

        public List<Node> getNodesAndDependentsByFunctionality(List<Functionality.Functions> funcs)
        {
            // sample functional parts (connected!)
            List<Node> nodes = new List<Node>();
            foreach (Functionality.Functions f in funcs)
            {
                List<Node> cur = getNodesAndDependentsByFunctionality(f);
                foreach (Node node in cur)
                {
                    if (!nodes.Contains(node))
                    {
                        nodes.Add(node);
                    }
                }
            }
            return nodes;
        }// getNodesAndDependentsByFunctionality

        public List<Node> getMainNodes()
        {
            // sample functional parts (connected!)
            List<Node> nodes = new List<Node>();
            foreach (Node node in _nodes)
            {
                if (Functionality.ContainsMainFunction(node._funcs))
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }// getMainNodes

        public void replaceNodes(List<Node> oldNodes, List<Node> newNodes)
        {
            // replace the old nodes from this graph by newNodes
            // UPDATE nodes first            
            foreach (Node old in oldNodes)
            {
                _nodes.Remove(old);
            }
            List<Node> nodes_in_oppo_list = getOutConnectedNodes(oldNodes);
            foreach (Node node in newNodes)
            {
                _nodes.Add(node);
            }
            resetNodeIndex();

            // UPDATE edges
            // 1.1 remove inner edges from oldNodes
            List<Edge> inner_edges_old = GetInnerEdges(oldNodes);
            foreach (Edge e in inner_edges_old)
            {
                this.deleteEdge(e);
            }

            List<Edge> out_old_edges = getOutgoingEdges(oldNodes);
            List<Edge> out_new_edges = getOutgoingEdges(newNodes);
            List<Node> out_nodes = new List<Node>();
            List<Edge> out_edges = new List<Edge>();
            if (out_new_edges.Count > out_old_edges.Count)
            {
                out_nodes = newNodes;
                out_edges = out_new_edges;
            }
            else
            {
                out_nodes = oldNodes;
                out_edges = out_old_edges;
                nodes_in_oppo_list = new List<Node>(newNodes);
            }
            // 1.2 remove out edges from oldNodes
            foreach (Edge e in out_old_edges)
            {
                this.deleteEdge(e);
            }
            // 2. handle edges from newNodes
            List<Edge> inner_edges_new = GetInnerEdges(newNodes);
            // 2.1 remove all edges from newNodes
            foreach (Node node in newNodes)
            {
                node._edges.Clear();
                node._adjNodes.Clear();
            }
            // 2.2 add inner edges from newNodes
            foreach (Edge e in inner_edges_new)
            {
                this.addAnEdge(e._start, e._end, e._contacts);
            }
            // 3. connect  
            if (nodes_in_oppo_list.Count > 0)
            {
                foreach (Edge e in out_edges)
                {
                    // find the nearest node depending on contacts
                    Node cur = null;
                    if (_nodes.Contains(e._start) && !_nodes.Contains(e._end))
                    {
                        cur = e._start;
                    }
                    else if (!_nodes.Contains(e._start) && _nodes.Contains(e._end))
                    {
                        cur = e._end;
                    }
                    else
                    {
                        throw new Exception();
                    }
                    foreach (Contact c in e._contacts)
                    {
                        Node closest = getNodeNearestToContact(nodes_in_oppo_list, c);
                        if (closest != null)
                        {
                            this.addAnEdge(cur, closest, c._pos3d);
                        }
                    }
                }
            }
            // 4. adjust contacts for new nodes
            //this.resetUpdateStatus();
            //foreach (Node node in newNodes)
            //{
            //    adjustContacts(node);
            //}
            this.adjustContacts();
            this.resetUpdateStatus();
            _NEdges = _edges.Count;
        }// replaceNodes

        public void adjustContacts()
        {
            foreach (Edge e in _edges)
            {
                Node n1 = e._start;
                Node n2 = e._end;
                // find a list of closest points between n1 and n2
                List<Vector3d> pnts = findClosestPointsBetweenNodes(n1, n2);
                if (pnts.Count < e._contacts.Count)
                {
                    continue;
                }
                foreach (Contact c in e._contacts)
                {
                    Vector3d p = c._pos3d;
                    double mind = double.MaxValue;
                    int idx = -1;
                    for (int i = 0; i < pnts.Count; ++i)
                    {
                        double d = (p - pnts[i]).Length();
                        if (d < mind)
                        {
                            mind = d;
                            idx = i;
                        }
                    }
                    c._pos3d = pnts[idx];
                    pnts.RemoveAt(idx);
                }
            }
        }// adjustContacts

        private List<Vector3d> findClosestPointsBetweenNodes(Node m, Node n)
        {
            Vector3d[] m_pnts = m._PART._MESH.VertexVectorArray;
            Vector3d[] n_pnts = n._PART._MESH.VertexVectorArray;
            if (m._PART._partSP != null && m._PART._partSP._points != null &&
                m._PART._partSP._points.Length > m_pnts.Length)
            {
                m_pnts = m._PART._partSP._points;
            }
            if (n._PART._partSP != null && n._PART._partSP._points != null &&
                n._PART._partSP._points.Length > n_pnts.Length)
            {
                n_pnts = n._PART._partSP._points;
            }
            List<Vector3d> points = new List<Vector3d>();
            double thr = 0.04;
            while (points.Count < Common._min_point_num && thr < 0.1)
            {
                points.Clear();
                for (int i = 0; i < m_pnts.Length; ++i)
                {
                    for (int j = 0; j < n_pnts.Length; ++j)
                    {
                        double d = (m_pnts[i] - n_pnts[j]).Length();
                        if (d < 0.08)
                        {
                            Vector3d c = (m_pnts[i] + n_pnts[j]) / 2;
                            points.Add(c);
                        }
                    }
                }
                thr += 0.02;
            }
            return points;
            // TOO COST TO SORT THE DISTANCES
            //List<double> distances = new List<double>();
            //for (int i = 0; i < m_pnts.Length; ++i)
            //{
            //    for (int j = 0; j < n_pnts.Length; ++j)
            //    {
            //        double d = (m_pnts[i] - n_pnts[j]).Length();
            //        int idx = 0;
            //        for (int t = 0; t < distances.Count; ++t)
            //        {
            //            if (d < distances[t])
            //            {
            //                idx = t;
            //            }
            //        }
            //        Vector3d c = (m_pnts[i] + n_pnts[j]) / 2;
            //        distances.Insert(idx, d);
            //        points.Insert(idx, c);
            //    }
            //}
            //// get the most nearest ones
            //int num = 20;
            //int nn = points.Count > num ? num : points.Count;
            //return points.GetRange(0, nn);
        }

        public void addSubGraph(List<Node> toConnect, List<Node> newNodes)
        {

            foreach (Node node in newNodes)
            {
                _nodes.Add(node);
            }
            resetNodeIndex();

            // UPDATE edges
            List<Node> out_nodes = newNodes;
            List<Edge> out_edges = getOutgoingEdges(newNodes);
            // 2. handle edges from newNodes
            List<Edge> inner_edges_new = GetInnerEdges(newNodes);
            // 2.1 remove all edges from newNodes
            foreach (Node node in newNodes)
            {
                node._edges.Clear();
                node._adjNodes.Clear();
            }
            // 2.2 add inner edges from newNodes
            foreach (Edge e in inner_edges_new)
            {
                this.addAnEdge(e._start, e._end, e._contacts);
            }
            // 3. connect  
            if (toConnect.Count > 0)
            {
                foreach (Edge e in out_edges)
                {
                    // find the nearest node depending on contacts
                    Node cur = null;
                    if (_nodes.Contains(e._start) && !_nodes.Contains(e._end))
                    {
                        cur = e._start;
                    }
                    else if (!_nodes.Contains(e._start) && _nodes.Contains(e._end))
                    {
                        cur = e._end;
                    }
                    else
                    {
                        throw new Exception();
                    }
                    foreach (Contact c in e._contacts)
                    {
                        Node closest = getNodeNearestToContact(toConnect, c);
                        if (closest != null)
                        {
                            this.addAnEdge(cur, closest, c._pos3d);
                        }
                    }
                }
            }
            // 4. adjust contacts for new nodes
            //this.resetUpdateStatus();
            //foreach (Node node in newNodes)
            //{
            //    adjustContacts(node);
            //}
            this.adjustContacts();
            this.resetUpdateStatus();
            _NEdges = _edges.Count;
        }// replaceNodes

        private void adjustContacts(Node node)
        {
            double thr = 0.1;
            foreach (Edge e in node._edges)
            {
                if (e._contactUpdated)
                {
                    continue;
                }
                Node n1 = e._start;
                Node n2 = e._end;
                foreach (Contact c in e._contacts)
                {
                    Vector3d v = this.getVertextNearToContactMeshes(n1._PART._MESH, n2._PART._MESH, c._pos3d);
                    c._originPos3d = c._pos3d = v;
                }
                // remove overlapping contacts
                Vector3d cnt;
                double min_d = this.getDistBetweenParts(n1._PART, n2._PART, out cnt);
                for (int i = 0; i < e._contacts.Count - 1; ++i)
                {
                    for (int j = i + 1; j < e._contacts.Count; ++j)
                    {
                        double d = (e._contacts[i]._pos3d - e._contacts[j]._pos3d).Length();
                        if (d < thr)
                        {
                            // remove either i or j
                            double di = (e._contacts[i]._pos3d - cnt).Length();
                            double dj = (e._contacts[j]._pos3d - cnt).Length();
                            if (di > dj)
                            {
                                e._contacts.RemoveAt(i);
                                --i;
                                break;
                            }
                            else
                            {
                                e._contacts.RemoveAt(j);
                                --j;
                                continue;
                            }
                        }
                    }
                }
                e._contactUpdated = true;
            }
        }// adjustContacts

        private Node getNodeNearestToContact(List<Node> nodes, Contact c)
        {
            Node res = null;
            double min_dis = double.MaxValue;
            // calculation based on meshes (would be more accurate if the vertices on the mesh is uniform)
            foreach (Node node in nodes)
            {
                Mesh mesh = node._PART._MESH;
                Vector3d[] vecs = mesh.VertexVectorArray;
                if (node._PART._partSP != null && node._PART._partSP._points != null 
                    && node._PART._partSP._points.Length > Common._min_point_num)
                {
                    vecs = node._PART._partSP._points;
                }
                for (int i = 0; i < vecs.Length; ++i)
                {
                    double d = (vecs[i] - c._pos3d).Length();
                    if (d < min_dis)
                    {
                        min_dis = d;
                        res = node;
                    }
                }
            }
            // possibly the edge should be deleted, e.g., a middle part group connecting chair seat and ground touching nodes
            // if try to connect the new groud nodes to a node below it, we could not find one
            if (min_dis > 0.3)
            {
                return null;
            }
            return res;
        }// getNodeNearestToContact

        public static List<Edge> GetInnerEdges(List<Node> nodes)
        {
            List<Edge> edges = new List<Edge>();
            foreach (Node node in nodes)
            {
                foreach (Edge edge in node._edges)
                {
                    Node other = edge._start == node ? edge._end : edge._start;
                    if (nodes.Contains(other) && !edges.Contains(edge))
                    {
                        edges.Add(edge);
                    }
                }
            }
            return edges;
        }// GetInnerEdges

        public static List<Edge> GetAllEdges(List<Node> nodes)
        {
            List<Edge> edges = new List<Edge>();
            foreach (Node node in nodes)
            {
                foreach (Edge edge in node._edges)
                {
                    if (!edges.Contains(edge))
                    {
                        edges.Add(edge);
                    }
                }
            }
            return edges;
        }// GetInnerEdges

        public List<Node> getNodePropagation(List<Node> nodes)
        {
            // propagate the nodes to all inner nodes that only connect to the input #nodes#
            List<Node> inner_nodes = new List<Node>();
            inner_nodes.AddRange(nodes);
            foreach (Node node in nodes)
            {
                foreach (Node adj in node._adjNodes)
                {
                    if (inner_nodes.Contains(adj))
                    {
                        continue;
                    }
                    // check if #adj# is an innter node
                    bool add = true;
                    foreach (Node adjadj in adj._adjNodes)
                    {
                        if (!inner_nodes.Contains(adjadj))
                        {
                            add = false;
                            break;
                        }
                    }
                    if (add)
                    {
                        inner_nodes.Add(adj);
                    }
                }
            }
            return inner_nodes;
        }// GetNodePropagation

        public Vector3d getGroundTouchingNodesCenter()
        {
            Vector3d center = new Vector3d();
            int n = 0;
            foreach (Node node in _nodes)
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

        public List<Node> selectFuncNodes(Functionality.Functions func)
        {
            List<Node> nodes = new List<Node>();
            foreach (Node node in _nodes)
            {
                if (node._funcs.Contains(func) && !nodes.Contains(node))
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }// selectFuncNodes

        public List<Node> selectSymmetryFuncNodes(Functionality.Functions func)
        {
            List<Node> sym_nodes = new List<Node>();
            foreach (Node node in _nodes)
            {
                if (node.symmetry != null && node._funcs.Contains(func) && !sym_nodes.Contains(node) && !sym_nodes.Contains(node.symmetry))
                {
                    sym_nodes.Add(node);
                    sym_nodes.Add(node.symmetry);
                    break;
                }
            }
            return sym_nodes;
        }// selectSymmetryFuncNodes

        private void updateNodeIndex()
        {
            int idx = 0;
            foreach (Node node in _nodes)
            {
                node._INDEX = idx++;
            }
        }// updateNodeIndex

        public void markGroundTouchingNodes()
        {
            foreach (Node node in _nodes)
            {
                double ydist = node._PART._MESH.MinCoord.y;
                if (Math.Abs(ydist) < Common._thresh)
                {
                    node._isGroundTouching = true;
                    node.addFunctionality(Functionality.Functions.GROUND_TOUCHING);
                }
            }
        }// markGroundTouchingNodes

        private void buildGraph()
        {
            if (_NNodes == 0)
            {
                return;
            }
            for (int i = 0; i < _NNodes - 1; ++i)
            {
                Part ip = _nodes[i]._PART;
                for (int j = i + 1; j < _NNodes; ++j)
                {
                    Part jp = _nodes[j]._PART;
                    // measure the relation between ip and jp
                    Vector3d contact;
                    double mind = getDistBetweenParts(ip, jp, out contact);
                    if (mind < Common._thresh)
                    {
                        Edge e = new Edge(_nodes[i], _nodes[j], contact);
                        _edges.Add(e);
                    }
                }
            }
        }// buildGraph

        public void addANode(Node node)
        {
            _nodes.Add(node);
        }

        private double getDistBetweenParts(Part p1, Part p2, out Vector3d contact)
        {
            //return getDistBetweenMeshes(p1._MESH, p2._MESH, out contact);
            // after correct the face index of sampling points
            if (p1._partSP == null || p2._partSP == null ||
                p1._partSP._points == null || p2._partSP._points == null)
            {
                return getDistBetweenMeshes(p1._MESH, p2._MESH, out contact);
            }
            contact = new Vector3d();
            double mind = double.MaxValue;
            Vector3d[] v1 = p1._partSP._points;
            Vector3d[] v2 = p2._partSP._points;
            for (int i = 0; i < v1.Length; ++i)
            {
                for (int j = 0; j < v2.Length; ++j)
                {
                    double d = (v1[i] - v2[j]).Length();
                    if (d < mind)
                    {
                        mind = d;
                        contact = (v1[i] + v2[j]) / 2;
                    }
                }
            }
            return mind;
        }// getDistBetweenParts

        private double getDistBetweenMeshes(Mesh m1, Mesh m2, out Vector3d contact)
        {
            contact = new Vector3d();
            double mind = double.MaxValue;
            Vector3d[] v1 = m1.VertexVectorArray;
            Vector3d[] v2 = m2.VertexVectorArray;
            for (int i = 0; i < v1.Length; ++i)
            {
                for (int j = 0; j < v2.Length; ++j)
                {
                    double d = (v1[i] - v2[j]).Length();
                    if (d < mind)
                    {
                        mind = d;
                        contact = (v1[i] + v2[j]) / 2;
                    }
                }
            }
            // between faces
            for (int i = 0; i < m1.FaceCount; ++i)
            {
                Vector3d c1 = m1.getFaceCenter(i);
                for (int j = 0; j < m2.FaceCount; ++j)
                {
                    Vector3d c2 = m2.getFaceCenter(j);
                    double d = (c1 - c2).Length();
                    if (d < mind)
                    {
                        mind = d;
                        contact = (c1 + c2) / 2;
                    }
                }
            }
            return mind;
        }// getDistBetweenMeshes

        private Vector3d getVertextNearToContactMeshes(Mesh m1, Mesh m2, Vector3d pos)
        {
            Vector3d vertex = pos;
            double mind12 = double.MaxValue;
            double mindc = double.MaxValue;
            double thr = 0.2;
            Vector3d[] v1 = m1.VertexVectorArray;
            Vector3d[] v2 = m2.VertexVectorArray;
            for (int i = 0; i < v1.Length; ++i)
            {
                for (int j = 0; j < v2.Length; ++j)
                {
                    double d_v1_v2 = (v1[i] - v2[j]).Length();
                    Vector3d v0 = (v1[i] + v2[j]) / 2;
                    double dc = (v0 - pos).Length();
                    if (d_v1_v2 < mind12 && dc < thr)
                    {
                        mind12 = d_v1_v2;
                        mindc = dc;
                        vertex = v0;
                    }
                }
            }
            return vertex;
        }// getVertextNearToContactMeshes

        public void addAnEdge(Node n1, Node n2)
        {
            Edge e = isEdgeExist(n1, n2);
            if (e == null)
            {
                Vector3d contact;
                double mind = getDistBetweenParts(n1._PART, n2._PART, out contact);
                e = new Edge(n1, n2, contact);
                addEdge(e);
            }
            _NEdges = _edges.Count;
        }// addAnEdge

        public void addAnEdge(Node n1, Node n2, Vector3d c)
        {
            Edge e = isEdgeExist(n1, n2);
            if (e == null)
            {
                e = new Edge(n1, n2, c);
                addEdge(e);
            }
            else if (e._contacts.Count < Common._max_edge_contacts)
            {
                e._contacts.Add(new Contact(c));
            }
            _NEdges = _edges.Count;
        }// addAnEdge

        public void addAnEdge(Node n1, Node n2, List<Contact> contacts)
        {
            Edge e = isEdgeExist(n1, n2);
            if (e == null)
            {
                e = new Edge(n1, n2, contacts);
                addEdge(e);
            }
            else
            {
                e._contacts = contacts;
            }
            _NEdges = _edges.Count;
        }// addAnEdge

        public void deleteAnEdge(Node n1, Node n2)
        {
            Edge e = isEdgeExist(n1, n2);
            if (e != null)
            {
                deleteEdge(e);
            }
            _NEdges = _edges.Count;
        }// deleteAnEdge

        private void addEdge(Edge e)
        {
            _edges.Add(e);
            e._start.addAnEdge(e);
            e._end.addAnEdge(e);
            _NEdges = _edges.Count;
        }// addEdge

        private void deleteEdge(Edge e)
        {
            _edges.Remove(e);
            e._start._adjNodes.Remove(e._end);
            e._end._adjNodes.Remove(e._start);
            e._start._edges.Remove(e);
            e._end._edges.Remove(e);
            _NEdges = _edges.Count;
        }// deleteEdge

        private bool isEdgeExist(Edge edge)
        {
            foreach (Edge e in _edges)
            {
                if ((e._start == edge._start && e._end == edge._end) ||
                    (e._start == edge._end && e._end == edge._start))
                {
                    return true;
                }
            }
            return false;
        }// isEdgeExist

        public Edge isEdgeExist(Node i, Node j)
        {
            foreach (Edge e in _edges)
            {
                if ((e._start == i && e._end == j) || (e._start == j && e._end == i))
                {
                    return e;
                }
            }
            return null;
        }// isEdgeExist

        public void markSymmtry(Node a, Node b)
        {
            a.symmetry = b;
            b.symmetry = a;

            Vector3d symm_center = (a._pos + b._pos) / 2;
            Vector3d symm_axis = (a._pos - b._pos).normalize();
            Symmetry symm = new Symmetry(symm_center, symm_axis);

            a.symm = symm;
            b.symm = symm;
        }// markSymmtry

        public List<Node> findReplaceableNodes(List<Node> nodes)
        {
            // nodes: from another graph
            // return: nodes that match the structure
            List<Node> res = new List<Node>();

            return res;
        }// findReplaceableNodes

        public List<Edge> getOutgoingEdges(List<Node> nodes)
        {
            // for the substructure, find out the edges that are to be connected
            List<Edge> edges = new List<Edge>();
            foreach (Node node in nodes)
            {
                foreach (Edge e in node._edges)
                {
                    if (edges.Contains(e))
                    {
                        continue;
                    }
                    Node other = e._start == node ? e._end : e._start;
                    if (!nodes.Contains(other))
                    {
                        edges.Add(e);
                    }
                }
            }
            return edges;
        }// getOutgoingEdges

        public List<Node> getOutConnectedNodes(List<Node> nodes)
        {
            // for the substructure, find out the nodes that are to be connected
            List<Node> conns = new List<Node>();
            foreach (Node node in nodes)
            {
                foreach (Edge e in node._edges)
                {
                    Node other = e._start == node ? e._end : e._start;
                    if (!nodes.Contains(other) && !conns.Contains(other))
                    {
                        conns.Add(other);
                    }
                }
            }
            return conns;
        }// getOutConnectedNodes

        public List<Node> getGroundTouchingNodes()
        {
            List<Node> nodes = new List<Node>();
            foreach (Node node in _nodes)
            {
                if (node._isGroundTouching)
                {
                    nodes.Add(node);
                }
            }
            return nodes;
        }// getGroundTouchingNodes

        public Node getKeyNode()
        {
            int nMaxConn = 0;
            Node key = null;
            foreach (Node node in _nodes)
            {
                if (node._edges.Count > nMaxConn)
                //&& (node._funcs.Contains(Functionality.Functions.PLACEMENT) || node._funcs.Contains(Functionality.Functions.SITTING)))
                {
                    nMaxConn = node._edges.Count;
                    key = node;
                }
            }
            return key;
        }// getKeyNode

        private List<Node> getKeyNodes()
        {
            List<Node> keys = new List<Node>();
            int max_funcs = 0;
            Node key = null;
            foreach (Node node in _nodes)
            {
                if (node._funcs.Count > max_funcs)
                {
                    max_funcs = node._funcs.Count;
                    key = node;
                }
            }
            if (key != null && !keys.Contains(key))
            {
                keys.Add(key);
                if (key.symmetry != null && !keys.Contains(key.symmetry))
                {
                    keys.Add(key.symmetry);
                }
            }
            if (keys.Count == 0)
            {
                key = getKeyNode();
                keys.Add(key);
                if (key.symmetry != null)
                {
                    keys.Add(key.symmetry);
                }
            }
            return keys;
        }// getKeyNodes

        public List<List<Node>> splitAlongKeyNode()
        {
            List<List<Node>> splitNodes = new List<List<Node>>();
            // key node(s)
            List<Node> keyNodes = getKeyNodes();
            bool[] added = new bool[_NNodes];
            foreach (Node node in keyNodes)
            {
                added[node._INDEX] = true;
            }
            splitNodes.Add(keyNodes);
            // split 1
            List<Node> split1 = new List<Node>();
            // dfs
            Queue<Node> queue = new Queue<Node>();
            // put an arbitrary not-visited node
            foreach (Node node in keyNodes)
            {
                foreach (Node adj in node._adjNodes)
                {
                    if (!added[adj._INDEX])
                    {
                        queue.Enqueue(adj);
                        added[adj._INDEX] = true;
                        if (adj.symmetry != null)
                        {
                            queue.Enqueue(adj.symmetry);
                            added[adj.symmetry._INDEX] = true;
                        }
                        break;
                    }
                }
                if (queue.Count > 0)
                {
                    break;
                }
            }
            bool containGround1 = false;
            while (queue.Count > 0)
            {
                Node cur = queue.Dequeue();
                split1.Add(cur);
                if (!containGround1 && cur._isGroundTouching)
                {
                    containGround1 = true;
                }
                foreach (Node node in cur._adjNodes)
                {
                    if (!added[node._INDEX])
                    {
                        queue.Enqueue(node);
                        added[node._INDEX] = true;
                        if (node.symmetry != null)
                        {
                            queue.Enqueue(node.symmetry);
                            added[node.symmetry._INDEX] = true;
                        }
                    }
                }
            }

            List<Node> split2 = new List<Node>();
            bool containGround2 = false;
            foreach (Node node in _nodes)
            {
                if (!added[node._INDEX])
                {
                    split2.Add(node);
                    if (!containGround2 && node._isGroundTouching)
                    {
                        containGround2 = true;
                    }
                }
            }
            bool add_split1_first = true;
            if (containGround1 && !containGround2)
            {
                add_split1_first = true;
            }
            else if (!containGround1 && containGround2)
            {
                add_split1_first = false;
            }
            else if (getOutgoingEdges(split2).Count > getOutgoingEdges(split1).Count)
            {
                add_split1_first = false;
            }

            if (add_split1_first)
            {
                splitNodes.Add(split1);
                if (split2.Count > 0)
                {
                    splitNodes.Add(split2);
                }
            }
            else
            {
                if (split2.Count > 0)
                {
                    splitNodes.Add(split2);
                }
                splitNodes.Add(split1);
            }
            return splitNodes;
        }// splitAlongKeyNode

        public List<List<Node>> getSymmetryPairs()
        {
            List<List<Node>> symPairs = new List<List<Node>>();
            for (int i = 0; i < _NNodes; ++i)
            {
                if (_nodes[i].symmetry != null && _nodes[i]._INDEX < _nodes[i].symmetry._INDEX)
                {
                    List<Node> syms = new List<Node>();
                    syms.Add(_nodes[i]);
                    syms.Add(_nodes[i].symmetry);
                    symPairs.Add(syms);
                }
            }
            return symPairs;
        }// getSymmetryPairs

        private void resetNodeIndex()
        {
            int i = 0;
            foreach (Node node in _nodes)
            {
                node._INDEX = i++;
            }
        }// resetNodeIndex

        public void resetUpdateStatus()
        {
            foreach (Node node in _nodes)
            {
                node._updated = false;
                node._allNeigborUpdated = false;
            }
            foreach (Edge e in _edges)
            {
                e._contactUpdated = false;
                foreach (Contact c in e._contacts)
                {
                    c.updateOrigin();
                }
            }
        }// resetEdgeContactStatus

        public void reset()
        {
            resetNodeIndex();
            resetUpdateStatus();
        }// reset

        public void unify()
        {
            Vector3d maxCoord = Vector3d.MinCoord;
            Vector3d minCoord = Vector3d.MaxCoord;
            foreach (Node node in _nodes)
            {
                maxCoord = Vector3d.Max(maxCoord, node._PART._MESH.MaxCoord);
                minCoord = Vector3d.Min(minCoord, node._PART._MESH.MinCoord);
            }
            Vector3d scale = maxCoord - minCoord;
            double maxS = scale.x > scale.y ? scale.x : scale.y;
            maxS = maxS > scale.z ? maxS : scale.z;
            maxS = 1.0 / maxS;
            Vector3d center = (maxCoord + minCoord) / 2;
            Matrix4d T = Matrix4d.TranslationMatrix(center);
            Matrix4d S = Matrix4d.ScalingMatrix(new Vector3d(maxS, maxS, maxS));
            Matrix4d Q = T * S * Matrix4d.TranslationMatrix(new Vector3d() - center);
            this.transformAll(Q);
            // y == 0
            minCoord = Vector3d.MaxCoord;
            foreach (Node node in _nodes)
            {
                minCoord = Vector3d.Min(minCoord, node._PART._MESH.MinCoord);
            }

            Vector3d t = new Vector3d();
            t.y = -minCoord.y;
            T = Matrix4d.TranslationMatrix(t);

            this.transformAll(T);

            recomputeSPnormals();
        }// unify

        public void recomputeSPnormals()
        {
            // recompute sample points normals
            foreach (Node node in _nodes)
            {
                if (node._PART._partSP != null)
                {
                    node._PART._partSP.updateNormals(node._PART._MESH);
                }
            }
        }

        public void transformAll(Matrix4d T)
        {
            foreach (Node node in _nodes)
            {
                node.Transform(T);
            }
            foreach (Edge edge in _edges)
            {
                if (!edge._contactUpdated)
                {
                    edge.TransformContact(T);
                }
            }
            resetUpdateStatus();
        }// transformAll

        public void cloneSubgraph(List<Node> nodes, out List<Node> clone_nodes, out List<Edge> clone_edges)
        {
            clone_nodes = new List<Node>();
            clone_edges = new List<Edge>();
            List<Edge> visited = new List<Edge>();
            foreach (Node node in nodes)
            {
                Node cloned = node.Clone() as Node;
                clone_nodes.Add(cloned);
            }
            for (int i = 0; i < nodes.Count; ++i)
            {
                Node node = nodes[i];
                Node cloned = clone_nodes[i];
                foreach (Edge e in node._edges)
                {
                    if (visited.Contains(e))
                    {
                        continue;
                    }
                    Node adj = e._start == node ? e._end : e._start;
                    int idx = nodes.IndexOf(adj);
                    if (idx != -1)
                    {
                        Node adj_cloned = clone_nodes[idx];
                        List<Contact> contacts = new List<Contact>();
                        foreach (Contact c in e._contacts)
                        {
                            contacts.Add(new Contact(new Vector3d(c._pos3d)));
                        }
                        Edge cloned_e = new Edge(cloned, adj_cloned, contacts);
                        clone_edges.Add(cloned_e);
                    }
                    visited.Add(e);
                }
            }
        }// cloneSubgraph

        public Node getNodeToAttach()
        {
            double minz = double.MaxValue;
            Node attach = null;
            foreach (Node node in _nodes)
            {
                if (node._funcs.Contains(Functionality.Functions.HUMAN_BACK) || node._funcs.Contains(Functionality.Functions.PLACEMENT))
                {
                    if (node._PART._MESH.MinCoord.z < minz)
                    {
                        minz = node._PART._MESH.MinCoord.z;
                        attach = node;
                    }
                }
            }
            return attach;
        }// getNodeToAttach

        //*********** Validation ***********//
        public bool isValid()
        {
            resetNodeIndex();
            if (!isIndexValid())
            {
                return false;
            }
            if (hasBadScale())
            {
                return false;
            }
            if (!isValidContacts())
            {
                return false;
            }
            if (hasNIsolatedNodes() > 0)
            {
                return false;
            }
            if (hasDetachedParts())
            {
                return false;
            }
            if (!isPhysicalValid())
            {
                return false;
            }
            return true;
        }// isValid

        public void fitNodeFunctionalSpaceAgent()
        {
            foreach (Node node in _nodes)
            {
                if (!node._funcs.Contains(Functionality.Functions.PLACEMENT))
                {
                    continue;
                }
                // analyze the functional node
                string name = node._PART._partName.ToLower();
                if (name.Contains("container"))
                {
                    node._functionalSpaceAgent = approximateFunctionalSpace(node, 0);
                }
                else
                {
                    node._functionalSpaceAgent = approximateFunctionalSpace(node, 1);
                }
            }
        }// fitNodeFunctionalSpaceAgent

        public bool hasAnyNonObstructedFunctionalPart(int catId)
        {
            bool hasFunctionalPart = false;
            foreach(Node node in _nodes)
            {
                string name = node._PART._partName.ToLower();
                if (node._functionalSpaceAgent == null) 
                {
                    continue;
                }     
                if (catId >= 0)
                {
                    string catName = Functionality.getCategoryName(catId).ToLower();
                    if (!name.Contains(catName))
                    {
                        continue;
                    }
                }         
                if (this.isFunctionalSpaceAgentOversized(node._functionalSpaceAgent) ||
                    this.isFunctionalSpaceEnclosed(node))
                {
                    continue;
                } 
                if (!this.ifFunctionalSpaceObstructed(node))
                {
                    hasFunctionalPart = true;
                    break; 
                }
            }
            return hasFunctionalPart; 
        }// hasAnyNonObstructedFunctionalPart

        private bool isFunctionalSpaceAgentOversized(Prism prism)
        {
            double volume = Common.ComputePolygonVolume(prism);
            Console.WriteLine("volume: " + volume.ToString());
            if (volume > 0.5)
            {
                return true;
            }
            return false;
        }// isFunctionalSpaceAgentOversized

        private bool isFunctionalSpaceEnclosed(Node cur)
        {
            Prism prism = cur._functionalSpaceAgent;
            if (prism == null)
            {
                return false;
            }
            foreach(Node node in _nodes)
            {
                if (node == cur)
                {
                    continue;
                }
                // some bounding box is cylinder, not easy to evaluate, use cuboid instead
                Vector3d vmin = node._PART._BOUNDINGBOX.MinCoord;
                Vector3d vmax = node._PART._BOUNDINGBOX.MaxCoord;
                Prism cuboid = new Prism(vmin, vmax);
                int nEnclosePnt = 0;
                foreach(Vector3d v in prism._POINTS3D)
                {
                    if (Common.PointInPolygon(v, cuboid))
                    {
                        ++nEnclosePnt;
                    }
                }
                if (nEnclosePnt > 4)
                {
                    return true;
                }
            }
            return false;
        }// isFunctionalSpaceEnclosed

        private Prism approximateFunctionalSpace(Node node, int option)
        {
            double shift = 0.05;
            if (option == 0)
            {
                // basket container
                shift *= 2;
                Vector3d lower = new Vector3d(node._PART._BOUNDINGBOX.MinCoord.x + shift,
                node._PART._BOUNDINGBOX.MinCoord.y + shift, node._PART._BOUNDINGBOX.MinCoord.z + shift);
                Vector3d upper = new Vector3d(node._PART._BOUNDINGBOX.MaxCoord.x - shift,
                     node._PART._BOUNDINGBOX.MaxCoord.y - shift, node._PART._BOUNDINGBOX.MaxCoord.z - shift);
                return new Prism(lower, upper);
            }
            // find the upper bounding box
            double minMaxY = 0;
            foreach (Node nd in _nodes)
            {
                if (nd._PART._BOUNDINGBOX.MaxCoord.y > minMaxY)
                {
                    minMaxY = nd._PART._BOUNDINGBOX.MaxCoord.y;
                }
            }
            minMaxY += 0.1;
            shift = minMaxY / 20;
            shift = Math.Max(0.1, minMaxY);
            //shift = 0.2;

            bool isTop = true;
            for (int i = 0; i < _nodes.Count; ++i)
            {
                if (_nodes[i] == node || _nodes[i]._PART._BOUNDINGBOX.MinCoord.y < node._PART._BOUNDINGBOX.MaxCoord.y + shift)
                {
                    continue;
                }
                // check if the node is above
                Vector3d center = _nodes[i]._PART._BOUNDINGBOX.CENTER;
                if (center.x < node._PART._BOUNDINGBOX.MaxCoord.x - shift && center.x > node._PART._BOUNDINGBOX.MinCoord.x + shift
                    && center.z < node._PART._BOUNDINGBOX.MaxCoord.z -shift  && center.z > node._PART._BOUNDINGBOX.MinCoord.z + shift)
                {
                    minMaxY = minMaxY < _nodes[i]._PART._BOUNDINGBOX.MinCoord.y ? minMaxY : _nodes[i]._PART._BOUNDINGBOX.MinCoord.y;
                    isTop = false;
                }
            }
            Vector3d bot = new Vector3d(node._PART._BOUNDINGBOX.MinCoord.x + shift,
                node._PART._BOUNDINGBOX.MaxCoord.y, node._PART._BOUNDINGBOX.MinCoord.z + shift);
            if (isTop)
            {
                minMaxY = Math.Max(1, minMaxY);
            }
            Vector3d top = new Vector3d(node._PART._BOUNDINGBOX.MaxCoord.x - shift,
                minMaxY - shift, node._PART._BOUNDINGBOX.MaxCoord.z - shift);
            Prism prism = new Prism(bot, top);
            prism.computeMaxMin();
            return prism;
        }// approximateFunctionalSpace

        private bool ifFunctionalSpaceObstructed(Node node)
        {
            if (node._functionalSpaceAgent == null)
            {
                return false;
            }
            List<Vector3d> points = new List<Vector3d>();
            foreach(Node other in _nodes)
            {
                if (other == node || other._PART._partSP == null)
                {
                    continue;
                }
                foreach(Vector3d pnt in other._PART._partSP._points)
                {
                    if (Common.PointInPolygon(pnt, node._functionalSpaceAgent))
                    {
                        points.Add(pnt);
                    }
                }
            }
            if (points.Count < 10)
            {
                return false;
            }
            // fit
            Prism poly = Part.FitProxy(0, points.ToArray());
            double volume_poly = Common.ComputePolygonVolume(poly);
            double volume = Common.ComputePolygonVolume(node._functionalSpaceAgent);
            if (volume == 0)
            {
                return true;
            }
            double occupy = volume_poly / volume;
            return occupy > 0.5;
        }// ifFunctionalSpaceObstructed

        public bool isPhysicalValid()
        {
            // if the functional part is tilted
            int nFuncParts = 0;
            bool hasMainFunc = false;
            foreach (Node node in _nodes)
            {
                if (node._funcs.Contains(Functionality.Functions.PLACEMENT))
                {
                    ++nFuncParts;
                }
                if (!hasMainFunc)
                {
                    foreach (Functionality.Functions f in node._funcs)
                    {
                        if (Functionality.IsMainFunction(f))
                        {
                            hasMainFunc = true;
                        }
                    }
                }
            }
            if (!hasMainFunc)
            {
                return false;
            }
            int nMaxTitled = Math.Min(1, nFuncParts);
            int nTitled = 0;
            foreach (Node node in _nodes)
            {
                if (node._funcs.Contains(Functionality.Functions.PLACEMENT))
                {
                    Vector3d nor = node._PART._BOUNDINGBOX._PLANES[0].normal;
                    double angle = Math.Acos(Math.Abs(nor.Dot(Common.uprightVec)));
                    if (angle > 0.2)
                    {
                        ++nTitled;
                    }
                    //Console.WriteLine("functional part angle: " + angle.ToString());
                }
            }
            if (nTitled > nMaxTitled)
            {
                return false;
            }
            
            // center of mass falls in the supporting polygon
                _centerOfMass = new Vector3d();
            List<Vector2d> centers2d = new List<Vector2d>();
            List<Vector3d> groundPnts = new List<Vector3d>();
            foreach (Node node in _nodes)
            {
                Vector3d v = node._PART._BOUNDINGBOX.CENTER;
                _centerOfMass += v;
                centers2d.Add(new Vector2d(v.x, v.z));
                if (node._funcs.Contains(Functionality.Functions.GROUND_TOUCHING))
                {
                    groundPnts.Add(node._PART._BOUNDINGBOX.MinCoord);
                    groundPnts.Add(node._PART._BOUNDINGBOX.MaxCoord);
                }
            }
            _centerOfMass /= _NNodes;
            Vector2d center = new Vector2d(_centerOfMass.x, _centerOfMass.z);
            Vector2d minCoord = Vector2d.MaxCoord();
            Vector2d maxCoord = Vector2d.MinCoord();
            foreach (Vector3d v in groundPnts)
            {
                Vector2d v2 = new Vector2d(v.x, v.z);
                minCoord = Vector2d.Min(v2, minCoord);
                maxCoord = Vector2d.Max(v2, maxCoord);
            }
            // in case some model only has 2 ground touching points
            Vector2d[] groundPnts2d = new Vector2d[4];
            groundPnts2d[0] = new Vector2d(minCoord.x, minCoord.y);
            groundPnts2d[1] = new Vector2d(minCoord.x, maxCoord.y);
            groundPnts2d[2] = new Vector2d(maxCoord.x, maxCoord.y);
            groundPnts2d[3] = new Vector2d(maxCoord.x, minCoord.y);
            if (!Polygon2D.isPointInPolygon(center, groundPnts2d))
            {
                return false;
            }
            //foreach (Vector2d v in centers2d)
            //{
            //    if (!Polygon2D.isPointInPolygon(v, groundPnts2d))
            //    {
            //        return false;
            //    }
            //}
            return true;
        }// isPhysicalValid

        private bool isIndexValid()
        {
            foreach (Edge e in _edges)
            {
                if (e._start._INDEX >= _NNodes || e._end._INDEX >= _NNodes)
                {
                    return false;
                }
            }
            return true;
        }// isValidGraph

        private bool isValidContacts()
        {
            foreach (Edge e in _edges)
            {
                foreach (Contact c in e._contacts)
                {
                    if (!c._pos3d.isValidVector())
                    {
                        return false;
                    }
                }
            }
            return true;
        }// isValidContacts

        public int hasNIsolatedNodes()
        {
            int nIsolatedNodes = 0;
            foreach (Node node in this._nodes)
            {
                if (node._edges == null || node._edges.Count == 0)
                {
                    ++nIsolatedNodes;
                    continue;
                }
            }
            if (nIsolatedNodes > 0)
            {
                return nIsolatedNodes;
            }
            // try to walk through one node to all the others
            List<Node> start = new List<Node>();
            start.Add(_nodes[0]);
            List<Node> bfs_nodes = this.bfs_regionGrowingAnyNodes(start);
            return _nodes.Count - bfs_nodes.Count;
        }// hasNIsolatedNodes

        private bool hasBadScale()
        {
            Vector3d minScale = Vector3d.MinCoord;
            Vector3d maxScale = Vector3d.MaxCoord;
            foreach (Node node in _nodes)
            {
                node._PART._BOUNDINGBOX.computeMaxMin();
                minScale = Vector3d.Min(minScale, node._PART._BOUNDINGBOX._scale);
                maxScale = Vector3d.Max(maxScale, node._PART._BOUNDINGBOX._scale);
            }
            double[] scales = (maxScale - minScale).ToArray();
            Array.Sort(scales);
            double thr = 3;
            return scales[2] / scales[0] > thr && scales[2] / scales[1] > thr;
        }

        private bool isViolateOriginalScales()
        {
            // geometry filter
            double[] vals = calScale();
            double max_adj_nodes_dist = Math.Max(_maxAdjNodesDist, 0.05); // not working for non-uniform meshes
            double min_box_scale = Math.Min(_minNodeBboxScale, Common._min_scale);
            double max_box_scale = Math.Max(_maxNodeBboxScale, Common._max_scale);
            // max scale is not reliable, since a large node may replace many small nodes
            if (vals[0] > max_adj_nodes_dist || vals[1] < min_box_scale || vals[2] > max_box_scale)
            {
                return true;
            }
            foreach (Node node in _nodes)
            {
                if (node._PART._BOUNDINGBOX.MinCoord.y < Common._minus_thresh)
                {
                    return true;
                }
            }
            return false;
        }// isViolateOriginalScales

        private bool isLoseOriginalFunctionality()
        {
            List<Functionality.Functions> funs = this.getGraphFuncs();
            if (funs.Count < _origin_funcs.Count)
            {
                return true;
            }
            //foreach (Functionality.Functions f in _origin_funcs)
            //{
            //    if (!funs.Contains(f))
            //    {
            //        return true;
            //    }
            //}
            return false;
        }// isLoseOriginalFunctionality

        private bool hasDetachedParts()
        {
            // 1. tow parts need to be conneceted if there is a connection
            foreach (Edge e in _edges)
            {
                if (!isPartConnected(e._start._PART, e._end._PART))
                {
                    return true;
                }
            }

            // if any node that is detached
            // check if we can walk through one node to all the other nodes
            bool[] visited = new bool[_nodes.Count];
            Node start = _nodes[0];
            List<Node> queue = new List<Node>();
            queue.Add(start);
            while (queue.Count > 0)
            {
                List<Node> cur = new List<Node>(queue);
                foreach (Node node in cur)
                {
                    visited[node._INDEX] = true;
                }
                queue.Clear();
                foreach (Node c in cur)
                {
                    // include all connected
                    Mesh m1 = c._PART._MESH;
                    foreach (Node node in _nodes)
                    {
                        if (visited[node._INDEX] || queue.Contains(node))
                        {
                            continue;
                        }
                        Mesh m2 = node._PART._MESH;
                        if (isPartConnected( c._PART, node._PART))
                        {
                            queue.Add(node);
                        }
                    }
                }
            }
            for (int i = 0; i < visited.Length; ++i)
            {
                if (!visited[i])
                {
                    return true;
                }
            }
            return false;
        }// hasDetachedParts

        private bool isPartConnected(Part p1, Part p2)
        {
            // uniformly sampled points on mesh
            double thr = 0.04;
            double mind = double.MaxValue;
            Vector3d[] v1 = p1._partSP == null ? p1._MESH.VertexVectorArray : p1._partSP._points;
            Vector3d[] v2 = p2._partSP == null ? p2._MESH.VertexVectorArray : p2._partSP._points;
            bool useMesh1 = false;
            bool useMesh2 = false;
            if (v1 == null || v1.Length < 10)
            {
                v1 = p1._MESH.VertexVectorArray;
                useMesh1 = true;
            }
            if (v2 == null || v2.Length < 10)
            {
                v2 = p2._MESH.VertexVectorArray;
                useMesh2 = true;
            }
            if (useMesh1 && useMesh2)
            {
                thr = 0.08;
            }
            for (int i = 0; i < v1.Length; ++i)
            {
                for (int j = 0; j < v2.Length; ++j)
                {
                    double d = (v1[i] - v2[j]).Length();
                    if (d < thr)
                    {
                        return true;
                    }
                    if (d < mind)
                    {
                        mind = d;
                    }
                }
            }
            return mind < thr;
        }// isPartConnected

        private bool isConnected(Mesh m1, Mesh m2, double thr)
        {
            // work for uniform mesh -- vertex are equally distributed
            double mind = double.MaxValue;
            Vector3d[] v1 = m1.VertexVectorArray;
            Vector3d[] v2 = m2.VertexVectorArray;
            for (int i = 0; i < v1.Length; ++i)
            {
                //for (int j = 0; j < m2.FaceCount; ++j)
                //{
                //    Vector3d center = m2.getFaceCenter(j);
                //    Vector3d nor = m2.getFaceNormal(j);
                //    double d = Common.PointDistToPlane(v1[i], center, nor);
                for (int j = 0; j < v2.Length; ++j)
                {
                    double d = (v1[i] - v2[j]).Length();
                    if (d < thr)
                    {
                        return true;
                    }
                    if (d < mind)
                    {
                        mind = d;
                    }
                }
            }
            return mind < thr;
        }// is connected

        private bool isTwoPolygonInclusive(Vector3d v1_min, Vector3d v1_max, Vector3d v2_min, Vector3d v2_max)
        {
            double thr = 0.01;
            for (int i = 0; i < 3; ++i)
            {
                if ( (v1_min[i] <= v2_max[i] + thr && v1_min[i] >= v2_min[i] - thr &&
                    v1_max[i] <= v2_max[i] + thr && v1_max[i] >= v2_min[i] - thr) ||
                     (v2_min[i] <= v1_max[i] + thr && v2_min[i] >= v1_min[i] - thr &&
                    v2_max[i] <= v1_max[i] + thr && v2_max[i] >= v1_min[i] - thr))
                {
                    return true;
                }
            }
            return false;
        }

        private bool isTwoPolyDetached(Vector3d v1_min, Vector3d v2_max)
        {
            double thr = 0.05;
            return v1_min.x > v2_max.x + thr || v1_min.y > v2_max.y + thr || v1_min.z > v2_max.z + thr;
        }// isTwoPolyOverlap

        // Functions features
        //public void computeFeatures()
        //{
        //    // 1. point featurs
        //    computePointFeatures();
        //    // 2. curvature features
        //    computeCurvatureFeatures();
        //    // 3. pca features
        //    computePCAFeatures();
        //    // 4. ray features
        //    computeRayFeatures();
        //    // 5. convex hull center features
        //    computeDistAndAngleToCenterOfConvexHull();
        //    // 6. center of mass features
        //    computeDistAndAngleToCenterOfMass();
        //}// computeFeatures


        public void addAPartGroup(List<Node> selectedNodes)
        {
            if (!this.shouldCreateNewPartGroup(_partGroups, selectedNodes))
            {
                return;
            }
            PartGroup ng = new PartGroup(selectedNodes, 0);
            _partGroups.Add(ng);
        }

        private List<Node> getAllSupprotingNodes()
        {
            // only if the supporting nodes connect to ground touching nodes
            // hinting ground touching nodes do not connect to main functional nodes directly
            List<Node> supportNodes = this.getNodesAndDependentsByFunctionality(Functionality.Functions.SUPPORT);
            List<Node> dependNodes = this.bfs_regionGrowingNonFunctionanlNodes(supportNodes);
            List<Node> groundNodes = this.getNodesAndDependentsByFunctionality(Functionality.Functions.GROUND_TOUCHING);
            groundNodes = this.bfs_regionGrowingNonFunctionanlNodes(groundNodes);
            if (supportNodes.Count == 0 || groundNodes.Count == 0)
            {
                return null;
            }
            List<Node> supportingNodes = new List<Node>(groundNodes);
            foreach (Node node in supportNodes)
            {
                if (!supportingNodes.Contains(node) && hasConnections(node, groundNodes))
                {
                    supportingNodes.Add(node);
                }
            }
            return supportingNodes;
        }// getAllSupprotingNodes

        private List<Node> getHumanSittingNodes()
        {
            List<Node> sitNodes = this.getNodesAndDependentsByFunctionality(Functionality.Functions.SITTING);
            sitNodes = this.bfs_regionGrowingNonFunctionanlNodes(sitNodes);
            List<Node> backNodes = this.getNodesAndDependentsByFunctionality(Functionality.Functions.HUMAN_BACK);
            backNodes = this.bfs_regionGrowingNonFunctionanlNodes(backNodes);
            List<Node> handNodes = this.getNodesAndDependentsByFunctionality(Functionality.Functions.HAND_HOLD);
            handNodes = this.bfs_regionGrowingNonFunctionanlNodes(handNodes);
            List<Node> sittingNodes = new List<Node>(sitNodes);
            foreach (Node node in backNodes)
            {
                if (!sittingNodes.Contains(node) && hasConnections(node, sittingNodes))
                {
                    sittingNodes.Add(node);
                }
            }
            foreach (Node node in handNodes)
            {
                if (!sittingNodes.Contains(node) && hasConnections(node, sittingNodes))
                {
                    sittingNodes.Add(node);
                }
            }
            return sittingNodes;
        }// getAllSupprotingNodes

        private bool hasConnections(Node node, List<Node> nodes)
        {
            foreach (Node adj in node._adjNodes)
            {
                if (nodes.Contains(adj))
                {
                    return true;
                }
            }
            return false;
        }// hasConnections

        public void initializePartGroups()
        {
            _partGroups = new List<PartGroup>();
            // 1. connected parts
            List<List<int>> comIndices = new List<List<int>>();
            // empty
            _partGroups.Add(new PartGroup(new List<Node>(), 0));
            comIndices.Add(new List<int>());
            // 1. functionality group
            // Note that in different models, some parts have multiple functionality due to the segmentation
            // e.g., one piece of furniture, like chair seat and back are not separable
            // In the element level, one segment can only have one function, if a segment has multiple functions,
            // that means it contains unserparable parts, this information should be encoded for pairing with 
            // element(s) from another shape having the same functions.
            var allFuncs = Enum.GetValues(typeof(Functionality.Functions));
            foreach (Functionality.Functions func in allFuncs)
            {
                List<Node> nodes = this.getNodesAndDependentsByFunctionality(func);
                if (nodes.Count == 0)
                {
                    continue;
                }
                PartGroup ng = new PartGroup(nodes, 0);
                List<int> indices = new List<int>();
                foreach (Node nd in nodes)
                {
                    indices.Add(nd._INDEX);
                }
                if (getIndex(comIndices, indices) == -1 && shouldCreateNewPartGroup(_partGroups, nodes))
                {
                    comIndices.Add(indices);
                    _partGroups.Add(ng);
                }
                if (Functionality.IsMainFunction(func))
                {
                    // main functionality part
                    List<Node> single = new List<Node>();
                    single.Add(nodes[0]);
                    ng = new PartGroup(single, 0);
                    indices = new List<int>();
                    indices.Add(nodes[0]._INDEX);
                    if (getIndex(comIndices, indices) == -1 && shouldCreateNewPartGroup(_partGroups, nodes))
                    {
                        comIndices.Add(indices);
                        _partGroups.Add(ng);
                    }
                }
            }
            // special cases: 
            // s1. all support structure
            List<int> indices_support = new List<int>();
            List<Node> supportingNodes = this.getAllSupprotingNodes();
            if (supportingNodes != null)
            {
                foreach (Node node in supportingNodes)
                {
                    indices_support.Add(node._INDEX);
                }
                if (getIndex(comIndices, indices_support) == -1 && shouldCreateNewPartGroup(_partGroups, supportingNodes))
                {
                    comIndices.Add(indices_support);
                    PartGroup ng = new PartGroup(supportingNodes, 0);
                    _partGroups.Add(ng);
                }
            }
            // s2. chair back and seat
            List<int> indices_human = new List<int>();
            List<Node> sittingNodes = this.getHumanSittingNodes();
            if (sittingNodes != null)
            {
                foreach (Node node in sittingNodes)
                {
                    indices_human.Add(node._INDEX);
                }
                if (getIndex(comIndices, indices_human) == -1 && shouldCreateNewPartGroup(_partGroups, sittingNodes))
                {
                    comIndices.Add(indices_human);
                    PartGroup ng = new PartGroup(sittingNodes, 0);
                    _partGroups.Add(ng);
                }
            }
            // 2. symmetry parts
            bool[] added = new bool[_NNodes];
            for (int i = 0; i < _NNodes; ++i)
            {
                if (added[i] || _nodes[i].symmetry == null)
                {
                    continue;
                }
                Node sym = _nodes[i].symmetry;
                added[i] = true;
                added[sym._INDEX] = true;
                List<Node> symNodes = new List<Node>();
                symNodes.Add(_nodes[i]);
                symNodes.Add(sym);
                PartGroup pg = new PartGroup(symNodes, 0);
                List<int> indices = new List<int>();
                indices.Add(i);
                indices.Add(sym._INDEX);
                if (getIndex(comIndices, indices) == -1)
                {
                    _partGroups.Add(pg);
                    comIndices.Add(indices);
                }
                // symmetry breaking
                List<Node> nodes1 = new List<Node>();
                nodes1.Add(_nodes[i]);
                if (Functionality.ContainsMainFunction(Functionality.getNodesFunctionalities(symNodes))
                    && shouldCreateNewPartGroup(_partGroups, nodes1))
                {
                    _partGroups.Add(new PartGroup(nodes1, 0));
                    indices = new List<int>();
                    indices.Add(i);
                    comIndices.Add(indices);
                    pg._isSymmBreak = true;
                }
                //List<Node> nodes2 = new List<Node>();
                //nodes2.Add(sym);
                //if (shouldCreateNewPartGroup(_partGroups, nodes2))
                //{
                //    _partGroups.Add(new PartGroup(nodes2, 0));
                //    indices = new List<int>();
                //    indices.Add(sym._INDEX);
                //    comIndices.Add(indices);
                //}
            }
            // 3. connected parts to exisiting groups
            int nGroups = _partGroups.Count;
            for (int i = 0; i < nGroups; ++i)
            {
                PartGroup pg = _partGroups[i];
                if (pg._NODES.Count == 0)
                {
                    continue;
                }
                if (pg._NODES.Count == 1 && Functionality.ContainsMainFunction(pg._NODES[0]._funcs))
                {
                    continue;
                }
                List<Node> propogationNodes = bfs_regionGrowingNonFunctionanlNodes(pg._NODES);
                if (propogationNodes.Count > 0)
                {
                    List<int> indices = new List<int>();
                    foreach (Node node in propogationNodes)
                    {
                        indices.Add(node._INDEX);
                    }
                    if (getIndex(comIndices, indices) != -1)
                    {
                        continue;
                    }
                    comIndices.Add(indices);
                    PartGroup ppg = new PartGroup(propogationNodes, 0);
                    //_partGroups.Add(ppg);
                    _partGroups[i] = ppg; //! in this way, only use connected nodes
                }
            }
        }// initializePartGroups

        private List<Node> bfs_regionGrowingAnyNodes(List<Node> nodes)
        {
            List<Node> res = new List<Node>();
            Queue<Node> queue = new Queue<Node>();
            bool[] visited = new bool[_nodes.Count];
            foreach (Node node in nodes)
            {
                queue.Enqueue(node);
                visited[node._INDEX] = true;
                res.Add(node);
            }

            while (queue.Count > 0)
            {
                List<Node> cur = new List<Node>();
                while (queue.Count > 0)
                {
                    Node qn = queue.Dequeue();
                    cur.Add(qn);
                }
                foreach (Node nd in cur)
                {
                    foreach (Node adj in nd._adjNodes)
                    {
                        if (visited[adj._INDEX])
                        {
                            continue;
                        }
                        queue.Enqueue(adj);
                        visited[adj._INDEX] = true;
                        res.Add(adj);
                    }
                }
            }// while
            return res;
        }// bfs_regionGrowingNonFunctionanlNodes

        private List<Node> bfs_regionGrowingNonFunctionanlNodes(List<Node> nodes)
        {
            List<Node> res = new List<Node>();
            Queue<Node> queue = new Queue<Node>();
            bool[] visited = new bool[_nodes.Count];
            foreach (Node node in nodes)
            {
                queue.Enqueue(node);
                visited[node._INDEX] = true;
                res.Add(node);
            }
            
            while (queue.Count > 0)
            {
                List<Node> cur = new List<Node>();
                while (queue.Count > 0)
                {
                    Node qn = queue.Dequeue();
                    cur.Add(qn);
                }
                foreach (Node nd in cur)
                {
                    foreach (Node adj in nd._adjNodes)
                    {
                        // only consider region growing on trivial parts
                        if (visited[adj._INDEX] || adj._funcs.Count > 0)
                        {
                            continue;
                        }
                        queue.Enqueue(adj);
                        visited[adj._INDEX] = true;
                        res.Add(adj);
                    }
                }
            }// while
            return res;
        }// bfs_regionGrowingNonFunctionanlNodes

        public List<Node> bfs_regionGrowingDependentNodes(List<Node> nodes)
        {
            List<Node> res = new List<Node>();
            Queue<Node> queue = new Queue<Node>();
            bool[] visited = new bool[_nodes.Count];
            foreach (Node node in nodes)
            {
                queue.Enqueue(node);
                visited[node._INDEX] = true;
            }

            while (queue.Count > 0)
            {
                List<Node> cur = new List<Node>();
                while (queue.Count > 0)
                {
                    Node qn = queue.Dequeue();
                    cur.Add(qn);
                }
                foreach (Node nd in cur)
                {
                    foreach (Node adj in nd._adjNodes)
                    {
                        if (visited[adj._INDEX] || adj._funcs.Contains(Functionality.Functions.GROUND_TOUCHING))
                        {
                            continue;
                        }
                        queue.Enqueue(adj);
                        visited[adj._INDEX] = true;
                        res.Add(adj);
                    }
                }
            }// while
            return res;
        }// bfs_regionGrowingDependentNodes

        private bool shouldCreateNewPartGroup(List<PartGroup> partGroups, List<Node> nodes)
        {
            // case 1: one node only, but there already exists a node as part group that
            //          has the same functionality
            if (nodes.Count != 1)
            {
                return true;
            }
            List<Functionality.Functions> funcs = nodes[0]._funcs;
            foreach (PartGroup pg in partGroups)
            {
                if (pg._NODES.Count == 1)
                {
                    bool iden = false;
                    foreach (Functionality.Functions f in funcs)
                    {
                        if (pg._NODES[0]._funcs.Contains(f))
                        {
                            iden = true;
                            break;
                        }
                    }
                    if (iden)
                    {
                        return false;
                    }
                }
            }
            return true;
        }// shouldCreateNewPartGroup

        public void deleteNodes(List<Node> selectedNodes)
        {
            // with parts together
            foreach (Node del in selectedNodes)
            {
                _nodes.Remove(del);
                if (del.symmetry != null && !selectedNodes.Contains(del.symmetry))
                {
                    Node sym = del.symmetry;
                    del.symmetry = null;
                    sym.symmetry = null;
                }
                int n = del._edges.Count;
                for (int j = 0; j < n; ++j)
                {
                    Edge e = del._edges[0];
                    this.deleteAnEdge(e._start, e._end);
                }
            }
            resetNodeIndex();
        }// deleteNodes
        public void deleteNodes(List<int> indices)
        {
            indices.Sort();
            // with parts together
            for (int i = indices.Count - 1; i >= 0; --i)
            {
                Node del = _nodes[indices[i]];
                _nodes.RemoveAt(indices[i]);
                if (del.symmetry != null && !indices.Contains(del.symmetry._INDEX))
                {
                    Node sym = del.symmetry;
                    del.symmetry = null;
                    sym.symmetry = null;
                }
                int n = del._edges.Count;
                for(int j = 0; j < n; ++j)
                {
                    Edge e = del._edges[0];
                    this.deleteAnEdge(e._start, e._end);
                }
            }
            resetNodeIndex();
        }// deleteNodes

        private int getIndex(List<List<int>> com, List<int> cand)
        {
            for (int i = 0; i < com.Count; ++i)
            {
                List<int> c = com[i];
                if (c.Count != cand.Count)
                {
                    continue;
                }
                bool notIden = false;
                foreach (int num in cand)
                {
                    if (!c.Contains(num))
                    {
                        notIden = true;
                        break;
                    }
                }
                if (!notIden)
                {
                    return i;
                }
            }
            return -1;
        }// getIndex

        public List<Functionality.Functions> collectMainFunctions()
        {
            List<Functionality.Functions> res = new List<Functionality.Functions>();
            foreach (Node node in _nodes)
            {
                foreach (Functionality.Functions f in node._funcs)
                {
                    if ((Functionality.IsMainFunction(f) || f == Functionality.Functions.ROLLING) && !res.Contains(f))
                    {
                        res.Add(f);
                    }
                }
            }
            return res;
        }// collectMainFunctions

        public List<Functionality.Functions> collectAllDistinceFunctions()
        {
            List<Functionality.Functions> res = new List<Functionality.Functions>();
            foreach (Node node in _nodes)
            {
                foreach (Functionality.Functions f in node._funcs)
                {
                    if (!res.Contains(f))
                    {
                        res.Add(f);
                    }
                }
            }
            return res;
        }// collectAllDistinceFunctions

       
        public List<Node> _NODES
        {
            get
            {
                return _nodes;
            }
        }

        public List<Edge> _EDGES
        {
            get
            {
                return _edges;
            }
        }

        public int _NNodes
        {
            get
            {
                return _nodes.Count;
            }
        }
    }// Graph

    public class Node
    {
        Part _part;
        private int _index = -1;
        public List<Edge> _edges;
        public List<Node> _adjNodes;
        public Vector3d _pos;
        public bool _isGroundTouching = false;
        public bool _updated = false;
        public bool _allNeigborUpdated = false;
        public Node symmetry = null;
        public Symmetry symm = null;
        // only if the node contains multiple semantic parts that cannot be segmented in the mesh
        // or, maybe try to even separate the mesh
        public List<Functionality.Functions> _funcs = new List<Functionality.Functions>();
        public Vector3d _ratios = new Vector3d();
        public bool[] _isFunctionalPatch = new bool[Functionality._TOTAL_FUNCTONAL_PATCHES];
        public Prism _functionalSpaceAgent;

        public Node(Part p, int idx)
        {
            _part = p;
            _index = idx;
            _edges = new List<Edge>();
            _adjNodes = new List<Node>();
            _pos = p._BOUNDINGBOX.CENTER;
            setDefaultFunctionalPatch();
        }

        private void setDefaultFunctionalPatch()
        {
            for (int i = 0; i < _isFunctionalPatch.Length; ++i)
            {
                _isFunctionalPatch[i] = true;
            }
        }

        public void addAnEdge(Edge e)
        {
            Node adj = e._start == this ? e._end : e._start;
            if (!_adjNodes.Contains(adj))
            {
                _adjNodes.Add(adj);
            }
            Edge edge = getEdge(adj);
            if (edge == null)
            {
                _edges.Add(e);
            }
            else
            {
                edge._contacts = e._contacts;
            }
        }// addAnEdge  

        public void deleteAnEdge(Edge e)
        {
            _edges.Remove(e);
            Node other = e._start == this ? e._end : e._start;
            _adjNodes.Remove(other);
        }// deleteAnEdge

        private Edge getEdge(Node adj)
        {
            foreach (Edge e in _edges)
            {
                if (e._start == adj || e._end == adj)
                {
                    return e;
                }
            }
            return null;
        }// getEdge

        public void addFunctionality(Functionality.Functions func)
        {
            if (!_funcs.Contains(func))
            {
                _funcs.Add(func);
            }
            if (func == Functionality.Functions.GROUND_TOUCHING)
            {
                _isGroundTouching = true;
            }
        }// addFunctionality

        public void removeAllFuncs()
        {
            _funcs.Clear();
        }// removeAllFuncs

        public void calRatios()
        {
            //this._part._BOUNDINGBOX.computeMaxMin();
            Vector3d scale = this._part._BOUNDINGBOX.MaxCoord - this._part._BOUNDINGBOX.MinCoord;
            _ratios.x = 1.0;
            _ratios.y = scale.y / scale.x;
            _ratios.z = scale.z / scale.x;
        }// calRatios

        public Object Clone(Part p)
        {
            Node cloned = new Node(p, _index);
            cloned._isGroundTouching = _isGroundTouching;
            cloned._funcs = new List<Functionality.Functions>(_funcs);
            return cloned;
        }// Clone

        public Object Clone()
        {
            Part p = _part.Clone() as Part;
            Node cloned = new Node(p, _index);
            cloned._isGroundTouching = _isGroundTouching;
            cloned._funcs = new List<Functionality.Functions>(_funcs);
            if (this._functionalSpaceAgent != null)
            {
                cloned._functionalSpaceAgent = this._functionalSpaceAgent.Clone() as Prism;
            }
            return cloned;
        }// Clone

        public void Transform(Matrix4d T)
        {
            _part.Transform(T);
            _pos = _part._BOUNDINGBOX.CENTER;
            if (_functionalSpaceAgent != null)
            {
                _functionalSpaceAgent.Transform(T);
            }
        }

        public void TransformFromOrigin(Matrix4d T)
        {
            _part.TransformFromOrigin(T);
            _pos = _part._BOUNDINGBOX.CENTER;
            if (_functionalSpaceAgent != null)
            {
                _functionalSpaceAgent.TransformFromOrigin(T);
            }
        }

        public void updateOriginPos()
        {
            _part.updateOriginPos();
            if (_functionalSpaceAgent != null)
            {
                _functionalSpaceAgent.updateOrigin();
            }
        }

        public void setPart(Part p)
        {
            _part = p;
            _pos = p._BOUNDINGBOX.CENTER;
        }

        public bool isAllNeighborsUpdated()
        {
            if (_allNeigborUpdated)
            {
                return true;
            }
            foreach (Node node in _adjNodes)
            {
                if (!node._updated)
                {
                    return false;
                }
            }
            _allNeigborUpdated = true;
            return true;
        }// isAllNeighborsUpdated

        public Part _PART
        {
            get
            {
                return _part;
            }
        }

        public int _INDEX
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
            }
        }
    }// Node

    public class Edge
    {
        public Node _start;
        public Node _end;
        public List<Contact> _contacts;
        public bool _contactUpdated = false;
        public Common.NodeRelationType _type;

        public Edge(Node a, Node b, Vector3d c)
        {
            _start = a;
            _end = b;
            _contacts = new List<Contact>();
            _contacts.Add(new Contact(c));
        }

        public Edge(Node a, Node b, List<Contact> contacts)
        {
            _start = a;
            _end = b;
            _contacts = contacts;
        }

        private void analyzeEdgeType()
        {
            Vector3d ax1 = _start._PART._BOUNDINGBOX.coordSys[0];
            Vector3d ax2 = _end._PART._BOUNDINGBOX.coordSys[0];
            double acos = ax1.Dot(ax2);
            double thr = Math.PI / 18;
            this._type = Common.NodeRelationType.None;
            if (Math.Abs(acos) < thr)
            {
                this._type = Common.NodeRelationType.Orthogonal;
            }
        }// analyzeEdgeType

        public void TransformContact(Matrix4d T)
        {
            foreach (Contact p in _contacts)
            {
                p.TransformFromOrigin(T);
            }
            _contactUpdated = true;
        }

        public List<Vector3d> getOriginContactPoints()
        {
            List<Vector3d> pnts = new List<Vector3d>();
            foreach (Contact p in _contacts)
            {
                pnts.Add(p._originPos3d);
            }
            return pnts;
        }// getOriginContactPoints

        public List<Vector3d> getContactPoints()
        {
            List<Vector3d> pnts = new List<Vector3d>();
            foreach (Contact p in _contacts)
            {
                pnts.Add(p._pos3d);
            }
            return pnts;
        }// getContactPoints
    }// Edge

    public class Symmetry
    {
        public Vector3d _center;
        public Vector3d _axis;
        public Symmetry(Vector3d c, Vector3d a)
        {
            _center = c;
            _axis = a;
        }
    }// Symmetry

    public class ReplaceablePair
    {
        public Graph _g1;
        public Graph _g2;
        public List<List<Node>> _pair1;
        public List<List<Node>> _pair2;

        public ReplaceablePair(Graph g1, Graph g2, List<List<int>> idx1, List<List<int>> idx2)
        {
            _g1 = g1;
            _g2 = g2;
            _pair1 = new List<List<Node>>();
            _pair2 = new List<List<Node>>();
            for (int i = 0; i < idx1.Count; ++i)
            {
                List<Node> nodes1 = new List<Node>();
                List<Node> nodes2 = new List<Node>();
                foreach (int j in idx1[i])
                {
                    nodes1.Add(_g1._NODES[j]);
                }
                foreach (int j in idx2[i])
                {
                    nodes2.Add(_g2._NODES[j]);
                }
                _pair1.Add(nodes1);
                _pair2.Add(nodes2);
            }
        }
    }// ReplaceablePair

    public class FuncFeatures
    {
        public double[] _pointFeats;
        public double[] _curvFeats;
        public double[] _pcaFeats;
        public double[] _rayFeats;
        public double[] _conhullFeats;
        public double[] _cenOfMassFeats;
        public bool[] _visibliePoint;

        public FuncFeatures() { }

        public FuncFeatures(double[] pf, double[] cf, double[] pcaf, double[] rf, double[] chf, double[] cmf) {
            _pointFeats = pf;
            _curvFeats = cf;
            _pcaFeats = pcaf;
            _rayFeats = rf;
            _conhullFeats = chf;
            _cenOfMassFeats = cmf;
        }

        public Object clone()
        {
            if (_pointFeats == null || _curvFeats == null || _pcaFeats == null)
            {
                return null;
            }
            double[] pointFeats_c = _pointFeats.Clone() as double[];
            double[] curvFeats_c = _curvFeats.Clone() as double[];
            double[] pcaFeats_c = _pcaFeats.Clone() as double[];
            double[] rayFeats_c = _rayFeats.Clone() as double[];
            double[] conhullFeats_c = _conhullFeats.Clone() as double[];
            double[] cenMassFeats_c = _cenOfMassFeats.Clone() as double[];
            FuncFeatures ff = new FuncFeatures(pointFeats_c, curvFeats_c, pcaFeats_c, rayFeats_c, conhullFeats_c, cenMassFeats_c);
            return ff;
        }
    }// FuncFeatures

    public class FunctionalityFeatures
    {
        public Functionality.Category[] _cats = new Functionality.Category[Functionality._NUM_CATEGORIY];
        public double[] _funScores = new double[Functionality._NUM_CATEGORIY];
        public List<Functionality.Category> _parentCategories = new List<Functionality.Category>();
        public double[] _inClassProbs = new double[Functionality._NUM_CATEGORIY];
        public double[] _outClassProbs = new double[Functionality._NUM_CATEGORIY];
        public double[] _classProbs = new double[Functionality._NUM_CATEGORIY];
        public double _noveltyVal = Functionality._NOVELTY_MINIMUM;
        public double _validityVal = 0;
        public FunctionalityFeatures()
        {
            for (int i = 0; i < Functionality._NUM_CATEGORIY; ++i)
            {
                _cats[i] = (Functionality.Category)i;
            }
        }

        public FunctionalityFeatures(List<Functionality.Category> cats, List<double> vals)
        {
            _cats = cats.ToArray();
            _funScores = vals.ToArray();
        }

        public Object clone()
        {
            List<Functionality.Category> cats = new List<Functionality.Category>(_cats);
            List<double> vals = new List<double>(_funScores);
            FunctionalityFeatures ff = new FunctionalityFeatures(cats, vals);
            return ff;
        }

        public void addParentCategories(List<Functionality.Category> parents)
        {
            foreach (Functionality.Category cat in parents) {
                if (!_parentCategories.Contains(cat))
                {
                    _parentCategories.Add(cat);
                }
            }
        }
    }// FunctionalityFeatures

}// namespace
