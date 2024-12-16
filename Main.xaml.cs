using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static RDR2PhotoView.DataCore;
using System.IO;
using Path = System.IO.Path;
using MessageBox = System.Windows.MessageBox;
using static RDR2PhotoView.Main;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using EasyUpdateFromGithub;
//using DialogResult = System.Windows.Forms.DialogResult;

namespace RDR2PhotoView {
	public partial class Main : Window {
		public Main() {
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			MainData.ReadData();
			if (mainData.First) {
				try {
					mainData.SourceDirPath = Directory.GetDirectories(mainData.SourceDirPath)[0];//尝试自动获取路径
					mainData.First = false;
				} catch { }
			}
			if (!Directory.Exists(FilePath.photoCacheDir))
				Directory.CreateDirectory(FilePath.photoCacheDir);

			LoadImages();

			ProgramUpdate();
		}
		private void Window_Closed(object sender, EventArgs e) {
			MainData.SaveData();
		}

		List<ImageInfo> imageInfo = [];
		List<string> sourceFiles = [];
		List<string> cacheFiles = [];
		/// <summary>
		/// 加载图像
		/// </summary>
		/// <param name="reloadCache">是否重载缓存</param>
		private void LoadImages(bool reloadCache = true) {
			if (reloadCache) {
				photoList.ItemsSource = null;
				photoList.Items.Clear();
				sourceFiles = [];//源文件路径			
				{
					string[] allFiles = Directory.GetFiles(mainData.SourceDirPath);
					foreach (string sf in allFiles) {//过滤出目标文件的地址
						if (Path.GetFileName(sf)[..4] == "PRDR") {
							sourceFiles.Add(sf);
						}
					}
				}
				cacheFiles = [];//经过处理的临时文件路径
				{
					foreach (string sf in sourceFiles) {
						cacheFiles.Add($"{FilePath.photoCacheDir}{Path.GetFileName(sf)}.jpg");
					}
				}
				for (int i = 0; i < sourceFiles.Count; i++) {
					byte[] sourceBs = File.ReadAllBytes(sourceFiles[i]);
					byte[] cacheBs = new byte[sourceBs.Length - 300];
					for (int j = 300; j < sourceBs.Length; j++) {
						cacheBs[j - 300] = sourceBs[j];
					}
					File.WriteAllBytes(cacheFiles[i], cacheBs);
				}
			}

			{
				imageInfo = [];
				foreach (string cf in cacheFiles) {
					imageInfo.Add(new() { Path = cf });
				}
			}

			photoList.ItemsSource = imageInfo;
			if (photoList.Items.Count > 0) {
				exportAll.IsEnabled = true;
			}
			else {
				exportAll.IsEnabled = false;
			}
		}
		public class ImageInfo {
			public required string Path { get; set; }
			public ImageSource ImageSource {
				get {
					var bitmap = new BitmapImage();
					using (var stream = File.OpenRead(Path)) {
						bitmap.BeginInit();
						bitmap.CacheOption = BitmapCacheOption.OnLoad;
						bitmap.StreamSource = stream;
						bitmap.EndInit();
					}
					return bitmap;
				}
			}
		}

		private void PhotoList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (photoList.SelectedIndex != -1) {
				photoView.Source = imageInfo[photoList.SelectedIndex].ImageSource;

				exportSelected.IsEnabled = true;
				deleteSelected.IsEnabled = true;
			}
			else {
				photoView.Source = null;

				exportSelected.IsEnabled = false;
				deleteSelected.IsEnabled = false;
			}
		}


