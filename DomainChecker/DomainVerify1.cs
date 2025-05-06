using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

public class DomainVerify1
{
    static List<string> Tlds;
    static List<string> Subdomains;
    static List<string> domainsToCheck;
    public DomainVerify1(List<string> _Tlds,List<string> subdomains , List<string> Domaincheck)
    {
        Tlds = _Tlds;
        Subdomains = subdomains;
        domainsToCheck = Domaincheck;
    }
    public async Task DomainCheck()
    {
        
        var stopwatch = Stopwatch.StartNew();
        var found = new ConcurrentBag<string>();

        await Parallel.ForEachAsync(domainsToCheck, new ParallelOptions { MaxDegreeOfParallelism = 100 }, async (domain, ct) =>
        {
            foreach (var hostname in GeneratePossibleHostnames(domain))
            {
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
            }
        });
        stopwatch.Stop();
        foreach (var line in found)
        {
            Console.WriteLine(line);
        }
        Console.WriteLine($"Finished in {stopwatch.Elapsed.TotalMinutes:F2} minutes");
    }

    private static IEnumerable<string> GeneratePossibleHostnames(string baseHostname)
    {
        foreach (var sub in Subdomains)
        {
            foreach (var tld in Tlds)
            {
                yield return string.IsNullOrEmpty(sub) ? $"{baseHostname}{tld}" : $"{sub}.{baseHostname}{tld}";
            }
        }
    }
}
