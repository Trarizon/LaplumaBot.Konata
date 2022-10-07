global using static Tra.Lapluma.Core.BotData;
using System;
using System.Collections.Generic;
using System.IO;
using Tra.Lapluma.Core.Tasks;
using Tra.Lapluma.Core.Utilities;

namespace Tra.Lapluma.Core;
internal static class BotData
{
	public static readonly Bot Lapluma = new();

	public const string ActPfx = "/";

	#region Path Constants
	public const string AssetsDir = @"Assets\";
	public const string ConfigDir = @"Config\";

	public const string TaskDir = ConfigDir + @"Task\";

	public const string UinAbbrsPath = ConfigDir + "UinAbbrs.lpm";
	public const string EventsPath = ConfigDir + "Events.lpm";

	public static string GetTaskDir(BaseTask task) => TaskDir + task.Name + '\\';
	#endregion

	public const uint DoctorUin = 2223998963;

	#region Group
	private static Dictionary<string, uint> _uinAbbrs = new();
	private static Dictionary<string, uint> UinAbbrs => _uinAbbrs;

	internal static void LoadUinAbbrsValue()
	{
		if (!File.Exists(UinAbbrsPath)) return;
		Dictionary<string, uint> dict = new();

		using var sr = new StreamReader(UinAbbrsPath);
		while (!sr.EndOfStream) {
			string? line = sr.ReadLine();
			if (line is null) {
				Util.Output("Read GroupAbbrs reach null");
				continue;
			}

			string[] pair = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			if (uint.TryParse(pair[1], out uint uin))
				dict.Add(pair[0], uin);
			else
				Util.Output($"Read GroupAbbrs reach invalid group uin: {pair[0]} {pair[1]}");
		}
		_uinAbbrs = dict;
	}

	internal static void SaveUinAbbrs()
	{
		Directory.CreateDirectory(ConfigDir);
		using var file = File.Create(UinAbbrsPath);
		using var sw = new StreamWriter(file);
		foreach (var abbr in UinAbbrs) {
			Console.WriteLine("write");
			sw.WriteLine($"{abbr.Key} {abbr.Value}");
		}
	}
	#endregion

	/// <summary>
	/// 
	/// </summary>
	/// <param name="uinOrAbbr"></param>
	/// <returns><see langword="0"/> if not found</returns>
	public static uint SearchUin(string uinOrAbbr)
	{
		if (uint.TryParse(uinOrAbbr, out uint group) ||
			UinAbbrs.TryGetValue(uinOrAbbr, out group))
			return group;
		else return 0;
	}

}
