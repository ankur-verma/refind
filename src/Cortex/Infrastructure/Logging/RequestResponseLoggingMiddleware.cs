using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cortex.Infrastructure.Logging;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
    private readonly string _logFilePath;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        // Save logs to the code folder as requested
        _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "api_logs.txt");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        
        // Format request
        var request = context.Request;
        var requestTime = DateTime.UtcNow;
        var method = request.Method;
        var url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
        
        // Log Request Body
        request.EnableBuffering();
        var requestBody = await ReadStreamInChunks(request.Body);
        request.Body.Position = 0;

        // Create a new memory stream for the response body
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            // Execute the next middleware
            await _next(context);
        }
        catch (Exception ex)
        {
            await LogToFileAsync($"[{requestTime:O}] ERROR: {method} {url}\nException: {ex.Message}\nStackTrace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            sw.Stop();
            
            // Format response
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var responseReader = new StreamReader(context.Response.Body, Encoding.UTF8, true, 1024, leaveOpen: true);
            var responseText = await responseReader.ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            
            var statusCode = context.Response.StatusCode;
            
            var logEntry = $"[{requestTime:O}] {method} {url} => {statusCode} ({sw.ElapsedMilliseconds}ms)\n" +
                           $"RequestBody: {requestBody}\n" +
                           $"ResponseBody: {responseText}\n" +
                           new string('-', 80) + "\n";
                           
            await LogToFileAsync(logEntry);

            // Copy the contents of the new memory stream (which contains the response) to the original stream
            await responseBody.CopyToAsync(originalBodyStream);
        }
    }

    private async Task<string> ReadStreamInChunks(Stream stream)
    {
        const int readChunkBufferLength = 4096;
        stream.Seek(0, SeekOrigin.Begin);
        using var textWriter = new StringWriter();
        // IMPORTANT: leaveOpen: true is required so we don't dispose the request body!
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
        var readChunk = new char[readChunkBufferLength];
        int readChunkLength;
        do
        {
            readChunkLength = await reader.ReadBlockAsync(readChunk, 0, readChunkBufferLength);
            await textWriter.WriteAsync(readChunk, 0, readChunkLength);
        } while (readChunkLength > 0);
        return textWriter.ToString();
    }

    private async Task LogToFileAsync(string message)
    {
        try
        {
            // Use a simple lock or rely on FileShare if needed, but since this is dev it's okay.
            // Using a simple retry or appending text asynchronously
            await File.AppendAllTextAsync(_logFilePath, message);
        }
        catch
        {
            // Ignore file logging errors to prevent crashing the app
        }
    }
}
