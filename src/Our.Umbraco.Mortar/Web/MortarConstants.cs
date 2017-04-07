using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Umbraco.Core.IO;

namespace Our.Umbraco.Mortar.Web
{
	public static class MortarConstants
	{
		public const string PackageNameAlias = "Our.Umbraco.Mortar";

		private static Version _applicationVersion;
		public static Version ApplicationVersion
		{
			get
			{
				if (_applicationVersion == null)
				{
					var assembly = Assembly.GetExecutingAssembly();
					if (assembly != null)
					{
						var info = FileVersionInfo.GetVersionInfo(assembly.Location);
						if (info != null && !string.IsNullOrWhiteSpace(info.ProductVersion))
						{
							Version.TryParse(info.ProductVersion, out _applicationVersion);
						}
					}
				}

				return _applicationVersion;
			}
		}

		private static Version _currentVersion;
		public static Version CurrentVersion
		{
			get
			{
				var path = IOHelper.MapPath("~/App_Plugins/Mortar/version");
				if (File.Exists(path))
				{
					var version = File.ReadAllText(path);
					if (!string.IsNullOrWhiteSpace(version))
					{
						Version.TryParse(version, out _currentVersion);
					}
				}

				return _currentVersion;
			}
		}
	}
}