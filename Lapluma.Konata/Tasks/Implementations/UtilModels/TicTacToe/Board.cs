using Lapluma.Konata.Utilities;
using System;
using System.Drawing;
using static Lapluma.Konata.Tasks.Implementations.UtilModels.Assets.TicTacToe;
using static Lapluma.Konata.Tasks.Implementations.UtilModels.TicTacToe.Piece;
using static Lapluma.Konata.Tasks.Implementations.UtilModels.TicTacToe.StateType;

namespace Lapluma.Konata.Tasks.Implementations.UtilModels.TicTacToe;
internal class Board
{
	private readonly Piece[,] _board = new[,]
	{
		{ N, N, N },
		{ N, N, N },
		{ N, N, N },
	};
	private readonly Bitmap _image;
	private readonly Graphics _graphics;
	private int _nPlaced = 0;

	public byte[] Image => _image.ToBytes();
	public StateType Checkmate { get; private set; }

	public Piece this[int col, int row]
	{
		get => _board[col, row];
		set {
			if (col is < 0 or > 2)
				throw new ArgumentOutOfRangeException(nameof(col), "Placing piece out of range");
			if (row is < 0 or > 2)
				throw new ArgumentOutOfRangeException(nameof(row), "Placing piece out of range");
			if (value == N)
				throw new ArgumentException($"Piece cannot be {N}", nameof(value));
			if (_board[col, row] != N)
				throw new ArgumentException("This place has been placed");

			_board[col, row] = value;
			_nPlaced++;

			// Check State
			{
				if (_board[col, row] == Piece.N) return;

				// Draw
				if (_nPlaced == 9)
					Checkmate = Draw;
				// Hor
				else if (Equal(_board[col, 0], _board[col, 1], _board[col, 2]))
					Checkmate = Horizontal | (StateType)col;
				// Ver
				else if (Equal(_board[0, row], _board[1, row], _board[2, row]))
					Checkmate = Vertical | (StateType)row;
				// Slash
				else if (col + row == 2 && Equal(_board[0, 2], _board[1, 1], _board[2, 0]))
					Checkmate = Slash;
				// BackSlash
				else if (col == row && Equal(_board[0, 0], _board[1, 1], _board[2, 2]))
					Checkmate = BackSlash;

				static bool Equal(Piece a, Piece b, Piece c) => a == b && b == c;
			}
			// Draw piece
			{
				var paintBmp = value switch
				{
					O => PieceOImage,
					X => PieceXImage,
					_ => throw new InvalidOperationException()
				};
				_graphics.DrawImage(paintBmp,
					BOARD_EDGE_BLANK + row * GRID_SIZE,
					BOARD_EDGE_BLANK + col * GRID_SIZE);
			}
			// Draw Checkmate
			{
				switch (Checkmate.Absolute()) {
					case Slash:
						_graphics.DrawImage(CheckmateSlashImage, BOARD_EDGE_BLANK, BOARD_EDGE_BLANK);
						break;
					case BackSlash:
						_graphics.DrawImage(CheckmateBackslashImage, BOARD_EDGE_BLANK, BOARD_EDGE_BLANK);
						break;
					case Horizontal:
						_graphics.DrawImage(CheckmateHorImage, BOARD_EDGE_BLANK, CHECKMATE_BASE_COORD + GRID_SIZE * Checkmate.GetLine());
						break;
					case Vertical:
						_graphics.DrawImage(CheckmateVerImage, CHECKMATE_BASE_COORD + GRID_SIZE * Checkmate.GetLine(), BOARD_EDGE_BLANK);
						break;
					default:
						break;
				}
			}
		}
	}

	public Board()
	{
		_image = new(BOARD_FILE);
		_graphics = Graphics.FromImage(_image);
	}
}
