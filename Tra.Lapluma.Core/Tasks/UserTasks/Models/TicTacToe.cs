using Konata.Core.Events.Model;
using Konata.Core.Message.Model;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tra.Lapluma.Core.Tasks.UserTasks.Models.Utils.TicTacToe;
using Tra.Lapluma.Core.Tasks.UserTasks.TaskPackages;
using Tra.Lapluma.Core.Utilities;
using static Konata.Core.Message.BaseChain.ChainType;

namespace Tra.Lapluma.Core.Tasks.UserTasks.Models;
internal sealed class TicTacToe : MultiPlayerTask<TicTacToe.Package>
{
	public TicTacToe(Bot bot) : base(bot,
		actRgx: "井字棋|tictactoe",
		name: nameof(TicTacToe),
		summary: "简单的井字棋游戏（其实是写来测试用的",
		help: "井字棋|tictactoe [@somebody]\n" +
		"# @somebody => 指定对战玩家" +
		"游戏开启后发送1即可加入游戏\n" +
		"发送对应坐标(如a3下棋)" +
		"取消|结束 => 在游戏正式开始前关闭游戏\n" +
		"投降|认输|请求和局 => 字面意思\n",
		groupDefaultEnable: false)
	{ }

	protected override async Task<bool> OnAwakeAsync(GroupMessageEvent ev, Package pkg)
	{
		if (ev.Chain.ToString().Trim().IsMatchActRegex("井字棋|tictactoe(.*)", RegexOptions.IgnoreCase) != true)
			return false;

		var ats = ev.Chain[At];
		switch (ats.Count) {
			case 0: return await AnyBodyComeOn();
			case 1: return await YouWhoIDesignate(((AtChain)ats[0]).AtUin);
			default:
				await _bot.SendGroupMessageAsync(ev, Message
					.Reply(ev.Message)
					.Text("您好，这是双人游戏呢"));
				return true;
		}

		async Task<bool> AnyBodyComeOn()
		{
			switch (pkg.State) {
				case BasePackage.StateType.Asleep:
					pkg.Player1 = ev.MemberUin;
					await _bot.SendGroupMessageAsync(ev, Message
						.At(pkg.Player1)
						.Text("开启了一局井字棋，有人参加吗？"));
					pkg.State = BasePackage.StateType.Awake;
#if DEBUG
					pkg.StartTimer(300,
#else
					pkg.StartTimer(60,
#endif
						() => _bot.SendGroupMessageAsync(ev, "唔，没有人玩呢，那我们下次再说的吧"),false);
					return true;

				case BasePackage.StateType.Awake:
					await _bot.SendGroupMessageAsync(ev, Message
						.Reply(ev.Message)
						.At(pkg.Player1)
						.Text("已经开始了一局游戏，发送1即可加入"));
					return true;

				case BasePackage.StateType.Processing:
					await _bot.SendGroupMessageAsync(ev, Message
						.Reply(ev.Message)
						.Text("有一局游戏正在进行中，稍等哦"));
					return true;
				default:
					throw new InvalidOperationException();
			}
		}

		async Task<bool> YouWhoIDesignate(uint atUin)
		{
			switch (pkg.State) {
				case BasePackage.StateType.Asleep:
					pkg.Player1 = ev.MemberUin;
					pkg.Player2 = atUin;
					await _bot.SendGroupMessageAsync(ev, Message
						.At(atUin)
						.Text("一起玩井字棋吗？(同意|拒绝)"));
					pkg.State = BasePackage.StateType.Awake;
					pkg.DesignatedPlayer2 = true;
					pkg.StartTimer(30,
						() => _bot.SendGroupMessageAsync(ev, "啊啦，对方没理你呢"));
					return true;

				case BasePackage.StateType.Awake:
					await _bot.SendGroupMessageAsync(ev, Message
						.Reply(ev.Message)
						.Text("一局游戏正在等待对方回应，稍等哦"));
					return true;

				case BasePackage.StateType.Processing:
					await _bot.SendGroupMessageAsync(ev, Message
						.Reply(ev.Message)
						.Text("有一局游戏正在进行中，稍等哦"));
					return true;
				default:
					throw new InvalidOperationException();
			}
		}
	}

	protected override async Task<bool> OnGameCancelAsync(GroupMessageEvent ev, Package pkg)
	{
		if (ev.Chain.ToString() is "取消" or "结束") {
			await _bot.SendGroupMessageAsync(ev, "真可惜，那么有机会再玩吧");
			pkg.State = BasePackage.StateType.Terminated;
			return true;
		}
		return false;
	}

	protected override async Task<bool> OnGameRunningAsync(GroupMessageEvent ev, Package pkg)
	{
		string input = ev.Chain.ToString().ToLower();
		if (ev.MemberUin != pkg.CurrentPlayer
			|| !Regex.IsMatch(input, "^[a-c][1-3]$"))
			return false;

		// Decline Draw
		pkg.DeclineDraw();

		if (pkg.PlacePiece(input[0] - 'a', input[1] - '1')) {
			switch (pkg.Checkmate) {
				case CheckmateType.Unfinished:
					await _bot.SendGroupMessageAsync(ev, Message
						.Text("接下来请").At(pkg.CurrentPlayer)
						.Text("下子。当前局面:\n")
						.Image(pkg.Board.Image));
					pkg.ResetTimer();
					break;

				case CheckmateType.Draw:
					await _bot.SendGroupMessageAsync(ev, Message
						.Text("双方平手，最终局面如下\n")
						.Image(pkg.Board.Image));
					pkg.State = BasePackage.StateType.Terminated;
					break;

				default:
					await _bot.SendGroupMessageAsync(ev, Message
						.At(pkg.LastPlayer).Text("获胜，最终局面如下\n")
						.Image(pkg.Board.Image));
					pkg.State = BasePackage.StateType.Terminated;
					break;
			}
		}
		else
			await _bot.SendGroupMessageAsync(ev, "不能在这里下子");
		return true;
	}

	protected override async Task<bool> OnPlayerDrawAsync(GroupMessageEvent ev, Package pkg)
	{
		var iPlayer = pkg.IsPlayer(ev.MemberUin);
		if (iPlayer == -1) return false;

		var chainstr = ev.Chain.ToString();
		// Asking draw
		if (pkg.DrawAsker == 0 && chainstr is "请求和局") {
			pkg.AskingForDraw(ev.MemberUin);
			await _bot.SendGroupMessageAsync(ev, Message
				.At(ev.MemberUin).Text("发起了和局请求，是否同意？(同意|拒绝，继续下棋默认拒绝)"));
		}
		// Accept or decline draw
		else if (pkg.DrawAsker != 0 && ev.MemberUin != pkg.DrawAsker) {
			if (chainstr is "同意" or "请求和局") {
				await _bot.SendGroupMessageAsync(ev, Message
					.Text("和局，游戏结束，最终局面如下\n")
					.Image(pkg.Board.Image));
				pkg.State = BasePackage.StateType.Terminated;
			}
			else if (chainstr is "拒绝") {
				await _bot.SendGroupMessageAsync(ev, Message
					.Text("对方拒绝和棋，游戏继续")
					.Image(pkg.Board.Image));
			}
			else return false;
		}
		else return false;
		return true;
	}

	protected override async Task<bool> OnPlayerGiveUpAsync(GroupMessageEvent ev, Package pkg)
	{
		var iPlayer = pkg.IsPlayer(ev.MemberUin);
		if (iPlayer == -1) return false;

		var chainstr = ev.Chain.ToString();
		if (chainstr is "投降" or "认输") {
			await _bot.SendGroupMessageAsync(ev, Message
				.Text("由于对方认输，")
				.At(pkg.Players[1 ^ iPlayer]).Text("获胜，游戏结束，棋面如下")
				.Image(pkg.Board.Image));
			pkg.State = BasePackage.StateType.Terminated;
			return true;
		}
		return false;
	}

	protected override async Task<bool> OnPlayerJoinAsync(GroupMessageEvent ev, Package pkg)
	{
		if (pkg.DesignatedPlayer2 && ev.MemberUin == pkg.Player2) {
			if (ev.Chain.ToString() is "1" or "同意") {
				await GameStart();
				return true;
			}
			else if (ev.Chain.ToString() is "拒绝") {
				await _bot.SendGroupMessageAsync(ev, Message
					.At(pkg.Player1)
					.Text("对方拒绝了你的请求（笑"));
				pkg.State = BasePackage.StateType.Terminated;
				return true;
			}
		}
		else if (ev.Chain.ToString() is "1" && ev.MemberUin != pkg.Player1) {
			pkg.Player2 = ev.MemberUin;
			await GameStart();
			return true;
		}
		return false;

		async Task GameStart()
		{
			// Random first move
			if (Util.ProbabilisticAssert(0.5))
				(pkg.Player1, pkg.Player2) = (pkg.Player2, pkg.Player1);

			await _bot.SendGroupMessageAsync(ev, Message
				.Text("游戏开始")
				.At(pkg.Player1).Text("执X先手，")
				.At(pkg.Player2).Text("执O。棋面如下\n")
				.Image(pkg.Board.Image));
			pkg.State = BasePackage.StateType.Processing;
			pkg.StartTimer(300, () => _bot.SendGroupMessageAsync(ev, "不要下棋下到一半不见人影啊"));
		}
	}

	internal sealed class Package : TwoPlayerPackage
	{
		public bool DesignatedPlayer2 { get; set; } = false;

		public Board Board { get; }
		public CheckmateType Checkmate => Board.Checkmate.Absolute();

		public Package()
		{
			Board = new();
			PackageClose += pkg => ((Package)pkg).Board.Dispose();
		}

		/// <remarks>
		/// If placed successfully, the turn will change
		/// </remarks>
		public bool PlacePiece(int col, int row)
		{
			if (Board.Checkmate != CheckmateType.Unfinished)
				throw new InvalidOperationException("Game has over");

			if (Board.PlacePiece(col, row, _iCurrentPlayer == 0 ? Piece.X : Piece.O)) {
				NextTurn();
				return true;
			}
			else return false;
		}
	}
}
