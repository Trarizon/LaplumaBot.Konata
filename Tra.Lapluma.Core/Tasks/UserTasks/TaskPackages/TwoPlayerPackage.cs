namespace Tra.Lapluma.Core.Tasks.UserTasks.TaskPackages;
public class TwoPlayerPackage : MultiPlayersPackage
{
	public uint Player1
	{
		get => Players[0];
		set => Players[0] = value;
	}
	public uint Player2 {
		get => Players[1];
		set => Players[1] = value;
	}

	public uint OppositePlayer => Players[_iCurrentPlayer ^ 1];

	public TwoPlayerPackage()
	{
		Players.Add(0);
		Players.Add(0);
	}
}
