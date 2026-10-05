using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace MelodySuite.Match3.Runtime
{
    public class Board
    {
        // public PieceDefinition[] pieces { get; }
        
        private GamePiece[][] board;
        public int rows { get; private set; }
        public int columns { get; private set; }
        public Board(int rows, int columns, IGamePieceGenerator gamePieceGenerator)
        {
            this.gamePieceGenerator = gamePieceGenerator;
            // this.pieces = pieces;
            this.rows = rows;
            this.columns = columns;
        
            board = new GamePiece[rows][];
            for (var row = board.Length - 1; row >= 0; row--)
            {
                board[row] = new GamePiece[columns];
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is not Board other)
                return false;
            if (other.rows != rows || other.columns != columns)
                return false;
            for (var row = 0; row < board.Length; row++)
            {
                for (var column = 0; column < board[row].Length; column++)
                {
                    if (board[row][column] != other.board[row][column])
                        return false;
                }
            }
            return true;
        }

        public Board(Board other)
        {
            rows = other.rows;
            columns = other.columns;
            // pieces = other.pieces;
        
            board = new GamePiece[rows][];
            for (var row = board.Length - 1; row >= 0; row--)
            {
                board[row] = new GamePiece[columns];
            }
        
            for (var row = 0; row < other.board.Length; row++)
            {
                for (var column = 0; column < other.board[row].Length; column++)
                {
                    board[row][column] = other.board[row][column];
                }
            }
        }

        public List<Match> GetEstimatedMatches(BoardPosition i, BoardPosition i1)
        {
            var board = new Board(this);
            board.Swap(i, i1, SwapType.ShiftTiles, checkIfMatches: false);
            return board.FindMatches();
        }

        public bool InLine(BoardPosition i, BoardPosition i1)
        {
            return i.Row == i1.Row || i.Column == i1.Column;
        }
    
        public int IndexOf(GamePiece piece)
        {
            for (var i = 0; i < board.Length; i++)
            {
                for (var i1 = 0; i1 < board[i].Length; i1++)
                {
                    if (piece == board[i][i1])
                        return i;
                }
            }

            throw new ArgumentOutOfRangeException();
        }

        public IBoardHandler Handler { get; set; }

        public IEnumerator Matches()
        {
            var initialMatches = FindMatches();
            if (initialMatches.Count > 0)
            {
                if (Handler != null)
                    yield return Handler.HandleMatches(initialMatches);

                FallData fallData;
                while ((fallData = Fall()).tilesToFill.Count > 0)
                {
                    foreach (var boardPosition in fallData.tilesToFill)
                    {
                        SetPiece(boardPosition.Row, boardPosition.Column, gamePieceGenerator.Generate());
                    }
            
                    if (Handler != null)
                        yield return Handler.HandleFall(fallData);
                
                    var matches = FindMatches();
                    if (matches.Count == 0)
                        continue;
                
                    if (Handler != null)
                        yield return Handler.HandleMatches(matches);
                }
            }
        }
        
        public IEnumerator Move(BoardPosition piece, BoardPosition destination, SwapType type, Action callback = null)
        {
            var swapped = Swap(piece, destination, type);
            if (!swapped)
                throw new Exception("Invalid Move");
            if (Handler != null)
                yield return Handler.HandleSwap();

            yield return Matches();
        
            callback?.Invoke();
        }

        public enum SwapType
        {
            Simple,
            ShiftTiles
        }
    
        public bool Swap(BoardPosition piece, BoardPosition destination, SwapType type, List<(BoardPosition from, BoardPosition to)> moves = null, bool checkIfMatches = true)
        {
            if (checkIfMatches && GetEstimatedMatches(piece, destination).Count == 0)
                return false;

            return type switch
            {
                SwapType.Simple => SimpleTileSwap(piece, destination),
                SwapType.ShiftTiles => ShiftTileSwap(piece, destination, moves),
                _ => false
            };
        }

        private bool SimpleTileSwap(BoardPosition piece, BoardPosition destination)
        {
            var fromPiece = board[piece.Row][piece.Column];
            var toPiece = board[destination.Row][destination.Column];
            board[piece.Row][piece.Column] = fromPiece;
            board[destination.Row][destination.Column] = toPiece;
            return true;
        }

        private bool ShiftTileSwap(BoardPosition piece, BoardPosition destination, List<(BoardPosition from, BoardPosition to)> moves = null)
        { 
            if (!InLine(piece, destination))
                throw new Exception("Invalid swap");
            var piecePiece = board[piece.Row][piece.Column];
            if (piece.Row != destination.Row)
            {
                var column = piece.Column;
            
                // left
                if (destination.Row < piece.Row)
                {
                    for (var row = piece.Row; row > destination.Row; row--)
                    {
                        if (moves != null && board[row][column] != board[row - 1][column])
                            moves.Add((new BoardPosition(row, column), new BoardPosition(row - 1, column)));
                        board[row][column] = board[row - 1][column];
                    }
                }
                else
                {
                    for (var row = piece.Row; row < destination.Row; row++)
                    {
                        if (moves != null && board[row][column] != board[row + 1][column]) 
                            moves.Add((new BoardPosition(row, column), new BoardPosition(row + 1, column)));
                        board[row][column] = board[row + 1][column];
                    }
                }

                board[destination.Row][column] = piecePiece;
                return true;
            }
            else if (piece.Column != destination.Column)
            {
                var row = piece.Row;
            
                // right
                if (destination.Column < piece.Column)
                {
                    for (var column = piece.Column; column > destination.Column; column--)
                    {
                        if (moves != null && board[row][column] != board[row][column - 1]) 
                            moves.Add((new BoardPosition(row, column), new BoardPosition(row, column - 1)));
                        board[row][column] = board[row][column - 1];
                    }
                }
                else
                {
                    for (var column = piece.Column; column < destination.Column; column++)
                    {
                        if (moves != null && board[row][column] != board[row][column + 1]) 
                            moves.Add((new BoardPosition(row, column), new BoardPosition(row, column + 1)));
                        board[row][column] = board[row][column + 1];
                    }
                }

                board[row][destination.Column] = piecePiece;
                return true;
            }

            return true;
        }
    
        // public GamePiece Generate()
        // {
        //     return new GamePiece(pieces[Random.Range(0, pieces.Length)]);
        // }
    
        public void GenerateBoard()
        {
            for (var row = board.Length - 1; row >= 0; row--)
            {
                for (var column = board[row].Length - 1; column >= 0; column--)
                {
                    board[row][column] = gamePieceGenerator.Generate();
                }
            }
        
            FindMatches();
        
            FallData fallData;
            while ((fallData =  Fall()).tilesToFill.Count > 0)
            {
                foreach (var boardPosition in fallData.tilesToFill)
                {
                    board[boardPosition.Row][boardPosition.Column] = gamePieceGenerator.Generate();
                }

                FindMatches();
            }
        }

        public List<Match> FindMatches(bool clear = true)
        {
            List<Match> matches = new List<Match>();
            for (var i = 0; i < board.Length; i++)
            {
                for (var i1 = 0; i1 < board[i].Length; i1++)
                {
                    var match = FindMatch(i, i1);
                    if (match == null)
                        continue;
                    matches.Add(match);

                    if (!clear)
                        continue;
                
                    foreach (var boardPosition in match.Tiles)
                    {
                        board[boardPosition.Row][boardPosition.Column] = null;
                    }
                }
            }

            return matches;
        }
    
        public FallData Fall()
        {
            List<BoardPosition> tilesToFill = new List<BoardPosition>();
            List<(Vector2Int origin, Vector2Int destination)> movements = new();
            for (var column = 0; column < columns; column++)
            {
                var targetRow = 0;

                for (var row = 0; row < rows; row++)
                {
                    if (board[row][column] == null)
                        continue;

                    if (targetRow != row)
                    {
                        board[targetRow][column] = board[row][column];
                        board[row][column] = null;
                        movements.Add((new Vector2Int(column, row), new Vector2Int(column, targetRow)));
                    }

                    targetRow++;
                }
            
                for (var row = targetRow; row < rows; row++)
                {
                    tilesToFill.Add(new BoardPosition(row, column));
                }
            }
            return new FallData(movements, tilesToFill);
        }
    
        private List<Vector2Int> CollectPieces(Vector2Int start, Vector2Int direction)
        {
            List<Vector2Int> pieces = new List<Vector2Int>();

            var startPiece = board[start.y][start.x];

            if (startPiece == null)
                return pieces;
        
            for (int i = 1; i < Mathf.Max(rows, columns); i++)
            {
                var targetX = start.x + direction.x * i;
                var targetY = start.y + direction.y * i;

                if (targetY < 0 || targetY >= rows ||
                    targetX < 0 || targetX >= columns)
                {
                    break;
                }

                var targetPiece = board[targetY][targetX];

                if (targetPiece == null)
                    break;

                if (targetPiece.Type != startPiece.Type)
                    break;

                pieces.Add(new Vector2Int(targetX, targetY));
            }

            return pieces;
        }

        public readonly IGamePieceGenerator gamePieceGenerator;
        
        [CanBeNull]
        public Match FindMatch(int row, int column)
        {
            var matchLength = 3;

            var horizontal = new List<Vector2Int>();
            var piece = board[row][column];
        
            horizontal.AddRange(CollectPieces(new Vector2Int(column, row), new Vector2Int(1, 0)));
            horizontal.AddRange(CollectPieces(new Vector2Int(column, row), new Vector2Int(-1, 0)));

            var vertical = new List<Vector2Int>();
            vertical.AddRange(CollectPieces(new Vector2Int(column, row), new Vector2Int(0, 1)));
            vertical.AddRange(CollectPieces(new Vector2Int(column, row), new Vector2Int(0, -1)));
        
            List<Vector2Int> otherPieces = new List<Vector2Int>();
            if (horizontal.Count + 1 >= matchLength)
            {
                otherPieces.AddRange(horizontal);
            }

            if (vertical.Count + 1 >= matchLength)
            {
                otherPieces.AddRange(vertical);
            }

            if (otherPieces.Count <= 0)
                return null;
        
            var tiles = new List<BoardPosition> { new(row, column) };
            tiles.AddRange(otherPieces.Select(o => new BoardPosition(o.y, o.x)));
        
            return new Match(tiles, piece.Type, tiles.Select(o => GetPiece(o.Row, o.Column)).ToList());
        }

        public void ComputeIfPresent(Action<int, int> action)
        {
            for (var i = board.Length - 1; i >= 0; i--)
            {
                for (var i1 = board[i].Length - 1; i1 >= 0; i1--)
                {
                    if (board[i][i1] == null)
                        continue;
                    action(i, i1);
                }
            }
        }

        public GamePiece GetPiece(int row, int column)
        {
            if (row < 0 || column < 0)
                return null;
        
            if (row >= rows || column >= columns)
                return null;
        
            return board[row][column];
        }

        public void SetPiece(int row, int column, GamePiece piece)
        {
            board[row][column] = piece;
        }
    }

    public interface IGamePieceGenerator
    {
        GamePiece Generate();
    }
    
    public class GamePiece
    {
        public PieceDefinition Type { get; }

        public GamePiece(PieceDefinition type)
        {
            Type = type;
        }
    }

    public class Match
    {

        public int ValuePerPiece => Length - (3 - 1);

        public Match(List<BoardPosition> tiles, PieceDefinition piece, List<GamePiece> pieces)
        {
            Pieces = pieces;
            Tiles = tiles;
            Piece = piece;
        }

        public List<BoardPosition> Tiles { get; }
        public PieceDefinition Piece { get; }
        public int Length => Tiles.Count;
        public List<GamePiece> Pieces { get; }
    }

    public class FallData
    {
        public FallData(List<(Vector2Int origin, Vector2Int destination)> movements, List<BoardPosition> tilesToFill)
        {
            this.movements = movements;
            this.tilesToFill = tilesToFill;
        }

        public List<(Vector2Int origin, Vector2Int destination)> movements;
        public List<BoardPosition> tilesToFill;
    }

    public interface IBoardHandler
    {
        IEnumerator HandleSwap(Action onComplete = null);
        IEnumerator HandleMatches(List<Match> matches, Action onComplete = null);
        IEnumerator HandleFall(FallData data, Action onComplete = null);
    }
    
    public class BoardPosition
    {
        public override string ToString()
        {
            return "[" + Row + "," + Column + "]";
        }

        public BoardPosition(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public readonly int Row;
        public readonly int Column;

        public override bool Equals(object? obj)
        {
            return obj is BoardPosition other &&
                   Row == other.Row &&
                   Column == other.Column;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }

        public static bool operator ==(BoardPosition left, BoardPosition right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null || right is null)
                return false;

            return left.Row == right.Row &&
                   left.Column == right.Column;
        }

        public static bool operator !=(BoardPosition left, BoardPosition right)
        {
            return !(left == right);
        }

        public Vector2Int ToVector2Int()
        {
            return new Vector2Int(Column, Row);
        }
    }
}