using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kezyma.WholeHistoryRating
{
    public class WholeHistoryRating
    {
        public IList<Player> Players;
        public IList<Game> Games;
        public Config Config;
        
        public WholeHistoryRating(Config config)
        {
            Config = config;
            Players = new List<Player>();
            Games = new List<Game>();
        }

        public double LogLikelihood
        {
            get
            {
                var score = 0.0;
                foreach (var p in Players)
                {
                    if (p.Days != null && p.Days.Any()) score += p.LogLikelihood;
                }
                return score;
            }
        }

        public Player PlayerById(int id)
        {
            var player = Players.FirstOrDefault(x => x.Id == id);
            if (player == null)
            {
                Players.Add(new Player(id, Config));
                return PlayerById(id);
            }
            return player;
        }

        public IList<double[]> RatingsForPlayer(int id)
        {
            var player = PlayerById(id);
            return player.Days.Select(x => new [] { x.Day, x.Elo, (x.Uncertainty * 100.0) }).ToList();
        }

        public Game SetupGame(int player1, int player2, WHResult winner, int timeStep)
        {
            var p1 = PlayerById(player1);
            var p2 = PlayerById(player2);
            var game = new Game(p1, p2, winner, timeStep);
            return game;
        }

        public Game CreateGame(int player1, int player2, WHResult winner, int timeStep)
        {
            var game = SetupGame(player1, player2, winner, timeStep);
            return AddGame(game);
        }

        public Game AddGame(Game game)
        {
            game.Player1.AddGame(game);
            game.Player2.AddGame(game);
            Games.Add(game);
            return game;
        }

        public void Iterate(int count)
        {
            for (int i = 0; i < count; i++)
            {
                RunOneIteration();
            }
            UpdateAllUncertainty();
        }

        private void UpdateAllUncertainty()
        {
            foreach (var p in Players)
            {
                p.UpdateUncertainty();
            }
        }

        public void IterateUntilConvergenceOf(double convergence = 0.001) // Default = 10 * Math.Pow(10, -4)
        {
            //if (convergence == 0) convergence = ();
            var currentLL = LogLikelihood;
            int i = 0;
            while (Math.Abs(currentLL - LogLikelihood) > convergence || i == 0)
            {
                currentLL = LogLikelihood;
                i++;
                RunOneIteration();
            }
            UpdateAllUncertainty();
        }

        public void RunOneIteration()
        {
            foreach (var p in Players)
            {
                p.RunOneNewtonIteration();
            }
        }
    }
}
