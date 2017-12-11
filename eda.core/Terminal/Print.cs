namespace eda.Terminal {

	public static class Print {
		// Returns the human-readable file size for an arbitrary, 64-bit file size 
		// The default format is "0.### XB", e.g. "4.2 KB" or "1.434 GB"
		public static string Bytes(long i) {
			// Get absolute value
			long absoluteI = (i < 0 ? -i : i);
			// Determine the suffix and readable value
			string suffix;
			double readable;
			if (absoluteI >= 0x1000000000000000) // Exabyte
			{
				suffix = "EiB";
				readable = (i >> 50);
			} else if (absoluteI >= 0x4000000000000) // Petabyte
			{
				suffix = "PiB";
				readable = (i >> 40);
			} else if (absoluteI >= 0x10000000000) // Terabyte
			{
				suffix = "TiB";
				readable = (i >> 30);
			} else if (absoluteI >= 0x40000000) // Gigabyte
			{
				suffix = "GiB";
				readable = (i >> 20);
			} else if (absoluteI >= 0x100000) // Megabyte
			{
				suffix = "MiB";
				readable = (i >> 10);
			} else if (absoluteI >= 0x400) // Kilobyte
			{
				suffix = "KiB";
				readable = i;
			} else {
				return i.ToString("0 B"); // Byte
			}
			// Divide by 1024 to get fractional value
			readable = (readable / 1024);
			if (readable < 10) {
				return readable.ToString("0.## ") + suffix;
			} else {
				return readable.ToString("0.# ") + suffix;
			}

			// Return formatted number with suffix
			
		}
	}

}