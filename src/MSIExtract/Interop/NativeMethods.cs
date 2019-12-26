// Copyright (c) William Kent. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace MSIExtract.Interop
{
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1601:Partial elements should be documented", Justification = "not worth it for interop code")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "not worth it for interop code")]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:Enumeration items should be documented", Justification = "not worth it for interop code")]
    internal static partial class NativeMethods
    {
        internal enum SIGDN : uint
        {
            NORMALDISPLAY = 0x00000000,
            PARENTRELATIVEPARSING = 0x80018001,
            DESKTOPABSOLUTEPARSING = 0x80028000,
            PARENTRELATIVEEDITING = 0x80031001,
            DESKTOPABSOLUTEEDITING = 0x8004c000,
            FILESYSPATH = 0x80058000,
            URL = 0x80068000,
            PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
            PARENTRELATIVE = 0x80080001,
        }

        [Flags]
        internal enum SIIGBF : int
        {
            RESIZETOFIT = 0,
            BIGGERSIZEOK = 0x1,
            MEMORYONLY = 0x2,
            ICONONLY = 0x4,
            THUMBNAILONLY = 0x8,
            INCACHEONLY = 0x10,
            CROPTOSQUARE = 0x20,
            WIDETHUMBNAILS = 0x40,
            ICONBACKGROUND = 0x80,
            SCALEUP = 0x100,
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellItem
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            object BindToHandler([In, MarshalAs(UnmanagedType.Interface)] object pbc, [In] ref Guid bhid, [In] ref Guid riid);

            void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            void GetDisplayName([In] SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

            void GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);

            void Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
        }

        [ComImport]
        [Guid("7E9FB0D3-919F-4307-AB2E-9B1860310C93")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellItem2 : IShellItem
        {
            [return: MarshalAs(UnmanagedType.Interface)]
            object GetPropertyStore(uint flags, [In] ref Guid riid);

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetPropertyStoreWithCreateObject(uint flags, [MarshalAs(UnmanagedType.IUnknown)] object punkCreateObject, [In] ref Guid riid);

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetPropertyStoreForKeys(IntPtr rgKeys, uint cKeys, uint flags, [In] ref Guid riid);

            [return: MarshalAs(UnmanagedType.Interface)]
            object GetPropertyDescriptionList(IntPtr keyType, [In] ref Guid riid);

            void Update([MarshalAs(UnmanagedType.IUnknown)] object pbc);

            void GetProperty(IntPtr key, out IntPtr pv);

            Guid GetCLSID(IntPtr key);

            System.Runtime.InteropServices.ComTypes.FILETIME GetFileTime(IntPtr key);

            int GetInt32(IntPtr key);

            [return: MarshalAs(UnmanagedType.LPWStr)]
            string GetString(IntPtr key);

            uint GetUInt32(IntPtr key);

            ulong GetUInt64(IntPtr key);

            [return: MarshalAs(UnmanagedType.Bool)]
            bool GetBool(IntPtr key);
        }

        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellItemImageFactory
        {
            void GetImage(SIZE size, SIIGBF flags, out IntPtr hBitmap);
        }

        [DllImport("user32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        public static extern void DeleteObject(IntPtr hGdiObject);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern IShellItem2 SHCreateItemFromParsingName(string pszPath, IntPtr pbc, Guid iid);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
        public static extern IntPtr ShellExecute(IntPtr hWnd, string operation, string file, string? parameters, string? directory, int nShowCmd = 10);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.LPWStr)]
        public static extern string[] CommandLineToArgvW(string commandLine, out int argc);

        internal struct SIZE
        {
            public int cx;
            public int cy;

            public SIZE(int cx, int cy)
            {
                this.cx = cx;
                this.cy = cy;
            }
        }
    }
}
