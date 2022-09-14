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
		"# save => 保存所有task，若要保存单个任务，使用taskname:save",
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

		switch (match.Groups[PARAM].Value.ToLower()) {
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
			default: return false;
		}
		return true;
	}


}
