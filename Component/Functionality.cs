using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Component
{
    public class Functionality
    {
        /***
         * Common functions for #Functions
        ***/

        /********** Variables **********/
        public static int _TOTAL_FUNCTONAL_PATCHES = 35; // ncat * npatch
        public static int _NUM_FUNCTIONALITY = 6;
        public static int _NUM_CATEGORIY = 15;
        public static int _NUM_UNARY_FEATURE = 420;
        public static int _NUM_BINARY_FEATURE = 110;
        public static int[] _PAIR_INDEX_1 = { 0 }; // 1 functional patch
        public static int[] _PAIR_INDEX_2 = { 0, 2, 3 }; // 2 patches - 4 pairs
        public static int[] _PAIR_INDEX_3 = { 0, 3, 6, 5, 7, 8 }; // 3 patches - 6 pairs
        public static int _MAX_MATRIX_DIM = 300;
        public static int _MAX_TRY_TIMES = 60;

        public static int _MAX_GEN_HYBRID_NUMBER = 10;
        public static int _MAX_USE_PRESENT_NUMBER = 20;
        public static int _NUM_INTER_BEFORE_RERUN = 5;

        public static double _NOVELTY_MINIMUM = 0.5;
        public static double _NOVELTY_MAXIMUM = 1.0;

        public static int _POINT_FEAT_DIM = 3;
        public static int _CURV_FEAT_DIM = 4;
        public static int _PCA_FEAT_DIM = 5;
        public static int _RAY_FEAT_DIM = 2;
        public static int _CONVEXHULL_FEAT_DIM = 2;
        public static int _POINT_FEATURE_DIM = 18;

        public enum Functions { PLACEMENT, STORAGE, SITTING, HUMAN_BACK, HAND_HOLD, GROUND_TOUCHING, SUPPORT, HANG, ROLLING, ROCKING, NONE };

        public enum Category { Backpack, Basket, Bicycle, Chair, Desk, DryingRack, Handcart, Hanger, Hook, Shelf, 
            Stand, Stroller, Table, TVBench, Vase, None };


        /********** Functions **********/
        public Functionality() { }

        public static string getTopPredictedCategories(double[] scores)
        {
            int n = scores.Length;
            int mtop = 4;
            int[] index = new int[n];
            for (int i = 0; i < n; ++i)
            {
                index[i] = i;
            }
            Array.Sort(index, (a, b) => scores[b].CompareTo(scores[a]));
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < mtop; ++i)
            {
                sb.Append(getCategoryName(index[i]));
                sb.Append(" ");
            }
            return sb.ToString();
        }// getTopPredictedCategories

        public static bool isKnownCategory(int index)
        {
            return index >= 0 && index < 15;
        }

        public static bool IsPlacementFunction(Functions f)
        {
            return f == Functions.PLACEMENT || f == Functions.STORAGE
                || f == Functions.SITTING;
        }

        public static bool IsMainFunction(Functions f)
        {
            return f == Functions.PLACEMENT || f == Functions.STORAGE 
                || f == Functions.SITTING || f == Functions.HANG;
        }

        public static bool IsSecondaryFunction(Functions f)
        {
            // usually connect to the main functional parts
            return f == Functions.HUMAN_BACK || f == Functions.HAND_HOLD;
        }

        public static bool IsSupportFunction(Functions f)
        {
            // ground touching points
            return f == Functions.GROUND_TOUCHING;
        }

        public static bool IsSecondarySupportFunction(Functions f)
        {
            // connect the main functional parts and ground touching parts
            return f == Functions.SUPPORT;
        }

        public static bool ContainsMainFunction(List<Functions> funcs)
        {
            foreach (Functions f in funcs)
            {
                if (IsMainFunction(f))
                {
                    return true;
                }
            }
            return false;
        }// ContainsMainFunction

        public static bool ContainsSecondaryFunction(List<Functions> funcs)
        {
            foreach (Functions f in funcs)
            {
                if (IsSecondaryFunction(f))
                {
                    return true;
                }
            }
            return false;
        }// ContainsSecondaryFunction

        public static double[] GetPartGroupCompatibility(Model m1, Model m2, PartGroup pg1, PartGroup pg2)
        {
            // 0: not replaceable
            // res[0]: if pg1 can be replaced by pg2 with the compatibility val
            // res[1]: if pg2 can be replaced by pg1 with the compatibility val
            double[] res = new double[2];
            // part groups should not come from the same source model
            if (pg1._ParentModelIndex == pg2._ParentModelIndex || 
                (pg1._NODES.Count == 0 && pg2._NODES.Count == 0))
            {
                return res;
            }
            // 1. if compatible functions exist
            List<Functions> funcs1 = getNodesFunctionalities(pg1._NODES);
            List<Functions> funcs2 = getNodesFunctionalities(pg2._NODES);
            //int comp = IsFunctionCompatible(funcs1, funcs2);
            //if (comp == -1)
            //{
            //    return res;
            //}
            //// 2. if #1. stands, check if the functionality of the model  is preserved
            //if (comp == 0 || comp == 2)
            //{
            //    if (IsUpdatedModelFunctional(m1, pg1, pg2))
            //    {
            //        res[0] = 1;
            //    }
            //}
            //if (comp == 1 || comp == 2)
            //{
            //    if (IsUpdatedModelFunctional(m2, pg2, pg1))
            //    {
            //        res[1] = 1;
            //    }
            //}

            // same funcs
            if (hasCompatibleFunctions(funcs1, funcs2))
            {
                res[0] = 1;
            }
            if (hasCompatibleFunctions(funcs2, funcs1))
            {
                res[1] = 1;
            }
            return res;
        }// IsTwoPartGroupCompatible

        public static bool hasCompatibleFunctions(List<Functions> funcs1, List<Functions> funcs2)
        {
            // OUTPUT:
            // -1: neither is replaceable
            // 0: funcs1 can be replaced by funcs2
            // 1: funcs2 can be replaced by funcs1
            // 2: both are replaceable
            bool containsMainFunc1 = false;
            bool containsMainFunc2 = false;
            bool containsSecondaryFunc1 = false;
            bool containsSecondaryFunc2 = false;
            bool containsSupportFunc1 = false;
            bool containsSupportFunc2 = false;
            int res = -1;
            //// assume all possible functions of nodes have already been measured
            //var same = funcs1.Intersect(funcs2);
            //return same.Count() > 0;
            if (funcs1.Count > funcs2.Count)
            {
                return false;
            }
            for (int i = 0; i < funcs1.Count; ++i)
            {
                for (int j = 0; j < funcs2.Count; ++j)
                {
                    if (isFunctionCompatible(funcs1[i], funcs2[j]))
                    {
                        return true;
                    }
                }
            }
            //if (funcs1.Count == 0 && funcs2.Count > 0)
            //{
            //    return true;
            //}
            return false;
        }// hasCompatibleFunctions

        public static bool isTrivialReplace(List<Node> nodes1, List<Node> nodes2)
        {
            // if the functionality of nodes1 is compatible with nodes2
            return nodes1.Count == nodes2.Count && nodes1.Count == 1;
        }

        private static bool isFunctionCompatible(Functions f1, Functions f2)
        {
            if (f1 == f2)
            {
                return true;
            }
            if ((f1 == Functions.GROUND_TOUCHING && f2 == Functions.SUPPORT) ||
                (f1 == Functions.SUPPORT && f2 == Functions.GROUND_TOUCHING))
            {
                return true;
            }
            if (IsPlacementFunction(f1) && IsPlacementFunction(f2))
            {
                return true;
            }
            return false;
        }

        public static bool IsUpdatedModelFunctional(Model m, PartGroup pg1, PartGroup pg2)
        {
            List<Node> nodes = new List<Node>(m._GRAPH._NODES);
            foreach (Node node in pg1._NODES)
            {
                nodes.Remove(node);
            }
            nodes.AddRange(pg2._NODES);
            Category cat = m._CAT;
            List<Functions> funcs = getFunctionalityFromCategory(cat);
            List<Functions> updated = getNodesFunctionalities(nodes);
            var sub = funcs.Except(updated);
            if (sub.ToList().Count > 0)
            {
                return false;
            }
            var add = updated.Except(funcs);
            if (add.Count() == 0)
            {
                return false; // no supplementary
            }
            return true;
        }// IsUpdatedModelFunctional

        public static int IsFunctionCompatible(List<Functions> funcs1, List<Functions> funcs2)
        {
            // OUTPUT:
            // -1: neither is replaceable
            // 0: funcs1 can be replaced by funcs2
            // 1: funcs2 can be replaced by funcs1
            // 2: both are replaceable
            bool containsMainFunc1 = false;
            bool containsMainFunc2 = false;
            bool containsSecondaryFunc1 = false;
            bool containsSecondaryFunc2 = false;
            bool containsSupportFunc1 = false;
            bool containsSupportFunc2 = false;
            int res = -1;
            // assume all possible functions of nodes have already been measured
            var sub1 = funcs1.Except(funcs2);
            var sub2 = funcs2.Except(funcs1);
            CheckFunctions(sub1.ToList(), out containsMainFunc1, out containsSecondaryFunc1, out containsSupportFunc1);
            CheckFunctions(sub2.ToList(), out containsMainFunc2, out containsSecondaryFunc2, out containsSupportFunc2);
            if (!containsMainFunc1 && !containsSecondaryFunc1 && !containsSupportFunc1)
            {
                res = 0; // not losing any important function
            }
            if (!containsMainFunc2 && !containsSecondaryFunc2 && !containsSupportFunc2)
            {
                res += 2;
            }
            return res;
        }// IsFunctionCompatible

        public static bool isRollableFunction(Functions f)
        {
            return f == Functions.ROCKING || f == Functions.ROLLING;
        }

        public static void CheckFunctions(List<Functions> funcs,
            out bool containsMainFunc,
            out bool containsSecondaryFunc,
            out bool containsSupportFunc)
        {
            containsMainFunc = false;
            containsSecondaryFunc = false;
            containsSupportFunc = false;
            foreach (Functions f in funcs)
            {
                if (IsMainFunction(f))
                {
                    containsMainFunc = true;
                }
                if (IsSecondaryFunction(f))
                {
                    containsSecondaryFunc = true;
                }
                if (IsSupportFunction(f))
                {
                    containsSupportFunc = true;
                }
            }
        }// CheckFunctions

        public static string getCategoryName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Backpack";
                case 1:
                    return "Basket";
                case 2:
                    return "Bicycle";
                case 3:
                    return "Chair";
                case 4:
                    return "Desk";
                case 5:
                    return "DryingRack";
                case 6:
                    return "Handcart";
                case 7:
                    return "Hanger";
                case 8:
                    return "Hook";
                case 9:
                    return "Shelf";
                case 10:
                    return "Stand";
                case 11:
                    return "Stroller";
                case 12:
                    return "Table";
                case 13:
                    return "TVBench";
                case 14:
                    return "Vase";
                default:
                    return "None";
            }
        }// getCategoryName

        public static Category getCategory(string str)
        {
            string sstr = str.ToLower();
            switch (sstr)
            {
                case "backpack":
                    return Category.Backpack;
                case "basket":
                    return Category.Basket;
                case "bicycle":
                    return Category.Bicycle;
                case "chair":
                    return Category.Chair;
                case "desk":
                    return Category.Desk;
                case "dryingrack":
                    return Category.DryingRack;
                case "handcart":
                    return Category.Handcart;
                case "hanger":
                    return Category.Hanger;
                case "hook":
                    return Category.Hook;
                case "shelf":
                    return Category.Shelf;
                case "stand":
                    return Category.Stand;
                case "stroller":
                    return Category.Stroller;
                case "table":
                    return Category.Table;
                case "tvbench":
                    return Category.TVBench;
                case "vase":
                    return Category.Vase;
                default:
                    return Category.None;
            }
        }// getCategory

        public static Functions getFunction(string str)
        {
            string sstr = str.ToLower();
            switch (sstr)
            {
                case "placement":
                    return Functions.PLACEMENT;
                case "storage":
                    return Functions.STORAGE;
                case "support":
                    return Functions.SUPPORT;
                case "rolling":
                    return Functions.ROLLING;
                case "rocking":
                    return Functions.ROCKING;
                case "groundtouching":
                    return Functions.GROUND_TOUCHING;
                case "sitting":
                case "human_hip":
                    return Functions.SITTING;
                case "backsupport":
                    return Functions.HUMAN_BACK;
                default:
                    return Functions.NONE;
            }
        }// getFunction

        public static string getFunctionString(Functions f)
        {
            switch (f)
            {
                case Functions.PLACEMENT:
                    return "placement";
                case Functions.STORAGE:
                    return "storage";
                case Functions.SUPPORT:
                    return "support";
                case Functions.ROLLING:
                    return "rolling";
                case Functions.ROCKING:
                    return "rocking";
                case Functions.GROUND_TOUCHING:
                    return "groundtouching";
                case Functions.SITTING:
                    return "sitting";
                case Functions.HUMAN_BACK:
                    return "backsupport";
                default:
                    return "none";
            }
        }// getFunction

        public static int getNumberOfFunctionalPatchesPerCategory(Category cat)
        {
            switch (cat)
            {
                case Category.Backpack:
                    return 1;
                case Category.Basket:
                    return 2;
                case Category.Bicycle:
                    return 2;
                case Category.Chair:
                    return 2;
                case Category.Desk:
                    return 3;
                case Category.DryingRack:
                    return 2;
                case Category.Handcart:
                    return 3;
                case Category.Hanger:
                    return 2;
                case Category.Hook:
                    return 2;
                case Category.Shelf:
                    return 3;
                case Category.Stand:
                    return 2;
                case Category.Stroller:
                    return 3;
                case Category.Table:
                    return 3;
                case Category.TVBench:
                    return 3;
                case Category.Vase:
                    return 2;
                default:
                    return 0;
            }
        }// getNumberOfFunctionalPatchesPerCategory

        public static int[] getCategoryPatchIndicesInFeatureVector(Functionality.Category cat)
        {
            int[] idxs = new int[getNumberOfFunctionalPatchesPerCategory(cat)];
            int catIdx = (int)cat;
            int start = 0;
            for (int i = 0; i < catIdx; ++i)
            {
                start += getNumberOfFunctionalPatchesPerCategory((Category)i);
            }
            for (int i = 0; i < idxs.Length; ++i)
            {
                idxs[i] = start + i;
            }
            return idxs;
        }// getCategoryPatchIndicesInFeatureVector

        public static List<Functions> getFunctionalityFromCategory(Category cat)
        {
            List<Functions> funcs = new List<Functions>();
            if (cat == Category.Chair)
            {
                funcs.Add(Functions.SITTING);
                funcs.Add(Functions.GROUND_TOUCHING);
                funcs.Add(Functions.HUMAN_BACK);
            }
            if (cat == Category.Handcart)
            {
                funcs.Add(Functions.HAND_HOLD);
                funcs.Add(Functions.GROUND_TOUCHING);
                funcs.Add(Functions.PLACEMENT);
            }
            if (cat == Category.Basket)
            {
                funcs.Add(Functions.HAND_HOLD);
                funcs.Add(Functions.GROUND_TOUCHING);
            }
            if (cat == Category.Shelf)
            {
                funcs.Add(Functions.PLACEMENT);
                funcs.Add(Functions.GROUND_TOUCHING);
            }
            if (cat == Category.DryingRack)
            {
                funcs.Add(Functions.HANG);
                funcs.Add(Functions.GROUND_TOUCHING);
            }
            if (cat == Category.Stand)
            {
                funcs.Add(Functions.GROUND_TOUCHING);
                funcs.Add(Functions.HANG);
            }
            if (cat == Category.Stroller)
            {
                funcs.Add(Functions.GROUND_TOUCHING);
                funcs.Add(Functions.PLACEMENT);
                funcs.Add(Functions.HUMAN_BACK);
            }
            return funcs;
        }// getFunctionalityFromCategory

        public static List<Functions> getFunctionalityFromCategories(List<Category> cats)
        {
            List<Functions> funcs = new List<Functions>();
            foreach (Category cat in cats)
            {
                funcs.AddRange(getFunctionalityFromCategory(cat));
            }
            return funcs;
        }// getFunctionalityFromCategories

        public static List<Functions> getNodesFunctionalities(List<Node> nodes)
        {
            List<Functions> funcs = new List<Functions>();
            foreach (Node node in nodes)
            {
                foreach (Functions f in node._funcs)
                {
                    if (!funcs.Contains(f))
                    {
                        funcs.Add(f);
                    }
                }
            }
            return funcs;
        }// getNodesFunctionalities

        public static List<Functions> getNodesFunctionalitiesIncludeNone(List<Node> nodes)
        {
            List<Functions> funcs = new List<Functions>();
            foreach (Node node in nodes)
            {
                foreach (Functions f in node._funcs)
                {
                    if (!funcs.Contains(f))
                    {
                        funcs.Add(f);
                    }
                }
                if (node._funcs.Count == 0 && !funcs.Contains(Functions.NONE))
                {
                    funcs.Add(Functions.NONE);
                }
            }
            return funcs;
        }// getNodesFunctionalities

    }// Functionality
}
