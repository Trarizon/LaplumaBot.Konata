using Konata.Core.Message;
using Konata.Core.Message.Model;

namespace Lapluma.Konata.Utilities;
internal static class Message
{
	public static MessageBuilder At(uint uin) => new(AtChain.Create(uin));
	public static MessageBuilder Text(string message) => new(message);
	public static MessageBuilder Image(byte[] image) => new(ImageChain.Create(image));
	public static MessageBuilder Reply(MessageStruct replyMessage) => new(ReplyChain.Create(replyMessage));

	public static class Chain
	{
		public static AtChain At(uint uin) => AtChain.Create(uin);
		public static TextChain Text(string message) => TextChain.Create(message);
		public static ImageChain Image(byte[] image) => ImageChain.Create(image);
		public static ReplyChain Reply(MessageStruct replyMessage) => ReplyChain.Create(replyMessage);
	}
}
