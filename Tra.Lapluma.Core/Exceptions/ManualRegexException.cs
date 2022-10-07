namespace Tra.Lapluma.Core.Exceptions;
internal class ManualRegexException : TaskException
{
	public ManualRegexException(string message, int index, string substring)
		: base($"{message}" +
			$"匹配失败于第{index}个字符，{substring}")
	{ }

	public ManualRegexException(string message)
	: base(message)
	{ }

}
