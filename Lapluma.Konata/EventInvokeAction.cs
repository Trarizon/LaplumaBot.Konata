using Konata.Core;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Lapluma.Konata.Utilities.Structrues;
using System;
using System.Text.RegularExpressions;

namespace Lapluma.Konata;
internal static class EventInvokeAction
{
	public static async void OnFriendMessageInvokeAsync(Bot _, FriendMessageEvent e)
	{
		if (e.Message.Sender.Uin == Lapluma.Uin) return;

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
		} catch (Exception ex) {
			if (e.FriendUin != Lapluma.Doctor)
				await Lapluma.SendFriendMessageAsync(e, ex.Message);
			await Lapluma.ReportExceptionAsync(ex);
		}
	}

	public static async void OnGroupMessageInvokeAsync(Bot _, GroupMessageEvent e)
	{
		if (e.MemberUin == Lapluma.Uin) return;

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
		} catch (Exception ex) {
			await Lapluma.SendGroupMessageAsync(e, "诶，出错了");
			await Lapluma.ReportExceptionAsync(ex);
		}
	}

	public static void OnBotOnlineInvokeAsync(Bot _, BotOnlineEvent e)
		=> Lapluma.SendDoctorMessageAsync("ドクター、こんにちは");

	public static void OnBotOfflineInvokeAsync(Bot _, BotOfflineEvent e) { }

	public static void OnFriendPokeInvokeAsync(Bot _, FriendPokeEvent e)
		=> Lapluma.SendFriendMessageAsync(e.FriendUin, "唔嗯？");

	public static void OnGroupPokeInvokeAsync(Bot _, GroupPokeEvent e)
	{
		if (e.MemberUin == Lapluma.Uin)
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

		void WaitResponse(Bot _,FriendMessageEvent e)
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
