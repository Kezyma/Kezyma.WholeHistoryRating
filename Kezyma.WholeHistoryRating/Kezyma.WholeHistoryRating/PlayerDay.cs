using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kezyma.WholeHistoryRating
{
    public class PlayerDay
    {
        public IList<Game> WonGames;
        public IList<Game> LostGames;
        public IList<Game> DrawnGames;
        public int Day;
        public Player Player;
        public double R;
        public bool IsFirstDay;
        public double Uncertainty;

        public PlayerDay(Player player, int day)
        {
            Day = day;
            Player = player;
            IsFirstDay = false;
            WonGames = new List<Game>();
            DrawnGames = new List<Game>();
            LostGames = new List<Game>();
        }

        public double Gamma
        {
            get => Math.Exp(R);
            set => R = Math.Log(value);
        }

        public double Elo
        {
            get => (R * 400.0) / (Math.Log(10));
            set => R = value * (Math.Log(10) / 400.0);
        }

        private IList<double[]> _wonGameTerms;
        private IList<double[]> _lostGameTerms;
        private IList<double[]> _drawnGameTerms;

        public void ClearGameTermsCache()
        {
            _wonGameTerms = null;
            _lostGameTerms = null;
            _drawnGameTerms = null;
        }

        private IList<double[]> WonGameTerms
        {
            get
            {
                if (_wonGameTerms == null)
                {
                    _wonGameTerms = new List<double[]>();
                    foreach (var g in WonGames)
                    {
                        var otherGamma = g.OpponentsAdjustedGamma(Player);
                        if (otherGamma == 0 || double.IsNaN(otherGamma) || double.IsInfinity(otherGamma))
                        {
                            // Something's Wrong!
                        }
                        _wonGameTerms.Add(new[] { 1.0, 0.0, 1.0, otherGamma });
                    }
                    if (IsFirstDay)
                    {
                        _wonGameTerms.Add(new[] { 1.0, 0.0, 1.0, 1.0 });
                    }
                }
                return _wonGameTerms;
            }
        }

        private IList<double[]> LostGameTerms
        {
            get
            {
                if (_lostGameTerms == null)
                {
                    _lostGameTerms = new List<double[]>();
                    foreach (var g in LostGames)
                    {
                        var otherGamma = g.OpponentsAdjustedGamma(Player);
                        if (otherGamma == 0 || double.IsNaN(otherGamma) || double.IsInfinity(otherGamma))
                        {
                            // Something's Wrong!
                        }
                        _lostGameTerms.Add(new[] { 0.0, otherGamma, 1.0, otherGamma });
                    }
                    if (IsFirstDay)
                    {
                        _lostGameTerms.Add(new[] { 0.0, 1.0, 1.0, 1.0 });
                    }
                }
                return _lostGameTerms;
            }
        }

        private IList<double[]> DrawnGameTerms
        {
            get
            {
                if (_drawnGameTerms == null)
                {
                    _drawnGameTerms = new List<double[]>();
                    foreach (var g in DrawnGames)
                    {
                        var otherGamma = g.OpponentsAdjustedGamma(Player);
                        if (otherGamma == 0 || double.IsNaN(otherGamma) || double.IsInfinity(otherGamma))
                        {
                            // Something's Wrong!
                        }
                        _drawnGameTerms.Add(new[] { 0.5, 0.5, 1.0, otherGamma });
                    }
                    if (IsFirstDay)
                    {
                        _drawnGameTerms.Add(new[] { 0.5, 0.5, 1.0, 1.0 });
                    }
                }
                return _drawnGameTerms;
            }
        }

        public double LogLikelihoodSecondDerivative
        {
            get
            {
                var sum = 0.0;
                var allTerms = WonGameTerms
                    .Concat(LostGameTerms);
                if (Player.Config.AllowDraws) allTerms = allTerms.Concat(DrawnGameTerms);
                foreach (var t in allTerms)
                {
                    sum += (t[2] * t[3]) / (Math.Pow(t[2] * Gamma + t[3], 2.0));
                }
                if (double.IsNaN(Gamma) || double.IsNaN(sum))
                {
                    // Something's Wrong!
                }
                return -1 * Gamma * sum;
            }
        }

        public double LogLikelihoodDerivative
        {
            get
            {
                var tally = 0.0;
                var allTerms = WonGameTerms
                    .Concat(LostGameTerms);
                if (Player.Config.AllowDraws) allTerms = allTerms.Concat(DrawnGameTerms);
                foreach (var t in allTerms)
                {
                    tally += t[2] / (t[2] * Gamma + t[3]);
                }
                return WonGameTerms.Count() - Gamma * tally;
            }
        }

        public double LogLikelihood
        {
            get
            {
                var tally = 0.0;
                var posGameTerms = WonGameTerms;
                if (Player.Config.AllowDraws) posGameTerms = posGameTerms.Concat(DrawnGameTerms).ToList();
                foreach (var w in posGameTerms)
                {
                    tally += Math.Log(w[0] * Gamma);
                    tally -= Math.Log(w[2] * Gamma + w[3]);
                }
                foreach (var l in LostGameTerms)
                {
                    tally += Math.Log(l[1]);
                    tally -= Math.Log(l[2] * Gamma + l[3]);
                }
                return tally;
            }
        }

        public void AddGame(Game game)
        {
            switch (game.Winner)
            {
                case WHResult.Draw:
                    DrawnGames.Add(game);
                    break;
                case WHResult.Player1Win:
                    if (game.Player1.Id == Player.Id) WonGames.Add(game);
                    else LostGames.Add(game);
                    break;
                case WHResult.Player2Win:
                    if (game.Player2.Id == Player.Id) WonGames.Add(game);
                    else LostGames.Add(game);
                    break;
            }
        }

        public void UpdateBy1DNewtonsMethod()
        {
            R = R - (LogLikelihoodDerivative / LogLikelihoodSecondDerivative);
        }
    }
}
