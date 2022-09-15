using Konata.Core;
using Konata.Core.Common;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Lapluma.Konata.Exceptions;
using Lapluma.Konata.Tasks;
using Lapluma.Konata.Tasks.Implementations;
using Lapluma.Konata.Utilities;
using Lapluma.Konata.Utilities.Structrues;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lapluma.Konata;
public static class Lapluma
{
	internal const string ASSETS_DIR = @"Assets\";
	internal const string CONFIG_DIR = @"Config\";

	#region Bot info
	private static BotConfig Config { get; } = new()
	{
		EnableAudio = true,
		TryReconnect = true,
		HighwayChunkSize = 8192
	};

	private static BotDevice Device
	{
		get {
			const string DEVICE_LOG = CONFIG_DIR + @"BotInfo\Device.json";

			if (File.Exists(DEVICE_LOG))
				return JsonSerializer.Deserialize<BotDevice>(File.ReadAllText(DEVICE_LOG));

			BotDevice device = BotDevice.Default();
			string ser = JsonSerializer.Serialize(device);
			Directory.CreateDirectory(CONFIG_DIR + "BotInfo");
			File.Create(DEVICE_LOG).Close();
			File.WriteAllText(DEVICE_LOG, ser);
			return device;
		}
	}

	private static BotKeyStore KeyStore
	{
		get {
			const string KEY_STORE_LOG = CONFIG_DIR + @"BotInfo\KeyStore.json";

			if (File.Exists(KEY_STORE_LOG))
				return JsonSerializer.Deserialize<BotKeyStore>(File.ReadAllText(KEY_STORE_LOG));

			Console.WriteLine("Enter ID:");
			string id = Console.ReadLine();
			Console.WriteLine("Enter password");
			string pw = Console.ReadLine();

			BotKeyStore keyStore = new(id, pw);
			string ser = JsonSerializer.Serialize(keyStore);
			Directory.CreateDirectory(CONFIG_DIR + "BotInfo");
			File.Create(KEY_STORE_LOG).Close();
			File.WriteAllText(KEY_STORE_LOG, ser);
			Console.Clear();
			return keyStore;
		}
	}
	#endregion

	internal const string CmdPfx = "";
	public const uint Doctor = 2223998963;


	static Lapluma()
	{
		//LoadBotEvents();

		//static void LoadBotEvents()
		{
			Bot.OnLog += (_, e) => Console.WriteLine(e.EventMessage);
			Bot.OnCaptcha += (bot, e) =>
			{
				switch (e.Type) {
					case CaptchaEvent.CaptchaType.Sms:
						Console.WriteLine($"A SMS has been sent to {e.Phone}.\nCode: ");
						if (!bot.SubmitSmsCode(Console.ReadLine()))
							throw new BotInitializationFailException("SMS captcha failed.");
						break;
					case CaptchaEvent.CaptchaType.Slider:
						Console.WriteLine($"Slider captcha required. Url:\n{e.SliderUrl}");
						if (!bot.SubmitSliderTicket(Console.ReadLine()))
							throw new BotInitializationFailException("Slide captcha failed.");
						break;
					default:
						throw new BotInitializationFailException("Unknown captcha type.");
				}
			};

			Bot.OnFriendMessage += EventInvokeAction.OnFriendMessageInvokeAsync;
			Bot.OnGroupMessage += EventInvokeAction.OnGroupMessageInvokeAsync;
			Bot.OnBotOnline += EventInvokeAction.OnBotOnlineInvokeAsync;
			Bot.OnBotOffline += EventInvokeAction.OnBotOfflineInvokeAsync;
			Bot.OnFriendPoke += EventInvokeAction.OnFriendPokeInvokeAsync;
			Bot.OnGroupPoke += EventInvokeAction.OnGroupPokeInvokeAsync;
		}
	}

	private static Dictionary<string, uint> _uidLabels = new();

	public static Bot Bot { get; } = BotFather.Create(Config, Device, KeyStore);

	public static uint Uin => Bot.Uin;
	public static Uid Uid { get; } = new(0, Uin);
	public static Uid DoctorUid { get; } = new(0, Doctor);

	public static IEnumerable<uint> FriendList => Bot.GetFriendList().GetAwaiter().GetResult().Select(f => f.Uin);
	public static IEnumerable<uint> GroupList => Bot.GetGroupList().GetAwaiter().GetResult().Select(g => g.Uin);

	public static BaseTask[] Tasks { get; } = new BaseTask[] {
		new BotHelp(),
		new AgainstQuickFactorization(),
		new FixedReply(),
		new GetWyyResource(),
		new Hug(),
		new SendMessage(),
		new TicTacToe(),
		new DrawDeemoChartClip(),
	};

	public static async Task<bool> Login()
	{
		try {
			Load();
			DisplayMessage("Loaded");
		} catch (Exception ex) {
			DisplayMessage($"Load() Exception: {ex.Message}");
			return false;
		}

		return await Bot.Login();
	}

