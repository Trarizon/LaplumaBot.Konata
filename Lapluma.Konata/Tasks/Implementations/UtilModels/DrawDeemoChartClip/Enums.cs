using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Lapluma.Konata.Tasks.Implementations.UtilModels.DrawDeemoChartClip;
internal enum NoteType
{
	Piano,
	NoSound,
	Slide
}

internal static class EnumExtensions
{
	public static Image Image(this NoteType noteType)
		=> noteType switch
		{
			NoteType.Piano => Assets.Deemo.PianoNoteImage,
			NoteType.NoSound => Assets.Deemo.NoSoundNoteImage,
			NoteType.Slide => Assets.Deemo.SlideImage,
			_ => throw new InvalidOperationException()
		};
}