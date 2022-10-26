using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Repro8853
{
    internal static class Worker
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
        
        public static long Run(double cpuSeconds, ILogger log)
        {
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
                if (workers[idx].Result && candidates[idx] > found)
                {
                    found = candidates[idx];
                }
                candidate += 2;
                candidates[idx] = candidate;
                var copy = candidate;
                workers[idx] = Task.Run(() => CheckPrime(copy, cts.Token));
            }
            cts.Cancel();
            return found;
        }
    }
}
