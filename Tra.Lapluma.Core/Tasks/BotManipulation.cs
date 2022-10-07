using Konata.Core.Events.Model;
using Konata.Core.Message;
using System;
using System.Threading.Tasks;
using Tra.Lapluma.Core.Utilities;

namespace Tra.Lapluma.Core.Tasks;
internal class BotManipulation : BaseTask
{
	public BotManipulation(Bot bot) : base(bot,
		actRgx: "bot_manip",
		name: nameof(BotManipulation),
		summary: "用于操控bot",
		help: $"{ActPfx}[bot_manip] <param>\n" +
		"# reload => 全部重新加载\n" +
		"# save => 全部保存\n" +
		"# (enable|disable) (f|g)(msg|pok)")
	{ }

	protected override Task<bool> OnActivateAsync(FriendMessageEvent ev)
		=> Manipulate(ev.Message, _bot.SenderToFriend(ev));

	protected override Task<bool> OnActivateAsync(GroupMessageEvent ev)
		=> Manipulate(ev.Message, _bot.SenderToGroup(ev));

	public async Task<bool> Manipulate(MessageStruct message, Message.Sender sender)
	{
		var match = message.Chain.ToString().MatchActRegex(@"(?:bot_manip)? (.+)");
		if (!match.Success) return false;
		const int PARAM = 1;

		var parameters = match.Groups[PARAM].Value.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
		switch (parameters[0]) {
			case "reload":
				try {
					_bot.Load();
					await sender(Message.Text("已重新加载"));
				} catch (Exception) {
					await sender(Message.Text($"加载出错"));
					throw;
				}
				break;
			case "save":
				try {
					_bot.Save();
					await sender(Message.Text("已全部保存"));
				} catch (Exception) {
					await sender(Message.Text($"保存出错"));
					throw;
				}
				break;

			case "enable":
				await sender(Message.Text(EnableDisable(true, parameters[1])));
				break;
			case "disable":
				await sender(Message.Text(EnableDisable(false, parameters[1])));
				break;

			default: return false;
		}

		return true;

		string EnableDisable(bool enable, string paramInLowerCase)
		{
			switch (paramInLowerCase) {
				case "fmsg":
					_bot.FriendMessage = enable;
					return $"已{(enable ? "启用" : "禁用")}FriendMessage";
				case "gmsg":
					_bot.GroupMessage = enable;
					return $"已{(enable ? "启用" : "禁用")}GroupMessage";
				case "fpok":
					_bot.FriendPoke = enable;
					return $"已{(enable ? "启用" : "禁用")}FriendPoke";
				case "gpok":
					_bot.GroupPoke = enable;
					return $"已{(enable ? "启用" : "禁用")}GroupPoke";
				default: return $"未识别{paramInLowerCase}";
			}
		}

	}
}
