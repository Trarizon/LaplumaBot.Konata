using Konata.Core.Common;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tra.Lapluma.Core.Exceptions;
using Tra.Lapluma.Core.Tasks;
using Tra.Lapluma.Core.Tasks.UserTasks;
using Tra.Lapluma.Core.Tasks.UserTasks.Models;
using Tra.Lapluma.Core.Utilities;
using static Tra.Lapluma.Core.EventInvokeActions;
using KntBot = Konata.Core.Bot;

namespace Tra.Lapluma.Core;
public sealed class Bot
{
	internal KntBot Knt { get; }

	public uint Uin => Knt.Uin;
	public uint Doctor => DoctorUin;

	public Bot()
	{
		// Bot Initialization
		{
			BotConfig cfg = new()
			{
				EnableAudio = true,
				TryReconnect = false,
				HighwayChunkSize = 8192
			};

			BotDevice dev;
			const string DEVICE_LOG = ConfigDir + @"BotInfo\Device.json";
			if (File.Exists(DEVICE_LOG))
				dev = JsonSerializer.Deserialize<BotDevice>(File.ReadAllText(DEVICE_LOG));
			else {
				dev = BotDevice.Default();
				Directory.CreateDirectory(ConfigDir + "BotInfo");
				using var sw = File.CreateText(DEVICE_LOG);
				sw.Write(JsonSerializer.Serialize(dev));
			}

			BotKeyStore ks;
			const string KEYSTORE_LOG = ConfigDir + @"BotInfo\KeyStore.json";
			if (File.Exists(KEYSTORE_LOG))
				ks = JsonSerializer.Deserialize<BotKeyStore>(File.ReadAllText(KEYSTORE_LOG));
			else {
				ks = new(
					Util.Input("Enter ID: "),
					Util.Input("Enter password: "));
				Directory.CreateDirectory(ConfigDir + "BotInfo");
				using var sw = File.CreateText(KEYSTORE_LOG);
				sw.Write(JsonSerializer.Serialize(ks));
			}

			Knt = BotFather.Create(cfg, dev, ks);
		}
		// Config
		{
			Knt.OnLog += (_, ev) => Util.Output(ev.EventMessage);
			Knt.OnCaptcha += (bot, e) =>
			{
				switch (e.Type) {
					case CaptchaEvent.CaptchaType.Sms:
						bot.SubmitSmsCode(Util.Input($"A SMS has been sent to {e.Phone}.\nCode: \n"));
						break;
					case CaptchaEvent.CaptchaType.Slider:
						bot.SubmitSliderTicket(Util.Input($"Slider captcha required. Url:\n{e.SliderUrl}\n"));
						break;
					default:
						throw new Exception("Unknown captcha type.");
				}
			};
		}

		// Events
		{
			Knt.OnFriendMessage += this.Encapsulate<FriendMessageEvent>(OnFriendMessageInvokeAsync);
			Knt.OnGroupMessage += this.Encapsulate<GroupMessageEvent>(OnGroupMessageInvokeAsync);
			Knt.OnBotOnline += this.Encapsulate<BotOnlineEvent>(OnBotOnlineInvokeAsync);
			Knt.OnBotOffline += this.Encapsulate<BotOfflineEvent>(OnBotOfflineInvokeAsync);
			Knt.OnFriendPoke += this.Encapsulate<FriendPokeEvent>(OnFriendPokeInvokeAsync);
			Knt.OnGroupPoke += this.Encapsulate<GroupPokeEvent>(OnGroupPokeInvokeAsync);
		}

		// Tasks
		_commandTasks = new()
		{
			new BotManipulation(this),
			new SendMessage(this),
		};
		_userTasks = new()
		{
			new BotHelp(this),
			new DeemoChartClipPainter(this),
			new GetWyyResource(this),
			new SolveHikariQf(this),
			new TicTacToe(this),
		};
	}

	public bool FriendMessage { get; set; }
	public bool GroupMessage { get; set; }
	public bool FriendPoke { get; set; }
	public bool GroupPoke { get; set; }

	private readonly List<BaseTask> _commandTasks;
	private readonly List<UserTask> _userTasks;
	public IEnumerable<UserTask> UserTasks => _userTasks;
	public IEnumerable<BaseTask> Tasks => _commandTasks.Concat(_userTasks);

	public void Load()
	{
		LoadUinAbbrsValue();
		LoadEvents();
		foreach (var task in Tasks) task.Load();
	}

