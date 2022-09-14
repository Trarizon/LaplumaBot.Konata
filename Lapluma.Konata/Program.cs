using Konata.Core.Interfaces.Api;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lapluma.Konata
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			await Lapluma.Login();

			while (true) {
				switch (Console.ReadLine()) {
					case "login":
						if (!Lapluma.Bot.IsOnline()) await Lapluma.Login();
						break;
					case "logout":
						if (Lapluma.Bot.IsOnline()) await Lapluma.Logout();
						break;
					case "ret":
						await Lapluma.Logout();
						return;
				}
			}
		}
	}
}
