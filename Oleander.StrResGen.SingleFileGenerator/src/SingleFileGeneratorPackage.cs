using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using System.Runtime.InteropServices;
using Task = System.Threading.Tasks.Task;
using System;
using System.Threading;
using System.IO;

namespace Oleander.StrResGen.SingleFileGenerator;

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(StrResGenSingleFileGeneratorPackage.PackageGuidString)]
    [ProvideCodeGenerator(typeof(StrResGenCodeGenerator), "SRG", "string resource generator", true)]
    public sealed class StrResGenSingleFileGeneratorPackage : AsyncPackage
    {
        public const string PackageGuidString = "40bd30fb-a6b5-4183-8c78-a5861a267465";

        public StrResGenSingleFileGeneratorPackage()
        {
            Log.Write("StrResGenSingleFileGeneratorPackage()");
        }

    #region Package Members

    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Log.Write("InitializeAsync");
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        #endregion
    }

    internal static class Log
    {
        internal static void Write(string text)
        {
            var path = Path.Combine(Path.GetTempPath(), "srg.log");
            //if (!File.Exists(path)) return;

            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {text}{Environment.NewLine}");
        }
    }

