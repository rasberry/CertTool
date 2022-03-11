using System;
using System.IO;
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

			switch(Options.Action) {
				case ActionKind.File: FileCertInfo(); break;
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
				var folders = Directory.EnumerateFiles(Options.InputFileFolder,Options.FilePattern,opts);
				foreach(var f in folders) {
					PrintOneCertInfo(sb, f);
					Print(sb);
				}
			}
			else {
				PrintOneCertInfo(sb,Options.InputFileFolder);
				Print(sb);
			}
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
				var xname = Options.ExportFile ?? cert.SerialNumber;
				var xdata = cert.Export(X509ContentType.Cert);
				File.WriteAllBytes(xname+".crt",xdata);
			}

			sb.ND(0,"File",file);
			sb.ND(0,"Type",type.ToString());

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

		static string PrintOID(Oid oid)
		{
			return $"{oid.FriendlyName} - {oid.Value}";
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
			sb.ND(0,"FriendlyName",cert.FriendlyName);
			sb.ND(0,"NotBefore",cert.NotBefore.ToString("O"));
			sb.ND(0,"NotAfter",cert.NotAfter.ToString("O"));
			sb.ND(0,"Format",cert.GetFormat());
			sb.ND(0,"SignatureAlgorithm",PrintOID(cert.SignatureAlgorithm));
			sb.ND(0,"SerialNumber",cert.GetSerialNumberString());
			sb.ND(0,"Thumbprint",cert.Thumbprint);
			sb.ND(0,"HasPrivateKey",cert.HasPrivateKey?"Yes":"No");
			sb.ND(0,"Version",cert.Version.ToString());
			if (Options.ValidateCert || Options.ValidateCertOffline) {
				sb.ND(0,"IsValid",cert.Verify()?"Yes":"No");
			}
		}
	}
}