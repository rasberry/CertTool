using System;
using System.IO;
using System.Text;
using Rasberry.Cli;

namespace CertTool
{
	static class Options
	{
		public static ActionKind Action = ActionKind.None;
		public static string InputFileFolder = null;
		public static bool IncludeChain = false;
		public static bool MuteCertErrors = false;
		public static bool ValidateCert = false;
		public static bool ValidateCertOffline = false;
		public static bool ExportCert = false;
		public static string ExportFile = null;
		public static bool RecurseFolders = false;
		public static bool IsFolder = false;
		public static string FilePattern = null;

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
			sb.ND(1,"-i (file/folder)"   ,"Input file or folder name");
			sb.ND(1,"-c"                 ,"Output chain as well");
			sb.ND(1,"-v"                 ,"Validate certificate");
			sb.ND(1,"-vo"                ,"Validate certificate offline only");
			sb.ND(1,"-x [file]"          ,"Export the certificate as a file");
			sb.ND(1,"-q"                 ,"Suppress certificate error messages");
			sb.ND(1,"-r"                 ,"Recurse folders when the input is a folder");
			sb.ND(1,"-s (pattern)"       ,"Folder search pattern (see below)");
			sb.WT();
			sb.WT(0,"Search Pattern Info:");
			sb.WT(1,"Search Pattern can be a combination of literal and wildcard characters, but it doesn't support regular expressions. The following wildcard specifiers are permitted in the search pattern:");
			sb.ND(1,"Wildcard specifier","Matches");
			sb.ND(2,"* (asterisk)"," Zero or more characters in that position");
			sb.ND(2,"? (question mark)"," Zero or one character in that position");
			sb.WT(1,@"Characters other than the wildcard are literal characters. For example, the string ""*t"" searches for all names in ending with the letter ""t"". "". The searchPattern string ""s*"" searches for all names in path beginning with the letter ""s""");
			Log.Message(sb.ToString());
		}

		static string ActionKindDesc(ActionKind kind)
		{
			switch(kind) {
			case ActionKind.File: return "Show certificate info for given file";
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

			if (p.Default("-i",out InputFileFolder).IsInvalid()) {
				Log.Error("Invalid value for -i (file)");
			}
			if (p.Default("-s",out FilePattern).IsInvalid()) {
				Log.Error("Invalid value for -s (pattern)");
			}
			if (p.Has("-c").IsGood()) {
				IncludeChain = true;
			}
			if (p.Has("-q").IsGood()) {
				MuteCertErrors = true;
			}
			if (p.Has("-v").IsGood()) {
				ValidateCert = true;
			}
			if (p.Has("-vo").IsGood()) {
				ValidateCertOffline = true;
			}
			if (p.Has("-x").IsGood()) {
				ExportCert = true;
			}
			if (p.Has("-r").IsGood()) {
				RecurseFolders = true;
			}
			//do this last
			var left = p.Remaining();
			if (left.Length > 0) {
				ExportFile = left[0];
			}

			//sanity checks
			if (Directory.Exists(InputFileFolder)) {
				IsFolder = true;
			}
			if (!IsFolder && !File.Exists(InputFileFolder)) {
				Log.Error($"Cannot find '{InputFileFolder}'");
				return false;
			}
			if (String.IsNullOrWhiteSpace(FilePattern)) {
				FilePattern = "*";
			}

			return true;
		}
	}
}