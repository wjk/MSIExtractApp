param(
    [Parameter(Mandatory)]
    [string]$OutputPath,

    [Parameter(Mandatory)]
    [string]$CommonName,

    [Parameter(Mandatory)]
    [string]$Password
)

$ErrorActionPreference = 'Stop'

Import-Module PKI

if (-not (Get-PSDrive | ? { $_.Name -eq 'Cert' })) {
    New-PSDrive -Root '\' -Name Cert -PSProvider Certificate
}

try {
$cert = New-SelfSignedCertificate -Type Custom -Subject $CommonName -KeyUsage DigitalSignature `
	-FriendlyName "Test MSIX Package Signing Certificate" `
	-CertStoreLocation "Cert:\CurrentUser\My" `
	-TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
	-KeyExportPolicy Exportable

Export-PfxCertificate -Cert $cert -Password (ConvertTo-SecureString $Password -AsPlainText -Force) -FilePath $OutputPath
} catch {
    Get-Error | Write-Host
    exit 1
}
