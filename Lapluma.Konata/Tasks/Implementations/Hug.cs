using Konata.Core.Events.Model;
using Lapluma.Konata.Utilities;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Konata.Core.Message.BaseChain.ChainType;

namespace Lapluma.Konata.Tasks.Implementations;
internal sealed class Hug : BaseTask
{
	public Hug() : base(
		name: nameof(Hug),
		summary: "抱抱~",
		help: "@Lapluma 抱抱 => 抱抱！",
		cmdRgx: "抱抱|hug",
		friendCfg: (true, FriendRange.All),
		groupCfg: (true, GroupRange.All, OperatorRange.Admin))
	{ }

	protected override async Task<bool> ExecuteAsync(FriendMessageEvent e)
	{
		if (Regex.IsMatch(e.Chain.ToString(), @"^抱+[~!！]?$")) {
			await Lapluma.SendFriendMessageAsync(e, "抱抱~");
			return true;
		}
		return false;
	}

	protected override async Task<bool> ExecuteAsync(GroupMessageEvent e)
	{
		if (e.Chain.Ated(Lapluma.Uin) && Regex.IsMatch(e.Chain[Text][0].ToString(), @"^抱+[~!！]?$")) {
			await Lapluma.SendGroupMessageAsync(e, Message.At(e.MemberUin).Text("抱抱~"));
			return true;
		}
		return false;
	}
}
