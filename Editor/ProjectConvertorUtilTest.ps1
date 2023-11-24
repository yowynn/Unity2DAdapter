$TestPath='Q:\iHuman\Working\StoryBook\03_CCS\story\story_'
$TestIds='0003,0004'

Set-Location '..\..\..\..\..'
$UnityBin='C:\Program Files\Unity\Hub\Editor\2019.4.16f1\Editor\Unity.exe'
$UnityProjectPath=Get-Location
$MethodName='Unity2DAdapter.ProjectConvertorUtil.CocosToUnity'
$LogFile='Unity Command.log'
$FilePath=$UnityBin
$ArgumentList="`"$TestPath`" `"$TestIds`" -batchmode -quit -projectPath `"$UnityProjectPath`" -executeMethod `"$MethodName`" -logFile `"$LogFile`""
$Process=Start-Process -FilePath $FilePath -ArgumentList $ArgumentList -PassThru -NoNewWindow
$Process.WaitForExit()
# foreach($Property in $Process | Get-Member -MemberType Property){
#     Write-Host "$($Property.Name): $($Process.$($Property.Name))"
# }
Write-Host "Process has exited!"
