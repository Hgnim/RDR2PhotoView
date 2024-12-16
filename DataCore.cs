using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using static RDR2PhotoView.FilePath;
using static RDR2PhotoView.DataCore;
using System.IO;

namespace RDR2PhotoView {
	internal struct PInfo {
		internal const string name = "RDR2PhotoView";
		internal const string version = "1.0.2.20241216_beta";
		internal const string githubUrl = "https://github.com/Hgnim/RDR2PhotoView";
		internal const string aboutText =
@$"程序名: RDR2照片查看器
别名: {name}
版本: V{version}
Copyright (C) 2024 Hgnim, All rights reserved.
Github: {githubUrl}";
	}
	internal struct DataCore {
		internal static MainData mainData = new() {
			First = true,
			SourceDirPath= @$"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\Rockstar Games\Red Dead Redemption 2\Profiles\",
		};
	}
	internal class MainData {
		/// <summary>
		/// 将配置数据保存至配置文件中
		/// </summary>
		internal static void SaveData() {
			ISerializer yamlS = new SerializerBuilder()
					.Build();
			
			if (!Directory.Exists(configDir))Directory.CreateDirectory(configDir);
			using StreamWriter sw = new(mainDataFile, false);
			sw.WriteLine("#注意，私自修改数据文件导致的程序错误，开发者概不负责!");
			sw.Write(yamlS.Serialize(mainData));
		}
		/// <summary>
		/// 读取数据文件并将数据写入实例中
		/// </summary>
		internal static void ReadData() {
			IDeserializer yamlD = new DeserializerBuilder()
					.Build();

			if (File.Exists(mainDataFile)) {
				using StreamReader sr = new(mainDataFile);
				mainData = yamlD.Deserialize<MainData>(sr.ReadToEnd());
			}
		}

		//bool first=true;
		/// <summary>
		/// 是否首次打开程序
		/// </summary>
		public required bool First {  get; set; }
		//string sourceDirPath = @$"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\Rockstar Games\Red Dead Redemption 2\Profiles\";
		/// <summary>
		/// 源文件夹路径
		/// </summary>
		public required string SourceDirPath {  get; set; }
	}
	internal readonly struct FilePath {
		

		internal static readonly string configDir = @$"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\{PInfo.name}\";
		internal static readonly string mainDataFile = configDir + "data.dat";

		internal static readonly string tempDir = Path.GetTempPath() + @$"{PInfo.name}\";
		internal static readonly string photoCacheDir = $@"{tempDir}\PhotoCache\";
	}
}
