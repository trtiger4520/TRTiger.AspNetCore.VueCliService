using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.AspNetCore.SpaServices.Prerendering;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TRTiger.AspNetCore.VueCliService.Npm;
using TRTiger.AspNetCore.VueCliService.Util;

namespace TRTiger.AspNetCore.VueCliService
{
    [Obsolete("Prerendering is no longer supported out of box")]
    public class VueCliBulider : ISpaPrerendererBuilder
    {
        public static TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5);
        public readonly string NpmScriptName;

        public VueCliBulider(string npmScriptName)
        {
            NpmScriptName = npmScriptName;
        }

        public async Task Build(ISpaBuilder spaBuilder)
        {
            var sourcePath = spaBuilder.Options.SourcePath;
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new InvalidOperationException($"To use {nameof(VueCliBulider)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            var logger = LoggerFinder.GetOrCreateLogger(
                spaBuilder.ApplicationBuilder,
                nameof(VueCliBulider));
            var npmScriptRunner = new NpmScriptRunner(
                sourcePath,
                NpmScriptName,
                "--watch",
                null);
            npmScriptRunner.AttachToLogger(logger);

            using (var stdOutReader = new EventedStreamStringReader(npmScriptRunner.StdOut))
            using (var stdErrReader = new EventedStreamStringReader(npmScriptRunner.StdErr))
            {
                try
                {
                    await npmScriptRunner.StdOut.WaitForMatch(
                        new Regex("Date", RegexOptions.None, RegexMatchTimeout));
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        $"The NPM script '{NpmScriptName}' exited without indicating success.\n" +
                        $"Output was: {stdOutReader.ReadAsString()}\n" +
                        $"Error output was: {stdErrReader.ReadAsString()}", ex);
                }
                catch (OperationCanceledException ex)
                {
                    throw new InvalidOperationException(
                        $"The NPM script '{NpmScriptName}' timed out without indicating success. " +
                        $"Output was: {stdOutReader.ReadAsString()}\n" +
                        $"Error output was: {stdErrReader.ReadAsString()}", ex);
                }
            }
        }
    }
}
