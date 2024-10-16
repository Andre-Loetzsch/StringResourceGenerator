# dotnet-oleander-strresgen-tool
This dotnet tool generates culture-specific string resources


- dotnet tool install --global --prerelease dotnet-oleander-strresgen-tool
- dotnet tool update --global --prerelease dotnet-oleander-strresgen-tool
- dotnet tool uninstall --global dotnet-oleander-strresgen-tool


# Visual Studio External Tool
- Title: ResGen
- Command: $(ProjectDir)dotnet-tool.cmd
- Arguments: strresgen "generate -p $(ProjectFileName) -f $(ItemPath)"
- Initial directory: $(ProjectDir)

# dotnet-tool.cmd

@echo off

for /f "delims=" %%i in (%2) do (
   set "args=%%i" 
)

%1 %args%
