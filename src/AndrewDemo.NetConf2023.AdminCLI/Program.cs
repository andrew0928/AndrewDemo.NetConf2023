using System.Text;
using AndrewDemo.NetConf2023.AdminCLI;

Console.OutputEncoding = new UTF8Encoding(false);
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
return await CliApplication.RunAsync(args, Environment.GetEnvironmentVariable,
    Console.Out, Console.Error, cancellation.Token);
