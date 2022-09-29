using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Lapluma.Konata.Exceptions;
using Lapluma.Konata.Utilities.Structrues;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Lapluma.Konata;
internal static class EventInvokeAction
{
	public static bool EnableFriendMessage { get; set; } = true;
	public static bool EnableGroupMessage { get; set; } = true;
	public static bool EnableFriendPoke { get; set; } = true;
	public static bool EnableGroupPoke { get; set; } = true;

	const string FILE = Lapluma.CONFIG_DIR + "Events.lpm";
	public static void Load()
	{
		if (!File.Exists(FILE)) return;
		using var sr = new StreamReader(FILE);

		bool fmsg, gmsg, fpok, gpok;
		fmsg = gmsg = fpok = gpok = true;
		while (!sr.EndOfStream) {
			var line = sr.ReadLine();
			if (line is null) {
				Console.WriteLine("Load Events meet null");
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

		EnableFriendMessage = fmsg;
		EnableGroupMessage = gmsg;
		EnableFriendPoke = fpok;
		EnableGroupPoke = gpok;
	}

	public static void Save()
	{
		Directory.CreateDirectory(Lapluma.CONFIG_DIR);
		using var sw = new StreamWriter(FILE);

		sw.WriteLine($"fmsg {(EnableFriendMessage ? 1 : 0)}");
		sw.WriteLine($"gmsg {(EnableGroupMessage ? 1 : 0)}");
		sw.WriteLine($"fpok {(EnableFriendPoke ? 1 : 0)}");
		sw.WriteLine($"fpok {(EnableGroupPoke ? 1 : 0)}");
	}

	public static async void OnFriendMessageInvokeAsync(Bot _, FriendMessageEvent e)
	{
		if (!EnableFriendMessage ||
			e.Message.Sender.Uin == Lapluma.Uin) return;

		try {
			var match = Regex.Match(e.Chain.ToString(), @"^([^ \n:]+):([^:]+)$");
			if (match.Success) {
				foreach (var task in Lapluma.Tasks)
					if (await task.OperateAsync(new Uid(e), match.Groups[1].Value, match.Groups[2].Value))
						return;
			}
			else {
				foreach (var task in Lapluma.Tasks) {
					if (await task.ActivateAsync(e))
						return;
				}
			}
		} catch (TaskException ex) {
			await Lapluma.SendFriendMessageAsync(e, ex.Message);
		} catch (Exception ex) {
			if (e.FriendUin != Lapluma.Doctor)
				await Lapluma.SendFriendMessageAsync(e, ex.Message);
			await Lapluma.ReportExceptionAsync(ex);
		}
	}

	public static async void OnGroupMessageInvokeAsync(Bot _, GroupMessageEvent e)
	{
		if (!EnableGroupMessage ||
			e.MemberUin == Lapluma.Uin) return;

		try {
			var match = Regex.Match(e.Chain.ToString(), @"^([^ \n:]+):([^:]+)$");
			if (match.Success) {
				foreach (var task in Lapluma.Tasks)
					if (await task.OperateAsync(new Uid(e), match.Groups[1].Value, match.Groups[2].Value))
						return;
			}
			else {
				foreach (var task in Lapluma.Tasks)
					if (await task.ActivateAsync(e))
						return;
			}
		} catch (TaskException ex) {
			await Lapluma.SendGroupMessageAsync(e, ex.Message);
		} catch (Exception ex) {
			await Lapluma.SendGroupMessageAsync(e, "诶，出错了");
			await Lapluma.ReportExceptionAsync(ex);
		}
	}

	public static void OnBotOnlineInvokeAsync(Bot _, BotOnlineEvent e)
		=> Lapluma.SendDoctorMessageAsync("ドクター、こんにちは");

	public static void OnBotOfflineInvokeAsync(Bot _, BotOfflineEvent e) { }

	public static void OnFriendPokeInvokeAsync(Bot _, FriendPokeEvent e)
	{
		if (EnableFriendPoke)
			Lapluma.SendFriendMessageAsync(e.FriendUin, "唔嗯？");
	}

	public static void OnGroupPokeInvokeAsync(Bot _, GroupPokeEvent e)
	{
		if (EnableGroupPoke && 
			e.MemberUin == Lapluma.Uin)
			Lapluma.SendGroupMessageAsync(e.GroupUin, "唔嗯？");
	}

	public static async void OnGroupInviteInvokeAsync(Bot _, GroupInviteEvent e)
	{
		await Lapluma.SendDoctorMessageAsync($"博士，{e.InviterUin}({e.InviterNick})邀请我加入{e.GroupUin}({e.GroupName})，我可以进吗？");

		bool result = false;
		Lapluma.Bot.OnFriendMessage += WaitResponse;

		if (result)
			await Lapluma.Bot.ApproveGroupInvitation(e.GroupUin, e.InviterUin, e.Token);
		else
			await Lapluma.Bot.DeclineGroupInvitation(e.GroupUin, e.InviterUin, e.Token);

		void WaitResponse(Bot _, FriendMessageEvent e)
		{
			if (e.FriendUin != Lapluma.Doctor) return;

			switch (e.Chain.ToString()) {
				case "行":
				case "可以":
					result = true;
					break;
				case "不行":
				case "不可以":
					result = true;
					break;
				default:
					return;
			}

			Lapluma.Bot.OnFriendMessage -= WaitResponse;
		}
	}
}