	public static async Task<bool> Logout()
	{
		try {
			foreach (var task in Tasks) task.Save();
			DisplayMessage("Tasks Saved.");
		} catch (Exception ex) {
			DisplayMessage($"Save() Exception: {ex.Message}");
			return false;
		}

		return await Bot.Logout();
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="label"></param>
	/// <returns><see langword="0"/> When not found</returns>
	public static uint GetUidFromAbbr(string label)
		=> _uidLabels.TryGetValue(label, out uint uid) ? uid : 0;

	public enum DataOption { UidLabel = 1, Tasks = 2, All = UidLabel | Tasks }
	public static void Load(DataOption options = DataOption.All)
	{
		if (options.HasFlag(DataOption.UidLabel)) {
			LoadGroupAbbrs();
		}
		if (options.HasFlag(DataOption.Tasks)) {
			foreach (var task in Tasks) task.Load();
		}
	}

	public static void Save(DataOption options = DataOption.All)
	{
		if (options.HasFlag(DataOption.UidLabel)) {
			SaveGroupAbbrs();
		}
		if (options.HasFlag(DataOption.Tasks)) {
			foreach (var task in Tasks) task.Save();
		}
	}

	private const string GROUP_ABBRS_FILE = @"GroupAbbrs.lpm";
	private static void LoadGroupAbbrs()
	{
		var filename = CONFIG_DIR + GROUP_ABBRS_FILE;
		if (!File.Exists(filename)) return;

		using var sr = new StreamReader(filename);
		Dictionary<string, uint> dict = new();
		while (!sr.EndOfStream) {
			string? line = sr.ReadLine();
			if (line is null) {
				Console.WriteLine("Read GroupAbbrs reach null");
				continue;
			}

			string[] parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
			if (uint.TryParse(parts[1], out var uin))
				dict.Add(parts[0], uin);
			else
				Console.WriteLine($"Read GroupAbbrs reach invalid group id: {parts[1]}");
		}
		_uidLabels = dict;
	}

	private static void SaveGroupAbbrs()
	{
		Directory.CreateDirectory(CONFIG_DIR);
		using var file = File.Create(CONFIG_DIR + GROUP_ABBRS_FILE);
		using var sw = new StreamWriter(file);

		foreach (var abbr in _uidLabels) {
			sw.WriteLine(abbr.Key + ' ' + abbr.Value);
		}
	}

	#region Message
	public static async Task SendFriendMessageAsync(uint friendUin, MessageBuilder builder)
	{
		if (!await Bot.SendFriendMessage(friendUin, builder))
			throw new SendMessageFailException(friendUin, false, builder);
	}
	public static Task SendFriendMessageAsync(uint friendUin, string message) => SendFriendMessageAsync(friendUin, Message.Text(message));
	public static Task SendFriendMessageAsync(FriendMessageEvent e, MessageBuilder builder) => SendFriendMessageAsync(e.FriendUin, builder);
	public static Task SendFriendMessageAsync(FriendMessageEvent e, string message) => SendFriendMessageAsync(e.FriendUin, message);

	public static async Task SendGroupMessageAsync(uint groupUin, MessageBuilder builder)
	{
		if (!await Bot.SendGroupMessage(groupUin, builder))
			throw new SendMessageFailException(groupUin, true, builder);
	}
	public static Task SendGroupMessageAsync(uint groupUin, string message) => SendGroupMessageAsync(groupUin, Message.Text(message));
	public static Task SendGroupMessageAsync(GroupMessageEvent e, MessageBuilder builder) => SendGroupMessageAsync(e.GroupUin, builder);
	public static Task SendGroupMessageAsync(GroupMessageEvent e, string message) => SendGroupMessageAsync(e.GroupUin, message);

	public static Task SendDoctorMessageAsync(MessageBuilder builder) => SendFriendMessageAsync(Doctor, builder);
	public static Task SendDoctorMessageAsync(string message) => SendFriendMessageAsync(Doctor, message);

	public static Task ReportExceptionAsync(Exception ex)
	{
		try {
			return SendDoctorMessageAsync(Message.Text(ex.Message));
		} catch (Exception iex) {
			Console.WriteLine(iex.Message);
			throw;
		}
	}

	public static Task ReportMessageAsync(FriendMessageEvent e) => SendDoctorMessageAsync(Message.Text(
		$"Received message from friend {e.Message.Sender.Uin} {e.Message.Sender.Name}\n" +
		$"Time: {e.Message.Time}\n" +
		$"Uuid: {e.Message.Uuid}\n" +
		$"Random: {e.Message.Random}\n" +
		$"Message: {e.Chain}\n" +
		$"ChainCount: {e.Chain.Count}\n" +
		$"Sequence: {e.Message.Sequence}\n" +
		$"SessionSequence: {e.SessionSequence}\n" +
		$"ResultCode: {e.ResultCode}"));

	public static Task ReportMessageAsync(GroupMessageEvent e) => SendDoctorMessageAsync(
		$"Received message from group {e.GroupUin} {e.GroupName}\n" +
		$"Time: {e.Message.Time}\n" +
		$"Uuid: {e.Message.Uuid}\n" +
		$"Message: {e.Chain}\n" +
		$"ChainCount: {e.Chain.Count}\n" +
		$"Sequence: {e.Message.Sequence}\n" +
		$"SessionSequence: {e.SessionSequence}\n" +
		$"ResultCode: {e.ResultCode}");
	#endregion

	public static void DisplayMessage(string message) => Console.WriteLine(message);
}
