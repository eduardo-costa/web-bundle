using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Hjg.Pngcs;
using Hjg.Pngcs.Zlib;
using System.Threading;
using System.Reflection;


namespace wb
{
    class Program
    {
        static string VERSION = "1.0.0";

        /// <summary>
        /// Error codes.
        /// </summary>
        //static int ERROR_INVALID_ARGS          = 1;
        static int ERROR_PATH_NOT_FOUND        = 2;
        static int ERROR_NO_FILES              = 3;
        static int ERROR_OUTPUT_NOT_FOUND      = 4;
        static int ERROR_BUNDLE_LIMIT_EXCEEDED = 5;
        static int ERROR_DECODE_ERROR          = 6;

        static int MAX_WIDTH = 16384;

        /// <summary>
        /// Flag that indicates if logging will be used.
        /// </summary>
        static bool is_verbose;

        /// <summary>
        /// Will search deep in the file system.
        /// </summary>
        static bool is_recursive;

        /// <summary>
        /// Will read the input png and extract all files.
        /// </summary>
        static bool is_decode;
        
        /// <summary>
        /// Target folder.
        /// </summary>
        static string resource_path;

        /// <summary>
        /// Hash used to encript the data bytes.
        /// </summary>
        static string hash;

        /// <summary>
        /// Flag that indicates the hash file is valid.
        /// </summary>
        static bool has_hash;
        
        /// <summary>
        /// Hash iterator.
        /// </summary>
        static int hk;

        /// <summary>
        /// Header with data information.
        /// </summary>
        static string bundle_header;

        /// <summary>
        /// Bundle file name.
        /// </summary>
        static string bundle_path;

        /// <summary>
        /// Bundle file list.
        /// </summary>
        static string[] bundle_files;

        /// <summary>
        /// Bundle bitmap.
        /// </summary>
        //static Bitmap bundle;

        /// <summary>
        /// Bundle compressed size.
        /// </summary>
        static long bundle_length;

        /// <summary>
        /// Bundle raw size.
        /// </summary>
        static long bundle_raw_length;

        /// <summary>
        /// Entry Point
        /// </summary>
        /// <param name="args"></param>
        static int Main(string[] args)
        {
            
            int arglen       = args.Length;
            bool is_help = false;
            if (arglen <= 1) 
            { 
                //Console.WriteLine("Not enough arguments!");
                is_help = true;                
            }

            is_verbose     = false;
            is_recursive   = false;            
            is_decode      = false;
            has_hash = false;

            hash = "";
            hk = 0;

            resource_path = "";

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-?":    is_help = true; break;
                    case "-r":    is_recursive = true; break;
                    case "-i":    if (i < (arglen - 1)) resource_path = args[i + 1]; break;
                    case "-o":    if (i < (arglen - 1)) bundle_path   = args[i + 1]; break;
                    case "-v":    is_verbose = true; break;
                    case "-h": if (i < (arglen - 1)) { hash = args[i + 1]; has_hash = !string.IsNullOrEmpty(hash); } break;
                    case "-d":    is_decode = true; break;
                }
            }

            Console.WriteLine("Web Bundle Packer - v" + VERSION);

            if (is_help)
            {
                Console.WriteLine("  -? outputs help");
                Console.WriteLine("  -r recursive search");
                Console.WriteLine("  -i input path");
                Console.WriteLine("  -o output file");
                Console.WriteLine("  -v enable logging");
                Console.WriteLine("  -h encript hash");
                Console.WriteLine("  -d execute as decoder");
            }

            if (string.IsNullOrEmpty(resource_path))
            { 
                LogLine("Target path not specified!");
                return ERROR_PATH_NOT_FOUND; 
            }

            if (string.IsNullOrEmpty(bundle_path))
            {
                LogLine("Output path not specified!");
                return ERROR_OUTPUT_NOT_FOUND;
            }

            Log(is_decode ? "Decoding " : "Encoding ");
            Log("[" + resource_path + "] ");
            if(!is_decode) Log("recursive["+is_recursive+"] ");
            Log("@ [" + bundle_path + "]\n");

            if (is_decode)
            {
                if (!ReadBundle(resource_path, bundle_path))
                {
                    LogLine("Error: Failed to decode ["+resource_path+"]");
                    return ERROR_DECODE_ERROR;
                }
                LogLine("Success - All files extracted!");
                return 0;
            }

