using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TWS
{
    class WebSocketClient
    {
        public static string accessToken = "";
        private static string clientId = "SKYWS1";        // Replace with your actual client ID


        // Function to generate the SHA256 encryption
        private static string GenerateEncryptedToken(string token)
        {
            // Perform the double SHA256 encryption
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // First SHA256
                string firstHash = Convert.ToBase64String(sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(token)));

                // Second SHA256
                string secondHash = Convert.ToBase64String(sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(firstHash)));

                return secondHash;
            }
        }

        // Function to prepare the request payload
        private static string PrepareRequestPayload(string encryptedToken)
        {
            // Prepare the request JSON
            string requestPayload = $"{{" +
                $"\"susertoken\": \"{encryptedToken}\", " +
                $"\"t\": \"c\", " +
                $"\"actid\": \"{clientId}_WEB\", " +
                $"\"uid\": \"{clientId}_WEB\", " +
                $"\"source\": \"WEB\"" +
                $"}}";

            return requestPayload;
        }

        public static async void CreateClient()
        {
            // Generate the encrypted token
            string encryptedToken = GenerateEncryptedToken(accessToken);

            // Create the WebSocket connection
            using (ClientWebSocket ws = new ClientWebSocket())
            {
                // Set up WebSocket connection
                Uri serverUri = new Uri("wss://feed.gopocket.in/NorenWS/");

                // Connect to the WebSocket server
                await ws.ConnectAsync(serverUri, CancellationToken.None);
                Console.WriteLine("Connected to the WebSocket server.");

                // Send the request once connected
                string requestPayload = PrepareRequestPayload(encryptedToken);
                await SendRequest(ws, requestPayload);

                // Start receiving messages
                await ReceiveMessages(ws);
            }

        }

        // Function to send request over WebSocket
        private static async Task SendRequest(ClientWebSocket ws, string requestPayload)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(requestPayload);
            await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine("Request sent: " + requestPayload);
        }

        // Function to receive messages from WebSocket
        private static async Task ReceiveMessages(ClientWebSocket ws)
        {
            byte[] buffer = new byte[1024];
            WebSocketReceiveResult result;
            string message;

            // Keep receiving messages until the WebSocket is closed
            while (ws.State == WebSocketState.Open)
            {
                result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine("Message received: " + message);
            }

            Console.WriteLine("Connection closed.");
        }
    }
}


