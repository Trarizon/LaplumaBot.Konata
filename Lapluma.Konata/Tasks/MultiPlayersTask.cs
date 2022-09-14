using Konata.Core.Events.Model;
using System;
using System.Threading.Tasks;

namespace Lapluma.Konata.Tasks;
internal abstract class MultiPlayersTask<TPackage> : LoopTask<TPackage> where TPackage : MultiPlayerPackage, new()
{
	public MultiPlayersTask(string name, string summary, string help, string cmdRgx) :
		base(name, summary, help, cmdRgx,
			// friendCfg: (true, FriendRange.Banned), // Not Support Private
			groupCfg: (false, GroupRange.All, OperatorRange.Admin))
	{ }

	protected override sealed Task<bool> OnAwakeAsync(FriendMessageEvent e, TPackage pkg) => throw new NotSupportedException();

	//protected override Task<bool> OnAwakeAsync(GroupMessageEvent e, TPackage pkg)
	//{
	//	throw new NotImplementedException();
	//}

	protected override sealed Task<bool> OnConfirmAsync(FriendMessageEvent e, TPackage pkg) => throw new NotSupportedException();

	protected override sealed async Task<bool> OnConfirmAsync(GroupMessageEvent e, TPackage pkg)
		=> await OnGameReadyAsync(e, pkg)
		|| await OnGameCancelAsync(e, pkg);

	protected override sealed Task<bool> OnLoopAsync(FriendMessageEvent e, TPackage pkg) => throw new NotSupportedException();

	protected override sealed async Task<bool> OnLoopAsync(GroupMessageEvent e, TPackage pkg)
		=> await OnGameRunningAsync(e, pkg)
		|| await OnRequestGiveUpAsync(e, pkg)
		|| await OnRequestDrawAsync(e, pkg);

	protected abstract Task<bool> OnGameReadyAsync(GroupMessageEvent e, TPackage pkg);
	protected abstract Task<bool> OnGameCancelAsync(GroupMessageEvent e, TPackage pkg);
	protected abstract Task<bool> OnGameRunningAsync(GroupMessageEvent e, TPackage pkg);
	protected abstract Task<bool> OnRequestGiveUpAsync(GroupMessageEvent e, TPackage pkg);
	protected abstract Task<bool> OnRequestDrawAsync(GroupMessageEvent e, TPackage pkg);
}
