using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace River
{
	public class SocksServerTunnelClientWorker : SocksServerClientWorker
	{
		private readonly SocksServerToRiverClient _server;

		//private RiverClient _forward;
		protected TcpClient _clientForward;
		protected NetworkStream _streamFroward;

		private byte[] _bufferForwardRead = new byte[1024 * 32];
		private int _bufferForwardReadPos;

		public override void Dispose()
		{
			try
			{
				_clientForward?.Close();
				_clientForward = null;
			}
			catch { }
			try
			{
				_streamFroward?.Close();
				_streamFroward = null;
			}
			catch { }

			base.Dispose();
		}

		public SocksServerTunnelClientWorker(SocksServer<SocksServerTunnelClientWorker> server, TcpClient client) : base(client)
		{
			_server = (SocksServerToRiverClient)server;
		}

		protected override void EstablishForwardConnection()
		{
			_clientForward = new TcpClient();
			//_clientForward.Connect("127.0.0.1", 8888); // fiddler
			_clientForward.Connect(_server.RiverHost, _server.RiverPort);
			_streamFroward = _clientForward.GetStream();

			var target = _dnsNameRequested ?? _addressesRequested[0].ToString();
			var targetPort = _portRequested;

			var requestString = $"GET http://{_server.RiverHost}:{_server.RiverPort}/?{Obfuscate(0)}={Obfuscate()}&{Obfuscate(1, 'v')}=1&{Obfuscate(2, 'c')}=c&{Obfuscate(3, 'h')}={target}&{Obfuscate(4, 'p')}={targetPort}&{Obfuscate(5)}={Obfuscate()} HTTP/1.0\r\n"
				+ "Connection: keep-alive\r\n"
				//+ "User-Agent: Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:50.0) Gecko/20100101 Firefox/50.0\r\n"
				+ "Accept: text/html\r\n"
				+ $"Host: {_server.RiverHost}:{_server.RiverPort}\r\n"
				+ "Accept-Encoding: gzip, deflate\r\n"
				+ "Cache-Control: no-cache\r\n"
				+ "\r\n";
			var request = _utf.GetBytes(requestString);
			_streamFroward.Write(request, 0, request.Length);

			var count = _streamFroward.Read(_bufferForwardRead, 0, _bufferForwardRead.Length);
			var response = _utf.GetString(_bufferForwardRead, 0, count);
			if (!response.StartsWith("HTTP/1.0 200"))
			{
				throw new Exception();
			}
			_client.NoDelay = true;
			_clientForward.NoDelay = true;
			_streamFroward.BeginRead(_bufferForwardRead, 0, _bufferForwardRead.Length, ReceiveFromForward, null);
		}

		void ReceiveFromForward(IAsyncResult ar)
		{
			if (_streamFroward == null)
			{
				return;
			}
			try
			{
				// _bufferForwardRead
				var count = _streamFroward.EndRead(ar);
				if (count > 0)
				{
					// do the job - unpack the bytes from river and send it to the client
					// header parsing limited to 1024 with char-to-byte encoding
					var responseHeaderString = Encoding.ASCII.GetString(_bufferForwardRead, 0, count > 1024 ? 1024 : count);
					// parse content length and end of header
					var eoh = responseHeaderString.IndexOf("\r\n\r\n") + 4;
					var headers = new Dictionary<string, string>();
					for (int i = 0; i < eoh - 4; )
					{
						var start = i;
						i = responseHeaderString.IndexOf("\r\n", i) + 2;
						var sp = responseHeaderString.IndexOf(':', start);
						if (sp > i)
						{
							continue; // this is first line
						}
						var headerKey = responseHeaderString.Substring(start, sp - start).Trim();
						var headerValue = responseHeaderString.Substring(sp + 1, i - sp - 1).Trim();
						headers[headerKey] = headerValue;
					}
					// make sure headers and full body are in place
					// extract body
					string lenStr;
					if (!headers.TryGetValue("Content-Length", out lenStr))
					{
						throw new Exception("Content-Length is mandatory");
					}
					int len = int.Parse(lenStr);
					if (len < count + _bufferForwardReadPos - eoh)
					{
						// not complete - read some more
						_bufferForwardReadPos += count;
						_streamFroward.BeginRead(_bufferForwardRead, _bufferForwardReadPos, _bufferForwardRead.Length - _bufferForwardReadPos, ReceiveFromForward, null);
					}
					else
					{
						// process - decode the body
						var data = new byte[len];
						Array.Copy(_bufferForwardRead, eoh, data, 0, len);
						for (int i = 0; i < len; i++)
						{
							data[i] = (byte)(data[i] ^ 0xAA);
						}
						// send back to SOCKS client
						var debug = Encoding.ASCII.GetString(data, 0, data.Length);
						_stream.Write(data);

						// get ready for next message
						_bufferForwardReadPos = 0;
						_streamFroward.BeginRead(_bufferForwardRead, 0, _bufferForwardRead.Length, ReceiveFromForward, null);
					}
				}
				else
				{
					Dispose();
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("ReceiveFromForward Exception: " + ex);
				Dispose();
			}
		}

		protected override void SendForward(byte[] buffer, int pos, int count)
		{
			if (_streamFroward == null)
			{
				return;
			}

			var requestString = $"POST http://{_server.RiverHost}:{_server.RiverPort}/ HTTP/1.0\r\n"
				+ $"Connection: keep-alive\r\n"
				//+ "User-Agent: Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:50.0) Gecko/20100101 Firefox/50.0\r\n"
				+ $"Accept: text/html\r\n"
				+ $"Content-Type: text/html\r\n"
				+ $"Host: {_server.RiverHost}:{_server.RiverPort}\r\n"
				+ $"Accept-Encoding: gzip, deflate\r\n"
				+ $"Cache-Control: no-cache\r\n"
				+ $"Content-Length: {count}\r\n"
				+ "\r\n";
			var request = _utf.GetBytes(requestString);
			var requestBuf = new byte[request.Length + count];
			Array.Copy(request, requestBuf, request.Length);
			for (int i = pos; i < pos+count; i++)
			{
				buffer[i]= (byte)(buffer[i] ^ 0xAA);
			}
			Array.Copy(buffer, pos, requestBuf, request.Length, count);

			_streamFroward.Write(requestBuf, 0, requestBuf.Length);
		}

		private static readonly Random _rnd = new Random();

		static string Obfuscate(int id = -1, char actualName = '_')
		{
			var sb = new StringBuilder();
			for (int i = 0, m=_rnd.Next(2)+id+2; i < m; i++)
			{
				if (i == id)
				{
					sb.Append(actualName);
				}
				else
				{
					sb.Append((char)('a'+_rnd.Next(26)));
				}
			}
			return sb.ToString();
		}
	}
}