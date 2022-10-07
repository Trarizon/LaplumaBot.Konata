using System;
using System.Drawing;

namespace Tra.Lapluma.Core.Tasks.UserTasks.Models.Utils.Deemo;
internal enum NoteType
{
	Piano,
	NoSound,
	Slide,
}

internal static class EnumExt
{
	public static Image Image(this NoteType note)
		=> note switch
		{
			NoteType.Piano => LocalAssets.Deemo.PianoNoteImage,
			NoteType.NoSound => LocalAssets.Deemo.NoSoundNoteImage,
			NoteType.Slide => LocalAssets.Deemo.SlideNoteImage,
			_ => throw new InvalidOperationException()
		};
}
