using System;
using System.Timers;
using Lapluma.Konata.Tasks.Interfaces;

namespace Lapluma.Konata.Tasks;
internal class LoopPackage : BasePackage, ITimingPackage
{
	protected Timer _timer;
	private Action _elapsedAction = () => { };

	public LoopPackage()
	{
		_timer = new() { AutoReset = false };
		_timer.Elapsed += (_, _) => _elapsedAction();
		PackageClose += () => _timer.Stop();
	}

	public void ResetTimer()
	{
		_timer.Stop();
		_timer.Start();
	}

	public void StopTimer()
	{
		_timer.Stop();
	}

	public void WaitTimeout(double time_s, Action elapsedAction)
	{
		_timer.Interval = time_s * 1000;
		_elapsedAction = () => { elapsedAction(); State = StateType.Terminated; };
		_timer.Start();
	}
}
