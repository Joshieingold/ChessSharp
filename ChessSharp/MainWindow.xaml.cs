using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace ChessSharp
{
    public partial class MainWindow : Window
    {
        // Global variables.
        private const int BoardSize = 8;
        private const double SquareSize = 60;
        private Canvas chessBoard;
        private (int row, int col)? selectedPiece; // Global tracking of the selected pieces location
        private ChessPiece[,] boardState = new ChessPiece[BoardSize, BoardSize];
        private int turnNum = 1; // Slightly useless now, but mainly keeps track of whose turn it is
        private List<string> FENList = new List<string>();
        int currentIndex = 0;
        public ObservableCollection<ChessMove> MoveHistory { get; set; } = new ObservableCollection<ChessMove>(); // Stores the moves, mainly for en passant.
        public MainWindow()
        {
            InitializeComponent();
            InitializeChessBoard();
            ScoreSheetListBox.ItemsSource = MoveHistory;
        }
        private void RecordMove(int turnNumber, string whiteMove, string blackMove) // Adds move number, and the move made to the movehistory, stored as a class (ChessMove)
        {
            // Add a new move to the history
            MoveHistory.Add(new ChessMove
            {
                TurnNumber = turnNumber,
                WhiteMove = whiteMove,
                BlackMove = blackMove
            });
        }
        private void ClearScoreSheet_Click(object sender, RoutedEventArgs e)
        {
            // Clear the move history
            MoveHistory.Clear();
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
                string fen = GenerateFen(boardState);
                FENList.Add(fen); // Add the FEN string to the list
                UpdateStatus("Chess board initialized successfully.");

            }
            catch (Exception ex)
            {
                UpdateStatus($"Error initializing chess board: {ex.Message}");
            }
        }
        private void InitializeBoardState() =>
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
        private void PerformCastling(King king, int fromRow, int fromCol, int toRow, int toCol)
        {
            // Move the king
            boardState[toRow, toCol] = king;
            boardState[fromRow, fromCol] = null;
            king.HasMoved = true;

            // Determine if kingside or queenside castling
            int rookFromCol = toCol > fromCol ? fromCol + 3 : fromCol - 4; // Rook's starting column
            int rookToCol = toCol > fromCol ? toCol - 1 : toCol + 1;       // Rook's ending column

            // Move the rook
            ChessPiece rook = boardState[fromRow, rookFromCol];
            boardState[fromRow, rookToCol] = rook;
            boardState[fromRow, rookFromCol] = null;
            rook.HasMoved = true;
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
                string imagePath = "initialize";
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
                    HighlightValidMoves(row, col);
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
            bool isCapture = boardState[row, col] != null; // Check if a piece exists at the destination
            // Check if the move is valid
            if (selectedPieceObject.IsValidMove(selectedRow, selectedCol, row, col, boardState))
            {
                
                string moveNotation = GetMoveNotation(selectedPieceObject, selectedRow, selectedCol, row, col, isCapture);
                if (selectedPieceObject.Color == "White" && turnNum % 2 != 0)
                {
                    if (selectedPieceObject is King king && Math.Abs(col - selectedCol) == 2)
                    {
                        PerformCastling(king, selectedRow, selectedCol, row, col);
                        moveNotation = "O-O" + (col > selectedCol ? "" : "O-O-O"); // King-side or Queen-side castling notation
                        string fen = GenerateFen(boardState);
                        FENList.Add(fen); // Add the FEN string to the list
                        UpdateStatus(fen);
                    }
                    else if (selectedPieceObject is Pawn pawn && Math.Abs(selectedCol - col) == 1 && boardState[row, col] == null)
                    {
                        // Handle en passant capture for White
                        int capturedRow = row + 1; // En passant target is one row below the destination
                        boardState[capturedRow, col] = null; // Remove the captured pawn
                        moveNotation += " e.p."; // En passant notation
                        MovePiece(selectedRow, selectedCol, row, col);
                        string fen = GenerateFen(boardState);
                        FENList.Add(fen); // Add the FEN string to the list
                        UpdateStatus(fen);
                    }
                    else
                    {
                        MovePiece(selectedRow, selectedCol, row, col);
                        string fen = GenerateFen(boardState);
                        FENList.Add(fen); // Add the FEN string to the list
                        UpdateStatus(fen);
                    }

                    selectedPieceObject.HasMoved = true;

                    // Add to the score sheet for White
                    RecordMove((turnNum + 1) / 2, moveNotation, string.Empty);
                    SwitchTurn(); // Switch turn after each valid move
                }
                else if (selectedPieceObject.Color == "Black" && turnNum % 2 == 0)
                {
                    if (selectedPieceObject is King king && Math.Abs(col - selectedCol) == 2)
                    {
                        PerformCastling(king, selectedRow, selectedCol, row, col);
                        moveNotation = "O-O" + (col > selectedCol ? "" : "O-O-O"); // King-side or Queen-side castling notation
                        string fen = GenerateFen(boardState);
                        FENList.Add(fen); // Add the FEN string to the list
                        UpdateStatus(fen);
                    }
                    else if (selectedPieceObject is Pawn pawn && Math.Abs(selectedCol - col) == 1 && boardState[row, col] == null)
                    {
                        // Handle en passant capture for Black
                        int capturedRow = row - 1; // En passant target is one row above the destination
                        boardState[capturedRow, col] = null; // Remove the captured pawn
                        moveNotation += " e.p."; // En passant notation
                        MovePiece(selectedRow, selectedCol, row, col);
                        string fen = GenerateFen(boardState);
                        FENList.Add(fen); // Add the FEN string to the list
                        UpdateStatus(fen);
                    }
                    else
                    {
                        MovePiece(selectedRow, selectedCol, row, col);
                        string fen = GenerateFen(boardState);
                        FENList.Add(fen); // Add the FEN string to the list
                        UpdateStatus(fen);
                    }

                    selectedPieceObject.HasMoved = true;

                    // Add to the score sheet for Black
                    var lastMove = MoveHistory.Last();
                    lastMove.BlackMove = moveNotation;
                    ScoreSheetListBox.Items.Refresh(); // Refresh the UI
                    SwitchTurn(); // Switch turn after each valid move

                }
                else
                {
                    return; // Invalid turn for the piece color
                }
            }
            else
            {
                // Handle invalid move by selecting a new piece if one exists
                ChessPiece clickedPiece = boardState[row, col];
                if (clickedPiece != null)
                {
                    // Check if the clicked piece belongs to the current turn
                    if ((clickedPiece.Color == "White" && turnNum % 2 != 0) ||
                        (clickedPiece.Color == "Black" && turnNum % 2 == 0))
                    {
                        selectedPiece = (row, col); // Update selected piece
                        ClearHighlights();
                        HighlightValidMoves(row, col); // Highlight valid moves for the newly selected piece
                        return; // Exit after selecting the new piece
                    }
                    else
                    {
                        selectedPiece = null; // Deselect if the piece doesn't match the turn
                        ClearHighlights(); // Clear highlights
                        return;
                    }
                }
                else
                {
                    selectedPiece = null; // Deselect if no piece is clicked
                    ClearHighlights(); // Clear highlights
                    return;
                }
            }

            ClearHighlights(); // Clear highlights after completing the move
            selectedPiece = null; // Deselect after the move
        }
        // Helper to generate move notation
        private string GetMoveNotation(ChessPiece piece, int startRow, int startCol, int endRow, int endCol, bool isCapture)
{
    // Get the piece notation (e.g., "N" for Knight, empty for Pawn)
    string pieceName = piece is Pawn ? string.Empty : piece.GetType().Name[0].ToString();
    if (piece is Knight)
        pieceName = "N";

    // Determine the file and rank of the destination square
    string destination = $"{(char)('a' + endCol)}{8 - endRow}";

    // Handle captures
    if (isCapture)
    {
        if (piece is Pawn)
        {
            // For pawns, include the starting file
            return $"{(char)('a' + startCol)}x{destination}";
        }
        else
        {
            // For other pieces, include the "x" and destination
            return $"{pieceName}x{destination}";
        }
    }
    else
    {
        // Regular move (no capture)
        return $"{pieceName}{destination}";
    }
}
        // Helper method to determine disambiguation if needed
        private string GetDisambiguation(ChessPiece piece, int startRow, int startCol, int endRow, int endCol)
        {
            if (piece is Pawn)
                return string.Empty; // Pawns don't require disambiguation

            bool sameFile = false, sameRank = false, samePieceConflict = false;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (r == startRow && c == startCol)
                        continue;

                    ChessPiece otherPiece = boardState[r, c];
                    if (otherPiece?.GetType() == piece.GetType() && otherPiece?.Color == piece.Color)
                    {
                        if (otherPiece.IsValidMove(r, c, endRow, endCol, boardState))
                        {
                            samePieceConflict = true;
                            if (r == startRow)
                                sameRank = true;
                            if (c == startCol)
                                sameFile = true;
                        }
                    }
                }
            }

            if (!samePieceConflict)
                return string.Empty; // No disambiguation needed

            if (sameRank && sameFile)
                return $"{(char)('a' + startCol)}{8 - startRow}"; // Use both rank and file
            else if (sameFile)
                return $"{8 - startRow}"; // Use rank only
            else
                return $"{(char)('a' + startCol)}"; // Use file only
        }
        // Example placeholders for IsCheck and IsCheckmate
        private bool IsCheck()
        {
            // Logic to determine if the move puts the opponent's king in check
            return false;
        }

        private bool IsCheckmate()
        {
            // Logic to determine if the move checkmates the opponent's king
            return false;
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                // Move to the previous FEN if possible
                if (currentIndex > 0)
                {
                    currentIndex--;
                    UpdateBoardFromCurrentFEN();
                }
            }
            else if (e.Key == Key.Right)
            {
                // Move to the next FEN if possible
                if (currentIndex < FENList.Count - 1)
                {
                    currentIndex++;
                    UpdateBoardFromCurrentFEN();
                }
            }
        }
        private void UpdateBoardFromCurrentFEN()
        {
            // Ensure that the currentIndex is valid
            if (currentIndex >= 0 && currentIndex < FENList.Count)
            {
                // Get the current FEN string
                string currentFEN = FENList[currentIndex];
                CreateBoardFromFen(currentFEN); // Call the method to create the board
                DrawChessBoard();
                DrawPieces();
            }
        }
        private void CreateBoardFromFen(string fen)
        {
            // Split the FEN string to get the board state
            string[] parts = fen.Split(' ');
            string boardPart = parts[0]; // The first part contains the board layout

            // Initialize the board (assuming an 8x8 board)
            ChessPiece[,] boardState = new ChessPiece[8, 8];

            // Split the board part into ranks
            string[] ranks = boardPart.Split('/');

            for (int rank = 0; rank < 8; rank++)
            {
                string currentRank = ranks[rank];
                int file = 0; // File index

                foreach (char c in currentRank)
                {
                    if (char.IsDigit(c))
                    {
                        // If the character is a digit, it represents empty squares
                        int emptySquares = (int)char.GetNumericValue(c);
                        file += emptySquares; // Move the file index forward
                    }
                    else
                    {
                        // Use CreatePieceFromChar to get the piece information
                        List<string> pieceInfo = CreatePieceFromChar(c);
                        if (pieceInfo != null)
                        {
                            // Create the appropriate ChessPiece based on the color and type
                            string color = pieceInfo[0];
                            string pieceType = pieceInfo[1];

                            ChessPiece piece = pieceType switch
                            {
                                "Rook" => new Rook(color),
                                "Knight" => new Knight(color),
                                "Bishop" => new Bishop(color),
                                "Queen" => new Queen(color),
                                "King" => new King(color),
                                "Pawn" => new Pawn(color),
                            };

                            boardState[rank, file] = piece; // Place the piece on the board
                            file++; // Move to the next file
                        }
                        else
                        {
                            boardState[rank, file] = null;
                        }
                    }
                }
            }

            // Assign the board state to your class variable or property
            this.boardState = boardState; // Assuming you have a class-level variable for the board
        }

        private List<string> CreatePieceFromChar(char pieceChar)
        {
            List<string> values = new List<string>();
            // Dictionary to map piece characters to their properties
            var pieceMap = new Dictionary<char, (string Color, string PieceType)>
            {
                { 'r', ("Black", "Rook") },
                { 'n', ("Black", "Knight") },
                { 'b', ("Black", "Bishop") },
                { 'q', ("Black", "Queen") },
                { 'k', ("Black", "King") },
                { 'p', ("Black", "Pawn") },
                { 'R', ("White", "Rook") },
                { 'N', ("White", "Knight") },
                { 'B', ("White", "Bishop") },
                { 'Q', ("White", "Queen") },
                { 'K', ("White", "King") },
                { 'P', ("White", "Pawn") }
            };

            // Check if the character is in the dictionary
            if (pieceMap.TryGetValue(pieceChar, out var pieceInfo))
            {
                // Add the color and piece type to the values list
                values.Add(pieceInfo.Color);
                values.Add(pieceInfo.PieceType);
                return values; // Return the list of values
            }

            return null; // Unknown piece
        }
        private string GenerateFen(ChessPiece[,] boardState)
        {
            string fen = "";
            int boardSize = boardState.GetLength(0); // Assuming a square board

            for (int rank = 0; rank < boardSize; rank++)
            {
                int emptyCount = 0;
                for (int file = 0; file < boardSize; file++)
                {
                    ChessPiece piece = boardState[rank, file];
                    if (piece == null)
                    {
                        emptyCount++;
                    }
                    else
                    {
                        if (emptyCount > 0)
                        {
                            fen += emptyCount.ToString(); // Add empty squares count
                            emptyCount = 0;
                        }

                        // Declare pieceChar outside the if-else block
                        char pieceChar;

                        // Append the piece character based on color and type
                        if (piece.PieceType == "Knight")
                        {
                            pieceChar = 'N'; // Use 'N' for Knight
                        }
                        else
                        {
                            pieceChar = piece.PieceType[0]; // Assuming PieceType is a string like "Pawn", "Bishop", etc.
                        }

                        // Append the character to the FEN string based on color
                        if (piece.Color == "White")
                        {
                            fen += char.ToUpper(pieceChar); // Uppercase for white pieces
                        }
                        else
                        {
                            fen += char.ToLower(pieceChar); // Lowercase for black pieces
                        }
                    }
                }
                if (emptyCount > 0)
                {
                    fen += emptyCount.ToString(); // Add remaining empty squares count
                }
                if (rank < boardSize - 1)
                {
                    fen += "/"; // Separate ranks
                }
            }

            // Add additional FEN components (active color, castling rights, en passant, etc.)
            fen += " w KQkq - 0 1"; // Example: assuming white to move, all castling rights, no en passant, halfmove clock 0, fullmove number 1

            return fen;
        }
        private void HighlightValidMoves(int row, int col)
        {
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    if (boardState[row, col]?.IsValidMove(row, col, r, c, boardState) == true)
                    {
                        var highlight = new Ellipse
                        {
                            Width = SquareSize / 3, // Adjust size to your preference
                            Height = SquareSize / 3,
                            Opacity = 0.4
                        };

                        // Check if the square contains an opponent's piece
                        ChessPiece targetPiece = boardState[r, c];
                        if (targetPiece != null && targetPiece.Color != boardState[row, col]?.Color)
                        {
                            // Red circle for opponent's piece
                            highlight.Fill = Brushes.Red;
                        }
                        else
                        {
                            // Black circle for valid move
                            highlight.Fill = Brushes.Black;
                        }

                        // Position the circle in the center of the square
                        Canvas.SetLeft(highlight, c * SquareSize + (SquareSize - highlight.Width) / 2);
                        Canvas.SetTop(highlight, r * SquareSize + (SquareSize - highlight.Height) / 2);
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
        }
        private void PromotePawn(Pawn pawn, int toRow, int toCol)
        {
            // Check for pawn promotion: White pawn reaches the 0th row, Black pawn reaches the 7th row
            if (pawn.Color == "White" && toRow == 0)
            {
                // Promote to Queen (you can customize to allow other pieces)
                ChessPiece promotedQueen = new Queen("White");
                AddPiece(promotedQueen, toRow, toCol); // Add the new Queen to the board
            }
            else if (pawn.Color == "Black" && toRow == 7)
            {
                // Promote to Queen (you can customize to allow other pieces)
                ChessPiece promotedQueen = new Queen("Black");
                AddPiece(promotedQueen, toRow, toCol); // Add the new Queen to the board
            }
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
                // Regular move for white pawn (one step forward)
                if (fromRow - 1 == toRow && fromCol == toCol && boardState[toRow, toCol] == null)
                    return true;

                // Regular diagonal capture
                if (fromRow - 1 == toRow && Math.Abs(fromCol - toCol) == 1 && boardState[toRow, toCol]?.Color == "Black")
                    return true;

                // White pawn initial double move (only from row 6 to 4)
                if (fromRow == 6 && fromRow - 2 == toRow && fromCol == toCol && boardState[toRow, toCol] == null)
                {
                    // Record the move
                    MoveHistoryManager.MoveHistory.Add(new Move(this, fromRow, fromCol, toRow, toCol, "White"));
                    return true;
                }
            }
            else if (Color == "Black")
            {
                // Regular move for black pawn (one step forward)
                if (fromRow + 1 == toRow && fromCol == toCol && boardState[toRow, toCol] == null)
                    return true;

                // Regular diagonal capture
                if (fromRow + 1 == toRow && Math.Abs(fromCol - toCol) == 1 && boardState[toRow, toCol]?.Color == "White")
                    return true;

                // Black pawn initial double move (only from row 1 to 3)
                if (fromRow == 1 && fromRow + 2 == toRow && fromCol == toCol && boardState[toRow, toCol] == null)
                {
                    // Record the move
                    MoveHistoryManager.MoveHistory.Add(new Move(this, fromRow, fromCol, toRow, toCol, "Black"));
                    return true;
                }
            }

            // Check for en passant
            return CanCaptureEnPassant(fromRow, fromCol, toRow, toCol, boardState);
        }

        private bool CanCaptureEnPassant(int fromRow, int fromCol, int toRow, int toCol, ChessPiece[,] boardState)
        {
            if (MoveHistoryManager.MoveHistory.Count == 0)
                return false;

            // Get the last move from the history
            var lastMove = MoveHistoryManager.MoveHistory.Last();

            // En Passant is possible if the last move was a 2-square move by an opponent's pawn
            if (Math.Abs(lastMove.ToRow - lastMove.FromRow) == 2 &&
                lastMove.Piece is Pawn &&
                lastMove.PlayerColor != this.Color && // Check opponent's move
                lastMove.ToRow == fromRow && // Last move ended at the current pawn's row
                Math.Abs(lastMove.ToCol - fromCol) == 1) // Last move was adjacent to the current pawn
            {
                // Check if the current move is an en passant move
                if ((this.Color == "Black" && toRow == fromRow + 1 && toCol == lastMove.FromCol) ||
                    (this.Color == "White" && toRow == fromRow - 1 && toCol == lastMove.FromCol))
                {
                    // Do not modify the board here; just return true to indicate a valid en passant move
                    return true;
                }
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
            // Normal king move: 1 square in any direction
            if (Math.Abs(fromRow - toRow) <= 1 && Math.Abs(fromCol - toCol) <= 1)
            {
                // Ensure the destination is either empty or occupied by an opponent's piece
                return boardState[toRow, toCol] == null || boardState[toRow, toCol].Color != Color;
            }

            // Castling move
            if (!HasMoved && fromRow == toRow) // King hasn't moved and stays in the same row
            {
                // Check if castling is to the right (kingside)
                if (toCol == fromCol + 2)
                {
                    return CanCastle(fromRow, fromCol, fromCol + 3, boardState);
                }
                // Check if castling is to the left (queenside)
                else if (toCol == fromCol - 2)
                {
                    return CanCastle(fromRow, fromCol, fromCol - 4, boardState);
                }
            }

            return false;
        }

        private bool CanCastle(int kingRow, int kingCol, int rookCol, ChessPiece[,] boardState)
        {
            // Ensure the rook is in the correct position and hasn't moved
            if (boardState[kingRow, rookCol] is not Rook rook || rook.HasMoved || rook.Color != Color)
            {
                return false;
            }

            // Ensure all squares between the king and the rook are empty
            int step = rookCol > kingCol ? 1 : -1;
            for (int col = kingCol + step; col != rookCol; col += step)
            {
                if (boardState[kingRow, col] != null)
                {
                    return false;
                }
            }

            // TODO: Add checks for the king not being in check, passing through check, or ending in check
            
            return true;
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
    public class Move
    {
        public ChessPiece Piece { get; set; }
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
        public string PlayerColor { get; set; }

        public Move(ChessPiece piece, int fromRow, int fromCol, int toRow, int toCol, string playerColor)
        {
            Piece = piece;
            FromRow = fromRow;
            FromCol = fromCol;
            ToRow = toRow;
            ToCol = toCol;
            PlayerColor = playerColor;
        }
    }
    public static class MoveHistoryManager
    {
        // This will hold all the moves made in the game
        public static List<Move> MoveHistory = new List<Move>();
    }
    public class ChessMove
    {
        public int TurnNumber { get; set; }
        public string WhiteMove { get; set; }
        public string BlackMove { get; set; }
    }

}
