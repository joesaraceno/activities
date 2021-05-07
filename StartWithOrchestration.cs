using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace js.proto
{
    public static class StartWithOrchestration
    {
        [FunctionName("ExecuteMessageProcedural")]
        // The orchestration that is called from the http trigger
        // will start the Procedural
        public static async Task<List<string>> ExecuteMessageProcedural(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            outputs.Add(await context.CallActivityAsync<string>("PostMessage", "Hello"));
            outputs.Add(await context.CallActivityAsync<string>("PostMessage", "My"));
            outputs.Add(await context.CallActivityAsync<string>("PostMessage", "Darlin'"));

            return outputs;
        }


        [FunctionName("PostMessage")]
        // the actual function that is called
        // This is the activity, really
        public static string PostMessage([ActivityTrigger] string message, ILogger log)
        {
            log.LogInformation($"Posting {message}.");
            return message;
        }

        [FunctionName("StartWithOrchestration")]
        // the outer HttpTrigger that kicks everything off
        // returns a reference to the job
        // which can be polled until ready
        public static async Task<IActionResult> Start(

            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient client,
            ILogger log)
        {
            var timer = new Stopwatch();
            timer.Start();
            string instanceId = await client.StartNewAsync("ExecuteMessageProcedural", null);
            HttpManagementPayload payload = client.CreateHttpManagementPayload(instanceId);
            timer.Stop();
            log.LogCritical($"Started orchestration with ID = '{instanceId}'. It took {timer.ElapsedMilliseconds}ms");
            return new JsonResult(payload.StatusQueryGetUri);
            // return new RedirectResult($"http://localhost:3000?statusUri={payload.StatusQueryGetUri}");
        }
    }
}