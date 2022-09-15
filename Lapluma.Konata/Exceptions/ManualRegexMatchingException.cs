using System;
using System.Collections.Generic;
using System.Text;

namespace Lapluma.Konata.Exceptions;
public class ManualRegexMatchingException : Exception
{
	public ManualRegexMatchingException(string message, int index, string substring)
		: base($"{message}" +
			$"匹配失败于第{index}个字符，{substring}")
	{ }
}
