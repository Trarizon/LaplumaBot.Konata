namespace Lapluma.Konata.Tasks.Implementations.UtilModels.DrawDeemoChartClip;
internal class SimpleNote
{
	public static SimpleNote Empty { get; } = new(0, 0, 0, NoteType.Empty);

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
