param(
    [Parameter(Mandatory)]
    [string]$OutputPath,

    [Parameter(Mandatory)]
    [string]$CommonName,

    [Parameter(Mandatory)]
    [string]$Password
)

Import-Module PKI
$cert = New-SelfSignedCertificate -Type Custom -Subject $CommonName -KeyUsage DigitalSignature `
	-FriendlyName "Test MSIX Package Signing Certificate" `
	-CertStoreLocation "Cert:\CurrentUser\My" `
	-TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
	-KeyExportPolicy Exportable

$pwd = ConvertTo-SecureString $Password -AsPlainText
Export-PfxCertificate -Cert $cert -Password $pwd -FilePath $OutputPath
