using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;
using OfficeOpenXml;

namespace DomainChecker
{
    public class DomainVerify4
    {
        private const int MaxConcurrency = 100;
        private const int BatchSize = 10000;
        private const string OutputFilePath = "E:\\DomainChecker\\output\\domain-results.txt";

        public async Task DomainCheck()
        {
            var stopwatch = Stopwatch.StartNew();
            var found = new ConcurrentBag<string>();
            var allHostnames = GenerateAllHostnamesFromCsv(); // or GenerateAllHostnames()

            Directory.CreateDirectory(Path.GetDirectoryName(OutputFilePath)!);
            using var outputWriter = new StreamWriter(OutputFilePath, false);
            using var semaphore = new SemaphoreSlim(MaxConcurrency);

            foreach (var batch in allHostnames.Chunk(BatchSize))
            {
                var tasks = batch.Select(async hostname =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var entry = await Dns.GetHostEntryAsync(hostname);
                        var ip = entry.AddressList.FirstOrDefault();
                        if (ip != null)
                        {
                            var result = $"{hostname} taken - {ip}";
                            found.Add(result);
                        }
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostNotFound)
                    {
                        var result = $"{hostname} not exists";
                        found.Add(result);
                    }
                    catch (Exception ex)
                    {
                        var result = $"Error with {hostname}: {ex.Message}";
                        found.Add(result);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToList();

                await Task.WhenAll(tasks);
                Console.WriteLine($"Processed batch of {batch.Length} domains.");
            }

            // Write results to file
            foreach (var line in found)
            {
                await outputWriter.WriteLineAsync(line);
            }

            stopwatch.Stop();
            Console.WriteLine($"Finished in {stopwatch.Elapsed.TotalMinutes:F2} minutes");
            Console.WriteLine($"Results written to {OutputFilePath}");
        }

        public List<string> GenerateAllHostnamesFromCsv()
        {
            string filePath = "E:\\DomainChecker\\DomainChecker\\Domain find April- Deepak - Domain.csv";
            var hostnames = new List<string>();

            using (var reader = new StreamReader(filePath))
            {
                bool isFirstLine = true;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    if (isFirstLine)
                    {
                        isFirstLine = false;
                        continue;
                    }

                    var values = line.Split(',');

                    if (values.Length > 0 && !string.IsNullOrWhiteSpace(values[0]))
                    {
                        hostnames.Add(values[0].Trim());
                    }
                }
            }

            return hostnames;
        }

        public List<string> GenerateAllHostnames()
        {
            string filePath = "E:\\DomainChecker\\DomainChecker\\building-domain-test.xlsx";
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var hostnames = new List<string>();

            using (var package = new ExcelPackage(new FileInfo(filePath)))
            {
                var worksheet = package.Workbook.Worksheets[0];
                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    var hostname = worksheet.Cells[row, 1].Text;
                    if (!string.IsNullOrWhiteSpace(hostname))
                    {
                        hostnames.Add(hostname);
                    }
                }
            }

            return hostnames;
        }
    }

    // Extension for batch chunking
    public static class Extensions
    {
        public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size)
        {
            var chunk = new List<T>(size);
            foreach (var item in source)
            {
                chunk.Add(item);
                if (chunk.Count == size)
                {
                    yield return chunk.ToArray();
                    chunk.Clear();
                }
            }
            if (chunk.Count > 0)
                yield return chunk.ToArray();
        }
    }
}
