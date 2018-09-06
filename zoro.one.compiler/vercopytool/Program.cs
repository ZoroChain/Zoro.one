using System;

namespace vercopytool
{
    class Program
    {
        static void Main(string[] args)
        {
            var srcfile = args[0];
            var outdir = args[1];

            //show version
            var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(srcfile);
            Console.WriteLine("ver=" + info.ProductVersion);

            //清理/创建目标目录
            var outpath = System.IO.Path.Combine(outdir, "v" + info.ProductVersion);
            if (System.IO.Directory.Exists(outpath))
            {
                System.IO.Directory.Delete(outpath, true);
            }
            System.IO.Directory.CreateDirectory(outpath);
            Console.WriteLine("outpath=" + outpath);

            //复制文件
            var srcdir = System.IO.Path.GetDirectoryName(srcfile);
            var files = System.IO.Directory.GetFiles(srcdir, ".", System.IO.SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var file = f.Substring(srcdir.Length + 1);
                var outfile = System.IO.Path.Combine(outpath, file);
                var outfiledir = System.IO.Path.GetDirectoryName(outfile);
                if (System.IO.Directory.Exists(outfiledir) == false)
                    System.IO.Directory.CreateDirectory(outfiledir);
                Console.WriteLine("copy " + f + " => " + outfile);
                System.IO.File.Copy(f, outfile);
            }
        }
    }
}
