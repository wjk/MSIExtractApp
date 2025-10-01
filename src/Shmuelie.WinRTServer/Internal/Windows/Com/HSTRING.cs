using System.Runtime.InteropServices.Marshalling;
using Shmuelie.WinRTServer.Internal.Windows.Com.Marshalling;

namespace Windows.Win32.System.WinRT;

[NativeMarshalling(typeof(HStringMarshaller))]
partial struct HSTRING;