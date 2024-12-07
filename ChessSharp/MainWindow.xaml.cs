using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Windows.Input;

namespace ChessSharp
{
    public partial class MainWindow : Window
    {
        private const int BoardSize = 8;
        private const double SquareSize = 60;
        private Canvas chessBoard;
        private (int row, int col)? selectedPiece;
        private ChessPiece[,] boardState = new ChessPiece[BoardSize, BoardSize];
        private int turnNum = 1;
        public MainWindow()
        {
            InitializeComponent();
            InitializeChessBoard();
        }

        private void UpdateStatus(string message)
        {
            StatusItem.Content = message;
        }

        private void InitializeChessBoard()
        {
            try
            {
                chessBoard = new Canvas
                {
                    Width = BoardSize * SquareSize,
                    Height = BoardSize * SquareSize
                };

                var boardCanvas = (Canvas)this.FindName("ChessBoardCanvas");
                boardCanvas.Children.Add(chessBoard);

                InitializeBoardState();
                DrawChessBoard();
                DrawPieces();
                UpdateStatus("Chess board initialized successfully.");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error initializing chess board: {ex.Message}");
            }
        }

        private void InitializeBoardState()
        {
            // Initialize the board with ChessPiece objects
            boardState = new ChessPiece[BoardSize, BoardSize]
            {
                { new Rook("Black"), new Knight("Black"), new Bishop("Black"), new Queen("Black"), new King("Black"), new Bishop("Black"), new Knight("Black"), new Rook("Black") },
                { new Pawn("Black"), new Pawn("Black"), new Pawn("Black"), new Pawn("Black"), new Pawn("Black"), new Pawn("Black"), new Pawn("Black"), new Pawn("Black") },
                { null, null, null, null, null, null, null, null },
                { null, null, null, null, null, null, null, null },
                { null, null, null, null, null, null, null, null },
                { null, null, null, null, null, null, null, null },
                { new Pawn("White"), new Pawn("White"), new Pawn("White"), new Pawn("White"), new Pawn("White"), new Pawn("White"), new Pawn("White"), new Pawn("White") },
                { new Rook("White"), new Knight("White"), new Bishop("White"), new Queen("White"), new King("White"), new Bishop("White"), new Knight("White"), new Rook("White") }
            };
        }

