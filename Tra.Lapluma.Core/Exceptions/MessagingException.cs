using Konata.Core.Message;
using System;

namespace Tra.Lapluma.Core.Exceptions;
internal class MessagingException : Exception
{
	public MessagingException(uint uin, bool toGroup, string message) :
		base($"Send message to {(toGroup ? "Group" : "Friend")} {uin} failed.\n" +
			$"Message content:\n" + message)
	{ }

	public MessagingException(uint uin, bool toGroup, MessageBuilder message) :
		this(uin, toGroup, message.Build().ToString())
	{ }
}
