using System;
using System.Collections.Generic;
using System.Text;

namespace Lapluma.Konata.Tasks;
internal abstract class BasePackage
{
	public enum StateType { Asleep, Awake, Running, Terminated }

	private StateType _state;

	public StateType State
	{
		get => _state;
		set {
			_state = value;
			if (_state == StateType.Terminated)
				PackageClose?.Invoke();
		}
	}

	protected event Action? PackageClose;
}
