using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe
{
    var nintMax = nint.MaxValue;

    var pointer = (void*)nintMax;

    Console.WriteLine("{0:x}", (nuint)pointer);

    var mmapNint = mmap.nint();

    Console.WriteLine("Allocated nint pointer: 0x{0:x} ({1})", mmapNint, mmapNint);

    var shiftedDown = (mmapNint >> ((sizeof(nint) * 8) - 1));

    // Check if high bit is set.
    if ((shiftedDown & 1) == 1)
    {
        Console.WriteLine("Top bit of pointer is set");
    }
    else
    {
        Console.WriteLine("Top bit of pointer is not set");
    }

    Console.WriteLine("Casting to void*...");

    try
    {
        var voidPointer = (void*)mmapNint;

        // On .NET6 and up, this will fail, as the earlier cast to void* is optimised out for a direct cast to checked((UIntPtr)mmapNint).
        Console.WriteLine("Successfully converted to void* pointer: 0x{0:x}", (nuint)voidPointer);
    } 
    catch (OverflowException)
    {
        Console.WriteLine("Failed to convert to void* pointer; saw overflow exception due to checked pointer casting");
    }

    var mmapSafeHandle = mmap.safehandle();

    Console.WriteLine("Allocated safehandle: 0x{0:x}", mmapSafeHandle.GetHandle());

    var shiftedDownHandle = (mmapSafeHandle.GetHandle() >> ((sizeof(nint) * 8) - 1));

    // Check if high bit is set.
    if ((shiftedDownHandle & 1) == 1)
    {
        Console.WriteLine("Top bit of pointer is set");
    }
    else
    {
        Console.WriteLine("Top bit of pointer is not set");
    }


    Console.WriteLine("Casting safehandle pointer to void*...");

    try
    {
        var voidPointer = mmapSafeHandle.GetPointer();

        // On .NET6 this will succeed, as the GetPointer method does a straight cast to void* which is not checked.
        // On .NET8 this will fail, as the GetPointer method does a checked cast to void*.
        Console.WriteLine("Successfully safehandle converted to void* pointer: 0x{0:x}", (nuint)voidPointer);
    }
    catch (OverflowException)
    {
        Console.WriteLine("Failed to convert to void* pointer; saw overflow exception due to checked pointer casting");
    }
}

static class mmap
{
    // mmap prot and flags const definitions:
    const int PROT_READ = 0x1;
    const int PROT_WRITE = 0x2;

    const int MAP_PRIVATE = 0x02;
    const int MAP_ANONYMOUS = 0x20;

    public static MySafeHandle safehandle() => mmap_safehandle(IntPtr.Zero, 32, PROT_READ | PROT_WRITE, MAP_ANONYMOUS | MAP_PRIVATE, -1, 0);

    public static nint nint() => mmap_nint(IntPtr.Zero, 32, PROT_READ | PROT_WRITE, MAP_ANONYMOUS | MAP_PRIVATE, -1, 0);

    // Pinvoke signature for linux mmap function
    [DllImport("libc", SetLastError = true, EntryPoint = "mmap")]
    static extern MySafeHandle mmap_safehandle(IntPtr addr, int length, int prot, int flags, int fd, nuint offset);

    // Pinvoke signature for linux mmap function
    [DllImport("libc", SetLastError = true, EntryPoint = "mmap")]
    static extern nint mmap_nint(IntPtr addr, int length, int prot, int flags, int fd, nuint offset);
}

unsafe class MySafeHandle : SafeHandle
{
    public MySafeHandle() : base(IntPtr.Zero, true)
    {
    }

    public override bool IsInvalid => false;

    public nint GetHandle() => handle;

    // This throws an exception on .NET8, but not on .NET6.
    public void* GetPointer() => (void*)handle;

    protected override bool ReleaseHandle()
    {
        return true;
    }
}
