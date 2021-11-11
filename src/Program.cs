﻿using System; 
using System.IO;
using static VVVF_Generator_Porting.vvvf_wave;

namespace VVVF_Generator_Porting
{
    internal class Program
    {

        static double count = 0;
        static int div_dreq = 192 * 1000;
        static void generate_sound(String output_path)
        {

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
            writer.Write(div_dreq); // SAMPLING FREQ
            writer.Write(div_dreq); // BYTES IN 1SEC
            writer.Write(new byte[] { 0x01, 0x00 }); // Block Size = 1
            writer.Write(new byte[] { 0x08, 0x00 }); // 1 Sample bits
            writer.Write(0x61746164);
            writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x00 }); //WAVE SIZE

            Boolean do_frequency_change = true;
            Boolean brake = false;

            Int32 sound_block_count = 0;

            double wave_stat = 0;
            int mascon_count = 0;
            bool mason_off = false;

            while (true)
            {
                vvvf_wave.sin_time += 1.00 / div_dreq;
                vvvf_wave.saw_time += 1.00 / div_dreq;

                Wave_Values wv_U = calculate_toyo_IGBT(brake, Math.PI * 2.0 / 3.0 * 0, wave_stat );
                Wave_Values wv_V = calculate_toyo_IGBT(brake, Math.PI * 2.0 / 3.0 * 1, wave_stat );

                for (int i = 0; i < 1; i++)
                {
                    if (wv_U.pwm_value - wv_V.pwm_value > 0) writer.Write((byte)0xB0);
                    else if (wv_U.pwm_value - wv_V.pwm_value < 0) writer.Write((byte)0x50);
                    else writer.Write((byte)0x80);
                }
                sound_block_count++;

                count++;
                if (count % 60 == 0 && do_frequency_change)
                {
                    double sin_new_angle_freq = sin_angle_freq;
                    if (!brake) sin_new_angle_freq += Math.PI / 500 * 1.5;
                    else sin_new_angle_freq -= Math.PI / 500 * 1.5;
                    double amp = sin_angle_freq / sin_new_angle_freq;
                    sin_angle_freq = sin_new_angle_freq;
                    sin_time = amp * sin_time;
                }

                if (sin_angle_freq / 2 / Math.PI > 100 && !brake && do_frequency_change)
                {
                    do_frequency_change = false;
                    count = 0;
                }
                else if (count / div_dreq > 2 && !do_frequency_change)
                {
                    do_frequency_change = true;
                    brake = true;
                }
                else if (sin_angle_freq / 2 / Math.PI < 0 && brake && do_frequency_change) break;


                if (do_frequency_change)
                {
                    if(mason_off == true) mason_off = false;
                    wave_stat = sin_angle_freq / (Math.PI * 2) * mascon_count / 80000.0;
                    if (++mascon_count > 80000) mascon_count = 80000;
                }
                else
                {
                    if (mason_off == false) mason_off = true;
                    wave_stat = sin_angle_freq / (Math.PI * 2) * mascon_count / 80000.0;
                    if (--mascon_count < 0) mascon_count = 0;
                }

            }

            writer.Seek(4, SeekOrigin.Begin);
            writer.Write(sound_block_count + 36);

            writer.Seek(40, SeekOrigin.Begin);
            writer.Write(sound_block_count);

            writer.Close();
        }
        static void Main(string[] args)
        {
            Console.Write("Enter the export path for vvvf sound : ");
            String output_path = Console.ReadLine();

            DateTime startDt = DateTime.Now;

            generate_sound(output_path);

            DateTime endDt = DateTime.Now;

            TimeSpan ts = endDt - startDt;

            Console.WriteLine("Time took to generate vvvf sound : " + ts.TotalSeconds);
        }
    }
}
