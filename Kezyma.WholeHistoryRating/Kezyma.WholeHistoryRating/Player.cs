using KezymaWeb.WholeHistoryRating.Collection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kezyma.WholeHistoryRating
{ 
    public class Player
    {
        public int Id;
        public double AnchorGamma;
        public IList<PlayerDay> Days;
        public double W2;
        public Config Config;

        public Player(int id, Config config)
        {
            Id = id;
            W2 = Math.Pow(Math.Sqrt(config.W2) * Math.Log(10) / 400.0, 2.0);
            Days = new List<PlayerDay>();
            Config = config;
        }

        public double LogLikelihood
        {
            get
            {
                var sum = 0.0;
                var sigma2 = ComputeSigma2();
                var n = Days.Count();

                for (int i = 0; i < n; i++)
                {
                    var prior = 0.0;
                    if (i < n-1)
                    {
                        var rd = Days[i].R - Days[i + 1].R;
                        prior += (1 / (Math.Sqrt(2 * Math.PI * sigma2[i]))) * Math.Exp(-Math.Pow(rd, 2) / 2 * sigma2[i]);
                    }
                    if (i > 0)
                    {
                        var rd = Days[i].R - Days[i - 1].R;
                        prior += (1 / (Math.Sqrt(2 * Math.PI * sigma2[i-1]))) * Math.Exp(-Math.Pow(rd, 2) / 2 * sigma2[i-1]);
                    }
                    if (prior == 0)
                    {
                        sum += Days[i].LogLikelihood;
                    }
                    else
                    {
                        if (double.IsInfinity(Days[i].LogLikelihood) || double.IsInfinity(Math.Log(prior)))
                        {
                            // Something's Wrong!
                        }
                        sum += Days[i].LogLikelihood + Math.Log(prior);
                    }
                }

                return sum;
            }
        }

        private Matrix Hessian(IList<PlayerDay> days, IList<double> sigma2)
        {
            var n = days.Count();
            var matrix = new Matrix(n);
            for (int col = 0; col < n; col++)
            {
                for (int row = 0; row < n; row++)
                {
                    if (row == col)
                    {
                        var prior = 0.0;
                        if (row < n-1) prior += -1.0 / sigma2[row];
                        if (row > 0) prior += -1.0 / sigma2[row - 1];
                        matrix[col][row] = days[row].LogLikelihoodSecondDerivative + prior - 0.001;
                    }
                    else if (row == col-1)
                    {
                        matrix[col][row] = 1.0 / sigma2[row];
                    }
                    else if (row == col+1)
                    {
                        matrix[col][row] = 1.0 / sigma2[col];
                    }
                    else
                    {
                        matrix[col][row] = 0;
                    }
                }
            }
            return matrix;
        }

        private IList<double> Gradient(IList<double> r, IList<PlayerDay> days, IList<double> sigma2)
        {
            var g = new List<double>();
            var n = days.Count();
            for (int idx = 0; idx < n; idx++)
            {
                var day = days[idx];
                var prior = 0.0;
                if (idx < n - 1) prior += -(r[idx] - r[idx + 1]) / sigma2[idx];
                if (idx > 0) prior += -(r[idx] - r[idx - 1]) / sigma2[idx - 1];
                g.Add(day.LogLikelihoodDerivative + prior);
            }
            return g;
        }

        public void RunOneNewtonIteration()
        {
            foreach (var day in Days)
            {
                day.ClearGameTermsCache();
            }

            if (Days.Count() == 1)
            {
                Days[0].UpdateBy1DNewtonsMethod();
            }
            else if (Days.Count() > 1)
            {
                UpdateByNdimNewton();
            }
        }

        public IList<double> ComputeSigma2()
        {
            var sigma2 = new List<double>();
            foreach (var d in Enumerable.Range(0, Days.Count() - 1).Select(x => Enumerable.Range(x, 2).ToArray()))
            {
                var d1 = Days[d[0]];
                var d2 = Days[d[1]];
                sigma2.Add(Math.Abs(d2.Day - d1.Day) * W2);
            }
            return sigma2;
        }

        public void UpdateByNdimNewton()
        {
            var r = Days.Select(rr => rr.R).ToList();
            var sigma2 = ComputeSigma2();
            var h = Hessian(Days, sigma2);
            var g = Gradient(r, Days, sigma2);

            var a = new DefaultDictionary<int, double?>(null);
            var d = new DefaultDictionary<int, double?>(null) { { 0, h[0][0] } };
            var b = new DefaultDictionary<int, double?>(null) { { 0, h[0][1] } };
            var n = r.Count();
            for (int i = 1; i < n; i++)
            {
                a[i] = h[i][i - 1] / d[i - 1];
                d[i] = h[i][i] - a[i] * b[i - 1];
                b[i] = h[i][i + 1];
            }

            var y = new DefaultDictionary<int, double?>(null) { { 0, g[0] } };
            for (int i = 1; i < n; i++)
            {
                y[i] = g[i] - a[i] * y[i - 1];
            }

            var x = new DefaultDictionary<int, double?>(null);
            x[n - 1] = y[n - 1] / d[n - 1];
            for (int i = n-2; i >= 0; i--)
            {
                x[i] = (y[i] - b[i] * x[i + 1]) / d[i];
            }

            var newR = r.Zip(x, (ri, xi) => ri - xi.Value);

            foreach (var nr in newR)
            {
                if (nr > 650)
                {
                    // Somerthing's Wrong.
                }
            }

            for (int idx = 0; idx < Days.Count(); idx++)
            {
                Days[idx].R = Days[idx].R - x[idx].Value;
            }
        }

        public Matrix Covariance
        {
            get
            {
                var r = Days.Select(x => x.R).ToList();
                var sigma2 = ComputeSigma2();
                var h = Hessian(Days, sigma2);
                var g = Gradient(r, Days, sigma2);
                var n = Days.Count();
                var a = new DefaultDictionary<int, double?>(null);
                var d = new DefaultDictionary<int, double?>(null) { { 0, h[0][0] } };
                var b = new DefaultDictionary<int, double?>(null) { { 0, h[0][1] } };

                for (int i = 1; i < n; i++)
                {
                    a[i] = h[i][i - 1] / d[i - 1];
                    d[i] = h[i][i] - a[i] * b[i - 1];
                    b[i] = h[i][i + 1];
                }

                var dp = new DefaultDictionary<int, double?>(null) { { n - 1, h[n - 1][n - 1] } };
                var bp = new DefaultDictionary<int, double?>(null) { { n - 1, h[n - 1][n - 2] } };
                var ap = new DefaultDictionary<int, double?>(null);

                for (int i = n-2; i >= 0; i--)
                {
                    ap[i] = h[i][i + 1] / dp[i + 1];
                    dp[i] = h[i][i] - ap[i] * bp[i + 1];
                    bp[i] = h[i][i - 1];
                }

                var v = new DefaultDictionary<int, double?>(null);
                for (int i = 0; i < n-1; i++)
                {
                    v[i] = dp[i + 1] / (b[i] * bp[i + 1] - d[i] * dp[i + 1]);
                }
                v[n - 1] = -1 / d[n - 1];

                var matrix = new Matrix(n);
                for (int col = 0; col < n; col++)
                {
                    for (int row = 0; row < n; row++)
                    {
                        if (row==col)
                        {
                            matrix[col][row] = v[row].Value;
                        }
                        else if (row == col - 1)
                        {
                            matrix[col][row] = -1 * a[col].Value * v[col].Value;
                        }
                        else
                        {
                            matrix[col][row] = 0;
                        }
                    }
                }

                return matrix;
            }
        }

        public void UpdateUncertainty()
        {
            if (Days.Count > 0)
            {
                var c = Covariance;
                var u = new List<double>();
                for (int i = 0; i < Days.Count(); i++)
                {
                    u.Add(c[i][i].Value);
                }
                Days.Zip(u, (d, uu) => d.Uncertainty = uu);
            }
            else
            {
                // 5
            }
        }

        public void AddGame(Game game)
        {
            if (Days == null || !Days.Any() || Days.Last().Day != game.Day)
            {
                var newPDay = new PlayerDay(this, game.Day);
                if (Days == null || !Days.Any())
                {
                    Days = new List<PlayerDay>();
                    newPDay.IsFirstDay = true;
                    newPDay.Gamma = 1;
                }
                else
                {
                    newPDay.Gamma = Days.Last().Gamma;
                }
                Days.Add(newPDay);
            }
            if (game.Player1.Id == Id) game.Player1Day = Days.Last();
            if (game.Player2.Id == Id) game.Player2Day = Days.Last();
            Days.Last().AddGame(game);
        }
    }
}
