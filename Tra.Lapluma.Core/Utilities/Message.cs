using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using System.Threading.Tasks;

namespace Tra.Lapluma.Core.Utilities;
internal static class Message
{
	public static MessageBuilder At(uint uin) => new(AtChain.Create(uin));
	public static MessageBuilder Text(string message) => new(message);
	public static MessageBuilder Image(byte[] image) => new(ImageChain.Create(image));
	public static MessageBuilder Reply(MessageStruct replyMessage) => new(ReplyChain.Create(replyMessage));


	public delegate Task Sender(MessageBuilder message);

	public static Sender SenderToFriend(this Bot bot, FriendMessageEvent ev) => msg => bot.SendFriendMessageAsync(ev, msg);
	public static Sender SenderToGroup(this Bot bot, GroupMessageEvent ev) => msg => bot.SendGroupMessageAsync(ev, msg);
	public static Sender SenderToFriend(this Bot bot, uint uin) => msg => bot.SendFriendMessageAsync(uin, msg);
	public static Sender SenderToGroup(this Bot bot, uint uin) => msg => bot.SendGroupMessageAsync(uin, msg);
}
