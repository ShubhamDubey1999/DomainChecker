using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

public class DomainVerify3
{
    private readonly List<string> Tlds;
    private readonly List<string> Subdomains;
    private readonly List<string> DomainsToCheck;

    public DomainVerify3(List<string> tlds, List<string> subdomains, List<string> domainsToCheck)
    {
        Tlds = tlds;
        Subdomains = subdomains;
        DomainsToCheck = domainsToCheck;
    }

    public async Task DomainCheck()
    {
        var stopwatch = Stopwatch.StartNew();
        var found = new ConcurrentBag<string>();
        var allHostnames = GenerateAllHostnames();

        // Use a SemaphoreSlim for better throttling (e.g., DNS rate limits)
        using var semaphore = new SemaphoreSlim(100);

        var tasks = allHostnames.Select(async hostname =>
        {
            await semaphore.WaitAsync();
            try
            {
                var entry = await Dns.GetHostEntryAsync(hostname);
                foreach (var ip in entry.AddressList)
                {
                    found.Add($"{hostname} taken - {ip}");
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == System.Net.Sockets.SocketError.HostNotFound)
            {
                Console.WriteLine($"{hostname} not exists");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with {hostname}: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        foreach (var line in found)
        {
            Console.WriteLine(line);
        }
        Console.WriteLine($"Finished in {stopwatch.Elapsed.TotalMinutes:F2} minutes");
    }
    private IEnumerable<string> GenerateAllHostnames()
    {
        foreach (var baseHostname in DomainsToCheck)
        {
            foreach (var sub in Subdomains)
            {
                foreach (var tld in Tlds)
                {
                    yield return string.IsNullOrEmpty(sub)
                        ? $"{baseHostname}{tld}"
                        : $"{sub}.{baseHostname}{tld}";
                }
            }
        }
    }
}
