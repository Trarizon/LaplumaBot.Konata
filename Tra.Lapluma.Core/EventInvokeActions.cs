using Konata.Core.Events;
using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using System;
using System.Threading;
using Tra.Lapluma.Core.Exceptions;
using Knt = Konata.Core.Bot;

namespace Tra.Lapluma.Core;
internal static class EventInvokeActions
{
	public static Knt.KonataEvent<TEventArgs> Encapsulate<TEventArgs>(this Bot bot, Action<Bot, TEventArgs> action)
		where TEventArgs : BaseEvent
		=> (_, ev) => action(bot, ev);

	public static async void OnFriendMessageInvokeAsync(Bot bot, FriendMessageEvent ev)
	{
		if ((!bot.FriendMessage || ev.Message.Sender.Uin == bot.Uin)
			&& ev.Message.Sender.Uin != bot.Doctor) // always availble to dr.
			return;

		try {
			foreach (var task in bot.Tasks) {
				if (await task.ActivateAsync(ev))
					return;
			}
		} catch (TaskException ex) {
			await bot.SendFriendMessageAsync(ev, ex.Message);
			await bot.ReportExceptionAsync(ex);
		} catch (TaskExecutingException ex) {
			await bot.ReportExceptionAsync(ex);
			if (ex.InnerException is not null)
				await bot.ReportExceptionAsync(ex.InnerException);
		} catch (Exception ex) {
			// await bot.SendFriendMessageAsync(ev, "呜，出错了");
			await bot.ReportExceptionAsync(ex);
		}
	}

	public static async void OnGroupMessageInvokeAsync(Bot bot, GroupMessageEvent ev)
	{
		if (!bot.GroupMessage || ev.Message.Sender.Uin == bot.Uin)
			return;

		try {
			foreach (var task in bot.Tasks) {
				if (await task.ActivateAsync(ev))
					return;
			}
		} catch (TaskException ex) {
			// await bot.SendGroupMessageAsync(ev, ex.Message);
			await bot.ReportExceptionAsync(ex);
		} catch (TaskExecutingException ex) {
			await bot.ReportExceptionAsync(ex);
			if (ex.InnerException is not null)
				await bot.ReportExceptionAsync(ex.InnerException);
		} catch (Exception ex) {
			// await bot.SendGroupMessageAsync(ev, "呜，出错了");
			await bot.ReportExceptionAsync(ex);
		}
	}

	public static void OnBotOnlineInvokeAsync(Bot bot, BotOnlineEvent ev)
		=> bot.SendDoctorMessageAsync("ドクター、こんにちは");

	public static void OnBotOfflineInvokeAsync(Bot bot, BotOfflineEvent ev) { }

	public static async void OnFriendPokeInvokeAsync(Bot bot, FriendPokeEvent ev)
	{
		if (bot.FriendPoke)
			await bot.SendFriendMessageAsync(ev.FriendUin, "唔嗯？");
	}

	public static void OnGroupPokeInvokeAsync(Bot bot, GroupPokeEvent ev)
	{
		if (bot.GroupPoke && ev.MemberUin == bot.Uin)
			bot.SendGroupMessageAsync(ev.GroupUin, "唔嗯？");
	}

	public static async void OnGroupInviteInvokeAsync(Bot bot, GroupInviteEvent ev)
	{
		await bot.SendDoctorMessageAsync($"博士，{ev.InviterUin}({ev.InviterNick})邀请我加入{ev.GroupUin}({ev.GroupName})，可以进吗？");

		bool result = false;
		AutoResetEvent are = new(false);
		bot.Knt.OnFriendMessage += WaitResponse;

		are.WaitOne();
		if (result)
			await bot.Knt.ApproveGroupInvitation(ev.GroupUin, ev.InviterUin, ev.Token);
		else
			await bot.Knt.DeclineGroupInvitation(ev.GroupUin, ev.InviterUin, ev.Token);
		are.Dispose();

		void WaitResponse(Knt knt, FriendMessageEvent e)
		{
			if (e.FriendUin != bot.Doctor) return;

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

			are.Set();

			knt.OnFriendMessage -= WaitResponse;
		}
	}

}
