using Konata.Core.Events.Model;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tra.Lapluma.Core.Tasks.UserTasks.Packages;
using Tra.Lapluma.Core.Utilities;

namespace Tra.Lapluma.Core.Tasks.UserTasks.Models;
internal sealed class SolveHikariQf : LoopTask<SolveHikariQf.Package>
{
	public SolveHikariQf(Bot bot) : base(bot,
		actRgx: "solve_qf",
		name: nameof(SolveHikariQf),
		summary: "看羽毛笔如何爆杀光光（不是",
		help: "solve_qf [<maxp>]\n" +
		"# <maxp> => 这是光光的参数\n" +
		"停 => 停止答题",
		friendDefaultEnable: false,
		groupDefaultEnable: false)
	{ }

	private const uint Hikari = 3378448768;

	protected override Task<bool> OnAwakeAsync(FriendMessageEvent ev, Package pkg) => Task.FromResult(false);

	protected override async Task<bool> OnAwakeAsync(GroupMessageEvent ev, Package pkg)
	{
		var match = ev.Chain.ToString().ToLower().MatchActRegex("solve_qf(?: (.+))?");
		if (!match.Success) return false;
		const int MAXP = 1;

		// Awakened
		if (pkg.State != BasePackage.StateType.Asleep) {
			await _bot.SendGroupMessageAsync(ev, "在解了在解了");
			return true;
		}

		var maxpstr = match.Groups[MAXP].Value;
		if (maxpstr == "") {
			pkg.Prepare(ev.MemberUin, 7);
			await _bot.SendGroupMessageAsync(ev, "/qf");
		}
		else {
			if (!int.TryParse(maxpstr, out var maxp) || (maxp is < 100 and >= 5))
				return false;

			pkg.Prepare(ev.MemberUin, maxp);
			await _bot.SendGroupMessageAsync(ev, $"/qf {maxp}");
		}
		pkg.State = BasePackage.StateType.Awake;
		pkg.StartTimer(10,
			() => _bot.SendGroupMessageAsync(ev, $"嗯...光光不在吗？"));
		return true;
	}

	protected override Task<bool> OnConfirmAsync(FriendMessageEvent ev, Package pkg) => Task.FromResult(false);

	protected override Task<bool> OnConfirmAsync(GroupMessageEvent ev, Package pkg)
	{
		if (ev.MemberUin == Hikari
			&& ev.Chain.ToString().Contains("请准备好")) {
			pkg.State = BasePackage.StateType.Processing;
			pkg.StartTimer(10, () => _bot.SendGroupMessageAsync(ev, "光光呢？"));
			return Task.FromResult(true);
		}
		return Task.FromResult(false);
	}

	protected override Task<bool> OnLoopAsync(FriendMessageEvent ev, Package pkg) => Task.FromResult(false);

	protected override async Task<bool> OnLoopAsync(GroupMessageEvent ev, Package pkg)
	{
		return await Process()
			|| await UserCommand()
			|| await GameTerminate();

		async Task<bool> Process()
		{
			var match = Regex.Match(ev.Chain.ToString(), @"^Q(?:.+): (.+)");
			if (!match.Success) return false;
			if (ev.MemberUin != Hikari) {
				await _bot.SendGroupMessageAsync(ev, Message
					.Reply(ev.Message)
					.Text("你在干什么（盯——）"));
				return true;
			}

			if (!long.TryParse(match.Groups[1].Value, out var num)) {
				await _bot.SendGroupMessageAsync(ev, "¿");
				pkg.State = BasePackage.StateType.Terminated;
				return true;
			}

			await Task.Delay(Util.RandomInt(200, 3000));

			var result = string.Join(' ', Factorize(num, pkg.PrimeFactors));
			await _bot.SendGroupMessageAsync(ev, result);
			pkg.ResetTimer();
			return true;
		}

		async Task<bool> UserCommand()
		{
			if (ev.Chain.ToString() == "停") {
				pkg.State = BasePackage.StateType.Terminated;
				await _bot.SendGroupMessageAsync(ev, "行吧，那不玩了");
				return true;
			}
			else return false;
		}

		async Task<bool> GameTerminate()
		{
			if (ev.MemberUin != Hikari) return false;

			var msg = ev.Chain.ToString();
			if (msg.Contains("最终分数"))
				pkg.State = BasePackage.StateType.Terminated;
			else if (msg.Contains("开挂石锤")) {
				pkg.State = BasePackage.StateType.Terminated;
				await _bot.SendGroupMessageAsync(ev, "Bot的事，怎么能叫挂呢");
			}
			else return false;
			return true;
		}
	}

	private static List<int> Factorize(long num, int[] primes)
	{
		List<int> result = new();
		int i = 0;
		while (num != 1) {
			int f = primes[i];
			if (num % f == 0) {
				num %= f;
				result.Add(f);
			}
			else i++;
		}
		return result;
	}

	internal sealed class Package : TimerPackage
	{
		public uint Caller { get; private set; }

		public int[] PrimeFactors { get; private set; }

		public Package()
		{
			Caller = 0;
			PrimeFactors = Array.Empty<int>();
		}

		public void Prepare(uint caller, int maxFactor)
		{
			Caller = caller;

			List<int> primes = new();
			for (int i = 2; i < maxFactor; i++)
				if (IsPrime(i)) primes.Add(i);
			PrimeFactors = primes.ToArray();

			static bool IsPrime(int num)
			{
				// if (num < 2) return false;
				for (int i = 2; i <= Math.Sqrt(num); i++)
					if (num % i == 0)
						return false;
				return true;
			}
		}

	}
}
