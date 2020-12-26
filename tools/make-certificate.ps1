param(
    [Parameter(Mandatory)]
    [string]$OutputPath,
)

# N.B. This must be kept in sync with the Package/Publisher/@Identity
#      attribute in src\Sunburst.WixSharpApp.AppxPackage\Package.appxmanifest
$signer_name = 'CN=382B267D-6047-4C7A-8414-C3EC3B88FF82'

Import-Module PKI -SkipEditionCheck
$cert = New-SelfSignedCertificate -Type Custom -Subject $signer_name -KeyUsage DigitalSignature `
	-FriendlyName "Test Certificate for MSI Viewer app" `
	-CertStoreLocation "Cert:\CurrentUser\My" `
	-TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") `
	-KeyExportPolicy Exportable

$Password = "1234"
$pwd = ConvertTo-SecureString $Password -AsPlainText
Export-PfxCertificate -Cert $cert -Password $pwd -FilePath $OutputPath
