using Konata.Core.Events.Model;

namespace Lapluma.Konata.Utilities.Structrues;
public record class Uid
{
	public static Uid Zero { get; } = new(0, 0);

	public uint Group { get; init; }
	public uint User { get; init; }

	public bool IsFromGroup => Group != 0;

	public Uid(uint group, uint user)
	{
		Group = group;
		User = user;
	}

	public Uid(FriendMessageEvent e) : this(0, e.FriendUin) { }

	public Uid(GroupMessageEvent e) : this(e.GroupUin, e.MemberUin) { }
}
