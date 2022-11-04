using System ;
using System.Security.Cryptography.X509Certificates ;
using System.Security ;
using System.Net.Security ;
using System.Security.Authentication ;
using System.Security.Cryptography ;

namespace WebSockets
{
	/// <summary>
	/// Class that connectes X509Certificate2 instance and it origin(file source)
	/// </summary>
	public class CertificateSource
	{
		/// <summary>
		/// X509Certificate2 instance
		/// </summary>
		public X509Certificate2 certificate
		{
			get ;
			protected set ;
		}
		/// <summary>
		/// File path, uri or something
		/// </summary>
		public string source
		{
			get ;
			protected set ;
		}
		/// <summary>
		/// Creates new instance of CertificateSource class
		/// </summary>
		/// <param name="certificate">X509Certificate2 instance</param>
		/// <param name="source">File path, uri or something</param>
		public CertificateSource ( X509Certificate2 certificate , string source )
		{
			this.certificate = certificate ;
			this.source = source ;
		}
	}
}
