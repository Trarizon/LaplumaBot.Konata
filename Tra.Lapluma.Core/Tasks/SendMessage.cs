using Konata.Core.Events.Model;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using System.Linq;
using System.Threading.Tasks;
using Tra.Lapluma.Core.Utilities;

namespace Tra.Lapluma.Core.Tasks;
internal sealed class SendMessage : BaseTask
{
	public SendMessage(Bot bot) : base(bot,
		actRgx: "s(f|g)?msg",
		name: nameof(SendMessage),
		summary: "操控bot发送消息至群",
		help: "s[f|g]msg <uid> <msg>\n" +
		"f|g => 发送好友或群聊，默认群聊\n" +
		"uid => 号码")
	{ }

	protected override Task<bool> OnActivateAsync(FriendMessageEvent ev)
		=> ExecuteAsync(ev.Chain, _bot.SenderToFriend(ev));

	protected override Task<bool> OnActivateAsync(GroupMessageEvent ev)
		=> ExecuteAsync(ev.Chain, _bot.SenderToGroup(ev));

	private async Task<bool> ExecuteAsync(MessageChain chains, Message.Sender sender)
	{
		var firstChain = chains[0].ToString()?.ToLower();
		var match = firstChain?.MatchActRegex("s(f|g)?msg +([^ ]+) (.*)");
		if (match is null || !match.Success) return false;
		const int TYPE = 1;
		const int UID = 2;
		const int MSG = 3;

		bool toGroup = match.Groups[TYPE].Value != "f";

		// Uin
		var uinstr = match.Groups[UID].Value;
		var uin = SearchUin(uinstr);
		if (uin == 0) {
			await sender(Message.Text($"唔，没找到 {uinstr} 对应的群或好友呢"));
			return false;
		}

		if (toGroup
			&& (await _bot.Knt.GetGroupList()).Any(g => g.Uin == uin)) {
			await sender(Message.Text("唔，可是我没加过这个群"));
			return false;
		}
		else if ((await _bot.Knt.GetFriendList()).Any(f => f.Uin == uin)) {
			await sender(Message.Text("唔，我没有他的好友"));
			return false;
		}

		// Message
		var builder = new MessageBuilder();
		var first = match.Groups[MSG].Value;
		if (first != "") builder.Text(first);
		builder.Add(chains.Skip(1));

		if (builder.Build().Count == 0)
			return true;

		if (toGroup)
			await _bot.SendFriendMessageAsync(uin, builder);
		else
			await _bot.SendGroupMessageAsync(uin, builder);
		return true;
	}
}
