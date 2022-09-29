using System;

namespace Lapluma.Konata.Exceptions;
public class TaskException : Exception
{
	public TaskException(string message) : base(message) { }
}
