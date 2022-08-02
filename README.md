# YK-Enroll

Simple application to manage certificates and related operations for the PIV function on YubiKeys.

Using the [Yubico YubiKey .NET SDK](https://github.com/Yubico/Yubico.NET.SDK) for access to the YubiKey.
The YKEnroll.Lib\Yubico folder includes the KeyConverter and dependent classes from Yubico's [sample code](https://github.com/Yubico/Yubico.NET.SDK/tree/develop/Yubico.YubiKey/examples/PivSampleCode) for converting public keys between Yubico's PivPublicKey and .NET AsymmetricAlgorithm.

## Current features:
* Enroll wizard (Create keypair, generate csr, request and import certificate from Windows CA).
* Generate csr to file.
* Request certificate from Windows CA.
* Import certificates.
* Enable/disable YubiKey applications (eg. PIV, OTP, FIDO2...).
* Change PIN/PUK/Management key.

## Screenshots
![Screenshot1](/screenshot1.png?raw=true "Main Window screen")
![Screenshot2](/screenshot2.png?raw=true "Enroll Window")

## Attributions
    Icons from icons8.com and materialdesignicons.com.

## License
    Licensed under the MIT license. The included sample code from Yubico located in YKEnroll.Lib\Yubico is licensed under the Apache 2.0 license.
