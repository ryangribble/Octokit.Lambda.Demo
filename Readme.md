# Octokit.Lambda.Demo

This is a demonstration of a .NET Core lambda function that uses [Octokit.net]() to respond to a GitHub WebHook

When receiving an `issues` `opened` webhook event, the lambda function will
* Label the issue as `to_be_reviewed`
* Comment on the issue
