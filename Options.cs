using System;
using System.IO;
using System.Text;
using Rasberry.Cli;

namespace CertTool
{
	static class Options
	{
		public static ActionKind Action = ActionKind.None;
		public static bool ExportCert = false;
		public static string ExportFile = null;
		public static string FilePattern = null;
		public static bool IncludeChain = false;
		public static bool IncludeExtensions = false;
		public static string InputResource = null;
		public static bool IsFolder = false;
		public static bool MuteCertErrors = false;
		public static bool RecurseFolders = false;
		public static bool ValidateCert = false;
		public static bool ValidateCertOffline = false;
		public static ExportKind ExportType = ExportKind.None;

		public static void Usage()
		{
			var sb = new StringBuilder();
			sb.WT(0,$"{nameof(CertTool)} (action) [options]");
			sb.WT();
			sb.WT(0,"Actions:");
			sb.PrintEnum<ActionKind>(1,descriptionMap:ActionKindDesc,excludeZero:true);
			sb.WT();
			sb.WT(0,"Options:");
			sb.ND(1,"--help"             ,"Show this help");
			sb.ND(1,"-i (resource)"      ,"Input file, folder, url, or domain");
			sb.ND(1,"-c"                 ,"Also show chain");
			sb.ND(1,"-e"                 ,"Also show extensions");
			sb.ND(1,"-v"                 ,"Validate certificate");
			sb.ND(1,"-vo"                ,"Validate certificate offline only");
			sb.ND(1,"-x [file]"          ,"Export the certificate as a file");
			sb.ND(1,"-xt (export type)"  ,"Select the export type (default DER)");
			sb.ND(1,"-q"                 ,"Suppress certificate error messages");
			sb.ND(1,"-r"                 ,"Recurse folders when the input is a folder");
			sb.ND(1,"-s (pattern)"       ,"Folder search pattern (see below)");
			sb.WT();
			sb.WT(0,"Export Types:");
			sb.PrintEnum<ExportKind>(1,descriptionMap:ExportKindDesc,excludeZero:true);
			sb.WT();
			sb.WT(0,"Search Pattern Info:");
			sb.WT(1,"Search Pattern can be a combination of literal and wildcard characters, but it doesn't support regular expressions. The following wildcard specifiers are permitted in the search pattern:");
			sb.ND(1,"Wildcard specifier","Matches");
			sb.ND(2,"* (asterisk)"," Zero or more characters in that position");
			sb.ND(2,"? (question mark)"," Zero or one character in that position");
			sb.WT(1,@"Characters other than the wildcard are literal characters. For example, the string ""*t"" searches for all names in ending with the letter ""t"". The searchPattern string ""s*"" searches for all names in path beginning with the letter ""s""");
			Log.Message(sb.ToString());
		}

		static string ActionKindDesc(ActionKind kind)
		{
			switch(kind) {
				case ActionKind.File: return "Certificate info for file or folder";
				case ActionKind.Domain: return "Certificate info for url or domain";
			}
			return "";
		}

		static string ExportKindDesc(ExportKind kind)
		{
			switch(kind) {
				case ExportKind.DER: return "Distinguished Encoding Rules - ASN.1";
				case ExportKind.PEM: return "Privacy Enhanced Mail - RFC7468";
				case ExportKind.PFX: return "Personal Information Exchange - RFC7292";
			}
			return "";
		}

		public static bool Parse(string[] argv)
		{
			var p = new ParseParams(argv);
			if (p.Has("--help").IsGood()) {
				Usage();
				return false;
			}

			//do this first
			if (p.Expect(out Action).IsInvalid()) {
				Log.Error("A valid action must be provided");
				return false;
			}

			if (p.Default("-i",out InputResource).IsInvalid()) {
				Log.Error("Invalid value for -i (resource)");
				return false;
			}
			if (p.Default("-s",out FilePattern).IsInvalid()) {
				Log.Error("Invalid value for -s (pattern)");
				return false;
			}
			if (p.Default("-xt",out ExportType).IsInvalid()) {
				Log.Error("Invalid value for -xt (export type)");
				return false;
			}
			if (p.Has("-c").IsGood()) { IncludeChain = true; }
			if (p.Has("-q").IsGood()) { MuteCertErrors = true; }
			if (p.Has("-v").IsGood()) { ValidateCert = true; }
			if (p.Has("-vo").IsGood()) { ValidateCertOffline = true; }
			if (p.Has("-x").IsGood()) { ExportCert = true; }
			if (p.Has("-r").IsGood()) { RecurseFolders = true; }
			if (p.Has("-e").IsGood()) { IncludeExtensions = true; }

			//do this last
			var left = p.Remaining();
			if (left.Length > 0) {
				ExportFile = left[0];
			}

			//sanity checks
			if (Action == ActionKind.File) {
				if (Directory.Exists(InputResource)) {
					IsFolder = true;
				}
				if (!IsFolder && !File.Exists(InputResource)) {
					Log.Error($"Cannot find '{InputResource}'");
					return false;
				}
				if (String.IsNullOrWhiteSpace(FilePattern)) {
					FilePattern = "*";
				}
			}

			if (ExportType == ExportKind.None) {
				ExportType = ExportKind.DER;
			}

			return true;
		}
	}
}