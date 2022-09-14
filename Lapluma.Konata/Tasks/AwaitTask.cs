using Konata.Core.Events.Model;
using Lapluma.Konata.Utilities.Structrues;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Lapluma.Konata.Tasks;
internal abstract class AwaitTask<TPackage> : BaseTask where TPackage : AwaitPackage, new()
{
	/// <summary>
	/// Do not change this field
	/// </summary>
	private static readonly TPackage _placeHolderPackage = new();
	private readonly ConcurrentDictionary<Uid, TPackage> _runnings = new();

	public AwaitTask(string name, string summary, string help, string cmdRgx,
		[Optional] (bool byBlocking, FriendRange permission) friendCfg,
		[Optional] (bool byBlocking, GroupRange permmision, OperatorRange operate) groupCfg) :
		base(name, summary, help, cmdRgx, friendCfg, groupCfg)
	{ }

	protected override sealed async Task<bool> ExecuteAsync(FriendMessageEvent e)
	{
		Uid uid = new(e);
		TPackage pkg = _runnings.GetOrAdd(uid, _placeHolderPackage);

		switch (pkg.State) {
			case BasePackage.StateType.Asleep:
				return await OnAwakeAsync(e, pkg);
			case BasePackage.StateType.Awake:
				return await OnProcessAsync(e, pkg)
					|| await OnAwakeAsync(e, pkg);
			case BasePackage.StateType.Terminated:
				pkg = _placeHolderPackage;
				goto case BasePackage.StateType.Asleep;
			default:
				throw new InvalidOperationException();
		}
	}

	protected override sealed async Task<bool> ExecuteAsync(GroupMessageEvent e)
	{
		Uid uid = new(e.GroupUin, 0);
		TPackage pkg = _runnings.GetOrAdd(uid, _placeHolderPackage);

		switch (pkg.State) {
			case BasePackage.StateType.Asleep:
				return await OnAwakeAsync(e, pkg);
			case BasePackage.StateType.Awake:
				return await OnProcessAsync(e, pkg)
					|| await OnAwakeAsync(e, pkg);
			case BasePackage.StateType.Terminated:
				pkg = _placeHolderPackage;
				goto case BasePackage.StateType.Asleep;
			default:
				throw new InvalidOperationException();
		}
	}

	protected abstract Task<bool> OnAwakeAsync(FriendMessageEvent e, TPackage pkg);
	protected abstract Task<bool> OnAwakeAsync(GroupMessageEvent e, TPackage pkg);
	protected abstract Task<bool> OnProcessAsync(FriendMessageEvent e, TPackage pkg);
	protected abstract Task<bool> OnProcessAsync(GroupMessageEvent e, TPackage pkg);
}
