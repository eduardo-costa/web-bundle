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
        /// Will store raw channel pixels.
        /// </summary>
        static bool store_channels;

        /// <summary>
        /// Target folder.
        /// </summary>
        static string resource_path;

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
            store_channels = false;

            resource_path = "";

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-h":    is_help = true; break;
                    case "-r":    is_recursive = true; break;
                    case "-i":    if (i < (arglen - 1)) resource_path = args[i + 1]; break;
                    case "-o":    if (i < (arglen - 1)) bundle_path   = args[i + 1]; break;
                    case "-v":    is_verbose = true; break;                    
                }
            }

            Console.WriteLine("Web Bundle Packer - v" + VERSION);

            if (is_help)
            {
                Console.WriteLine("  -h outputs help");
                Console.WriteLine("  -r recursive search");
                Console.WriteLine("  -i input path");
                Console.WriteLine("  -o output file");
                Console.WriteLine("  -v enable verbose");
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

            LogLine("Encoding [" + resource_path + "] recursive["+is_recursive+"] @ ["+bundle_path+"]");

            try
            {
                bundle_files = Directory.GetFiles(resource_path, "*.*", is_recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }
            catch (Exception err)
            {                
                if(err!=null)LogLine("Error: Wrong path!");
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
                long len = GetLength(f, store_channels);                
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

                switch (ext)
                {
                    case "png":
                        fd = store_channels ? GetBitmapBytes(f) : File.ReadAllBytes(f);
                        break;

                    case "jpg":
                        fd = store_channels ? GetBitmapBytes(f) : File.ReadAllBytes(f);
                        break;

                    default:
                        fd = File.ReadAllBytes(f);
                        break;

                }
                LogLine("[" + FormatMem(fd.Length) + "]");
                                
                //string ss = "";
                for (int i = 0; i < fd.Length; i++)
                {
                    //ss += fd[i] + " ";
                    buffer[k++] = fd[i];
                }
                
                //File.WriteAllText(f + ".txt", ss);
                
            }

            int bw = 0;
            int bh = 0;

            LogLine("Generating PNG bundle...");

            //Writes the buffer data in the Bitmap
            //GenerateBundle(bundle_path,buffer,MAX_WIDTH,out bw, out bh);
            GenerateBundleNew(bundle_path, buffer, MAX_WIDTH, out bw, out bh);

            if (bw > MAX_WIDTH)  { LogLine("Bundle exceeded [" + MAX_WIDTH + "] limit!"); return ERROR_BUNDLE_LIMIT_EXCEEDED; }
            if (bh > MAX_WIDTH) { LogLine("Bundle exceeded [" + MAX_WIDTH + "] limit!"); return ERROR_BUNDLE_LIMIT_EXCEEDED; }

            
            bundle_length = GetLength(bundle_path,store_channels);

            float compressed = (float)bundle_length;
            float total      = (float)bundle_raw_length;
            int percent      = (int)((1.0f - (compressed / total)) * 100f);
            percent = Math.Max(percent, 0);
            LogLine("Bundle Generated - ["+bundle_files.Length+" files]["+bw+"x"+bh+"] original[" + FormatMem((int)bundle_raw_length) + "] compressed[" + FormatMem((int)bundle_length) + "] " + percent + "% compressed");

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
                long len   = GetLength(f,store_channels);

                List<string> tks = new List<string>(f.Split('\\'));
                if(tks.Count>1) tks.RemoveAt(0);
                f = string.Join("\\", tks.ToArray());

                h += f + "," + ext + "," + len + ";";
                if (i < (p_files.Length - 1)) h += "\n";
            }
            return h;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_file"></param>
        /// <param name="p_buffer"></param>
        /// <param name="p_max_width"></param>
        /// <param name="p_w"></param>
        /// <param name="p_h"></param>
        static void GenerateBundleNew(string p_file, byte[] p_buffer, int p_max_width,out int p_w,out int p_h)
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
        /// Generates the bundle bitmap.
        /// </summary>
        /// <param name="p_buffer"></param>
        /// <param name="p_max_width"></param>
        static void GenerateBundle(string p_name,byte[] p_buffer, int p_max_width, out int p_w, out int p_h)
        {
            
            byte[] d = p_buffer;

            //PixelFormat fmt = PixelFormat.Format24bppRgb;

            //int cc = fmt == PixelFormat.Format32bppArgb ? 4 : 3;
            
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

            //int k = 0;

            /*
            //Create the bitmap
            Bitmap png = new Bitmap(w, h, fmt);

            //Extract the pixel buffer.
            Rectangle r = new Rectangle(0, 0, w, h);
            BitmapData bd = png.LockBits(r, ImageLockMode.ReadWrite, png.PixelFormat);

            IntPtr ptr        = bd.Scan0;
            int byte_count    = Math.Abs(bd.Stride) * bd.Height;
            byte[] png_buffer = new byte[byte_count];

            // Copy the RGB values into the array.
            Marshal.Copy(ptr, png_buffer, 0, byte_count);

            //BGR
            //Watch out for the stride+padding!!
            for (int i = 0; i < pixel_count; i++)
            {   
                int px = i % w;
                int py = (int)(i / w);
                int p  = (px*cc) + (py * bd.Stride);

                png_buffer[p+2] = k < d.Length ? d[k++] : (byte)' ';
                png_buffer[p+1] = k < d.Length ? d[k++] : (byte)' ';
                png_buffer[p  ] = k < d.Length ? d[k++] : (byte)' ';
                if (cc >= 4)
                png_buffer[p+3] = k < d.Length ? d[k++] : (byte)255;
            }

            Marshal.Copy(png_buffer, 0, ptr, byte_count);

            png.UnlockBits(bd);
            
            
            //Fill the data                
            for (int i = 0; i < pixel_count; i++)
            {   
                int px = i % w;
                int py = (int)(i / w);
                byte cr = k < d.Length ? (byte)d[k++] : (byte)255;
                byte cg = k < d.Length ? (byte)d[k++] : (byte)255;
                byte cb = k < d.Length ? (byte)d[k++] : (byte)255;
                byte ca = 
                    //(cc>=4) ? (k < d.Length ? (byte)d[k++] : (byte)' ') : 
                    (byte)255;
                png.SetPixel(px, py, Color.FromArgb(ca, cr, cg, cb));
            }
            

            if (w < p_max_width)
            {
                png.Save(bundle_path, System.Drawing.Imaging.ImageFormat.Bmp);
                png.Dispose();
            }
            //*/
        }

        /// <summary>
        /// Returns the raw byte data of the informed image.
        /// </summary>
        /// <param name="p_path"></param>
        /// <returns></returns>
        static byte[] GetBitmapBytes(string p_path)
        {
            /*
            Bitmap img = (Bitmap) Bitmap.FromFile(p_path);
            int cc = 4;
            switch(img.PixelFormat)
            {
                case PixelFormat.Alpha:             cc = 1; break;
                case PixelFormat.Format24bppRgb:    cc = 3; break;
                case PixelFormat.Format32bppArgb:   cc = 4; break;
                default: return new byte[1]{0};
            }

            Log("   w["+img.Width+"] h["+img.Height+"] channels["+cc+"]");
            
            byte[] buff = new byte[img.Width * img.Height * cc];
            int k = 0;
            for(int i=0;i<img.Height;i++)
            for (int j = 0; j < img.Width; j++)
            {
                Color c = img.GetPixel(j, i);
                buff[k++] = c.R;
                buff[k++] = c.G;
                buff[k++] = c.B;
                if (cc >= 4) buff[k++] = c.A;
            }            
            return buff;
            //*/
            return null;
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
        static long GetLength(string p_file,bool read_channels) 
        {
            string ext = GetExtension(p_file);
            if (!read_channels) ext = "";
            Bitmap b;
            int cc;
            long len = 0;
            switch (ext)
            {
                case "png": b = (Bitmap)Bitmap.FromFile(p_file); cc = GetChannels(b); len = b.Width * b.Height * cc; b.Dispose(); break;
                case "jpg": b = (Bitmap)Bitmap.FromFile(p_file); cc = GetChannels(b); len = b.Width * b.Height * cc; b.Dispose(); break;
                default:
                FileStream fs = File.OpenRead(p_file);
                len = fs.Length; 
                fs.Close();
                break;
            }
            return len;
        }


        static int GetChannels(Bitmap b) { return 3; /*b.PixelFormat == PixelFormat.Alpha ? 1 : (b.PixelFormat == PixelFormat.Format24bppRgb ? 3 : 4);*/ }

        static void LogLine(string msg) { if (is_verbose) Console.WriteLine(msg); }
        static void Log(string msg) { if (is_verbose) Console.Write(msg); }
    }
}