		#region buttons
		private void ExportAll_Click(object sender, RoutedEventArgs e) {
			if (sourceFiles.Count > 0) {
				using (FolderBrowserDialog fbd = new() {
					Description = "选择保存位置",
				}) {
					if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
						List<string> saveFiles = [];
						bool isOverwrite = false;
						{
							List<string> sameNameTarget = [];
							foreach (string cf in cacheFiles) {
								saveFiles.Add(Path.Combine(fbd.SelectedPath, Path.GetFileName(cf)));
							}
							foreach (string sf in saveFiles) {
								if (File.Exists(sf))
									sameNameTarget.Add(sf);
							}
							if (sameNameTarget.Count != 0) {
								string outputText = "警告，发现保存目录下包含同名文件:";
								foreach (string snt in sameNameTarget) {
									outputText += "\r\n" + snt;
								}
								outputText += "\r\n是否覆盖上述文件，选择是则覆盖这些文件，选择否则跳过这些文件。";
								MessageBoxResult mbr =
								MessageBox.Show(outputText,
								this.Title, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.No);
								if (mbr == MessageBoxResult.Yes) {
									isOverwrite = true;
								}
								else if (mbr == MessageBoxResult.No) {
									isOverwrite = false;
								}
								else {
									goto over;
								}
							}
						}
						{
							List<string> errorTarget = [];
							for (int i = 0; i < saveFiles.Count; i++) {
								try {
									File.Copy(cacheFiles[i], saveFiles[i], isOverwrite);
								} catch { errorTarget.Add(saveFiles[i]); }
							}
							if (errorTarget.Count == 0) {
								MessageBox.Show("已全部成功导出", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
							}
							else {
								string outputText = "以下内容导出失败:";
								foreach (string et in errorTarget) {
									outputText += "\r\n" + et;
								}
								MessageBox.Show(outputText, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
							}
						}
					}
				}
			over:;
			}
			else {
				MessageBox.Show("没有可导出的内容", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ExportSelected_Click(object sender, RoutedEventArgs e) {
			if (photoList.SelectedIndex != -1) {
				using SaveFileDialog sfd = new() {
					Title = "选择导出位置",
					FileName = Path.GetFileName(cacheFiles[photoList.SelectedIndex]),
					Filter = "图像文件|*.jpg",
				};
				if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
					File.Copy(cacheFiles[photoList.SelectedIndex], sfd.FileName, true);
				}
			}
		}

		private void DeleteSelected_Click(object sender, RoutedEventArgs e) {
			if (MessageBox.Show($"是否确认删除\"{sourceFiles[photoList.SelectedIndex]}\"，它将被移动至回收站。", this.Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK)
				== MessageBoxResult.OK
				) {
				FileSystem.DeleteFile(sourceFiles[photoList.SelectedIndex], UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
				sourceFiles.Remove(sourceFiles[photoList.SelectedIndex]);
				cacheFiles.Remove(cacheFiles[photoList.SelectedIndex]);
				LoadImages(false);
			}
		}

		private void FlushList_Click(object sender, RoutedEventArgs e) {
			LoadImages();
		}

		private void SelectSourceDir_Click(object sender, RoutedEventArgs e) {
			using FolderBrowserDialog fbd = new() {
				Description = "选择一个文件夹",
				InitialDirectory = mainData.SourceDirPath,
			};
			if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
				mainData.SourceDirPath = fbd.SelectedPath;
				LoadImages();
			}
		}
		#endregion

		private void About_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			MessageBox.Show(PInfo.aboutText, "关于");
		}


		readonly UpdateFromGithub ufg = new() {
			EasySetCacheDir = PInfo.name,
			ProgramVersion = PInfo.version,
			RepositoryURL = PInfo.githubUrl,
		};
		/// <summary>
		/// 检查程序更新
		/// </summary>
		/// <param name="isAuto">是否是自动检查更新，如果不是，则在检查完后反馈</param>
		async void ProgramUpdate(bool isAuto = false) {
			try {
				UpdateFromGithub.CheckUpdateValue cuv = await ufg.CheckUpdateAsync();
				if (cuv.HaveUpdate) {
					switch (MessageBox.Show(
@$"检查到可用的更新，是否进行更新？
当前版本: V{PInfo.version}
最新版本: {cuv.LatestVersionStr}
发布时间: {cuv.PublishedTime_Local.AddHours(8)}"/*将UTC时间转换为北京时间*/
									, this.Title, MessageBoxButton.YesNo, MessageBoxImage.Information)) {
						case MessageBoxResult.Yes:
							UpdateFromGithub.InfoOfInstall? ioi = await ufg.DownloadReleaseAsync(0);
							if (ioi != null) {
								if (MessageBox.Show("最新版本下载完毕，是否执行安装？", this.Title, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes) {
									ufg.InstallFile(ioi, waitTime: 900);
									this.Close();
								}
							}
							else {
								MessageBox.Show("下载更新失败！", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
							}
							break;
						default:
							break;
					}
				}
				else if (!isAuto)
					MessageBox.Show("当前已是最新版本", this.Title, MessageBoxButton.OK, MessageBoxImage.Information);
			} catch {
				if (!isAuto)
					MessageBox.Show("更新检查失败！", this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}