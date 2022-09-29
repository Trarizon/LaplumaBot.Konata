using Konata.Core.Events.Model;
using Lapluma.Konata.Utilities.Structrues;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lapluma.Konata.Tasks.Implementations;
internal sealed class BotManipulation : BaseTask
{
	public BotManipulation() : base(
		name: nameof(BotManipulation),
		summary: "# {param} => 操控bot",
		help: "# reload => 全部重新加载，若要重新加载单个任务，使用taskname:reload\n" +
		"# save => 保存所有task，若要保存单个任务，使用taskname:save" +
		"# (enable|disable) (fmsg|gmsg|fpok|gpok)",
		cmdRgx: "bot_manip|#",
		friendCfg: (true, FriendRange.Doctor),
		groupCfg: (true, GroupRange.Doctor, OperatorRange.Doctor))
	{ }

	protected override Task<bool> ExecuteAsync(FriendMessageEvent e)
		=> GeneralExecute(e.Chain.ToString(), MessageHandlers.SendFriendStringAsync(e));

	protected override Task<bool> ExecuteAsync(GroupMessageEvent e)
		=> GeneralExecute(e.Chain.ToString(), MessageHandlers.SendGroupStringAsync(e));

	private static async Task<bool> GeneralExecute(string chainstr, MessageHandler<string> handler)
	{
		var match = Regex.Match(chainstr, @"^(?:#|bot_manip) (.+)$");
		if (!match.Success) return false;
		#region Regex index
		const int PARAM = 1;
		#endregion

		string input = match.Groups[PARAM].Value.ToLower();
		switch (input) {
			case "reload":
				try {
					Lapluma.Load();
					await handler("已经重新加载了！");
				} catch (Exception ex) {
					await handler($"加载出问题了\n{ex.Message}");
				}
				break;
			case "save":
				try {
					Lapluma.Save();
					await handler("全部保存完毕");
				} catch (Exception ex) {
					await handler($"保存出问题了\n{ex.Message}");
				}
				break;
			default: break;
		}

		var splits = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
		switch (splits[0]) {
			case "enable": await handler(EnableOrDisable(true, splits[1])); break;
			case "disable": await handler(EnableOrDisable(false, splits[1])); break;
			default: return false;
		}
		return true;

		static string EnableOrDisable(bool enable, string paramInLowerCase)
		{
			switch (paramInLowerCase) {
				case "fmsg":
					EventInvokeAction.EnableFriendMessage = enable;
					return $"已{(enable ? "启用" : "禁用")}FriendMessage";
				case "gmsg":
					EventInvokeAction.EnableGroupMessage = enable;
					return $"已{(enable ? "启用" : "禁用")}GroupMessage";
				case "fpok":
					EventInvokeAction.EnableFriendPoke = enable;
					return $"已{(enable ? "启用" : "禁用")}FriendPoke";
				case "gpok":
					EventInvokeAction.EnableGroupPoke = enable;
					return $"已{(enable ? "启用" : "禁用")}GroupPoke";
				default: return $"未识别{paramInLowerCase}";
			}
		}
	}


}
