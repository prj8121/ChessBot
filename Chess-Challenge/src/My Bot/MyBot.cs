using ChessChallenge.API;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        
        if (len(moves) >= 2)
        {
            return moves[1];
        }
	
        return moves[0];
    }
}