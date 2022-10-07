using Konata.Core.Events.Model;
using System.Threading.Tasks;
using Tra.Lapluma.Core.Tasks.UserTasks.TaskPackages;

namespace Tra.Lapluma.Core.Tasks.UserTasks;
public abstract class MultiPlayerTask<TPackage> : LoopTask<TPackage>
	where TPackage : MultiPlayersPackage, new()
{
	public MultiPlayerTask(Bot bot, string actRgx, string name, string summary, string help, bool groupDefaultEnable) :
		base(bot, actRgx, name, summary, help, false, groupDefaultEnable)
	{ }

	protected override sealed Task<bool> OnAwakeAsync(FriendMessageEvent ev, TPackage pkg) => Task.FromResult(false);

	// protected override abstract Task<bool> OnAwakeAsync(GroupMessageEvent ev, TPackage pkg) => Implement by derived class

	protected override sealed Task<bool> OnConfirmAsync(FriendMessageEvent ev, TPackage pkg) => Task.FromResult(false);

	protected override sealed async Task<bool> OnConfirmAsync(GroupMessageEvent ev, TPackage pkg)
		=> await OnPlayerJoinAsync(ev, pkg)
		|| await OnGameCancelAsync(ev, pkg);

	protected override sealed Task<bool> OnLoopAsync(FriendMessageEvent ev, TPackage pkg) => Task.FromResult(false);

	protected override sealed async Task<bool> OnLoopAsync(GroupMessageEvent ev, TPackage pkg)
		=> await OnGameRunningAsync(ev, pkg)
		|| await OnPlayerGiveUpAsync(ev, pkg)
		|| await OnPlayerDrawAsync(ev, pkg);

	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Awake"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnGameCancelAsync(GroupMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Processing"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnGameRunningAsync(GroupMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Processing"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnPlayerDrawAsync(GroupMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Processing"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnPlayerGiveUpAsync(GroupMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Awake"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnPlayerJoinAsync(GroupMessageEvent ev, TPackage pkg);
}
