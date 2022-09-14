using Konata.Core.Events.Model;
using System.Threading.Tasks;
using Konata.Core.Message;

namespace Lapluma.Konata.Utilities.Structrues;
delegate Task MessageHandler<T>(T message);

internal static class MessageHandlers
{
	public static MessageHandler<MessageBuilder> SendFriendAsync(FriendMessageEvent e) => msg => Lapluma.SendFriendMessageAsync(e, msg);
	public static MessageHandler<MessageBuilder> SendGroupAsync(GroupMessageEvent e) => msg => Lapluma.SendGroupMessageAsync(e, msg);

	public static MessageHandler<string> SendFriendStringAsync(FriendMessageEvent e) => msg => Lapluma.SendFriendMessageAsync(e, msg);
	public static MessageHandler<string> SendGroupStringAsync(GroupMessageEvent e) => msg => Lapluma.SendGroupMessageAsync(e, msg);
}