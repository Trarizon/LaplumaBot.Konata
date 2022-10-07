using Konata.Core.Common;
using Konata.Core.Events.Model;
using Konata.Core.Message;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tra.Lapluma.Core.Utilities;
using Tra.Lapluma.Core.Utilities.Model;

namespace Tra.Lapluma.Core.Tasks.UserTasks.Models;
internal sealed class BotHelp : UserTask
{
	public BotHelp(Bot bot) : base(bot,
		actRgx: "lpm",
		name: nameof(BotHelp),
		summary: "Bot的帮助页面",
		help: "lpm [<param>]\n" +
		"# help => 使用教程\n" +
		"# tasks (-all) => 列出启用任务，管理员以上可以列出所有任务",
		friendDefaultEnable: true,
		groupDefaultEnable: true)
	{
	}

	protected override Task<bool> OnActivateAsync(FriendMessageEvent ev)
		=> GetHelp(Uid.Of(ev), ev.Chain, _bot.SenderToFriend(ev));

	protected override Task<bool> OnActivateAsync(GroupMessageEvent ev)
		=> GetHelp(Uid.Of(ev), ev.Chain, _bot.SenderToGroup(ev));

	private async Task<bool> GetHelp(Uid uid, MessageChain chains, Message.Sender sender)
	{
		var match = chains.ToString().ToLower()
			.MatchActRegex(@"lpm(?: (.*))?");
		if (!match.Success) return false;
		const int PARAM = 1;

		string? msg = match.Groups[PARAM].Value switch
		{
			"" =>
			DateTime.Now.Hour switch
			{
				< 5 => "很晚了哦",
				< 10 => "早安",
				< 12 => "早上好",
				< 14 => "午安",
				< 18 => "下午好",
				_ => "晚上好"
			} +
			"，这里是拉菲艾拉，代号羽毛笔。有我可以做到的事欢迎找我帮忙哦\n",

			"help" =>
			$"发送{ActPfx}<taskcmd>[ params]执行task；\n" +
			$"发送{ActPfx}<name|cmd>:params可获取任务信息，管理员以上可以开关任务，\n" +
			"可用参数有:name, summary, help, enable, disable\n" +
			"私聊功能需要添加好友",

			"tasks" => GetTasks(uid, false),
			"tasks -all" => GetTasks(uid, true),

			"friendreq" => "RafaelaSilva",

			"poke" => "唔嗯？",

			_ => null
		};
		if (msg == null) return false;
		await sender(Message.Text(msg));
		return true;

		string GetTasks(Uid uid, bool all)
		{
			StringBuilder sb;
			var usertasks = _bot.UserTasks;
			// Group admin
			if (all & uid.Group != 0 && _bot.CheckAuthorizationAsync(uid.User, uid.Group, RoleType.Admin).AwaitSync()) {
				sb = new("目前拥有的task\n");
				foreach (var task in usertasks)
					sb.AppendLine($"{task.Name} - {task.Summary}");
			}
			// Doctor's private chat
			else if (uid.Group == 0 && uid.User == _bot.Doctor) {
				sb = new("目前拥有的task\n");
				foreach (var task in usertasks)
					sb.AppendLine($"{task.Name}: " +
						$"私聊{(task.FriendActive ? "启用" : "禁用")} " +
						$"群聊{(task.GroupActive ? "启用" : "禁用")}");
			}
			// normal
			else if (!all) {
				sb = new("已启用的task\n");
				foreach (var task in usertasks.Where(t => t.Allow(uid)))
					sb.AppendLine(task.Name + " - " + task.Summary);
			}
			else
				return "权限不足";

			return sb.ToString();
		}
	}
}