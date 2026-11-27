using System.Net; // IPAddress
using System.Net.Sockets; // TcpListener to pick a free port
using AvaloniaDCT.Services; // LanChatService host + join

namespace AvaloniaDCT.Tests; // Group chat without Avalonia windows

public class LanChatServiceTests // Two in-process clients on 127.0.0.1
{
    [Fact] // Host and Join exchange one line each, the way two Chat tabs do
    public async Task Host_and_client_exchange_group_messages()
    {
        var port = FreeTcpPort(); // Avoid colliding with a running DCT Host on 27000
        await using var host = new LanChatService(); // Tom
        await using var client = new LanChatService(); // Bob

        var hostReceived = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously); // Client → host
        host.MessageReceived += (from, text) => // Tom also hears his own SendAsync locally
        {
            if (from == "Bob") // Only Bob's line
            {
                hostReceived.TrySetResult(text); // from-client
            } // end if Bob
        }; // end host MessageReceived
        var clientReceived = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously); // Host → client
        client.MessageReceived += (from, text) => // Bob hears the host broadcast
        {
            if (from == "Tom") // Only Tom's line
            {
                clientReceived.TrySetResult(text); // from-host
            } // end if Tom
        }; // end client MessageReceived

        var twoPeers = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); // Hello finished
        client.PeersChanged += names => // Host broadcasts the list after hello
        {
            if (names.Count >= 2) // Tom and Bob
            {
                twoPeers.TrySetResult(); // Join is usable
            } // end if both online
        }; // end PeersChanged

        await host.StartHostAsync(port, "Tom"); // Listen
        Assert.True(host.IsRunning); // Disconnect would be enabled
        await client.JoinAsync("127.0.0.1", port, "Bob"); // Same machine
        await twoPeers.Task.WaitAsync(TimeSpan.FromSeconds(5)); // Hello + peers list

        await host.SendAsync("from-host"); // In-process + fan-out
        Assert.Equal("from-host", await clientReceived.Task.WaitAsync(TimeSpan.FromSeconds(5))); // Bob's transcript

        await client.SendAsync("from-client"); // Host echoes to everyone
        Assert.Equal("from-client", await hostReceived.Task.WaitAsync(TimeSpan.FromSeconds(5))); // Tom's transcript

        await client.StopAsync(); // Bob disconnects
        await host.StopAsync(); // Release the port
        Assert.False(host.IsRunning); // Idle
        Assert.False(client.IsRunning); // Idle
    } // end host and client exchange

    [Fact] // Blank name is rejected before a socket is opened
    public async Task StartHostAsync_requires_a_display_name()
    {
        await using var host = new LanChatService(); // Unused
        await Assert.ThrowsAsync<ArgumentException>(() => host.StartHostAsync(FreeTcpPort(), "  ")); // Who is online would be empty
    } // end requires name

    private static int FreeTcpPort() // Ephemeral listen, then close, then reuse that number
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); // OS picks a free port
        listener.Start(); // Bind
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; // 1–65535
        listener.Stop(); // Free it for LanChatService
        return port; // Host binds this next
    } // end FreeTcpPort
} // end LanChatServiceTests
