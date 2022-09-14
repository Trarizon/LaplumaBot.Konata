using Konata.Core.Events.Model;
using Konata.Core.Message;
using Lapluma.Konata.Utilities;
using Lapluma.Konata.Utilities.Structrues;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lapluma.Konata.Tasks.Implementations;
internal sealed class BotHelp : BaseTask
{
	public BotHelp() : base(
		name: nameof(BotHelp),
		summary: "lpm {#param} Bot帮助",
		help: "lpm {#param} Bot帮助\n" +
		"# help => 查看帮助\n" +
		"# tasks => 查看当前启用tasks\n" +
		"# friendreq => 添加好友问题答案",
		cmdRgx: "lpm",
		friendCfg: (true, FriendRange.All),
		groupCfg: (true, GroupRange.All, OperatorRange.Doctor))
	{ }

	protected override Task<bool> ExecuteAsync(FriendMessageEvent e)
		=> GeneralExecuteAsync(new(e), e.Chain, MessageHandlers.SendFriendAsync(e));

	protected override Task<bool> ExecuteAsync(GroupMessageEvent e)
		=> GeneralExecuteAsync(new(e), e.Chain, MessageHandlers.SendGroupAsync(e));

	private static async Task<bool> GeneralExecuteAsync(Uid uid, MessageChain chains, MessageHandler<MessageBuilder> handler)
	{
		var match = Regex.Match(chains.ToString().ToLower(), @"^lpm(?: +(.*))?$");
		if (!match.Success) return false;
		#region Regex index
		const int PARAM = 1;
		#endregion
		string msg = match.Groups[PARAM].Value.ToLower() switch
		{
			"" =>
			(DateTime.Now.Hour switch { < 5 => "很晚了哦", < 10 => "早安", < 12 => "早上好", < 14 => "午安", < 18 => "下午好", _ => "晚上好" }) +
			$"，这里是拉菲艾拉，代号羽毛笔。博士说可以让我帮大家处理一些简单的工作，如果有我能够做到的事，可以找我哦。\n",

			"help" =>
			"发送taskname[ params]执行task；\n" +
			"发送taskname:param获取task信息，或开启/关闭任务，通用的可用参数有:name, summary, help, enable, disable, reload；\n" +
			"API限制，私聊功能需要添加好友",

			"tasks" => GetTasks(uid),

			"friendreq" => "RafaelaSilva",
			_ => "唔...我不明白什么意思",
		};
		await handler(Message.Text(msg));
		return true;

		static string GetTasks(Uid uid)
		{
			StringBuilder sb;
			if (uid == Lapluma.DoctorUid) {
				sb = new("目前拥有的tasks\n");
				foreach (var task in Lapluma.Tasks) {
					sb.AppendLine($"{task.Name}: " +
						$"私聊{(task.FriendEnable ? (task.FriendOn ? "开启" : "关闭") : "禁用")} " +
						$"群聊{(task.GroupEnable ? (task.GroupOn ? "开启" : "关闭") : "禁用")}");
				}
			}
			else {
				sb = new("已有启用功能有\n");
				foreach (var task in Lapluma.Tasks.Where(t => t.IsEnable(uid))) {
					sb.AppendLine(task.Summary);
				}
			}
			return sb.ToString();
		}
	}
}
