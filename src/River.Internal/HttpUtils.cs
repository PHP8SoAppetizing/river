﻿ using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace River
{
	public class HttpUtils
	{
		public const int MaxHeaderSize = 1024 * 8;

		public static IDictionary<string, string> TryParseHttpHeader(byte[] buffer, int pos, int length)
		{
			int eoh;
			string headerString;
			return TryParseHttpHeader(buffer, pos, length, out eoh, out headerString);
		}

		public static IDictionary<string, string> TryParseHttpHeader(byte[] buffer, int pos, int length, out int eoh)
		{
			string headerString;
			return TryParseHttpHeader(buffer, pos, length, out eoh, out headerString);
		}

		private static readonly Regex _requestLineParser = new Regex(@"(?ix)^
(?'v'\w+)\s+ # VERB
(?'u' # URL
  ((?'p'https?):\/\/)? # protocol
  (?'h'[\d_a-z\.-]+)? # host
  (:(?'pr'\d+))? # port
  /?
  [^\s]* # other arguments
)\s+
HTTP/(?'hv'\d\.\d)# http ver

", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		public static IDictionary<string, string> TryParseHttpHeader(byte[] buffer, int pos, int length, out int bufEoh, out string headerString)
		{
			headerString = Encoding.ASCII.GetString(buffer, pos, length > MaxHeaderSize ? MaxHeaderSize : length);
			var eoh = headerString.IndexOf("\r\n\r\n");
			if (eoh > 0)
			{
				eoh += 4;
				var headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
				for (int i = 0; i < eoh - 4;)
				{
					var start = i;
					i = headerString.IndexOf("\r\n", i + 1) + 2;
					var colon = headerString.IndexOf(':', start);
					if (start == 0)
					{
						// this is first line
						var match = _requestLineParser.Match(headerString, 0, i);
						headers["_verb"] = match.Groups["v"].Value;
						headers["_url"] = match.Groups["u"].Value;
						headers["_url_host"] = match.Groups["h"].Value;
						headers["_url_port"] = match.Groups["pr"].Value;
						headers["_http_ver"] = match.Groups["hv"].Value;
					}
					if (colon <= i && colon >= 0)
					{
						var headerKey = headerString.Substring(start, colon - start).Trim();
						var headerValue = headerString.Substring(colon + 1, i - colon - 1).Trim();
						headers[headerKey] = headerValue.Trim();
					}
				}
				bufEoh = eoh + pos;
				return headers;
			}
			bufEoh = eoh;
			return null;
		}
	}
}
