using System;

namespace CertTool
{
	public static class Log
	{
		public static void Message(string m)
		{
			Console.WriteLine(m);
		}

		public static void Debug(string m)
		{
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine("D: "+m);
			Console.ResetColor();
		}

		public static void Warn(string m)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Error.WriteLine("W: "+m);
			Console.ResetColor();
		}

		public static void Error(string m)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Error.WriteLine("E: "+m);
			Console.ResetColor();
		}
	}
}