using HyacineProxy;

var config = Config.LoadConfig();
var service = new Service(config);

Console.WriteLine($"HyacineProxy started on port {config.ProxyPort}");

var waitForExit = new ManualResetEvent(false);
Console.CancelKeyPress += (sender, e) => 
{
    e.Cancel = true;
    waitForExit.Set();
    OnProcessExit(sender, e);
};
waitForExit.WaitOne();
return;

void OnProcessExit(object? sender, EventArgs e)
{
    Console.WriteLine("Shutting down the proxy...");
    service.Shutdown();
}
