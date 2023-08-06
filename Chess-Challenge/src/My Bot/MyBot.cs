using ChessChallenge.API;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;


public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int factor = board.IsWhiteToMove ? 1 : -1;
        Move[] moves = board.GetLegalMoves();

        Console.WriteLine("--------------");
        List<float> scores = new(moves.Length);
        List <(float, float, float)> breakDownList = new(moves.Length);
        for(int i = 0; i < moves.Length; i++){
            board.MakeMove(moves[i]);
            var tup = ScoreBoard(board);
            scores.Add(factor * tup.Item1);
            breakDownList.Add(tup.Item2);
            board.UndoMove(moves[i]);
        }

        int highestScoreIndex = scores.IndexOf(scores.Max());
        List<int> highestIndices = scores
            .Select((number, index) => new { Number = number, Index = index })
            .OrderByDescending(item => item.Number)
            .Take(3)
            .Select(item => item.Index)
            .ToList();

        foreach (int i in highestIndices){
            var breakDown = breakDownList[i];
            Console.WriteLine(
                $"score: {scores[i]}\t" +
                $"move: {moveToString(moves[i])}\t" +
                $"pVal: {breakDown.Item1}\t" + 
                $"tVal: {breakDown.Item2}\t" + 
                $"fVal: {breakDown.Item3}\t"
                );
        }
        return moves[highestScoreIndex];
    }

    static readonly Dictionary<PieceType, int> PieceValMap = new()
    {
        {PieceType.Pawn, 1},
        {PieceType.Knight, 3},
        {PieceType.Bishop, 3},
        {PieceType.Rook, 5},
        {PieceType.Queen, 9},
        {PieceType.King, 100}
    };

    private static (float, (float, float, float)) ScoreBoard(Board board){
        int factor = board.IsWhiteToMove ? 1 : -1;
        float fWeight = 0.02F;
        float pVal = CountPieceVal(board);
        //float tVal = factor * SearchCaptureTree(board);
        float tVal = CountThreatVal(board);
        float fVal = CountPieceFreedom(board) * fWeight;
        float score = pVal + tVal + fVal;
        var breakDown = (pVal, tVal, fVal);
        var ret = (score, breakDown);
        return ret;
    }

    private static String moveToString(Move move){
        return move.StartSquare.Name + move.TargetSquare.Name;
    }

    private static int CountPieceVal(Board board)
    {
        int total = 0;
        PieceList[] pLists = board.GetAllPieceLists();
        foreach (PieceList pList in pLists){
            if (pList.IsWhitePieceList){
                total += pList.Count * PieceValMap[pList.TypeOfPieceInList];
            } else {
                total -= pList.Count * PieceValMap[pList.TypeOfPieceInList];
            }
        }
        return total;
    }

    private static float CountPieceFreedom(Board board) {
        float total = 0;
        int factor = board.IsWhiteToMove? 1 : -1;
        total += factor * board.GetLegalMoves().Length;
        board.ForceSkipTurn();
        total -= factor * board.GetLegalMoves().Length;
        board.UndoSkipTurn();

        return total;
    }

    private static float SearchCaptureTree(Board board){
        float val = SCapTree_r(board, 0);
        return val;
    }

    private static float SCapTree_r(Board board, int depth){
        List<Move> capList = new(board.GetLegalMoves(capturesOnly:true));
        List<Move> moveList = new(board.GetLegalMoves());
        List<Move> nonCapList = new(moveList.Except(capList));
        int factor = board.IsWhiteToMove ? 1 : -1;

        if (nonCapList.Count > 0){
            Move lowestThreatMove = nonCapList[0];
            board.MakeMove(nonCapList[0]);
            float lowestThreatMoveVal = -1 * factor * CountThreatVal(board);
            board.UndoMove(nonCapList[0]);
            foreach (Move move in nonCapList) {
                //Console.WriteLine(moveToString(move));
                board.MakeMove(move);
                float immediateThreatVal = -1 * factor * CountThreatVal(board);
                if (immediateThreatVal < lowestThreatMoveVal){
                    lowestThreatMove = move;
                    lowestThreatMoveVal = immediateThreatVal;
                }
                board.UndoMove(move);
            }
            //Console.WriteLine(moveToString(lowestThreatMove));
            capList.Add(lowestThreatMove);
        }

        List<float> scoreList = new(capList.Count);

        if (capList.Count < 2 || depth > 8){
            return factor * CountPieceVal(board);
        }
        
        /* I think I need to account for the fact that the opponent won't just
         mindlessly capture over and over, perhaps by adding a few moves to 
         explore that maximally reduce the threat value */
        
        foreach (Move cap in capList){
            board.MakeMove(cap);
            scoreList.Add(factor * SCapTree_r(board, depth+1));
            board.UndoMove(cap);
        }
        return scoreList.Min();
    }

    private static float CountThreatVal_r(Board board, int depth=0){
        float total = 0;
        float factor = board.IsWhiteToMove? 1 : -1;
        Move[] availableCaptures = board.GetLegalMoves(capturesOnly:true);

        foreach (Move move in availableCaptures){
            float cap_piece_val = PieceValMap[move.CapturePieceType];
            float move_piece_val = PieceValMap[move.MovePieceType];

            total += factor * cap_piece_val / move_piece_val;
            if (depth == 0) {
                board.ForceSkipTurn();
                total += CountThreatVal_r(board, depth + 1);
                board.UndoSkipTurn();
            }
        }

        return total;
    }


    private static float immediateThreatVal(Board board){
        return immediateThreatVal_r(board, 0);
    }

    private static float immediateThreatVal_r(Board board, int depth){

        int factor = board.IsWhiteToMove ? 1 : -1;
        float currentThreatVal = CountThreatVal(board);
        // If the currentThreatVal is really small we are probably fine
        if (Math.Abs(factor * currentThreatVal) < 0.5 || depth > 12){
            return currentThreatVal;
        }

        // if currentThreatVal is unfavorable look for a move that equalizes
        if (factor * currentThreatVal < 0){
            
        }


    }

    private static float CountThreatVal(Board board){
        return CountThreatVal_r(board, 0);
    }
}