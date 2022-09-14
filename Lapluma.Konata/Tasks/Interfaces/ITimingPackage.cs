using System;

namespace Lapluma.Konata.Tasks.Interfaces;
internal interface ITimingPackage
{
    void ResetTimer();
    void StopTimer();
    void WaitTimeout(double timeout_s, Action elapsedAction);
}
