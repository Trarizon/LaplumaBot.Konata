using System;
using System.Collections.Generic;
using System.Text;

namespace Lapluma.Konata.Tasks.Implementations.UtilModels.TicTacToe;
internal enum Piece
{
	/// <summary>
	/// Placeholder, not a piece
	/// </summary>
	N,
	X,
	O
}

internal enum StateType
{
	Unfinished = 0,
	Draw = 0x10,
	/// <summary>
	/// /
	/// </summary>
	Slash = 0x20,
	/// <summary>
	/// \
	/// </summary>
	BackSlash = 0x30,
	/// <summary>
	/// Use <see cref="EnumExtensions.GetLine(StateType)"/> to get column
	/// </summary>
	Horizontal = 0x40,
	/// <summary>
	/// Use <see cref="EnumExtensions.GetLine(StateType)"/> to get row
	/// </summary>
	Vertical = 0x50,
}

internal static class EnumExtensions
{
	public static int GetLine(this StateType state) => (int)state & 0xf;
	public static StateType Absolute(this StateType state) => state & (StateType)0xf0;
}