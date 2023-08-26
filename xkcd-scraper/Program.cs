using System.Net;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace xkcd_scraper;

internal class Program
{
    const int ItemCount = 2812;

    static int Main(string[] args)
    {
        return MainAsync(args).Result;
    }
    static async Task<int> MainAsync(string[] args)
    {
        // first get all items in the list
        var urls = new string[ItemCount].Select((x, i) => $"https://xkcd.com/{i + 1}/info.0.json").ToArray();

        var func = async (string x) =>
        {
            try
            {
                var decoded = JsonConvert.DeserializeObject<Dictionary<string, object>>(x);
                
                Console.WriteLine($"Decoded item {(long)decoded["num"]}");

                return decoded;
            }
            catch
            {
                return null;
            }
        };

        var results = await WebHelpers.GetAndProcess(urls, func);

        results = results.Where(x => x != null).ToArray();

        urls = results.Select(x => (string)x["img"]).ToArray();

        var func2 = async (byte[] data, int index, Dictionary<string, object> arg) =>
        {
            await File.WriteAllBytesAsync(MakePathSafe($"xkcd/{index} - {((string)arg["title"]).Replace("/", " ")}.png"), data);
            Console.WriteLine($"Wrote '{index} - {(string)arg["title"]}'.png");
            return true;
        };

        Directory.CreateDirectory("xkcd");

        await WebHelpers.GetAndProcessBinary<bool, Dictionary<string, object>>(urls, func2, results);

        return 0;
    }

    static string MakePathSafe(string path)
    {
        // Remove invalid characters
        string safePath = string.Join("_", path.Split(Path.GetInvalidFileNameChars()));

        return safePath;
    }
}
