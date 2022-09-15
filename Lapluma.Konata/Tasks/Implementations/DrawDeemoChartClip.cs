using Konata.Core.Events.Model;
using Konata.Core.Message;
using Lapluma.Konata.Utilities;
using Lapluma.Konata.Utilities.Structrues;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Lapluma.Konata.Tasks.Implementations.UtilModels.DrawDeemoChartClip;
using System.Threading.Tasks;
using Lapluma.Konata.Exceptions;
using System.Drawing;
using System.Linq;

namespace Lapluma.Konata.Tasks.Implementations;
internal sealed class DrawDeemoChartClip : BaseTask
{
	public DrawDeemoChartClip() : base(
		name: nameof(DrawDeemoChartClip),
		summary: "decht {#speed} {#chart} => 绘制Deemo谱面",
		help: "decht {#speed} {#chart} => 绘制Deemo谱面\n" +
		"# speed => 相邻note间隔\n" +
		"# chart => 描述谱面",
		cmdRgx: "decht",
		friendCfg: (true, FriendRange.All),
		groupCfg: (true, GroupRange.All, OperatorRange.Admin))
	{ }

	protected override Task<bool> ExecuteAsync(FriendMessageEvent e)
		=> GeneralExecute(e.Chain.ToString(), MessageHandlers.SendFriendAsync(e));

	protected override Task<bool> ExecuteAsync(GroupMessageEvent e)
		=> GeneralExecute(e.Chain.ToString(), MessageHandlers.SendGroupAsync(e));

	private static async Task<bool> GeneralExecute(string chainstr, MessageHandler<MessageBuilder> handler)
	{
		var match = Regex.Match(chainstr, @"^decht(?: {(.+)})? (.*)$");
		if (!match.Success) return false;
		#region Regex index
		const int SPEED = 1;
		const int CHART = SPEED + 1;
		#endregion

		double speed = 1.0;
		var speedstr = match.Groups[SPEED].Value;
		if (speedstr != "" && !double.TryParse(speedstr, out speed)) {
			await handler(Message.Text("你这速度写的什么东西"));
			return true;
		}

		IEnumerable<SimpleNote> chart;
		try {
			chart = ParseChartString(match.Groups[CHART].Value);
		} catch (ManualRegexMatchingException ex) {
			await handler(Message.Text(ex.Message));
			return true;
		}
		if (!chart.Any()) {
			await handler(Message.Text("空白的就不用我画了吧"));
			return true;
		}
		await handler(Message.Image(Draw(chart, speed).ToBytes()));
		return true;
	}

	/**Regular Expression
	 * Most complex note: 1.[2]n => {pos=1.5,size=2,type=nosound}
	 * Multinote: (02[2]1.) => {pos=0}{pos=2,size=2}{pos=1.5}
	 * 
	 * posticeFloat:pf: \d+(.\d+)?
	 * note:nt: -?\d.?(\[\pf\])?(n|s|p)?
	 * multiNote:mnt: \(\nt*\)
	 * chart:cht: (\mnt|\nt)*
	 */

	private static IEnumerable<SimpleNote> ParseChartString(string chartstr)
	{
		int index = 0;
		int noteTime = 0;
		return Chart();

		// (\mnt|\nt)*
		IEnumerable<SimpleNote> Chart()
		{
			List<SimpleNote> notes = new ();

			while (index < chartstr.Length) {
				var first = CurrentChar();
				// MultiNote
				if (first == '(')
					foreach (var note in MultiNote()) notes.Add(note);
				// Note
				else
					notes.Add(Note());
				noteTime++;
			}

			return notes;
		}

		// \(\nt*\)
		IEnumerable<SimpleNote> MultiNote()
		{
			List<SimpleNote> notes = new();

			index++;
			while (CurrentChar() != ')')
				notes.Add(Note());
			index++;

			return notes;
		}

		// -?\d.?(\[\pf\])?(s|n|p)?
		SimpleNote Note()
		{
			int flag;
			float absPos;
			float size;
			NoteType noteType;

			if (CurrentChar() == '-') {
				flag = -1;
				index++;
			}
			else flag = 1;

			if (char.IsDigit(CurrentChar())) {
				absPos = CurrentChar() - '0';
				if (absPos > 2) throw new ManualRegexMatchingException($"Note position {CurrentChar()} out of bound.", index, chartstr.Substring(index, 5));
				index++;
			}
			else throw new ManualRegexMatchingException($"Undefined char '{CurrentChar()}'.", index, chartstr.Substring(index, 5));

			if (CurrentChar() == '.') {
				absPos += 0.5f;
				if (absPos == 2) throw new ManualRegexMatchingException("Note position 2.5 out of bound.", index - 1, chartstr.Substring(index - 1, 5));
				index++;
			}

			if (CurrentChar() == '[') {
				int start = index + 1;
				int end;
				for (end = start; end < chartstr.Length; end++) {
					if (chartstr[end] == ']') break;
				}
				if (end >= chartstr.Length) throw new ManualRegexMatchingException("Unclosed bracket.", index, chartstr.Substring(index, 5));

				string ipt = chartstr[start..end];
				if (ipt == "") size = 1;
				else if (!float.TryParse(ipt, out size)) throw new ManualRegexMatchingException($"Invalid note size {ipt}.", start, chartstr.Substring(index, 5));
				index = end + 1;
			}
			else size = 1;

			switch (char.ToLower(CurrentChar())) {
				case 'n':
					noteType = NoteType.NoSound;
					index++;
					break;
				case 's':
					noteType = NoteType.Slide;
					index++;
					break;
				case 'p':
					noteType = NoteType.Piano;
					index++;
					break;
				default:
					noteType = NoteType.Piano;
					break;
			}

			return new SimpleNote(noteTime, flag * absPos, size, noteType);
		}

		char CurrentChar() => index < chartstr.Length ? chartstr[index] : (char)0;
	}

	private static Bitmap Draw(IEnumerable<SimpleNote> chart, double speed)
	{
		const int TOP_EMPTY = 20;
		double spacing = speed * 20;
		var height = chart.Last().Time *  spacing + TOP_EMPTY * 2;
		const int width = 560;

		var pic = new Bitmap(width, (int)height);
		using var graphics = Graphics.FromImage(pic);
		graphics.Clear(Color.Black);

		foreach (var note in chart.Reverse()) {
			var noteImg = note.NoteType.Image();
			float w = noteImg.Width * note.Size;
			float h = noteImg.Height;
			graphics.DrawImage(noteImg,
				(float)(width / 2 + note.Position * 100 - w / 2),
				(float)(height - (TOP_EMPTY + note.Time * spacing) - h / 2),
				w, h);
		}

		return pic;
	}
}