        private void DrawChessBoard()
        {
            bool isWhite = false;

            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    var square = new Rectangle
                    {
                        Width = SquareSize,
                        Height = SquareSize,
                        Fill = isWhite ? Brushes.White : Brushes.Gray,
                        Stroke = Brushes.Black,
                        StrokeThickness = 0.5
                    };

                    Canvas.SetLeft(square, col * SquareSize);
                    Canvas.SetTop(square, row * SquareSize);

                    square.MouseLeftButtonDown += (s, e) => OnSquareClick(row, col);

                    chessBoard.Children.Add(square);
                    isWhite = !isWhite;
                }
                isWhite = !isWhite;
            }
        }

        private void DrawPieces()
        {
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    ChessPiece piece = boardState[row, col];
                    if (piece != null)
                    {
                        AddPiece(piece, row, col);
                    }
                }
            }
        }

        private void AddPiece(ChessPiece piece, int row, int col)
        {
            try
            {
                string imagePath = "unset";
                if (piece.PieceType == "Knight")
                {
                    imagePath = $"/Images/{piece.Color[0].ToString().ToLower()}N.png";
                }
                else
                {
                    imagePath = $"/Images/{piece.Color[0].ToString().ToLower()}{piece.PieceType[0].ToString().ToLower()}.png";
                }
                var pieceImage = new Image
                {
                    Source = new BitmapImage(new Uri(imagePath, UriKind.Relative)),
                    Width = SquareSize,
                    Height = SquareSize
                };
                
                Canvas.SetLeft(pieceImage, col * SquareSize);
                Canvas.SetTop(pieceImage, row * SquareSize);
                chessBoard.Children.Add(pieceImage);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error adding piece {piece.PieceType} at ({row}, {col}): {ex.Message}");
            }
        }

        private void OnSquareClick(int row, int col)
        {
            try
            {
                if (selectedPiece == null)
                {
                    SelectPiece(row, col);
                }
                else
                {
                    MoveSelectedPiece(row, col);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Error handling square click: {ex.Message}");
            }
        }

        private void SelectPiece(int row, int col)
        {
            if (boardState[row, col] != null)
            {
                selectedPiece = (row, col);
                ChessPiece selectedPieceObject = boardState[row, col];
                UpdateStatus($"Piece selected at ({row}, {col})");
                if (selectedPieceObject.Color == "White" && turnNum % 2 != 0)
                {
                    HighlightValidMoves(row, col);
                }
                else if (selectedPieceObject.Color == "Black" && turnNum % 2 == 0)
                { 
                
                }
                }
            else
            {
                UpdateStatus("No piece at the clicked square.");
            }
        }

        private void MoveSelectedPiece(int row, int col)
        {
            var (selectedRow, selectedCol) = selectedPiece.Value;
            ChessPiece selectedPieceObject = boardState[selectedRow, selectedCol];
            if (selectedPieceObject.IsValidMove(selectedRow, selectedCol, row, col, boardState))
            {
                if (selectedPieceObject.Color == "White" && turnNum % 2 != 0)
                {
                    MovePiece(selectedRow, selectedCol, row, col);
                    selectedPieceObject.HasMoved = true;
                    SwitchTurn();  // Switch turn after each valid move
                }
                else if (selectedPieceObject.Color == "Black" && turnNum % 2 == 0)
                {
                    MovePiece(selectedRow, selectedCol, row, col);
                    selectedPieceObject.HasMoved = true;
                    SwitchTurn();  // Switch turn after each valid move
                }
                else
                {
                    return;
                }

            }
            ClearHighlights();
            selectedPiece = null;
        }

        private void HighlightValidMoves(int row, int col)
        {
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    if (boardState[row, col]?.IsValidMove(row, col, r, c, boardState) == true)
                    {
                        var highlight = new Rectangle
                        {
                            Width = SquareSize,
                            Height = SquareSize,
                            Fill = Brushes.Yellow,
                            Opacity = 0.5
                        };

                        Canvas.SetLeft(highlight, c * SquareSize);
                        Canvas.SetTop(highlight, r * SquareSize);
                        chessBoard.Children.Add(highlight);
                    }
                }
            }
        }

        private void MovePiece(int fromRow, int fromCol, int toRow, int toCol)
        {
            ChessPiece piece = boardState[fromRow, fromCol];
            boardState[fromRow, fromCol] = null;
            boardState[toRow, toCol] = piece;

            chessBoard.Children.Clear();
            DrawChessBoard();
            DrawPieces();
            UpdateStatus("Piece moved and board redrawn.");
        }

        private void ClearHighlights()
        {
            chessBoard.Children.Clear();
            DrawChessBoard();
            DrawPieces();
        }

        private void ChessBoardCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(ChessBoardCanvas);
            int row = (int)(mousePos.Y / SquareSize);
            int col = (int)(mousePos.X / SquareSize);
            OnSquareClick(row, col);
        }
        private void SwitchTurn()
        {
            // Alternate turns between 1 (White) and 2 (Black)
            turnNum++;

            // Update status to show whose turn it is
            
            UpdateStatus($"it is Turn Number: {turnNum}");
        }
    }

    public abstract class ChessPiece
    {
        public string Color { get; set; }
        public bool HasMoved { get; set; }  // Track if the piece has moved
        public string PieceType { get; set; }  // Store piece type (e.g., "Pawn", "Rook", etc.)

        public ChessPiece(string color, string pieceType)
        {
            Color = color;
            PieceType = pieceType;
            HasMoved = false;
        }

        public abstract bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol, ChessPiece[,] boardState);
    }
    public class Pawn : ChessPiece
    {
        public Pawn(string color) : base(color, "Pawn") { }

        public override bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol, ChessPiece[,] boardState)
        {
            if (Color == "White")
            {
                // White pawn can move 1 step forward always as long as no piece is in the way
                if (fromRow - 1 == toRow && fromCol == toCol && boardState[toRow, toCol] == null)
                    return true;
                // White pawn can capture diagonally
                if (fromRow - 1 == toRow && Math.Abs(fromCol - toCol) == 1 && boardState[toRow, toCol]?.Color == "Black")
                    return true;
                // Pawns can move two squares on their first move.
                if (fromRow - 2 == toRow && fromCol == toCol && boardState[toRow, toCol] == null && HasMoved == false) 
                    return true;
                
            }
            else if (Color == "Black")
            {
                // Black pawn can move 1 step forward
                if (fromRow + 1 == toRow && fromCol == toCol && boardState[toRow, toCol] == null)
                    return true;
                // Black pawn can capture diagonally
                if (fromRow + 1 == toRow && Math.Abs(fromCol - toCol) == 1 && boardState[toRow, toCol]?.Color == "White")
                    return true;// Pawns can move two squares on their first move.
                if (fromRow + 2 == toRow && fromCol == toCol && boardState[toRow, toCol] == null && HasMoved == false)
                    return true;
            }

            return false;
        }
    }
    public class Rook : ChessPiece
    {
        public Rook(string color) : base(color, "Rook") { }

        public override bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol, ChessPiece[,] boardState)
        {
            // Rook moves vertically or horizontally
            if (fromRow == toRow || fromCol == toCol)
            {
                int rowDir = toRow > fromRow ? 1 : (toRow < fromRow ? -1 : 0);
                int colDir = toCol > fromCol ? 1 : (toCol < fromCol ? -1 : 0);

                // Check for obstacles between the current position and target position
                int r = fromRow + rowDir;
                int c = fromCol + colDir;
                if (boardState[toRow, toCol] != null && boardState[toRow, toCol].Color == Color)
                    return false;
                if (fromRow == toRow && fromCol == toCol)
                    return false;
                while (r != toRow || c != toCol)
                {
                    if (boardState[r, c] != null) return false;  // Blocked by another piece
                    r += rowDir;
                    c += colDir;
                }
                return true;
            }

            return false;
        }
    }
    public class King : ChessPiece
    {
        public King(string color) : base(color, "King") { }

        public override bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol, ChessPiece[,] boardState)
        {
            if (boardState[toRow, toCol] != null && boardState[toRow, toCol].Color == Color)
                return false;
            // King can move 1 square in any direction
            return Math.Abs(fromRow - toRow) <= 1 && Math.Abs(fromCol - toCol) <= 1;
        }
    }
    public class Queen : ChessPiece
    {
        public Queen(string color) : base(color, "Queen") { }

        public override bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol, ChessPiece[,] boardState)
        {
            if (boardState[toRow, toCol] != null && boardState[toRow, toCol].Color == Color)
                return false;
            // Queen moves like both a rook and a bishop (straight or diagonal)
            Rook rook = new Rook(Color);
            Bishop bishop = new Bishop(Color);

            return rook.IsValidMove(fromRow, fromCol, toRow, toCol, boardState) || bishop.IsValidMove(fromRow, fromCol, toRow, toCol, boardState);
        }
    }
    public class Bishop : ChessPiece
    {
        public Bishop(string color) : base(color, "Bishop") { }

        public override bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol, ChessPiece[,] boardState)
        {
            if (boardState[toRow, toCol] != null && boardState[toRow, toCol].Color == Color)
                return false;
            // Bishop moves diagonally
            if (Math.Abs(fromRow - toRow) == Math.Abs(fromCol - toCol))
            {
                int rowDir = toRow > fromRow ? 1 : (toRow < fromRow ? -1 : 0);
                int colDir = toCol > fromCol ? 1 : (toCol < fromCol ? -1 : 0);

                // Check for obstacles between the current position and target position
                int r = fromRow + rowDir;
                int c = fromCol + colDir;
                while (r != toRow || c != toCol)
                {
                    if (boardState[r, c] != null) return false;  // Blocked by another piece
                    r += rowDir;
                    c += colDir;
                }
                return true;
            }

            return false;
        }
    }
    public class Knight : ChessPiece
    {
        public Knight(string color) : base(color, "Knight") { }

        public override bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol, ChessPiece[,] boardState)
        {
            if (boardState[toRow, toCol] != null && boardState[toRow, toCol].Color == Color)
                return false;
            // Knight moves in an "L" shape
            return (Math.Abs(fromRow - toRow) == 2 && Math.Abs(fromCol - toCol) == 1) ||
                   (Math.Abs(fromRow - toRow) == 1 && Math.Abs(fromCol - toCol) == 2);
        }
    }
    public class Board
    {
        public ChessPiece[,] BoardState { get; set; }

        public Board()
        {
            // Initialize the board with 8x8 grid
            BoardState = new ChessPiece[8, 8];

            // Initialize black pieces
            BoardState[0, 0] = new Rook("Black");
            BoardState[0, 1] = new Knight("Black");
            BoardState[0, 2] = new Bishop("Black");
            BoardState[0, 3] = new Queen("Black");
            BoardState[0, 4] = new King("Black");
            BoardState[0, 5] = new Bishop("Black");
            BoardState[0, 6] = new Knight("Black");
            BoardState[0, 7] = new Rook("Black");
            for (int i = 0; i < 8; i++) BoardState[1, i] = new Pawn("Black");

            // Initialize white pieces
            BoardState[7, 0] = new Rook("White");
            BoardState[7, 1] = new Knight("White");
            BoardState[7, 2] = new Bishop("White");
            BoardState[7, 3] = new Queen("White");
            BoardState[7, 4] = new King("White");
            BoardState[7, 5] = new Bishop("White");
            BoardState[7, 6] = new Knight("White");
            BoardState[7, 7] = new Rook("White");
            for (int i = 0; i < 8; i++) BoardState[6, i] = new Pawn("White");
        }
    }

}
