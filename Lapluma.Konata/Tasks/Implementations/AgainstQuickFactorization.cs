using Konata.Core.Events.Model;
using Lapluma.Konata.Utilities;
using Lapluma.Konata.Utilities.Structrues;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lapluma.Konata.Tasks.Implementations;
internal sealed class AgainstQuickFactorization : LoopTask<AgainstQuickFactorization.Package>
{
	public AgainstQuickFactorization() : base(
		name: nameof(AgainstQuickFactorization),
		summary: "against_qf [#maxp] => 某种程度上，我是Bot不是挂",
		help: "against_qf [#maxp] => 直到喊停或者游戏结束，我会一直答题\n" +
		"停 => 停止答题",
		cmdRgx: "against_qf",
		groupCfg: (false, GroupRange.All, OperatorRange.Admin))
	{ }

	public class Package : LoopPackage
	{
		public Uid CallerUid { get; }

		public int[] PrimeFactors { get; private set; } = new[] { 2, 3, 5 };

		public Package()
		{
			CallerUid = Uid.Zero;
			PrimeFactors = Array.Empty<int>();
		}

		public Package(Uid uid, int maxFactor)
		{
			CallerUid = uid;

			List<int> primes = new();
			for (int i = 2; i < maxFactor; i++)
				if (IsPrime(i)) primes.Add(i);
			PrimeFactors = primes.ToArray();

			static bool IsPrime(int num)
			{
				for (int i = 2; i < num / 2; i++)
					if (num % i == 0) return false;
				return true;
			}
		}
	}

	private const uint Hikari = 3378448768;
	private static readonly Random random = new();

	protected override Task<bool> OnAwakeAsync(FriendMessageEvent e, Package pkg) => throw new NotSupportedException();

	protected override async Task<bool> OnAwakeAsync(GroupMessageEvent e, Package pkg)
	{
		var match = Regex.Match(e.Chain.ToString().ToLower(), @"^against_qf(?: (.+))?$");
		if (!match.Success) return false;
		#region Regex index
		const int PARAM = 1;
		#endregion
		// Awakened
		if (pkg.State != BasePackage.StateType.Asleep) {
			await Lapluma.SendGroupMessageAsync(e, "在解了在解了");
			return true;
		}

		var paramstr = match.Groups[PARAM].Value;
		if (paramstr == "") {
			pkg = new(new(e), 7);
			await Lapluma.SendGroupMessageAsync(e, "/qf");
		}
		else {
			if (!int.TryParse(paramstr, out var param))
				return false;

			pkg = new(new(e), param);
			await Lapluma.SendGroupMessageAsync(e, $"/qf {param}");
		}
		pkg.State = BasePackage.StateType.Awake;
		pkg.WaitTimeout(5, () => Lapluma.SendGroupMessageAsync(e, $"嗯...光光不在吗？"));
		return true;
	}

	protected override Task<bool> OnConfirmAsync(FriendMessageEvent e, Package pkg) => throw new NotSupportedException();

	protected override Task<bool> OnConfirmAsync(GroupMessageEvent e, Package pkg)
	{
		if (e.MemberUin == Hikari && e.Chain.ToString().Contains("请准备好")) {
			pkg.State = BasePackage.StateType.Running;
			pkg.WaitTimeout(5, () => Lapluma.SendGroupMessageAsync(e, "光光呢？"));
			return Task.FromResult(true);
		}
		return Task.FromResult(false);
	}

	protected override Task<bool> OnLoopAsync(FriendMessageEvent e, Package pkg) => throw new NotSupportedException();

	protected override async Task<bool> OnLoopAsync(GroupMessageEvent e, Package pkg)
	{
		return await Process()
			|| UserCommand()
			|| await Terminate();

		async Task<bool> Process()
		{
			var match = Regex.Match(e.Chain.ToString(), @"^Q(?:.+): (.+)$");
			if (!match.Success) return false;
			if (e.MemberUin != Hikari) {
				await Lapluma.SendGroupMessageAsync(e, Message
					.Reply(e.Message)
					.Text("你在干什么（盯——）"));
				return true;
			}

			if (!long.TryParse(match.Groups[1].Value, out var num)) {
				await Lapluma.SendGroupMessageAsync(e, "¿");
				pkg.State = BasePackage.StateType.Terminated;
				return true;
			}

			await Task.Delay(random.Next(200, 5000));

			var result = string.Join(' ', Factorize(num, pkg.PrimeFactors));
			await Lapluma.SendGroupMessageAsync(e, result);
			pkg.ResetTimer();
			return true;
		}

		bool UserCommand()
		{
			if (e.Chain.ToString() == "停") {
				pkg.State = BasePackage.StateType.Terminated;
				return true;
			}
			else return false;
		}

		async Task<bool> Terminate()
		{
			if (e.MemberUin != Hikari) return false;

			var msg = e.Chain.ToString();
			if (msg.Contains("最终分数"))
				pkg.State = BasePackage.StateType.Terminated;
			else if (msg.Contains("开挂石锤")) {
				pkg.State = BasePackage.StateType.Terminated;
				await Lapluma.SendGroupMessageAsync(e, "Bot的事，怎么能叫挂呢");
			}
			else return false;
			return true;
		}
	}

	private static int[] Factorize(long num, int[] primes)
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
		return result.ToArray();
	}
}
