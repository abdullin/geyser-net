using System;
using System.IO;

namespace eda.Terminal {

	public abstract class ConsoleSyntax {
		public string Action;
		public string Description;

		protected readonly TestWriter Printer = new TestWriter();
		public abstract void Run();

	}

	public sealed class TestWriter {
		readonly int _width;

		public TestWriter() {
			try {
				_width = Console.WindowWidth;
			} catch (IOException) {
				_width = 80;
			}
		}



		public void Write(params object[] args) {
			var init = Console.ForegroundColor;
			foreach (var o in args) {
				var s = o as string;
				ConsoleColor color;
				if (s != null && s.StartsWith("/") && Enum.TryParse(s.Trim('/'), true, out color)) {

					Console.ForegroundColor = color;

					continue;
				}
				if (s == "/reset") {
					Console.ForegroundColor = init;
					continue;
				}
				Console.Write(o);
				Console.Write(" ");
			}

			Console.ForegroundColor = init;
		}


		public void Hr(char c) {
			Console.WriteLine(new string(c, _width - 1));
		}

		public void WriteLine(params object[] args) {
			Write(args);
			Console.WriteLine();
		}
	}

}