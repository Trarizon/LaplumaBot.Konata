using Konata.Core.Events.Model;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Lapluma.Konata.Utilities;
using Lapluma.Konata.Utilities.Structrues;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Lapluma.Konata.Tasks.Implementations;
internal sealed class GetWyyResource : AwaitTask<GetWyyResource.Package>
{
	public GetWyyResource() : base(
		name: nameof(GetWyyResource),
		summary: "wyy [-(c|m)] [url] => 获取网易云专辑图或音乐链接",
		help: "wyy -(c|m) [#url] => 获取网易云专辑图或音乐链接\n" +
		"# -(c|m) => c获取专辑图，m获取音乐链接，留空一并获取\n" +
		"# [url] => 网易云链接，留空则等待转发消息",
		cmdRgx: "wyy",
		friendCfg: (true, FriendRange.All),
		groupCfg: (false, GroupRange.All, OperatorRange.Admin))
	{ }

	protected override Task<bool> OnAwakeAsync(FriendMessageEvent e, Package pkg)
		=> GeneralAwakeAsync(e.Chain.ToString().ToLower(), pkg, MessageHandlers.SendFriendAsync(e));

	protected override Task<bool> OnAwakeAsync(GroupMessageEvent e, Package pkg)
		=> GeneralAwakeAsync(e.Chain.ToString().ToLower(), pkg, MessageHandlers.SendGroupAsync(e));

	protected override Task<bool> OnProcessAsync(FriendMessageEvent e, Package pkg)
		=> GeneralProcessAsync(e.Chain[0], pkg, MessageHandlers.SendFriendAsync(e));

	protected override Task<bool> OnProcessAsync(GroupMessageEvent e, Package pkg)
		=> GeneralProcessAsync(e.Chain[0], pkg, MessageHandlers.SendGroupAsync(e));


	public enum ResourceType { Cover = 1, Music = 2 }
	public class Package : AwaitPackage
	{
		public ResourceType ResourceType { get; set; }
	}

	private static async Task<bool> GeneralAwakeAsync(string chainstr, Package pkg, MessageHandler<MessageBuilder> handler)
	{
		var match = Regex.Match(chainstr,
			@"^wyy(?:[ \n]+-(c|m))?(?:[ \n]+(.*))?$");
		if (!match.Success) return false;
		#region Regex index
		const int TYPE = 1;
		const int URL = TYPE + 1;
		#endregion

		var typestr = match.Groups[TYPE].Value.ToLower();
		ResourceType type = typestr switch
		{
			"c" => ResourceType.Cover,
			"m" => ResourceType.Music,
			"" => ResourceType.Cover | ResourceType.Music,
			_ => throw new InvalidOperationException()
		};

		var param = match.Groups[URL].Value;
		// From Json
		if (param == "") {
			// Awakened
			if (pkg.State != BasePackage.StateType.Asleep) {
				await handler(Message.Text("请转发网易云消息"));
				return true;
			}
			await handler(Message.Text("请转发网易云消息"));
			pkg.State = BasePackage.StateType.Awake;
			pkg.ResourceType = type;
			pkg.WaitTimeout(2 * 60, () => handler(Message.Text("Timeout")));
		}
		// From Url
		else {
			await handler(await FromUrl(param, type));
			pkg.State = BasePackage.StateType.Terminated;
		}

		return true;
	}

	private static async Task<bool> GeneralProcessAsync(BaseChain firstChain, Package pkg, MessageHandler<MessageBuilder> handler)
	{
		if (firstChain is not JsonChain chain) return false;

		var res = await FromJson(chain, pkg.ResourceType);
		if (res is null) return false;

		await handler(res);
		pkg.State = BasePackage.StateType.Terminated;
		return true;
	}

	private static async Task<MessageBuilder> FromUrl(string urlstr, ResourceType resType)
	{
		if (!Uri.TryCreate(urlstr.Replace("/#/", "/"), UriKind.Absolute, out var uri) ||
			uri.Host != "music.163.com")
			return Message.Text("参数需要为网易云链接哦");

		var builder = new MessageBuilder();

		switch (uri.LocalPath) {
			case "/dj":
				string html = await Util.DownloadHttpInString(uri);
				if (resType.HasFlag(ResourceType.Cover)) {
					builder.Add(await GetCover(html));
				}
				if (resType.HasFlag(ResourceType.Music)) {
					builder.Text(GetDjMusic(html));
				}
				break;
			case "/song":
				if (resType.HasFlag(ResourceType.Cover)) {
					html = await Util.DownloadHttpInString(uri);
					builder.Add(await GetCover(html));
				}
				if (resType.HasFlag(ResourceType.Music)) {
					builder.Text(GetSongMusic(uri));
				}
				break;
			default:
				return Message.Text("我记得，路径应该只能是dj或者song");
		}
		return builder;

		static async Task<BaseChain> GetCover(string html)
		{
			var match = Regex.Match(html, @"<meta property=""og:image"" content=""(.*)"".*/>");
			if (match.Success && Uri.TryCreate(match.Groups[1].Value, UriKind.Absolute, out var imgUri))
				return Message.Chain.Image(await Util.DownloadHttpInBytes(imgUri));
			else
				return Message.Chain.Text("未获取到专辑图");
		}

		static string GetDjMusic(string html)
		{
			var match = Regex.Match(html, @"{""mainSong"":{""name"":""(?:.*)"",""id"":(.*),""position""");
			var id = match.Success ? match.Groups[1].Value : null;
			return string.IsNullOrEmpty(id) ? "没有获取到歌曲链接"
				: $"http://music.163.com/song/media/outer/url?id={id}.mp3";
		}

		static string GetSongMusic(Uri uri)
		{
			var id = HttpUtility.ParseQueryString(uri.Query)["id"];
			return string.IsNullOrEmpty(id) ? "没有获取到歌曲链接"
				: $"http://music.163.com/song/media/outer/url?id={id}.mp3";
		}
	}

	private static async Task<MessageBuilder?> FromJson(JsonChain chain, ResourceType resType)
	{
		var jobj = chain.ToJToken();
		var meta = jobj["meta"] ?? throw new Exception("Field 'meta' missing.");
		meta = (string?)jobj["desc"] switch
		{
			"音乐" => meta["music"],
			"新闻" => meta["news"],
			_ => null
		};
		if (meta is null || (string?)meta["tag"] != "网易云音乐") return null;

		var builder = new MessageBuilder();
		if (resType.HasFlag(ResourceType.Cover)) {
			var imgUrlstr = (string?)meta["preview"];
			if (imgUrlstr is null || !Uri.TryCreate(imgUrlstr, UriKind.Absolute, out var imgUri))
				builder.Text("无法获取图片");
			else builder.Image(await Util.DownloadHttpInBytes(imgUri));
		}
		if (resType.HasFlag(ResourceType.Music)) {
			builder.Text((string?)meta["musicUrl"] ?? "无法获取链接");
		}
		return builder;
	}
}
