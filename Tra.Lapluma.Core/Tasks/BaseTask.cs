using Konata.Core.Events.Model;
using System;
using System.Threading.Tasks;
using Tra.Lapluma.Core.Utilities;
using Tra.Lapluma.Core.Exceptions;

namespace Tra.Lapluma.Core.Tasks;
public abstract class BaseTask
{
	protected readonly Bot _bot;
	private readonly string _actRgx;

	public string Name { get; }
	public string Summary { get; }
	public string Help { get; }

	public BaseTask(Bot bot, string actRgx, string name, string summary, string help)
	{
		_bot = bot;
		_actRgx = actRgx;
		Name = name;
		Summary = summary;
		Help = help;
	}

	public Task<bool> ActivateAsync(FriendMessageEvent ev)
	{
		if (CanActivate(ev)) {
			var match = ev.Chain.ToString()
				.MatchActRegex($@"(?:{Name}|{_actRgx}):(.+)");
			try {
				return match.Success
					? SpCommandAsync(ev.FriendUin == _bot.Doctor, ev.FriendUin, match.Groups[1].Value.ToLower())
					: OnActivateAsync(ev);
			} catch (Exception ex) {
				throw new TaskExecutingException(this, ex);
			}
		}
		else return Task.FromResult(false);
	}

	public Task<bool> ActivateAsync(GroupMessageEvent ev)
	{
		// User
		if (CanActivate(ev)) {
			var match = ev.Chain.ToString()
				.MatchActRegex($"(?:{Name}|{_actRgx}):(.+)");
			try {
				return match.Success
					? SpCommandAsync(false, ev.MemberUin, ev.GroupUin, match.Groups[1].Value.ToLower())
					: OnActivateAsync(ev);
			} catch (Exception ex) {
				throw new TaskExecutingException(this, ex);
			}
		}
		// Admin
		else if (CanSpCommand(ev)) {
			var match = ev.Chain.ToString()
				.MatchActRegex($"(?:{Name}|{_actRgx}):(.+)");
			try {
				if (match.Success)
					return SpCommandAsync(true, ev.MemberUin, ev.GroupUin, match.Groups[1].Value.ToLower());
			} catch (Exception ex) {
				throw new TaskExecutingException(this, ex);
			}
		}
		return Task.FromResult(false);
	}

	public virtual void Load() { }
	public virtual void Save() { }

	#region Authorization
	// Authorization: Doctor
	protected virtual bool CanActivate(FriendMessageEvent ev)
		=> ev.FriendUin == _bot.Doctor;

	// Authorization: Doctor
	protected virtual bool CanActivate(GroupMessageEvent ev)
		=> ev.MemberUin == _bot.Doctor;

	protected virtual bool CanSpCommand(GroupMessageEvent ev)
		=> CanActivate(ev);
	#endregion

	#region SpCommand
	private async Task<bool> SpCommandAsync(bool sp, uint uin, string paramLower)
	{
		string? msg = GetInfo(paramLower);
		if (msg == null && sp)
			OnSpCommand(uin, paramLower);

		if (msg == null) return false;
		if (msg != string.Empty)
			await _bot.SendFriendMessageAsync(uin, msg);
		return true;
	}

	private async Task<bool> SpCommandAsync(bool sp, uint member, uint group, string paramLower)
	{
		string? msg = GetInfo(paramLower);
		if (msg == null && sp)
			msg = OnSpCommand(member, group, paramLower);

		if (msg == null) return false;
		if (msg != string.Empty)
			await _bot.SendGroupMessageAsync(group, msg);
		return true;
	}

	private string? GetInfo(string paramLower)
		=> paramLower switch
		{
			"name" => Name,
			"summary" => Summary,
			"help" => Help,
			_ => null,
		};
	#endregion

	/// <returns>
	/// <see langword="null"/> if not executed, 
	/// <see cref="string.Empty"/> if executed but send nothing.
	/// </returns>
	protected virtual string? OnSpCommand(uint friendUin, string paramLower) => null;
	/// <returns>
	/// <see langword="null"/> if not executed, 
	/// <see cref="string.Empty"/> if executed but send nothing.
	/// </returns>
	protected virtual string? OnSpCommand(uint member, uint group, string paramLower) => null;

	protected abstract Task<bool> OnActivateAsync(FriendMessageEvent ev);
	protected abstract Task<bool> OnActivateAsync(GroupMessageEvent ev);
}
