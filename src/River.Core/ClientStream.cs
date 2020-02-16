﻿using River.Common;
using River.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace River
{
	public abstract class ClientStream : SimpleNetworkStream
	{


		public TcpClient Client { get; private set; }
		protected Stream Stream { get; set; }

		/// <summary>
		/// Negotiate to establish stream
		/// </summary>
		public abstract void Route(string targetHost, int targetPort, bool? proxyDns = null);

		protected string ProxyHost { get; private set; }
		protected int ProxyPort { get; private set; }

		/// <summary>
		/// Plug to a new socket
		/// </summary>
		public virtual void Plug(Uri uri)
		{
			if (uri is null)
			{
				throw new ArgumentNullException(nameof(uri));
			}

			ProxyHost = uri.Host;
			ProxyPort = uri.Port;

			if (Stream != null)
			{
				throw new Exception("Already been plugged");
			}
			Client = new TcpClient(ProxyHost, ProxyPort);
			Client.Client.NoDelay = true;
			Stream = Client.GetStream();
		}

		/// <summary>
		/// Plug to existing channel
		/// </summary>
		public virtual void Plug(Uri uri, Stream stream)
		{
			if (Stream != null)
			{
				throw new Exception("Already been plugged");
			}
			if (uri != null)
			{
				ProxyHost = uri.Host;
				ProxyPort = uri.Port;
			}

			/*
			if (!(stream is MustFlushStream))
			{
				stream = new MustFlushStream(stream);
			}
			*/
			Stream = stream;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var r = Stream.Read(buffer, offset, count);
			if (r <= 0) Close();
			return r;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Stream.Write(buffer, offset, count);
			Stream.Flush();
		}

		bool _closing;

		public override void Close()
		{
			_closing = true;
			base.Close();
			try
			{
				Client?.Client?.Shutdown(SocketShutdown.Both);
			}
			catch { }
			Stream.Close();
		}

		public override void Flush()
			=> Stream.Flush();

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			=> Stream.BeginRead(buffer, offset, count, callback, state);

		public override int EndRead(IAsyncResult asyncResult)
		{
			if (_closing) return 0;
			return Stream.EndRead(asyncResult);
		}			

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			=> Stream.BeginWrite(buffer, offset, count, callback, state);

		public override void EndWrite(IAsyncResult asyncResult)
		{
			if (_closing) return;
			Stream.EndWrite(asyncResult);
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> Stream.ReadAsync(buffer, offset, count, cancellationToken);

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			=> Stream.WriteAsync(buffer, offset, count, cancellationToken);
	}
}
