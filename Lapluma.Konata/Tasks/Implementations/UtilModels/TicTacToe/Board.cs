using Lapluma.Konata.Tasks.Interfaces;
using Lapluma.Konata.Utilities;
using System;
using System.Drawing;
using static Lapluma.Konata.Tasks.Implementations.UtilModels.TicTacToe.Piece;
using static Lapluma.Konata.Tasks.Implementations.UtilModels.TicTacToe.StateType;

namespace Lapluma.Konata.Tasks.Implementations.UtilModels.TicTacToe;
internal class Board : IDrawable
{
	#region Static Assets
	private const int BOARD_EDGE = 50;
	private const int GRID_SIZE = 75;
	private const int CHECKMATE_BASE_COORD = BOARD_EDGE + GRID_SIZE / 2 - 6;
	private const string ASSETS_DIR = $@"{Lapluma.ASSETS_DIR}TicTacToe\";
	private const string BOARD_FILE = $"{ASSETS_DIR}Board.png";

	private readonly static Bitmap s_pieceOBmp = new($"{ASSETS_DIR}PieceO.png");
	private readonly static Bitmap s_pieceXBmp = new(System.IO.File.OpenRead($"{ASSETS_DIR}PieceX.png"));// new($"{ASSETS_DIR}PieceX.png");
	private readonly static Bitmap s_checkmateHorBmp = new($"{ASSETS_DIR}CheckmateHor.png");
	private readonly static Bitmap s_checkmateVerBmp = new($"{ASSETS_DIR}CheckmateVer.png");
	private readonly static Bitmap s_checkmateSlashBmp = new($"{ASSETS_DIR}CheckmateSlash.png");
	private readonly static Bitmap s_checkmateBackSlashBmp = new($"{ASSETS_DIR}CheckmateBackSlash.png");
	#endregion

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
				else
					return;

				static bool Equal(Piece a, Piece b, Piece c) => a == b && b == c;
			}
			// Draw piece
			{
				var paintBmp = value switch
				{
					O => s_pieceOBmp,
					X => s_pieceXBmp,
					_ => throw new InvalidOperationException()
				};
				_graphics.DrawImage(paintBmp,
					BOARD_EDGE + row * GRID_SIZE,
					BOARD_EDGE + col * GRID_SIZE);
			}
			// Draw Checkmate
			{
				switch (Checkmate.Absolute()) {
					case Slash:
						_graphics.DrawImage(s_checkmateSlashBmp, BOARD_EDGE, BOARD_EDGE);
						break;
					case BackSlash:
						_graphics.DrawImage(s_checkmateBackSlashBmp, BOARD_EDGE, BOARD_EDGE);
						break;
					case Horizontal:
						_graphics.DrawImage(s_checkmateHorBmp, BOARD_EDGE, CHECKMATE_BASE_COORD + GRID_SIZE * Checkmate.GetLine());
						break;
					case Vertical:
						_graphics.DrawImage(s_checkmateVerBmp, CHECKMATE_BASE_COORD + GRID_SIZE * Checkmate.GetLine(), BOARD_EDGE);
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
