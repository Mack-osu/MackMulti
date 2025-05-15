using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

Console.Title = "Messager";

Console.WriteLine("Messager out of order in this version of the bot. Feel free to close this window.");

await Task.Delay(-1);

//while (true)
//{
//	try
//	{
//		using (var client = new NamedPipeClientStream(".", "MessagePipe", PipeDirection.Out))
//		{
//			Console.WriteLine("Connecting to server...");
//			client.Connect(5000);
//			Console.WriteLine("Connected.");

//			using (var writer = new StreamWriter(client) { AutoFlush = true })
//			{
//				while (true)
//				{
//					string msg = Console.ReadLine();
//					if (string.IsNullOrWhiteSpace(msg)) continue;

//					try
//					{
//						writer.WriteLine(msg);
//					}
//					catch (IOException ex)
//					{
//						Console.WriteLine("Failed to write to pipe: " + ex.Message);
//						break; // Exit inner loop, reconnect
//					}
//				}
//			}
//		}
//	}
//	catch (TimeoutException)
//	{
//		Console.WriteLine("Server not available. Retrying in 3 seconds...");
//	}
//	catch (IOException ex)
//	{
//		Console.WriteLine("Pipe error: " + ex.Message);
//	}

//	Thread.Sleep(3000); // Wait before retrying
//}



//NamedPipeClientStream? pipeClient = null;
//StreamWriter? writer = null;

//async Task ConnectToServerAsync()
//{
//	int retryDelay = 2000;

//	while (true)
//	{
//		try
//		{
//			pipeClient = new NamedPipeClientStream(".", "MessagePipe", PipeDirection.Out);
//			await pipeClient.ConnectAsync(5000);

//			writer = new StreamWriter(pipeClient, Encoding.UTF8)
//			{
//				AutoFlush = true
//			};

//			Console.WriteLine("Connected to Lobby.");
//			break;
//		}
//		catch (Exception ex)
//		{
//			Console.WriteLine($"Failed to connect: {ex.Message}. Retrying in {retryDelay / 1000} seconds...");
//			await Task.Delay(retryDelay);

//			retryDelay = Math.Min(retryDelay * 2, 10000); // Exponential backoff, max 10s
//		}
//	}
//}

//async Task RunAsync()
//{
//	await ConnectToServerAsync();

//	Console.WriteLine("Enter messages to send to the lobby:");

//	while (true)
//	{
//		string? message = Console.ReadLine();

//		if (string.IsNullOrWhiteSpace(message)) continue;

//		if (writer == null || pipeClient == null || !pipeClient.IsConnected)
//		{
//			Console.WriteLine("Connection lost. Attempting to reconnect...");
//			Cleanup();
//			await ConnectToServerAsync();
//		}

//		try
//		{
//			await writer!.WriteLineAsync(message);
//		}
//		catch (IOException ioEx)
//		{
//			Console.WriteLine($"Write failed: {ioEx.Message}. Reconnecting...");
//			Cleanup();
//			await ConnectToServerAsync();
//		}
//		catch (Exception ex)
//		{
//			Console.WriteLine($"Unexpected error: {ex.Message}");
//		}
//	}
//}

//void Cleanup()
//{
//	try
//	{
//		writer?.Dispose();
//		pipeClient?.Dispose();
//	}
//	catch
//	{
//		// Ignore cleanup errors
//	}

//	writer = null;
//	pipeClient = null;
//}

//// Run the main loop
//await RunAsync();
