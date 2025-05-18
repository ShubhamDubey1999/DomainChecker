using DomainChecker;
using NamecheapApiExample;
using Newtonsoft.Json;
using System.Net;

public class Program
{
    public static void Main(string[] args)
    {
        var json = File.ReadAllText("E:\\DomainChecker\\DomainChecker\\domain-config.json");
        var config = JsonConvert.DeserializeObject<Rootobject>(json);
        var tlds = config.Tlds.ToList();
        var subdomains = config.Subdomains.ToList();
        var domainsToCheck = config.DomainsToCheck.ToList();

        //DomainVerify1 domainVerify1 = new DomainVerify1(tlds, subdomains, domainsToCheck);
        //domainVerify1.DomainCheck().Wait();

        //DomainVerify2 domainCheck2 = new DomainVerify2(tlds, subdomains, domainsToCheck);
        //domainCheck2.DomainCheck().Wait();

        //DomainVerify3 domainCheck3 = new DomainVerify3(tlds, subdomains, domainsToCheck);
        //domainCheck3.DomainCheck().Wait();

        DomainVerify4 domainVerify = new();
        domainVerify.DomainCheck().Wait();

        //NameCheap domainVerify5 = new();
        //domainVerify5.DomainCheck().Wait();
    }
}
public class Rootobject
{
    public string[] Tlds { get; set; }
    public string[] Subdomains { get; set; }
    public string[] DomainsToCheck { get; set; }
}
