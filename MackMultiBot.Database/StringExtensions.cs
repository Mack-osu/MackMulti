using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class StringExtensions
{
	public static string ToIrcNameFormat(this string str)
	{
		return str.Replace(' ', '_');
	}
}
