using System.Runtime.InteropServices;
using eda.Lightning.Native;

namespace eda.Lightning
{
    /// <summary>
    /// Represents lmdb version information.
    /// </summary>
    public class LightningVersionInfo
    {
        internal static LightningVersionInfo Get()
        {
            int minor, major, patch;
            var version = Lmdb.mdb_version(out major, out minor, out patch);

            return new LightningVersionInfo
            {
                Version = Marshal.PtrToStringAnsi(version),
                Major = major,
                Minor = minor,
                Patch = patch
            };
        }

        private LightningVersionInfo()
        {}

        /// <summary>
        /// Major version number.
        /// </summary>
        public int Major { get; private set; }

        /// <summary>
        /// Minor version number.
        /// </summary>
        public int Minor { get; private set; }

        /// <summary>
        /// Patch version number.
        /// </summary>
        public int Patch { get; private set; }

        /// <summary>
        /// Version string.
        /// </summary>
        public string Version { get; private set; }
    }
}
