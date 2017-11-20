using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace Octokit.Lambda.Demo
{
    public class Function
    {
        /// <summary>
        /// Processes a GitHub WebHook
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine($"Webhook function executing");

            try
            {
                var message = await ProcessWebHook(request);
                Console.WriteLine($"SUCCESS: {message}");
                return new APIGatewayProxyResponse
                {
                    Body = JsonConvert.SerializeObject(new Dictionary<string, string>() { { "message", message } }),
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                throw ex;
            }
        }

        public async Task<string> ProcessWebHook(APIGatewayProxyRequest request)
        {
            // Configure GitHub Access
            var token = Environment.GetEnvironmentVariable("github_token");
            
            var github = new GitHubClient(new ProductHeaderValue("octokit_lambda"))
            {
                Credentials = new Credentials(token)
            };

            Console.WriteLine($"Octokit connection initialized");

            // Get webhook event type from header
            var eventType = "unknown";
            try
            {
                eventType = request.Headers["X-GitHub-Event"];
            }
            catch { }

            // Deserialize webhook payload from body
            dynamic data = null;
            try
            {
                //Console.WriteLine($"Request Body: {request.Body ?? "null"}");
                data = JsonConvert.DeserializeObject(request.Body);
            }
            catch { }

            // Get webhook action from payload
            var action = data?.action ?? "unknown";

            Console.WriteLine($"Received GitHub WebHook event '{eventType}' action '{action}'");

            // Process events
            var message = "";
            if (eventType == "issues" && action == "opened")
            {
                // Extract repo/issue details from request body
                string owner = data?.repository?.owner?.login;
                string repo = data?.repository?.name;
                int issueNumber = data?.issue?.number ?? 0;

                Console.WriteLine($"Processing {owner}/{repo}#{issueNumber}");

                // Add "to_be_reviewed" label to the issue
                var labelResponse = await github.Issue.Labels.AddToIssue(owner, repo, issueNumber, new[] { "to_be_reviewed" });

                // Add a comment to the issue
                var commentResponse = await github.Issue.Comment.Create(owner, repo, issueNumber, CannedResponses.ISSUE_SEEN);

                await github.Issue.Assignee.AddAssignees(owner, repo, issueNumber, new AssigneesUpdate(new[] { "ryangribble" }));
                message = $"Issue {owner}/{repo}#{issueNumber} is now under review";
            }
            else
            {
                message = $"No processing required for event '{eventType}' action '{action}'";
            }

            return message;
        }
    }
}

public static class CannedResponses
{
    public static string ISSUE_SEEN = "## :rotating_light: Review Pending :rotating_light:\nThankyou for your issue, someone will be taking a :eyes: shortly!";
    public static string ISSUE_CLOSED = "# :skull: Denied!!! :skull:\n\n![](https://media1.giphy.com/media/qiDb8McXyj6Eg/giphy.gif)";
    public static string ISSUE_LGTM = "# :champagne: Congrats - You Rock!!! :guitar:\n\n![](https://media1.giphy.com/media/Deyr6El6Wk0z6/giphy.gif)";
}