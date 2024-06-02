# StringResourceGenerator
Generates culture-specific string resources

# add nuget source
dotnet nuget add source --username Andre-Loetzsch --password ********************* --store-password-in-clear-text --name oleander-github "https://nuget.pkg.github.com/Andre-Loetzsch/index.json"

# install Oleander.StrResGen.Tool
dotnet tool install --global --prerelease Oleander.StrResGen.Tool
dotnet tool update --global --prerelease Oleander.StrResGen.Tool
dotnet tool uninstall --global Oleander.StrResGen.Tool

# Visual Studio 2022 Projectfile

<Target Name="PreBuild" BeforeTargets="PreBuildEvent">
  <Exec Command="strresgen generate -p $(ProjectPath)" />
</Target>

# Visual Studio External Tool
Title:			ResGen
Command: 		$(ProjectDir)dotnet-tool.cmd
Arguments: 		strresgen "generate -p $(ProjectFileName) -f $(ItemPath)"
Initial directory:	$(ProjectDir)

# dotnet-tool.cmd

@echo off

for /f "delims=" %%i in (%2) do (
   set "args=%%i" 
)

%1 %args%