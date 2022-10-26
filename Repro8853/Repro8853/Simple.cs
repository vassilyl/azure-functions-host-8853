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
    public static class Simple
    {

        [FunctionName("Simple")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            double cpuSeconds = double.Parse(req.Query["cpuSeconds"]);
            var found = Worker.Run(cpuSeconds, log);
            return new OkObjectResult($"Last prime number found is {found}.");
        }
    }
}
