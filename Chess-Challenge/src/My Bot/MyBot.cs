using ChessChallenge.API;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;


public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        /*Console.WriteLine("CountThreatVal:");
        Console.WriteLine(CountThreatVal(board));
        Console.WriteLine("CountPieceVal:");
        Console.WriteLine(CountPieceVal(board));*/

        Console.WriteLine("--------------");
        List<float> scores = new(moves.Length);
        for(int i = 0; i < moves.Length; i++){
            board.MakeMove(moves[i]);
            scores.Add(ScoreBoard(board));
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
            Console.WriteLine(
                $"score: \t{scores[i]} \tmove:{moveToString(moves[i])}"
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

    private static float ScoreBoard(Board board){
        float fWeight = 0.1F;
        int pVal = CountPieceVal(board);
        float tVal = SearchCaptureTree(board);
        float fVal = CountPieceFreedom(board);
        float score = (float)pVal + tVal + (fVal*fWeight);
        return score;
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
        /*
        PieceList[] pLists = board.GetAllPieceLists();
        foreach (PieceList plist in pLists){
            foreach(Piece piece in plist){
                piece.Square.
            }
        }
        */

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
        Move[] moveList = board.GetLegalMoves(capturesOnly:true);
        List<float> scoreList = new(moveList.Length);

        if (moveList.Length == 0 || depth > 4){
            return CountPieceVal(board);
        }


        foreach (Move move in moveList){
            board.MakeMove(move);
            scoreList.Add(SCapTree_r(board, depth+1));
            board.UndoMove(move);
        }
        return scoreList.Max();
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

    private static float CountThreatVal(Board board){
        return CountThreatVal_r(board, 0);
    }
}