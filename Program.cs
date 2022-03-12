using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Rasberry.Cli;

namespace CertTool
{
	class Program
	{
		public static void Main(string[] argv)
		{
			if (argv.Length < 1) {
				Options.Usage();
				return;
			}
			if (!Options.Parse(argv)) {
				return;
			}

			try {
				switch(Options.Action) {
					case ActionKind.File: FileCertInfo(); break;
					case ActionKind.Domain: DomainCertInfo(); break;
				}
			}
			catch(Exception e) {
				#if DEBUG
				Log.Message(e.ToString());
				#else
				while(e != null) {
					Log.Error(e.Message);
					e = e.InnerException;
				}
				#endif
			}
		}

		static void Print(StringBuilder sb)
		{
			if (sb.Length > 0) {
				Log.Message(sb.ToString());
				sb.Clear();
			}
		}

		static void FileCertInfo()
		{
			StringBuilder sb = new();
			if (Options.IsFolder) {
				var opts = new EnumerationOptions {
					RecurseSubdirectories = Options.RecurseFolders
				};
				var folders = Directory.EnumerateFiles(Options.InputResource,Options.FilePattern,opts);
				foreach(var f in folders) {
					PrintOneCertInfo(sb, f);
					Print(sb);
				}
			}
			else {
				PrintOneCertInfo(sb,Options.InputResource);
				Print(sb);
			}
		}

		static void DomainCertInfo()
		{
			string resource = Options.InputResource;
			Uri uri;
			if (!Uri.TryCreate(resource,UriKind.Absolute,out uri)) {
				if (!Uri.TryCreate($"https://{resource}",UriKind.Absolute,out uri)) {
					Log.Error($"Unrecognized domain or uri {resource}");
					return;
				}
			}

			X509Certificate2 domainCert = null;
			var handler = new HttpClientHandler {
				UseDefaultCredentials = true,
				ServerCertificateCustomValidationCallback = (sender, cert, chain, error) => {
					domainCert = cert;
					return true;
				}
			};

			using HttpClient client = new(handler);
			client.Timeout = TimeSpan.FromMinutes(2);
			var settings = HttpCompletionOption.ResponseHeadersRead;
			using HttpResponseMessage response = client.GetAsync(uri,settings).Result;
			//using HttpContent content = response.Content;

			if (domainCert == null) {
				Log.Error($"Unable to acquire certificate for {uri}");
				return;
			}

			if (Options.ExportCert) {
				var name = Options.ExportFile ?? domainCert.SerialNumber;
				ExportCertAs(Options.ExportType,name,domainCert);
			}

			StringBuilder sb = new();
			PrintCertChain(sb,domainCert);
			Print(sb);
		}

		static void PrintOneCertInfo(StringBuilder sb, string file)
		{
			X509ContentType type;
			try {
				type = X509Certificate2.GetCertContentType(file);
			}
			catch {
				if (!Options.MuteCertErrors) {
					Log.Error($"Unable to find certificate for {file}");
				}
				return;
			}

			var wild = X509Certificate.CreateFromSignedFile(file);
			using var cert = new X509Certificate2(wild);

			if (Options.ExportCert) {
				var name = Options.ExportFile ?? cert.SerialNumber;
				ExportCertAs(Options.ExportType,name,cert);
			}

			sb.ND(0,"File",file);
			sb.ND(0,"Type",type.ToString());
			PrintCertChain(sb,cert);
		}

		static void PrintCertChain(StringBuilder sb, X509Certificate2 cert)
		{
			var policy = new X509ChainPolicy();
			if (Options.ValidateCertOffline) {
				policy.RevocationFlag = X509RevocationFlag.EntireChain;
				policy.RevocationMode = X509RevocationMode.Offline;
				policy.VerificationFlags = X509VerificationFlags.NoFlag;
			}
			else if (Options.ValidateCert) {
				policy.RevocationFlag = X509RevocationFlag.EntireChain;
				policy.RevocationMode = X509RevocationMode.Online;
				policy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
				policy.VerificationFlags = X509VerificationFlags.NoFlag;
			}
			else {
				policy.RevocationMode = X509RevocationMode.NoCheck;
				policy.VerificationFlags = X509VerificationFlags.NoFlag;
			}

			using var chain = new X509Chain { ChainPolicy = policy };
			chain.Build(cert);
			foreach (var elem in chain.ChainElements)
			{
				PrintChainElement(sb, elem);
				if (!Options.IncludeChain) { break; } //only print the first one
				sb.WT();
			}
		}

