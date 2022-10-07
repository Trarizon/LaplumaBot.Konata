using System;
using System.Timers;

namespace Tra.Lapluma.Core.Tasks.UserTasks.TaskPackages;
public class TimerPackage : BasePackage
{
	private readonly Timer _timer;
	private Action? _elapsedAction;

	public TimerPackage()
	{
		_timer = new() { AutoReset = false };
		_timer.Elapsed += (_, _) => _elapsedAction?.Invoke();
		PackageClose += s =>
		{
			var pkg = (TimerPackage)s;
			pkg._timer.Stop(); 
			pkg._timer.Dispose();
		};
	}

	public void ResetTimer()
	{
		_timer.Stop();
		_timer.Start();
	}

	public void StopTimer()
		=> _timer.Stop();

	public void StartTimer(double interval_s, Action? elapsedAction = null, bool terminateAfterElapse = true)
	{
		if (terminateAfterElapse)
			_elapsedAction = () =>
			{
				elapsedAction?.Invoke();
				State = StateType.Terminated;
			};
		else
			_elapsedAction = elapsedAction;
		_timer.Interval = interval_s * 1000;
		_timer.Start();
	}
}
