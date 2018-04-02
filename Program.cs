using System;
using System.Windows.Forms;

namespace FameBase
{
	static class Program
	{
		private static Interface formMain = null;
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Program.formMain = new Interface();
			Application.Run(Program.formMain);
		}
		static public Interface GetFormMain()
		{
			return Program.formMain;
		}

        public static void writeToConsole(string s)
        {
            Program.formMain.writeToConsole(s);
        }
	}
}
