using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ScintillaNET.Demo {
	static class Program {
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() {
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Catch unhandled exceptions on the UI thread
			Application.ThreadException += Application_ThreadException;

			// Catch unhandled exceptions on non-UI threads
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			// Suppress the default .NET unhandled exception dialog
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

			Application.Run(new MainForm());
		}

		private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
		{
			// Let truly fatal exceptions crash — the runtime is unreliable after these
			if (e.Exception is OutOfMemoryException || e.Exception is StackOverflowException)
			{
				MessageBox.Show("Fatal error: " + e.Exception.Message, "Fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Application.Exit();
				return;
			}

			MainForm form = Application.OpenForms.Count > 0 ? Application.OpenForms[0] as MainForm : null;
			if (form != null)
			{
				form.ShowError("Unexpected error:", e.Exception);
			}
			else
			{
				MessageBox.Show(e.Exception.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = e.ExceptionObject as Exception;
			MainForm form = Application.OpenForms.Count > 0 ? Application.OpenForms[0] as MainForm : null;
			if (form != null && ex != null)
			{
				form.ShowError("Fatal error:", ex);
			}
			else
			{
				string msg = ex != null ? ex.Message : "Unknown error";
				MessageBox.Show(msg, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}
