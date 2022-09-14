using Konata.Core.Common;
using Konata.Core.Events.Model;
using Lapluma.Konata.Utilities;
using Lapluma.Konata.Utilities.Structrues;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lapluma.Konata.Tasks;
public abstract class BaseTask
{
	#region 
	public enum FriendRange { Banned = 0, Doctor, All }
	public enum GroupRange { Banned = 0, Doctor, Owner, Admin, All }
	public enum OperatorRange { Doctor = 0, Owner, Admin }
	#endregion

	private readonly FriendRange _friendPerm;
	private readonly GroupRange _groupPerm;
	private readonly OperatorRange _operatePerm;

	private readonly bool _isFriendByBlocking;
	private readonly bool _isGroupByBlocking;
	private HashSet<uint> _friendExcepts = new();
	private HashSet<uint> _groupExcepts = new();
	private readonly object __lockObj_Excepts = new();

	private readonly string _cmdRgx;

	protected string InfoDir => Lapluma.CONFIG_DIR + $@"Task\{Name}\";

	public string Name { get; }
	public string Summary { get; }
	public string Help { get; }

	public bool FriendOn { get; private set; } = true;
	public bool GroupOn { get; private set; } = true;

	public bool FriendEnable => _friendPerm != FriendRange.Banned;
	public bool GroupEnable => _groupPerm != GroupRange.Banned;

	protected event Action? LoadEvent;
	protected event Action? SaveEvent;

	public BaseTask(string name, string summary, string help, string cmdRgx,
		[Optional] (bool byBlocking, FriendRange permission) friendCfg,
		[Optional] (bool byBlocking, GroupRange permmision, OperatorRange operate) groupCfg)
	{
		Name = name;
		Summary = summary;
		Help = help;
		_cmdRgx = cmdRgx;
		_friendPerm = friendCfg.permission;
		_groupPerm = groupCfg.permmision;
		_operatePerm = groupCfg.operate;
		_isFriendByBlocking = friendCfg.byBlocking;
		_isGroupByBlocking = groupCfg.byBlocking;
	}

	/// <summary>
	/// Activate
	/// </summary>
	/// <param name="e"></param>
	/// <returns></returns>
	public async Task<bool> ActivateAsync(FriendMessageEvent e)
	{
		Uid uid = new(e);
		return IsEnable(uid) && Permit(uid) && await ExecuteAsync(e);
	}

	/// <summary>
	/// Activate
	/// </summary>
	/// <param name="e"></param>
	/// <returns></returns>
	public async Task<bool> ActivateAsync(GroupMessageEvent e)
	{
		Uid uid = new(e);
		return IsEnable(uid) && Permit(uid) && await ExecuteAsync(e);
	}

	/// <summary>
	/// enable/disable or get meta info of tha task
	/// </summary>
	/// <param name="uid">From event</param>
	/// <param name="cmd">command, the name of task</param>
	/// <param name="parameter">string behind ':'</param>
	/// <returns></returns>
	public async Task<bool> OperateAsync(Uid uid, string cmd, string parameter)
	{
		if (!Regex.IsMatch(cmd.ToLower(), $@"^({_cmdRgx})$"))
			return false;

		parameter = parameter.ToLower();
		string? msg = null;

		// Meta info
		if (IsEnable(uid)) {
			msg = parameter switch
			{
				"name" => Name,
				"summary" => Summary,
				"help" => Help,
				_ => null
			};
		}
		// Enable & disable
		if (msg is null) {
			if (uid.IsFromGroup)
				msg = OperateGroup(uid, parameter);
			else
				msg = OperateFriend(uid.User, parameter);
		}
		// Reload
		if (msg is null && parameter == "reload") {
			Load();
			msg = $"已重新加载{Name}";
		}
		// Save
		if (msg is null && parameter == "save") {
			Save();
			msg = $"已保存{Name}";
		}

		if (msg is null) return false;
		if (uid.IsFromGroup)
			await Lapluma.SendGroupMessageAsync(uid.Group, msg);
		else
			await Lapluma.SendFriendMessageAsync(uid.User, msg);
		return true;


		string? OperateGroup(Uid uid, string parameter)
		{
			bool enable;
			switch (parameter) {
				case "enable": enable = true; break;
				case "disable": enable = false; break;
				default: return null;
			}
			if (!PermitOperate(uid)) return null;

			Adjust(uid, enable);

			return $"{Name}已{(enable ? "启用" : "禁用")}";
		}

		string? OperateFriend(uint uin, string parameter)
		{
			if (uin != Lapluma.Doctor) return null;
			// (enable|disable) (-f|-g)? [uins]?

			bool enable;
			var splits = parameter.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			switch (splits[0]) {
				case "enable": enable = true; break;
				case "disable": enable = false; break;
				default: return null;
			}
			if (splits.Length == 1) {
				FriendOn = GroupOn = enable;
				return $"{Name}已全局{(enable ? "启用" : "禁用")}";
			}

			bool isGroup;
			switch (splits[1]) {
				case "-f": isGroup = false; break;
				case "-g": isGroup = true; break;
				default: return null;
			}
			if (splits.Length == 2) {
				if (isGroup) GroupOn = enable;
				else FriendOn = enable;
			}

			List<string> adjusted = new();
			List<string> escaped = new();
			foreach (var param in splits.Skip(1)) {
				if (!uint.TryParse(param, out var opUin)) {
					uin = Lapluma.GetUidFromAbbr(param);
				}
				if (uin != 0) {
					Adjust(isGroup ? new Uid(opUin, 0) : new Uid(0, opUin), enable);
					adjusted.Add(param);
				}
				else escaped.Add(param);
			}

			StringBuilder sb;
			if (adjusted.Count == 0) return "未识别任何ID";
			else sb = new StringBuilder($"已{(isGroup ? "群" : "私")}聊{(enable ? "启用" : "禁用")}于")
				.AppendJoin('\n', adjusted);
			if (escaped.Count == 0) return sb.ToString();
			else return sb.Append("\n未识别ID:").AppendJoin('\n', escaped).ToString();

		}

		void Adjust(Uid uid, bool enable)
		{
			lock (__lockObj_Excepts) {
				if (uid.IsFromGroup) {
					if (enable == _isGroupByBlocking)
						_groupExcepts.Remove(uid.Group);
					else _groupExcepts.Add(uid.Group);
				}
				else {
					if (enable == _isFriendByBlocking)
						_friendExcepts.Remove(uid.User);
					else _friendExcepts.Add(uid.User);
				}
			}
		}
	}


