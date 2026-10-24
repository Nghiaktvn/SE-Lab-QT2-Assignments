param(
    [ValidateSet('All','Ticket','Admin','Portal','Test')][string]$App='All',
    [switch]$SetupDatabase,
    [switch]$BrowserTest,
    [switch]$ButtonTest,
    [string]$SqlServer='localhost\SQLEXPRESS'
)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
function Invoke-DotNet {
    param([string[]]$Arguments)
    & dotnet @Arguments
    if($LASTEXITCODE -ne 0){throw "dotnet failed: $($Arguments -join ' ')"}
}
if($SetupDatabase){
    & sqlcmd -S $SqlServer -E -C -b -i 'Assignment1\database\01_create_database.sql'
    if($LASTEXITCODE -ne 0){throw 'Ticketing SQL setup failed.'}
    & sqlcmd -S $SqlServer -E -C -b -i 'Assignment2\database\01_create_database.sql'
    if($LASTEXITCODE -ne 0){throw 'Wholesale SQL setup failed.'}
}
if($App -eq 'Test'){
    Invoke-DotNet -Arguments @('test','Assignment1\tests\SmartTicketing.Tests\SmartTicketing.Tests.csproj')
    Invoke-DotNet -Arguments @('run','--project','Assignment1\tests\SmartTicketing.IntegrationTests','--',"$PSScriptRoot\Assignment1")
    Invoke-DotNet -Arguments @('test','Assignment2\tests\Wholesale.Tests\Wholesale.Tests.csproj')
    $integrationOutput="$PSScriptRoot\tmp\build-a2-int\"
    Invoke-DotNet -Arguments @('build','Assignment2\tests\Wholesale.IntegrationTests\Wholesale.IntegrationTests.csproj',"-p:BaseOutputPath=$integrationOutput")
    $integrationArgs=@("${integrationOutput}Debug\net10.0-windows\Wholesale.IntegrationTests.dll","$PSScriptRoot\Assignment2")
    if($BrowserTest){$integrationArgs+='--browser'}
    Invoke-DotNet -Arguments $integrationArgs
    if($ButtonTest){
        Invoke-DotNet -Arguments @('build','Assignment1\src\SmartTicketing.WinForms\SmartTicketing.WinForms.csproj')
        $ticketAudit="$PSScriptRoot\Assignment1\docs\evidence\tvm-button-audit.json"
        & "$PSScriptRoot\Assignment1\src\SmartTicketing.WinForms\bin\Debug\net10.0-windows\SmartTicketing.WinForms.exe" --button-audit $ticketAudit
        if((Get-Content -LiteralPath $ticketAudit -Raw | ConvertFrom-Json).status -ne 'PASS'){throw 'Lab 1 button audit failed.'}
        $adminOutput="$PSScriptRoot\tmp\button-test-a2\"
        Invoke-DotNet -Arguments @('build','Assignment2\src\Wholesale.AdminWinForms\Wholesale.AdminWinForms.csproj',"-p:BaseOutputPath=$adminOutput")
        if(-not $env:WHOLESALE_CAPTURE_USER){$env:WHOLESALE_CAPTURE_USER='admin'}
        if(-not $env:WHOLESALE_CAPTURE_PASSWORD){$env:WHOLESALE_CAPTURE_PASSWORD='Demo123!'}
        $adminExe="${adminOutput}Debug\net10.0-windows\Wholesale.AdminWinForms.exe"
        $loginAudit="$PSScriptRoot\Assignment2\docs\evidence\admin-login-button-audit.json"
        $adminAudit="$PSScriptRoot\Assignment2\docs\evidence\admin-button-audit.json"
        & $adminExe --login-button-audit $loginAudit
        if((Get-Content -LiteralPath $loginAudit -Raw | ConvertFrom-Json).status -ne 'PASS'){throw 'Lab 2 admin login button audit failed.'}
        & $adminExe --button-audit $adminAudit
        if((Get-Content -LiteralPath $adminAudit -Raw | ConvertFrom-Json).status -ne 'PASS'){throw 'Lab 2 admin button audit failed.'}
        Write-Host 'Native button audits passed: MetroGo, Northstar Admin login, Northstar Admin dashboard.'
    }
    return
}
if($App -in @('All','Ticket')){
    Invoke-DotNet -Arguments @('build','Assignment1\SmartTicketing.sln')
    Start-Process -FilePath "$PSScriptRoot\Assignment1\src\SmartTicketing.WinForms\bin\Debug\net10.0-windows\SmartTicketing.WinForms.exe"
}
if($App -in @('All','Admin','Portal')){Invoke-DotNet -Arguments @('build','Assignment2\WholesaleManagement.sln')}
if($App -in @('All','Admin')){Start-Process -FilePath "$PSScriptRoot\Assignment2\src\Wholesale.AdminWinForms\bin\Debug\net10.0-windows\Wholesale.AdminWinForms.exe"}
if($App -in @('All','Portal')){
    Write-Host 'Agent Portal: http://127.0.0.1:5400'
    Invoke-DotNet -Arguments @('run','--no-build','--project','Assignment2\src\AgentPortal','--urls','http://127.0.0.1:5400')
}
