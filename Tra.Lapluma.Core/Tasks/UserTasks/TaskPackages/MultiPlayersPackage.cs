using System.Collections.Generic;
// TODO: ignore it temporary
namespace Tra.Lapluma.Core.Tasks.UserTasks.TaskPackages;
public class MultiPlayersPackage : TimerPackage
{
	public uint DrawAsker { get; private set; }

	protected int _iCurrentPlayer = 0;

	public List<uint> Players { get; }
	public uint CurrentPlayer => Players[_iCurrentPlayer];
	public uint LastPlayer => Players[(_iCurrentPlayer - 1 + Players.Count) % Players.Count];
	public uint NextPlayer => Players[(_iCurrentPlayer + 1) % Players.Count];

	public MultiPlayersPackage()
	{
		Players = new List<uint>();
	}

	/// <summary>
	/// Check if a specific uin is player of current game
	/// </summary>
	/// <param name="uin"></param>
	/// <returns>index if is player, -1 if not</returns>
	public int IsPlayer(uint uin) => Players.IndexOf(uin);


	public void NextTurn()
	{
		_iCurrentPlayer = (_iCurrentPlayer + 1) % Players.Count;
		ResetTimer();
	}

	public void AskingForDraw(uint asker)
	{
		DrawAsker = asker;
	}

	public void DeclineDraw()
	{
		DrawAsker = 0;
	}
}
