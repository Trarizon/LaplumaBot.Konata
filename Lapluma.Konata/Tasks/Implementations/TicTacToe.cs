using Konata.Core.Events.Model;
using Lapluma.Konata.Tasks.Implementations.UtilModels.TicTacToe;
using Lapluma.Konata.Utilities;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Model = Lapluma.Konata.Tasks.Implementations.UtilModels.TicTacToe;

namespace Lapluma.Konata.Tasks.Implementations;
internal sealed class TicTacToe : MultiPlayersTask<TicTacToe.Package>
{
	public TicTacToe() : base(
		name: nameof(TicTacToe),
		summary: "井字棋|tictactoe",
		help: "井字棋|tictactoe => 开启一局井字棋\n" +
		"取消|结束 => 关闭游戏" +
		"投降|认输|请求和局 => 字面意思" +
		"回复1加入游戏\n" +
		"回复对应坐标(如a3)下棋",
		cmdRgx: "井字棋|tictactoe")
	{ }

	public class Package : MultiPlayerPackage
	{
		public uint PlayerX
		{
			get => Players[0];
			set => Players[0] = value;
		}

		public uint PlayerO
		{
			get => Players[1];
			set => Players[1] = value;
		}

		public Model.Board Board { get; }

		public Model.StateType CheckmateState => Board.Checkmate.Absolute();

		public Package() : base(2)
		{
			Board = new();
		}

		public bool PlacePiece(int col, int row)
		{
			if (Board.Checkmate != Model.StateType.Unfinished)
				throw new InvalidOperationException("Game has over");
			if (Board[col, row] != Model.Piece.N)
				return false;

			Board[col, row] = _currentPlayerIndex == 0 ? Model.Piece.X : Model.Piece.O;
			_currentPlayerIndex ^= 1;
			return true;
		}
	}

	private static readonly Random random = new();

	protected override async Task<bool> OnAwakeAsync(GroupMessageEvent e, Package pkg)
	{
		if (!Regex.IsMatch(e.Chain.ToString().ToLower(),
			@"^井字棋|tictactoe$"))
			return false;

		switch (pkg.State) {
			case BasePackage.StateType.Asleep:
				pkg = new() { PlayerX = e.MemberUin };
				await Lapluma.SendGroupMessageAsync(e, Message
					.At(pkg.PlayerX)
					.Text("开启了一局井字棋，有人参加吗？"));
				pkg.State = BasePackage.StateType.Awake;
				pkg.WaitTimeout(60, () =>
					Lapluma.SendGroupMessageAsync(e, "唔，没有人玩呢，那我们下次再说的吧"));
				return true;
			case BasePackage.StateType.Awake:
				await Lapluma.SendGroupMessageAsync(e, Message
					.Reply(e.Message)
					.At(pkg.PlayerX)
					.Text("已经开始了一局游戏，回复1即可加入"));
				return true;
			case BasePackage.StateType.Running:
				await Lapluma.SendGroupMessageAsync(e, Message
					.Reply(e.Message)
					.Text("有一局游戏正在进行中，稍等哦"));
				return true;
			default: throw new InvalidOperationException();
		}

	}

	protected override async Task<bool> OnGameCancelAsync(GroupMessageEvent e, Package pkg)
	{
		if (pkg.State == BasePackage.StateType.Awake &&
			e.Message.ToString() is "取消" or "结束") {
			await Lapluma.SendGroupMessageAsync(e, "游戏已结束");
			pkg.State = BasePackage.StateType.Terminated;
			return true;
		}
		return false;
	}

	protected override async Task<bool> OnGameReadyAsync(GroupMessageEvent e, Package pkg)
	{
		if (pkg.State == BasePackage.StateType.Awake &&
			e.Message.ToString() == "1") {
			pkg.PlayerO = e.MemberUin;
			// Random first move
			if (random.NextDouble() < 0.5)
				(pkg.PlayerO, pkg.PlayerX) = (pkg.PlayerX, pkg.PlayerO);

			await Lapluma.SendGroupMessageAsync(e, Message
				.Text("游戏开始")
				.At(pkg.PlayerX).Text("执X先手，")
				.At(pkg.PlayerO).Text("执O。棋面如下\n")
				.Image(pkg.Board.Image));
			pkg.State = BasePackage.StateType.Running;
			pkg.WaitTimeout(10 * 60, () => Lapluma.SendGroupMessageAsync(e, "有人放鸽子了"));
			return true;
		}
		return false;
	}

	protected override async Task<bool> OnGameRunningAsync(GroupMessageEvent e, Package pkg)
	{
		string input = e.Chain.ToString().ToLower();
		if (e.MemberUin == pkg.CurrentPlayer &&
			Regex.IsMatch(input, "^[a-c][1-3]$")) {
			// Reject Draw
			pkg.GameState = (MultiPlayerPackage.GameStateType.Gaming, 0);

			if (pkg.PlacePiece(input[0] - 'a', input[1] - '1')) {
				switch (pkg.CheckmateState) {
					case Model.StateType.Unfinished:
						await Lapluma.SendGroupMessageAsync(e, Message
							.Text("接下来请").At(pkg.CurrentPlayer).Text("下子。当前局面如下\n")
							.Image(pkg.Board.Image));
						pkg.ResetTimer();
						break;

					case Model.StateType.Draw:
						await Lapluma.SendGroupMessageAsync(e, Message
							.Text("双方平手，最终局面如下\n")
							.Image(pkg.Board.Image));
						pkg.State = BasePackage.StateType.Terminated;
						break;

					default:
						await Lapluma.SendGroupMessageAsync(e, Message
							.At(pkg.LastPlayer).Text("获胜，最终局面如下\n")
							.Image(pkg.Board.Image));
						pkg.State = BasePackage.StateType.Terminated;
						break;
				}
			}
			else
				await Lapluma.SendGroupMessageAsync(e, "不能在这里下子");
			return true;
		}
		return false;
	}

	protected override async Task<bool> OnRequestDrawAsync(GroupMessageEvent e, Package pkg)
	{
		var iPlayer = pkg.IsPlayer(e.MemberUin);
		if (iPlayer == -1) return false;

		var chainstr = e.Chain.ToString();

		if (chainstr == "请求和局") {
			pkg.GameState = (MultiPlayerPackage.GameStateType.AskingDraw, iPlayer);
			await Lapluma.SendGroupMessageAsync(e, Message
				.At(pkg.Players[iPlayer]).Text("发起了和局请求，回复“和局”同意请求。"));
		}

		else if (pkg.GameState.State == MultiPlayerPackage.GameStateType.AskingDraw &&
			e.MemberUin == pkg.Players[1 - iPlayer] &&
			chainstr == "和局") {
			await Lapluma.SendGroupMessageAsync(e, Message
				.Text("和局，游戏结束，局面如下\n")
				.Image(pkg.Board.Image));
			pkg.State = BasePackage.StateType.Terminated;
		}
		else return false;
		return true;
	}

	protected override async Task<bool> OnRequestGiveUpAsync(GroupMessageEvent e, Package pkg)
	{
		var iPlayer = pkg.IsPlayer(e.MemberUin);
		if (iPlayer == -1) return false;

		var chainstr = e.Chain.ToString();
		if (chainstr is "投降" or "认输") {
			await Lapluma.SendGroupMessageAsync(e, Message
				.Text("由于").At(pkg.Players[iPlayer]).Text("认输")
				.At(pkg.Players[1 - iPlayer]).Text("获胜，游戏结束，棋面如下")
				.Image(pkg.Board.Image));
			pkg.State = BasePackage.StateType.Terminated;
			return true;
		}
		return false;
	}
}
