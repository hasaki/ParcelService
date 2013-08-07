using System;
using System.Diagnostics;
using System.IO;
using Config = System.Configuration.ConfigurationManager;

namespace Asp.Net.WebDeployer
{
	static class Program
	{
		private const string PathToCompressionAppToken = "PathToCompressionApp ";
		private const string DirectoryToWatchToken = "DirectoryToWatch";
		private const string PatternToWatchForToken = "PatternToWatchFor";
		private const string TemporaryFileNameWhileDeployingToken = "TemporaryFileNameWhileDeploying";
		private const string DeploymentKeyToken = "DeploymentKey";
		private const string AutoWebDeployDirectoryName = "AutoWebDeploy";
		private const string WebDeployCommandLine = @"C:\Windows\System32\cmd.exe & /V:ON /K cd ""C:\Program Files\IIS\Microsoft Web Deploy V2\""&set path=!PATH!;C:\Program Files\IIS\Microsoft Web Deploy V2\";

		public static void Main()
		{
			var files = GetFiles();
			var temporaryFileName = Config.AppSettings[TemporaryFileNameWhileDeployingToken];
			var temporaryDirectory = Path.Combine(Path.GetTempPath(), AutoWebDeployDirectoryName);

			if (files.Length <= 0)
				return;

			Directory.CreateDirectory(temporaryDirectory);

			// keep running until there are no more files in the directory
			while (files.Length > 0)
			{
				foreach (var file in files)
				{

					// rename the file!
					var directoryName = Path.GetDirectoryName(file);

					var extension = Path.GetExtension(file);
					var temporaryFile = string.Concat(temporaryFileName, extension);

					Debug.Assert(directoryName != null, "directoryName != null");
					var tempFile = Path.Combine(directoryName, temporaryFile);
					File.Move(file, tempFile);

					ExtractFiles(tempFile, temporaryDirectory);
					DeployWebSite(temporaryDirectory);
					File.Delete(tempFile);
				}

				files = GetFiles();

			} // end while

			Directory.Delete(temporaryDirectory, true);
		}

		private static string[] GetFiles()
		{
			return Directory.GetFiles(DirectoryToWatch, PatternToWatchFor);
		}

		private static void ExtractFiles(string fileName, string extractToDirectory)
		{
			var pathToCompressionApp = Config.AppSettings[PathToCompressionAppToken];
			var password = Config.AppSettings[DeploymentKeyToken];
			var args = string.Format("{0} x {1} -o{2} -p{3}", pathToCompressionApp, fileName, extractToDirectory, password);
			Process.Start(args);
		}

		private static void DeployWebSite(string deploymentDirectoryPath)
		{
			var files = Directory.GetFiles(deploymentDirectoryPath, "*.deploy.cmd");
			if (files.Length == 0)
				throw new FileNotFoundException("Deployment command file not found");

			if (files.Length > 0)
				throw new InvalidOperationException("Multiple deployment files found: \n\n" + string.Join("\n", files));

			Process.Start(WebDeployCommandLine, "/Y " + files[0]);

		}

		private static string DirectoryToWatch
		{
			get { return Config.AppSettings[DirectoryToWatchToken]; }
		}

		private static string PatternToWatchFor
		{
			get { return Config.AppSettings[PatternToWatchForToken]; }
		}

	}
}
