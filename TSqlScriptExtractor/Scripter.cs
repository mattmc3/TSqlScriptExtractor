using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Common.Extensions;

namespace SqlScriptExtractor {
	public class SqlObject {
		public string Schema { get; set; }
		public string Name { get; set; }
		public string SqlDefinition { get; set; }
	}

	public class Scripter {
		private enum SqlObjectType {
			Table,
			View,
			Procedure,
			ScalarFunction,
			TableFunction
		}

		private string _serverName;
		private string _databaseName;
		private string _userName;
		private string _password;

		public Scripter(string serverName, string databaseName, string userName = null, string password = null) {
			_serverName = serverName;
			_databaseName = databaseName;
			_userName = userName;
			_password = password;
		}

		private IEnumerable<SqlObject> GetObject(Func<Database, IEnumerable<SqlObject>> fn) {
			ServerConnection cnn = null;
			if (_userName == null) {
				cnn = new ServerConnection(_serverName);
			}
			else {
				cnn = new ServerConnection(_serverName, _userName, _password);
			}
			cnn.LoginSecure = (_userName == null);

			IEnumerable<SqlObject> result;
			try {
				var svr = new Server(cnn);
				var db = (from Database d in svr.Databases where d.Name.Equals(_databaseName, StringComparison.OrdinalIgnoreCase) select d).Single();
				result = fn(db);
			}
			catch {
				throw;
			}
			finally {
				cnn.Disconnect();
			}
			return result;
		}

		public IEnumerable<SqlObject> GetTables() {
			return GetObject((x) => GetTables(x));
		}

		private IEnumerable<SqlObject> GetTables(Database db) {
			var opts = new ScriptingOptions();
			opts.ClusteredIndexes = true;
			opts.Default = true;
			opts.DriAll = true;
			opts.Indexes = true;
			opts.AnsiPadding = false;
			opts.AllowSystemObjects = false;
			opts.Triggers = true;
			opts.ExtendedProperties = true;
			opts.IncludeHeaders = false;
			opts.NoCollation = true;
			opts.Permissions = true;

			return (
				from Table t in db.Tables
				where !t.IsSystemObject
				select new SqlObject {
					Schema = t.Schema,
					Name = t.Name,
					SqlDefinition = FixDdl(t.Schema, t.Name, String.Join(Environment.NewLine, t.Script(opts).Cast<string>()), SqlObjectType.Table)
				}).ToList();
		}

		public IEnumerable<SqlObject> GetViews() {
			return GetObject((x) => GetViews(x));
		}

		public IEnumerable<SqlObject> GetViews(Database db) {
			var opts = new ScriptingOptions();
			opts.AnsiPadding = false;
			opts.AllowSystemObjects = false;
			opts.Permissions = true;

			return (
				from View v in db.Views
				where !v.IsSystemObject
				select new SqlObject {
					Schema = v.Schema,
					Name = v.Name,
					SqlDefinition = FixDdl(v.Schema, v.Name, String.Join(Environment.NewLine, v.Script(opts).Cast<string>()), SqlObjectType.View)
				}).ToList();
		}

		public IEnumerable<SqlObject> GetUserDefinedFunctions() {
			return GetObject((x) => GetUserDefinedFunctions(x));
		}

		public IEnumerable<SqlObject> GetUserDefinedFunctions(Database db) {
			var opts = new ScriptingOptions();
			opts.AnsiPadding = false;
			opts.AllowSystemObjects = false;
			opts.Permissions = true;
			opts.IncludeHeaders = false;

			return (
				from UserDefinedFunction udf in db.UserDefinedFunctions
				where udf.ImplementationType != ImplementationType.SqlClr
				&& !udf.IsSystemObject
				let fnType = (udf.FunctionType == UserDefinedFunctionType.Scalar ? SqlObjectType.ScalarFunction : SqlObjectType.TableFunction)
				select new SqlObject {
					Schema = udf.Schema,
					Name = udf.Name,
					SqlDefinition = FixDdl(udf.Schema, udf.Name, String.Join(Environment.NewLine, udf.Script(opts).Cast<string>()), fnType)
				}).ToList();
		}

		public IEnumerable<SqlObject> GetStoredProcedures() {
			return GetObject((x) => GetStoredProcedures(x));
		}

		public IEnumerable<SqlObject> GetStoredProcedures(Database db) {
			var opts = new ScriptingOptions();
			opts.AnsiPadding = false;
			opts.AllowSystemObjects = false;
			opts.Permissions = true;

			return (
				from StoredProcedure sp in db.StoredProcedures
				where sp.ImplementationType != ImplementationType.SqlClr
				&& !sp.IsSystemObject
				select new SqlObject {
					Schema = sp.Schema,
					Name = sp.Name,
					SqlDefinition = FixDdl(sp.Schema, sp.Name, String.Join(Environment.NewLine, sp.Script(opts).Cast<string>()), SqlObjectType.Procedure)
				}).ToList();
		}

		private string FixDdl(string schema, string name, string sqlDdl, SqlObjectType type) {
			var header = new StringBuilder();

			if (type != SqlObjectType.Table) {
				header.AppendLine("use {0}".FormatWith(_databaseName));
				header.AppendLine("go");
				header.AppendLine("if objectproperty(object_id('{0}'), 'Is{1}') is null begin".FormatWith(GetSafeName(schema, name), type.ToString()));
				if (type == SqlObjectType.Procedure) {
					header.AppendLine("\texec('create proc {0} as')".FormatWith(GetSafeName(schema, name)));
				}
				else if (type == SqlObjectType.ScalarFunction) {
					header.AppendLine("\texec('create function {0}() returns int as begin return null end')".FormatWith(GetSafeName(schema, name)));
				}
				else if (type == SqlObjectType.View) {
					header.AppendLine("\texec('create view {0} as select 1 as z')".FormatWith(GetSafeName(schema, name)));
				}
				header.AppendLine("end");
				header.AppendLine("go");
			}

			var lines = sqlDdl.SplitLines();
			var result = lines.ToList();
			var offset = 0;
			var re = new Regex(@"\s* (create\s*) \s (function|proc|view)", RegexOptionsHelper.GetRegexOptions("xmsi"));
			for (var i = 0; i < lines.Length; i++) {
				var line = lines[i];
				if (line == "SET ANSI_NULLS ON" || line == "SET QUOTED_IDENTIFIER ON") {
					// ditch it
					result.RemoveAt(i + offset);
					offset -= 1;
				}
				else if (re.IsMatch(line)) {
					result[i + offset] = re.ReplaceSubstringMatch(line, "alter", 1);
				}
				else if (line.StartsWith("GRANT EXECUTE ON ", StringComparison.OrdinalIgnoreCase)) {
					// add a "go" before this statement
					result.Insert(i + offset, "go");
					offset++;
				}
			}
			if (result[result.Count - 1] == "") {
				result.Insert(result.Count - 1, "go");
			}
			else {
				result.Add("go");
				result.Add("");
			}
			return header.ToString() + String.Join(Environment.NewLine, result);
		}

		private string GetSafeName(string schema, string name) {
			return schema + "." + name;
		}
	}
}
