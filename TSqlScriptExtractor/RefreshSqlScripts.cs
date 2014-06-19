using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Extensions;

namespace SqlScriptExtractor {
	public class DbLocation {
		public string ServerName {
			get;
			private set;
		}
		
		public string DbName {
			get;
			private set;
		}
		
		public DbLocation(string serverName, string dbName) {
			this.ServerName = serverName;
			this.DbName = dbName;
		}
	}
	
	public class RefreshSqlScripts {

		public IWriter Buffer {
			get;
			set;
		}

		private StringBuilder _buf = new StringBuilder();
		
		public RefreshSqlScripts() {
			this.Buffer = new StringBuilderWriter(_buf);
		}

		public void Run(string outpath, IEnumerable<DbLocation> dbs) {
			foreach (var db in dbs) {
				var scripter = new Scripter(db.ServerName, db.DbName);
				RefreshScripts(outpath, scripter.GetTables(), db.DbName, "Tables");
				RefreshScripts(outpath, scripter.GetViews(), db.DbName, "Views");
				RefreshScripts(outpath, scripter.GetStoredProcedures(), db.DbName, "Stored Procedures");
				RefreshScripts(outpath, scripter.GetUserDefinedFunctions(), db.DbName, "Functions");
			}

		}

		private void RefreshScripts(string outpath, IEnumerable<SqlObject> objects, string db, string objectType) {
			var scripts = new List<string>();
			var basePath = Path.Combine(outpath, @"{0}\Create Scripts".FormatWith(db));
			var scriptDir = Path.Combine(basePath, objectType);
			if (!Directory.Exists(scriptDir)) {
				Directory.CreateDirectory(scriptDir);
			}

			foreach (var item in objects) {
				this.Buffer.WriteLine("Writing {0}: {1}", objectType, item.Name);
				var filePath = Path.Combine(scriptDir, item.Schema.Replace("\\", "_") + "." + item.Name + ".sql");
				if (item.Name.StartsWith("_")) {
					if (File.Exists(filePath)) File.Delete(filePath);
					continue;
				}
				scripts.Add(filePath);
				File.WriteAllText(filePath, item.SqlDefinition);
			}

			var deletedScripts = (from fp in Directory.GetFiles(scriptDir, "*.sql") where !scripts.Contains(fp) select fp);
			if (deletedScripts.Count() > 0) {
				this.Buffer.WriteLine("---- DELETED SCRIPTS -----");
				this.Buffer.WriteLine(String.Join(Environment.NewLine, deletedScripts));
			}
		}
	}
}
