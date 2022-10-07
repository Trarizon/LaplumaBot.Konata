using Konata.Core.Message;
using System;

namespace Tra.Lapluma.Core.Exceptions;
/// <summary>
/// shouldn't happened exceptions to be reported to sender or group
/// </summary>
public class TaskException : Exception
{
	private readonly MessageBuilder _message;

	public TaskException(MessageBuilder message) => _message = new MessageBuilder("TaskEx:").Add(message.Build());
	public TaskException(string message) => _message = new MessageBuilder($"TaskEx:{message}");
	public TaskException() : this("Unknown") { }

	public override string Message => _message.Build().ToString();

	public MessageBuilder ReportMessage => _message;
}
