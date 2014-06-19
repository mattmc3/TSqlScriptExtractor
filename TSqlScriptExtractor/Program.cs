using System;
using System.IO;
using Mono.Options;

namespace SqlScriptExtractor {
	class Program {
		public static void Main(string[] args) {
			var cmd = new SqlScriptExtractor();
			cmd.Main(args);
		}
	}
	
	public class SqlScriptExtractor : CliCommand {
		protected override string CommandName {
			get { return "SqlScriptExtractor"; }
		}
	
		protected override string CommandDescription {
			get { return "Extracts T-SQL scripts from a SQL Server database and places the files in the specified location."; }
		}
	
		private string ServerName { get; set; }
		private string DatabaseName { get; set; }
		private string ScriptPath { get; set; }
	
		public SqlScriptExtractor() {
		}
	
		public override bool Main(string[] args) {
			var p = new OptionSet() {
				{ "?|help", "show this message and exit", v => this.ShowHelp = v != null },
				{ "s=|server=", "The database server", v => this.ServerName = v },
				{ "d=|db=", "The database", v => this.DatabaseName = v },
				{ "p=|scriptpath=", "The path to the root folder for scripts", v => this.ScriptPath = v },
			};
			return Run(p, args);
		}
		
		protected override void Check() {
			if (!Directory.Exists(this.ScriptPath)) {
				throw new DirectoryNotFoundException("The script path must point to an existing directory");
			}
			if (String.IsNullOrWhiteSpace(this.ServerName)) {
				throw new ArgumentNullException("The server name is not specified");
			}
			if (String.IsNullOrWhiteSpace(this.DatabaseName)) {
				throw new ArgumentNullException("The server name is not specified");
			}
			
		}
	
		protected override void Run() {
			var proc = new RefreshSqlScripts();
			var dbloc = new DbLocation(this.ServerName, this.DatabaseName);
			proc.Buffer = new ConsoleWriter();
			proc.Run(this.ScriptPath, new[] {dbloc});
		}
	}
}
