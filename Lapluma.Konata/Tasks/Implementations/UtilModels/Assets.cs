using System.Drawing;

namespace Lapluma.Konata.Tasks.Implementations.UtilModels;
internal static class Assets
{
	public static class Deemo
	{
		const string DIR = Lapluma.ASSETS_DIR + "Deemo\\";

		public static readonly Image PianoNoteImage = Image.FromFile(DIR + "BlackNote.png");
		public static readonly Image NoSoundNoteImage = Image.FromFile(DIR + "NoSoundNote.png");
		public static readonly Image SlideImage = Image.FromFile(DIR + "SlideNote.png");
	}

	public static class TicTacToe
	{
		public const int BOARD_EDGE_BLANK = 50;
		public const int GRID_SIZE = 75;
		public const int CHECKMATE_BASE_COORD = BOARD_EDGE_BLANK + GRID_SIZE / 2 - 6;
		public const string DIR = Lapluma.ASSETS_DIR + "TicTacToe\\";

		public const string BOARD_FILE = DIR + "Board.png";

		public readonly static Bitmap PieceOImage = new($"{DIR}PieceO.png");
		public readonly static Bitmap PieceXImage = new(System.IO.File.OpenRead($"{DIR}PieceX.png"));// new($"{ASSETS_DIR}PieceX.png");
		public readonly static Bitmap CheckmateHorImage = new($"{DIR}CheckmateHor.png");
		public readonly static Bitmap CheckmateVerImage = new($"{DIR}CheckmateVer.png");
		public readonly static Bitmap CheckmateSlashImage = new($"{DIR}CheckmateSlash.png");
		public readonly static Bitmap CheckmateBackslashImage = new($"{DIR}CheckmateBackSlash.png");

	}
}
