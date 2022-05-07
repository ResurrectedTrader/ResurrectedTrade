using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ResurrectedTrade.AgentBase.Memory
{
    [Flags]
    public enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VirtualMemoryOperation = 0x00000008,
        VirtualMemoryRead = 0x00000010,
        VirtualMemoryWrite = 0x00000020,
        DuplicateHandle = 0x00000040,
        CreateProcess = 0x000000080,
        SetQuota = 0x00000100,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        QueryLimitedInformation = 0x00001000,
        Synchronize = 0x00100000
    }

    public class RemoteMemoryAccess : MemoryAccess
    {
        private readonly Ptr _handle;
        private readonly bool _ownsHandle;

        public RemoteMemoryAccess(Ptr handle, Ptr baseAddress)
        {
            _handle = handle;
            BaseAddress = baseAddress;
            _ownsHandle = false;
        }

        public RemoteMemoryAccess(Process process)
        {
            _handle = OpenProcess(
                (uint)ProcessAccessFlags.VirtualMemoryRead, false, process.Id
            );
            BaseAddress = process.MainModule.BaseAddress;
            _ownsHandle = true;
        }

        public override Ptr BaseAddress { get; }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern Ptr OpenProcess(
            uint processAccess,
            bool bInheritHandle,
            int processId
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(Ptr hHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            Ptr hProcess,
            Ptr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out Ptr lpNumberOfBytesRead
        );

        protected override bool ReadMemory(Ptr address, ref byte[] buffer, int size)
        {
            return ReadProcessMemory(_handle, address, buffer, size, out _);
        }

        public override void Dispose()
        {
            if (_ownsHandle)
            {
                CloseHandle(_handle);
            }
        }
    }
}
