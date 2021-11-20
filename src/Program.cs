﻿using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using static VVVF_Generator_Porting.vvvf_wave;

namespace VVVF_Generator_Porting
{
    internal class Program
    {
        static double count = 0;
        static int div_freq = 192 * 1000;
        static int mascon_off_count = 120000;

        // variables for controlling parameters
        static Boolean do_frequency_change = true;
        static Boolean brake = false;

        static double wave_stat = 0;
        static int mascon_count = mascon_off_count;
        static bool mascon_off = false;
        static int temp_count = 0;

        static void reset_control_variables()
        {
            do_frequency_change = true;
            brake = false;

            wave_stat = 0;
            mascon_count = mascon_off_count;
            mascon_off = false;
            temp_count = 0;
        }

        static Boolean check_for_freq_change()
        {
            count++;
            if (count % 60 == 0 && do_frequency_change && mascon_count == mascon_off_count)
            {
                double sin_new_angle_freq = sin_angle_freq;
                if (!brake) sin_new_angle_freq += Math.PI / 500 * 1.5;
                else sin_new_angle_freq -= Math.PI / 500 * 1.5;
                double amp = sin_angle_freq / sin_new_angle_freq;
                sin_angle_freq = sin_new_angle_freq;
                sin_time = amp * sin_time;
            }

            if (temp_count == 0)
            {
                if (sin_angle_freq / 2 / Math.PI > 90 && !brake && do_frequency_change)
                {
                    do_frequency_change = false;
                    mascon_off = true;
                    count = 0;
                }
                else if (count / div_freq > 2 && !do_frequency_change)
                {
                    do_frequency_change = true;
                    mascon_off = false;
                    brake = true;
                    temp_count++;
                }
            }
            else if (temp_count == 1)
            {
                if (sin_angle_freq / 2 / Math.PI < 30 && brake && do_frequency_change)
                {
                    do_frequency_change = false;
                    mascon_off = true;
                    count = 0;
                }
                else if (count / div_freq > 2 && !do_frequency_change)
                {
                    do_frequency_change = true;
                    mascon_off = false;
                    brake = false;
                    temp_count++;
                }
            }
            else if (temp_count == 2)
            {
                if (sin_angle_freq / 2 / Math.PI > 45 && !brake && do_frequency_change)
                {
                    do_frequency_change = false;
                    mascon_off = true;

                    count = 0;
                }
                else if (count / div_freq > 2 && !do_frequency_change)
                {
                    do_frequency_change = true;
                    mascon_off = false;
                    brake = true;
                    temp_count++;
                }
            }
            else
            {
                if (sin_angle_freq / 2 / Math.PI < 0 && brake && do_frequency_change) return false;
            }



            if (!mascon_off)
            {
                wave_stat = sin_angle_freq / (Math.PI * 2) * mascon_count / (double)mascon_off_count;
                if (++mascon_count > mascon_off_count) mascon_count = mascon_off_count;
            }
            else
            {
                wave_stat = sin_angle_freq / (Math.PI * 2) * mascon_count / (double)mascon_off_count;
                if (--mascon_count < 0) mascon_count = 0;
            }

            return true;
        }

        static void generate_sound(String output_path)
        {
            reset_control_variables();
            reset_all_variables();

            Int32 sound_block_count = 0;
            DateTime dt = DateTime.Now;
            String gen_time = dt.ToString("yyyy-MM-dd_HH-mm-ss");


            String fileName = output_path + "\\" + gen_time + ".wav";

            BinaryWriter writer = new BinaryWriter(new FileStream(fileName, FileMode.Create));

            //WAV FORMAT DATA
            writer.Write(0x46464952); // RIFF
            writer.Write(new byte[]{ 0x00,0x00,0x00,0x00}); //CHUNK SIZE
            writer.Write(0x45564157); //WAVE
            writer.Write(0x20746D66); //fmt 
            writer.Write(16);
            writer.Write(new byte[] { 0x01, 0x00 }); // LINEAR PCM
            writer.Write(new byte[] { 0x01, 0x00 }); // MONORAL
            writer.Write(div_freq); // SAMPLING FREQ
            writer.Write(div_freq); // BYTES IN 1SEC
            writer.Write(new byte[] { 0x01, 0x00 }); // Block Size = 1
            writer.Write(new byte[] { 0x08, 0x00 }); // 1 Sample bits
            writer.Write(0x61746164);
            writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 }); //WAVE SIZE

            bool loop = true;

            while (loop)
            {
                vvvf_wave.sin_time += 1.00 / div_freq;
                vvvf_wave.saw_time += 1.00 / div_freq;

                Wave_Values wv_U = calculate_toei_6300_3(brake, !mascon_off, mascon_count != mascon_off_count, Math.PI * 2.0 / 3.0 * 0, wave_stat);
                Wave_Values wv_V = calculate_toei_6300_3(brake, !mascon_off, mascon_count != mascon_off_count, Math.PI * 2.0 / 3.0 * 1, wave_stat );

                for (int i = 0; i < 1; i++)
                {
                    if (wv_U.pwm_value - wv_V.pwm_value > 0) writer.Write((byte)0xB0);
                    else if (wv_U.pwm_value - wv_V.pwm_value < 0) writer.Write((byte)0x50);
                    else writer.Write((byte)0x80);
                }
                sound_block_count++;

                loop = check_for_freq_change();

            }

            