            try
            {
                bundle_files = Directory.GetFiles(resource_path, "*.*", is_recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
            catch (Exception err)
            {                
                if(err!=null)LogLine("Error: Path not found!");
                return ERROR_NO_FILES;
            }

            if (bundle_files.Length <= 0)
            {
                LogLine("No files found!");
                return ERROR_NO_FILES;
            }

            //Generates the header string from the file list.
            bundle_header = GenerateHeader(bundle_files);

            //Calculates total byte length of the bundle.
            bundle_raw_length = 0;
            bundle_raw_length += bundle_header.Length + 1; //string + 0
            foreach (string f in bundle_files)
            {
                long len = GetLength(f);                
                bundle_raw_length += len;
            }

            //Creates the temp buffer.
            byte[] buffer = new byte[bundle_raw_length];

            //Writes the header string in the buffer.
            for (int i = 0; i < bundle_header.Length; i++) buffer[i] = (byte)bundle_header[i];

            buffer[bundle_header.Length] = 0;

            //Starts at 'header' offset bytes.
            int k = bundle_header.Length + 1;

            //Writes the file list bytes in the buffer.
            foreach (string f in bundle_files)
            {
                string ext = GetExtension(f);
                if (ext == "jpeg") ext = "jpg";

                Log("packing [" + f + "]");
                
                byte[] fd  = null;

                fd = File.ReadAllBytes(f);

                LogLine("[" + FormatMem(fd.Length) + "]");
                
                for (int i = 0; i < fd.Length; i++)
                {                
                    byte v = fd[i];
                    if (has_hash)
                    {
                        v = (byte)(v ^ ((byte)hash[hk]));
                        hk = (hk + 1) % hash.Length;
                    }
                    buffer[k++] = v;
                }
                
            }

            int bw = 0;
            int bh = 0;

            LogLine("Writing PNG bundle...");

            //Writes the buffer data in the Bitmap            
            WriteBundle(bundle_path, buffer, MAX_WIDTH, out bw, out bh);

            if (bw > MAX_WIDTH)  { LogLine("Bundle exceeded [" + MAX_WIDTH + "] limit!"); return ERROR_BUNDLE_LIMIT_EXCEEDED; }
            if (bh > MAX_WIDTH) { LogLine("Bundle exceeded [" + MAX_WIDTH + "] limit!"); return ERROR_BUNDLE_LIMIT_EXCEEDED; }

            bundle_length = GetLength(bundle_path);

            float compressed = (float)bundle_length;
            float total      = (float)bundle_raw_length;
            int percent      = (int)((1.0f - (compressed / total)) * 100f);
            percent = Math.Max(percent, 0);
            LogLine("Success - ["+bundle_files.Length+" files]["+bw+"x"+bh+"] original[" + FormatMem((int)bundle_raw_length) + "] compressed[" + FormatMem((int)bundle_length) + "] " + percent + "% compressed");

            return 0;
        }

        /// <summary>
        /// Generates the header string.
        /// </summary>
        /// <param name="p_path"></param>
        /// <param name="p_recursive"></param>
        /// <returns></returns>
        static string GenerateHeader(string[] p_files)
        {
            string h = "";            
            for(int i=0;i<p_files.Length;i++)
            {
                string f   = p_files[i];                
                string ext = GetExtension(f);
                long len   = GetLength(f);
                List<string> tks = new List<string>(f.Split('\\'));
                if(tks.Count>1) tks.RemoveAt(0);
                f = string.Join("\\", tks.ToArray());
                h += f + "," + ext + "," + len + ";";
                if (i < (p_files.Length - 1)) h += "\n";
            }
            return h;
        }

        /// <summary>
        /// Receives the byte buffer with all data and writes the bundle file and saves it with the specified name. Returns the size of the generated PNG.
        /// </summary>
        /// <param name="p_file"></param>
        /// <param name="p_buffer"></param>
        /// <param name="p_max_width"></param>
        /// <param name="p_w"></param>
        /// <param name="p_h"></param>
        static void WriteBundle(string p_file, byte[] p_buffer, int p_max_width,out int p_w,out int p_h)
        {
            byte[] d = p_buffer;
            int cc = 3;
            //Detects the PNG ideal width and height based on byte count.
            int pixel_count = d.Length / cc;
            int w = 1;
            int h = 1;
            for (int i = 0; i < p_max_width; i++) if ((i * i) >= pixel_count) { w = i; break; }
            h = w;
            for (int i = h; i > 0; i--) { if ((w * i) < pixel_count) { h = i + 1; break; } }
            p_w = w;
            p_h = h;
            ImageInfo imi = new ImageInfo(w, h, 8, false); // 8 bits per channel, no alpha             
            // open image for writing 
            PngWriter png = FileHelper.CreatePngWriter(p_file, imi, true);
            byte[] dr = new byte[w*cc];
            int k = 0;
            for (int i = 0; i < h; i++)
            {
                ImageLine iline = new ImageLine(imi);
                
                for (int j = 0; j < w; j++)
                {
                    byte cr = (k >= d.Length) ? ((byte)255) : d[k++];
                    byte cg = (k >= d.Length) ? ((byte)255) : d[k++];
                    byte cb = (k >= d.Length) ? ((byte)255) : d[k++];
                    int p = j*3;
                    dr[p]   = cr;
                    dr[p+1] = cg;
                    dr[p+2] = cb;
                }
                png.WriteRowByte(dr, i);
            }
            png.CompressionStrategy = EDeflateCompressStrategy.Filtered;
            png.CompLevel = 9;
            png.End();
        }

        /// <summary>
        /// Reads and extract all data from the bundle.
        /// </summary>
        /// <param name="p_file"></param>
        static bool ReadBundle(string p_file,string p_target)
        {
            string[] tks;
            tks = p_file.Split('\\');
            string file_name = tks.Length <= 0 ? "" : tks[tks.Length - 1];            
            if (string.IsNullOrEmpty(file_name)) return false;
            tks = file_name.Split('.');
            file_name = tks.Length <= 0 ? "" : tks[0];
            if (string.IsNullOrEmpty(file_name)) return false;

            if(!File.Exists(p_file)) return false;

            FileStream fs = File.OpenRead(p_file);

            PngReader png = new PngReader(fs);
            int cc = png.ImgInfo.Alpha ? 4 : 3;            
            int w = png.ImgInfo.BytesPerRow / cc;
            int h = png.ImgInfo.Rows;
            byte[] buffer = new byte[w * h * cc];
            int k = 0;
            int[][] im = png.ReadRowsInt().Scanlines;
            for (int i = 0; i < im.Length; i++)
            {
                int[] row = im[i];
                for (int j = 0; j < row.Length; j++)
                {   
                    buffer[k++] = (byte)row[j];                    
                }
            }
            png.End();
            fs.Close();

            string header = "";

            k=0;

            while (buffer[k] != 0)
            {
                if (k >= buffer.Length) return false;
                header += (char)buffer[k];
                k++;
            }

            k++;

            header = header.Replace("\n", "");

            tks = header.Split(';');

            List<string> dir = new List<string>();

            for (int i = 0; i < tks.Length; i++)
            {
                if (string.IsNullOrEmpty(tks[i])) continue;
                string[] hd = tks[i].Split(',');
                string path    = p_target+"\\"+hd[0];
                int byte_count = int.Parse(hd[2]);

                dir.Clear();
                dir.AddRange(path.Split('\\'));
                dir.RemoveAt(dir.Count - 1);
                string target_dir = string.Join("\\", dir.ToArray());
                if (!string.IsNullOrEmpty(target_dir)) Directory.CreateDirectory(target_dir);

                byte[] file_buffer = new byte[byte_count];

                for (int j = 0; j < byte_count; j++)
                {
                    byte v = buffer[k++];
                    if (has_hash)
                    {
                        v = (byte)(v ^ ((byte)hash[hk]));
                        hk = (hk + 1) % hash.Length;
                    }
                    file_buffer[j] = v;
                }

                LogLine("unpacking ["+path+"]["+FormatMem(byte_count)+"]");

                File.WriteAllBytes(path, file_buffer);

            }

            return true;
        }

        /// <summary>
        /// Return the file extension in lowercase.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        static string GetExtension(string f) { string[] tk = f.Split('.'); if (tk.Length <= 0) return ""; return tk[tk.Length - 1].ToLower(); }

        /// <summary>
        /// Format memory values to string.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        static string FormatMem(int v)
        {
            float fv    = (float)v;
            float len   = v < 1024 ? fv : (v < 1048576 ? (fv / 1024f) : (fv / 1024f / 1024f));
            string unit = v < 1024 ? "bytes" : (v < 1048576 ? "kb" : "mb");
            return (Math.Floor(len * 10f) / 10f) + "" + unit;
        }

        /// <summary>
        /// Get the length of a file in bytes. If the file is an image the raw byte count is returned.
        /// </summary>
        /// <param name="p_file"></param>
        /// <returns></returns>
        static long GetLength(string p_file) 
        {
            long len = 0;
            FileStream fs = File.OpenRead(p_file);
            len = fs.Length;
            fs.Close();
            return len;        
        }

        /// <summary>
        /// Log helper
        /// </summary>
        /// <param name="msg"></param>
        static void LogLine(string msg) { if (is_verbose) Console.WriteLine(msg); }
        static void Log(string msg) { if (is_verbose) Console.Write(msg); }

    }
}