		static void PrintChainElement(StringBuilder sb, X509ChainElement elem)
		{
			var cert = elem.Certificate;
			PrintCert(sb,cert);
			if (Options.ValidateCert || Options.ValidateCertOffline) {
				foreach(var s in elem.ChainElementStatus) {
					var flag = s.Status;
					sb.ND(0,"Status",flag.ToString());
				}
			}
		}

		static void PrintCert(StringBuilder sb, X509Certificate2 cert)
		{
			sb.ND(0,"Subject",cert.Subject);
			sb.ND(0,"Issuer",cert.Issuer);
			sb.ND(0,"FriendlyName",cert.PublicKey.Oid.FriendlyName);
			sb.ND(0,"NotBefore",cert.NotBefore.ToString("O"));
			sb.ND(0,"NotAfter",cert.NotAfter.ToString("O"));
			sb.ND(0,"Format",cert.GetFormat());
			sb.ND(0,"SignatureAlgorithm",cert.SignatureAlgorithm.FriendlyName);
			sb.ND(0,"SerialNumber",cert.GetSerialNumberString());
			sb.ND(0,"Thumbprint",cert.Thumbprint);
			sb.ND(0,"HasPrivateKey",cert.HasPrivateKey?"Yes":"No");
			sb.ND(0,"Version",cert.Version.ToString());
			sb.ND(0,"KeySize",GetKeySizeString(cert) ?? "Unknown");
			if (Options.ValidateCert || Options.ValidateCertOffline) {
				sb.ND(0,"IsValid",cert.Verify()?"Yes":"No");
			}
			if (Options.IncludeExtensions) {
				foreach(var ex in cert.Extensions) {
					var asn = new AsnEncodedData(ex.Oid,ex.RawData);
					sb.ND(0,ex.Oid.FriendlyName,asn.Format(false));
				}
			}
		}

		static string GetKeySizeString(X509Certificate2 cert)
		{
			var pub = cert.PublicKey;

			var rsa = pub.GetRSAPublicKey();
			if (rsa != null) {
				return rsa.KeySize.ToString();
			}
			var dsa = pub.GetDSAPublicKey();
			if (dsa != null) {
				return dsa.KeySize.ToString();
			}
			var edh = pub.GetECDiffieHellmanPublicKey();
			if (edh != null) {
				return edh.KeySize.ToString();
			}
			var esa = pub.GetECDsaPublicKey();
			if (esa != null) {
				return esa.KeySize.ToString();
			}
			return null;
		}

		static void ExportCertAs(ExportKind kind, string name, X509Certificate2 cert)
		{
			byte[] export = null;
			string ext = null;
			if (kind == ExportKind.DER) {
				export = cert.Export(X509ContentType.Cert);
				ext = ".der";
			}
			else if (kind == ExportKind.PEM) {
				var data = cert.Export(X509ContentType.Cert);
				var pem = ""
					+ "----BEGIN CERTIFICATE-----\n"
					+ Convert.ToBase64String(data,Base64FormattingOptions.InsertLineBreaks)
					+ "\n-----END CERTIFICATE-----"
				;
				export = Encoding.ASCII.GetBytes(pem);
				ext = ".pem";
			}
			else if (kind == ExportKind.PFX) {
				export = cert.Export(X509ContentType.Pfx);
				ext = ".pfx";
			}

			if (export != null) {
				File.WriteAllBytes(name + ext, export);
			}
		}
	}
}