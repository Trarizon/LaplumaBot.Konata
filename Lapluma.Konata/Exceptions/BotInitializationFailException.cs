using System;

namespace Lapluma.Konata.Exceptions;
public class BotInitializationFailException : Exception
{
	public BotInitializationFailException(string message)
		: base($"BotInitialization failed.\n{message}") { }
}
