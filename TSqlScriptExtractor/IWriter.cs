using System;
using System.Text;

namespace SqlScriptExtractor {
	public interface IWriter {
		void Write(string s, params object[] args);
		void WriteLine(string s, params object[] args);
	}
	
	public class StringBuilderWriter : IWriter {
		public StringBuilder Buffer {
			get;
			private set;
		}
		
		public StringBuilderWriter(StringBuilder buf) {
			this.Buffer = buf;
		}
		
		public void Write(string s, params object[] args) {
			this.Buffer.Append(String.Format(s, args));
		}
		
		public void WriteLine(string s, params object[] args) {
			this.Buffer.AppendLine(String.Format(s, args));
		}
	}
	
	public class ConsoleWriter : IWriter {
		public ConsoleWriter() {
		}
		
		public void Write(string s, params object[] args) {
			Console.Write(String.Format(s, args));
		}
		
		public void WriteLine(string s, params object[] args) {
			Console.WriteLine(String.Format(s, args));
		}
	}
}
