using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
namespace GPAK_Extractor {
	public class Program {
		public static void Main(string[] args) {
			string path = args?.Length == 0 ? @"game.gpak" : args[0];

			try {
				GPAK gpak = new GPAK(path);
				gpak.Execute();
			} catch (Exception e) {
				Console.WriteLine(e.ToString());
				Console.ReadKey();
			}
		}
	}
	public class GPAK {
		public string FilePath { get; set; }
		public int Count { get; set; }
		public List<Entry> Entries { get; set; }

		public GPAK(string filePath) {
			FilePath = filePath;
			Entries = new List<Entry>();
			Count = 0;

			if (File.Exists(filePath)) {
				ReadFile();
			} else if (Directory.Exists(filePath)) {
				ReadDirectory();
			}
		}

		public void Execute() {
			if (File.Exists(FilePath)) {
				string outPath = Path.Combine(Path.GetDirectoryName(FilePath), "Output");

				for (int i = 0; i < Count; i++) {
					Entry entry = Entries[i];

					string path = Path.Combine(outPath, entry.Path);
					Directory.CreateDirectory(Path.GetDirectoryName(path));

					Console.WriteLine(path);
					if (File.Exists(path)) {
						File.SetAttributes(path, FileAttributes.Normal);
					}
					File.WriteAllBytes(path, entry.Read(FilePath));
				}
			} else if (Directory.Exists(FilePath)) {
				string outPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(FilePath)), "output.gpak");

				using (FileStream file = new FileStream(outPath, FileMode.Create)) {
					WriteInt(file, Count);

					for (int i = 0; i < Count; i++) {
						Entry entry = Entries[i];

						WriteShort(file, (short)entry.Path.Length);
						WriteText(file, entry.Path);
						WriteInt(file, entry.Length);
					}

					for (int i = 0; i < Count; i++) {
						Entry entry = Entries[i];

						string path = Path.Combine(FilePath, entry.Path);
						Console.WriteLine(path);
						file.Write(File.ReadAllBytes(path), 0, entry.Length);
					}
				}
			}
		}
		private void ReadFile() {
			int position = 0;
			using (FileStream file = new FileStream(FilePath, FileMode.Open)) {
				Count = ReadInt(file);

				for (int i = 0; i < Count; i++) {
					int textLen = ReadShort(file);

					Entry newEntry = new Entry();
					newEntry.Path = ReadText(file, textLen);
					newEntry.Length = ReadInt(file);
					Entries.Add(newEntry);
				}

				position = (int)file.Position;
				file.Close();
			}

			for (int i = 0; i < Count; i++) {
				Entry entry = Entries[i];
				entry.Offset = position;
				position += entry.Length;
			}
		}
		private void ReadDirectory() {
			FilePath = Path.IsPathRooted(FilePath) ? FilePath : Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), FilePath);
			if (!FilePath.EndsWith("\\") && !FilePath.EndsWith("/")) {
				FilePath = FilePath + Path.DirectorySeparatorChar;
			}

			string[] files = Directory.GetFiles(FilePath, "*", SearchOption.AllDirectories);

			Count = 0;
			for (int i = 0; i < files.Length; i++) {
				string file = files[i];
				FileInfo info = new FileInfo(file);

				if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) {
					continue;
				}

				Count++;

				Entry newEntry = new Entry();
				newEntry.Path = file.Substring(FilePath.Length);
				newEntry.Length = (int)info.Length;
				Entries.Add(newEntry);
			}
		}
		private int ReadInt(FileStream file) {
			byte[] data = new byte[4];
			file.Read(data, 0, 4);
			return BitConverter.ToInt32(data, 0);
		}
		private int ReadShort(FileStream file) {
			byte[] data = new byte[2];
			file.Read(data, 0, 2);
			return BitConverter.ToInt16(data, 0);
		}
		private string ReadText(FileStream file, int length) {
			byte[] data = new byte[length];
			file.Read(data, 0, length);
			return Encoding.UTF8.GetString(data);
		}
		private void WriteInt(FileStream file, int value) {
			file.Write(BitConverter.GetBytes(value), 0, 4);
		}
		private void WriteShort(FileStream file, short value) {
			file.Write(BitConverter.GetBytes(value), 0, 2);
		}
		private void WriteText(FileStream file, string value) {
			file.Write(Encoding.UTF8.GetBytes(value.Replace('\\', '/')), 0, value.Length);
		}
	}
	public class Entry {
		public string Path { get; set; }
		public int Length { get; set; }
		public int Offset { get; set; }

		public byte[] Read(string filePath) {
			byte[] data = new byte[Length];
			using (FileStream file = new FileStream(filePath, FileMode.Open)) {
				file.Position = Offset;
				file.Read(data, 0, Length);
				file.Close();
			}
			return data;
		}
		public override string ToString() {
			return Path + "(" + Length + ")";
		}
	}
}