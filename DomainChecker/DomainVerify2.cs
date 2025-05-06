using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
public class DomainVerify2
{
    static List<string> Tlds;
    static List<string> Subdomains;
    static List<string> domainsToCheck;
    public DomainVerify2(List<string> _Tlds, List<string> subdomains, List<string> Domaincheck)
    {
        Tlds = _Tlds;
        Subdomains = subdomains;
        domainsToCheck = Domaincheck;
    }
    private static readonly SemaphoreSlim Semaphore = new(100); // Max 100 concurrent lookups
    public async Task DomainCheck()
    {        
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();
        foreach (var domain in domainsToCheck)
        {
            foreach (var hostname in GeneratePossibleHostnames(domain))
            {
                await Semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await DoGetHostEntry(hostname).ConfigureAwait(false);
                    }
                    finally
                    {
                        Semaphore.Release();
                    }
                }));
            }
        }
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        Console.WriteLine($"Finished in {stopwatch.Elapsed.TotalMinutes:F2} minutes");
    }
    public static IEnumerable<string> GeneratePossibleHostnames(string baseHostname)
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
    public static async Task DoGetHostEntry(string hostname)
    {
        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(hostname).ConfigureAwait(false);
            foreach (var ip in hostEntry.AddressList)
            {
                Console.WriteLine($"{hostname} taken - {ip}");
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
    }
}