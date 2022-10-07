using Konata.Core.Common;
using Konata.Core.Events.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tra.Lapluma.Core.Utilities;
using Tra.Lapluma.Core.Utilities.Model;

namespace Tra.Lapluma.Core.Tasks.UserTasks;
public abstract class UserTask : BaseTask
{
	// Enable/disable to specific friend/group
	private readonly AuthorizeList<uint> _enabledFriends;
	private readonly AuthorizeList<uint> _enabledGroups;

	// Global control
	public bool FriendActive { get; set; } = true;
	public bool GroupActive { get; set; } = true;

	public UserTask(Bot bot,
		string actRgx, string name, string summary, string help,
		bool friendDefaultEnable, bool groupDefaultEnable) :
		base(bot, actRgx, name, summary, help)
	{
		_enabledFriends = new(friendDefaultEnable);
		_enabledGroups = new(groupDefaultEnable);
	}

	#region SpCommand
	protected override string? OnSpCommand(uint friendUin, string paramLower)
	{
		var parameters = paramLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parameters.Length <= 0) return null;

		return parameters[0] switch
		{
			"on" or "启用" => EnableDisable(true),
			"off" or "禁用" => EnableDisable(false),
			_ => null,
		};

		string EnableDisable(bool enable)
		{
			switch (parameters.Length) {
				case 1: // Global
					FriendActive = GroupActive = enable;
					return $"{Name}已全局启用";
				case 2: // Single Active
					switch (parameters[1]) {
						case "-g":
							GroupActive = enable;
							return $"群聊{Name}已全局启用";
						case "-f":
							FriendActive = enable;
							return $"私聊{Name}已全局启用";
						default:
							return $"未识别{parameters[1]}";
					}
				default: // Designate Enable
					var uins = from uinstr in parameters
							   let uin = SearchUin(uinstr)
							   where uin != 0
							   select uin;
					return parameters[1] switch
					{
						"-g" => DesignateSwitch(_enabledGroups, parameters.Skip(2), enable, "群聊" + Name),
						"-f" => DesignateSwitch(_enabledFriends, parameters.Skip(2), enable, "私聊" + Name),
						_ => $"未识别{parameters[1]}",
					};
			}

			static string DesignateSwitch(AuthorizeList<uint> list, IEnumerable<string> uinstrs, bool enable, string task)
			{
				List<uint> uins = new(), invalid = new();
				foreach (var uinstr in uinstrs) {
					var uin = SearchUin(uinstr);
					(uin == 0 ? invalid : uins).Add(uin);
				}
				if (enable)
					foreach (var uin in uins) list.Enable(uin);
				else
					foreach (var uin in uins) list.Disable(uin);

				return new StringBuilder($"已{(enable ? "启用" : "禁用")}{task}于")
					.AppendJoin(',', uinstrs)
					.Append("\n以下号码未识别:")
					.AppendJoin(',', invalid)
					.ToString();
			}
		}
	}

	protected override string? OnSpCommand(uint member, uint group, string paramLower)
	{
		var parameters = paramLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parameters.Length <= 0) return null;

		switch (parameters[0]) {
			case "on":
			case "启用":
				_enabledGroups.Enable(group);
				return $"已启用{Name}";
			case "off":
			case "禁用":
				_enabledGroups.Disable(group);
				return $"已禁用{Name}";
			default:
				return null;
		}
	}
	#endregion

	#region Authorization
	internal bool Allow(Uid uid)
		=> uid.Group == 0
		? _enabledFriends.Allow(uid.User)
		: _enabledGroups.Allow(uid.Group);

	protected override bool CanActivate(FriendMessageEvent ev)
		=> ev.FriendUin == _bot.Doctor // Always available to dr.
		|| (FriendActive && _enabledFriends.Allow(ev.FriendUin));

	protected override bool CanActivate(GroupMessageEvent ev)
		=> GroupActive && _enabledGroups.Allow(ev.GroupUin);

	protected override bool CanSpCommand(GroupMessageEvent ev)
		=> GroupActive && _bot.CheckAuthorizationAsync(ev.MemberUin, ev.GroupUin, RoleType.Admin).AwaitSync();
	#endregion

	#region DataCache
	protected event Action<UserTask>? Loading;
	protected event Action<UserTask>? Saving;

	private const string AUTHORIZE_LIST_FILE = "AuthorizeList.lpm";
	public override void Load()
	{
		string file = GetTaskDir(this) + AUTHORIZE_LIST_FILE;
		if (!File.Exists(file)) return;

		using var sr = new StreamReader(file);
		HashSet<uint> fset = new(), gset = new();
		var curList = fset;
		while (!sr.EndOfStream) {
			var line = sr.ReadLine();
			if (line == null) {
				Util.Output($"Exception when loading task except list of {Name}");
				continue;
			}
			if (line == "") {
				curList = gset;
				continue;
			}

			if (uint.TryParse(line, out var uin))
				curList.Add(uin);
			else
				Util.Output($"Read {line} when loading task except list of {Name}");
		}
		_enabledFriends.ExceptList = fset;
		_enabledGroups.ExceptList = gset;

		Loading?.Invoke(this);
	}

	public override void Save()
	{
		if (_enabledFriends.ExceptList.Count == 0
			&& _enabledGroups.ExceptList.Count == 0)
			return;
		Directory.CreateDirectory(GetTaskDir(this));
		using var sw = File.CreateText(GetTaskDir(this) + AUTHORIZE_LIST_FILE);

		foreach (var uin in _enabledFriends.ExceptList)
			sw.WriteLine(uin);
		sw.WriteLine();
		foreach (var uin in _enabledGroups.ExceptList)
			sw.WriteLine(uin);

		Saving?.Invoke(this);
	}
	#endregion
}
