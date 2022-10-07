using System;

namespace Tra.Lapluma.Core.Tasks.UserTasks.TaskPackages;
public abstract class BasePackage
{
    public enum StateType { Asleep, Awake, Processing, Terminated }

    private StateType _state;

    public StateType State
    {
        get => _state;
        set
        {
            if (_state == StateType.Terminated) return;
            _state = value;
            if (_state == StateType.Terminated)
                PackageClose?.Invoke(this);
        }
    }

    protected event Action<BasePackage>? PackageClose;
}
