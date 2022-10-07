using Konata.Core.Events.Model;

namespace Tra.Lapluma.Core.Utilities.Model;
// Unique identification of sender
internal record class Uid
{
	public uint Group { get; }
	public uint User { get; }

	private Uid(uint group, uint user)
	{
		Group = group;
		User = user;
	}

	public static Uid Of(FriendMessageEvent ev) => new(0, ev.FriendUin);

	public static Uid Of(GroupMessageEvent ev) => new(ev.GroupUin, ev.MemberUin);

	public static Uid OfGroup(GroupMessageEvent ev) => new(ev.GroupUin, 0);

	public static bool operator ==(Uid uid, (uint Group, uint User) tuple)
		=> uid.Group == tuple.Group && uid.User == tuple.User;
	public static bool operator !=(Uid uid, (uint Group, uint User) tuple) => !(uid == tuple);
}
