using Konata.Core.Message;
using System;

namespace Lapluma.Konata.Exceptions;
internal class SendMessageFailException : Exception
{
	public SendMessageFailException(uint uin, bool isGroup, string message)
		: base($"Send message to {(isGroup ? "Group" : "Friend")} {uin} failed.\n" +
			$"Message content:\n" + message)
	{ }

	public SendMessageFailException(uint uin, bool isGroup, MessageBuilder message)
		: this(uin, isGroup, message.Build().ToString())
	{ }
}
