using Newtonsoft.Json;

using System.Collections;
using System.ComponentModel;
using System.Numerics;
using System.Threading.Tasks;

namespace xkcd_scraper;

internal class WebHelpers
{
    public static int RetryCount = 10;
    public static int RetryDelay = 1000;
    private static HttpClient _client = new HttpClient();

    private static async Task<T> ExecuteWithRetry<T>(Func<Task<T>> action, int retryCount, int  retryDelay)
    {
        var tries = 0;

        while (tries < retryCount  - 1)
        {
            try
            {
                return await action();
            }
            catch
            {
                tries++;
                await Task.Delay(retryDelay);
            }
        }

        return await action();
    }

    public static async Task<string> SimpleGet(string address)
    {
        var result = await ExecuteWithRetry(() => _client.GetAsync(address), RetryCount, RetryDelay);

        // XKCD nr. 404 doesn't exist...
        // we'll manage the returned error later on
        if (!address.Contains("404"))
            result.EnsureSuccessStatusCode();

        var str = await result.Content.ReadAsStringAsync();

        return str;
    }

    public  static IEnumerable<Task<byte[]>> SimpleGetsTasksBinary(string[] addresses)
    {
        return addresses.Select(SimpleGetBinary);
    }

    public static IEnumerable<Task<string>> SimpleGetsTasks(string[] addresses)
    {
        return addresses.Select(SimpleGet);
    }

    public static async Task<string[]> SimpleGets(string[] addresses)
    {
        return await Task.WhenAll(SimpleGetsTasks(addresses));
    }

    public static async Task<byte[]> SimpleGetBinary(string address)
    {

        var result = await ExecuteWithRetry(() => _client.GetAsync(address), RetryCount, RetryDelay);

        return await result.Content.ReadAsByteArrayAsync();
    }

    public static async Task<byte[][]> SimpleGetsBinary(string[] addresses)
    {
        return await Task.WhenAll(addresses.Select(SimpleGetBinary));
    }

    public static async Task<T[]> GetAndProcess<T>(string[] addresses, Func<string, Task<T>> func)
    {
        var tasks = SimpleGetsTasks(addresses).ToArray();
        var results = new T[addresses.Length];

        while (tasks.Any(x => x != null))
        {
            var t = await Task.WhenAny(tasks.Where(x => x != null).ToArray());

            var taskIndex = IndexOf(tasks, t);
            tasks[taskIndex] = null;

            results[taskIndex] = await func(t.Result);
        }

        return results;
    }

    public static async Task<T[]> GetAndProcessBinary<T, TArg>(string[] addresses, Func<byte[], int, TArg, Task<T>> func, TArg[] funcArgs)
    {
        var tasks = SimpleGetsTasksBinary(addresses).ToArray();
        var processingTasks = new Task<T>[addresses.Length];

        while (tasks.Any(x => x != null))
        {
            var t = await Task.WhenAny(tasks.Where(x => x != null).ToArray());

            var taskIndex = IndexOf(tasks, t);
            tasks[taskIndex] = null;

            processingTasks[taskIndex] = func(t.Result, taskIndex, funcArgs[taskIndex]);
        }

        Task.WaitAll(processingTasks);

        return processingTasks.Select(x => x.Result).ToArray();
    }

    private static int IndexOf<T>(T[] items, T item)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].Equals(item))
                return i;
        }

        throw new Exception("Item not in list!");
    }
}
