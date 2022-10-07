using Konata.Core.Events.Model;
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Tra.Lapluma.Core.Tasks.UserTasks.TaskPackages;
using Tra.Lapluma.Core.Utilities.Model;

namespace Tra.Lapluma.Core.Tasks.UserTasks;
/// <summary>
/// user: Call <br/>
/// bot:  Waiting <br/>
/// user: Give parameter <br/>
/// bot:  Execute <br/>
/// 效果为一人唤醒全群可用
/// </summary>
/// <remarks>
/// 即时触发的效果建议不使用Package
/// </remarks>
/// <typeparam name="TPackage"></typeparam>
public abstract class AwaitTask<TPackage> : UserTask
	where TPackage : TimerPackage, new()
{
	private readonly ConcurrentDictionary<Uid, TPackage> _runnings = new();

	protected AwaitTask(Bot bot, string actRgx, string name, string summary, string help, bool friendDefaultEnable, bool groupDefaultEnable) : 
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
				return await OnProcessAsync(ev, pkg)
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
				return await OnProcessAsync(ev, pkg)
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
	/// </remarks>
	protected abstract Task<bool> OnAwakeAsync(FriendMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Asleep"/><br/>
	/// <see cref="BasePackage.StateType.Awake"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnAwakeAsync(GroupMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Awake"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnProcessAsync(FriendMessageEvent ev, TPackage pkg);
	/// <remarks>
	/// Called on<br/>
	/// <see cref="BasePackage.StateType.Awake"/><br/>
	/// </remarks>
	protected abstract Task<bool> OnProcessAsync(GroupMessageEvent ev, TPackage pkg);

}
