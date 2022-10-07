namespace Tra.Lapluma.Core.Tasks.UserTasks.Models.Utils.TicTacToe;
internal enum Piece
{
	/// <summary>
	/// Placeholder, not a piece
	/// </summary>
	N,
	X,
	O
}

internal enum CheckmateType
{
	Unfinished = 0,
	Draw = 0x10,
	/// <summary>
	/// /
	/// </summary>
	Leftdown = 0x20,
	/// <summary>
	/// \
	/// </summary>
	Rightdown = 0x30,
	/// <summary>
	/// Use <see cref="EnumExt.GetLine(CheckmateType)"/> to get column
	/// </summary>
	Horizontal = 0x40,
	/// <summary>
	/// Use <see cref="EnumExt.GetLine(CheckmateType)"/> to get row
	/// </summary>
	Vertical = 0x50,
}

internal static class EnumExt
{
	public static int GetLine(this CheckmateType mate) => (int)mate & 0xf;
	public static CheckmateType Absolute(this CheckmateType mate) => mate & (CheckmateType)0xf0;
	public static CheckmateType Combine(this CheckmateType mate, int line) => mate | (CheckmateType)line;
}
