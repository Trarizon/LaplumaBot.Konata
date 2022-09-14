using System;
using System.Collections.Generic;
using System.Text;

namespace Lapluma.Konata.Tasks;
internal class MultiPlayerPackage : LoopPackage
{
	public enum GameStateType { Gaming, AskingDraw }

	public (GameStateType State, int PlayerIndex) GameState { get; set; }

	protected int _currentPlayerIndex = 0;

	public uint[] Players { get; }
	public uint CurrentPlayer => Players[_currentPlayerIndex];
	public uint LastPlayer => Players[(_currentPlayerIndex - 1 + Players.Length) % Players.Length];

	public MultiPlayerPackage()
	{
		Players = Array.Empty<uint>();
	}

	public MultiPlayerPackage(int maxPlayersCount)
	{
		Players = new uint[maxPlayersCount];
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="uin"></param>
	/// <returns>index if found, -1 if not</returns>
	public int IsPlayer(uint uin)
	{
		for (int i = 0; i < Players.Length; i++)
			if (Players[i] == uin) return i;
		return -1;
	}

	public void NextTurn() => _currentPlayerIndex = (_currentPlayerIndex + 1) % Players.Length;
}
