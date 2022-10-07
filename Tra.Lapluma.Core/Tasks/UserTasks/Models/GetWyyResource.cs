using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Tra.Lapluma.Core.Exceptions;
using Tra.Lapluma.Core.Tasks.UserTasks.Packages;
using Tra.Lapluma.Core.Utilities;
using static Tra.Lapluma.Core.Tasks.UserTasks.Models.GetWyyResource.Package;
using static Tra.Lapluma.Core.Utilities.Message;

namespace Tra.Lapluma.Core.Tasks.UserTasks.Models;
internal sealed class GetWyyResource : AwaitTask<GetWyyResource.Package>
{
	public GetWyyResource(Bot bot) : base(bot,
		actRgx: "wyy|网易云",
		name: nameof(GetWyyResource),
		summary: "获取网易云专辑图或音乐链接",
		help: "wyy [(-c|-m)] [<url>]\n" +
		"# (-c|-m) => 指定获取专辑图(c)或音乐链接(m)，留空一并获取\n" +
		"# <url> => 网易云音乐的连接，留空则等待网易云转发消息",
		friendDefaultEnable: true,
		groupDefaultEnable: true)
	{ }

	protected override Task<bool> OnAwakeAsync(FriendMessageEvent ev, Package pkg)
		=> AwakeAsync(ev.Chain.ToString(), pkg, _bot.SenderToFriend(ev));

	protected override Task<bool> OnAwakeAsync(GroupMessageEvent ev, Package pkg)
		=> AwakeAsync(ev.Chain.ToString(), pkg, _bot.SenderToGroup(ev));

	protected override Task<bool> OnProcessAsync(FriendMessageEvent ev, Package pkg)
		=> ProcessAsync(ev.Chain[0], pkg, _bot.SenderToFriend(ev));

	protected override Task<bool> OnProcessAsync(GroupMessageEvent ev, Package pkg)
		=> ProcessAsync(ev.Chain[0], pkg, _bot.SenderToGroup(ev));

	private async Task<bool> AwakeAsync(string chainstr, Package pkg, Sender sender)
	{
		var match = chainstr.MatchActRegex(@"(?:wyy|网易云)((?: -.)*)(?: (.*))?$");
		if (!match.Success) return false;
		const int OPT = 1;
		const int URL = 2;

		var optstrs = match.Groups[OPT].Value.ToLower().Split(" -");
		ResourceOption opt = ResourceOption.None;
		foreach (var optstr in optstrs) {
			switch (optstr) {
				case "c": opt |= ResourceOption.Cover; break;
				case "m": opt |= ResourceOption.Music; break;
				default: break;
			}
		}
		if (opt == ResourceOption.None)
			opt = ResourceOption.Cover | ResourceOption.Music;

		var urlstr = match.Groups[URL].Value.ToLower();
		// From json
		if (urlstr == "") {
			// If awakened
			if (pkg.State != BasePackage.StateType.Asleep) {
				await sender(Text("我在等哦，请转发网易云消息"));
				return true;
			}
			await sender(Text("请转发网易云消息"));
			pkg.State = BasePackage.StateType.Awake;
			pkg.Option = opt;
			pkg.StartTimer(120); // 默默超时不进行提示.jpg
		}
		// From Url
		else {
			await sender(await FromUrl(urlstr, opt));
		}
		return true;
	}

	private static async Task<bool> ProcessAsync(BaseChain firstChain, Package pkg, Sender sender)
	{
		if (firstChain is not JsonChain chain) return false;

		var res = await FromJson(chain, pkg.Option);
		if (res is null) return false;

		await sender(res);
		pkg.State = BasePackage.StateType.Terminated;
		return true;
	}

	private static async Task<MessageBuilder> FromUrl(string urlstr, ResourceOption option)
	{

		// '/#/' will disturb the creation
		if (!Uri.TryCreate(urlstr.Replace("/#/", "/"), UriKind.Absolute, out var uri) ||
			uri.Host != "music.163.com")
			return Text("网址似乎不对吧");

		var builder = new MessageBuilder();

		switch (uri.LocalPath) {
			case "/dj":
				string html = await Util.DownloadHttpString(uri);
				if (option.HasFlag(ResourceOption.Cover))
					builder.Add(await Cover(html));
				if (option.HasFlag(ResourceOption.Music))
					builder.Text(Dj(html));
				break;
			case "/song":
				if (option.HasFlag(ResourceOption.Cover)) {
					html = await Util.DownloadHttpString(uri);
					builder.Add(await Cover(html));
				}
				if (option.HasFlag(ResourceOption.Music))
					builder.Text(Song(uri));
				break;
			default:
				return Text("网址似乎不对？");
		}
		return builder;

		#region Methods
		static async Task<BaseChain> Cover(string html)
		{
			var match = Regex.Match(html, @"<meta property=""og:image"" content=""(.*)"".*/>");
			if (match.Success &&
				Uri.TryCreate(match.Groups[1].Value, UriKind.Absolute, out var imguri))
				return ImageChain.Create(await Util.DownloadHttpBytes(imguri));
			else
				throw new TaskException("未获取到专辑图");
		}


		string Dj(string html)
		{
			var match = Regex.Match(html, @"{""mainSong"":{""name"":""(?:.*)"",""id"":(.*),");
			var id = match.Success ? match.Groups[1].Value : null;
			return string.IsNullOrEmpty(id) ? throw new TaskException("未获取到电台歌曲链接")
				: $"http://music.163.com/song/media/outer/url?id={id}.mp3";
		}

		string Song(Uri uri)
		{
			var id = HttpUtility.ParseQueryString(uri.Query)["id"];
			return string.IsNullOrEmpty(id) ? throw new TaskException("未获取到歌曲链接")
				: $"http://music.163.com/song/media/outer/url?id={id}.mp3";
		}
		#endregion
	}

	private static async Task<MessageBuilder?> FromJson(JsonChain chain, ResourceOption option)
	{
		var jtok = chain.ToJToken();
		var meta = jtok["meta"] ?? throw new Exception("Field 'meta' missiong\nJsonChain content:\n" + chain.Content);
		meta = (string?)jtok["desc"] switch
		{
			"音乐" => meta["music"],
			"新闻" => meta["news"],
			_ => null,
		};
		if (meta is null || (string?)meta["tag"] != "网易云音乐")
			return null;

		var builder = new MessageBuilder();

		if (option.HasFlag(ResourceOption.Cover)) {
			if (Uri.TryCreate((string?)meta["preview"], UriKind.Absolute, out var imguri))
				builder.Image(await Util.DownloadHttpBytes(imguri));
			else
				throw new TaskException("无法获取图片");
		}
		if (option.HasFlag(ResourceOption.Music)) {
			builder.Text((string?)meta["musicUrl"]
				?? throw new TaskException("无法获取链接"));
		}
		return builder;
	}

	internal sealed class Package : TimerPackage
	{
		public enum ResourceOption { None = 0, Cover = 1, Music = 2 }
		public ResourceOption Option { get; set; }
	}
}
