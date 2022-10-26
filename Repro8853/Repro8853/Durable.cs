using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Repro8853
{
    public static class Durable
    {
        [FunctionName("Durable")]
        public static async Task<long> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var cpuSeconds = context.GetInput<double>();
            var found = await context.CallActivityAsync<long>("Run", cpuSeconds);
            return found;
        }

        [FunctionName("Run")]
        public static long Run([ActivityTrigger] double cpuSeconds, ILogger log)
        {
            return Worker.Run(cpuSeconds, log);
        }

        [FunctionName("Compute")]
        public static async Task<IActionResult> Compute(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            double cpuSeconds = double.Parse(req.Query["cpuSeconds"]);
            string instanceId = $"Durable{cpuSeconds}";
            log.LogInformation($"Retrieving status of orchestration with ID = '{instanceId}'.");
            var status = await starter.GetStatusAsync(instanceId);
            if (status == null)
            {
                await starter.StartNewAsync("Durable", instanceId, cpuSeconds);
                log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
                status = await starter.GetStatusAsync(instanceId);
            }
            else if (status.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
            {
                var found = (string)status.Output;
                await starter.PurgeInstanceHistoryAsync(instanceId);
                return new OkObjectResult($"found {found}");
            }
                return new OkObjectResult($"status '{status.RuntimeStatus}'.");
        }
    }
}