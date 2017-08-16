[CmdletBinding()]
param(
    [String]$ToofzApiBaseAddress = 'https://localhost:44300/',
    [Parameter(Mandatory = $true)]
    [String]$SteamWebApiKey,
    [String]$ToofzApiUserName = 'ReplaysService',
    [String]$ToofzApiPassword = 'password',
    [String]$StorageConnectionString = 'UseDevelopmentStorage=true',
    [Switch]$Overwrite = $false
)

. .\CredMan.ps1

function ShouldWrite-Credentials {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [String]$Target
    )

    return ($Overwrite -eq $true) -or -not((Read-Creds -Target $Target) -is [PsUtils.CredMan+Credential])
}

function Write-Credentials {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [String]$Target,
        [Parameter(ParameterSetName = 'UserName')]
        [String]$UserName,
        [Parameter(Mandatory = $true)]
        [String]$Password
    )

    if (ShouldWrite-Credentials -Target $Target) {
        switch ($PsCmdlet.ParameterSetName) {
            'UserName' { $result = Write-Creds -Target $Target -UserName $UserName -Password $Password -CredPersist LOCAL_MACHINE }
            default { $result = Write-Creds -Target $Target -Password $Password -CredPersist LOCAL_MACHINE }
        }
        if ($result -eq 0) { 
            Write-Output "Credentials for '$Target' have been saved." 
        } else { 
            Write-Output "An error code of '$result' was returned when saving credentials for '$Target'."
        }    
    } else {
        Write-Output "Credentials for '$Target' exist and -Overwrite was not specified."
    }
}

$toofzApiBaseAddress = 'toofzApiBaseAddress'
if (($Overwrite -eq $true) -or -not(Test-Path Env:\$toofzApiBaseAddress)) {
    $env:toofzApiBaseAddress = $ToofzApiBaseAddress
    Write-Output "The environment variable '$toofzApiBaseAddress' has been updated."
} else {
    Write-Output "The environment variable '$toofzApiBaseAddress' exists and -Overwrite was not specified."
}

Write-Credentials -Target 'toofz/SteamWebApiKey' -Password $SteamWebApiKey
Write-Credentials -Target 'toofz/ReplaysService' -UserName $ToofzApiUserName -Password $ToofzApiPassword
Write-Credentials -Target 'toofz/StorageConnectionString' -Password $StorageConnectionString