using System;
using System.Collections.Generic;
using System.Text;

namespace KezymaWeb.WholeHistoryRating.Collection
{
    public class Matrix : DefaultDictionary<int, MatrixRow>
    {
        public Matrix(int size) : base(new MatrixRow())
        {
            for (int i = 0; i < size; i++)
            {
                var col = new MatrixRow();
                for (int j = 0; j < size; j++)
                {
                    col.Add(j, null);
                }
                Add(i, col);
            }
        }
    }

    public class MatrixRow : DefaultDictionary<int, double?>
    {
        public MatrixRow() : base(null) { }
    }
}
