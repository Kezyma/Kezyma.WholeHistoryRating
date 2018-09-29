using System;
using System.Collections.Generic;
using System.Text;

namespace Kezyma.WholeHistoryRating
{
    public class Config
    {
        public Config(double w2 = 150, bool allowDraws = false)
        {
            W2 = w2;
            AllowDraws = allowDraws;
        }

        public double W2;

        public bool AllowDraws;
    }
}
