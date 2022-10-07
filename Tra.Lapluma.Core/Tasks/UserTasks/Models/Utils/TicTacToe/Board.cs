using System;
using System.Drawing;
using Tra.Lapluma.Core.Utilities;
using static Tra.Lapluma.Core.Tasks.UserTasks.Models.Utils.LocalAssets.TicTacToe;
using static Tra.Lapluma.Core.Tasks.UserTasks.Models.Utils.TicTacToe.CheckmateType;
using static Tra.Lapluma.Core.Tasks.UserTasks.Models.Utils.TicTacToe.Piece;

namespace Tra.Lapluma.Core.Tasks.UserTasks.Models.Utils.TicTacToe;
internal class Board : IDisposable
{
	private readonly Piece[,] _board = new[,]
	{
		{ N, N, N },
		{ N, N, N },
		{ N, N, N },
	};
	private readonly Image _image;
	private readonly Graphics _graphics;
	private int _nPlaced = 0;

	public byte[] Image => _image.ToBytes();
	public CheckmateType Checkmate { get; private set; }

	public Piece this[int col, int row] => _board[col, row];

	public Board()
	{
		_image = GetBoardImage();
		_graphics = Graphics.FromImage(_image);
	}

	public bool PlacePiece(int col, int row, Piece piece)
	{
		if (col is < 0 or > 2)
			throw new ArgumentOutOfRangeException(nameof(col), "Placing piece out of range");
		if (row is < 0 || row > 2)
			throw new ArgumentOutOfRangeException(nameof(row), "Placing piece out of range");
		if (piece != O && piece != X)
			throw new ArgumentOutOfRangeException(nameof(piece), "Placed unexcepted piece");
		if (_board[col, row] != N)
			return false;

		_board[col, row] = piece;
		_nPlaced++;

		// Check state
		{
			static bool Equal3(Piece a, Piece b, Piece c) => a == b && b == c;

			// Hor
			if (Equal3(_board[col, 0], _board[col, 1], _board[col, 2]))
				Checkmate = Horizontal.Combine(col);
			// Ver
			else if (Equal3(_board[0, row], _board[1, row], _board[2, row]))
				Checkmate = Vertical.Combine(row);
			// /
			else if (col + row == 2 && Equal3(_board[0, 2], _board[1, 1], _board[2, 0]))
				Checkmate = Leftdown;
			// \
			else if (col == row && Equal3(_board[0, 0], _board[1, 1], _board[2, 2]))
				Checkmate = Rightdown;
			// Draw
			else if (_nPlaced == 9)
				Checkmate = Draw;
		}

		// Draw piece
		{
			var paintImg = piece switch
			{
				O => PieceOImage,
				X => PieceXImage,
				_ => throw new InvalidOperationException()
			};
			_graphics.DrawImage(paintImg,
				BoardEdgeBlankSize + row * GridSize,
				BoardEdgeBlankSize + col * GridSize);
		}

		// Draw checkmate
		{
			switch (Checkmate.Absolute()) {
				case Leftdown:
					_graphics.DrawImage(CheckmateLtdnImage, BoardEdgeBlankSize, BoardEdgeBlankSize);
					break;
				case Rightdown:
					_graphics.DrawImage(CheckmateRtdnImage, BoardEdgeBlankSize, BoardEdgeBlankSize);
					break;
				case Horizontal:
					_graphics.DrawImage(CheckmateHorImage, BoardEdgeBlankSize, CheckmateBaseCoord + GridSize * Checkmate.GetLine());
					break;
				case Vertical:
					_graphics.DrawImage(CheckmateVerImage, CheckmateBaseCoord + GridSize * Checkmate.GetLine(), BoardEdgeBlankSize);
					break;
				default:
					break;
			}
		}

		return true;
	}

	public void Dispose()
	{
		_image.Dispose();
		_graphics.Dispose();
	}
}
