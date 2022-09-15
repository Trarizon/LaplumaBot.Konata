using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

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
}
