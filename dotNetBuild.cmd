


D:\dev\git\tentakeldev\NuGet\nuget.exe restore "Oleander.StrResGen.SingleFileGenerator.sln"

rem dotnet build "Oleander.StrResGen.SingleFileGenerator.sln" --configuration Debug --property:VersionDevSuffix=dev --property:GeneratePackageOnBuild=false --property:versioningTask-disabled=true

rem "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\msbuild.exe" "Oleander.StrResGen.SingleFileGenerator.sln" /p:platform="Any CPU" /p:configuration="Release" --property:VersionDevSuffix=dev --property:GeneratePackageOnBuild=false --property:versioningTask-disabled=true

rem "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\msbuild.exe" "Oleander.StrResGen.SingleFileGenerator.sln" -/p:configuration="Debug" -property:VersionDevSuffix=dev -property:GeneratePackageOnBuild=false -property:versioningTask-disabled=true



"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\msbuild.exe" "Oleander.StrResGen.SingleFileGenerator.sln" -p:configuration="Debug" -p:VersionDevSuffix=dev -p:GeneratePackageOnBuild=false -p:versioningTask-disabled=true

dotnet test "Oleander.StrResGen.SingleFileGenerator.sln" --configuration Debug --no-build 
pause

