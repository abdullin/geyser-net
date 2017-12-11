using System.Linq;
using System.Reflection;
using eda.Terminal;

namespace eda.tool {

	public static class ConsoleUtil {
		const string title = @"
   ____                               
  / ___|  ___  _   _  ___   ___  _ __ 
 | |  _  / _ \| | | |/ __| / _ \| '__|
 | |_| ||  __/| |_| |\__ \|  __/| |   
  \____| \___| \__, ||___/ \___||_|   
               |___/                  ";

		public static void PrintArguments(object options) {
			var type = options.GetType();
			var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

			var printer = new TestWriter();

			if (props.Length == 0) return;
			var length = props.Max(p => p.Name.Length);

			printer.WriteLine("/green", title);

			printer.WriteLine("Executing", "/yellow", type.Name, "/reset", "with arguments:");
			foreach (var propertyInfo in props) {
				var name = propertyInfo.Name;
				name = name + new string(' ', length - name.Length);
				var value = propertyInfo.GetValue(options);

				printer.WriteLine("  ", "/yellow", name, "/reset", " = ", "/gray", value);
			}
		}
	}

}