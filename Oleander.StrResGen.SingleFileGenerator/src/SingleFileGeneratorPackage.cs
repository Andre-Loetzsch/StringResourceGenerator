using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace Oleander.StrResGen.SingleFileGenerator
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(StrResGenSingleFileGeneratorPackage.PackageGuidString)]
    [ProvideCodeGenerator(typeof(StrResGenCodeGenerator), "SRG", "string resource generator", true)]
    public sealed class StrResGenSingleFileGeneratorPackage : AsyncPackage
    {
        public const string PackageGuidString = "40bd30fb-a6b5-4183-8c78-a5861a267465";

        #region Package Members
   
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        #endregion
    }
}
