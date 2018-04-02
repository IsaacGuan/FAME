using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Geometry
{
	public class Triplet
	{
		public int row;
		public int col;
		public double value;
		public Triplet(int r, int c, double v)
		{
			this.row = r;
			this.col = c;
			this.value = v;
		}

		public Triplet(Triplet t)
		{
			this.row = t.row;
			this.col = t.col;
			this.value = t.value;
		}

		public class TripletRowComparer : IComparer<Triplet>
		{
			public int Compare(Triplet t1, Triplet t2)
			{
				return t1.col - t2.col;
			}
		}

		public class TripletColumnComparer : IComparer<Triplet>
		{
			public int Compare(Triplet t1, Triplet t2)
			{
				return t1.row - t2.row;
			}
		}
	}//class-Triplet	

	public class SparseMatrix
	{
		private List<Triplet> triplets = null;
		private int nRows, nCols;
		private List<List<Triplet>> rowTriplets = null;
		private List<List<Triplet>> colTriplets = null;

		#region Count-info
		public int NRow
		{
			get
			{
				return this.nRows;
			}
		}

		public int NCol
		{
			get
			{
				return this.nCols;
			}
		}

		public int NTriplets
		{
			get
			{
				return triplets.Count;
			}
		}
	#endregion

		public SparseMatrix()
		{
			clear();
		}

		public SparseMatrix(int m,int n)
		{
			clear();
			nRows = m;
			nCols = n;
            MakeRowColTriplets();
		}

        public SparseMatrix(SparseMatrix m)
        {
            triplets = new List<Triplet>(m.triplets);
            nRows = m.nRows;
            nCols = m.nCols;
            MakeRowColTriplets();
        }

        public SparseMatrix(SparseMatrix m, int r, int c)
        {
            triplets = new List<Triplet>(m.triplets);
            nRows = r;
            nCols = c;
            MakeRowColTriplets();
        }

		public SparseMatrix(List<Triplet> triplets, int nrow, int ncol)
		{
			triplets = new List<Triplet>(triplets);
			nRows = nrow;
			nCols = ncol;
			MakeRowColTriplets();
		}

		public SparseMatrix(List<Triplet> triplets)
		{
			triplets = new List<Triplet>(triplets);
			nRows = 0;
			nCols = 0;
			for (int i = 0; i < NTriplets; ++i)
			{
				nRows = triplets[i].row > nRows ? triplets[i].row : nRows;
				nCols = triplets[i].col > nCols ? triplets[i].col : nCols;
			}
			MakeRowColTriplets();
		}

		public void clear()
		{
			triplets = new List<Triplet>();
			rowTriplets = new List<List<Triplet>>();
			colTriplets = new List<List<Triplet>>();
		}

		private void MakeRowColTriplets()
		{
			rowTriplets = new List<List<Triplet>>();
			colTriplets = new List<List<Triplet>>();
			for (int i = 0; i < nRows; ++i)
			{
				rowTriplets.Add(new List<Triplet>());
			}
			for (int i = 0; i < nCols; ++i)
			{
                colTriplets.Add(new List<Triplet>());
			}
			for (int i = 0; i < NTriplets; ++i)
			{
				rowTriplets[triplets[i].row].Add(triplets[i]);
				colTriplets[triplets[i].col].Add(triplets[i]);
			}
		}

		public void AddTriplet(Triplet triplet)
		{
			if (triplets == null) // do not initialize here, as row/col is unknown
			{
				throw new NullReferenceException();
			}
			if (triplet.row < 0 || triplet.row >= nRows || triplet.col < 0 || triplet.col >= nCols)
			{
				throw new IndexOutOfRangeException();
			}
			triplets.Add(triplet);
			rowTriplets[triplet.row].Add(triplet);
			rowTriplets[triplet.row].Sort(new Triplet.TripletRowComparer());
			colTriplets[triplet.col].Add(triplet);
			colTriplets[triplet.col].Sort(new Triplet.TripletColumnComparer());
		}

		public void AddTriplet(int row, int col, double value)
		{
			Triplet triplet = new Triplet(row, col, value);
			Triplet curr = this.GetTriplet(row, col);
			if (curr == null)
			{
				this.AddTriplet(triplet);
			}
			else
			{
				curr.value = value;
			}
            //if (value == 0)
            //{
            //    // remove
            //    triplets.Remove(triplet);
            //    rowTriplets[row].Remove(triplet);
            //    colTriplets[col].Remove(triplet);
            //}
        }

        public void RemoveATriplet(int row, int col)
        {
            Triplet curr = this.GetTriplet(row, col);
            if (curr != null)
            {
                triplets.Remove(curr);
                rowTriplets[row].Remove(curr);
                colTriplets[col].Remove(curr);
            }
        }

        public Triplet GetTriplet(int index)
        {
            if (index < 0 || index > NTriplets)
            {
                throw new IndexOutOfRangeException();
            }
            return triplets[index];
        }

        public List<Triplet> GetTriplets()
        {
            return triplets;
        }

        public List<Triplet> GetRowTriplets(int rowIndex)
		{
			if(rowIndex < 0 || rowIndex >= nRows)
			{
				throw new IndexOutOfRangeException();
			}
			return rowTriplets[rowIndex];
		}

		public List<Triplet> GetColumnTriplets(int colIndex)
		{
			if (colIndex < 0 || colIndex >= nCols)
			{
				throw new IndexOutOfRangeException();
			}
			return colTriplets[colIndex];
		}

		public Triplet GetTriplet(int rowIndex, int colIndex)
		{
			if (rowIndex < 0 || rowIndex >= nRows || colIndex < 0 || colIndex >= nCols)
			{
				throw new IndexOutOfRangeException();
			}
			foreach( Triplet t in rowTriplets[rowIndex])
			{
				if(t.col == colIndex)
				{
					return t;
				}
			}
			return null;
		}

		public void AddARow()
		{
			List<Triplet> newRow = new List<Triplet>();
			rowTriplets.Add(newRow);
			nRows++;
		}

		public void AddARow(List<Triplet> rTriplet)
		{
			rowTriplets.Add(rTriplet);
			nRows++;
            foreach (Triplet tri in rTriplet) 
            {
                colTriplets[tri.col].Add(tri);
            }
		}

		public void AddAColumn()
		{
			List<Triplet> newCol = new List<Triplet>();
			colTriplets.Add(newCol);
			nCols++;
		}

		public void AddAColumn(List<Triplet> cTriplet)
		{
			colTriplets.Add(cTriplet);
			nCols++;
            foreach (Triplet tri in cTriplet)
            {
                rowTriplets[tri.row].Add(tri);
            }
		}

        public int[][] getRowIndex()
        {
            int[][] rowIndex = new int[NRow][];
            for (int i = 0; i < rowTriplets.Count; ++i)
            {
                rowIndex[i] = new int[rowTriplets[i].Count];
                for (int j = 0; j < rowTriplets[i].Count; ++j)
                {
                    rowIndex[i][j] = rowTriplets[i][j].col;
                }
            }
            return rowIndex;
        }

        public int[][] getColIndex()
        {
            int[][] colIndex = new int[NCol][];
            for (int i = 0; i < colTriplets.Count; ++i)
            {
                colIndex[i] = new int[colTriplets[i].Count];
                for (int j = 0; j < colTriplets[i].Count; ++j)
                {
                    colIndex[i][j] = colTriplets[i][j].row;
                }
            }
            return colIndex;
        }

		// operators
		static public SparseMatrix operator +(SparseMatrix mat1, SparseMatrix mat2)
		{
			if (mat1 == null || mat2 == null)
			{
				return null;
			}
			int m = mat1.NRow, n = mat1.NCol;
			if (m != mat2.NRow || n != mat2.NCol)
			{
				Console.WriteLine("Dimension not matched.");
				return null;
			}
			SparseMatrix mat = new SparseMatrix(m, n);
			List<Triplet> newTriplets = new List<Triplet>();
			for (int r = 0; r < m; ++r )
			{
				List<Triplet> trips = new List<Triplet>();
				List<Triplet> row1 = mat1.GetRowTriplets(r);
				List<Triplet> row2 = mat2.GetRowTriplets(r);
				int c1 = 0, c2 = 0;		
				while(c1 < row1.Count && c2 < row2.Count)
				{
					Triplet t1 = null, t2 = null;
					t1 = row1[c1];
					t2 = row2[c2];
					if (t1 != null && t2 != null)
					{
						if (t1.col < t2.col)
						{
							trips.Add(new Triplet(t1));
							++c1;
						}
						else if (t2.col < t1.col)
						{
							trips.Add(new Triplet(t2));
							++c2;
						}
						else
						{
							trips.Add(new Triplet(r, c1, t1.value + t2.value));
							++c1;
							++c2;
						}
					}
				}//while
				while (c1 < row1.Count)
				{
					trips.Add(new Triplet(row1[c1]));
					++c1;
				}
				while (c2 < row2.Count)
				{
					trips.Add(new Triplet(row2[c2]));
					++c2;
				}
				newTriplets.AddRange(trips);
			}//for-each-row
			mat = new SparseMatrix(newTriplets, m, n);
			return mat;
		}

		static public SparseMatrix operator -(SparseMatrix mat1, SparseMatrix mat2)
		{
			if (mat1 == null || mat2 == null)
			{
				return null;
			}
			SparseMatrix minus_mat2 = -1.0 * mat2;
			return mat1 + minus_mat2;
		}

		static public SparseMatrix operator *(double factor, SparseMatrix mat)
		{
			if (mat == null)
			{
				return null;
			}
			int m = mat.NRow, n = mat.NCol;
			SparseMatrix new_mat = new SparseMatrix(m, n);
			List<Triplet> newTriplets = new List<Triplet>();
			for (int r = 0; r < m; ++r)
			{
				List<Triplet> rowr = mat.GetRowTriplets(r);
				for(int j = 0; j < rowr.Count;++j)
				{
					newTriplets.Add(new Triplet(r, rowr[j].col, rowr[j].value * factor));
				}
			}//for-each-row
			mat = new SparseMatrix(newTriplets);
			return mat;
		}

		static public SparseMatrix operator /(SparseMatrix mat, double factor)
		{
			if (mat == null)
			{
				return null;
			}
			if (Math.Abs(factor) < 1e-6)
			{
				throw new DivideByZeroException();
			}
			return 1.0/factor * mat;
		}

		static public SparseMatrix operator *(SparseMatrix mat1, SparseMatrix mat2)
		{
			if (mat1 == null || mat2 == null)
			{
				return null;
			}
			int m = mat1.NRow, n = mat2.NCol;
			if (mat1.NCol != mat2.NRow)
			{
				Console.WriteLine("Dimension not matched.");
				return null;
			}
			SparseMatrix mat = new SparseMatrix(m, n);
			List<Triplet> newTriplets = new List<Triplet>();
			for (int r = 0; r < m; ++r)
			{
				List<Triplet> row1 = mat1.GetRowTriplets(r);
				for (int c = 0; c < n; ++c)
				{
					List<Triplet> col2 = mat2.GetColumnTriplets(c);
					double sum = 0.0;
					int n1 = 0, n2 = 0;
					while(n1 < row1.Count && n2 < col2.Count)
					{
						int c1 = row1[n1].col;
						int r2 = col2[n2].row;
						if(c1 < r2)
						{
							++n1;
						}else if(r2 < c1)
						{
							++n2;
						}
						else
						{
							sum += row1[n1].value * col2[n2].value;
							++n1;
							++n2;
						}
					}//while
					if(Math.Abs(sum) > 1e-6)
					{
						Triplet triplet = new Triplet(r, c, sum);
						newTriplets.Add(triplet);
					}
				}
			}//for-each-row
			mat = new SparseMatrix(newTriplets);
			return mat;
		}

        // comparable class
        private class RowCompare : IComparer<Triplet>
        {
            public int Compare(Triplet t1, Triplet t2)
            {
                return t1.col - t2.col;
            }
        }

        private class ColCompare : IComparer<Triplet>
        {
            public int Compare(Triplet t1, Triplet t2)
            {
                return t1.row - t2.row;
            }
        }

        public void sort()
        {
            RowCompare rowCompare = new RowCompare();
            ColCompare colCompare = new ColCompare();
            foreach (List<Triplet> r in rowTriplets)
            {
                r.Sort(rowCompare);
            }
            foreach (List<Triplet> c in colTriplets)
            {
                c.Sort(colCompare);
            }
        }
	}//class-SparseMatrix
}
