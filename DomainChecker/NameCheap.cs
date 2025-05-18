using System.Diagnostics;
using System.Xml.Linq;
using OfficeOpenXml;

namespace NamecheapApiExample
{
    class NameCheap
    {
        public async Task DomainCheck()
        {
            var stopwatch = Stopwatch.StartNew();

            var apiUser = "Yeshkumar";
            var apiKey = "0b7e658f64a74e1a95addf26a0e20cf4";
            var userName = "Yeshkumar";
            var clientIp = "223.237.30.127";
            clientIp = "223.237.10.88";
            var filePath = "E:\\DomainChecker\\DomainChecker\\Domain find April- Deepak - Domain.csv";
            //filePath = "E:\\DomainChecker\\DomainChecker\\building-domain-test.xlsx";
            var outputFile = "domain_results.txt";

            var domains = await ReadDomainsFromCsvAsync(filePath);
            //var domains = await ReadDomainsFromExcelAsync(filePath);

            if (domains.Count == 0)
            {
                Console.WriteLine("No domains found to check.");
                return;
            }

            using var httpClient = new HttpClient();

            // Clear or create the output file
            File.WriteAllText(outputFile, $"Domain Check Results - {DateTime.Now}\n\n");

            foreach (var batch in domains.Chunk(50))
            {
                var domainList = string.Join(",", batch);
                var url = $"https://api.namecheap.com/xml.response?ApiUser={apiUser}&ApiKey={apiKey}&UserName={userName}&Command=namecheap.domains.check&ClientIp={clientIp}&DomainList={domainList}";
                try
                {
                    var response = await httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var xmlString = await response.Content.ReadAsStringAsync();
                        var doc = XDocument.Parse(xmlString);

                        var domainElements = doc.Descendants("DomainCheckResult");
                        foreach (var domainElement in domainElements)
                        {
                            var domain = domainElement.Attribute("Domain")?.Value;
                            var available = domainElement.Attribute("Available")?.Value;

                            var resultLine = $"Domain: {domain}, Available: {available}";
                            Console.WriteLine(resultLine);
                            await File.AppendAllTextAsync(outputFile, resultLine + Environment.NewLine);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        await File.AppendAllTextAsync(outputFile, $"Batch failed with status code {response.StatusCode}\n");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    await File.AppendAllTextAsync(outputFile, $"Exception: {ex.Message}\n");
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"\n✅ Domain check completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
            Console.WriteLine($"Results saved to: {outputFile}");
        }

        private async Task<List<string>> ReadDomainsFromCsvAsync(string filePath)
        {
            var domains = new List<string>();

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"CSV file not found: {filePath}");
                return domains;
            }

            using var reader = new StreamReader(filePath);
            bool isFirstLine = true;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(line))
                    domains.Add(line.Trim());
            }

            return domains;
        }
        private async Task<List<string>> ReadDomainsFromExcelAsync(string excelPath)
        {
            var domains = new List<string>();

            if (!File.Exists(excelPath))
            {
                Console.WriteLine($"Excel file not found: {excelPath}");
                return domains;
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage(new FileInfo(excelPath));
            var worksheet = package.Workbook.Worksheets[0]; // First worksheet

            int row = 2; // Assuming row 1 is a header
            while (true)
            {
                var domain = worksheet.Cells[row, 1].Text; // Column A (1)
                if (string.IsNullOrWhiteSpace(domain)) break;

                domains.Add(domain.Trim());
                row++;
            }

            return domains;
        }

    }
}
