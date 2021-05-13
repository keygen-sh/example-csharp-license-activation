# Example C# Machine Activation

This is an example of a typical machine activation flow written in C# and .NET.
You may of course choose to implement a different flow if required - this
only serves as an example implementation.

## Running the example

First, install dependencies with [`dotnet`](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet):

```
dotnet restore
```

Then run the program:

```
dotnet run
```

You should see log output indicating the current device was activated:

```
[INFO] [ValidateLicense] Invalid=fingerprint scope does not match any associated machines ValidationCode=FINGERPRINT_SCOPE_MISMATCH
[INFO] [ActivateDevice] DeviceId=ae09c0a5-8c59-4c11-b745-5ac994d9fcc6 LicenseId=c460da8d-1b5a-44f7-8a74-9eec429876ec
[INFO] [ValidateLicense] Valid=is valid ValidationCode=VALID
[INFO] [Main] Valid=True RecentlyActivated=True
```

Subsequent runs will indicate the device is already activated:

```
[INFO] [ValidateLicense] Valid=is valid ValidationCode=VALID
[INFO] [Main] Valid=True RecentlyActivated=False
```

## Questions?

Reach out at [support@keygen.sh](mailto:support@keygen.sh) if you have any
questions or concerns!
