using Konata.Core.Events.Model;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lapluma.Konata.Tasks.Implementations;
internal sealed class FixedReply : BaseTask
{
	private static readonly ConcurrentDictionary<string, string> _replys = new();

	public FixedReply() : base(
		name: nameof(FixedReply),
		summary: "对部分消息的固定回复",
		help: "fxreply {#sender}-=>{#reply} => 定义回复消息",
		cmdRgx: "fxreply",
		friendCfg: (false, FriendRange.Banned),
		groupCfg: (false, GroupRange.All, OperatorRange.Admin))
	{ }

	protected override Task<bool> ExecuteAsync(FriendMessageEvent e) => throw new NotSupportedException();

	protected override async Task<bool> ExecuteAsync(GroupMessageEvent e)
	{
		var chainstr = e.Chain.ToString().ToLower();
		var match = Regex.Match(chainstr, @"^fxreply (.+)-=>(.+)$");
		if (!match.Success) {
			// Reply
			if (!_replys.TryGetValue(chainstr, out var rp))
				return false;

			await Lapluma.SendGroupMessageAsync(e, rp);
			return true;
		}
		else {
			#region Regex index
			const int SEND = 1;
			const int REPLY = SEND + 1;
			#endregion
			var rp = match.Groups[REPLY].Value;
			_replys.AddOrUpdate(match.Groups[SEND].Value, rp, (_, _) => rp);
			await Lapluma.SendGroupMessageAsync(e, "新规则已添加");
			return true;
		}
	}
}
