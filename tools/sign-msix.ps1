param(
	[Parameter(Mandatory)]
	$Path
)

signtool sign /a /fd SHA256 /n '382B267D-6047-4C7A-8414-C3EC3B88FF82' $Path