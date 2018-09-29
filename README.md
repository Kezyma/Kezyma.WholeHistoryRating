# Kezyma.WholeHistoryRating

C# Port of the Whole History Rating system from Ruby https://github.com/goshrine/whole_history_rating

Usage:

Create a new instance of the WholeHistoryRating and supply a config.
The parameters for the config are the w2 value as a double and a boolean representing whether draws are allowed.

    var config = new Config(300, false);
    var whr = new WholeHistoryRating(config);
    
Then add your games using CreateGame() supplying a player id for each player, the result of the game and an integer for the 'day number' of the game.

    whr.CreateGame(1, 2, WHResult.Player1Win, 1); // Player 1 defeats Player 2 on Day 1
    whr.CreateGame(1, 3, WHResult.Player1Win, 2); // Player 1 defeats Player 3 on Day 2
    whr.CreateGame(2, 3, WHResult.Player2Win, 3); // Player 3 defeats Player 2 on Day 3

Then you can iterate by using Iterate() and supplying an integer representing the number of iterations.

    whr.Iterate(200);
    
To get your resulting rankings you can then access the Elo values of the last PlayerDay of each Player.

    var rankings = whr.Players.Select(p => new KeyValuePair(p.Id, p.Days.Last().Elo)).OrderByDescending(p => p.Value);
