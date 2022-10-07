using System.Drawing;

namespace Tra.Lapluma.Core.Tasks.UserTasks.Models.Utils;
internal static class LocalAssets
{
	public static class Deemo
	{
		const string DIR = AssetsDir + "Deemo\\";

		public static readonly Image PianoNoteImage = Image.FromFile(DIR + "BlackNote.png");
		public static readonly Image NoSoundNoteImage = Image.FromFile(DIR + "NoSoundNote.png");
		public static readonly Image SlideNoteImage = Image.FromFile(DIR + "SlideNote.png");
	}

	public static class TicTacToe
	{
		const string DIR = AssetsDir + "TicTacToe\\";

		public const int BoardEdgeBlankSize = 50;
		public const int GridSize = 75;
		public const int CheckmateBaseCoord = BoardEdgeBlankSize + GridSize / 2 - 6;

		public static readonly Image PieceOImage = Image.FromFile(DIR + "PieceO.png");
		public static readonly Image PieceXImage = Image.FromFile(DIR + "PieceX.png");
		public static readonly Image CheckmateHorImage = Image.FromFile(DIR + "CheckmateHor.png");
		public static readonly Image CheckmateVerImage = CloneRotate90Image(CheckmateHorImage);
		public static readonly Image CheckmateLtdnImage = Image.FromFile(DIR + "CheckmateLeftdown.png");
		public static readonly Image CheckmateRtdnImage = CloneRotate90Image(CheckmateLtdnImage);

		public static Image GetBoardImage() => Image.FromFile(DIR + "Board.png");

		private static Image CloneRotate90Image(Image origin)
		{
			var img = (Image)origin.Clone();
			img.RotateFlip(RotateFlipType.Rotate90FlipNone);
			return img;
		}
	}
}
