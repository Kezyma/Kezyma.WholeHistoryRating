using System;
using System.Collections.Generic;
using System.Text;

namespace Kezyma.WholeHistoryRating
{
    public enum WHResult
    {
        Player1Win = 1,
        Player2Win = 2,
        Draw = 3
    }

    public class Game
    {
        public WHResult Winner;
        public Player Player1;
        public Player Player2;
        public PlayerDay Player1Day;
        public PlayerDay Player2Day;
        public int Day { get; set; }

        public Game(Player player1, Player player2, WHResult winner, int timeStep)
        {
            Player1 = player1;
            Player2 = player2;
            Winner = winner;
            Day = timeStep;
        }

        public double OpponentsAdjustedGamma(Player player)
        {
            double opponentElo = 0;
            if (player.Id == Player1.Id)
            {
                opponentElo = Player2Day.Elo;
            }
            if (player.Id == Player2.Id)
            {
                opponentElo = Player1Day.Elo;
            }
            var rval = Math.Pow(10, opponentElo / 400.0);
            if (rval == 0 || double.IsInfinity(rval) || double.IsNaN(rval))
            {
                // Something's Wrong.
            }
            return rval;
        }

        public Player Opponent(Player player)
        {
            if (player.Id == Player1.Id)
            {
                return Player2;
            }
            else
            {
                return Player1;
            }
        }

        public double PredictionScore
        {
            get
            {
                if (Player1WinProbability == 0.5)
                {
                    if (Winner == WHResult.Draw) return 1.0;
                    return 0.5;
                }
                else
                {
                    return ((Winner == WHResult.Player1Win && Player1WinProbability > 0.5) || (Winner == WHResult.Player2Win && Player1WinProbability < 0.5)) ? 1.0 : 0.0;
                }
            }
        }

        public double Player1WinProbability => Player1Day.Gamma / (Player1Day.Gamma + OpponentsAdjustedGamma(Player1));
        
        public double Player2WinProbability => Player2Day.Gamma / (Player2Day.Gamma + OpponentsAdjustedGamma(Player2));

    }
}
