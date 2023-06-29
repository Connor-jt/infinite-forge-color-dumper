using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;


/*
 * THIS CLASS WAS COPIED FROM A WORK IN PROGRESS VERSION
 * OF THE FORGE TOOL, CODE MAY BE OUT OF DATE, UNWORKING
 * THIS CODE WILL NOT BE MAINTAINED
 */

namespace InfiniteForgeTool.code_stuff{
    internal class CMem{
        #region IMPORTS
        // IMPORTS 
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);        
        // const int PROCESS_WM_READ = 0x0010;
        // const int PROCESS_VM_WRITE = 0x0020;
        // const int PROCESS_VM_OPERATION = 0x0008;
        // const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);
        #endregion

        #region VARIABLES
        public Process? hooked_process;
        public IntPtr p_handle;
        private Dictionary<string, long>? module_table;
        #endregion

        #region CORE PROCESS FUNCTIONS
        public bool hook_and_open_process(string p_name){
            if (hooked_process != null) close_or_clear_process();

            hooked_process = Process.GetProcessesByName(p_name).FirstOrDefault();
            if (hooked_process == null) return false; // skip script if null

            //p_handle = OpenProcess(read_only? PROCESS_WM_READ : PROCESS_ALL_ACCESS, false, hooked_process.Id);
            p_handle = hooked_process.Handle; // this should be significantly more efficient
            build_modules_dict();
            return p_handle != IntPtr.Zero;
        }
        private void close_or_clear_process(){ // private unless needed otherwise
            hooked_process = null; 
            p_handle = IntPtr.Zero;
        }
        private void build_modules_dict(){
            module_table = new();
            try{for (int i = 0; i < hooked_process.Modules.Count; i++)
                    module_table.Add(hooked_process.Modules[i].ModuleName, (long)hooked_process.Modules[i].BaseAddress);
            } catch (Exception e){
                Debug.WriteLine(e.ToString());
        }}
        public bool IsHooked(){
            return (hooked_process != null && !hooked_process.HasExited);
        }
        #endregion

        #region CORE MEM FUNCTIONS
        public byte[]? read_mem(long address, int length){ // read length from address, return read mem (byte[]?)
            if (!IsHooked()) return null;

            byte[] buffer = new byte[length];
            int bytesRead = 0;
            try{if (ReadProcessMemory(p_handle, (IntPtr)address, buffer, buffer.Length, ref bytesRead))
                    return buffer;
            } catch { };
            return null;
        }
        public bool write_mem(long address, byte[] write){ // write bytes at address, return completion status (bool)
            if (!IsHooked()) return false;

            int bytesWritten = 0;
            try{return WriteProcessMemory(p_handle, (IntPtr)address, write, write.Length, ref bytesWritten);
            }catch { };
            return false;
        }
        #endregion

        // ADVANCED MEM FUNCTIONS
        #region MEM READING
        public byte? read_int8(long address){
            byte[]? read_var = read_mem(address, 1);
            return read_var != null ? read_var[0] : null;
        }
        public short? read_int16(long address){
            byte[]? read_var = read_mem(address, 2);
            return read_var != null ? BitConverter.ToInt16(read_var) : null;
        }
        public int? read_int32(long address){
            byte[]? read_var = read_mem(address, 4);
            return read_var != null ? BitConverter.ToInt32(read_var) : null;
        }
        public long? read_int64(long address){
            byte[]? read_var = read_mem(address, 8);
            return read_var != null ? BitConverter.ToInt64(read_var) : null;
        }
        public float? read_float(long address){
            byte[]? read_var = read_mem(address, 4);
            return read_var != null ? BitConverter.ToSingle(read_var) : null;
        }
        #endregion
        #region MEM WRITING
        public bool write_int8(long address, byte content){
            return write_mem(address, new byte[1] { content });
        }
        public bool write_int16(long address, short content){
            return write_mem(address, BitConverter.GetBytes(content));
        }
        public bool write_int32(long address, int content){
            return write_mem(address, BitConverter.GetBytes(content));
        }
        public bool write_int64(long address, long content){
            return write_mem(address, BitConverter.GetBytes(content));
        }
        public bool write_float(long address, float content){
            return write_mem(address, BitConverter.GetBytes(content));
        }
        #endregion

        #region POINTERS
        public long return_module_address_by_name(string module_name){ // this needs to be optimised, cycling through all 180 procs each time is prolly expensive
            long module_address;
            if (module_table.TryGetValue(module_name, out module_address))
                return module_address;
            else return -1L;
        }
        public long? read_module_pointer(string? module_base, long offset){
            if (!IsHooked()) 
                return null;

            long address_current = 0;
            if (!string.IsNullOrEmpty(module_base)){
                address_current = return_module_address_by_name(module_base);
                if (address_current == -1) 
                    return null;
            }

            return read_int64(address_current + offset);
        }
        public long? read_base_pointers(long starting_address, long[] offset){ // multilevel version
            if (!IsHooked()) 
                return null;

            long? address_current = starting_address;
            for (int i = 0; i < offset.Length; i++){
                address_current = read_int64((long)address_current);
                if (address_current == null) 
                    return null;
                address_current += offset[i];
            }
            return address_current;
        }
        #endregion

    }
}
