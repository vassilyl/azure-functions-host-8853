using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.Threading;

namespace Repro8853
{
    public static class Function1
    {
        static bool CheckPrime(long candidate, CancellationToken token)
        {
            long divisor = 3;
            long remainder = candidate % divisor;
            while (remainder != 0 && divisor * divisor < candidate && !token.IsCancellationRequested)
            {
                divisor += 2;
                remainder = candidate % divisor;
            }
            return remainder != 0;
        }

        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            double cpuSeconds = double.Parse(req.Query["cpuSeconds"]);
            var stop = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(cpuSeconds);
            log.LogInformation($"Will stop at {stop}");

            var cpuCount = int.Parse(Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS") ?? "16");
            var workers = new Task<bool>[cpuCount];
            var candidates = new long[cpuCount];
            var cts = new CancellationTokenSource();

            long found = 3;
            long candidate = found;
            for (int i = 0; i < cpuCount; i++)
            {
                candidate += 2;
                candidates[i] = candidate;
                var copy = candidate;
                workers[i] = Task.Run(() => CheckPrime(copy, cts.Token));
            }

            while (DateTimeOffset.UtcNow < stop)
            {
                var idx = Task.WaitAny(workers);
                if (workers[idx].Result)
                {
                    found = candidates[idx];
                }
                candidate += 2;
                candidates[idx] = candidate;
                var copy = candidate;
                workers[idx] = Task.Run(() => CheckPrime(copy, cts.Token));
            }
            cts.Cancel();
            return new OkObjectResult($"Last prime number found is {found}.");
        }
    }
}
