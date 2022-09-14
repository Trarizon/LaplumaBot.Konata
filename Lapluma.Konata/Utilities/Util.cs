using Konata.Core.Common;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Lapluma.Konata.Utilities;
internal static class Util
{
	private static HttpClient _httpClient = new();

	#region Convertor Extensions
	public static JToken ToJToken(this JsonChain chain) => JToken.Parse(chain.Content);
	
	public static byte[] ToBytes(this Image img)
	{
		using var mem = new MemoryStream();
		img.Save(mem, ImageFormat.Png);
		return mem.ToArray();
	}
	#endregion

	public static async Task<RoleType> GetRoleTypeAsync(uint user, uint group)
		=> (await Lapluma.Bot.GetGroupMemberInfo(group, user)).Role;

	public static Task<byte[]> DownloadHttpInBytes(Uri uri) => _httpClient.GetByteArrayAsync(uri);

	public static async Task<string> DownloadHttpInString(Uri uri)=>Encoding.UTF8.GetString(await DownloadHttpInBytes(uri));

	public static bool Ated(this MessageChain chains, uint uin)
		=> chains[BaseChain.ChainType.At].Exists(c => ((AtChain)c).AtUin == uin);
}