            writer.Seek(4, SeekOrigin.Begin);
            writer.Write(sound_block_count + 36);

            writer.Seek(40, SeekOrigin.Begin);
            writer.Write(sound_block_count);

            writer.Close();
        }

        //only works with windows
        static void generate_video(String output_path)
        {
            reset_control_variables();
            reset_all_variables();

            DateTime dt = DateTime.Now;
            String gen_time = dt.ToString("yyyy-MM-dd_HH-mm-ss");
            bool temp = true;
            Int32 sound_block_count = 0;


            

            int image_width = 2000;
            int image_height = 500;
            int movie_div = 3000;
            int wave_height = 100;

            String fileName = output_path + "\\" + gen_time + ".avi";
            VideoWriter vr = new VideoWriter(fileName, OpenCvSharp.FourCC.H264, div_freq / movie_div, new OpenCvSharp.Size(image_width, image_height));

            if (!vr.IsOpened())
            {
                return;
            }

            Boolean loop = true;
            while (loop)
            {
                if (sound_block_count % movie_div == 0 && temp)
                {
                    sin_time = 0;
                    saw_time = 0;
                    Bitmap image = new(image_width, image_height);
                    Graphics g = Graphics.FromImage(image);
                    g.FillRectangle(new SolidBrush(Color.White), 0, 0, image_width, image_height);
                    g.DrawLine(new Pen(Color.DarkGray), 0, image_height / 2, image_width, image_height / 2);

                    int[] points_U = new int[image_width];
                    int[] points_V = new int[image_width];


                    for (int i = 0; i < image_width; i++)
                    {
                        sin_time += Math.PI / 50000.0;
                        saw_time += Math.PI / 50000.0;

                        Wave_Values wv_U = calculate_tokyuu_1000_1500_IGBT(brake, !mascon_off, mascon_count != mascon_off_count, Math.PI * 2.0 / 3.0 * 0, wave_stat);
                        Wave_Values wv_V = calculate_tokyuu_1000_1500_IGBT(brake, !mascon_off, mascon_count != mascon_off_count, Math.PI * 2.0 / 3.0 * 1, wave_stat);

                        int voltage_stat = (int)(wv_U.pwm_value - wv_V.pwm_value);
                        points_U[i] = (int)wv_U.pwm_value;
                        points_V[i] = (int)wv_V.pwm_value;
                    }

                    for (int i = 0; i < image_width - 1; i++)
                    {

                        int curr_val = (int)(-(points_U[i] - points_V[i]) * wave_height + image_height / 2.0);
                        int next_val = (int)(-(points_U[i+1] - points_V[i+1]) * wave_height + image_height / 2.0);
                        g.DrawLine(new Pen(Color.Blue), i, curr_val, ((curr_val != next_val) ? i : i+1), next_val);

                        //g.DrawLine(new Pen(Color.Gray), i, (int)(points_U[i] * wave_height + image_height / 2.0), i + 1, (int)(points_U[i+1] * wave_height + image_height / 2.0));
                        //g.DrawLine(new Pen(Color.Gray), i, (int)(points_V[i] * wave_height + image_height / 2.0), i + 1, (int)(points_V[i + 1] * wave_height + image_height / 2.0));
                    }


                    MemoryStream ms = new MemoryStream();
                    image.Save(ms, ImageFormat.Png);
                    byte[] img = ms.GetBuffer();
                    Mat mat = OpenCvSharp.Mat.FromImageData(img);

                    Cv2.ImShow("Wave Form View",mat);
                    Cv2.WaitKey(1);

                    vr.Write(mat);

                    g.Dispose();
                    image.Dispose();

                    temp = false;
                }
                else if(sound_block_count % movie_div != 0)
                {
                    temp = true;
                }

                sound_block_count++;

                loop = check_for_freq_change();

            }

            vr.Release();
            vr.Dispose();
        }


        private static String get_Pulse_Name(Pulse_Mode mode)
        {
            if (mode == Pulse_Mode.Not_In_Sync)
            {
                double saw_freq = saw_angle_freq / Math.PI / 2.0;
                return String.Format("{0:f2}", saw_freq).PadLeft(6);
            }
            if(mode == Pulse_Mode.P_Wide_3)
                return "Wide 3 Pulse";

            String[] mode_name_type = mode.ToString().Split("_");
            String mode_name = "";
            if (mode_name_type[0] == "SP") mode_name = "Shifted ";

            mode_name += mode_name_type[1] + " Pulse";

            return mode_name;
        }
        static void generate_status_video(String output_path)
        {
            reset_control_variables();
            reset_all_variables();

            DateTime dt = DateTime.Now;
            String gen_time = dt.ToString("yyyy-MM-dd_HH-mm-ss");

            Int32 sound_block_count = 0;
            Boolean temp = true;

            int image_width = 500;
            int image_height = 1000;
            int movie_div = 3000;

            String fileName = output_path + "\\" + gen_time + ".avi";
            VideoWriter vr = new VideoWriter(fileName, OpenCvSharp.FourCC.H264, div_freq / movie_div, new OpenCvSharp.Size(image_width, image_height));

            if (!vr.IsOpened())
            {
                return;
            }

            bool loop = true;
            while (loop)
            {
                sin_time += 1.00 / div_freq;
                saw_time += 1.00 / div_freq;

                if (sound_block_count % movie_div == 0 && temp)
                {
                    sin_time = 0;
                    saw_time = 0;
                    Bitmap image = new(image_width, image_height);
                    Graphics g = Graphics.FromImage(image);
                    g.FillRectangle(new SolidBrush(Color.White), 0, 0, image_width, image_height);
                    

                    calculate_tokyuu_1000_1500_IGBT(brake, !mascon_off, mascon_count != mascon_off_count, Math.PI * 2.0 / 3.0 * 0, wave_stat);

                    FontFamily title_fontFamily = new FontFamily("Arial Rounded MT Bold");
                    Font title_fnt = new Font(
                       title_fontFamily,
                       40,
                       FontStyle.Regular,
                       GraphicsUnit.Pixel);

                    FontFamily val_fontFamily = new FontFamily("Arial Rounded MT Bold");
                    Font val_fnt = new Font(
                       val_fontFamily,
                       50,
                       FontStyle.Regular,
                       GraphicsUnit.Pixel);

                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 200, 200)), 0, 0, image_width, 68 - 0);
                    g.DrawString("Pulse Mode", title_fnt, Brushes.Black, 17, 13);
                    g.FillRectangle(Brushes.Red, 0, 68, image_width, 8);
                    g.DrawString(get_Pulse_Name(video_pulse_mode), val_fnt, Brushes.Black, 17, 100);

                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 200, 200)), 0, 226, image_width, 291 - 226);
                    g.DrawString("Sine Freq[Hz]", title_fnt, Brushes.Black, 17, 236);
                    g.FillRectangle(Brushes.Red, 0, 291, image_width, 8);
                    double sine_freq = sin_angle_freq / Math.PI / 2;
                    g.DrawString(String.Format("{0:f2}", sine_freq).PadLeft(6), val_fnt, Brushes.Black, 17, 323);

                    g.FillRectangle(new SolidBrush(Color.FromArgb(255, 200, 200)), 0, 447, image_width, 513 - 447);
                    g.DrawString("Sine Amplitude[%]", title_fnt, Brushes.Black, 17, 457);
                    g.FillRectangle(Brushes.Red, 0, 513, image_width, 8);
                    g.DrawString(String.Format("{0:f2}", video_sine_amplitude*100).PadLeft(6), val_fnt, Brushes.Black, 17, 548);

                    g.FillRectangle(new SolidBrush(Color.FromArgb(200,200,255)), 0, 669, image_width, 735- 669);
                    g.DrawString("Freerun", title_fnt, Brushes.Black, 17, 679);
                    g.FillRectangle(Brushes.Blue, 0, 735, image_width, 8);
                    g.DrawString((mascon_off).ToString(), val_fnt, Brushes.Black, 17, 750);

                    g.FillRectangle(new SolidBrush(Color.FromArgb(200, 200, 255)), 0, 847, image_width, 913 - 847);
                    g.DrawString("Brake", title_fnt, Brushes.Black, 17, 857);
                    g.FillRectangle(Brushes.Blue, 0, 913, image_width, 8);
                    g.DrawString(brake.ToString(), val_fnt, Brushes.Black, 17, 930);




                    MemoryStream ms = new MemoryStream();
                    image.Save(ms, ImageFormat.Png);
                    byte[] img = ms.GetBuffer();
                    Mat mat = OpenCvSharp.Mat.FromImageData(img);

                    Cv2.ImShow("Wave Status View", mat);
                    Cv2.WaitKey(1);

                    vr.Write(mat);




                    g.Dispose();
                    image.Dispose();

                    temp = false;
                }
                else if (sound_block_count % movie_div != 0)
                {
                    temp = true;
                }

                sound_block_count++;

                loop = check_for_freq_change();
            }

            vr.Release();
            vr.Dispose();
        }
        static void Main(string[] args)
        {
            String output_path = "";
            while(output_path.Length == 0)
            {
                Console.Write("Enter the export path for vvvf sound : ");
                output_path = Console.ReadLine();
                if(output_path.Length == 0)
                {
                    Console.WriteLine("Error. Reenter a path.");
                }
            }

            DateTime startDt = DateTime.Now;

            generate_sound(output_path);
            //generate_video(output_path);
            generate_status_video(output_path);

            DateTime endDt = DateTime.Now;

            TimeSpan ts = endDt - startDt;

            Console.WriteLine("Time took to generate vvvf sound : " + ts.TotalSeconds);
        }
    }
}
