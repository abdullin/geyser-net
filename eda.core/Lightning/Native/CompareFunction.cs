using System.Runtime.InteropServices;

namespace eda.Lightning.Native
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int CompareFunction(ref ValueStructure left, ref ValueStructure right);
}
