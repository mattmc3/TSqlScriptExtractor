using System;
using System.Collections.Generic;
using System.Linq;
using Common.Extensions;
using Mono.Options;

public abstract class CliCommand {
	protected bool ShowHelp { get; set; }
	protected abstract string CommandName { get; }
	protected abstract string CommandDescription { get; }

	public abstract bool Main(string[] args);
	protected abstract void Run();
	
	protected virtual void Check() {
	}

	protected virtual bool Run(OptionSet opts, string[] args) {
		List<string> extra = null;
		try {
			extra = opts.Parse(args);
		}
		catch (OptionException e) {
			HandleException(e);
			return false;
		}

		if (this.ShowHelp) {
			Help(opts);
			return true;
		}

		try {
			Check();
			Run();
		}
		catch (Exception e) {
			HandleException(e);
			return false;
		}

		return true;
	}

	protected virtual void HandleException(Exception e) {
		Write("{0}: ".FormatWith(CommandName));
		WriteLine(e.Message);
		  WriteLine("-----");
		  WriteLine(e.StackTrace);
		WriteLine("Try `{0} --help' for more information.".FormatWith(CommandName));
	}

	protected virtual void Help(OptionSet opts) {
		WriteLine("Usage: {0} [OPTIONS]+".FormatWith(CommandName));
		WriteLine(this.CommandDescription);
		WriteLine();
		WriteLine("Options:");
		opts.WriteOptionDescriptions(Console.Out);
	}

	protected virtual void Write(string text = null) {
		Console.Write(text);
	}

	protected virtual void Write(string text, params object[] format) {
		Console.Write(text, format);
	}

	protected virtual void WriteLine(string text = null) {
		Console.WriteLine(text);
	}

	protected virtual void WriteLine(string text, params object[] format) {
		Console.WriteLine(text, format);
	}
}
