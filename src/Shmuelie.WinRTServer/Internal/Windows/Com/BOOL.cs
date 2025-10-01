using System.Runtime.InteropServices.Marshalling;
using Shmuelie.WinRTServer.Internal.Windows.Com.Marshalling;

namespace Windows.Win32.Foundation;

[NativeMarshalling(typeof(BoolMarshaller))]
partial struct BOOL;