using Konata.Core.Events.Model;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lapluma.Konata.Tasks.Implementations;
internal sealed class DrawDeemoChart : BaseTask
{
	public DrawDeemoChart() : base(
		name: nameof(DrawDeemoChart),
		summary: "decht {#speed} {#chart} => 绘制Deemo谱面",
		help: "decht {#speed} {#chart} => 绘制Deemo谱面\n" +
		"# speed => 相邻note间隔\n" +
		"# chart => 描述谱面",
		cmdRgx: "decht",
		friendCfg: (true, FriendRange.All),
		groupCfg: (true, GroupRange.All, OperatorRange.Admin))
	{ }

	protected override Task<bool> ExecuteAsync(FriendMessageEvent e)
	{
		throw new NotImplementedException();
	}

	protected override Task<bool> ExecuteAsync(GroupMessageEvent e)
	{
		throw new NotImplementedException();
	}
}
