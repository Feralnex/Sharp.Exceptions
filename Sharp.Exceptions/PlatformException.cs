using Sharp.Exceptions.Localization;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Sharp.Helpers;

namespace Sharp.Exceptions
{
    public class PlatformException : Exception
    {
        private static unsafe delegate* unmanaged[Cdecl]<int> GetErrorCode { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int> GetSocketErrorCode { get; }
        private static unsafe delegate* unmanaged[Cdecl]<int, sbyte**, bool> TryGetErrorMessage { get; }
        private static unsafe delegate* unmanaged[Cdecl]<bool> ShouldDeleteErrorMessage { get; }
        private static unsafe delegate* unmanaged[Cdecl]<sbyte*, void> DeleteErrorMessage { get; }

        private static Gate _gate;

        protected static ConcurrentDictionary<int, PlatformException> Cache { get; }

        public int Code { get; }

        public PlatformException(int code) : base(GetMessage(code))
        {
            Code = code;
        }

        public PlatformException(int code, string message) : base(message)
        {
            Code = code;
        }

        static unsafe PlatformException()
        {
            nint getErrorCodePointer = Library.GetExport(nameof(Exceptions), nameof(GetErrorCode));
            nint getSocketErrorCodePointer = Library.GetExport(nameof(Exceptions), nameof(GetSocketErrorCode));
            nint tryGetErrorMessagePointer = Library.GetExport(nameof(Exceptions), nameof(TryGetErrorMessage));
            nint shouldDeleteErrorMessagePointer = Library.GetExport(nameof(Exceptions), nameof(ShouldDeleteErrorMessage));
            nint deleteErrorMessagePointer = Library.GetExport(nameof(Exceptions), nameof(DeleteErrorMessage));

            GetErrorCode = (delegate* unmanaged[Cdecl]<int>)getErrorCodePointer;
            GetSocketErrorCode = (delegate* unmanaged[Cdecl]<int>)getSocketErrorCodePointer;
            TryGetErrorMessage = (delegate* unmanaged[Cdecl]<int, sbyte**, bool>)tryGetErrorMessagePointer;
            ShouldDeleteErrorMessage = (delegate* unmanaged[Cdecl]<bool>)shouldDeleteErrorMessagePointer;
            DeleteErrorMessage = (delegate* unmanaged[Cdecl]<sbyte*, void>)deleteErrorMessagePointer;

            bool shouldDeleteErrorMessage = ShouldDeleteErrorMessage();
            
            _gate = new Gate();
            Cache = new ConcurrentDictionary<int, PlatformException>();

            if (shouldDeleteErrorMessage)
                _gate.Open();
        }

        public static PlatformException FromCode(int code)
        {
            if (!Cache.TryGetValue(code, out PlatformException? exception))
            {
                exception = new PlatformException(code);

                Cache.TryAdd(code, exception);
            }

            return exception;
        }

        public static unsafe string Intercept(out int code, bool socketRelated = false)
        {
            if (socketRelated)
                code = GetSocketErrorCode();
            else
                code = GetErrorCode();

            return GetMessage(code);
        }

        public static unsafe string GetMessage(int code)
        {
            sbyte* pointer = default;

            if (TryGetErrorMessage(code, &pointer))
            {
                string message = Marshal.PtrToStringAnsi((nint)pointer)!;

                _gate.IfOpen(DeleteNativeErrorMessage, (nint)pointer);

                return message;
            }

            return string.Format(ExceptionMessages.NoSystemMessageFound, code);
        }

        private static unsafe void DeleteNativeErrorMessage(nint pointer)
            => DeleteErrorMessage((sbyte*)pointer);
    }
}
