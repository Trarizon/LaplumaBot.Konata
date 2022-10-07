using Konata.Core.Events.Model;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Tra.Lapluma.Core.Tasks.UserTasks.TaskPackages;
using Tra.Lapluma.Core.Utilities.Model;

namespace Tra.Lapluma.Core.Tasks.UserTasks;
public abstract class LoopTask<TPackage> : UserTask
	where TPackage : TimerPackage, new()
{
	private readonly ConcurrentDictionary<Uid, TPackage> _runnings = new();

	protected LoopTask(Bot bot, string actRgx, string name, string summary, string help, bool friendDefaultEnable, bool groupDefaultEnable) :
		base(bot, actRgx, name, summary, help, friendDefaultEnable, groupDefaultEnable)
	{ }

	protected override sealed async Task<bool> OnActivateAsync(FriendMessageEvent ev)
	{
		Uid uid = Uid.Of(ev);
		TPackage pkg = _runnings.GetOrAdd(uid, new TPackage());

		switch (pkg.State) {
			case BasePackage.StateType.Asleep:
				return await OnAwakeAsync(ev, pkg);
			case BasePackage.StateType.Awake:
				return await OnConfirmAsync(ev, pkg)
					|| await OnAwakeAsync(ev, pkg);
			case BasePackage.StateType.Processing:
				return await OnLoopAsync(ev, pkg)
					|| await OnAwakeAsync(ev, pkg);
			case BasePackage.StateType.Terminated:
				var newpkg = new TPackage();
				_runnings.TryUpdate(uid, newpkg, pkg);
				pkg = newpkg;
				goto case BasePackage.StateType.Asleep;
			default:
				throw new InvalidOperationException();
		}
	}

	protected override sealed async Task<bool> OnActivateAsync(GroupMessageEvent ev)
	{
		Uid uid = Uid.OfGroup(ev);
		TPackage pkg = _runnings.GetOrAdd(uid, new TPackage());

		switch (pkg.State) {
			case BasePackage.StateType.Asleep:
				return await OnAwakeAsync(ev, pkg);
			case BasePackage.StateType.Awake:
				return await OnConfirmAsync(ev, pkg)
					|| await OnAwakeAsync(ev, pkg);
			case BasePackage.StateType.Processing:
				return await OnLoopAsync(ev, pkg)
					|| await OnAwakeAsync(ev, pkg);
			case BasePackage.StateType.Terminated:
				var newpkg = new TPackage();
				_runnings.TryUpdate(uid, newpkg, pkg);
				pkg = newpkg;
				goto case BasePackage.StateType.Asleep;
			default:
				throw new InvalidOperationException();
		}
	}

	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Asleep"/><br/>
	/// <see cref="BasePackage.StateType.Awake"/><br/>
	/// <see cref="BasePackage.StateType.Processing"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnAwakeAsync(FriendMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Asleep"/><br/>
	/// <see cref="BasePackage.StateType.Awake"/><br/>
	/// <see cref="BasePackage.StateType.Processing"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnAwakeAsync(GroupMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Awake"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnConfirmAsync(FriendMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Awake"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnConfirmAsync(GroupMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Processing"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnLoopAsync(FriendMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Processing"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnLoopAsync(GroupMessageEvent ev, TPackage pkg);
}
