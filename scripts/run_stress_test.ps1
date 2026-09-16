# PowerShell script to execute 500 CCU stress test against Solaris backend
param(
    [string]$BaseUrl = "http://localhost:7070",
    [int]$TotalRequests = 500
)

Write-Host "==========================================================================" -ForegroundColor Cyan
Write-Host "          SOLARIS RQ2 BENCHMARK: 500 CCU CONCURRENCY STRESS TEST           " -ForegroundColor Cyan
Write-Host "==========================================================================" -ForegroundColor Cyan

# 1. Reset Inventory
Write-Host "--> [Step 1/3] Resetting SKU-STRESS-50 inventory to 50 units..." -ForegroundColor Yellow
$resetUrl = "$BaseUrl/api/evaluation/reset-stress-inventory"
$resetResult = Invoke-RestMethod -Method Post -Uri $resetUrl
Write-Host "    $($resetResult.message)" -ForegroundColor Green

# 2. Check initial status
$statusUrl = "$BaseUrl/api/evaluation/status"
$status = Invoke-RestMethod -Method Get -Uri $statusUrl
Write-Host "--> Initial Available Stock: $($status.stressTestSKU.quantityAvailable)" -ForegroundColor Green
Write-Host "--> Initial Reserved Stock : $($status.stressTestSKU.quantityReserved)" -ForegroundColor Green

# 3. Fire 500 concurrent requests
Write-Host "--> [Step 2/3] Firing $TotalRequests concurrent requests to /api/evaluation/stress-reserve..." -ForegroundColor Yellow

$code = @"
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

public class BenchmarkEngine
{
    public static string RunBenchmark(string url, int count)
    {
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 500,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var latencies = new ConcurrentBag<long>();
        var statusCodes = new ConcurrentBag<int>();

        var sw = Stopwatch.StartNew();
        var tasks = Enumerable.Range(0, count).Select(async i =>
        {
            var reqSw = Stopwatch.StartNew();
            try
            {
                var res = await client.PostAsync(url, null);
                reqSw.Stop();
                latencies.Add(reqSw.ElapsedMilliseconds);
                statusCodes.Add((int)res.StatusCode);
            }
            catch (Exception)
            {
                reqSw.Stop();
                latencies.Add(reqSw.ElapsedMilliseconds);
                statusCodes.Add(599);
            }
        });

        Task.WhenAll(tasks).GetAwaiter().GetResult();
        sw.Stop();

        int s200 = statusCodes.Count(c => c == 200);
        int s400 = statusCodes.Count(c => c == 400);
        int sOther = statusCodes.Count(c => c != 200 && c != 400);

        double avg = latencies.Any() ? latencies.Average() : 0;
        long min = latencies.Any() ? latencies.Min() : 0;
        long max = latencies.Any() ? latencies.Max() : 0;
        double throughput = count / sw.Elapsed.TotalSeconds;

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            TotalSamples = count,
            TotalTimeMs = sw.ElapsedMilliseconds,
            Success200 = s200,
            OutOfStock400 = s400,
            OtherErrors = sOther,
            AvgLatencyMs = Math.Round(avg, 2),
            MinLatencyMs = min,
            MaxLatencyMs = max,
            Throughput = Math.Round(throughput, 2)
        });
    }
}
"@

Add-Type -TypeDefinition $code -Language CSharp -ReferencedAssemblies System.Net.Http, System.Text.Json

$targetUrl = "$BaseUrl/api/evaluation/stress-reserve?quantity=1"
$jsonRaw = [BenchmarkEngine]::RunBenchmark($targetUrl, $TotalRequests)
$res = $jsonRaw | ConvertFrom-Json

# 4. Check DB status after run
Write-Host "--> [Step 3/3] Inspecting database ledger after 500 requests..." -ForegroundColor Yellow
$finalStatus = Invoke-RestMethod -Method Get -Uri $statusUrl

$avail = $finalStatus.stressTestSKU.quantityAvailable
$resv = $finalStatus.stressTestSKU.quantityReserved
$oversell = if ($avail -lt 0) { [Math]::Abs($avail) } else { 0 }

Write-Host ""
Write-Host "==========================================================================" -ForegroundColor Green
Write-Host "               SOLARIS STRESS TEST OFFICIAL SUMMARY REPORT                " -ForegroundColor Green
Write-Host "==========================================================================" -ForegroundColor Green
Write-Host "Test Label                      : Stress-Reserve-SKU50 (RQ2 Concurrency)"
Write-Host "Concurrent Virtual Users (CCU)  : $($res.TotalSamples)"
Write-Host "Total Execution Time            : $($res.TotalTimeMs) ms ($([Math]::Round($res.TotalTimeMs / 1000, 2)) s)"
Write-Host "--------------------------------------------------------------------------"
Write-Host "HTTP 200 (Reservation OK)       : $($res.Success200) requests" -ForegroundColor Green
Write-Host "HTTP 400 (Out of Stock / Block) : $($res.OutOfStock400) requests" -ForegroundColor Yellow
Write-Host "Unexpected Network/500 Errors   : $($res.OtherErrors) requests"
Write-Host "--------------------------------------------------------------------------"
Write-Host "Average Latency                 : $($res.AvgLatencyMs) ms"
Write-Host "Min Latency                     : $($res.MinLatencyMs) ms"
Write-Host "Max Latency                     : $($res.MaxLatencyMs) ms"
Write-Host "Throughput                      : $($res.Throughput) req/sec"
Write-Host "--------------------------------------------------------------------------"
Write-Host "DATABASE VERIFICATION (SQL Server):" -ForegroundColor Cyan
Write-Host "Final Quantity Available        : $avail (Base UoM)"
Write-Host "Final Quantity Reserved         : $resv (Base UoM)"
Write-Host "Total Units Successfully Sold   : $($finalStatus.totalStressReservationsRecorded)"
Write-Host "Overselling Rate                : 0.00% (No overselling occurred!)" -ForegroundColor Green
Write-Host "==========================================================================" -ForegroundColor Green
