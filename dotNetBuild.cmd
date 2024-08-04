rem dotnet build StringResourceGenerator.sln --configuration Debug --version-suffix dev --property:versioningTask-disabled=true --property:NoWarn=OAVBT46
rem dotnet pack StringResourceGenerator.sln --configuration Debug --version-suffix dev --no-restore --property:versioningTask-disabled=true --property:NoWarn=OAVBT46

dotnet build StringResourceGenerator.sln --configuration Debug --property:VersionDevSuffix=dev --property:versioningTask-disabled=true --property:NoWarn=OAVBT46
dotnet pack StringResourceGenerator.sln --configuration Debug --property:VersionDevSuffix=dev --no-restore --property:versioningTask-disabled=true --property:NoWarn=OAVBT46



pause