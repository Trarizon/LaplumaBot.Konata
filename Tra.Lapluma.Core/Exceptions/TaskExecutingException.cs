using System;
using Tra.Lapluma.Core.Tasks;

namespace Tra.Lapluma.Core.Exceptions;
/// <summary>
/// Need to tell sender but needn't detail
/// </summary>
internal class TaskExecutingException : Exception
{
	public TaskExecutingException(BaseTask task, Exception innerException) :
		base($"Exception in executing {task.Name}", innerException)
	{ }
}
