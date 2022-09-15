using System;
using System.Collections.Generic;
using System.Text;

namespace Lapluma.Konata.Tasks.Implementations.UtilModels.DrawDeemoChartClip;
internal class SimpleNote
{
	public int Time { get; }
	public float Position { get; }
	public float Size { get; }
	public NoteType NoteType { get; }

	public SimpleNote(int time, float position, float size, NoteType noteType)
	{
		Time = time;
		Position = position;
		Size = size;
		NoteType = noteType;
	}
}
