using Microsoft.AspNetCore.Http;
using Nbg.NetCore.Services.Cics.Http.Contract.Jcraupd;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class ProxyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpClient _httpClient;
    private readonly string _targetBaseUrl;

    public ProxyMiddleware(RequestDelegate next, HttpClient httpClient, string targetBaseUrl)
    {
        _next = next;
        _httpClient = httpClient;
        _targetBaseUrl = targetBaseUrl.TrimEnd('/');
    }

    public async Task InvokeAsync(HttpContext context)
    {

        
        //if not, return 401 Unauthorized
        var accessToken = context.Request.Headers["Authorization"].ToString();
        //check if the access token is valid with IdentityServer 
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            // Log the unauthorized access attempt
            Console.WriteLine($"[Unauthorized Access] {context.Request.Method} {context.Request.Path}");
            Console.WriteLine($"[Unauthorized Access] Access Token: {accessToken}");
            Console.WriteLine($"[Unauthorized Access] IsAuthenticated: {context.User.Identity?.IsAuthenticated}");  
            Console.WriteLine($"[Unauthorized Access] User: {context.User.Identity?.Name}");
            Console.WriteLine($"[Unauthorized Access] Claims: {string.Join(", ", context.User.Claims)}");

            
            // Return 401 Unauthorized
            context.Response.StatusCode = 401;

            await context.Response.WriteAsync("Unauthorized");
            return;
        }
     

        // Only forward /api requests
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var targetUrl = $"{_targetBaseUrl}{context.Request.Path}{context.Request.QueryString}";
        Console.WriteLine($"[Proxy] Forwarding to: {targetUrl}");

        var requestMessage = new HttpRequestMessage
        {
            Method = new HttpMethod(context.Request.Method),
            RequestUri = new Uri(targetUrl)
        };

        // Forward all headers
        foreach (var header in context.Request.Headers)
        {
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                requestMessage.Content ??= new StringContent(string.Empty);
                requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        // Forward request body if needed
        if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put)
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
            context.Request.Body.Position = 0;

            requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        try
        {
            using var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

            context.Response.StatusCode = (int)responseMessage.StatusCode;

            // Copy headers
            foreach (var header in responseMessage.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            foreach (var header in responseMessage.Content.Headers)
                context.Response.Headers[header.Key] = header.Value.ToArray();

            // Remove headers that might break response
            context.Response.Headers.Remove("transfer-encoding");

            // Copy body
            await responseMessage.Content.CopyToAsync(context.Response.Body);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Proxy Error] {ex.Message}");
            context.Response.StatusCode = 502;
            await context.Response.WriteAsync("Proxy error occurred.");
        }
    }
}
