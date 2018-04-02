using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Component
{
    public class JsonFile
    {
        public string modelView { get; set; }
        public List<GuideJson> guides;
        public List<SequenceJson> sequence;
    }

    public class SequenceJson
    {
        public string primitive { get; set; }
        public List<EllipseJson> ellipses;
        public string segment { get; set; }
        public string type { get; set; }
        public List<NewSequenceJson> sequence { get; set; }

        public List<string> hasGuides { get; set; }
        public List<GuideSequenceJson> guide_sequence;
        public List<GuideJson> guides;
        public List<PointJson> face_to_draw;
        public List<PointJson> face_to_highlight;
        public List<GuideJson> arrows { get; set; }
        public PrimPrevGuides previous_guides { get; set; }
        
    }

    public class NewSequenceJson
    {
        public List<PointJson> face_to_highlight;
        public List<PointJson> face_to_draw;
        public String type_text { get; set; }
        public userLevel user0 { get; set; }
        public userLevel user1 { get; set; }
        public userLevel user2 { get; set; }
    }

    public class GuideSequenceJson
    {
        public string type { get; set; }
        public List<string> guide_indexes { get; set; }
    }

    public class userLevel
    {
        public List<int> indexes;
        public List<int> previous_guides;
    }

    public class PrimPrevGuides
    {
        public List<int> user0;
        public List<int> user1;
        public List<int> user2;
    }

    public class GuideJson
    {
        public PointJson from;
        public PointJson to;
    }

    public class PointJson
    {
        public string x { get; set; }
        public string y { get; set; }
        public string z { get; set; }
    }

    public class EllipseJson
    {
        public double diameter { get; set; }
        public List<double> normal { get; set; }
        public List<double> translation { get; set; }
    }

    public class ContourJson
    {
        public double[] viewMatrix { get; set; }
        public List<SegmentJson> segmentContour { get; set; }
    }

    public class SegmentJson
    {
        public int index { get; set; }
        public List<double> contourPoints { get; set; }
    }
}
