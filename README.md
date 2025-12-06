# About
**Sharp.Exceptions** is a .NET library that extends and integrates with [Plus.Exceptions](https://github.com/Feralnex/Plus.Exceptions) to provide robust, platform-aware exception handling. It simplifies error interception, caching, and message retrieval from native libraries, ensuring developers can work with meaningful exceptions across different environments.

## Features
- **Platform-aware exceptions**: Wraps native error codes into managed `PlatformException` objects.
- **Error interception**: Captures both general and socket-related error codes directly from unmanaged exports.
- **Message resolution**: Retrieves human-readable error messages from native pointers.
- **Automatic cleanup**: Deletes native error messages when required, preventing memory leaks.
- **Caching**: Maintains a thread-safe cache of exceptions for reuse and consistency.

## Dependency
Sharp.Exceptions depends on **Plus.Exceptions**, which provides the native interop layer and exports required for error handling.

## Core Class: `PlatformException`
The `PlatformException` class extends `System.Exception` and encapsulates platform-specific error handling logic.

### Key Components
- **Delegates to native functions**
  - `GetErrorCode`: Retrieves the last error code.
  - `GetSocketErrorCode`: Retrieves the last socket-related error code.
  - `TryGetErrorMessage`: Attempts to resolve an error code into a native error message.
  - `ShouldDeleteErrorMessage`: Determines if native error messages should be freed.
  - `DeleteErrorMessage`: Cleans up native error message memory.

- **Cache**
  - A `ConcurrentDictionary<int, PlatformException>` ensures exceptions are reused for identical error codes.

- **Gate**
  - Controls whether native error messages should be deleted after retrieval.

### Constructors
- `PlatformException(int code)`  
  Creates an exception with a resolved message from the given error code.

- `PlatformException(int code, string message)`  
  Creates an exception with a custom message.

### Static Methods
- `FromCode(int code)`  
  Returns a cached `PlatformException` for the given error code, creating one if necessary.

- `Intercept(out int code, bool socketRelated = false)`  
  Retrieves the latest error code (general or socket-related) and its message.

- `GetMessage(int code)`  
  Resolves an error code into a human-readable message, deleting native memory if required.

## Usage Example
```csharp
try
{
    // Intercept the latest error
    int code;
    string message = PlatformException.Intercept(out code);

    throw new PlatformException(code, message);
}
catch (PlatformException ex)
{
    Console.WriteLine($"Error Code: {ex.Code}, Message: {ex.Message}");
}
```