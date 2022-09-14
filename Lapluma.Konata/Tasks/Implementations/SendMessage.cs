using Konata.Core.Events.Model;
using Konata.Core.Message;
using Lapluma.Konata.Utilities;
using Lapluma.Konata.Utilities.Structrues;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lapluma.Konata.Tasks.Implementations;
internal sealed class SendMessage : BaseTask
{
	public SendMessage() : base(
		name: nameof(SendMessage),
		summary: "s(f|g)?msg {#uid} {#msg} => 操控发送消息至群",
		help: "s(f|g)?msg {#msg} => 操控羽毛笔发送消息",
		cmdRgx: "s(f|g)?msg",
		friendCfg: (true, FriendRange.Doctor),
		groupCfg: (false, GroupRange.Doctor, OperatorRange.Doctor))
	{ }

	protected override Task<bool> ExecuteAsync(FriendMessageEvent e)
		=> GeneralExecuteAsync(e.Chain, MessageHandlers.SendFriendAsync(e));

	protected override Task<bool> ExecuteAsync(GroupMessageEvent e)
		=> GeneralExecuteAsync(e.Chain, MessageHandlers.SendGroupAsync(e));

	private static async Task<bool> GeneralExecuteAsync(MessageChain chains, MessageHandler<MessageBuilder> handler)
	{
		var firstChain = chains[0].ToString()?.ToLower();
		var match = Regex.Match(firstChain,
			@"^s(f|g)?msg +([^ ]+) (.*)$");
		if (!match.Success) return false;
		#region Regex index
		const int TYPE = 1;
		const int UID = TYPE + 1;
		const int MSG = UID + 1;
		#endregion
		// Type
		bool toGroup = match.Groups[TYPE].Value != "f";
		// Get id
		var uidstr = match.Groups[UID].Value;
		if (!uint.TryParse(uidstr, out var uid))
			uid = Lapluma.GetUidFromAbbr(uidstr);
		// Check available
		if (uid == 0) {
			await handler(Message.Text($"唔，我没找到{uidstr}对应的ID"));
			return true;
		}
		if (!(toGroup ? Lapluma.GroupList : Lapluma.FriendList).Contains(uid)) {
			await handler(Message.Text("唔，可是我没加过这个群"));
			return true;
		}
		// Message
		var builder = new MessageBuilder();
		var first = match.Groups[MSG].Value;

		if (first != "") builder.Text(first);
		builder.Add(chains.Skip(1));

		if (builder.Build().Count == 0) return true; // Send nothing

		if (toGroup)
			await Lapluma.SendGroupMessageAsync(uid, builder);
		else
			await Lapluma.SendFriendMessageAsync(uid, builder);
		return true;
	}
}