	public void Save()
	{
		SaveUinAbbrs();
		SaveEvents();
		foreach (var task in Tasks) task.Save();
	}

	#region Login
	public bool IsOnline => Knt.IsOnline();

	public async Task<bool> Login()
	{
		try {
			Load();
			Util.Output("Data Loaded.");
		} catch (Exception ex) {
			Util.Output("Load() Exception: " + ex.Message);
			return false;
		}
		return await Knt.Login();
	}

	public async Task<bool> Logout()
	{
		if (await Knt.Logout()) {
			try {
				Save();
				Util.Output("Data Saved.");
			} catch (Exception ex) {
				Util.Output("Save() Exception: " + ex.Message);
				return false;
			}
			return true;
		}
		else return false;
	}
	#endregion

	#region Data Cache
	private void LoadEvents()
	{
		bool fmsg, gmsg, fpok, gpok;
		fmsg = gmsg = fpok = gpok = true;

		if (File.Exists(EventsPath)) {
			using var sr = new StreamReader(EventsPath);

			while (!sr.EndOfStream) {
				var line = sr.ReadLine();
				if (line is null) {
					Util.Output("Load Events meet null");
					continue;
				}
				var splits = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
				switch (splits[0]) {
					case "fmsg": fmsg = splits[1][0] != '0'; break;
					case "gmsg": gmsg = splits[1][0] != '0'; break;
					case "fpok": fpok = splits[1][0] != '0'; break;
					case "gpok": gpok = splits[1][0] != '0'; break;
					default: break;
				}
			}
		}

		FriendMessage = fmsg;
		GroupMessage = gmsg;
		FriendPoke = fpok;
		GroupPoke = gpok;
	}

	private void SaveEvents()
	{
		Directory.CreateDirectory(ConfigDir);
		using var sw = new StreamWriter(EventsPath);

		sw.WriteLine($"fmsg {(FriendMessage ? '1' : '0')}");
		sw.WriteLine($"gmsg {(GroupMessage ? '1' : '0')}");
		sw.WriteLine($"fpok {(FriendPoke ? '1' : '0')}");
		sw.WriteLine($"fpok {(GroupPoke ? '1' : '0')}");
	}
	#endregion

	#region Messaging
	public async Task SendFriendMessageAsync(uint friendUin, MessageBuilder message)
	{
		try {
			if (!await Knt.SendFriendMessage(friendUin, message))
				throw new MessagingException(friendUin, false, message);
		} catch (Exception ex) {
			Util.Output(ex.Message);
		}

	}
	public Task SendFriendMessageAsync(uint friendUin, string message) => SendFriendMessageAsync(friendUin, Message.Text(message));
	public Task SendFriendMessageAsync(FriendMessageEvent ev, MessageBuilder message) => SendFriendMessageAsync(ev.FriendUin, message);
	public Task SendFriendMessageAsync(FriendMessageEvent ev, string message) => SendFriendMessageAsync(ev.FriendUin, Message.Text(message));

	// TODO : how if bot is muted, try to find when the func will return false
	public async Task SendGroupMessageAsync(uint groupUin, MessageBuilder message)
	{
#if DEBUG
		if (groupUin != 273547613) return;
#endif
		try {
			if (!await Knt.SendGroupMessage(groupUin, message))
				throw new MessagingException(groupUin, true, message);
		} catch (Exception ex) {
			Util.Output(ex.Message);
		}
	}
	public Task SendGroupMessageAsync(uint groupUin, string message) => SendGroupMessageAsync(groupUin, Message.Text(message));
	public Task SendGroupMessageAsync(GroupMessageEvent ev, MessageBuilder message) => SendGroupMessageAsync(ev.GroupUin, message);
	public Task SendGroupMessageAsync(GroupMessageEvent ev, string message) => SendGroupMessageAsync(ev.GroupUin, Message.Text(message));

	public Task SendDoctorMessageAsync(MessageBuilder message) => SendFriendMessageAsync(Doctor, message);
	public Task SendDoctorMessageAsync(string message) => SendFriendMessageAsync(Doctor, Message.Text(message));

	public Task ReportExceptionAsync(Exception ex)
	{
		try {
			return SendDoctorMessageAsync(ex.Message + '\n' + ex.StackTrace);
		} catch (Exception iex) {
			Console.WriteLine(iex.Message);
			throw;
		}
	}
	#endregion
}
