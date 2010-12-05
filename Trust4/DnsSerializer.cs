using System;
using System.Text;
using ARSoft.Tools.Net.Dns;
using System.Net;

namespace Trust4
{
	public static class DnsSerializer
	{
		public static string ToStore(DnsQuestion question)
		{
			switch (question.RecordType)
			{
				case RecordType.A:
					return "DNS!" + question.Name.ToLowerInvariant();
				case RecordType.CName:
					return "DNS!" + question.Name.ToLowerInvariant();
				default:
					throw new NotSupportedException("The specified DNS question is not supported by the serializer.");
			}
		}
		
		public static string ToStore(DnsRecordBase answer)
		{
			if (answer is ARecord)
				return "DNS!A!" + (answer as ARecord).Address.ToString();
			else if (answer is CNameRecord)
				return "DNS!CNAME!" + (answer as CNameRecord).CanonicalName.ToLowerInvariant();
			else
				throw new NotSupportedException("The specified DNS answer type is not supported by the serializer.");
		}
		
		public static DnsRecordBase FromStore(string domain, byte[] data)
		{
			string[] split = Encoding.ASCII.GetString(data).Split(new char[] { '!' });
			if (split[0] != "DNS") return null;
			
			switch (split[1])
			{
				case "A":
					// Only one field for this..
					IPAddress ip;
					if (IPAddress.TryParse(split[2], out ip))
						return new ARecord(domain, 3600, ip);
					else
						return null;
				case "CNAME":
					// Only one field for this..
					return new CNameRecord(domain, 3600, split[2].ToLowerInvariant());
				default:
					return null;
			}
		}
	}
}

