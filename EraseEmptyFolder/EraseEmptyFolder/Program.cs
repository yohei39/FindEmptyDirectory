using System;
using System.Collections.Generic;
using System.Linq;

namespace EraseEmptyFolder
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // 入力文字結合
            string inputVal = String.Join("", args);

            string topDirectory = @"C:\";

            List<Directory> directoryList = new List<Directory>();

            int directoryNumber = -1;

            try
            {
                // 初期設定
                if (SetCurrentDirectory(topDirectory) == false)
                {
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(0);
            }

            // ディレクトリ一覧作成
            while (true)
            {
                string[] getDirectorieFolders = null;

                try
                {
                    if (directoryNumber < 0)
                    {
                        getDirectorieFolders = System.IO.Directory.GetDirectories(System.IO.Directory.GetCurrentDirectory());
                    }
                    else
                    {
                        if (directoryList.Count <= directoryNumber)
                        {
                            // 完了
                            break;
                        }

                        Directory targetFolder = directoryList[directoryNumber];
                        Console.SetCursorPosition(0, 0);
                        Console.WriteLine(directoryNumber);

                        SetCurrentDirectory(targetFolder.ParentPath);

                        getDirectorieFolders = System.IO.Directory.GetDirectories(targetFolder.Name);
                    }

                    if (getDirectorieFolders.GetLength(0) > 0)
                    {
                        foreach (var path in getDirectorieFolders)
                        {
                            string fullPath = string.Empty;
                            bool hasFile = true;

                            try
                            {
                                fullPath = System.IO.Path.GetFullPath(path);

                                hasFile = System.IO.Directory.GetFiles(path).GetLength(0) > 0 ? true : false;
                            }
                            catch
                            {
                                // next directory
                            }

                            // パスが取得できない場合は探索対象に含めない
                            // ファイル有無の取得ができなかった場合はファイル有として削除対象にしない
                            if (string.IsNullOrEmpty(fullPath) == false)
                            {
                                directoryList.Add(new Directory
                                {
                                    FullPath = fullPath,
                                    HasFile = hasFile
                                });
                            }
                        }
                    }
                }
                catch (System.IO.DirectoryNotFoundException ex)
                {
                    directoryList[directoryNumber].AddError(ex.Message);
                }
                catch (System.IO.PathTooLongException ex)
                {
                    directoryList[directoryNumber].AddError(ex.Message);
                }
                catch (UnauthorizedAccessException ex)
                {
                    directoryList[directoryNumber].AddError(ex.Message);
                }
                directoryNumber++;
            }

            Console.WriteLine("抽出終了");

            // 削除可能ディレクトリ検索
            // 下位ディレクトリにファイルを持つディレクトリがある場合は削除不可

            // 最上位
            int minDepth = directoryList.Min(x => x.Depth);
            int maxDepth = directoryList.Max(x => x.Depth);
            int currentDepth = minDepth;

            HashSet<Directory> deleteDirectorys = new HashSet<Directory>();
            // ルートディレクトリのディレクトリ一覧を取得
            var rootFolders = directoryList.Where(x => x.Depth == minDepth).ToList();

            // ルートディレクトリの子ディレクトリ群を調査する
            foreach (var fld in rootFolders)
            {
                // 子ディレクトリ群を取得
                var relativeFolders = directoryList.Where(x => x.FullPath.StartsWith(fld.FullPath)).OrderByDescending(x => x.Depth).ToList();

                Directory deleteFolder = null;

                // 最下層から上位階層に向かってファイルの有無を判定して削除可能な最上位階層を探す
                foreach (var f in relativeFolders)
                {
                    if (f.HasFile)
                    {
                        break;
                    }
                    else
                    {
                        // 削除可能階層更新
                        deleteFolder = f;
                    }
                }

                if (deleteFolder != null)
                {
                    // 削除可能ディレクトリとして追加
                    deleteDirectorys.Add(deleteFolder);
                }
            }

            Console.WriteLine("エラー発生ディレクトリ");
            List<Directory> hasErrorDirectorys = new List<Directory>();

            hasErrorDirectorys = directoryList.Where(fld => fld.HasError == true).ToList();

            foreach (Directory hasErrorFolder in hasErrorDirectorys)
            {
                Console.WriteLine(hasErrorFolder.FullPath);
                Console.WriteLine(" {0}", hasErrorFolder.ErrorMsg);
            }

            Console.WriteLine("削除可能ディレクトリ");

            foreach (var x in deleteDirectorys)
            {
                Console.WriteLine(x.FullPath);
            }

            Console.WriteLine("以上");
            Console.ReadLine();
        }

        /// <summary>
        /// カレントディレクトリの移動
        /// </summary>
        /// <param name="setDirectoryFullPath"></param>
        private static bool SetCurrentDirectory(string setDirectoryFullPath)
        {
            string currentDirectory = System.IO.Directory.GetCurrentDirectory().TrimEnd('\\');

            string[] currentDirectoryArray = currentDirectory.Split('\\');

            string[] setDirectoryArray = setDirectoryFullPath.Split('\\');

            int directoryDepth = 0;

            while (true)
            {
                if (setDirectoryArray.Count() <= directoryDepth ||
                    currentDirectoryArray.Count() <= directoryDepth ||
                    currentDirectoryArray[directoryDepth].ToUpper() != setDirectoryArray[directoryDepth].ToUpper())
                {
                    // 相違位置よりcurrentDirectoryが深い場合はルートリセット
                    if (currentDirectoryArray.Length > directoryDepth)
                    {
                        directoryDepth = 0;
                    }
                    // 指定directoryまで移動
                    while (directoryDepth < setDirectoryArray.Length)
                    {
                        string directory = setDirectoryArray[directoryDepth];
                        if (directory.Length == 0)
                        {
                            break;
                        }

                        directory = directory.EndsWith(":") ? directory + "\\" : directory;
                        try
                        {
                            System.IO.Directory.SetCurrentDirectory(directory);
                        }
                        catch (System.IO.DirectoryNotFoundException ex)
                        {
                            return false;
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        directoryDepth++;
                    }

                    break;
                }
                directoryDepth++;
            }

            return true;
        }
    }

    /// <summary>
    /// ディレクトリの情報クラス
    /// </summary>
    internal class Directory
    {
        private string fullPath;
        private int depth;
        private string errMsg;

        public Directory()
        {
        }

        /// <summary>
        /// 絶対パス
        /// </summary>
        public string FullPath
        {
            get
            {
                return this.fullPath;
            }
            set
            {
                this.fullPath = value;

                depth = this.fullPath.TrimEnd('\\').Split('\\').Length;
            }
        }

        /// <summary>
        /// 親ディレクトリの絶対パス
        /// </summary>
        public string ParentPath
        {
            get
            {
                string[] path = this.fullPath.Split('\\');

                if (path.Length == 0)
                {
                    return null;
                }

                string parentPath = string.Empty;

                parentPath = path[0];

                for (int i = 1; i < path.Length - 1; i++)
                {
                    parentPath += "\\" + path[i];
                }

                return parentPath;
            }
        }

        /// <summary>
        /// ディレクトリアクセス時のエラーメッセージ登録
        /// </summary>
        /// <param name="erro"></param>
        public void AddError(string erro)
        {
            this.errMsg = erro;
        }

        public string ErrorMsg => this.errMsg;

        /// <summary>
        /// ルートディレクトリからの階層数
        /// </summary>
        public int Depth => depth;

        /// <summary>
        /// ディレクトリ名
        /// </summary>
        public string Name => this.fullPath.Split('\\').GetValue(this.fullPath.Split('\\').Length - 1).ToString();

        /// <summary>
        /// エラー有無
        /// </summary>
        public bool HasError => string.IsNullOrEmpty(errMsg) ? false : true;

        /// <summary>
        /// ディレクトリ内のファイル有無
        /// </summary>
        public bool HasFile { get; set; }
    }
}