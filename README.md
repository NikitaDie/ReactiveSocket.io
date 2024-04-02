# ReactiveSocket.io

# Quick start

## Client
```C#
const string host = "127.0.0.19";
const int port = 8080;
ReactiveSocket socket = new ReactiveSocket($"{host}:{port}"); //default instance of ITransport with default instance of IMessageBuilder

TextMessage ms = new TextMessage("Hello, World!");
TextMessage ms2 = new TextMessage("How do you do?");
TextMessage ms3 = new TextMessage("Have you done your homework?");

_ = socket.EmitAsync("test", ms, ms2, ms3);
_ = socket.EmitAsync("test2", new TextMessage("This message is for TestHandler"));

_ = socket.EmitAsync("akaTest", response =>
{
    Console.WriteLine(response.GetValue<TextMessage>(0).Content);
});

```

## Server
```C#
TcpListener listener = new TcpListener(IPAddress.Parse(host), port);
listener.Start();

while (true)
{
    TcpClient tcpClient = await _listener.AcceptTcpClientAsync();
    TcpTransport transportClient = new TcpTransport(tcpClient)
    {
        MessageBuilder = new MessageBuilder(),
    };
    await transportClient.InitializeAsync();
    ReactiveSocket client = new ReactiveSocket(transportClient);
                
                
    client.On("test", request =>
    {
        for (int i = 0; i < request.PayloadCount; ++i)
        {
            if (request.GetPayloadType(i) == typeof(TextMessage).ToString())
            {
                var x = request.GetValue<TextMessage>(i);
                Console.WriteLine(x.Content);
            }          
        }
    });

    client.On("akaTest", request =>
    {
        _ = request.CallbackAsync(new TextMessage("Hi, back!"));
    });

    HandleClientEvents(client);
}

void HandleClientEvents(ReactiveSocket client) //If the lambda handler is not registered for the event Name, 
{                                              //the OnRawEventAction event will be Invoke.
    client.OnRawEventAction += (eventName, response) =>
    {
        //Pass on to a router, for example
    };
}

```
