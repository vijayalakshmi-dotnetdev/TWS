using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TWS.Services
{
    internal class TCPConnectionHelper
    {
        public class TcpConnectionManager
        {
            private TcpClient _client;
            private NetworkStream _stream;
            private readonly string _serverIp;
            private readonly int _port;

            public TcpConnectionManager(string ip, int port)
            {
                _serverIp = ip;
                _port = port;
            }

            public async Task EnsureConnectedAsync()
            {
                if (_client == null || !_client.Connected)
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(_serverIp, _port);
                    _stream = _client.GetStream();
                }
            }

            public async Task<string> SendAndReceiveAsync(string jsonPayload)
            {
                await EnsureConnectedAsync();

                byte[] data = Encoding.UTF8.GetBytes(jsonPayload);

                byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthPrefix);

                await _stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
                await _stream.WriteAsync(data, 0, data.Length);

                byte[] responseLengthBuffer = new byte[4];
                int bytesRead = await _stream.ReadAsync(responseLengthBuffer, 0, 4);

                if (bytesRead == 0) throw new Exception("Server closed connection");

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(responseLengthBuffer);

                int responseLength = BitConverter.ToInt32(responseLengthBuffer, 0);

                byte[] responseBuffer = new byte[responseLength];
                int totalRead = 0;
                while (totalRead < responseLength)
                {
                    int read = await _stream.ReadAsync(responseBuffer, totalRead, responseLength - totalRead);
                    if (read == 0) break;
                    totalRead += read;
                }

                return Encoding.UTF8.GetString(responseBuffer);
            }
        }
    }
}