	private const string EXCEPT_LIST_FILE = "ExceptList.lpm";
	public void Save()
	{
		Directory.CreateDirectory(InfoDir);
		using var file = File.Create(InfoDir + EXCEPT_LIST_FILE);
		using var sw = new StreamWriter(file);

		foreach (var uin in _friendExcepts)
			sw.WriteLine(uin);
		sw.WriteLine();
		foreach (var uin in _groupExcepts)
			sw.WriteLine(uin);

		SaveEvent?.Invoke();
	}

	public void Load()
	{
		var filename = InfoDir + EXCEPT_LIST_FILE;
		if (!File.Exists(filename)) return;

		using var sr = new StreamReader(filename);
		HashSet<uint> fset = new(), gset = new();
		var curList = fset;
		while (!sr.EndOfStream) {
			var line = sr.ReadLine();
			if (line == null)
				throw new Exception($"Exception when loading task except list of {Name}");
			if (line == "") {
				curList = gset;
				continue;
			}

			if (uint.TryParse(line, out var uin))
				curList.Add(uin);
			else
				Lapluma.DisplayMessage($"Read {line} when loading task except list of {Name}");
		}
		_friendExcepts = fset;
		_groupExcepts = gset;

		LoadEvent?.Invoke();
	}


	public bool IsEnable(Uid uid)
		=> uid.IsFromGroup ?
		(GroupOn && _groupPerm != GroupRange.Banned && (_isGroupByBlocking != _groupExcepts.Contains(uid.Group))) :
		(FriendOn && _friendPerm != FriendRange.Banned && (_isFriendByBlocking != _friendExcepts.Contains(uid.User)));

	private bool Permit(Uid uid)
		=> uid.IsFromGroup ?
		_groupPerm switch
		{
			GroupRange.Banned => false,
			GroupRange.Doctor => uid.User == Lapluma.Doctor,
			GroupRange.Owner => uid.User == Lapluma.Doctor || Util.GetRoleTypeAsync(uid.User, uid.Group).GetAwaiter().GetResult() == RoleType.Owner,
			GroupRange.Admin => uid.User == Lapluma.Doctor || Util.GetRoleTypeAsync(uid.User, uid.Group).GetAwaiter().GetResult() >= RoleType.Admin,
			GroupRange.All => true,
			_ => throw new InvalidOperationException()
		} :
		_friendPerm switch
		{
			FriendRange.Banned => false,
			FriendRange.Doctor => uid.User == Lapluma.Doctor,
			FriendRange.All => true,
			_ => throw new InvalidOperationException()
		};

	private bool PermitOperate(Uid uid)
		=> _operatePerm switch
		{
			OperatorRange.Doctor => uid.User == Lapluma.Doctor,
			OperatorRange.Owner => Util.GetRoleTypeAsync(uid.User, uid.Group).GetAwaiter().GetResult() == RoleType.Owner,
			OperatorRange.Admin => Util.GetRoleTypeAsync(uid.User, uid.Group).GetAwaiter().GetResult() >= RoleType.Admin,
			_ => throw new InvalidOperationException()
		};

	protected abstract Task<bool> ExecuteAsync(FriendMessageEvent e);
	protected abstract Task<bool> ExecuteAsync(GroupMessageEvent e);
}
