using Konata.Core.Common;
using Konata.Core.Interfaces.Api;
using Konata.Core.Message;
using Konata.Core.Message.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tra.Lapluma.Core.Utilities;
internal static class Util
{
	#region Regex

	public static bool IsMatchActRegex(this string input, string patternBody, RegexOptions options = RegexOptions.None)
		=> Regex.IsMatch(input, $"^{ActPfx}{patternBody}$", options);

	public static Match MatchActRegex(this string input, string patternBody, RegexOptions options = RegexOptions.None)
		=> Regex.Match(input, $"^{ActPfx}{patternBody}$", options);

	#endregion

	#region Global IO
	public static string Input(string? prompt = null)
	{
		if (prompt != null)
			Console.Write(prompt);
		return Console.ReadLine();
	}

	public static void Output(string text)
	{
		Console.WriteLine(text);
	}
	#endregion

	#region Konata
	public static bool Ated(this MessageChain chains, uint uin)
		=> chains.Any(c => c is AtChain atc && atc.AtUin == uin);

	public static async Task<RoleType> GetRoleOf(this Bot bot, uint member, uint group)
		=> member == bot.Doctor ? RoleType.Owner + 1 : (await bot.Knt.GetGroupMemberInfo(group, member)).Role;

	public static async Task<bool> CheckAuthorizationAsync(this Bot bot, uint member, uint group, RoleType minRole)
		=> member == bot.Doctor
		|| (await bot.Knt.GetGroupMemberInfo(group, member)).Role >= minRole;
	#endregion

	#region Http
	private static readonly HttpClient _httpClient = new();

	public static Task<byte[]> DownloadHttpBytes(Uri uri) => _httpClient.GetByteArrayAsync(uri);

	public static async Task<string> DownloadHttpString(Uri uri) => Encoding.UTF8.GetString(await DownloadHttpBytes(uri));
	#endregion

	#region Random
	private static readonly Random _random = new();

	public static int RandomInt(int min, int max) => _random.Next(min, max);

	public static bool ProbabilisticAssert(double probability) => _random.NextDouble() < probability;
	// I dont know if it is useful but anyhow i just want to write it.
	public static TResult WeightedRandom<TResult>(params (double Weight, TResult Result)[] weights)
	{
		double total = weights.Sum(w => w.Weight);
		double randVal = _random.NextDouble() * total;

		double curSum = 0.0;
		foreach (var (w, res) in weights) {
			curSum += w;
			if (curSum > randVal) return res;
		}

		throw new InvalidOperationException();
	}
	#endregion

	#region Convertor Extensions
	public static JToken ToJToken(this JsonChain chain) => JToken.Parse(chain.Content);

	public static byte[] ToBytes(this Image img)
	{
		using var mem = new MemoryStream();
		img.Save(mem, ImageFormat.Png);
		return mem.ToArray();
	}
	#endregion

	public static T AwaitSync<T>(this Task<T> task) => task.GetAwaiter().GetResult();
}
