using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Config = System.Configuration.ConfigurationManager;

namespace Asp.Net.WebDeployer
{
	static class Program
	{
		private const string PathToCompressionAppToken = "PathToCompressionApp";
		private const string DirectoryToWatchToken = "DirectoryToWatch";
		private const string PatternToWatchForToken = "PatternToWatchFor";
		private const string TemporaryFileNameWhileDeployingToken = "TemporaryFileNameWhileDeploying";
		private const string DeploymentKeyToken = "DeploymentKey";
		private const string AutoWebDeployDirectoryName = "AutoWebDeploy";

		public static void Main()
		{
			var files = GetFiles();
			if (files.Length <= 0)
				return;

			var temporaryFileName = Config.AppSettings[TemporaryFileNameWhileDeployingToken];
			var temporaryDirectory = Path.Combine(Path.GetTempPath(), AutoWebDeployDirectoryName);
			if (!Directory.Exists(temporaryDirectory))
				Directory.CreateDirectory(temporaryDirectory);

			// keep running until there are no more files in the directory
			while (files.Length > 0)
			{
				foreach (var file in files)
				{

					// rename the file!
					var directoryName = Path.GetDirectoryName(file);

					Debug.Assert(directoryName != null, "directoryName != null");
					var tempFile = Path.Combine(directoryName, temporaryFileName);
					if (File.Exists(tempFile))
						File.Delete(tempFile);

					File.Move(file, tempFile);

					ExtractFiles(tempFile, temporaryDirectory);
					DeployWebSite(temporaryDirectory);

					File.Delete(tempFile);
				}

				files = GetFiles();

			} // end while

		}

		private static string[] GetFiles()
		{
			var directoryToWatch = DirectoryToWatch;
			return Directory.Exists(directoryToWatch) ? 
				Directory.GetFiles(directoryToWatch, Config.AppSettings[PatternToWatchForToken]) : 
				new string[0];
		}

		private static void ExtractFiles(string fileName, string extractToDirectory)
		{
			Process.Start(new ProcessStartInfo(Config.AppSettings[PathToCompressionAppToken])
				{
					Arguments = string.Format("e {0} -y -o{1} -p{2}", fileName, extractToDirectory, Config.AppSettings[DeploymentKeyToken])
				});

			// wait for the files to be extracted
			Thread.Sleep(1000);
		}

		private static void DeployWebSite(string deploymentDirectoryPath)
		{
			var files = Directory.GetFiles(deploymentDirectoryPath, "*.deploy.cmd");
			if (files.Length == 0)
				throw new FileNotFoundException("Deployment command file not found");

			if (files.Length > 1)
				throw new InvalidOperationException("Multiple deployment files found: \n\n" + string.Join("\n", files));

			Process.Start(new ProcessStartInfo(@"C:\Windows\System32\cmd.exe")
				{
					Arguments = string.Format(@"/V:ON /K cd ""C:\Program Files\IIS\Microsoft Web Deploy V2\""&set path=!PATH!;C:\Program Files\IIS\Microsoft Web Deploy V2\ ""{0}"" /Y", files[0])
				});

		}

		private static string DirectoryToWatch
		{
			get { return Config.AppSettings[DirectoryToWatchToken]; }
		}
	}
}
