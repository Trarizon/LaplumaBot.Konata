using System.Drawing;

namespace Tra.Lapluma.Core.Tasks.UserTasks.Models.Utils.Deemo;
internal class SimpleNote
{
	public static SimpleNote Empty { get; } = new(0, 0, 0, NoteType.Piano);

	public int Time { get; }
	public float Position { get; }
	public float Size { get; }
	public NoteType NoteType { get; }

	public Image Image => NoteType.Image();

	public SimpleNote(int time, float position, float size, NoteType noteType)
	{
		Time = time;
		Position = position;
		Size = size;
		NoteType = noteType;
	}
}
