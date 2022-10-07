using System;
using System.Threading.Tasks;
using Tra.Lapluma.Core;

namespace Tra.Lapluma.Test
{
	internal class Program
	{
		static async Task Main()
		{
			Bot lapluma = new();
			await lapluma.Login();
			while (true) {
				switch (Console.ReadLine()) {
					case "login":
						if (lapluma.IsOnline)
							await lapluma.Login();
						break;
					case "logout":
						if (lapluma.IsOnline)
							await lapluma.Logout();
						break;
					case "ret":
						if (lapluma.IsOnline)
							await lapluma.Logout();
						return;
					default:
						break;
				}
			}
		}
	}
}
