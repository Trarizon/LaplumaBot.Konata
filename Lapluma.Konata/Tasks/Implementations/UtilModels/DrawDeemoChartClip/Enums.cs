using System;
using System.Drawing;

namespace Lapluma.Konata.Tasks.Implementations.UtilModels.DrawDeemoChartClip;
internal enum NoteType
{
	Piano,
	NoSound,
	Slide,
	Empty
}

internal static class EnumExtensions
{
	public static Image? Image(this NoteType noteType)
		=> noteType switch
		{
			NoteType.Piano => Assets.Deemo.PianoNoteImage,
			NoteType.NoSound => Assets.Deemo.NoSoundNoteImage,
			NoteType.Slide => Assets.Deemo.SlideImage,
			NoteType.Empty => null,
			_ => throw new InvalidOperationException()
		};
}