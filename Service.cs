using System.Net;
using System.Net.Security;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;

namespace HyacineProxy;

public class Service
{
    private readonly Config _conf;
    private readonly ProxyServer _webProxyServer;

    public Service(Config conf)
    {
        _conf = conf;
        _webProxyServer = new ProxyServer();
        _webProxyServer.CertificateManager.EnsureRootCertificateAsync();

        _webProxyServer.BeforeRequest += BeforeRequest;
        _webProxyServer.ServerCertificateValidationCallback += OnCertValidation;

        var port = conf.ProxyPort == 0 ? Random.Shared.Next(10000, 60000) : conf.ProxyPort;
        SetEndPoint(new ExplicitProxyEndPoint(IPAddress.Any, port));
    }

    private void SetEndPoint(ExplicitProxyEndPoint explicitEp)
    {
        explicitEp.BeforeTunnelConnectRequest += BeforeTunnelConnectRequest;

        _webProxyServer.AddEndPoint(explicitEp);
        _webProxyServer.StartAsync();

        if (!OperatingSystem.IsWindows()) return;
        
        _webProxyServer.SetAsSystemHttpProxy(explicitEp);
        _webProxyServer.SetAsSystemHttpsProxy(explicitEp);
    }

    public void Shutdown()
    {
        _webProxyServer.Stop();
        _webProxyServer.Dispose();
    }

    private Task BeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs args)
    {
        var hostname = args.HttpClient.Request.RequestUri.Host;
        Console.WriteLine(hostname);
        args.DecryptSsl = ShouldRedirect(hostname);

        return Task.CompletedTask;
    }

    private Task OnCertValidation(object sender, CertificateValidationEventArgs args)
    {
        args.IsValid = args.SslPolicyErrors == SslPolicyErrors.None;
        return Task.CompletedTask;
    }
    
    private bool ShouldBlock(Uri uri)
    {
        var path = uri.AbsolutePath;
        return _conf.BlockUrls.Any(blockUrl => 
            path.Equals(blockUrl, StringComparison.OrdinalIgnoreCase));
    }

    private Task BeforeRequest(object sender, SessionEventArgs args)
    {
        var requestUrl = args.HttpClient.Request.Url;
        var hostname = args.HttpClient.Request.RequestUri.Host;
        var path = args.HttpClient.Request.RequestUri.AbsolutePath;
        
        if (!ShouldRedirect(hostname)) return Task.CompletedTask;
        if (ShouldBlock(args.HttpClient.Request.RequestUri))
        {
            Console.WriteLine($"Blocked: {requestUrl}");
            args.Respond(new Titanium.Web.Proxy.Http.Response
            {
                StatusCode = 404,
                StatusDescription = "",
            }, true);
            return Task.CompletedTask;
        }
        
        var endpoint = GetEndpointForDomain(hostname, path);
        if (endpoint == null) 
        {
            Console.WriteLine($"Blackhole: {requestUrl}");
            Console.WriteLine($"Host: {hostname}, Path: {path}");
            Console.WriteLine($"Body: {args.HttpClient.Request.Body}");
                
            args.Respond(new Titanium.Web.Proxy.Http.Response
            {
                StatusCode = 404,
                StatusDescription = "Blackholed",
            }, true);
            
            return Task.CompletedTask;
        }
        
        var local = new Uri($"http://{endpoint.Domain}:{endpoint.Port}/");
        var builtUrl = new UriBuilder(requestUrl)
        {
            Scheme = local.Scheme,
            Host = local.Host,
            Port = local.Port
        }.Uri;

        var replacedUrl = builtUrl.ToString();
        Console.WriteLine($"Redirecting: {requestUrl} -> {replacedUrl}");
        args.HttpClient.Request.Url = replacedUrl;
        
        return Task.CompletedTask;
    }
    
    private EndpointConfig? GetEndpointForDomain(string hostname, string path)
    {
        if (ContainsAnyTrigger(path, _conf.Dispatch.RedirectTrigger))
            return _conf.Dispatch;
            
        if (ContainsAnyTrigger(path, _conf.SDK.RedirectTrigger))
            return _conf.SDK;
            
        return null;
    }

    private static bool ContainsAnyTrigger(string path, List<string> triggers)
    {
        return triggers.Count != 0 && triggers.Any(path.Contains);
    }

    private bool ShouldRedirect(string hostname)
    {
        if (hostname.Contains(':'))
            hostname = hostname[0..hostname.IndexOf(':')];
        
        if (_conf.AlwaysIgnoreDomains.Any(domain => hostname.EndsWith(domain)))
            return false;
        
        return _conf.RedirectDomains.Any(domain => hostname.EndsWith(domain));
    }
}
