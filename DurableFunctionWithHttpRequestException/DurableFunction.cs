using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DurableFunctionWithHttpRequestException
{
    public static class DurableFunction
    {
        [FunctionName("DurableFunction")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var outputs = new List<string>();

            try
            {
                // Replace "hello" with the name of your Durable Activity Function.
                outputs.Add(await context.CallActivityAsync<string>("DurableFunction_Hello", "Tokyo"));
                outputs.Add(await context.CallActivityAsync<string>("DurableFunction_Hello", "Seattle"));
                outputs.Add(await context.CallActivityAsync<string>("DurableFunction_Hello", "London"));
            }
            catch (FunctionFailedException ex)
            {
                log.LogWarning("Why does this inner exception message is not my original message?");
                log.LogWarning($"{ex.InnerException.Message}");
            }

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("DurableFunction_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            throw new HttpRequestException("This is the message of my original HttpRequestException....");
            //TODO: comment the above exception to throw an InvalidOperationException, where the message is properly catch in the orchectrator
            throw new System.InvalidOperationException("This is the message of my original HttpRequestException....");

            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("DurableFunction_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("DurableFunction", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}