using System.Diagnostics;

namespace ScryForge.Services
{
    public class PDFService
    {
        public async Task RunAsync(string project)
        {
            var exe = AppConfig.PDFExe;
            if (!File.Exists(exe))
            {
                Console.WriteLine($"PDF Service executable missing at: {exe}");
                return;
            }

            var workingDir = Path.GetDirectoryName(exe);
            string projectFile = $"{project}.json";
            string arguments = $"/c \"{exe} --render --project {projectFile}\"";

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"--render --project \"{project}.json\"",
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.Error.WriteLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (workingDir == null)
            {
                throw new InvalidOperationException("workingDir cannot be null.");
            }

            var printMeFile = Path.Combine(workingDir, "_printme.pdf");

            var projectPdf = Path.Combine(workingDir, $"{project}.pdf");

            if (File.Exists(printMeFile))
            {
                if (File.Exists(projectPdf))
                {
                    File.Delete(projectPdf);
                }
                File.Move(printMeFile, projectPdf);
                Console.WriteLine($"PDF renamed to: {projectPdf}");
            }
            else
            {
                Console.WriteLine("Cannot find _printme.pdf to rename.");
            }

            Console.WriteLine($"PDF Service finished with exit code {process.ExitCode}");
        }
    }
}