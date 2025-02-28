using System.IO.Pipes;
using System;

Console.Title = "Messager";

using var pipeClient = new NamedPipeClientStream(".", "MessagePipe", PipeDirection.Out);

try
{
	pipeClient.Connect();
	using var writer = new StreamWriter(pipeClient) { AutoFlush = true };
	Console.WriteLine("Enter messages to send to the lobby:");

	while (true)
	{
		string? message = Console.ReadLine();
		if (message != null)
		{
			writer.WriteLine(message);
		}
	}
}
catch (Exception ex)
{
	Console.WriteLine($"Error: {ex.Message}");
}
