Oleander.StrResGen.SingleFileGenerator is a Visual Studio extension for easily creating culture-specific string resources. The extension is based on the dotnet tool [dotnet-oleander-strresgen-tool](https://github.com/Andre-Loetzsch/StringResourceGenerator/tree/main/Oleander.StrResGen.Tool). You can use text files with the extension ".strings" to store string resources.



## Example: 
- Create a new text file named SR.strings
- In the Properties window, set the Custom Tool attribute to **SRG**.
- Add the following lines and save the file

```
[strings]
Test(string s)=Test {0}
Raw = Raw string
StringArg(string name) = With name argument {0}
NumberArgs(int name2, decimal amount, float chanceOfWinning) = Integer {0}, Amount {1:C}, Change {2}%
MultiLineWithArgs(int age, string name) = My age is {0}
= My name is {0}

[strings.de]
Raw = German raw string
StringArg(string name) = German with name argument {0}

[strings.de-DE]
Raw = Germany raw string
StringArg(string name) = Germany with name argument {0}
```

The files **SR.cs**, **SR.srt.resx**, **SR.srt.de.resx** and **SR.srt.de-DE.resx** should be created.

## Access to the resources:

```
Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
var rawText = SR.Raw;                // Germany raw string
var stringArg = SR.StringArg("Bob"); // Germany with name argument Bob
```

