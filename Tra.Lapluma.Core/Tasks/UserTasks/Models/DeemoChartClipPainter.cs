using Konata.Core.Events.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Tra.Lapluma.Core.Exceptions;
using Tra.Lapluma.Core.Tasks.UserTasks.Models.Utils.Deemo;
using Tra.Lapluma.Core.Utilities;

namespace Tra.Lapluma.Core.Tasks.UserTasks.Models;
internal sealed class DeemoChartClipPainter : UserTask
{
	public DeemoChartClipPainter(Bot bot) : base(bot,
		actRgx: "decht",
		name: nameof(DeemoChartClipPainter),
		summary: "绘制de谱面",
		help: "decht [{<speed>}] <chart>\n" +
		"# speed => 相邻note间隔\n" +
		"# chart => 描述谱面\n" +
		"## Eg: 1.3[2]n = {pos=1.3, size=2, nosound}\n" +
		"## Eg: 1:[1.6]s = {pos=1.3, size=1.6, slide}\n" +
		"## Eg: (-112) = -1,1,2位置的多押" +
		"## pos只允许一位小数，空拍使用'_'",
		friendDefaultEnable: true,
		groupDefaultEnable: false)
	{ }

	protected override Task<bool> OnActivateAsync(FriendMessageEvent ev)
		=> ExecuteAsync(ev.Chain.ToString(), _bot.SenderToFriend(ev));

	protected override Task<bool> OnActivateAsync(GroupMessageEvent ev)
		=> ExecuteAsync(ev.Chain.ToString(), _bot.SenderToGroup(ev));

	private static async Task<bool> ExecuteAsync(string chainstr, Message.Sender sender)
	{
		var match = chainstr.MatchActRegex(@"decht(?: {(.*)})? (.*)");
		if (!match.Success) return false;
		const int SPEED = 1;
		const int CHART = 2;

		double speed = 1.0;
		var speedstr = match.Groups[SPEED].Value;
		if (speedstr != "" && !double.TryParse(speedstr, out speed)) {
			await sender(Message.Text("你这速度写的什么东西"));
			return true;
		}

		IEnumerable<SimpleNote> chart = ParseChartCode(match.Groups[CHART].Value);
		if (!chart.Any()) {
			await sender(Message.Text("白纸有这么好看吗"));
			return true;
		}
		using var chartimg = PaintImage(chart, speed);
		await sender(Message.Image(chartimg.ToBytes()));
		return true;
	}

	/**Regex
	 * pos:p:  -?\d(.\d|,)?
	 * note:nt: _|(<p>)(\[\f\])?(n|s|p)?
	 * notes:ns: \(<nt>*\)
	 * chart:cht: (<ns>|<nt>)
	 */

	private static IEnumerable<SimpleNote> ParseChartCode(string code)
	{
		int index = 0;
		int noteTime = 0;
		return Chart();

		// chart:cht: (<ns>|<nt>)
		IEnumerable<SimpleNote> Chart()
		{
			List<SimpleNote> chart = new();

			while (index < code.Length) {
				var first = Current();
				// Notes
				if (first == '(')
					chart.AddRange(Notes());
				// Note
				else
					chart.Add(Note());
				noteTime++;
			}

			return chart;
		}

		// \(<nt>*\)
		IEnumerable<SimpleNote> Notes()
		{
			List<SimpleNote> notes = new();

			do index++; // '(' first
			while (char.IsWhiteSpace(Current()));

			while (Current() != ')')
				notes.Add(Note());
			index++; //')'

			return notes;
		}

		// _|(<p>)(\[\f\])?(n|s|p)?
		SimpleNote Note()
		{
			while (char.IsWhiteSpace(Current())) index++;

			if (Current() == '_') {
				index++;
				return SimpleNote.Empty;
			}

			float pos = Pos();

			// Size
			float size;
			if (Current() == '[') {
				int start = index + 1;
				index = code.IndexOf(']', start);
				if (index == -1) throw new ManualRegexException("Unclosed bracket", index, Substr(index));

				string szstr = code[start..index];
				if (szstr == "") size = 1;
				else if (!float.TryParse(szstr, out size)) throw new ManualRegexException($"Invalid size {szstr}", start, Substr(start));
				index++; // ]
			}
			else size = 1;

			// Type
			NoteType type;
			switch (char.ToLower(Current())) {
				case 'n':
					type = NoteType.NoSound;
					index++;
					break;
				case 's':
					type = NoteType.Slide;
					index++;
					break;
				case 'p':
					type = NoteType.Piano;
					index++;
					break;
				default:
					type = NoteType.Piano;
					break;
			}

			return new SimpleNote(noteTime, pos, size, type);
		}

		float Pos()
		{
			int flag = 1;
			float abs = 0f;
			int ti = index;
			if (Current() == '-') {
				flag = -1;
				index++;
			}

			if (char.IsDigit(Current())) {
				abs = Current() - '0';
				index++;
			}
			else if (index >= code.Length) throw new ManualRegexException("Unexpected end of string.");
			else throw new ManualRegexException($"Undefined char '{Current()}'", index, Substr(index));

			if (Current() == '.') {
				index++; // .
				if (char.IsDigit(Current())) {
					abs += 0.1f * (Current() - '0');
					index++;
				}
				else if (index >= code.Length) throw new ManualRegexException("Unexpected end of string.");
				else throw new ManualRegexException($"Undefined char '{Current()}'", index, Substr(index));
			}
			else if (Current() == ',') {
				abs += 0.5f;
				index++;
			}

			if (abs > 2) throw new ManualRegexException($"Note position {abs * flag:F1} out of bound.", ti, Substr(ti));
			return abs * flag;
		}

		char Current() => index < code.Length ? code[index] : '\0';
		string Substr(int start)
		{
			if (start > code.Length) return "";
			if (start + 5 > code.Length) return code[start..];
			return code.Substring(start, 5);
		}
	}

	private static Image PaintImage(IEnumerable<SimpleNote> chart, double speed)
	{
		const int EDGE_EMPTY = 20;
		double spacing = speed * 24;
		const int width = 560;
		var height = (int)(chart.Last().Time * spacing + EDGE_EMPTY * 2);
		if (height > 65535) throw new TaskException("图片过大");

		var pic = new Bitmap(width, height);
		using var graphics = Graphics.FromImage(pic);
		graphics.Clear(Color.Black);

		foreach (var note in chart.Reverse()) {
			if (note == SimpleNote.Empty) continue; // Skip empty

			var img = note.Image;
			float w = img.Width * note.Size;
			float h = img.Height;
			graphics.DrawImage(img,
				(float)(width / 2 + note.Position * 100 - w / 2),
				(float)(height - (EDGE_EMPTY + note.Time * spacing) - h / 2),
				w, h);
		}

		return pic;
	}
}
